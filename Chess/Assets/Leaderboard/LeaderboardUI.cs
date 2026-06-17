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
        public Button submitScoreButton;

        [Header("模式筛选")]
        public TMP_Dropdown modeDropdown;

        [Header("玩家名显示")]
        public TMP_Text playerNameText;

        [Header("积分显示")]
        public TMP_Text scoreDisplayText;

        [Header("游戏玩家引用")]
        public Player player;

        [Header("状态显示")]
        public GameObject loadingIndicator;
        public TMP_Text myRankText;

        [Header("默认玩家名")]
        public string currentPlayerName = "Player";

        [Header("设置")]
        public int maxEntries = 20;
        public string gameMode = "default";
        public bool showOnStart = false;

        private static readonly Dictionary<string, string> ModeDisplayNames = new Dictionary<string, string>
        {
            { "robot", "Robot" },
            { "local", "Local" },
            { "online", "Online" },
            { "default", "Default" }
        };

        private static readonly Dictionary<string, string> DisplayNameToMode = new Dictionary<string, string>
        {
            { "全部", "all" },
            { "人机对战", "robot" },
            { "本地双人", "local" },
            { "在线对战", "online" }
        };

        private static readonly HashSet<string> ValidApiModes = new HashSet<string>
        {
            "all", "robot", "local", "online", "default"
        };

        private bool _bindingsInitialized;
        private bool _isLoading;

        private void OnEnable()
        {
            EnsureBindings();
        }

        private void Start()
        {
            EnsureBindings();
        }

        private void EnsureBindings()
        {
            if (_bindingsInitialized) return;
            _bindingsInitialized = true;

            if (refreshButton != null)
            {
                refreshButton.onClick.RemoveListener(RefreshData);
                refreshButton.onClick.AddListener(RefreshData);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HideLeaderboard);
                closeButton.onClick.AddListener(HideLeaderboard);
            }

            if (submitScoreButton != null)
            {
                submitScoreButton.onClick.RemoveListener(OnSubmitScoreClicked);
                submitScoreButton.onClick.AddListener(OnSubmitScoreClicked);
            }

            if (modeDropdown != null)
            {
                modeDropdown.onValueChanged.RemoveListener(OnModeChanged);
                modeDropdown.onValueChanged.AddListener(OnModeChanged);
            }

            if (playerNameText != null)
            {
                var name = (player != null) ? player.GetLeaderboardPlayerName() : currentPlayerName;
                playerNameText.text = !string.IsNullOrEmpty(name) ? name : currentPlayerName;
            }

            if (player != null && !string.IsNullOrEmpty(currentPlayerName) && currentPlayerName != "Player")
                player.SetLeaderboardPlayerName(currentPlayerName);

            if (panel != null && !panel.activeSelf && showOnStart)
                panel.SetActive(true);

            SetLoading(false);
            UpdateMyRankText(null);
            RefreshScoreDisplay();

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
            Debug.Log($"[LeaderboardUI] ToggleLeaderboard: panel={(panel != null ? panel.name : "null")}, activeSelf={(panel != null ? panel.activeSelf.ToString() : "N/A")}");
            if (panel != null && panel.activeSelf)
                HideLeaderboard();
            else
                ShowLeaderboard();
        }

        public void ShowLeaderboard()
        {
            Debug.Log("[LeaderboardUI] ShowLeaderboard called");
            if (panel != null)
                panel.SetActive(true);
            if (playerNameText != null && player != null)
                playerNameText.text = player.GetLeaderboardPlayerName();
            RefreshData();
            RefreshScoreDisplay();
        }

        public void HideLeaderboard()
        {
            Debug.Log("[LeaderboardUI] HideLeaderboard called");
            if (panel != null)
                panel.SetActive(false);
        }

        public void RefreshData()
        {
            if (_isLoading) return;

            var selectedMode = GetSelectedGameMode();
            if (selectedMode == "all")
                StartCoroutine(LoadAllModesData());
            else
                StartCoroutine(LoadSingleModeData(selectedMode));
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
                return;

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
                    if (resp.success && resp.data != null && resp.data.rank > 0)
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
                return;

            foreach (var entry in resp.data)
                CreateEntry(entry, showMode: true);
        }

        private void CreateEntry(ScoreEntry entry, bool showMode)
        {
            if (entryPrefab == null || contentParent == null) return;

            var go = Instantiate(entryPrefab, contentParent);

            // 旧版 Text
            var texts = go.GetComponentsInChildren<Text>();
            foreach (var t in texts)
                ApplyEntryText(t, t.name, entry, showMode, t);

            // TMP_Text
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

            if (nameLower.Contains("rank") || nameLower.Contains("排名") || nameLower.Contains("序号"))
                SetTextValue(entry.rank.ToString());
            else if (nameLower.Contains("name") || nameLower.Contains("玩家") || nameLower.Contains("姓名") || nameLower.Contains("用户"))
                SetTextValue(entry.player_name);
            else if (nameLower.Contains("score") || nameLower.Contains("分数") || nameLower.Contains("积分") || nameLower.Contains("得分"))
                SetTextValue(entry.score.ToString());
            else if ((nameLower.Contains("mode") || nameLower.Contains("模式")) && showMode)
                SetTextValue(GetModeDisplayName(entry.game_mode));
            else if (nameLower.Contains("date") || nameLower.Contains("日期") || nameLower.Contains("时间"))
                SetTextValue(FormatDate(entry.created_at));
            else
            {
                Debug.LogWarning($"[LeaderboardUI] ApplyEntryText: unmatched component name '{objName}' on {textComp.gameObject.name}");
            }
            return;

            void SetTextValue(string val)
            {
                if (legacyText != null)
                    legacyText.text = val;
                else if (textComp is TMP_Text tmp)
                    tmp.text = val;
            }
        }

        private void ClearEntries()
        {
            if (contentParent == null) return;
            for (int i = contentParent.childCount - 1; i >= 0; i--)
                Destroy(contentParent.GetChild(i).gameObject);
        }

        private void OnModeChanged(int index)
        {
            RefreshData();
            RefreshScoreDisplay();
        }

        private void OnSubmitScoreClicked()
        {
            var name = GetCurrentPlayerName();
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("[LeaderboardUI] Cannot submit score: player name is empty");
                return;
            }

            var mode = GetSelectedGameMode();
            if (mode == "all") mode = "robot";

            StartCoroutine(LeaderboardAPI.GetPlayerRank(name, mode,
                onSuccess: resp =>
                {
                    var score = (resp.success && resp.data != null) ? resp.data.score : 0;
                    if (score > 0)
                    {
                        Debug.Log($"[LeaderboardUI] Submitting current score: {score}");
                        StartCoroutine(LeaderboardAPI.SubmitScore(
                            name,
                            score,
                            mode,
                            onSuccess: submitResp =>
                            {
                                if (submitResp.success)
                                {
                                    Debug.Log($"[LeaderboardUI] Score submitted! Rank: #{submitResp.data.rank}");
                                    if (player != null)
                                        player.FetchPlayerScores();
                                    RefreshData();
                                    RefreshScoreDisplay();
                                }
                            },
                            onError: err => Debug.LogWarning($"[LeaderboardUI] Submit failed: {err}")
                        ));
                    }
                    else
                    {
                        Debug.LogWarning("[LeaderboardUI] No current score to submit");
                    }
                },
                onError: err => Debug.LogWarning($"[LeaderboardUI] Failed to get current score ({err})")
            ));
        }

        private string GetSelectedGameMode()
        {
            if (modeDropdown == null) return gameMode;

            var optionText = modeDropdown.options[modeDropdown.value].text;
            var cleanText = System.Text.RegularExpressions.Regex.Replace(optionText, @"^Option\s*\d+\s*:\s*", "");

            if (ValidApiModes.Contains(cleanText))
                return cleanText;

            if (DisplayNameToMode.TryGetValue(cleanText, out var mode))
                return mode;

            return gameMode;
        }

        private string GetCurrentPlayerName()
        {
            if (player != null)
            {
                var playerName = player.GetLeaderboardPlayerName();
                if (!string.IsNullOrEmpty(playerName))
                    return playerName;
            }
            if (playerNameText != null && !string.IsNullOrEmpty(playerNameText.text))
                return playerNameText.text.Trim();
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

        private void RefreshScoreDisplay()
        {
            if (scoreDisplayText == null) return;
            var mode = GetSelectedGameMode();

            if (player != null)
            {
                if (mode == "all")
                {
                    var robotScore = player.GetCurrentScore("robot");
                    var localScore = player.GetCurrentScore("local");
                    var onlineScore = player.GetCurrentScore("online");
                    var maxScore = Mathf.Max(robotScore, localScore, onlineScore);
                    scoreDisplayText.text = $"score:{maxScore}";
                }
                else
                {
                    var score = player.GetCurrentScore(mode);
                    scoreDisplayText.text = $"score:{score}";
                }
            }
            else
            {
                scoreDisplayText.text = "score:--";
            }
        }

        private void OnError(string error)
        {
            Debug.LogError($"[LeaderboardUI] {error}");
        }
    }
}
