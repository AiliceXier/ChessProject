using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ============================================================
// 升变选择 UI —— 兵到达底线时弹出选择面板
// ============================================================
public class PromotionUI : MonoBehaviour
{
    public event Action<PieceType> OnPieceSelected;

    private GameObject panel;
    private bool isVisible;

    // ============================================================
    // 初始化
    // ============================================================
    void Awake()
    {
        EnsureEventSystem();
        CreateUI();
        Hide();
    }

    /// 确保场景中有 EventSystem（没有的话 UI 按钮不响应点击）
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
    // 创建 UI（精简设计：仅中央面板，无全屏遮罩）
    // ============================================================
    void CreateUI()
    {
        // ── Canvas ──
        GameObject canvasObj = new GameObject("PromotionCanvas");
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // ── 面板：仅占屏幕中央一小块 ──
        panel = new GameObject("Panel");
        panel.transform.SetParent(canvasObj.transform, false);

        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.12f, 0.12f, 0.92f); // 深色半透明

        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(360, 310); // 固定尺寸，不占满屏幕
        prt.anchoredPosition = Vector2.zero;

        // ── 标题 ──
        CreateText("Title", panel.transform, "选择升变兵种", 36,
                   new Vector2(0, 110), new Vector2(360, 50));

        // ── 四个按钮（2×2 排列）──
        float bw = 150, bh = 80;   // 按钮大小
        float gap = 18;            // 间距
        float offX = bw / 2 + gap / 2;
        float offY = bh / 2 + gap / 2;

        CreatePromoButton("BtnQueen",  panel.transform, "♛  皇后",
                          new Vector2(-offX,  offY), new Vector2(bw, bh),
                          new Color(0.55f, 0.45f, 0.08f),
                          () => Select(PieceType.Queen));

        CreatePromoButton("BtnRook",   panel.transform, "♜  战车",
                          new Vector2( offX,  offY), new Vector2(bw, bh),
                          new Color(0.35f, 0.38f, 0.48f),
                          () => Select(PieceType.Rook));

        CreatePromoButton("BtnBishop", panel.transform, "♝  主教",
                          new Vector2(-offX, -offY), new Vector2(bw, bh),
                          new Color(0.42f, 0.20f, 0.42f),
                          () => Select(PieceType.Bishop));

        CreatePromoButton("BtnKnight", panel.transform, "♞  骑士",
                          new Vector2( offX, -offY), new Vector2(bw, bh),
                          new Color(0.25f, 0.45f, 0.25f),
                          () => Select(PieceType.Knight));
    }

    // ============================================================
    // 显示 / 隐藏
    // ============================================================
    public void Show()
    {
        panel.SetActive(true);
        isVisible = true;
    }

    public void Hide()
    {
        panel.SetActive(false);
        isVisible = false;
    }

    public bool IsVisible() => isVisible;

    // ============================================================
    // 内部
    // ============================================================
    void Select(PieceType type)
    {
        Hide();
        OnPieceSelected?.Invoke(type);
    }

    /// 创建一个升变按钮
    Button CreatePromoButton(string name, Transform parent, string label,
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

        // 悬停高亮效果
        ColorBlock cb = btn.colors;
        cb.normalColor = color;
        cb.highlightedColor = color * 1.35f;
        cb.pressedColor = color * 0.75f;
        cb.disabledColor = Color.gray;
        btn.colors = cb;

        // 按钮文字
        CreateText(name + "_Lbl", obj.transform, label, 28,
                   Vector2.zero, size);

        return btn;
    }

    /// 创建文字
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
        text.raycastTarget = false; // 不阻挡按钮点击

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        return obj;
    }

    /// 获取可用字体（兼容不同 Unity 版本）
    Font GetFont()
    {
        // Unity 2022+ 使用 LegacyRuntime.ttf
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f != null) return f;

        // 降级：尝试 Arial
        f = Font.CreateDynamicFontFromOSFont("Arial", 14);
        if (f != null) return f;

        // 最终降级
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
