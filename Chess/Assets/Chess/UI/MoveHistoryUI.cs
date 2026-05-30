using System.Collections.Generic;
using Chess;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.UI
{
    public class MoveHistoryUI : MonoBehaviour
    {
        public Player player;

        public Color panelColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        public Color headerColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        public Color evenRowColor = new Color(0.14f, 0.14f, 0.14f, 0.9f);
        public Color oddRowColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        public Color moveColor = new Color(0.95f, 0.95f, 0.6f);
        public Color numColor = new Color(0.55f, 0.55f, 0.55f);
        public Color btnColor = new Color(0.25f, 0.25f, 0.3f, 0.9f);

        private GameObject _panel;
        private GameObject _toggleBtn;
        private Transform _contentParent;
        private ScrollRect _scrollRect;
        private readonly List<GameObject> _entries = new();
        private ChessBoard _board;
        private bool _visible;

        private void Awake()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;
            BuildToggleButton(canvas.transform);
        }

        private void BuildToggleButton(Transform canvasTr)
        {
            _toggleBtn = new GameObject("MoveHistoryBtn");
            _toggleBtn.transform.SetParent(canvasTr, false);
            _toggleBtn.layer = 5;

            var btnRt = _toggleBtn.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 0f);
            btnRt.anchorMax = new Vector2(1f, 0f);
            btnRt.offsetMin = new Vector2(-100, 10);
            btnRt.offsetMax = new Vector2(-10, 44);

            _toggleBtn.AddComponent<CanvasRenderer>();
            var btnImg = _toggleBtn.AddComponent<Image>();
            btnImg.color = btnColor;
            btnImg.raycastTarget = true;

            var btn = _toggleBtn.AddComponent<Button>();
            btn.onClick.AddListener(Toggle);

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(_toggleBtn.transform, false);
            txtObj.layer = 5;
            txtObj.AddComponent<RectTransform>();
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "Moves";
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private void EnsurePanel()
        {
            if (_panel != null) return;
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;
            BuildPanel(canvas.transform);
        }

        private void BuildPanel(Transform canvasTr)
        {
            _panel = new GameObject("MoveHistoryPanel");
            _panel.transform.SetParent(canvasTr, false);
            _panel.layer = 5;
            _panel.SetActive(false);

            var panelRt = _panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(1f, 0f);
            panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.offsetMin = new Vector2(-270, 50);
            panelRt.offsetMax = new Vector2(-10, -10);

            _panel.AddComponent<CanvasRenderer>();
            var bg = _panel.AddComponent<Image>();
            bg.color = panelColor;
            bg.raycastTarget = false;

            var titleObj = CreateUIGameObject("Title", _panel.transform);
            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(0, -38);
            titleRt.offsetMax = Vector2.zero;
            titleObj.AddComponent<CanvasRenderer>();
            var titleImg = titleObj.AddComponent<Image>();
            titleImg.color = headerColor;
            titleImg.raycastTarget = false;
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Move History";
            titleText.fontSize = 18;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            var closeObj = CreateUIGameObject("CloseBtn", titleObj.transform);
            var closeRt = closeObj.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.offsetMin = new Vector2(-32, -14);
            closeRt.offsetMax = new Vector2(-4, 14);
            closeObj.AddComponent<CanvasRenderer>();
            var closeImg = closeObj.AddComponent<Image>();
            closeImg.color = new Color(0.8f, 0.25f, 0.25f);
            closeImg.raycastTarget = true;
            var closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Hide);
            var closeTxt = closeObj.AddComponent<TextMeshProUGUI>();
            closeTxt.text = "X";
            closeTxt.fontSize = 14;
            closeTxt.alignment = TextAlignmentOptions.Center;
            closeTxt.color = Color.white;

            var scrollObj = CreateUIGameObject("ScrollView", _panel.transform);
            var scrollRt = scrollObj.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(4, 4);
            scrollRt.offsetMax = new Vector2(-4, -42);
            scrollObj.AddComponent<CanvasRenderer>();
            var vpImg = scrollObj.AddComponent<Image>();
            vpImg.color = Color.clear;
            vpImg.raycastTarget = true;
            scrollObj.AddComponent<Mask>().showMaskGraphic = false;
            _scrollRect = scrollObj.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Elastic;

            var contentObj = CreateUIGameObject("Content", scrollObj.transform);
            var contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 1;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            _contentParent = contentObj.transform;

            _scrollRect.content = contentRt;
            _scrollRect.viewport = panelRt;
        }

        private GameObject CreateUIGameObject(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.layer = 5;
            obj.AddComponent<RectTransform>();
            return obj;
        }

        public void SetBoard(ChessBoard board)
        {
            _board = board;
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            ClearEntries();

            if (_board == null) return;

            var moves = _board.ExecutedMoves;
            if (moves == null || moves.Count == 0)
            {
                AddLabelEntry("No moves yet");
                return;
            }

            for (int i = 0; i < moves.Count; i += 2)
            {
                int num = (i / 2) + 1;
                string w = moves[i].San ?? moves[i].ToString();
                string b = (i + 1 < moves.Count) ? (moves[i + 1].San ?? moves[i + 1].ToString()) : "";
                AddMoveRow(num, w, b, (i / 2) % 2 == 0);
            }

            Canvas.ForceUpdateCanvases();
            if (_scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 0f;
        }

        private void AddMoveRow(int num, string white, string black, bool isEven)
        {
            var row = CreateUIGameObject($"Row_{num}", _contentParent);
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 26);
            row.AddComponent<CanvasRenderer>();
            var rowImg = row.AddComponent<Image>();
            rowImg.color = isEven ? evenRowColor : oddRowColor;
            rowImg.raycastTarget = false;

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = 4;
            hlg.padding = new RectOffset(6, 4, 3, 3);

            AddCell(row.transform, $"{num}.", numColor, FontStyles.Normal, 28);
            AddCell(row.transform, white, moveColor, FontStyles.Bold, 60);
            AddCell(row.transform, black, moveColor, FontStyles.Bold, 60);

            _entries.Add(row);
        }

        private void AddCell(Transform parent, string text, Color color, FontStyles style, float minWidth)
        {
            var cell = CreateUIGameObject("Cell", parent);
            var tmp = cell.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 15;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            var le = cell.AddComponent<LayoutElement>();
            le.minWidth = minWidth;
            le.preferredWidth = minWidth + 20;
        }

        private void AddLabelEntry(string text)
        {
            var obj = CreateUIGameObject("Label", _contentParent);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 15;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.6f, 0.6f, 0.6f);
            tmp.raycastTarget = false;
            _entries.Add(obj);
        }

        private void ClearEntries()
        {
            foreach (var obj in _entries)
                if (obj != null) Destroy(obj);
            _entries.Clear();
        }

        public void Show()
        {
            EnsurePanel();
            if (_panel != null) _panel.SetActive(true);
            _visible = true;
            RefreshDisplay();
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
            _visible = false;
        }

        public void Toggle()
        {
            if (_visible) Hide();
            else Show();
        }

        public void SetToggleButtonVisible(bool visible)
        {
            if (_toggleBtn != null) _toggleBtn.SetActive(visible);
        }
    }
}
