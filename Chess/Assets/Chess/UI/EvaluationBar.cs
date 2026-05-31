using Chess;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.UI
{
    public class EvaluationBar : MonoBehaviour
    {
        public Player player;

        public GameObject evalBarRef;

        private GameObject _barObj;
        private Image _whiteImg;
        private Image _blackImg;
        private TextMeshProUGUI _evalText;
        private ChessAI _chessAI;
        private float _currentRatio = 0.5f;
        private float _targetRatio = 0.5f;

        private static readonly Color WhiteBarColor = new Color(0.94f, 0.94f, 0.94f);
        private static readonly Color BlackBarColor = new Color(0.19f, 0.19f, 0.19f);

        private void Awake() { }

        private void EnsureBar()
        {
            if (_barObj != null) return;

            if (evalBarRef != null)
            {
                _barObj = evalBarRef;
                _blackImg = _barObj.GetComponent<Image>();
                var whiteTr = _barObj.transform.Find("WhiteBar");
                if (whiteTr != null) _whiteImg = whiteTr.GetComponent<Image>();
                var evalTextTr = _barObj.transform.Find("EvalText");
                if (evalTextTr != null) _evalText = evalTextTr.GetComponent<TextMeshProUGUI>();
                _barObj.SetActive(false);
                return;
            }

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;
            BuildBar(canvas.transform);
        }

        private void BuildBar(Transform canvasTr)
        {
            _barObj = new GameObject("EvalBar");
            _barObj.transform.SetParent(canvasTr, false);
            _barObj.layer = 5;

            var barRt = _barObj.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0.1f);
            barRt.anchorMax = new Vector2(0f, 0.9f);
            barRt.offsetMin = new Vector2(4, 0);
            barRt.offsetMax = new Vector2(26, 0);

            _barObj.AddComponent<CanvasRenderer>();
            var bgImg = _barObj.AddComponent<Image>();
            bgImg.color = BlackBarColor;
            bgImg.raycastTarget = false;
            _blackImg = bgImg;

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
            _whiteImg.color = WhiteBarColor;
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
            _evalText.fontSize = 11;
            _evalText.fontStyle = FontStyles.Bold;
            _evalText.alignment = TextAlignmentOptions.Center;
            _evalText.color = Color.gray;
            _evalText.raycastTarget = false;

            _barObj.SetActive(false);
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
            _targetRatio = Mathf.Clamp(1f / (1f + Mathf.Exp(-eval * 0.4f)), 0.05f, 0.95f);

            _currentRatio = Mathf.Lerp(_currentRatio, _targetRatio, Time.deltaTime * 5f);

            if (_whiteImg != null)
            {
                var rt = _whiteImg.rectTransform;
                rt.anchorMax = new Vector2(1f, _currentRatio);
            }

            if (_evalText != null)
            {
                _evalText.text = eval >= 0 ? $"+{eval:F1}" : $"{eval:F1}";
                _evalText.color = _currentRatio > 0.5f ? new Color(0.15f, 0.15f, 0.15f) : new Color(0.85f, 0.85f, 0.85f);
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
