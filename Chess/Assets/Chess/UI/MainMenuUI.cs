using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        public Player player;

        private GameObject _panel;
        private GameObject _onlineSubPanel;
        private TextMeshProUGUI _resultText;
        private string _pendingResult;

        private Color panelColor = new Color(0.1f, 0.1f, 0.12f, 0.98f);
        private Color cardColor = new Color(0.2f, 0.2f, 0.26f, 1f);
        private Color onlineCardColor = new Color(0.15f, 0.25f, 0.35f, 1f);
        private Color leaderboardCardColor = new Color(0.2f, 0.3f, 0.2f, 1f);

        private void Awake()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;
            BuildUI(canvas.transform);
        }

        private void BuildUI(Transform canvasTr)
        {
            _panel = new GameObject("MainMenuPanel");
            _panel.transform.SetParent(canvasTr, false);
            _panel.layer = 5;

            var panelRt = _panel.AddComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            _panel.AddComponent<CanvasRenderer>();
            var bg = _panel.AddComponent<Image>();
            bg.color = panelColor;
            bg.raycastTarget = false;

            var vlg = _panel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 16;
            vlg.padding = new RectOffset(60, 60, 40, 40);

            AddSpacer(_panel.transform, 40);

            var titleObj = CreateUIObj("Title", _panel.transform);
            var titleLe = titleObj.AddComponent<LayoutElement>();
            titleLe.preferredHeight = 50;
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "Chess";
            titleTxt.fontSize = 42;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = Color.white;
            titleTxt.raycastTarget = false;

            AddSpacer(_panel.transform, 10);

            var resultObj = CreateUIObj("ResultText", _panel.transform);
            var resultLe = resultObj.AddComponent<LayoutElement>();
            resultLe.preferredHeight = 36;
            _resultText = resultObj.AddComponent<TextMeshProUGUI>();
            _resultText.fontSize = 22;
            _resultText.fontStyle = FontStyles.Bold;
            _resultText.alignment = TextAlignmentOptions.Center;
            _resultText.color = new Color(0.9f, 0.85f, 0.4f);
            _resultText.raycastTarget = false;
            resultObj.SetActive(false);

            AddSpacer(_panel.transform, 20);

            CreateModeCard("Local Game", "Play with a friend on the same device", cardColor, () =>
            {
                player?.StartLocalGame();
                Hide();
            });

            CreateModeCard("vs AI", "Challenge the AI, choose difficulty", cardColor, () =>
            {
                player?.StartRobotGame();
            });

            CreateModeCard("Online Game", "Play with a friend over the internet", onlineCardColor, () =>
            {
                ShowOnlineOptions();
            });

            CreateModeCard("Leaderboard", "View global rankings", leaderboardCardColor, () =>
            {
                player?.ShowLeaderboard();
            });
        }

        private void ShowOnlineOptions()
        {
            if (_onlineSubPanel != null) Destroy(_onlineSubPanel);

            _onlineSubPanel = new GameObject("OnlineSubPanel");
            _onlineSubPanel.transform.SetParent(_panel.transform, false);
            _onlineSubPanel.layer = 5;

            var subRt = _onlineSubPanel.AddComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.2f, 0.1f);
            subRt.anchorMax = new Vector2(0.8f, 0.6f);
            subRt.offsetMin = Vector2.zero;
            subRt.offsetMax = Vector2.zero;

            _onlineSubPanel.AddComponent<CanvasRenderer>();
            var subBg = _onlineSubPanel.AddComponent<Image>();
            subBg.color = new Color(0.08f, 0.08f, 0.1f, 0.98f);
            subBg.raycastTarget = false;

            var subVlg = _onlineSubPanel.AddComponent<VerticalLayoutGroup>();
            subVlg.childAlignment = TextAnchor.MiddleCenter;
            subVlg.childControlWidth = true;
            subVlg.childControlHeight = false;
            subVlg.childForceExpandWidth = true;
            subVlg.childForceExpandHeight = false;
            subVlg.spacing = 12;
            subVlg.padding = new RectOffset(20, 20, 20, 20);

            var subTitle = CreateUIObj("SubTitle", _onlineSubPanel.transform);
            var subTitleLe = subTitle.AddComponent<LayoutElement>();
            subTitleLe.preferredHeight = 36;
            var subTitleTxt = subTitle.AddComponent<TextMeshProUGUI>();
            subTitleTxt.text = "Online Game";
            subTitleTxt.fontSize = 24;
            subTitleTxt.fontStyle = FontStyles.Bold;
            subTitleTxt.alignment = TextAlignmentOptions.Center;
            subTitleTxt.color = Color.white;
            subTitleTxt.raycastTarget = false;

            CreateSubButton("Create Room", new Color(0.2f, 0.5f, 0.3f), _onlineSubPanel.transform, () =>
            {
                player?.CreateGame();
                Hide();
            });

            var codeRow = CreateUIObj("CodeRow", _onlineSubPanel.transform);
            var codeLe = codeRow.AddComponent<LayoutElement>();
            codeLe.preferredHeight = 36;
            var codeHlg = codeRow.AddComponent<HorizontalLayoutGroup>();
            codeHlg.childAlignment = TextAnchor.MiddleCenter;
            codeHlg.childControlWidth = true;
            codeHlg.childControlHeight = false;
            codeHlg.childForceExpandWidth = true;
            codeHlg.childForceExpandHeight = false;
            codeHlg.spacing = 8;

            var codeInputObj = CreateUIObj("CodeInput", codeRow.transform);
            var codeInputLe = codeInputObj.AddComponent<LayoutElement>();
            codeInputLe.flexibleWidth = 1;
            codeInputLe.minWidth = 100;
            codeInputObj.AddComponent<CanvasRenderer>();
            var codeInputBg = codeInputObj.AddComponent<Image>();
            codeInputBg.color = new Color(0.15f, 0.15f, 0.15f);
            codeInputBg.raycastTarget = true;
            var codeInput = codeInputObj.AddComponent<TMP_InputField>();

            var codeTextArea = CreateUIObj("TextArea", codeInputObj.transform);
            var codeTaRt = codeTextArea.GetComponent<RectTransform>();
            codeTaRt.anchorMin = Vector2.zero;
            codeTaRt.anchorMax = Vector2.one;
            codeTaRt.offsetMin = new Vector2(6, 2);
            codeTaRt.offsetMax = new Vector2(-6, -2);
            codeTextArea.AddComponent<RectMask2D>();
            var codeInputTxt = codeTextArea.AddComponent<TextMeshProUGUI>();
            codeInputTxt.fontSize = 16;
            codeInputTxt.color = Color.white;
            codeInputTxt.raycastTarget = false;
            var codePlaceholder = codeTextArea.AddComponent<TextMeshProUGUI>();
            codePlaceholder.fontSize = 16;
            codePlaceholder.color = new Color(0.4f, 0.4f, 0.4f);
            codePlaceholder.text = "Room code...";
            codePlaceholder.raycastTarget = false;

            codeInput.textComponent = codeInputTxt;
            codeInput.placeholder = codePlaceholder;

            CreateSubButton("Join", new Color(0.2f, 0.3f, 0.6f), codeRow.transform, () =>
            {
                player?.SetLobbyCode(codeInput.text);
                player?.JoinLobbyByCode();
                Hide();
            });

            CreateSubButton("Back", new Color(0.4f, 0.2f, 0.2f), _onlineSubPanel.transform, () =>
            {
                if (_onlineSubPanel != null) Destroy(_onlineSubPanel);
            });
        }

        private void CreateModeCard(string title, string desc, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var card = CreateUIObj($"Card_{title}", _panel.transform);
            card.AddComponent<CanvasRenderer>();
            var cardImg = card.AddComponent<Image>();
            cardImg.color = color;
            cardImg.raycastTarget = true;
            var cardBtn = card.AddComponent<Button>();
            cardBtn.onClick.AddListener(onClick);
            var cardLe = card.AddComponent<LayoutElement>();
            cardLe.preferredHeight = 64;

            var cardVlg = card.AddComponent<VerticalLayoutGroup>();
            cardVlg.childAlignment = TextAnchor.MiddleCenter;
            cardVlg.childControlWidth = true;
            cardVlg.childControlHeight = false;
            cardVlg.childForceExpandWidth = true;
            cardVlg.childForceExpandHeight = false;
            cardVlg.spacing = 2;
            cardVlg.padding = new RectOffset(16, 16, 8, 8);

            var titleObj = CreateUIObj("Title", card.transform);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = title;
            titleTxt.fontSize = 20;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = Color.white;
            titleTxt.raycastTarget = false;

            var descObj = CreateUIObj("Desc", card.transform);
            var descTxt = descObj.AddComponent<TextMeshProUGUI>();
            descTxt.text = desc;
            descTxt.fontSize = 13;
            descTxt.alignment = TextAlignmentOptions.Center;
            descTxt.color = new Color(0.7f, 0.7f, 0.7f);
            descTxt.raycastTarget = false;
        }

        private void CreateSubButton(string text, Color color, Transform parent, UnityEngine.Events.UnityAction onClick)
        {
            var btnObj = CreateUIObj($"Btn_{text}", parent);
            btnObj.AddComponent<CanvasRenderer>();
            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = color;
            btnImg.raycastTarget = true;
            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            var btnLe = btnObj.AddComponent<LayoutElement>();
            btnLe.preferredHeight = 40;
            var btnTxt = btnObj.AddComponent<TextMeshProUGUI>();
            btnTxt.text = text;
            btnTxt.fontSize = 16;
            btnTxt.alignment = TextAlignmentOptions.Center;
            btnTxt.color = Color.white;
            btnTxt.raycastTarget = false;
        }

        private GameObject CreateUIObj(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.layer = 5;
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private void AddSpacer(Transform parent, float height)
        {
            var spacer = CreateUIObj("Spacer", parent);
            var le = spacer.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleHeight = 0;
        }

        public void Show()
        {
            if (_panel == null)
            {
                var canvas = FindObjectOfType<Canvas>();
                if (canvas != null) BuildUI(canvas.transform);
            }
            if (_panel != null) _panel.SetActive(true);
            if (!string.IsNullOrEmpty(_pendingResult) && _resultText != null)
            {
                _resultText.gameObject.SetActive(true);
                _resultText.text = _pendingResult;
                _pendingResult = null;
            }
        }

        public void ShowWithResult(string result)
        {
            _pendingResult = result;
            Show();
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
            if (_resultText != null) _resultText.gameObject.SetActive(false);
            if (_onlineSubPanel != null) Destroy(_onlineSubPanel);
        }
    }
}
