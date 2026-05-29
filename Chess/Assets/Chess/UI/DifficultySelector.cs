using Chess;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.UI
{
    public class DifficultySelector : MonoBehaviour
    {
        public Player player;

        public Color panelColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        public Color btnColor = new Color(0.2f, 0.25f, 0.35f, 1f);
        public Color selectedColor = new Color(0.2f, 0.5f, 0.2f, 1f);

        private GameObject _panel;
        private int _selectedDifficulty = 3;
        private bool _visible;

        private static readonly (string name, int depth)[] Difficulties =
        {
            ("简单", 1),
            ("中等", 3),
            ("困难", 4),
            ("大师", 5)
        };

        public int SelectedDepth => Difficulties[_selectedDifficulty].depth;

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

            _panel = new GameObject("DifficultyPanel");
            _panel.transform.SetParent(canvas.transform, false);
            _panel.layer = 5;

            var panelRt = _panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.offsetMin = new Vector2(-150, -120);
            panelRt.offsetMax = new Vector2(150, 120);

            _panel.AddComponent<CanvasRenderer>();
            var bg = _panel.AddComponent<Image>();
            bg.color = panelColor;

            var vlg = _panel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 8;
            vlg.padding = new RectOffset(16, 16, 16, 16);

            var titleObj = CreateUIObj("Title", _panel.transform);
            var titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "选择AI难度";
            titleTxt.fontSize = 20;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = Color.white;
            var titleLe = titleObj.AddComponent<LayoutElement>();
            titleLe.preferredHeight = 36;

            for (int i = 0; i < Difficulties.Length; i++)
            {
                var idx = i;
                var btnObj = CreateUIObj($"Btn_{Difficulties[i].name}", _panel.transform);
                btnObj.AddComponent<CanvasRenderer>();
                var btnImg = btnObj.AddComponent<Image>();
                btnImg.color = i == 1 ? selectedColor : btnColor;
                var btn = btnObj.AddComponent<Button>();
                btn.onClick.AddListener(() => OnDifficultySelected(idx));
                var btnLe = btnObj.AddComponent<LayoutElement>();
                btnLe.preferredHeight = 40;

                var btnTxt = btnObj.AddComponent<TextMeshProUGUI>();
                btnTxt.text = $"{Difficulties[i].name} (深度 {Difficulties[i].depth})";
                btnTxt.fontSize = 16;
                btnTxt.alignment = TextAlignmentOptions.Center;
                btnTxt.color = Color.white;
            }
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

            if (player != null)
            {
                player.StartRobotGameWithDifficulty(Difficulties[index].depth);
            }

            Hide();
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
