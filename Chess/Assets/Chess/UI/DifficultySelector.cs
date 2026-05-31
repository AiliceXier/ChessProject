using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        private static readonly (string name, int depth)[] Difficulties =
        {
            ("Easy", 1),
            ("Medium", 3),
            ("Hard", 4),
            ("Master", 5)
        };

        private Color panelColor = new Color(0.10f, 0.10f, 0.12f, 0.98f);
        private Color cardColor = new Color(0.22f, 0.22f, 0.28f, 1f);
        private Color selectedColor = new Color(0.29f, 0.48f, 0.71f, 1f);
        private Color btnColor = new Color(0.22f, 0.22f, 0.28f, 1f);

        private void Awake() { }

        private void EnsurePanel()
        {
            if (_panel != null) return;

            if (panelRef != null)
            {
                _panel = panelRef;

                if (difficultyBtnRefs != null)
                {
                    for (int i = 0; i < difficultyBtnRefs.Length && i < Difficulties.Length; i++)
                    {
                        if (difficultyBtnRefs[i] == null) continue;
                        var idx = i;
                        var btn = difficultyBtnRefs[i].GetComponent<Button>();
                        if (btn == null)
                            btn = difficultyBtnRefs[i].AddComponent<Button>();
                        btn.onClick.AddListener(() => OnDifficultySelected(idx));
                    }
                }

                if (backBtnRef != null)
                {
                    var backBtn = backBtnRef.GetComponent<Button>();
                    if (backBtn == null)
                        backBtn = backBtnRef.AddComponent<Button>();
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
            panelRt.sizeDelta = new Vector2(280, 280);
            Debug.Log($"[DifficultySelector] Panel sizeDelta={panelRt.sizeDelta}, anchoredPosition={panelRt.anchoredPosition}");

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
            vlg.spacing = 8;
            vlg.padding = new RectOffset(20, 20, 20, 20);

            var titleObj = CreateUIObj("Title", _panel.transform);
            var titleLe = titleObj.AddComponent<LayoutElement>();
            titleLe.preferredHeight = 36;
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "Select Difficulty";
            titleTxt.fontSize = 20;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = Color.white;
            titleTxt.raycastTarget = false;

            for (int i = 0; i < Difficulties.Length; i++)
            {
                var idx = i;
                var btnObj = CreateUIObj($"Btn_{Difficulties[i].name}", _panel.transform);
                btnObj.AddComponent<CanvasRenderer>();
                var btnImg = btnObj.AddComponent<Image>();
                btnImg.color = i == _selectedDifficulty ? selectedColor : btnColor;
                btnImg.raycastTarget = true;
                var btn = btnObj.AddComponent<Button>();
                btn.onClick.AddListener(() => OnDifficultySelected(idx));
                var btnLe = btnObj.AddComponent<LayoutElement>();
                btnLe.preferredHeight = 44;

                var btnTxtObj = CreateUIObj("Text", btnObj.transform);
                var btnTxtRt = btnTxtObj.GetComponent<RectTransform>();
                btnTxtRt.anchorMin = Vector2.zero;
                btnTxtRt.anchorMax = Vector2.one;
                btnTxtRt.offsetMin = Vector2.zero;
                btnTxtRt.offsetMax = Vector2.zero;
                var btnTxt = btnTxtObj.AddComponent<TextMeshProUGUI>();
                btnTxt.text = $"{Difficulties[i].name} (Depth {Difficulties[i].depth})";
                btnTxt.fontSize = 16;
                btnTxt.alignment = TextAlignmentOptions.Center;
                btnTxt.color = Color.white;
                btnTxt.raycastTarget = false;
            }

            var backObj = CreateUIObj("BackBtn", _panel.transform);
            backObj.AddComponent<CanvasRenderer>();
            var backImg = backObj.AddComponent<Image>();
            backImg.color = new Color(0.5f, 0.2f, 0.2f);
            backImg.raycastTarget = true;
            var backBtn = backObj.AddComponent<Button>();
            backBtn.onClick.AddListener(Hide);
            var backLe = backObj.AddComponent<LayoutElement>();
            backLe.preferredHeight = 36;
            var backTxtObj = CreateUIObj("Text", backObj.transform);
            var backTxtRt = backTxtObj.GetComponent<RectTransform>();
            backTxtRt.anchorMin = Vector2.zero;
            backTxtRt.anchorMax = Vector2.one;
            backTxtRt.offsetMin = Vector2.zero;
            backTxtRt.offsetMax = Vector2.zero;
            var backTxt = backTxtObj.AddComponent<TextMeshProUGUI>();
            backTxt.text = "Back";
            backTxt.fontSize = 15;
            backTxt.alignment = TextAlignmentOptions.Center;
            backTxt.color = Color.white;
            backTxt.raycastTarget = false;

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

        private void OnDifficultySelected(int index)
        {
            _selectedDifficulty = index;
            player?.StartRobotGameWithDifficulty(Difficulties[index].depth);
        }

        public void Show()
        {
            EnsurePanel();
            if (_panel != null) _panel.SetActive(true);
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
        }
    }
}
