using Chess;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.UI
{
    public class EvaluationBar : MonoBehaviour
    {
        public Player player;

        private GameObject _barObj;
        private Image _whiteImg;
        private TextMeshProUGUI _evalText;
        private ChessAI _chessAI;

        private void Awake() { }

        private void EnsureBar()
        {
            if (_barObj != null) return;
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;
            BuildBar(canvas.transform);
        }

        private void BuildBar(Transform canvasTr)
        {
            _barObj = new GameObject("EvalBar");
            _barObj.transform.SetParent(canvasTr, false);
            _barObj.layer = 5;
            _barObj.SetActive(false);

            var barRt = _barObj.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0.1f);
            barRt.anchorMax = new Vector2(0f, 0.9f);
            barRt.offsetMin = new Vector2(4, 0);
            barRt.offsetMax = new Vector2(24, 0);

            _barObj.AddComponent<CanvasRenderer>();
            var bgImg = _barObj.AddComponent<Image>();
            bgImg.color = Color.black;
            bgImg.raycastTarget = false;

            var whiteObj = new GameObject("WhiteBar");
            whiteObj.transform.SetParent(_barObj.transform, false);
            whiteObj.layer = 5;
            var whiteRt = whiteObj.AddComponent<RectTransform>();
            whiteRt.anchorMin = new Vector2(0f, 0f);
            whiteRt.anchorMax = new Vector2(1f, 0.5f);
            whiteRt.offsetMin = Vector2.zero;
            whiteRt.offsetMax = Vector2.zero;
            whiteObj.AddComponent<CanvasRenderer>();
            _whiteImg = whiteObj.AddComponent<Image>();
            _whiteImg.color = Color.white;
            _whiteImg.raycastTarget = false;

            var textObj = new GameObject("EvalText");
            textObj.transform.SetParent(_barObj.transform, false);
            textObj.layer = 5;
            var textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            _evalText = textObj.AddComponent<TextMeshProUGUI>();
            _evalText.fontSize = 10;
            _evalText.alignment = TextAlignmentOptions.Center;
            _evalText.color = Color.gray;
            _evalText.raycastTarget = false;
        }

        private void Update()
        {
            if (_barObj == null || !_barObj.activeSelf) return;
            if (player == null) return;
            var board = player.GetLocalBoard();
            if (board == null) return;

            if (_chessAI == null)
                _chessAI = new ChessAI(maxDepth: 3);

            float eval = _chessAI.EvaluatePosition(board) / 100f;
            float ratio = Mathf.Clamp(0.5f + eval * 0.05f, 0.05f, 0.95f);

            if (_whiteImg != null)
            {
                var rt = _whiteImg.rectTransform;
                rt.anchorMax = new Vector2(1f, ratio);
            }

            if (_evalText != null)
            {
                _evalText.text = eval >= 0 ? $"+{eval:F1}" : $"{eval:F1}";
                _evalText.color = ratio > 0.5f ? Color.black : Color.white;
            }
        }

        public void Show()
        {
            EnsureBar();
            if (_barObj != null) _barObj.SetActive(true);
        }

        public void Hide()
        {
            if (_barObj != null) _barObj.SetActive(false);
        }
    }
}
