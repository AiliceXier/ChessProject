using Chess;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.UI
{
    public class HintSystem : MonoBehaviour
    {
        public Player player;

        public GameObject hintBtnRef;

        private GameObject _hintBtn;
        private GameObject _highlight;
        private float _clearTime;

        private void Awake() { }

        private void EnsureButton()
        {
            if (_hintBtn != null) return;

            if (hintBtnRef != null)
            {
                _hintBtn = hintBtnRef;
                var btn = _hintBtn.GetComponent<Button>();
                if (btn == null)
                    btn = _hintBtn.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(ShowHint);
                return;
            }

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;
            BuildButton(canvas.transform);
        }

        private void BuildButton(Transform canvasTr)
        {
            _hintBtn = new GameObject("HintBtn");
            _hintBtn.transform.SetParent(canvasTr, false);
            _hintBtn.layer = 5;

            var btnRt = _hintBtn.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0f, 0f);
            btnRt.anchorMax = new Vector2(0f, 0f);
            btnRt.offsetMin = new Vector2(10, 52);
            btnRt.offsetMax = new Vector2(110, 86);

            _hintBtn.AddComponent<CanvasRenderer>();
            var btnImg = _hintBtn.AddComponent<Image>();
            btnImg.color = new Color(0.29f, 0.48f, 0.71f, 0.9f);
            btnImg.raycastTarget = true;

            var btn = _hintBtn.AddComponent<Button>();
            btn.onClick.AddListener(ShowHint);

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(_hintBtn.transform, false);
            txtObj.layer = 5;
            txtObj.AddComponent<RectTransform>();
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "Hint";
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private void Update()
        {
            if (_highlight != null && _highlight.activeSelf && Time.time > _clearTime)
                _highlight.SetActive(false);
        }

        private void ShowHint()
        {
            if (player == null) return;
            var board = player.GetLocalBoard();
            if (board == null) return;

            var ai = new ChessAI(maxDepth: 3);
            var bestMove = ai.GetBestMove(board);
            if (bestMove == null) return;

            ShowHighlight(bestMove.OriginalPosition.X, bestMove.OriginalPosition.Y);
            _clearTime = Time.time + 3f;
        }

        private void ShowHighlight(int x, int y)
        {
            if (_highlight == null)
            {
                _highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _highlight.name = "HintHighlight";
                Object.Destroy(_highlight.GetComponent<Collider>());
                var mr = _highlight.GetComponent<MeshRenderer>();
                mr.material = new Material(Shader.Find("Unlit/Transparent"));
                var c = new Color(0.38f, 0.60f, 0.14f, 0.5f);
                mr.material.color = c;
            }

            var boardObj = GameObject.Find("Board");
            if (boardObj == null) return;

            _highlight.transform.SetParent(boardObj.transform, false);
            _highlight.transform.localRotation = Quaternion.Euler(90, 0, 0);
            _highlight.transform.localPosition = new Vector3(x, 0.02f, y);
            _highlight.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            _highlight.SetActive(true);
        }

        public void ShowButton()
        {
            EnsureButton();
            if (_hintBtn != null) _hintBtn.SetActive(true);
        }

        public void HideButton()
        {
            if (_hintBtn != null) _hintBtn.SetActive(false);
            if (_highlight != null) _highlight.SetActive(false);
        }
    }
}
