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

        public Color panelColor = new Color(0.10f, 0.10f, 0.12f, 0.95f);
        public Color headerColor = new Color(0.15f, 0.15f, 0.17f, 1f);
        public Color evenRowColor = new Color(0.13f, 0.13f, 0.15f, 1f);
        public Color oddRowColor = new Color(0.22f, 0.22f, 0.24f, 1f);
        public Color moveColor = new Color(0.80f, 0.63f, 0.43f);
        public Color numColor = new Color(0.47f, 0.47f, 0.47f);
        public Color highlightColor = new Color(1f, 1f, 0.4f, 0.3f);
        public Color btnColor = new Color(0.22f, 0.22f, 0.28f, 0.9f);

        private const float RowHeight = 30f;
        private const float RowSpacing = 1f;
        private const float ContentPadding = 4f;

        private GameObject _panel;
        private GameObject _toggleBtn;
        private RectTransform _contentRt;
        private ScrollRect _scrollRect;
        private readonly List<GameObject> _entries = new();
        private ChessBoard _board;
        private bool _visible;

        private void Awake()
        {
            Debug.Log("[MoveHistoryUI] Awake called");
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[MoveHistoryUI] No Canvas found in Awake!");
                return;
            }
            BuildToggleButton(canvas.transform);
            Debug.Log("[MoveHistoryUI] Toggle button built");
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
            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "Moves";
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
        }

        private void EnsurePanel()
        {
            if (_panel != null) return;
            Debug.Log("[MoveHistoryUI] EnsurePanel - building panel for first time");
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[MoveHistoryUI] No Canvas found in EnsurePanel!");
                return;
            }
            BuildPanel(canvas.transform);
            Debug.Log("[MoveHistoryUI] Panel built successfully, _contentRt=" + (_contentRt != null));
        }

        private void BuildPanel(Transform canvasTr)
        {
            _panel = new GameObject("MoveHistoryPanel");
            _panel.transform.SetParent(canvasTr, false);
            _panel.layer = 5;

            var panelRt = _panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(1f, 0f);
            panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.offsetMin = new Vector2(-270, 50);
            panelRt.offsetMax = new Vector2(-10, -10);

            _panel.AddComponent<CanvasRenderer>();
            var bg = _panel.AddComponent<Image>();
            bg.color = panelColor;
            bg.raycastTarget = true;

            var vlg = _panel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 0;
            vlg.padding = new RectOffset(0, 0, 0, 0);

            BuildHeader(_panel.transform);
            BuildScrollView(_panel.transform);

            _panel.SetActive(false);
            Debug.Log("[MoveHistoryUI] BuildPanel complete");
        }

        private void BuildHeader(Transform parent)
        {
            var headerObj = CreateUIGameObject("Header", parent);
            var headerLe = headerObj.AddComponent<LayoutElement>();
            headerLe.preferredHeight = 36;
            headerLe.flexibleHeight = 0;
            headerObj.AddComponent<CanvasRenderer>();
            var headerImg = headerObj.AddComponent<Image>();
            headerImg.color = headerColor;
            headerImg.raycastTarget = false;

            var headerHlg = headerObj.AddComponent<HorizontalLayoutGroup>();
            headerHlg.childAlignment = TextAnchor.MiddleLeft;
            headerHlg.childControlWidth = true;
            headerHlg.childControlHeight = false;
            headerHlg.childForceExpandWidth = true;
            headerHlg.childForceExpandHeight = false;
            headerHlg.spacing = 4;
            headerHlg.padding = new RectOffset(8, 4, 0, 0);

            var titleObj = CreateUIGameObject("Title", headerObj.transform);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "Move History";
            titleTxt.fontSize = 16;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = Color.white;
            titleTxt.raycastTarget = false;
            var titleLe = titleObj.AddComponent<LayoutElement>();
            titleLe.flexibleWidth = 1;

            var closeObj = CreateUIGameObject("CloseBtn", headerObj.transform);
            closeObj.AddComponent<CanvasRenderer>();
            var closeImg = closeObj.AddComponent<Image>();
            closeImg.color = new Color(0.8f, 0.25f, 0.25f);
            closeImg.raycastTarget = true;
            var closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Hide);
            var closeTxtObj = CreateUIGameObject("Text", closeObj.transform);
            var closeTxtRt = closeTxtObj.GetComponent<RectTransform>();
            closeTxtRt.anchorMin = Vector2.zero;
            closeTxtRt.anchorMax = Vector2.one;
            closeTxtRt.offsetMin = Vector2.zero;
            closeTxtRt.offsetMax = Vector2.zero;
            var closeTxt = closeTxtObj.AddComponent<TextMeshProUGUI>();
            closeTxt.text = "X";
            closeTxt.fontSize = 14;
            closeTxt.alignment = TextAlignmentOptions.Center;
            closeTxt.color = Color.white;
            closeTxt.raycastTarget = false;
            var closeLe = closeObj.AddComponent<LayoutElement>();
            closeLe.minWidth = 28;
            closeLe.preferredWidth = 28;
        }

        private void BuildScrollView(Transform parent)
        {
            var scrollObj = CreateUIGameObject("ScrollView", parent);
            var scrollLe = scrollObj.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1;
            scrollLe.minHeight = 60;
            var scrollRt = scrollObj.GetComponent<RectTransform>();
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
            _contentRt = contentObj.GetComponent<RectTransform>();
            _contentRt.anchorMin = new Vector2(0f, 1f);
            _contentRt.anchorMax = new Vector2(1f, 1f);
            _contentRt.pivot = new Vector2(0.5f, 1f);
            _contentRt.offsetMin = new Vector2(0f, 0f);
            _contentRt.offsetMax = new Vector2(0f, 0f);

            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = (int)RowSpacing;
            vlg.padding = new RectOffset((int)ContentPadding, (int)ContentPadding, (int)ContentPadding, (int)ContentPadding);

            _scrollRect.content = _contentRt;
            _scrollRect.viewport = scrollRt;
        }

        private void UpdateContentSize()
        {
            if (_contentRt == null) return;
            // ContentSizeFitter 会自动处理，只需要强制刷新布局
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRt);
            Debug.Log("[MoveHistoryUI] UpdateContentSize: entries=" + _entries.Count + ", contentHeight=" + _contentRt.rect.height);
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
            Debug.Log("[MoveHistoryUI] SetBoard called, board=" + (board != null) + ", current _board=" + (_board != null));
            _board = board;
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            Debug.Log("[MoveHistoryUI] RefreshDisplay called, _contentRt=" + (_contentRt != null) + ", _board=" + (_board != null));

            if (_contentRt == null)
            {
                Debug.LogWarning("[MoveHistoryUI] RefreshDisplay SKIPPED - _contentRt is null (panel not built yet)");
                return;
            }

            ClearEntries();

            if (_board == null)
            {
                Debug.LogWarning("[MoveHistoryUI] RefreshDisplay - _board is null, showing 'No board' label");
                AddLabelEntry("No board set");
                UpdateContentSize();
                return;
            }

            var moves = _board.ExecutedMoves;
            Debug.Log("[MoveHistoryUI] ExecutedMoves: count=" + (moves != null ? moves.Count : -1));

            if (moves == null || moves.Count == 0)
            {
                Debug.Log("[MoveHistoryUI] No moves yet, showing label");
                AddLabelEntry("No moves yet");
                UpdateContentSize();
                return;
            }

            int totalRows = (moves.Count + 1) / 2;
            Debug.Log("[MoveHistoryUI] Rendering " + moves.Count + " moves in " + totalRows + " rows");

            for (int i = 0; i < moves.Count; i += 2)
            {
                int num = (i / 2) + 1;
                string w = moves[i].San ?? moves[i].ToString();
                string b = (i + 1 < moves.Count) ? (moves[i + 1].San ?? moves[i + 1].ToString()) : "";
                bool isLast = (i + 2 >= moves.Count);
                Debug.Log("[MoveHistoryUI] Row " + num + ": " + w + " | " + b + (isLast ? " (last)" : ""));
                AddMoveRow(num, w, b, (i / 2) % 2 == 0, isLast);
            }

            UpdateContentSize();

            if (_scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 0f;

            Debug.Log("[MoveHistoryUI] RefreshDisplay complete, _entries.Count=" + _entries.Count + ", contentHeight=" + _contentRt.rect.height);
        }

        private void AddMoveRow(int num, string white, string black, bool isEven, bool isLast)
        {
            var row = CreateUIGameObject($"Row_{num}", _contentRt);
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredHeight = RowHeight;
            rowLe.minHeight = RowHeight;
            rowLe.flexibleHeight = 0;

            row.AddComponent<CanvasRenderer>();
            var rowImg = row.AddComponent<Image>();
            if (isLast)
                rowImg.color = highlightColor;
            else
                rowImg.color = isEven ? evenRowColor : oddRowColor;
            rowImg.raycastTarget = false;

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = 4;
            hlg.padding = new RectOffset(6, 6, 0, 0);

            AddTextCell(row.transform, $"{num}.", numColor, FontStyles.Normal, 30, 12);
            AddTextCell(row.transform, white, moveColor, FontStyles.Bold, 90, 14);
            AddTextCell(row.transform, black, moveColor, FontStyles.Bold, 90, 14);

            _entries.Add(row);
        }

        private void AddTextCell(Transform parent, string text, Color color, FontStyles style, float width, float fontSize)
        {
            var cell = CreateUIGameObject("Cell", parent);
            var cellLe = cell.AddComponent<LayoutElement>();
            cellLe.preferredWidth = width;
            cellLe.minWidth = width;
            cellLe.flexibleWidth = 0;

            var cellRt = cell.GetComponent<RectTransform>();
            cellRt.sizeDelta = new Vector2(width, RowHeight);

            var tmp = cell.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
        }

        private void AddLabelEntry(string text)
        {
            var obj = CreateUIGameObject("Label", _contentRt);
            var objLe = obj.AddComponent<LayoutElement>();
            objLe.preferredHeight = RowHeight;
            objLe.minHeight = RowHeight;
            objLe.flexibleHeight = 0;

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.5f, 0.5f, 0.5f);
            tmp.raycastTarget = false;
            _entries.Add(obj);
        }

        private void ClearEntries()
        {
            int count = _entries.Count;
            foreach (var obj in _entries)
                if (obj != null) DestroyImmediate(obj);
            _entries.Clear();
            if (count > 0)
                Debug.Log("[MoveHistoryUI] ClearEntries removed " + count + " entries");
        }

        public void Show()
        {
            Debug.Log("[MoveHistoryUI] Show called, _panel exists=" + (_panel != null));
            EnsurePanel();
            if (_panel != null) _panel.SetActive(true);
            _visible = true;
            RefreshDisplay();
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
            _visible = false;
            Debug.Log("[MoveHistoryUI] Hide called");
        }

        public void Toggle()
        {
            Debug.Log("[MoveHistoryUI] Toggle called, _visible=" + _visible);
            if (_visible) Hide();
            else Show();
        }

        public void SetToggleButtonVisible(bool visible)
        {
            if (_toggleBtn != null) _toggleBtn.SetActive(visible);
            Debug.Log("[MoveHistoryUI] SetToggleButtonVisible=" + visible + ", _toggleBtn exists=" + (_toggleBtn != null));
        }
    }
}
