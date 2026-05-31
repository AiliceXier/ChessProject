using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        public Player player;

        private GameObject _panel;
        private GameObject _localGameBtn;
        private GameObject _robotGameBtn;
        private GameObject _createBtn;
        private GameObject _joinBtn;
        private GameObject _lobbyCodeInput;
        private GameObject _resultTextObj;
        private GameObject _leaderboardBtn;

        private GameObject _onlineGameBtn;
        private GameObject _backBtn;
        private GameObject _titleObj;
        private GameObject _onlineTitleObj;
        private TextMeshProUGUI _resultText;

        private enum MenuState { MainMenu, OnlineOptions, WaitingForOpponent, Hidden }
        private MenuState _state = MenuState.Hidden;
        private string _pendingResult;

        public bool IsWaitingForOpponent => _state == MenuState.WaitingForOpponent;

        private GameObject _waitingObj;
        private TextMeshProUGUI _waitingCodeText;

        private static readonly Color LocalGameColor = new Color(0.18f, 0.32f, 0.52f);
        private static readonly Color RobotGameColor = new Color(0.18f, 0.48f, 0.28f);
        private static readonly Color OnlineGameColor = new Color(0.38f, 0.22f, 0.52f);
        private static readonly Color LeaderboardColor = new Color(0.48f, 0.38f, 0.18f);
        private static readonly Color CreateColor = new Color(0.18f, 0.55f, 0.30f);
        private static readonly Color JoinColor = new Color(0.20f, 0.38f, 0.65f);
        private static readonly Color BackColor = new Color(0.45f, 0.18f, 0.18f);

        private void Awake() { }

        public void Initialize(GameObject panel)
        {
            _panel = panel;

            foreach (Transform child in _panel.transform)
            {
                var name = child.name;
                if (name == "Local Game Button") _localGameBtn = child.gameObject;
                else if (name == "Robot Game Button") _robotGameBtn = child.gameObject;
                else if (name == "Create Button") _createBtn = child.gameObject;
                else if (name == "Join Button") _joinBtn = child.gameObject;
                else if (name == "Lobby Code Input") _lobbyCodeInput = child.gameObject;
                else if (name == "Result Text") { _resultTextObj = child.gameObject; _resultText = child.GetComponent<TextMeshProUGUI>(); }
                else if (name == "LeaderboardButton") _leaderboardBtn = child.gameObject;
            }

            var robotTxt = _robotGameBtn?.GetComponentInChildren<TextMeshProUGUI>();
            if (robotTxt != null) robotTxt.text = "vs AI";

            SetupLayout();

            _titleObj = CreateLabel("Title", _panel.transform, "Chess", 32, FontStyles.Bold, Color.white, 44);
            _onlineTitleObj = CreateLabel("OnlineTitle", _panel.transform, "Online Game", 24, FontStyles.Bold, Color.white, 36);
            _onlineTitleObj.SetActive(false);

            _onlineGameBtn = CreateMenuButton("Online Game", _panel.transform, OnlineGameColor, () =>
            {
                SetState(MenuState.OnlineOptions);
            });

            _backBtn = CreateMenuButton("Back", _panel.transform, BackColor, () =>
            {
                SetState(MenuState.MainMenu);
            });
            _backBtn.SetActive(false);

            _waitingObj = new GameObject("WaitingPanel");
            _waitingObj.transform.SetParent(_panel.transform, false);
            _waitingObj.layer = 5;
            _waitingObj.AddComponent<RectTransform>();
            var wLe = _waitingObj.AddComponent<LayoutElement>();
            wLe.preferredHeight = 120;
            var wVlg = _waitingObj.AddComponent<VerticalLayoutGroup>();
            wVlg.childAlignment = TextAnchor.MiddleCenter;
            wVlg.childControlWidth = true;
            wVlg.childControlHeight = false;
            wVlg.childForceExpandWidth = true;
            wVlg.childForceExpandHeight = false;
            wVlg.spacing = 8;

            var waitTitle = CreateLabel("WaitTitle", _waitingObj.transform, "Waiting for Opponent...", 20, FontStyles.Bold, new Color(1f, 0.85f, 0.3f), 28);
            var codeLabel = CreateLabel("CodeLabel", _waitingObj.transform, "Room Code:", 16, FontStyles.Normal, new Color(0.6f, 0.6f, 0.6f), 24);
            _waitingCodeText = CreateLabel("CodeValue", _waitingObj.transform, "---", 32, FontStyles.Bold, Color.white, 40).GetComponent<TextMeshProUGUI>();
            _waitingObj.SetActive(false);

            ReorderChildren();

            if (_resultTextObj != null) _resultTextObj.SetActive(false);

            _panel.SetActive(false);
        }

        private void SetupLayout()
        {
            var img = _panel.GetComponent<Image>();
            if (img == null)
            {
                _panel.AddComponent<CanvasRenderer>();
                img = _panel.AddComponent<Image>();
            }
            img.color = new Color(0.08f, 0.08f, 0.10f, 0.98f);
            img.raycastTarget = true;

            var vlg = _panel.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = _panel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 12;
            vlg.padding = new RectOffset(50, 50, 50, 50);

            var rt = _panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            SetupButtonLayout(_localGameBtn, LocalGameColor);
            SetupButtonLayout(_robotGameBtn, RobotGameColor);
            SetupButtonLayout(_createBtn, CreateColor);
            SetupButtonLayout(_joinBtn, JoinColor);
            SetupButtonLayout(_leaderboardBtn, LeaderboardColor);

            if (_lobbyCodeInput != null)
            {
                var le = _lobbyCodeInput.GetComponent<LayoutElement>();
                if (le == null) le = _lobbyCodeInput.AddComponent<LayoutElement>();
                le.preferredHeight = 44;
            }

            if (_resultTextObj != null)
            {
                var le = _resultTextObj.GetComponent<LayoutElement>();
                if (le == null) le = _resultTextObj.AddComponent<LayoutElement>();
                le.preferredHeight = 36;
            }
        }

        private void SetupButtonLayout(GameObject btn, Color color)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = color;

            var le = btn.GetComponent<LayoutElement>();
            if (le == null) le = btn.AddComponent<LayoutElement>();
            le.preferredHeight = 56;
            le.minHeight = 56;
            le.flexibleHeight = 0;
        }

        private void ReorderChildren()
        {
            var t = _panel.transform;
            var order = new GameObject[]
            {
                _titleObj, _onlineTitleObj, _resultTextObj,
                _localGameBtn, _robotGameBtn, _onlineGameBtn, _leaderboardBtn,
                _createBtn, _lobbyCodeInput, _joinBtn, _backBtn
            };
            foreach (var obj in order)
            {
                if (obj != null) obj.transform.SetAsLastSibling();
            }
        }

        private GameObject CreateMenuButton(string text, Transform parent, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var btnObj = new GameObject($"Btn_{text}");
            btnObj.transform.SetParent(parent, false);
            btnObj.layer = 5;
            btnObj.AddComponent<RectTransform>();
            btnObj.AddComponent<CanvasRenderer>();
            var img = btnObj.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 56;
            le.minHeight = 56;
            le.flexibleHeight = 0;

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            txtObj.layer = 5;
            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 17;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return btnObj;
        }

        private GameObject CreateLabel(string name, Transform parent, string text, float fontSize, FontStyles style, Color color, float height)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.layer = 5;
            obj.AddComponent<RectTransform>();
            var le = obj.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.raycastTarget = false;
            return obj;
        }

        private void SetState(MenuState state)
        {
            _state = state;
            switch (state)
            {
                case MenuState.MainMenu:
                    ShowMainMenu();
                    break;
                case MenuState.OnlineOptions:
                    ShowOnlineMenu();
                    break;
                case MenuState.WaitingForOpponent:
                    break;
                case MenuState.Hidden:
                    Hide();
                    break;
            }
        }

        private void ShowMainMenu()
        {
            if (_titleObj != null) _titleObj.SetActive(true);
            if (_onlineTitleObj != null) _onlineTitleObj.SetActive(false);
            if (_resultTextObj != null && string.IsNullOrEmpty(_pendingResult) && (_resultText == null || string.IsNullOrEmpty(_resultText.text)))
                _resultTextObj.SetActive(false);

            if (_localGameBtn != null) _localGameBtn.SetActive(true);
            if (_robotGameBtn != null) _robotGameBtn.SetActive(true);
            if (_onlineGameBtn != null) _onlineGameBtn.SetActive(true);
            if (_leaderboardBtn != null) _leaderboardBtn.SetActive(true);

            if (_createBtn != null) _createBtn.SetActive(false);
            if (_joinBtn != null) _joinBtn.SetActive(false);
            if (_lobbyCodeInput != null) _lobbyCodeInput.SetActive(false);
            if (_backBtn != null) _backBtn.SetActive(false);

            if (_panel != null) _panel.SetActive(true);
        }

        private void ShowOnlineMenu()
        {
            if (_titleObj != null) _titleObj.SetActive(false);
            if (_onlineTitleObj != null) _onlineTitleObj.SetActive(true);
            if (_resultTextObj != null) _resultTextObj.SetActive(false);

            if (_localGameBtn != null) _localGameBtn.SetActive(false);
            if (_robotGameBtn != null) _robotGameBtn.SetActive(false);
            if (_onlineGameBtn != null) _onlineGameBtn.SetActive(false);
            if (_leaderboardBtn != null) _leaderboardBtn.SetActive(false);

            if (_createBtn != null) _createBtn.SetActive(true);
            if (_joinBtn != null) _joinBtn.SetActive(true);
            if (_lobbyCodeInput != null) _lobbyCodeInput.SetActive(true);
            if (_backBtn != null) _backBtn.SetActive(true);
            if (_waitingObj != null) _waitingObj.SetActive(false);

            if (_panel != null) _panel.SetActive(true);
        }

        public void ShowWaitingForOpponent(string lobbyCode)
        {
            if (_waitingCodeText != null) _waitingCodeText.text = lobbyCode;

            if (_titleObj != null) _titleObj.SetActive(false);
            if (_onlineTitleObj != null) _onlineTitleObj.SetActive(true);
            if (_resultTextObj != null) _resultTextObj.SetActive(false);

            if (_localGameBtn != null) _localGameBtn.SetActive(false);
            if (_robotGameBtn != null) _robotGameBtn.SetActive(false);
            if (_onlineGameBtn != null) _onlineGameBtn.SetActive(false);
            if (_leaderboardBtn != null) _leaderboardBtn.SetActive(false);
            if (_createBtn != null) _createBtn.SetActive(false);
            if (_joinBtn != null) _joinBtn.SetActive(false);
            if (_lobbyCodeInput != null) _lobbyCodeInput.SetActive(false);
            if (_backBtn != null) _backBtn.SetActive(true);
            if (_waitingObj != null) _waitingObj.SetActive(true);

            _state = MenuState.WaitingForOpponent;
            if (_panel != null) _panel.SetActive(true);
        }

        public void Show()
        {
            if (_panel == null) return;

            if (!string.IsNullOrEmpty(_pendingResult) && _resultText != null)
            {
                _resultText.text = _pendingResult;
                _pendingResult = null;
                if (_resultTextObj != null) _resultTextObj.SetActive(true);
            }

            SetState(MenuState.MainMenu);
        }

        public void ShowWithResult(string result)
        {
            _pendingResult = result;
            Show();
        }

        public void Hide()
        {
            _state = MenuState.Hidden;
            if (_panel != null) _panel.SetActive(false);
        }
    }
}
