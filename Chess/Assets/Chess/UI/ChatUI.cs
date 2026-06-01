using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.UI
{
    public class ChatUI : MonoBehaviour
    {
        public Player player;

        public Color panelColor = new Color(0.08f, 0.08f, 0.10f, 0.95f);
        public Color headerColor = new Color(0.14f, 0.14f, 0.16f, 1f);
        public Color inputBgColor = new Color(0.06f, 0.06f, 0.08f, 1f);
        public Color btnColor = new Color(0.22f, 0.22f, 0.28f, 0.9f);
        public Color selfMsgColor = new Color(0.29f, 0.48f, 0.71f, 0.6f);
        public Color otherMsgColor = new Color(0.22f, 0.22f, 0.24f, 0.6f);

        public static readonly string ChatServerUrl = "ws://121.36.101.82:3001";

        [Header("Scene References")]
        public GameObject toggleBtnRef;
        public GameObject panelRef;
        public TMP_InputField inputFieldRef;
        public ScrollRect scrollRectRef;
        public Transform contentParentRef;

        private GameObject _panel;
        private GameObject _toggleBtn;
        private Transform _contentParent;
        private TMP_InputField _inputField;
        private ScrollRect _scrollRect;

        private ChatWebSocketClient _wsClient;
        private string _localPlayerName;
        private bool _visible;

        private void Awake()
        {
            FindSceneReferences();

            if (toggleBtnRef != null)
            {
                _toggleBtn = toggleBtnRef;
                var btn = _toggleBtn.GetComponent<Button>();
                if (btn == null)
                {
                    btn = _toggleBtn.AddComponent<Button>();
                }
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(Toggle);
            }
            else
            {
                var canvas = FindObjectOfType<Canvas>();
                if (canvas == null) return;
                BuildToggleButton(canvas.transform);
            }

            if (panelRef != null)
            {
                panelRef.SetActive(false);
                Debug.Log("[ChatUI] Awake: panelRef found and hidden");
            }
            else
            {
                Debug.LogWarning("[ChatUI] Awake: panelRef NOT found!");
            }
        }

        private void FindSceneReferences()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[ChatUI] FindSceneReferences: Canvas not found!");
                return;
            }

            if (toggleBtnRef == null)
            {
                var foundToggle = canvas.transform.Find("ChatToggleBtn");
                if (foundToggle != null) toggleBtnRef = foundToggle.gameObject;
            }

            if (panelRef == null)
            {
                var foundPanel = canvas.transform.Find("ChatPanel");
                if (foundPanel != null) panelRef = foundPanel.gameObject;
            }

            if (panelRef == null)
            {
                foreach (Transform child in canvas.transform)
                {
                    if (child.name == "ChatPanel")
                    {
                        panelRef = child.gameObject;
                        break;
                    }
                }
            }

            if (scrollRectRef == null && panelRef != null)
            {
                var foundScroll = panelRef.transform.Find("ChatScrollView");
                if (foundScroll != null) scrollRectRef = foundScroll.GetComponent<ScrollRect>();
            }

            if (contentParentRef == null && panelRef != null)
            {
                var foundContent = panelRef.transform.Find("ChatScrollView/ChatViewport/ChatContent");
                if (foundContent != null) contentParentRef = foundContent;
            }

            if (inputFieldRef == null && panelRef != null)
            {
                var foundInput = panelRef.transform.Find("ChatInputRow/ChatInputField");
                if (foundInput != null) inputFieldRef = foundInput.GetComponent<TMP_InputField>();
            }

            Debug.Log($"[ChatUI] FindSceneReferences: panelRef={(panelRef != null ? panelRef.name : "NULL")}, toggleBtnRef={(toggleBtnRef != null ? toggleBtnRef.name : "NULL")}, inputFieldRef={(inputFieldRef != null ? "found" : "NULL")}, scrollRectRef={(scrollRectRef != null ? "found" : "NULL")}, contentParentRef={(contentParentRef != null ? "found" : "NULL")}");
        }

        private void OnDestroy()
        {
            _wsClient?.Dispose();
        }

        public void ConnectToRoom(string roomId, string playerName)
        {
            Debug.Log($"[ChatUI] ConnectToRoom: roomId={roomId}, playerName={playerName}, url={ChatServerUrl}");
            _localPlayerName = playerName;
            _wsClient?.Dispose();
            _wsClient = new ChatWebSocketClient(ChatServerUrl);
            _wsClient.OnMessageReceived += OnWebSocketMessage;
            _wsClient.OnError += OnWebSocketError;
            _wsClient.OnConnected += () =>
            {
                Debug.Log("[ChatUI] WebSocket connected successfully");
                AddMessage("System", "Connected to chat", false);
            };
            _wsClient.Connect(roomId, playerName);
        }

        public void Disconnect()
        {
            _wsClient?.Dispose();
            _wsClient = null;
        }

        private void OnWebSocketMessage(string sender, string message)
        {
            Debug.Log($"[ChatUI] OnWebSocketMessage: sender={sender}, message={message}");
            if (sender == "System")
            {
                AddMessage("System", message, false);
            }
            else
            {
                bool isSelf = sender == _localPlayerName;
                if (!isSelf)
                    AddMessage(sender, message, false);
            }
        }

        private void OnWebSocketError(string error)
        {
            AddMessage("System", $"Connection error", false);
        }

        private void BuildToggleButton(Transform canvasTr)
        {
            _toggleBtn = new GameObject("ChatToggleBtn");
            _toggleBtn.transform.SetParent(canvasTr, false);
            _toggleBtn.layer = 5;

            var btnRt = _toggleBtn.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0f, 0f);
            btnRt.anchorMax = new Vector2(0f, 0f);
            btnRt.offsetMin = new Vector2(120, 10);
            btnRt.offsetMax = new Vector2(220, 44);

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
            tmp.text = "Chat";
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private void EnsurePanel()
        {
            if (_panel != null) return;

            FindSceneReferences();

            if (panelRef != null)
            {
                _panel = panelRef;
                Debug.Log("[ChatUI] EnsurePanel: using panelRef from scene");

                if (inputFieldRef != null)
                {
                    _inputField = inputFieldRef;

                    var textAreaTr = _inputField.transform.Find("ChatTextArea");
                    if (textAreaTr != null)
                    {
                        _inputField.textViewport = textAreaTr.GetComponent<RectTransform>();

                        var textTr = textAreaTr.Find("ChatInputText");
                        if (textTr != null)
                            _inputField.textComponent = textTr.GetComponent<TMPro.TextMeshProUGUI>();

                        var placeholderTr = textAreaTr.Find("ChatPlaceholder");
                        if (placeholderTr != null)
                            _inputField.placeholder = placeholderTr.GetComponent<TMPro.TextMeshProUGUI>();
                    }

                    _inputField.onSubmit.RemoveAllListeners();
                    _inputField.onSubmit.AddListener(OnSend);
                }

                if (scrollRectRef != null)
                {
                    _scrollRect = scrollRectRef;
                    if (_scrollRect.content == null && contentParentRef != null)
                        _scrollRect.content = contentParentRef.GetComponent<RectTransform>();
                    if (_scrollRect.viewport == null)
                    {
                        var viewportTr = _panel.transform.Find("ChatScrollView/ChatViewport");
                        if (viewportTr != null)
                            _scrollRect.viewport = viewportTr.GetComponent<RectTransform>();
                    }
                }

                if (contentParentRef != null)
                    _contentParent = contentParentRef;
                else
                {
                    var contentTr = _panel.transform.Find("ChatScrollView/ChatViewport/ChatContent");
                    if (contentTr != null)
                        _contentParent = contentTr;
                }

                var closeBtnTr = _panel.transform.Find("ChatHeader/ChatCloseBtn");
                if (closeBtnTr != null)
                {
                    var closeBtn = closeBtnTr.GetComponent<Button>();
                    if (closeBtn != null)
                    {
                        closeBtn.onClick.RemoveAllListeners();
                        closeBtn.onClick.AddListener(Hide);
                    }
                }

                var sendBtnTr = _panel.transform.Find("ChatInputRow/ChatSendBtn");
                if (sendBtnTr != null)
                {
                    var sendBtn = sendBtnTr.GetComponent<Button>();
                    if (sendBtn != null)
                    {
                        sendBtn.onClick.RemoveAllListeners();
                        sendBtn.onClick.AddListener(() => OnSend(_inputField.text));
                    }
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
            _panel = new GameObject("ChatPanel");
            _panel.transform.SetParent(canvasTr, false);
            _panel.layer = 5;

            var panelRt = _panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 0f);
            panelRt.anchorMax = new Vector2(0.38f, 0.5f);
            panelRt.offsetMin = new Vector2(10, 50);
            panelRt.offsetMax = new Vector2(-10, -10);

            _panel.AddComponent<CanvasRenderer>();
            var bg = _panel.AddComponent<Image>();
            bg.color = panelColor;
            bg.raycastTarget = true;

            var vlg = _panel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 2;
            vlg.padding = new RectOffset(6, 6, 6, 6);

            BuildHeader(_panel.transform);
            BuildMessageArea(_panel.transform);
            BuildInputArea(_panel.transform);

            _panel.SetActive(false);
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
            titleTxt.text = "Chat";
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
            var closeTxtObj = CreateUIObj("Text", closeObj.transform);
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
            closeLe.minWidth = 24;
            closeLe.preferredWidth = 24;
        }

        private void BuildMessageArea(Transform parent)
        {
            var scrollObj = CreateUIObj("MsgScroll", parent);
            var scrollLe = scrollObj.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1;
            scrollLe.minHeight = 60;
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
            vlg.childAlignment = TextAnchor.LowerCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 4;
            _contentParent = contentObj.transform;

            _scrollRect.content = contentRt;
            _scrollRect.viewport = scrollObj.GetComponent<RectTransform>();
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
            inputLe.minWidth = 80;
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

            var inputTxtObj = CreateUIObj("Text", textArea.transform);
            var inputTxtRt = inputTxtObj.GetComponent<RectTransform>();
            inputTxtRt.anchorMin = Vector2.zero;
            inputTxtRt.anchorMax = Vector2.one;
            inputTxtRt.offsetMin = Vector2.zero;
            inputTxtRt.offsetMax = Vector2.zero;
            var inputTxt = inputTxtObj.AddComponent<TextMeshProUGUI>();
            inputTxt.fontSize = 14;
            inputTxt.color = Color.white;
            inputTxt.raycastTarget = false;

            var placeholderObj = CreateUIObj("Placeholder", textArea.transform);
            var placeholderRt = placeholderObj.GetComponent<RectTransform>();
            placeholderRt.anchorMin = Vector2.zero;
            placeholderRt.anchorMax = Vector2.one;
            placeholderRt.offsetMin = Vector2.zero;
            placeholderRt.offsetMax = Vector2.zero;
            var placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholder.fontSize = 14;
            placeholder.color = new Color(0.4f, 0.4f, 0.4f);
            placeholder.text = "Type a message...";
            placeholder.raycastTarget = false;

            _inputField.textComponent = inputTxt;
            _inputField.placeholder = placeholder;
            _inputField.textViewport = textAreaRt;
            _inputField.onSubmit.AddListener(OnSend);

            var sendObj = CreateUIObj("SendBtn", inputRow.transform);
            sendObj.AddComponent<CanvasRenderer>();
            var sendImg = sendObj.AddComponent<Image>();
            sendImg.color = new Color(0.2f, 0.5f, 0.8f);
            sendImg.raycastTarget = true;
            var sendBtn = sendObj.AddComponent<Button>();
            sendBtn.onClick.AddListener(() => OnSend(_inputField.text));
            var sendLe = sendObj.AddComponent<LayoutElement>();
            sendLe.minWidth = 50;
            sendLe.preferredWidth = 50;
            var sendTxtObj = CreateUIObj("Text", sendObj.transform);
            var sendTxtRt = sendTxtObj.GetComponent<RectTransform>();
            sendTxtRt.anchorMin = Vector2.zero;
            sendTxtRt.anchorMax = Vector2.one;
            sendTxtRt.offsetMin = Vector2.zero;
            sendTxtRt.offsetMax = Vector2.zero;
            var sendTxt = sendTxtObj.AddComponent<TextMeshProUGUI>();
            sendTxt.text = "Send";
            sendTxt.fontSize = 13;
            sendTxt.fontStyle = FontStyles.Bold;
            sendTxt.alignment = TextAlignmentOptions.Center;
            sendTxt.color = Color.white;
            sendTxt.raycastTarget = false;
        }

        private GameObject CreateUIObj(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.layer = 5;
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private void OnSend(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            Debug.Log($"[ChatUI] OnSend: wsClient={_wsClient != null}, isConnected={_wsClient?.IsConnected}, text={text}");

            EnsurePanel();

            if (_wsClient != null && _wsClient.IsConnected)
            {
                _wsClient.SendChatMessage(text);
                AddMessage(_localPlayerName ?? "You", text, true);
            }
            else
            {
                Debug.LogWarning("[ChatUI] WebSocket not connected, message not sent");
                AddMessage("You", text, true);
                AddMessage("System", "Not connected to chat server", false);
            }

            if (_inputField != null)
            {
                _inputField.text = "";
                _inputField.ActivateInputField();
            }
        }

        public void ReceiveMessage(string sender, string message)
        {
            AddMessage(sender, message, false);
        }

        private void AddMessage(string sender, string message, bool isSelf)
        {
            if (_contentParent == null)
            {
                Debug.LogWarning($"[ChatUI] AddMessage skipped: _contentParent is null. sender={sender}, msg={message}");
                return;
            }

            var msgObj = CreateUIObj("Msg", _contentParent);
            var msgLe = msgObj.AddComponent<LayoutElement>();
            msgLe.minHeight = 28;
            msgLe.flexibleHeight = -1;
            msgObj.AddComponent<CanvasRenderer>();
            var msgBg = msgObj.AddComponent<Image>();
            msgBg.color = isSelf ? selfMsgColor : otherMsgColor;
            msgBg.raycastTarget = false;

            var msgTxtObj = CreateUIObj("Text", msgObj.transform);
            var msgTxtRt = msgTxtObj.GetComponent<RectTransform>();
            msgTxtRt.anchorMin = Vector2.zero;
            msgTxtRt.anchorMax = Vector2.one;
            msgTxtRt.offsetMin = Vector2.zero;
            msgTxtRt.offsetMax = Vector2.zero;
            var msgTxt = msgTxtObj.AddComponent<TextMeshProUGUI>();
            msgTxt.text = $"<b>{sender}:</b> {message}";
            msgTxt.fontSize = 13;
            msgTxt.color = Color.white;
            msgTxt.raycastTarget = false;

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
