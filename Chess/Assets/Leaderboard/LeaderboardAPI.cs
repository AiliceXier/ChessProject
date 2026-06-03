using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Chess.Leaderboard
{
    public static class LeaderboardAPI
    {
        public static string BASE_URL = "http://121.36.101.82:3000";

        public static IEnumerator SubmitScore(
            string playerName,
            int score,
            string gameMode = "default",
            Action<ScoreSubmitResponse> onSuccess = null,
            Action<string> onError = null)
        {
            var body = new SubmitScoreBody
            {
                player_name = playerName,
                score = score,
                game_mode = gameMode
            };
            var json = JsonUtility.ToJson(body);

            using (var req = UnityWebRequest.Post(BASE_URL + "/score", json, "application/json"))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var resp = JsonUtility.FromJson<ScoreSubmitResponse>(req.downloadHandler.text);
                    if (resp.success)
                        onSuccess?.Invoke(resp);
                    else
                        onError?.Invoke(resp.message ?? "提交失败");
                }
                else
                {
                    onError?.Invoke(req.error);
                }
            }
        }

        public static IEnumerator GetLeaderboard(
            int limit = 10,
            string gameMode = "default",
            Action<LeaderboardResponse> onSuccess = null,
            Action<string> onError = null)
        {
            var url = $"{BASE_URL}/leaderboard?limit={Mathf.Clamp(limit, 1, 100)}&game_mode={gameMode}";

            using (var req = UnityWebRequest.Get(url))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var resp = JsonUtility.FromJson<LeaderboardResponse>(req.downloadHandler.text);
                    if (resp.success)
                        onSuccess?.Invoke(resp);
                    else
                        onError?.Invoke("获取排行榜失败");
                }
                else
                {
                    onError?.Invoke(req.error);
                }
            }
        }

        public static IEnumerator GetModes(
            Action<ModesResponse> onSuccess = null,
            Action<string> onError = null)
        {
            using (var req = UnityWebRequest.Get(BASE_URL + "/modes"))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var resp = JsonUtility.FromJson<ModesResponse>(req.downloadHandler.text);
                    if (resp.success)
                        onSuccess?.Invoke(resp);
                    else
                        onError?.Invoke("获取模式列表失败");
                }
                else
                {
                    onError?.Invoke(req.error);
                }
            }
        }

        public static IEnumerator GetAllModesLeaderboard(
            int limit = 10,
            Action<AllModesLeaderboardResponse> onSuccess = null,
            Action<string> onError = null)
        {
            var url = $"{BASE_URL}/leaderboard/all?limit={Mathf.Clamp(limit, 1, 100)}";

            using (var req = UnityWebRequest.Get(url))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var resp = JsonUtility.FromJson<AllModesLeaderboardResponse>(req.downloadHandler.text);
                    if (resp != null && resp.success)
                        onSuccess?.Invoke(resp);
                    else
                        onError?.Invoke("获取全模式排行榜失败");
                }
                else
                {
                    onError?.Invoke(req.error);
                }
            }
        }

        public static IEnumerator GetPlayerRank(
            string playerName,
            string gameMode = "default",
            Action<PlayerRankResponse> onSuccess = null,
            Action<string> onError = null)
        {
            var url = $"{BASE_URL}/rank/{playerName}?game_mode={gameMode}";

            using (var req = UnityWebRequest.Get(url))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var resp = JsonUtility.FromJson<PlayerRankResponse>(req.downloadHandler.text);
                    if (resp.success)
                        onSuccess?.Invoke(resp);
                    else
                        onError?.Invoke(resp.message ?? "查询失败");
                }
                else
                {
                    onError?.Invoke(req.error);
                }
            }
        }

        public static IEnumerator Ping(
            Action<PingResponse> onSuccess = null,
            Action<string> onError = null)
        {
            using (var req = UnityWebRequest.Get(BASE_URL + "/ping"))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var resp = JsonUtility.FromJson<PingResponse>(req.downloadHandler.text);
                    onSuccess?.Invoke(resp);
                }
                else
                {
                    onError?.Invoke(req.error);
                }
            }
        }

        public static IEnumerator UpdatePlayerName(
            string oldName,
            string newName,
            Action<PlayerRenameResponse> onSuccess = null,
            Action<string> onError = null)
        {
            var body = new RenameBody { new_name = newName };
            var json = JsonUtility.ToJson(body);

            var url = BASE_URL + "/player/" + UnityWebRequest.EscapeURL(oldName);
            using (var req = new UnityWebRequest(url, "PUT"))
            {
                var uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
                uploadHandler.contentType = "application/json";
                req.uploadHandler = uploadHandler;
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var resp = JsonUtility.FromJson<PlayerRenameResponse>(req.downloadHandler.text);
                    if (resp != null && resp.success)
                        onSuccess?.Invoke(resp);
                    else
                        onError?.Invoke(resp?.message ?? "重命名失败");
                }
                else
                {
                    onError?.Invoke(req.error);
                }
            }
        }

        [Serializable]
        private class SubmitScoreBody
        {
            public string player_name;
            public int score;
            public string game_mode;
        }

        [Serializable]
        private class RenameBody
        {
            public string new_name;
        }
    }
}
