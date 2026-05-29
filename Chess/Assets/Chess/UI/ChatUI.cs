using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.UI
{
    public class ChatUI : MonoBehaviour
    {
        public Player player;

        public Color panelColor = new Color(0.1f, 0.1f, 0.1f, 0.92f);
        public Color headerColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        public Color myMsgColor = new Color(0.15f, 0.25f, 0.15f, 0.9f);
        public Color otherMsgColor = new Color(0.2f, 0.15f, 0.15f, 0.9f);
        public Color systemColor = new Color(0.4f, 0.4f, 0.4f);

        private GameObject _panel;
        private Transform _contentParent;
        private ScrollRect _scrollRect;
        private TMP_InputField _inputField;
        private readonly List<ChatMessage> _messages = new();
        private bool _visible;

        public struct ChatMessage
        {
            public string sender;
            public string text;
            public bool isMe;
            public DateTime time;
        }

        private void Awake()
        {
            BuildUI();
        }

        private void Start()
        {
            Hide();
        }

        private void BuildUI()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            BuildToggleButton(canvas.transform);
            BuildPanel(canvas.transform);
        }

        private void BuildToggleButton(Transform canvasTr)
        {
            var btnObj = new GameObject("ChatToggleBtn");
            btnObj.transform.SetParent(canvasTr, false);
            btnObj.layer = 5;

            var btnRt = btnObj.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0f, 0f);
            btnRt.anchorMax = new Vector2(0f, 0f);
            btnRt.offsetMin = new Vector2(10, 50);
            btnRt.offsetMax = new Vector2(110, 84);

            btnObj.AddComponent<CanvasRenderer>();
            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.25f, 0.2f, 0.3f, 0.9f);
            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(Toggle);

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            txtObj.layer = 5;
            txtObj.AddComponent<RectTransform>();
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "Chat";
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private void BuildPanel(Transform canvasTr)
        {
            _panel = new GameObject("ChatPanel");
            _panel.transform.SetParent(canvasTr, false);
            _panel.layer = 5;

            var panelRt = _panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 0f);
            panelRt.anchorMax = new Vector2(0.35f, 0.5f);
            panelRt.offsetMin = new Vector2(10, 10);
            panelRt.offsetMax = new Vector2(-10, -10);

            _panel.AddComponent<CanvasRenderer>();
            var bg = _panel.AddComponent<Image>();
            bg.color = panelColor;

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
            titleTxt.text = "Chat";
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

            var scrollObj = CreateUIObj("ScrollView", _panel.transform);
            var scrollRt = scrollObj.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(4, 44);
            scrollRt.offsetMax = new Vector2(-4, -36);
            scrollObj.AddComponent<CanvasRenderer>();
            var vpImg = scrollObj.AddComponent<Image>();
            vpImg.color = Color.clear;
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
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.LowerCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 3;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            _contentParent = contentObj.transform;

            _scrollRect.content = contentRt;
            _scrollRect.viewport = panelRt;

            var inputRowObj = CreateUIObj("InputRow", _panel.transform);
            var inputRowRt = inputRowObj.GetComponent<RectTransform>();
            inputRowRt.anchorMin = new Vector2(0f, 0f);
            inputRowRt.anchorMax = new Vector2(1f, 0f);
            inputRowRt.offsetMin = new Vector2(4, 4);
            inputRowRt.offsetMax = new Vector2(-4, 38);
            inputRowObj.AddComponent<CanvasRenderer>();
            var inputRowBg = inputRowObj.AddComponent<Image>();
            inputRowBg.color = new Color(0.15f, 0.15f, 0.15f);

            var hlg = inputRowObj.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.spacing = 4;
            hlg.padding = new RectOffset(4, 4, 4, 4);

            var inputAreaObj = CreateUIObj("InputArea", inputRowObj.transform);
            inputAreaObj.AddComponent<CanvasRenderer>();
            var inputAreaBg = inputAreaObj.AddComponent<Image>();
            inputAreaBg.color = new Color(0.08f, 0.08f, 0.08f);
            inputAreaObj.AddComponent<RectMask2D>();
            var inputLe = inputAreaObj.AddComponent<LayoutElement>();
            inputLe.preferredWidth = 999;
            inputLe.flexibleWidth = 1;

            var textAreaObj = CreateUIObj("TextArea", inputAreaObj.transform);
            var textAreaRt = textAreaObj.GetComponent<RectTransform>();
            textAreaRt.anchorMin = Vector2.zero;
            textAreaRt.anchorMax = Vector2.one;
            textAreaRt.offsetMin = new Vector2(6, 4);
            textAreaRt.offsetMax = new Vector2(-6, -4);

            _inputField = inputAreaObj.AddComponent<TMP_InputField>();
            _inputField.textViewport = textAreaRt;
            _inputField.textComponent = textAreaObj.AddComponent<TextMeshProUGUI>();
            _inputField.textComponent.fontSize = 14;
            _inputField.textComponent.color = Color.white;

            var placeholderObj = CreateUIObj("Placeholder", textAreaObj.transform);
            var placeholderRt = placeholderObj.GetComponent<RectTransform>();
            placeholderRt.anchorMin = Vector2.zero;
            placeholderRt.anchorMax = Vector2.one;
            placeholderRt.offsetMin = new Vector2(6, 4);
            placeholderRt.offsetMax = new Vector2(-6, -4);
            var phTmp = placeholderObj.AddComponent<TextMeshProUGUI>();
            phTmp.fontSize = 14;
            phTmp.text = "Type a message...";
            phTmp.color = new Color(0.4f, 0.4f, 0.4f);
            _inputField.placeholder = phTmp;

            _inputField.onEndEdit.AddListener(OnMessageSubmitted);

            var sendBtnObj = CreateUIObj("SendBtn", inputRowObj.transform);
            sendBtnObj.AddComponent<CanvasRenderer>();
            var sendBtnImg = sendBtnObj.AddComponent<Image>();
            sendBtnImg.color = new Color(0.2f, 0.35f, 0.2f);
            var sendBtn = sendBtnObj.AddComponent<Button>();
            sendBtn.onClick.AddListener(OnSendClicked);
            var sendLe = sendBtnObj.AddComponent<LayoutElement>();
            sendLe.minWidth = 50;
            sendLe.preferredWidth = 50;
            var sendTxt = sendBtnObj.AddComponent<TextMeshProUGUI>();
            sendTxt.text = "Send";
            sendTxt.fontSize = 13;
            sendTxt.fontStyle = FontStyles.Bold;
            sendTxt.alignment = TextAlignmentOptions.Center;
            sendTxt.color = Color.white;

            AddSystemMessage("Chat is only available in online mode");
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
                SendMessage(_inputField.text.Trim());
                _inputField.text = "";
                _inputField.ActivateInputField();
            }
        }

        private void OnMessageSubmitted(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                SendMessage(text.Trim());
                _inputField.text = "";
            }
        }

        public void SendMessage(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            AddMessage("Me", text, true);

            if (player != null)
                player.SendChatMessage(text);
        }

        public void OnChatMessageReceived(string sender, string message)
        {
            AddMessage(sender, message, false);
        }

        private void AddMessage(string sender, string text, bool isMe)
        {
            var msg = new ChatMessage
            {
                sender = sender,
                text = text,
                isMe = isMe,
                time = DateTime.Now
            };
            _messages.Add(msg);
            CreateMessageBubble(msg);
        }

        private void AddSystemMessage(string text)
        {
            var obj = CreateUIObj("SysMsg", _contentParent);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 12;
            tmp.fontStyle = FontStyles.Italic;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = systemColor;

            Canvas.ForceUpdateCanvases();
            if (_scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 0f;
        }

        private void CreateMessageBubble(ChatMessage msg)
        {
            var bubble = CreateUIObj("MsgBubble", _contentParent);
            var bubbleRt = bubble.GetComponent<RectTransform>();
            bubbleRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 40);

            bubble.AddComponent<CanvasRenderer>();
            var bubbleImg = bubble.AddComponent<Image>();
            bubbleImg.color = msg.isMe ? myMsgColor : otherMsgColor;

            var vlg = bubble.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 1;
            vlg.padding = new RectOffset(8, 8, 4, 4);

            var senderObj = CreateUIObj("Sender", bubble.transform);
            var senderTmp = senderObj.AddComponent<TextMeshProUGUI>();
            senderTmp.text = $"{msg.sender}  {msg.time:HH:mm}";
            senderTmp.fontSize = 10;
            senderTmp.fontStyle = FontStyles.Bold;
            senderTmp.color = msg.isMe ? new Color(0.5f, 0.8f, 0.5f) : new Color(0.8f, 0.5f, 0.5f);

            var textObj = CreateUIObj("Text", bubble.transform);
            var textTmp = textObj.AddComponent<TextMeshProUGUI>();
            textTmp.text = msg.text;
            textTmp.fontSize = 14;
            textTmp.color = Color.white;
            textTmp.enableWordWrapping = true;

            Canvas.ForceUpdateCanvases();
            if (_scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 0f;
        }

        public void Show()
        {
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
    }
}
