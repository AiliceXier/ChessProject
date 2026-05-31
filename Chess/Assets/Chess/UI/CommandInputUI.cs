using Chess;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.UI
{
    public class CommandInputUI : MonoBehaviour
    {
        public Player player;

        public Color panelColor = new Color(0.08f, 0.08f, 0.10f, 0.30f);
        public Color inputBgColor = new Color(0.06f, 0.06f, 0.08f, 0.60f);
        public Color btnColor = new Color(0.22f, 0.22f, 0.28f, 0.9f);
        public Color successColor = new Color(0.38f, 0.60f, 0.14f);
        public Color errorColor = new Color(0.80f, 0.20f, 0.20f);
        public Color infoColor = new Color(0.5f, 0.7f, 0.9f);

        public GameObject toggleBtnRef;
        public GameObject panelRef;
        public TMP_InputField inputFieldRef;

        private GameObject _panel;
        private GameObject _toggleBtn;
        private TMP_InputField _inputField;
        private TextMeshProUGUI _outputText;
        private ScrollRect _scrollRect;

        private bool _visible;

        private void Awake()
        {
            if (toggleBtnRef != null)
            {
                _toggleBtn = toggleBtnRef;
                var btn = _toggleBtn.GetComponent<Button>();
                if (btn == null)
                {
                    btn = _toggleBtn.AddComponent<Button>();
                    btn.onClick.AddListener(Toggle);
                }
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

            if (panelRef != null && inputFieldRef != null)
            {
                _panel = panelRef;
                _inputField = inputFieldRef;
                _inputField.onSubmit.AddListener(OnSubmit);

                var sendBtnTr = _panel.transform.Find("InputRow/SendBtn");
                if (sendBtnTr != null)
                {
                    var sendBtn = sendBtnTr.GetComponent<Button>();
                    if (sendBtn != null)
                        sendBtn.onClick.AddListener(() => OnSubmit(_inputField.text));
                }

                _panel.SetActive(false);
                return;
            }

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;
            BuildPanel(canvas.transform);
        }

        private void BuildPanel(Transform canvasTr)
        {
            _panel = new GameObject("CommandPanel");
            _panel.transform.SetParent(canvasTr, false);
            _panel.layer = 5;

            var panelRt = _panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.offsetMin = new Vector2(-160, -20);
            panelRt.offsetMax = new Vector2(160, 20);

            _panel.AddComponent<CanvasRenderer>();
            var bg = _panel.AddComponent<Image>();
            bg.color = panelColor;
            bg.raycastTarget = true;

            var inputRow = CreateUIObj("InputRow", _panel.transform);
            var rowRt = inputRow.GetComponent<RectTransform>();
            rowRt.anchorMin = Vector2.zero;
            rowRt.anchorMax = Vector2.one;
            rowRt.offsetMin = new Vector2(6, 2);
            rowRt.offsetMax = new Vector2(-6, -2);

            var inputObj = CreateUIObj("InputField", inputRow.transform);
            var inputRt = inputObj.GetComponent<RectTransform>();
            inputRt.anchorMin = new Vector2(0f, 0f);
            inputRt.anchorMax = new Vector2(1f, 1f);
            inputRt.offsetMin = new Vector2(0, 0);
            inputRt.offsetMax = new Vector2(-56, 0);
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
            textAreaRt.offsetMin = new Vector2(8, 2);
            textAreaRt.offsetMax = new Vector2(-8, -2);
            textArea.AddComponent<RectMask2D>();

            var inputTxtObj = CreateUIObj("Text", textArea.transform);
            var inputTxtRt = inputTxtObj.GetComponent<RectTransform>();
            inputTxtRt.anchorMin = Vector2.zero;
            inputTxtRt.anchorMax = Vector2.one;
            inputTxtRt.offsetMin = Vector2.zero;
            inputTxtRt.offsetMax = Vector2.zero;
            var inputTxt = inputTxtObj.AddComponent<TextMeshProUGUI>();
            inputTxt.fontSize = 16;
            inputTxt.color = Color.white;
            inputTxt.raycastTarget = false;

            var placeholderObj = CreateUIObj("Placeholder", textArea.transform);
            var placeholderRt = placeholderObj.GetComponent<RectTransform>();
            placeholderRt.anchorMin = Vector2.zero;
            placeholderRt.anchorMax = Vector2.one;
            placeholderRt.offsetMin = Vector2.zero;
            placeholderRt.offsetMax = Vector2.zero;
            var placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholder.fontSize = 16;
            placeholder.color = new Color(0.6f, 0.6f, 0.6f);
            placeholder.text = "Enter move or command...";
            placeholder.raycastTarget = false;

            _inputField.textComponent = inputTxt;
            _inputField.placeholder = placeholder;
            _inputField.onSubmit.AddListener(OnSubmit);

            var sendObj = CreateUIObj("SendBtn", inputRow.transform);
            var sendRt = sendObj.GetComponent<RectTransform>();
            sendRt.anchorMin = new Vector2(1f, 0f);
            sendRt.anchorMax = new Vector2(1f, 1f);
            sendRt.offsetMin = new Vector2(-50, 0);
            sendRt.offsetMax = new Vector2(0, 0);
            sendObj.AddComponent<CanvasRenderer>();
            var sendImg = sendObj.AddComponent<Image>();
            sendImg.color = new Color(0.2f, 0.5f, 0.8f, 0.80f);
            sendImg.raycastTarget = true;
            var sendBtn = sendObj.AddComponent<Button>();
            sendBtn.onClick.AddListener(() => OnSubmit(_inputField.text));
            var sendTxtObj = CreateUIObj("Text", sendObj.transform);
            var sendTxtRt = sendTxtObj.GetComponent<RectTransform>();
            sendTxtRt.anchorMin = Vector2.zero;
            sendTxtRt.anchorMax = Vector2.one;
            sendTxtRt.offsetMin = Vector2.zero;
            sendTxtRt.offsetMax = Vector2.zero;
            var sendTxt = sendTxtObj.AddComponent<TextMeshProUGUI>();
            sendTxt.text = "Run";
            sendTxt.fontSize = 14;
            sendTxt.fontStyle = FontStyles.Bold;
            sendTxt.alignment = TextAlignmentOptions.Center;
            sendTxt.color = Color.white;
            sendTxt.raycastTarget = false;

            _panel.SetActive(false);
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
            ProcessCommand(text.Trim());
            _inputField.text = "";
            _inputField.ActivateInputField();
        }

        private void ProcessCommand(string cmd)
        {
            if (player == null) return;
            var result = player.MakeCommandMove(cmd);
            if (!result.success)
            {
                Debug.Log($"[Command] Error: {result.error}");
            }
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
