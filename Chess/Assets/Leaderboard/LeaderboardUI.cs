using System.Collections;
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
        public Button refreshButton;
        public Button closeButton;

        [Header("默认玩家名")]
        public string currentPlayerName = "Player";

        [Header("设置")]
        public int maxEntries = 20;
        public string gameMode = "default";
        public bool showOnStart = false;

        private void Start()
        {
            if (refreshButton != null)
                refreshButton.onClick.AddListener(RefreshData);

            if (closeButton != null)
                closeButton.onClick.AddListener(HideLeaderboard);

            if (panel != null)
                panel.SetActive(showOnStart);

            if (showOnStart)
                RefreshData();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (panel != null && panel.activeSelf)
                    HideLeaderboard();
                else
                    ShowLeaderboard();
            }
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
            StartCoroutine(LeaderboardAPI.GetLeaderboard(
                limit: maxEntries,
                gameMode: gameMode,
                onSuccess: OnDataLoaded,
                onError: OnError
            ));
        }

        private void OnDataLoaded(LeaderboardResponse resp)
        {
            ClearEntries();

            if (resp.data == null || resp.data.Count == 0)
            {
                Debug.Log("[LeaderboardUI] 排行榜数据为空");
                return;
            }

            foreach (var entry in resp.data)
            {
                if (entryPrefab != null && contentParent != null)
                {
                    var go = Instantiate(entryPrefab, contentParent);
                    var texts = go.GetComponentsInChildren<Text>();
                    foreach (var t in texts)
                    {
                        if (t.name.Contains("Rank") || t.name.ToLower().Contains("rank"))
                            t.text = entry.rank.ToString();
                        else if (t.name.Contains("Name") || t.name.ToLower().Contains("name"))
                            t.text = entry.player_name;
                        else if (t.name.Contains("Score") || t.name.ToLower().Contains("score"))
                            t.text = entry.score.ToString();
                    }

                    // 高亮当前玩家
                    if (entry.player_name == currentPlayerName && go.TryGetComponent<Image>(out var img))
                        img.color = new Color(1f, 0.9f, 0.5f);
                }
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

        private void OnError(string error)
        {
            Debug.LogError($"[LeaderboardUI] 加载失败: {error}");
        }
    }
}
