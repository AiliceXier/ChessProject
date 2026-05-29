using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.Leaderboard
{
    public class LeaderboardUI : MonoBehaviour
    {
        [Header("容器")]
        public GameObject panel;
        public Transform contentParent;

        [Header("条目预制体/模板")]
        public GameObject entryPrefab;

        [Header("按钮")]
        public Button openButton;
        public Button refreshButton;
        public Button closeButton;

        [Header("模式筛选")]
        public TMP_Dropdown modeDropdown;

        [Header("玩家名输入")]
        public TMP_InputField playerNameInput;

        [Header("游戏玩家引用")]
        public Player player;

        [Header("状态显示")]
        public GameObject loadingIndicator;
        public Text myRankText;

        [Header("默认玩家名")]
        public string currentPlayerName = "Player";

        [Header("设置")]
        public int maxEntries = 20;
        public string gameMode = "default";
        public bool showOnStart = false;

        private static readonly Dictionary<string, string> ModeDisplayNames = new Dictionary<string, string>
        {
            { "robot", "人机对战" },
            { "local", "本地双人" },
            { "online", "在线对战" },
            { "default", "默认" }
        };

        private static readonly Dictionary<string, string> DisplayNameToMode = new Dictionary<string, string>
        {
            { "全部", "all" },
            { "人机对战", "robot" },
            { "本地双人", "local" },
            { "在线对战", "online" }
        };

        private List<string> _serverModes = new List<string>();
        private bool _isLoading;

        private void Start()
        {
            if (openButton != null)
                openButton.onClick.AddListener(ToggleLeaderboard);

            if (refreshButton != null)
                refreshButton.onClick.AddListener(RefreshData);

            if (closeButton != null)
                closeButton.onClick.AddListener(HideLeaderboard);

            if (modeDropdown != null)
                modeDropdown.onValueChanged.AddListener(OnModeChanged);

            if (playerNameInput != null)
            {
                playerNameInput.text = currentPlayerName;
                playerNameInput.onEndEdit.AddListener(OnPlayerNameChanged);
            }

            if (player != null)
            {
                player.SetLeaderboardPlayerName(currentPlayerName);
            }

            if (panel != null)
                panel.SetActive(showOnStart);

            SetLoading(false);
            UpdateMyRankText(null);

            if (showOnStart)
                RefreshData();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                ToggleLeaderboard();
        }

        public void ToggleLeaderboard()
        {
            if (panel != null && panel.activeSelf)
                HideLeaderboard();
            else
                ShowLeaderboard();
        }

        public void ShowLeaderboard()
        {
            if (panel != null)
                panel.SetActive(true);
            RefreshData();
        }

        public void HideLeaderboard()
        {
            if (panel != null)
                panel.SetActive(false);
        }

        public void RefreshData()
        {
            if (_isLoading) return;

            var selectedMode = GetSelectedGameMode();
            if (selectedMode == "all")
            {
                StartCoroutine(LoadAllModesData());
            }
            else
            {
                StartCoroutine(LoadSingleModeData(selectedMode));
            }
        }

        private IEnumerator LoadSingleModeData(string mode)
        {
            SetLoading(true);
            yield return LeaderboardAPI.GetLeaderboard(
                limit: maxEntries,
                gameMode: mode,
                onSuccess: OnDataLoaded,
                onError: OnError
            );
            SetLoading(false);

            StartCoroutine(LoadMyRank(mode));
        }

        private IEnumerator LoadAllModesData()
        {
            SetLoading(true);

            yield return LeaderboardAPI.GetAllModesLeaderboard(
                limit: maxEntries,
                onSuccess: OnAllModesDataLoaded,
                onError: OnError
            );

            SetLoading(false);
        }

        private void OnAllModesDataLoaded(AllModesLeaderboardResponse resp)
        {
            ClearEntries();

            if (resp.data == null || resp.data.Count == 0)
            {
                Debug.Log("[LeaderboardUI] 全模式排行榜数据为空");
                return;
            }

            var allEntries = new List<ScoreEntry>();
            foreach (var modeData in resp.data)
            {
                if (modeData.entries != null)
                    allEntries.AddRange(modeData.entries);
            }

            allEntries.Sort((a, b) => b.score.CompareTo(a.score));

            for (int i = 0; i < allEntries.Count && i < maxEntries; i++)
            {
                allEntries[i].rank = i + 1;
                CreateEntry(allEntries[i], showMode: true);
            }
        }

        private IEnumerator LoadMyRank(string mode)
        {
            var name = GetCurrentPlayerName();
            if (string.IsNullOrEmpty(name)) yield break;

            yield return LeaderboardAPI.GetPlayerRank(
                name,
                mode,
                onSuccess: resp =>
                {
                    if (resp.success && resp.data != null)
                        UpdateMyRankText(resp.data.rank);
                    else
                        UpdateMyRankText(null);
                },
                onError: _ => UpdateMyRankText(null)
            );
        }

        private void OnDataLoaded(LeaderboardResponse resp)
        {
            ClearEntries();

            if (resp.data == null || resp.data.Count == 0)
            {
                Debug.Log("[LeaderboardUI] 排行榜数据为空");
                return;
            }

            var selectedMode = GetSelectedGameMode();
            var showMode = selectedMode == "all";

            foreach (var entry in resp.data)
            {
                CreateEntry(entry, showMode);
            }
        }

        private void CreateEntry(ScoreEntry entry, bool showMode)
        {
            if (entryPrefab == null || contentParent == null) return;

            var go = Instantiate(entryPrefab, contentParent);

            // 处理旧版 Text
            var texts = go.GetComponentsInChildren<Text>();
            foreach (var t in texts)
                ApplyEntryText(t, t.name, entry, showMode, t);

            // 处理 TMP_Text
            var tmpTexts = go.GetComponentsInChildren<TMP_Text>();
            foreach (var t in tmpTexts)
                ApplyEntryText(t, t.name, entry, showMode, null);

            if (entry.player_name == GetCurrentPlayerName())
            {
                if (go.TryGetComponent<Image>(out var img))
                    img.color = new Color(1f, 0.9f, 0.5f);
            }

            if (entry.rank <= 3)
            {
                if (go.TryGetComponent<Image>(out var img))
                {
                    img.color = entry.rank switch
                    {
                        1 => new Color(1f, 0.84f, 0f),
                        2 => new Color(0.75f, 0.75f, 0.75f),
                        3 => new Color(0.8f, 0.5f, 0.2f),
                        _ => img.color
                    };
                }
            }
        }

        private void ApplyEntryText(Component textComp, string objName, ScoreEntry entry, bool showMode, Text legacyText)
        {
            var nameLower = objName.ToLower();

            if (nameLower.Contains("rank"))
                SetTextValue("rank", entry.rank.ToString());
            else if (nameLower.Contains("name"))
                SetTextValue("name", entry.player_name);
            else if (nameLower.Contains("score"))
                SetTextValue("score", entry.score.ToString());
            else if (nameLower.Contains("mode") && showMode)
                SetTextValue("mode", GetModeDisplayName(entry.game_mode));
            else if (nameLower.Contains("date"))
                SetTextValue("date", FormatDate(entry.created_at));
            return;

            void SetTextValue(string _key, string _val)
            {
                if (legacyText != null)
                    legacyText.text = _val;
                else if (textComp is TMP_Text tmp)
                    tmp.text = _val;
            }
        }

        private void ClearEntries()
        {
            if (contentParent == null) return;

            for (int i = contentParent.childCount - 1; i >= 0; i--)
            {
                Destroy(contentParent.GetChild(i).gameObject);
            }
        }

        private void OnModeChanged(int index)
        {
            RefreshData();
        }

        private void OnPlayerNameChanged(string newName)
        {
            if (!string.IsNullOrEmpty(newName))
            {
                currentPlayerName = newName.Trim();
                if (player != null)
                    player.SetLeaderboardPlayerName(currentPlayerName);
            }
        }

        private string GetSelectedGameMode()
        {
            if (modeDropdown == null) return gameMode;

            var optionText = modeDropdown.options[modeDropdown.value].text;
            // 兼容 "Option X: " 前缀
            var cleanText = System.Text.RegularExpressions.Regex.Replace(optionText, @"^Option\s*\d+\s*:\s*", "");
            if (DisplayNameToMode.TryGetValue(cleanText, out var mode))
                return mode;

            return gameMode;
        }

        private string GetCurrentPlayerName()
        {
            if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
                return playerNameInput.text.Trim();
            return currentPlayerName;
        }

        private void SetLoading(bool loading)
        {
            _isLoading = loading;
            if (loadingIndicator != null)
                loadingIndicator.SetActive(loading);
        }

        private void UpdateMyRankText(int? rank)
        {
            if (myRankText != null)
                myRankText.text = rank.HasValue ? $"我的排名：第 {rank.Value} 名" : "我的排名：--";
        }

        private static string GetModeDisplayName(string mode)
        {
            if (string.IsNullOrEmpty(mode)) return "--";
            return ModeDisplayNames.TryGetValue(mode, out var display) ? display : mode;
        }

        private static string FormatDate(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr)) return "--";
            if (dateStr.Length >= 10)
                return dateStr.Substring(0, 10);
            return dateStr;
        }

        private void OnError(string error)
        {
            Debug.LogError($"[LeaderboardUI] 加载失败: {error}");
        }
    }
}
