using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        public Player player;

        public Color panelColor = new Color(0.08f, 0.08f, 0.12f, 0.98f);
        public Color cardColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
        public Color cardHoverColor = new Color(0.2f, 0.2f, 0.28f, 0.95f);
        public Color accentColor = new Color(0.3f, 0.5f, 0.8f, 1f);
        public Color goldColor = new Color(0.85f, 0.75f, 0.3f, 1f);

        private GameObject _panel;

        private void Awake()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            _panel = new GameObject("MainMenuPanel");
            _panel.transform.SetParent(canvas.transform, false);
            _panel.layer = 5;

            var panelRt = _panel.AddComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            _panel.AddComponent<CanvasRenderer>();
            var bg = _panel.AddComponent<Image>();
            bg.color = panelColor;

            var vlg = _panel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 16;
            vlg.padding = new RectOffset(60, 60, 40, 40);

            var titleObj = CreateUIObj("Title", _panel.transform);
            var titleLe = titleObj.AddComponent<LayoutElement>();
            titleLe.preferredHeight = 60;
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "♚ 国际象棋 ♔";
            titleTxt.fontSize = 42;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = goldColor;

            var subtitleObj = CreateUIObj("Subtitle", _panel.transform);
            var subtitleLe = subtitleObj.AddComponent<LayoutElement>();
            subtitleLe.preferredHeight = 28;
            var subtitleTxt = subtitleObj.AddComponent<TextMeshProUGUI>();
            subtitleTxt.text = "Chess Game";
            subtitleTxt.fontSize = 18;
            subtitleTxt.fontStyle = FontStyles.Normal;
            subtitleTxt.alignment = TextAlignmentOptions.Center;
            subtitleTxt.color = new Color(0.6f, 0.6f, 0.7f);

            AddSpacer(_panel.transform, 20);

            CreateModeCard("🎮 本地双人", "与朋友在同一设备上对弈", cardColor, () =>
            {
                player?.StartLocalGame();
                Hide();
            });

            CreateModeCard("🤖 人机对战", "挑战AI，选择难度等级", cardColor, () =>
            {
                player?.StartRobotGame();
                Hide();
            });

            CreateModeCard("🌐 在线对战", "通过房间代码与远方的朋友对弈", cardColor, () =>
            {
                ShowOnlineOptions();
            });

            AddSpacer(_panel.transform, 10);

            var versionObj = CreateUIObj("Version", _panel.transform);
            var versionLe = versionObj.AddComponent<LayoutElement>();
            versionLe.preferredHeight = 24;
            var versionTxt = versionObj.AddComponent<TextMeshProUGUI>();
            versionTxt.text = "v1.0 — Unity + Chess.NET";
            versionTxt.fontSize = 12;
            versionTxt.alignment = TextAlignmentOptions.Center;
            versionTxt.color = new Color(0.4f, 0.4f, 0.4f);
        }

        private void CreateModeCard(string title, string description, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var card = CreateUIObj("Card", _panel.transform);
            card.AddComponent<CanvasRenderer>();
            var cardImg = card.AddComponent<Image>();
            cardImg.color = color;
            var cardLe = card.AddComponent<LayoutElement>();
            cardLe.preferredHeight = 70;
            var cardBtn = card.AddComponent<Button>();

            var colors = cardBtn.colors;
            colors.highlightedColor = cardHoverColor;
            cardBtn.colors = colors;
            cardBtn.onClick.AddListener(onClick);

            var cvlg = card.AddComponent<VerticalLayoutGroup>();
            cvlg.childAlignment = TextAnchor.MiddleCenter;
            cvlg.childControlWidth = true;
            cvlg.childControlHeight = true;
            cvlg.childForceExpandWidth = true;
            cvlg.childForceExpandHeight = false;
            cvlg.spacing = 2;
            cvlg.padding = new RectOffset(16, 16, 8, 8);

            var titleObj = CreateUIObj("CardTitle", card.transform);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = title;
            titleTxt.fontSize = 20;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = Color.white;

            var descObj = CreateUIObj("CardDesc", card.transform);
            var descTxt = descObj.AddComponent<TextMeshProUGUI>();
            descTxt.text = description;
            descTxt.fontSize = 13;
            descTxt.alignment = TextAlignmentOptions.Center;
            descTxt.color = new Color(0.6f, 0.6f, 0.7f);
        }

        private void ShowOnlineOptions()
        {
            if (_panel != null) Destroy(_panel);

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            _panel = new GameObject("OnlinePanel");
            _panel.transform.SetParent(canvas.transform, false);
            _panel.layer = 5;

            var panelRt = _panel.AddComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            _panel.AddComponent<CanvasRenderer>();
            var bg = _panel.AddComponent<Image>();
            bg.color = panelColor;

            var vlg = _panel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 16;
            vlg.padding = new RectOffset(60, 60, 40, 40);

            var titleObj = CreateUIObj("Title", _panel.transform);
            var titleLe = titleObj.AddComponent<LayoutElement>();
            titleLe.preferredHeight = 50;
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "🌐 在线对战";
            titleTxt.fontSize = 32;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = goldColor;

            CreateModeCard("创建房间", "创建新房间并等待对手加入", cardColor, () =>
            {
                player?.CreateGame();
                Hide();
            });

            var inputObj = CreateUIObj("LobbyInput", _panel.transform);
            var inputLe = inputObj.AddComponent<LayoutElement>();
            inputLe.preferredHeight = 40;
            inputObj.AddComponent<CanvasRenderer>();
            var inputBg = inputObj.AddComponent<Image>();
            inputBg.color = new Color(0.12f, 0.12f, 0.15f);
            inputObj.AddComponent<RectMask2D>();

            var inputField = inputObj.AddComponent<TMP_InputField>();
            var textArea = CreateUIObj("TextArea", inputObj.transform);
            var textAreaRt = textArea.GetComponent<RectTransform>();
            textAreaRt.anchorMin = Vector2.zero;
            textAreaRt.anchorMax = Vector2.one;
            textAreaRt.offsetMin = new Vector2(10, 4);
            textAreaRt.offsetMax = new Vector2(-10, -4);
            var textComp = textArea.AddComponent<TextMeshProUGUI>();
            textComp.fontSize = 16;
            textComp.color = Color.white;
            inputField.textViewport = textAreaRt;
            inputField.textComponent = textComp;

            var phObj = CreateUIObj("Placeholder", textArea.transform);
            var phRt = phObj.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(10, 4);
            phRt.offsetMax = new Vector2(-10, -4);
            var phTmp = phObj.AddComponent<TextMeshProUGUI>();
            phTmp.text = "输入房间代码...";
            phTmp.fontSize = 16;
            phTmp.color = new Color(0.4f, 0.4f, 0.4f);
            inputField.placeholder = phTmp;

            CreateModeCard("加入房间", "输入房间代码加入对局", cardColor, () =>
            {
                var code = inputField.text;
                if (!string.IsNullOrWhiteSpace(code))
                {
                    player?.SetLobbyCode(code);
                    player?.JoinLobbyByCode();
                    Hide();
                }
            });

            AddSpacer(_panel.transform, 10);

            CreateModeCard("← 返回主菜单", "", new Color(0.15f, 0.15f, 0.15f, 0.9f), () =>
            {
                if (_panel != null) Destroy(_panel);
                BuildUI();
            });
        }

        private void AddSpacer(Transform parent, float height)
        {
            var spacer = CreateUIObj("Spacer", parent);
            var le = spacer.AddComponent<LayoutElement>();
            le.preferredHeight = height;
        }

        private GameObject CreateUIObj(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.layer = 5;
            obj.AddComponent<RectTransform>();
            return obj;
        }

        public void Show()
        {
            if (_panel != null) _panel.SetActive(true);
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
        }
    }
}
