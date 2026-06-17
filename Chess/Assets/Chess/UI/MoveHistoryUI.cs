using System.Collections;
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

        public GameObject toggleBtnRef;
        public GameObject panelRef;
        public ScrollRect scrollRectRef;
        public RectTransform contentRtRef;

        private const float RowHeight = 30f;
        private const float RowSpacing = 1f;
        private const float ContentPadding = 4f;

        private GameObject _panel;
        private GameObject _toggleBtn;
        private RectTransform _contentRt;
        private RectTransform _scrollViewportRt;
        private ScrollRect _scrollRect;
        private readonly List<GameObject> _entries = new();
        private ChessBoard _board;
        // Online games don't drive moves through a local ChessBoard, so
        // ExecutedMoves stays empty even after a FEN sync. Maintain a
        // separate, incremental SAN list for online play.
        private readonly List<string> _manualMoveSans = new();
        private bool _visible;

        private void Awake()
        {
            if (toggleBtnRef != null)
            {
                _toggleBtn = toggleBtnRef;
            }
            else
            {
                _toggleBtn = GameObject.Find("MovesBtn");
            }

            if (_toggleBtn != null)
            {
                var btn = _toggleBtn.GetComponent<Button>();
                if (btn == null)
                    btn = _toggleBtn.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(Toggle);
            }
            else
            {
                var canvas = FindObjectOfType<Canvas>();
                if (canvas == null) return;
                BuildToggleButton(canvas.transform);
            }
        }

        private void BuildToggleButton(Transform canvasTr)
        {
            _toggleBtn = new GameObject("MoveHistoryBtn");
            _toggleBtn.transform.SetParent(canvasTr, false);
            _toggleBtn.layer = 5;

            var btnRt = _toggleBtn.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0f, 0f);
            btnRt.anchorMax = new Vector2(0f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = new Vector2(89, 144);
            btnRt.sizeDelta = new Vector2(100, 34);

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

            if (panelRef != null && scrollRectRef != null && contentRtRef != null)
            {
                _panel = panelRef;
                _scrollRect = scrollRectRef;
                _contentRt = contentRtRef;
                _scrollViewportRt = _scrollRect.viewport;

                var closeBtnTr = _panel.transform.Find("Header/CloseBtn");
                if (closeBtnTr != null)
                {
                    var closeBtn = closeBtnTr.GetComponent<Button>();
                    if (closeBtn != null)
                        closeBtn.onClick.AddListener(Hide);
                }

                _panel.SetActive(false);
                return;
            }

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;
            BuildTestPanel(canvas.transform);
        }

        private void BuildTestPanel(Transform canvasTr)
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

            var headerObj = new GameObject("Header");
            headerObj.transform.SetParent(_panel.transform, false);
            headerObj.layer = 5;
            var headerRt = headerObj.AddComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(0f, 36f);
            headerRt.anchoredPosition = new Vector2(0f, 0f);
            headerObj.AddComponent<CanvasRenderer>();
            var headerImg = headerObj.AddComponent<Image>();
            headerImg.color = headerColor;

            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            titleObj.layer = 5;
            var titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = Vector2.zero;
            titleRt.anchorMax = Vector2.one;
            titleRt.offsetMin = new Vector2(8, 0);
            titleRt.offsetMax = new Vector2(-30, 0);
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "Move History";
            titleTmp.fontSize = 16;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
            titleTmp.color = Color.white;
            titleTmp.raycastTarget = false;

            var closeObj = new GameObject("CloseBtn");
            closeObj.transform.SetParent(headerObj.transform, false);
            closeObj.layer = 5;
            var closeRt = closeObj.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 0f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 0.5f);
            closeRt.sizeDelta = new Vector2(28f, 0f);
            closeRt.anchoredPosition = new Vector2(-4f, 0f);
            closeObj.AddComponent<CanvasRenderer>();
            var closeImg = closeObj.AddComponent<Image>();
            closeImg.color = new Color(0.8f, 0.25f, 0.25f);
            var closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Hide);
            var closeTxtObj = new GameObject("X");
            closeTxtObj.transform.SetParent(closeObj.transform, false);
            closeTxtObj.layer = 5;
            var closeTxtRt = closeTxtObj.AddComponent<RectTransform>();
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

            var scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(_panel.transform, false);
            scrollView.layer = 5;
            var svRt = scrollView.AddComponent<RectTransform>();
            svRt.anchorMin = new Vector2(0f, 0f);
            svRt.anchorMax = new Vector2(1f, 1f);
            svRt.pivot = new Vector2(0.5f, 0.5f);
            svRt.offsetMin = new Vector2(0f, 0f);
            svRt.offsetMax = new Vector2(0f, -36f);
            _scrollViewportRt = svRt;
            scrollView.AddComponent<CanvasRenderer>();
            var svImg = scrollView.AddComponent<Image>();
            svImg.color = new Color(0.05f, 0.05f, 0.08f, 1f);
            svImg.raycastTarget = true;
            var mask = scrollView.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            _scrollRect = scrollView.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Elastic;

            var content = new GameObject("Content");
            content.transform.SetParent(scrollView.transform, false);
            content.layer = 5;
            _contentRt = content.AddComponent<RectTransform>();
            _contentRt.anchorMin = new Vector2(0f, 1f);
            _contentRt.anchorMax = new Vector2(1f, 1f);
            _contentRt.pivot = new Vector2(0.5f, 1f);
            _contentRt.offsetMin = new Vector2(0f, 0f);
            _contentRt.offsetMax = new Vector2(0f, 0f);

            _scrollRect.content = _contentRt;
            _scrollRect.viewport = _scrollViewportRt;

            _panel.SetActive(false);
        }

        private void UpdateContentSize()
        {
            if (_contentRt == null) return;

            float totalHeight = ContentPadding * 2 + _entries.Count * RowHeight;
            if (_entries.Count > 1)
                totalHeight += (_entries.Count - 1) * RowSpacing;
            _contentRt.sizeDelta = new Vector2(0f, totalHeight);
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

        // Online play doesn't drive moves through a local ChessBoard, so we let
        // the caller push individual SAN strings as the cloud sends updates.
        public void PushMove(string san)
        {
            if (string.IsNullOrEmpty(san)) return;
            _manualMoveSans.Add(san);
            RefreshDisplay();
        }

        // Clear the manually-pushed SAN list (e.g. when starting a new game).
        public void ResetManualMoves()
        {
            _manualMoveSans.Clear();
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            if (_contentRt == null) return;

            ClearEntries();

            // Online path: render from the manual SAN list if it has anything.
            if (_manualMoveSans.Count > 0)
            {
                for (int i = 0; i < _manualMoveSans.Count; i += 2)
                {
                    int num = (i / 2) + 1;
                    string w = _manualMoveSans[i];
                    string b = (i + 1 < _manualMoveSans.Count) ? _manualMoveSans[i + 1] : "";
                    bool isLast = (i + 2 >= _manualMoveSans.Count);
                    AddMoveRow(num, w, b, (i / 2) % 2 == 0, isLast);
                }
                UpdateContentSize();
                if (_scrollRect != null) _scrollRect.verticalNormalizedPosition = 0f;
                return;
            }

            // Local / robot path: render from the ChessBoard's ExecutedMoves.
            if (_board == null)
            {
                AddLabelEntry("No board set");
                UpdateContentSize();
                return;
            }

            var moves = _board.ExecutedMoves;

            if (moves == null || moves.Count == 0)
            {
                AddLabelEntry("No moves yet");
                UpdateContentSize();
                return;
            }

            int totalRows = (moves.Count + 1) / 2;

            for (int i = 0; i < moves.Count; i += 2)
            {
                int num = (i / 2) + 1;
                string w = moves[i].San ?? moves[i].ToString();
                string b = (i + 1 < moves.Count) ? (moves[i + 1].San ?? moves[i + 1].ToString()) : "";
                bool isLast = (i + 2 >= moves.Count);
                AddMoveRow(num, w, b, (i / 2) % 2 == 0, isLast);
            }

            UpdateContentSize();

            if (_scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 0f;
        }

        private void AddMoveRow(int num, string white, string black, bool isEven, bool isLast)
        {
            var row = CreateUIGameObject($"Row_{num}", _contentRt);
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, 1f);
            rowRt.anchorMax = new Vector2(1f, 1f);
            rowRt.pivot = new Vector2(0.5f, 1f);
            rowRt.sizeDelta = new Vector2(-ContentPadding * 2, RowHeight);
            int idx = _entries.Count;
            rowRt.anchoredPosition = new Vector2(0f, -(ContentPadding + idx * (RowHeight + RowSpacing)));

            row.AddComponent<CanvasRenderer>();
            var rowImg = row.AddComponent<Image>();
            if (isLast)
                rowImg.color = highlightColor;
            else
                rowImg.color = isEven ? evenRowColor : oddRowColor;
            rowImg.raycastTarget = false;

            float x = 6f;
            AddTextCell(row.transform, $"{num}.", numColor, FontStyles.Normal, x, 30, 12);
            x += 30 + 4;
            AddTextCell(row.transform, white, moveColor, FontStyles.Bold, x, 90, 14);
            x += 90 + 4;
            AddTextCell(row.transform, black, moveColor, FontStyles.Bold, x, 90, 14);

            _entries.Add(row);
        }

        private void AddTextCell(Transform parent, string text, Color color, FontStyles style, float xPos, float width, float fontSize)
        {
            var cell = CreateUIGameObject("Cell", parent);
            var cellRt = cell.GetComponent<RectTransform>();
            cellRt.anchorMin = new Vector2(0f, 0.5f);
            cellRt.anchorMax = new Vector2(0f, 0.5f);
            cellRt.pivot = new Vector2(0f, 0.5f);
            cellRt.sizeDelta = new Vector2(width, RowHeight);
            cellRt.anchoredPosition = new Vector2(xPos, 0f);

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
            var objRt = obj.GetComponent<RectTransform>();
            objRt.anchorMin = new Vector2(0f, 1f);
            objRt.anchorMax = new Vector2(1f, 1f);
            objRt.pivot = new Vector2(0.5f, 1f);
            objRt.sizeDelta = new Vector2(-ContentPadding * 2, RowHeight);
            int idx = _entries.Count;
            objRt.anchoredPosition = new Vector2(0f, -(ContentPadding + idx * (RowHeight + RowSpacing)));

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
            foreach (var obj in _entries)
                if (obj != null) DestroyImmediate(obj);
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
