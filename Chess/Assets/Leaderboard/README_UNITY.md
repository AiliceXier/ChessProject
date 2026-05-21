# 积分榜 Unity 客户端集成说明

## 文件清单

| 文件 | 说明 |
|------|------|
| `LeaderboardResponse.cs` | API 响应数据模型，与 JsonUtility 兼容 |
| `LeaderboardAPI.cs` | 封装所有 HTTP 请求，使用 UnityWebRequest + 协程 |
| `LeaderboardUI.cs` | 排行榜 UI 控制脚本，挂载到 Canvas GameObject |

## 快速集成

### 1. 修改服务器地址
打开 `LeaderboardAPI.cs`，修改 `BASE_URL` 为你的服务器地址：
```csharp
public static string BASE_URL = "http://你的服务器IP:3000";
```

### 2. 创建 UI 预制体 (Entry Prefab)
在 Hierarchy 中创建一个条目模板：

```
EntryPrefab (Image + LayoutElement)
├── RankText (Text)       -- 排名
├── NameText (Text)       -- 玩家名
└── ScoreText (Text)      -- 分数
```

保存为预制体：拖入 `Assets/Prefabs/`。

### 3. 设置 Canvas
- 创建 Canvas，添加 VerticalLayoutGroup 到 Content 区域
- 添加 `LeaderboardUI` 组件
- 在 Inspector 中绑定：
  - **Panel**: 排行榜面板 GameObject
  - **Content Parent**: ScrollView/Viewport/Content
  - **Entry Prefab**: 上一步创建的条目预制体
  - **Refresh Button**: 刷新按钮
  - **Close Button**: 关闭按钮

### 4. 使用示例

```csharp
using Chess.Leaderboard;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private LeaderboardUI leaderboardUI;

    // 游戏结束时提交分数
    public void OnGameEnd(int finalScore)
    {
        StartCoroutine(LeaderboardAPI.SubmitScore(
            playerName: "Player1",
            score: finalScore,
            onSuccess: (resp) => {
                Debug.Log($"提交成功! 排名: {resp.data.rank}");
                leaderboardUI?.ShowLeaderboard();
            },
            onError: (err) => Debug.LogError($"提交失败: {err}")
        ));
    }

    // 打开排行榜面板
    public void OpenLeaderboard()
    {
        leaderboardUI?.ShowLeaderboard();
    }
}
```

### 5. UnityWebRequest 依赖
此 API 使用 Unity 内建的 `UnityEngine.Networking`，无需额外安装包。

## 可用 API

```csharp
LeaderboardAPI.SubmitScore(playerName, score, gameMode, onSuccess, onError)
LeaderboardAPI.GetLeaderboard(limit, gameMode, onSuccess, onError)
LeaderboardAPI.GetPlayerRank(playerName, gameMode, onSuccess, onError)
LeaderboardAPI.Ping(onSuccess, onError)
```
