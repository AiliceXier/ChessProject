using System;
using Chess;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.UI
{
    public class CommandInputUI : MonoBehaviour
    {
        public Player player;

        public Color panelColor = new Color(0.08f, 0.08f, 0.08f, 0.95f);
        public Color headerColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        public Color inputBgColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        public Color btnColor = new Color(0.2f, 0.35f, 0.2f, 1f);
        public Color errorColor = new Color(0.9f, 0.3f, 0.3f);
        public Color successColor = new Color(0.3f, 0.9f, 0.3f);

        private GameObject _panel;
        private TMP_InputField _inputField;
        private TMP_Text _outputText;
        private Button _toggleButton;
        private bool _visible;

        private void Awake()
        {
            BuildAllUI();
        }

        private void Start()
        {
            Hide();
        }

        private void BuildAllUI()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            BuildToggleButton(canvas.transform);
            BuildPanel(canvas.transform);
        }

        private void BuildToggleButton(Transform canvasTr)
        {
            var btnObj = new GameObject("CmdToggleBtn");
            btnObj.transform.SetParent(canvasTr, false);
            btnObj.layer = 5;

            var btnRt = btnObj.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 0.5f);
            btnRt.anchorMax = new Vector2(1f, 0.5f);
            btnRt.offsetMin = new Vector2(-100, -50);
            btnRt.offsetMax = new Vector2(-10, -10);

            btnObj.AddComponent<CanvasRenderer>();
            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);
            btnImg.raycastTarget = true;

            _toggleButton = btnObj.AddComponent<Button>();
            _toggleButton.onClick.AddListener(Toggle);

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            txtObj.layer = 5;
            txtObj.AddComponent<RectTransform>();
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "Cmd";
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private void BuildPanel(Transform canvasTr)
        {
            _panel = new GameObject("CommandPanel");
            _panel.transform.SetParent(canvasTr, false);
            _panel.layer = 5;

            var panelRt = _panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 0f);
            panelRt.anchorMax = new Vector2(0.4f, 0.55f);
            panelRt.offsetMin = new Vector2(10, 10);
            panelRt.offsetMax = new Vector2(-10, -10);

            _panel.AddComponent<CanvasRenderer>();
            var bg = _panel.AddComponent<Image>();
            bg.color = panelColor;
            bg.raycastTarget = true;

            var titleObj = CreateUIObj("Title", _panel.transform);
            var titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(0, -32);
            titleRt.offsetMax = Vector2.zero;
            titleObj.AddComponent<CanvasRenderer>();
            var titleImg = titleObj.AddComponent<Image>();
            titleImg.color = headerColor;
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "Command Input";
            titleTxt.fontSize = 16;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = Color.white;

            var closeObj = CreateUIObj("CloseBtn", titleObj.transform);
            var closeRt = closeObj.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.offsetMin = new Vector2(-28, -12);
            closeRt.offsetMax = new Vector2(-4, 12);
            closeObj.AddComponent<CanvasRenderer>();
            var closeImg = closeObj.AddComponent<Image>();
            closeImg.color = new Color(0.8f, 0.25f, 0.25f);
            var closeBtn = closeObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Hide);
            var closeTxt = closeObj.AddComponent<TextMeshProUGUI>();
            closeTxt.text = "X";
            closeTxt.fontSize = 13;
            closeTxt.alignment = TextAlignmentOptions.Center;
            closeTxt.color = Color.white;

            var outputObj = CreateUIObj("Output", _panel.transform);
            var outputRt = outputObj.GetComponent<RectTransform>();
            outputRt.anchorMin = Vector2.zero;
            outputRt.anchorMax = Vector2.one;
            outputRt.offsetMin = new Vector2(6, 44);
            outputRt.offsetMax = new Vector2(-6, -38);
            outputObj.AddComponent<CanvasRenderer>();
            var outputBg = outputObj.AddComponent<Image>();
            outputBg.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
            var scrollRect = outputObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            outputObj.AddComponent<Mask>().showMaskGraphic = false;

            var contentObj = CreateUIObj("Content", outputObj.transform);
            var contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _outputText = contentObj.AddComponent<TextMeshProUGUI>();
            _outputText.fontSize = 13;
            _outputText.fontStyle = FontStyles.Normal;
            _outputText.color = new Color(0.7f, 0.9f, 0.7f);
            _outputText.alignment = TextAlignmentOptions.TopLeft;
            _outputText.enableWordWrapping = true;

            scrollRect.content = contentRt;
            scrollRect.viewport = outputRt;

            var inputRowObj = CreateUIObj("InputRow", _panel.transform);
            var inputRowRt = inputRowObj.GetComponent<RectTransform>();
            inputRowRt.anchorMin = new Vector2(0f, 0f);
            inputRowRt.anchorMax = new Vector2(1f, 0f);
            inputRowRt.offsetMin = new Vector2(6, 6);
            inputRowRt.offsetMax = new Vector2(-6, 38);
            inputRowObj.AddComponent<CanvasRenderer>();
            var inputRowBg = inputRowObj.AddComponent<Image>();
            inputRowBg.color = inputBgColor;

            var hlg = inputRowObj.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.spacing = 4;
            hlg.padding = new RectOffset(4, 4, 4, 4);

            var inputAreaObj = CreateUIObj("InputArea", inputRowObj.transform);
            var inputAreaRt = inputAreaObj.GetComponent<RectTransform>();
            inputAreaObj.AddComponent<CanvasRenderer>();
            var inputAreaBg = inputAreaObj.AddComponent<Image>();
            inputAreaBg.color = new Color(0.1f, 0.1f, 0.1f);
            var inputAreaRectMask = inputAreaObj.AddComponent<RectMask2D>();

            var textAreaObj = CreateUIObj("TextArea", inputAreaObj.transform);
            var textAreaRt = textAreaObj.GetComponent<RectTransform>();
            textAreaRt.anchorMin = Vector2.zero;
            textAreaRt.anchorMax = Vector2.one;
            textAreaRt.offsetMin = new Vector2(4, 4);
            textAreaRt.offsetMax = new Vector2(-4, -4);

            _inputField = inputAreaObj.AddComponent<TMP_InputField>();
            _inputField.textViewport = textAreaRt;
            _inputField.textComponent = textAreaObj.AddComponent<TextMeshProUGUI>();
            _inputField.textComponent.fontSize = 14;
            _inputField.textComponent.color = Color.white;
            _inputField.textComponent.alignment = TextAlignmentOptions.MidlineLeft;

            var placeholderObj = CreateUIObj("Placeholder", textAreaObj.transform);
            var placeholderRt = placeholderObj.GetComponent<RectTransform>();
            placeholderRt.anchorMin = Vector2.zero;
            placeholderRt.anchorMax = Vector2.one;
            placeholderRt.offsetMin = new Vector2(4, 4);
            placeholderRt.offsetMax = new Vector2(-4, -4);
            var placeholderTmp = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholderTmp.fontSize = 14;
            placeholderTmp.text = "Enter move (e.g. e2e4 or Nf3)...";
            placeholderTmp.color = new Color(0.4f, 0.4f, 0.4f);
            placeholderTmp.alignment = TextAlignmentOptions.MidlineLeft;
            _inputField.placeholder = placeholderTmp;

            var inputLe = inputAreaObj.AddComponent<LayoutElement>();
            inputLe.preferredWidth = 999;
            inputLe.flexibleWidth = 1;

            _inputField.onEndEdit.AddListener(OnCommandSubmitted);

            var sendBtnObj = CreateUIObj("SendBtn", inputRowObj.transform);
            sendBtnObj.AddComponent<CanvasRenderer>();
            var sendBtnImg = sendBtnObj.AddComponent<Image>();
            sendBtnImg.color = btnColor;
            var sendBtn = sendBtnObj.AddComponent<Button>();
            sendBtn.onClick.AddListener(OnSendClicked);
            var sendBtnLe = sendBtnObj.AddComponent<LayoutElement>();
            sendBtnLe.minWidth = 50;
            sendBtnLe.preferredWidth = 50;
            var sendTxt = sendBtnObj.AddComponent<TextMeshProUGUI>();
            sendTxt.text = "Go";
            sendTxt.fontSize = 13;
            sendTxt.fontStyle = FontStyles.Bold;
            sendTxt.alignment = TextAlignmentOptions.Center;
            sendTxt.color = Color.white;

            AppendOutput("Welcome to command mode!\nEnter moves like: e2e4 / Nf3 / O-O\nType 'help' for commands\n");
        }

        private GameObject CreateUIObj(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.layer = 5;
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private void OnSendClicked()
        {
            if (_inputField != null && !string.IsNullOrEmpty(_inputField.text))
            {
                ProcessCommand(_inputField.text.Trim());
                _inputField.text = "";
                _inputField.ActivateInputField();
            }
        }

        private void OnCommandSubmitted(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                ProcessCommand(text.Trim());
                _inputField.text = "";
            }
        }

        private void ProcessCommand(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd)) return;

            cmd = cmd.Trim();

            if (cmd.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                ShowHelp();
                return;
            }

            if (cmd.Equals("clear", StringComparison.OrdinalIgnoreCase))
            {
                _outputText.text = "";
                return;
            }

            if (cmd.Equals("board", StringComparison.OrdinalIgnoreCase))
            {
                ShowAsciiBoard();
                return;
            }

            if (cmd.Equals("fen", StringComparison.OrdinalIgnoreCase))
            {
                ShowFen();
                return;
            }

            if (cmd.Equals("pgn", StringComparison.OrdinalIgnoreCase))
            {
                ShowPgn();
                return;
            }

            if (cmd.Equals("undo", StringComparison.OrdinalIgnoreCase))
            {
                UndoMove();
                return;
            }

            if (cmd.StartsWith("load fen ", StringComparison.OrdinalIgnoreCase))
            {
                LoadFen(cmd.Substring(9).Trim());
                return;
            }

            if (cmd.StartsWith("load pgn ", StringComparison.OrdinalIgnoreCase))
            {
                LoadPgn(cmd.Substring(9).Trim());
                return;
            }

            TryMakeMove(cmd);
        }

        private void TryMakeMove(string cmd)
        {
            if (player == null)
            {
                AppendOutputColored("Error: Not connected to game controller\n", errorColor);
                return;
            }

            var result = player.MakeCommandMove(cmd);
            if (result.success)
            {
                AppendOutputColored($"> {cmd}\n", new Color(0.5f, 0.8f, 0.5f));
                AppendOutputColored($"  Move executed successfully\n", successColor);
                ShowAsciiBoard();
            }
            else
            {
                AppendOutputColored($"> {cmd}\n", new Color(0.8f, 0.5f, 0.5f));
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
            var board = player?.GetLocalBoard();
            if (board == null)
            {
                AppendOutputColored("No active game\n", errorColor);
                return;
            }
            AppendOutput(board.ToAscii(true) + "\n");
        }

        private void ShowFen()
        {
            var board = player?.GetLocalBoard();
            if (board == null)
            {
                AppendOutputColored("No active game\n", errorColor);
                return;
            }
            AppendOutput($"FEN: {board.ToFen()}\n");
        }

        private void ShowPgn()
        {
            var board = player?.GetLocalBoard();
            if (board == null)
            {
                AppendOutputColored("No active game\n", errorColor);
                return;
            }
            AppendOutput($"PGN:\n{board.ToPgn()}\n");
        }

        private void UndoMove()
        {
            var board = player?.GetLocalBoard();
            if (board == null || board.ExecutedMoves.Count == 0)
            {
                AppendOutputColored("No moves to undo\n", errorColor);
                return;
            }

            player.UndoLastLocalMove();
            AppendOutputColored("Undo successful\n", successColor);
            ShowAsciiBoard();
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
                AppendOutputColored($"Board restored from FEN\n", successColor);
                ShowAsciiBoard();
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
                AppendOutputColored($"Board restored from PGN\n", successColor);
                ShowAsciiBoard();
            }
            else
            {
                AppendOutputColored($"PGN restore failed: {result.error}\n", errorColor);
            }
        }

        private void AppendOutput(string text)
        {
            if (_outputText != null)
            {
                _outputText.text += text;
                Canvas.ForceUpdateCanvases();
            }
        }

        private void AppendOutputColored(string text, Color color)
        {
            var hex = ColorUtility.ToHtmlStringRGBA(color);
            if (_outputText != null)
            {
                _outputText.text += $"<color=#{hex}>{text}</color>";
                Canvas.ForceUpdateCanvases();
            }
        }

        public void Show()
        {
            if (_panel != null) _panel.SetActive(true);
            _visible = true;
            if (_inputField != null) _inputField.ActivateInputField();
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
    }
}
