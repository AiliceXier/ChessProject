using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ============================================================
// 游戏结束画面 —— 将死 / 逼和时弹出
// ============================================================
public class GameOverUI : MonoBehaviour
{
    private GameObject panel;

    // ============================================================
    // 初始化
    // ============================================================
    void Awake()
    {
        EnsureEventSystem();
        CreateUI();
        Hide();
    }

    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    // ============================================================
    // 创建 UI
    // ============================================================
    void CreateUI()
    {
        // ── Canvas ──
        GameObject canvasObj = new GameObject("GameOverCanvas");
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // 高于升变面板

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // ── 面板 ──
        panel = new GameObject("Panel");
        panel.transform.SetParent(canvasObj.transform, false);

        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.08f, 0.94f);

        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(460, 340);
        prt.anchoredPosition = Vector2.zero;

        // ── 图标（🏆/🤝）──
        CreateText("Icon", panel.transform, "",
                   80, new Vector2(0, 90), new Vector2(460, 100));

        // ── 标题文字 ──
        CreateText("Title", panel.transform, "",
                   42, new Vector2(0, 20), new Vector2(460, 60));

        // ── 副标题 ──
        CreateText("Subtitle", panel.transform, "",
                   24, new Vector2(0, -40), new Vector2(460, 40));

        // ── 重新开始按钮 ──
        CreateButton("BtnRestart", panel.transform, "重新开始",
                     new Vector2(0, -110), new Vector2(200, 60),
                     new Color(0.25f, 0.50f, 0.25f),
                     () => SceneManager.LoadScene(
                         SceneManager.GetActiveScene().buildIndex));

        // ── 存下引用以便 Show 时更新文字 ──
        iconText     = panel.transform.Find("Icon").GetComponent<Text>();
        titleText    = panel.transform.Find("Title").GetComponent<Text>();
        subtitleText = panel.transform.Find("Subtitle").GetComponent<Text>();
    }

    private Text iconText, titleText, subtitleText;

    // ============================================================
    // 显示
    // ============================================================
    public void Show(GameState state)
    {
        switch (state)
        {
            case GameState.WhiteWon:
                iconText.text     = "🏆";
                titleText.text    = "白方胜利！";
                titleText.color   = new Color(1f, 0.92f, 0.55f); // 金色
                subtitleText.text = "将死 —— 白方获胜";
                break;

            case GameState.BlackWon:
                iconText.text     = "🏆";
                titleText.text    = "黑方胜利！";
                titleText.color   = new Color(0.7f, 0.7f, 0.7f); // 银色
                subtitleText.text = "将死 —— 黑方获胜";
                break;

            case GameState.Stalemate:
                iconText.text     = "🤝";
                titleText.text    = "逼和！";
                titleText.color   = new Color(0.7f, 0.7f, 0.85f); // 浅蓝
                subtitleText.text = "无子可动，平局";
                break;
        }

        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }

    // ============================================================
    // UI 构建
    // ============================================================
    Button CreateButton(string name, Transform parent, string label,
                        Vector2 anchoredPos, Vector2 size,
                        Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        Image img = obj.AddComponent<Image>();
        img.color = color;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        ColorBlock cb = btn.colors;
        cb.normalColor = color;
        cb.highlightedColor = color * 1.3f;
        cb.pressedColor = color * 0.7f;
        btn.colors = cb;

        // 按钮文字
        CreateText(name + "_Lbl", obj.transform, label, 28,
                   Vector2.zero, size);

        return btn;
    }

    GameObject CreateText(string name, Transform parent, string content,
                          int fontSize, Vector2 anchoredPos, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        Text text = obj.AddComponent<Text>();
        text.text = content;
        text.font = GetFont();
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        return obj;
    }

    Font GetFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f != null) return f;
        f = Font.CreateDynamicFontFromOSFont("Arial", 14);
        if (f != null) return f;
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
