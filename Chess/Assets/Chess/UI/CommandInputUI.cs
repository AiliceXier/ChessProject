using Chess;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.UI
{
    public class CommandInputUI : MonoBehaviour
    {
        public Player player;

        public Color panelColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        public Color headerColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        public Color inputBgColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        public Color btnColor = new Color(0.25f, 0.25f, 0.3f, 0.9f);
        public Color successColor = new Color(0.4f, 0.9f, 0.4f);
        public Color errorColor = new Color(0.9f, 0.3f, 0.3f);
        public Color infoColor = new Color(0.5f, 0.7f, 0.9f);

        private GameObject _panel;
        private GameObject _toggleBtn;
        private TMP_InputField _inputField;
        private TextMeshProUGUI _outputText;
        private ScrollRect _scrollRect;

        private bool _visible;

        private void Awake()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;
            BuildToggleButton(canvas.transform);
        }

        private void BuildToggleButton(Transform canvasTr)
        {
            _toggleBtn = new GameObject("CmdToggleBtn");
            _toggleBtn.transform.SetParent(canvasTr, false);
            _toggleBtn.layer = 5;

            var btnRt = _toggleBtn.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 0f);
            btnRt.anchorMax = new Vector2(1f, 0f);
            btnRt.offsetMin = new Vector2(-100, 50);
            btnRt.offsetMax = new Vector2(-10, 84);

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
            tmp.text = "Cmd";
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
            _panel = new GameObject("CommandPanel");
            _panel.transform.SetParent(canvasTr, false);
            _panel.layer = 5;
            _panel.SetActive(false);

            var panelRt = _panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 0f);
            panelRt.anchorMax = new Vector2(0.38f, 0.5f);
            panelRt.offsetMin = new Vector2(10, 50);
            panelRt.offsetMax = new Vector2(-10, -10);

            _panel.AddComponent<CanvasRenderer>();
            var bg = _panel.AddComponent<Image>();
            bg.color = panelColor;
            bg.raycastTarget = false;

            var vlg = _panel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 2;
            vlg.padding = new RectOffset(6, 6, 6, 6);

            BuildHeader(_panel.transform);
            BuildOutputArea(_panel.transform);
            BuildInputArea(_panel.transform);
        }

        private void BuildHeader(Transform parent)
        {
            var headerObj = CreateUIObj("Header", parent);
            var headerLe = headerObj.AddComponent<LayoutElement>();
            headerLe.preferredHeight = 32;
            headerLe.flexibleHeight = 0;
            headerObj.AddComponent<CanvasRenderer>();
            var headerImg = headerObj.AddComponent<Image>();
            headerImg.color = headerColor;
            headerImg.raycastTarget = false;

            var hlg = headerObj.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = false;
            hlg.spacing = 8;
            hlg.padding = new RectOffset(8, 4, 4, 4);

            var titleObj = CreateUIObj("Title", headerObj.transform);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "Command Input";
            titleTxt.fontSize = 16;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.color = Color.white;
            titleTxt.raycastTarget = false;
            var titleLe = titleObj.AddComponent<LayoutElement>();
            titleLe.flexibleWidth = 1;

            var closeObj = CreateUIObj("CloseBtn", headerObj.transform);
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
            var closeLe = closeObj.AddComponent<LayoutElement>();
            closeLe.minWidth = 24;
            closeLe.preferredWidth = 24;
        }

        private void BuildOutputArea(Transform parent)
        {
            var scrollObj = CreateUIObj("OutputScroll", parent);
            var scrollLe = scrollObj.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1;
            scrollLe.minHeight = 80;
            scrollObj.AddComponent<CanvasRenderer>();
            var vpImg = scrollObj.AddComponent<Image>();
            vpImg.color = Color.clear;
            vpImg.raycastTarget = true;
            scrollObj.AddComponent<Mask>().showMaskGraphic = false;
            _scrollRect = scrollObj.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Elastic;

            var contentObj = CreateUIObj("Content", scrollObj.transform);
            var contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 0;
            vlg.padding = new RectOffset(4, 4, 4, 4);

            _outputText = contentObj.AddComponent<TextMeshProUGUI>();
            _outputText.fontSize = 13;
            _outputText.color = new Color(0.8f, 0.8f, 0.8f);
            _outputText.alignment = TextAlignmentOptions.TopLeft;
            _outputText.raycastTarget = false;
            _outputText.text = "Type 'help' for commands.\n";

            _scrollRect.content = contentRt;
        }

        private void BuildInputArea(Transform parent)
        {
            var inputRow = CreateUIObj("InputRow", parent);
            var rowLe = inputRow.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 32;
            rowLe.flexibleHeight = 0;
            var hlg = inputRow.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = false;
            hlg.spacing = 4;

            var inputObj = CreateUIObj("InputField", inputRow.transform);
            var inputLe = inputObj.AddComponent<LayoutElement>();
            inputLe.flexibleWidth = 1;
            inputLe.minWidth = 100;
            inputObj.AddComponent<CanvasRenderer>();
            var inputBg = inputObj.AddComponent<Image>();
            inputBg.color = inputBgColor;
            inputBg.raycastTarget = true;
            _inputField = inputObj.AddComponent<TMP_InputField>();
            _inputField.targetGraphic = inputBg;

            var textArea = CreateUIObj("TextArea", inputObj.transform);
            var textAreaRt = textArea.GetComponent<RectTransform>();
            textAreaRt.anchorMin = Vector2.zero;
            textAreaRt.anchorMax = Vector2.one;
            textAreaRt.offsetMin = new Vector2(6, 2);
            textAreaRt.offsetMax = new Vector2(-6, -2);
            textArea.AddComponent<RectMask2D>();
            var inputTxt = textArea.AddComponent<TextMeshProUGUI>();
            inputTxt.fontSize = 14;
            inputTxt.color = Color.white;
            inputTxt.raycastTarget = false;
            var placeholder = textArea.AddComponent<TextMeshProUGUI>();
            placeholder.fontSize = 14;
            placeholder.color = new Color(0.4f, 0.4f, 0.4f);
            placeholder.text = "Enter move or command...";
            placeholder.raycastTarget = false;

            _inputField.textComponent = inputTxt;
            _inputField.placeholder = placeholder;
            _inputField.onSubmit.AddListener(OnSubmit);

            var sendObj = CreateUIObj("SendBtn", inputRow.transform);
            sendObj.AddComponent<CanvasRenderer>();
            var sendImg = sendObj.AddComponent<Image>();
            sendImg.color = new Color(0.2f, 0.5f, 0.8f);
            sendImg.raycastTarget = true;
            var sendBtn = sendObj.AddComponent<Button>();
            sendBtn.onClick.AddListener(() => OnSubmit(_inputField.text));
            var sendLe = sendObj.AddComponent<LayoutElement>();
            sendLe.minWidth = 50;
            sendLe.preferredWidth = 50;
            var sendTxt = sendObj.AddComponent<TextMeshProUGUI>();
            sendTxt.text = "Run";
            sendTxt.fontSize = 13;
            sendTxt.fontStyle = FontStyles.Bold;
            sendTxt.alignment = TextAlignmentOptions.Center;
            sendTxt.color = Color.white;
        }

        private GameObject CreateUIObj(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.layer = 5;
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private void OnSubmit(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            AppendOutput($"> {text}\n");
            ProcessCommand(text.Trim());
            _inputField.text = "";
            _inputField.ActivateInputField();
        }

        private void ProcessCommand(string cmd)
        {
            if (cmd.Equals("help", System.StringComparison.OrdinalIgnoreCase))
            {
                ShowHelp();
                return;
            }
            if (cmd.Equals("board", System.StringComparison.OrdinalIgnoreCase))
            {
                ShowAsciiBoard();
                return;
            }
            if (cmd.Equals("fen", System.StringComparison.OrdinalIgnoreCase))
            {
                ShowFen();
                return;
            }
            if (cmd.Equals("pgn", System.StringComparison.OrdinalIgnoreCase))
            {
                ShowPgn();
                return;
            }
            if (cmd.Equals("undo", System.StringComparison.OrdinalIgnoreCase))
            {
                UndoMove();
                return;
            }
            if (cmd.Equals("clear", System.StringComparison.OrdinalIgnoreCase))
            {
                _outputText.text = "";
                return;
            }
            if (cmd.StartsWith("load fen ", System.StringComparison.OrdinalIgnoreCase))
            {
                LoadFen(cmd.Substring(9));
                return;
            }
            if (cmd.StartsWith("load pgn ", System.StringComparison.OrdinalIgnoreCase))
            {
                LoadPgn(cmd.Substring(9));
                return;
            }

            if (player == null)
            {
                AppendOutputColored("Error: Not connected to game controller\n", errorColor);
                return;
            }
            var result = player.MakeCommandMove(cmd);
            if (!result.success)
            {
                AppendOutputColored($"  Error: {result.error}\n", errorColor);
            }
        }

        private void ShowHelp()
        {
            AppendOutput("-- Help --\n");
            AppendOutput("Move formats:\n");
            AppendOutput("  Coordinate: e2e4 / e7e8q (promotion)\n");
            AppendOutput("  SAN: e4 / Nf3 / O-O / O-O-O\n");
            AppendOutput("Commands:\n");
            AppendOutput("  help       - Show help\n");
            AppendOutput("  board      - Show ASCII board\n");
            AppendOutput("  fen        - Show current FEN\n");
            AppendOutput("  pgn        - Show PGN record\n");
            AppendOutput("  undo       - Undo last move\n");
            AppendOutput("  load fen <FEN> - Restore board from FEN\n");
            AppendOutput("  load pgn <PGN> - Restore board from PGN\n");
            AppendOutput("  clear      - Clear screen\n");
        }

        private void ShowAsciiBoard()
        {
            if (player == null)
            {
                AppendOutputColored("Error: Not connected to game controller\n", errorColor);
                return;
            }
            var board = player.GetLocalBoard();
            if (board == null)
            {
                AppendOutputColored("No active game\n", errorColor);
                return;
            }
            AppendOutput(board.ToString() + "\n");
        }

        private void ShowFen()
        {
            if (player == null)
            {
                AppendOutputColored("Error: Not connected to game controller\n", errorColor);
                return;
            }
            var board = player.GetLocalBoard();
            if (board == null)
            {
                AppendOutputColored("No active game\n", errorColor);
                return;
            }
            AppendOutput($"FEN: {board.ToFen()}\n");
        }

        private void ShowPgn()
        {
            if (player == null)
            {
                AppendOutputColored("Error: Not connected to game controller\n", errorColor);
                return;
            }
            var board = player.GetLocalBoard();
            if (board == null)
            {
                AppendOutputColored("No active game\n", errorColor);
                return;
            }
            AppendOutput($"PGN:\n{board.ToPgn()}\n");
        }

        private void UndoMove()
        {
            if (player == null)
            {
                AppendOutputColored("Error: Not connected to game controller\n", errorColor);
                return;
            }
            var result = player.UndoLastLocalMove();
            if (!result.success)
            {
                AppendOutputColored("No moves to undo\n", errorColor);
                return;
            }
            AppendOutputColored("Undo successful\n", successColor);
        }

        private void LoadFen(string fen)
        {
            if (player == null)
            {
                AppendOutputColored("Error: Not connected to game controller\n", errorColor);
                return;
            }
            var result = player.LoadFromFen(fen);
            if (result.success)
            {
                AppendOutputColored("Board restored from FEN\n", successColor);
            }
            else
            {
                AppendOutputColored($"FEN restore failed: {result.error}\n", errorColor);
            }
        }

        private void LoadPgn(string pgn)
        {
            if (player == null)
            {
                AppendOutputColored("Error: Not connected to game controller\n", errorColor);
                return;
            }
            var result = player.LoadFromPgn(pgn);
            if (result.success)
            {
                AppendOutputColored("Board restored from PGN\n", successColor);
            }
            else
            {
                AppendOutputColored($"PGN restore failed: {result.error}\n", errorColor);
            }
        }

        private void AppendOutput(string text)
        {
            if (_outputText != null)
                _outputText.text += text;
            ScrollToBottom();
        }

        private void AppendOutputColored(string text, Color color)
        {
            AppendOutput($"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>");
        }

        private void ScrollToBottom()
        {
            if (_scrollRect != null)
                Canvas.ForceUpdateCanvases();
            if (_scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 0f;
        }

        public void Show()
        {
            EnsurePanel();
            if (_panel != null) _panel.SetActive(true);
            _visible = true;
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
