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
            Debug.Log($"[LeaderboardUI] Start() - panel:{panel != null} openBtn:{openButton != null} refreshBtn:{refreshButton != null} closeBtn:{closeButton != null} modeDD:{modeDropdown != null} nameInput:{playerNameInput != null} player:{player != null} loading:{loadingIndicator != null} myRank:{myRankText != null} entryPrefab:{entryPrefab != null} contentParent:{contentParent != null}");

            if (openButton != null)
            {
                openButton.onClick.AddListener(ToggleLeaderboard);
                Debug.Log("[LeaderboardUI] openButton listener added");
            }
            else
                Debug.LogError("[LeaderboardUI] openButton is NULL!");

            if (refreshButton != null)
                refreshButton.onClick.AddListener(RefreshData);

            if (closeButton != null)
                closeButton.onClick.AddListener(HideLeaderboard);

            if (modeDropdown != null)
            {
                modeDropdown.onValueChanged.AddListener(OnModeChanged);
                Debug.Log($"[LeaderboardUI] modeDropdown listener added, options count={modeDropdown.options.Count}, value={modeDropdown.value}");
                for (int i = 0; i < modeDropdown.options.Count; i++)
                    Debug.Log($"[LeaderboardUI]   option[{i}]='{modeDropdown.options[i].text}'");
            }
            else
                Debug.LogError("[LeaderboardUI] modeDropdown is NULL!");

            if (playerNameInput != null)
            {
                playerNameInput.text = currentPlayerName;
                playerNameInput.onEndEdit.AddListener(OnPlayerNameChanged);
                Debug.Log($"[LeaderboardUI] playerNameInput init, text='{playerNameInput.text}'");
            }
            else
                Debug.LogError("[LeaderboardUI] playerNameInput is NULL!");

            if (player != null)
            {
                player.SetLeaderboardPlayerName(currentPlayerName);
                Debug.Log("[LeaderboardUI] player.SetLeaderboardPlayerName called");
            }
            else
                Debug.LogWarning("[LeaderboardUI] player ref is NULL");

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
            Debug.Log($"[LeaderboardUI] ToggleLeaderboard() - panel active={panel?.activeSelf}");
            if (panel != null && panel.activeSelf)
                HideLeaderboard();
            else
                ShowLeaderboard();
        }

        public void ShowLeaderboard()
        {
            Debug.Log("[LeaderboardUI] ShowLeaderboard()");
            if (panel != null)
                panel.SetActive(true);
            RefreshData();
        }

        public void HideLeaderboard()
        {
            Debug.Log("[LeaderboardUI] HideLeaderboard()");
            if (panel != null)
                panel.SetActive(false);
        }

        public void RefreshData()
        {
            Debug.Log($"[LeaderboardUI] RefreshData() _isLoading={_isLoading}");
            if (_isLoading) return;

            var selectedMode = GetSelectedGameMode();
            Debug.Log($"[LeaderboardUI] selectedMode='{selectedMode}'");

            if (selectedMode == "all")
            {
                Debug.Log("[LeaderboardUI] → LoadAllModesData()");
                StartCoroutine(LoadAllModesData());
            }
            else
            {
                Debug.Log($"[LeaderboardUI] → LoadSingleModeData('{selectedMode}')");
                StartCoroutine(LoadSingleModeData(selectedMode));
            }
        }

        private IEnumerator LoadSingleModeData(string mode)
        {
            Debug.Log($"[LeaderboardUI] LoadSingleModeData('{mode}') START url={LeaderboardAPI.BASE_URL}/leaderboard?limit={maxEntries}&game_mode={mode}");
            SetLoading(true);
            yield return LeaderboardAPI.GetLeaderboard(
                limit: maxEntries,
                gameMode: mode,
                onSuccess: OnDataLoaded,
                onError: OnError
            );
            Debug.Log($"[LeaderboardUI] LoadSingleModeData('{mode}') DONE");
            SetLoading(false);

            StartCoroutine(LoadMyRank(mode));
        }

        private IEnumerator LoadAllModesData()
        {
            Debug.Log($"[LeaderboardUI] LoadAllModesData() START url={LeaderboardAPI.BASE_URL}/leaderboard/all?limit={maxEntries}");
            SetLoading(true);

            yield return LeaderboardAPI.GetAllModesLeaderboard(
                limit: maxEntries,
                onSuccess: OnAllModesDataLoaded,
                onError: OnError
            );

            Debug.Log("[LeaderboardUI] LoadAllModesData() DONE");
            SetLoading(false);
        }

        private void OnAllModesDataLoaded(AllModesLeaderboardResponse resp)
        {
            Debug.Log($"[LeaderboardUI] OnAllModesDataLoaded() success={resp.success}, data count={(resp.data != null ? resp.data.Count : 0)}");
            ClearEntries();

            if (resp.data == null || resp.data.Count == 0)
            {
                Debug.LogWarning("[LeaderboardUI] 全模式排行榜数据为空");
                return;
            }

            var allEntries = new List<ScoreEntry>();
            foreach (var modeData in resp.data)
            {
                Debug.Log($"[LeaderboardUI]   mode='{modeData.game_mode}', entries={modeData.entries?.Count ?? 0}");
                if (modeData.entries != null)
                    allEntries.AddRange(modeData.entries);
            }

            allEntries.Sort((a, b) => b.score.CompareTo(a.score));

            Debug.Log($"[LeaderboardUI] rendering {System.Math.Min(allEntries.Count, maxEntries)} entries (showMode=true)");
            for (int i = 0; i < allEntries.Count && i < maxEntries; i++)
            {
                allEntries[i].rank = i + 1;
                CreateEntry(allEntries[i], showMode: true);
            }
        }

        private IEnumerator LoadMyRank(string mode)
        {
            var name = GetCurrentPlayerName();
            Debug.Log($"[LeaderboardUI] LoadMyRank('{mode}') player='{name}'");
            if (string.IsNullOrEmpty(name)) yield break;

            yield return LeaderboardAPI.GetPlayerRank(
                name,
                mode,
                onSuccess: resp =>
                {
                    Debug.Log($"[LeaderboardUI] GetPlayerRank OK - rank={resp.data?.rank}");
                    if (resp.success && resp.data != null)
                        UpdateMyRankText(resp.data.rank);
                    else
                        UpdateMyRankText(null);
                },
                onError: err =>
                {
                    Debug.LogWarning($"[LeaderboardUI] GetPlayerRank ERROR: {err}");
                    UpdateMyRankText(null);
                }
            );
        }

        private void OnDataLoaded(LeaderboardResponse resp)
        {
            Debug.Log($"[LeaderboardUI] OnDataLoaded() success={resp.success}, data count={(resp.data != null ? resp.data.Count : 0)}");
            ClearEntries();

            if (resp.data == null || resp.data.Count == 0)
            {
                Debug.LogWarning("[LeaderboardUI] 排行榜数据为空");
                return;
            }

            var selectedMode = GetSelectedGameMode();
            var showMode = selectedMode == "all";

            Debug.Log($"[LeaderboardUI] rendering {resp.data.Count} entries, showMode={showMode}");
            foreach (var entry in resp.data)
            {
                Debug.Log($"[LeaderboardUI]   entry: rank={entry.rank} name='{entry.player_name}' score={entry.score} mode='{entry.game_mode}' date='{entry.created_at}'");
                CreateEntry(entry, showMode);
            }
        }

        private void CreateEntry(ScoreEntry entry, bool showMode)
        {
            if (entryPrefab == null)
            {
                Debug.LogError("[LeaderboardUI] CreateEntry FAIL - entryPrefab is null");
                return;
            }
            if (contentParent == null)
            {
                Debug.LogError("[LeaderboardUI] CreateEntry FAIL - contentParent is null");
                return;
            }

            var go = Instantiate(entryPrefab, contentParent);
            Debug.Log($"[LeaderboardUI] CreateEntry() go={go.name}, childCount={go.transform.childCount}");

            // 处理旧版 Text
            var texts = go.GetComponentsInChildren<Text>();
            Debug.Log($"[LeaderboardUI]   legacy Text components: {texts.Length}");
            foreach (var t in texts)
                ApplyEntryText(t, t.name, entry, showMode, t);

            // 处理 TMP_Text
            var tmpTexts = go.GetComponentsInChildren<TMP_Text>();
            Debug.Log($"[LeaderboardUI]   TMP_Text components: {tmpTexts.Length}");
            foreach (var t in tmpTexts)
                ApplyEntryText(t, t.name, entry, showMode, null);

            if (entry.player_name == GetCurrentPlayerName())
            {
                Debug.Log($"[LeaderboardUI]   highlight current player: {entry.player_name}");
                if (go.TryGetComponent<Image>(out var img))
                    img.color = new Color(1f, 0.9f, 0.5f);
            }

            if (entry.rank <= 3)
            {
                Debug.Log($"[LeaderboardUI]   medal color: rank={entry.rank}");
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

            void SetTextValue(string key, string val)
            {
                if (legacyText != null)
                    legacyText.text = val;
                else if (textComp is TMP_Text tmp)
                    tmp.text = val;
                Debug.Log($"[LeaderboardUI]   SetText '{key}'='{val}' on '{objName}' (legacy={legacyText != null})");
            }
        }

        private void ClearEntries()
        {
            if (contentParent == null) return;
            Debug.Log($"[LeaderboardUI] ClearEntries() removing {contentParent.childCount} children");
            for (int i = contentParent.childCount - 1; i >= 0; i--)
            {
                Destroy(contentParent.GetChild(i).gameObject);
            }
        }

        private void OnModeChanged(int index)
        {
            string optionText = modeDropdown != null && index < modeDropdown.options.Count
                ? modeDropdown.options[index].text : "???";
            Debug.Log($"[LeaderboardUI] OnModeChanged(index={index}, text='{optionText}')");
            RefreshData();
        }

        private void OnPlayerNameChanged(string newName)
        {
            Debug.Log($"[LeaderboardUI] OnPlayerNameChanged('{newName}')");
            if (!string.IsNullOrEmpty(newName))
            {
                currentPlayerName = newName.Trim();
                if (player != null)
                    player.SetLeaderboardPlayerName(currentPlayerName);
            }
        }

        private string GetSelectedGameMode()
        {
            if (modeDropdown == null)
            {
                Debug.LogWarning($"[LeaderboardUI] GetSelectedGameMode() modeDropdown is null, return gameMode='{gameMode}'");
                return gameMode;
            }

            var optionText = modeDropdown.options[modeDropdown.value].text;
            var cleanText = System.Text.RegularExpressions.Regex.Replace(optionText, @"^Option\s*\d+\s*:\s*", "");
            Debug.Log($"[LeaderboardUI] GetSelectedGameMode() optionText='{optionText}' cleanText='{cleanText}'");

            // 直接是有效 API 模式名 (all, robot, local, online, default)
            var validModes = new HashSet<string> { "all", "robot", "local", "online", "default" };
            if (validModes.Contains(cleanText))
            {
                Debug.Log($"[LeaderboardUI]   cleanText is valid API mode → '{cleanText}'");
                return cleanText;
            }

            // 尝试中文名映射
            if (DisplayNameToMode.TryGetValue(cleanText, out var mode))
            {
                Debug.Log($"[LeaderboardUI]   chinese match → '{mode}'");
                return mode;
            }

            Debug.LogWarning($"[LeaderboardUI]   no match for '{cleanText}', fallback to gameMode='{gameMode}'");
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
            Debug.Log($"[LeaderboardUI] SetLoading({loading})");
            if (loadingIndicator != null)
                loadingIndicator.SetActive(loading);
        }

        private void UpdateMyRankText(int? rank)
        {
            var text = rank.HasValue ? $"我的排名：第 {rank.Value} 名" : "我的排名：--";
            Debug.Log($"[LeaderboardUI] UpdateMyRankText: '{text}'");
            if (myRankText != null)
                myRankText.text = text;
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
            Debug.LogError($"[LeaderboardUI] OnError: {error}");
        }
    }
}
