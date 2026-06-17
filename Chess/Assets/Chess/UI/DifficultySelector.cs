using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Chess.UI
{
    public class DifficultySelector : MonoBehaviour
    {
        public Player player;

        public GameObject panelRef;
        public GameObject[] difficultyBtnRefs;
        public GameObject backBtnRef;

        private GameObject _panel;
        private int _selectedDifficulty = 1;

        private static readonly (string name, int depth, string desc, string icon, Color color)[] Difficulties =
        {
            ("Easy", 1, "Beginner friendly", "\u2654", new Color(0.35f, 0.65f, 0.35f)),
            ("Medium", 3, "Casual challenge", "\u2655", new Color(0.25f, 0.55f, 0.80f)),
            ("Hard", 4, "Cloud AI \u2014 fast", "\u2656", new Color(0.80f, 0.50f, 0.20f)),
            ("Master", 5, "Cloud AI \u2014 thinking", "\u2657", new Color(0.75f, 0.20f, 0.20f))
        };

        private Color panelColor = new Color(0.08f, 0.08f, 0.10f, 0.98f);
        private Color btnColor = new Color(0.15f, 0.15f, 0.18f, 1f);
        private Color btnHoverColor = new Color(0.20f, 0.20f, 0.24f, 1f);
        private Color selectedBorderColor = new Color(0.90f, 0.75f, 0.25f, 1f);

        private GameObject[] _difficultyButtons;
        private Image[] _difficultyButtonImages;
        private GameObject[] _difficultyBorderImages;

        private void Awake() { }

        private void EnsurePanel()
        {
            if (_panel != null) return;

            if (panelRef != null)
            {
                _panel = panelRef;

                string[] btnNames = { "Btn_Easy", "Btn_Medium", "Btn_Hard", "Btn_Master" };
                _difficultyButtons = new GameObject[Difficulties.Length];
                _difficultyButtonImages = new Image[Difficulties.Length];
                _difficultyBorderImages = new GameObject[Difficulties.Length];

                for (int i = 0; i < btnNames.Length && i < Difficulties.Length; i++)
                {
                    var btnTr = _panel.transform.Find(btnNames[i]);
                    if (btnTr == null) continue;
                    var idx = i;
                    _difficultyButtons[i] = btnTr.gameObject;
                    _difficultyButtonImages[i] = btnTr.GetComponent<Image>();

                    var btn = btnTr.GetComponent<Button>();
                    if (btn == null)
                        btn = btnTr.gameObject.AddComponent<Button>();
                    btn.onClick.AddListener(() => OnDifficultySelected(idx));

                    var borderTr = btnTr.Find("Border");
                    if (borderTr != null)
                        _difficultyBorderImages[i] = borderTr.gameObject;
                }

                var backTr = _panel.transform.Find("BackBtn");
                if (backTr != null)
                {
                    var backBtn = backTr.GetComponent<Button>();
                    if (backBtn == null)
                        backBtn = backTr.gameObject.AddComponent<Button>();
                    backBtn.onClick.AddListener(Hide);
                }

                _panel.SetActive(false);
                return;
            }

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;
            BuildUI(canvas.transform);
        }

        private void BuildUI(Transform canvasTr)
        {
            _panel = new GameObject("DifficultyPanel");
            _panel.transform.SetParent(canvasTr, false);
            _panel.layer = 5;

            var panelRt = _panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(360, 480);

            _panel.AddComponent<CanvasRenderer>();
            var bg = _panel.AddComponent<Image>();
            bg.color = panelColor;
            bg.raycastTarget = true;

            var vlg = _panel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 10;
            vlg.padding = new RectOffset(28, 28, 28, 28);

            var titleObj = CreateUIObj("Title", _panel.transform);
            var titleLe = titleObj.AddComponent<LayoutElement>();
            titleLe.preferredHeight = 40;
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "Select Difficulty";
            titleTxt.fontSize = 24;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = Color.white;
            titleTxt.raycastTarget = false;

            var subtitleObj = CreateUIObj("Subtitle", _panel.transform);
            var subtitleLe = subtitleObj.AddComponent<LayoutElement>();
            subtitleLe.preferredHeight = 22;
            var subtitleTxt = subtitleObj.AddComponent<TextMeshProUGUI>();
            subtitleTxt.text = "Easy/Medium use on-device engine • Hard/Master use Claude (network)";
            subtitleTxt.fontSize = 13;
            subtitleTxt.alignment = TextAlignmentOptions.Center;
            subtitleTxt.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            subtitleTxt.raycastTarget = false;

            var spacerObj = CreateUIObj("Spacer", _panel.transform);
            var spacerLe = spacerObj.AddComponent<LayoutElement>();
            spacerLe.preferredHeight = 8;

            _difficultyButtons = new GameObject[Difficulties.Length];
            _difficultyButtonImages = new Image[Difficulties.Length];
            _difficultyBorderImages = new GameObject[Difficulties.Length];

            for (int i = 0; i < Difficulties.Length; i++)
            {
                var idx = i;
                var btnObj = CreateDifficultyCard(i, _panel.transform);
                _difficultyButtons[i] = btnObj;
                _difficultyButtonImages[i] = btnObj.GetComponent<Image>();

                var borderObj = new GameObject("Border");
                borderObj.transform.SetParent(btnObj.transform, false);
                borderObj.layer = 5;
                var borderRt = borderObj.AddComponent<RectTransform>();
                borderRt.anchorMin = Vector2.zero;
                borderRt.anchorMax = Vector2.one;
                borderRt.offsetMin = new Vector2(-3, -3);
                borderRt.offsetMax = new Vector2(3, 3);
                var borderImg = borderObj.AddComponent<Image>();
                borderImg.color = selectedBorderColor;
                borderImg.raycastTarget = false;
                var borderMask = borderObj.AddComponent<Mask>();
                borderMask.showMaskGraphic = false;
                var borderInner = new GameObject("Inner");
                borderInner.transform.SetParent(borderObj.transform, false);
                borderInner.layer = 5;
                var borderInnerRt = borderInner.AddComponent<RectTransform>();
                borderInnerRt.anchorMin = Vector2.zero;
                borderInnerRt.anchorMax = Vector2.one;
                borderInnerRt.offsetMin = new Vector2(2, 2);
                borderInnerRt.offsetMax = new Vector2(-2, -2);
                var borderInnerImg = borderInner.AddComponent<Image>();
                borderInnerImg.color = selectedBorderColor;
                borderInnerImg.raycastTarget = false;
                borderObj.SetActive(i == _selectedDifficulty);
                _difficultyBorderImages[i] = borderObj;

                var btn = btnObj.GetComponent<Button>();
                btn.onClick.AddListener(() => OnDifficultySelected(idx));

                AddHoverEffect(btnObj, btnColor, btnHoverColor);
            }

            var backObj = CreateUIObj("BackBtn", _panel.transform);
            backObj.AddComponent<CanvasRenderer>();
            var backImg = backObj.AddComponent<Image>();
            backImg.color = new Color(0.35f, 0.15f, 0.15f, 1f);
            backImg.raycastTarget = true;
            var backBtn = backObj.AddComponent<Button>();
            backBtn.onClick.AddListener(Hide);
            var backLe = backObj.AddComponent<LayoutElement>();
            backLe.preferredHeight = 44;
            var backTxtObj = CreateUIObj("Text", backObj.transform);
            var backTxtRt = backTxtObj.GetComponent<RectTransform>();
            backTxtRt.anchorMin = Vector2.zero;
            backTxtRt.anchorMax = Vector2.one;
            backTxtRt.offsetMin = Vector2.zero;
            backTxtRt.offsetMax = Vector2.zero;
            var backTxt = backTxtObj.AddComponent<TextMeshProUGUI>();
            backTxt.text = "Back";
            backTxt.fontSize = 15;
            backTxt.fontStyle = FontStyles.Bold;
            backTxt.alignment = TextAlignmentOptions.Center;
            backTxt.color = new Color(0.9f, 0.7f, 0.7f, 1f);
            backTxt.raycastTarget = false;

            _panel.SetActive(false);
        }

        private GameObject CreateDifficultyCard(int index, Transform parent)
        {
            var diff = Difficulties[index];

            var cardObj = new GameObject($"Btn_{diff.name}");
            cardObj.transform.SetParent(parent, false);
            cardObj.layer = 5;

            var cardRt = cardObj.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0, 0);
            cardRt.anchorMax = new Vector2(1, 0);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(0, 72);

            cardObj.AddComponent<CanvasRenderer>();
            var cardImg = cardObj.AddComponent<Image>();
            cardImg.color = btnColor;
            cardImg.raycastTarget = true;

            var cardBtn = cardObj.AddComponent<Button>();
            cardBtn.transition = Selectable.Transition.None;

            var cardLe = cardObj.AddComponent<LayoutElement>();
            cardLe.preferredHeight = 72;
            cardLe.minHeight = 72;
            cardLe.flexibleHeight = 0;

            var hlg = new GameObject("Content");
            hlg.transform.SetParent(cardObj.transform, false);
            hlg.layer = 5;
            var hlgRt = hlg.AddComponent<RectTransform>();
            hlgRt.anchorMin = Vector2.zero;
            hlgRt.anchorMax = Vector2.one;
            hlgRt.offsetMin = new Vector2(16, 0);
            hlgRt.offsetMax = new Vector2(-16, 0);
            var hlgComp = hlg.AddComponent<HorizontalLayoutGroup>();
            hlgComp.childAlignment = TextAnchor.MiddleLeft;
            hlgComp.childControlWidth = false;
            hlgComp.childControlHeight = false;
            hlgComp.childForceExpandWidth = false;
            hlgComp.childForceExpandHeight = false;
            hlgComp.spacing = 14;

            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(hlg.transform, false);
            iconObj.layer = 5;
            var iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.sizeDelta = new Vector2(36, 36);
            var iconTxt = iconObj.AddComponent<TextMeshProUGUI>();
            iconTxt.text = diff.icon;
            iconTxt.fontSize = 28;
            iconTxt.alignment = TextAlignmentOptions.Center;
            iconTxt.color = diff.color;
            iconTxt.raycastTarget = false;

            var textContainer = new GameObject("TextContainer");
            textContainer.transform.SetParent(hlg.transform, false);
            textContainer.layer = 5;
            var textContainerRt = textContainer.AddComponent<RectTransform>();
            textContainerRt.sizeDelta = new Vector2(200, 50);
            var textVlg = textContainer.AddComponent<VerticalLayoutGroup>();
            textVlg.childAlignment = TextAnchor.MiddleLeft;
            textVlg.childControlWidth = false;
            textVlg.childControlHeight = false;
            textVlg.childForceExpandWidth = false;
            textVlg.childForceExpandHeight = false;
            textVlg.spacing = 2;

            var nameObj = new GameObject("Name");
            nameObj.transform.SetParent(textContainer.transform, false);
            nameObj.layer = 5;
            var nameRt = nameObj.AddComponent<RectTransform>();
            nameRt.sizeDelta = new Vector2(200, 22);
            var nameTxt = nameObj.AddComponent<TextMeshProUGUI>();
            nameTxt.text = diff.name;
            nameTxt.fontSize = 17;
            nameTxt.fontStyle = FontStyles.Bold;
            nameTxt.alignment = TextAlignmentOptions.Left;
            nameTxt.color = Color.white;
            nameTxt.raycastTarget = false;

            var descObj = new GameObject("Desc");
            descObj.transform.SetParent(textContainer.transform, false);
            descObj.layer = 5;
            var descRt = descObj.AddComponent<RectTransform>();
            descRt.sizeDelta = new Vector2(200, 18);
            var descTxt = descObj.AddComponent<TextMeshProUGUI>();
            descTxt.text = diff.desc;
            descTxt.fontSize = 12;
            descTxt.alignment = TextAlignmentOptions.Left;
            descTxt.color = new Color(0.6f, 0.6f, 0.65f, 1f);
            descTxt.raycastTarget = false;

            var depthObj = new GameObject("Depth");
            depthObj.transform.SetParent(hlg.transform, false);
            depthObj.layer = 5;
            var depthRt = depthObj.AddComponent<RectTransform>();
            depthRt.sizeDelta = new Vector2(60, 20);
            var depthTxt = depthObj.AddComponent<TextMeshProUGUI>();
            depthTxt.text = $"Depth {diff.depth}";
            depthTxt.fontSize = 11;
            depthTxt.alignment = TextAlignmentOptions.Right;
            depthTxt.color = new Color(0.5f, 0.5f, 0.55f, 1f);
            depthTxt.raycastTarget = false;

            return cardObj;
        }

        private void AddHoverEffect(GameObject btnObj, Color normalColor, Color hoverColor)
        {
            var trigger = btnObj.GetComponent<EventTrigger>();
            if (trigger == null) trigger = btnObj.AddComponent<EventTrigger>();

            var enter = new EventTrigger.Entry();
            enter.eventID = EventTriggerType.PointerEnter;
            enter.callback.AddListener((data) =>
            {
                var img = btnObj.GetComponent<Image>();
                if (img != null) img.color = hoverColor;
            });
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry();
            exit.eventID = EventTriggerType.PointerExit;
            exit.callback.AddListener((data) =>
            {
                var img = btnObj.GetComponent<Image>();
                if (img != null) img.color = normalColor;
            });
            trigger.triggers.Add(exit);
        }

        private GameObject CreateUIObj(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.layer = 5;
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private void OnDifficultySelected(int index)
        {
            _selectedDifficulty = index;
            UpdateSelectionVisuals();
            player?.StartRobotGameWithDifficulty(Difficulties[index].depth);
        }

        private void UpdateSelectionVisuals()
        {
            if (_difficultyBorderImages == null) return;
            for (int i = 0; i < _difficultyBorderImages.Length; i++)
            {
                if (_difficultyBorderImages[i] != null)
                    _difficultyBorderImages[i].SetActive(i == _selectedDifficulty);
            }
        }

        public void Show()
        {
            EnsurePanel();
            if (_panel != null)
            {
                UpdateSelectionVisuals();
                _panel.SetActive(true);
            }
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
        }
    }
}
