using System;
using Chess;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.UI
{
    public class EvaluationBar : MonoBehaviour
    {
        public Player player;

        public Color whiteColor = new Color(0.95f, 0.95f, 0.9f);
        public Color blackColor = new Color(0.15f, 0.15f, 0.15f);
        public Color neutralColor = new Color(0.5f, 0.5f, 0.5f);

        private Image _barImage;
        private TMP_Text _evalText;
        private RectTransform _barFill;
        private ChessAI _evalAI;

        private void Awake()
        {
            BuildUI();
            _evalAI = new ChessAI(maxDepth: 2);
        }

        private void BuildUI()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            var container = new GameObject("EvalBar");
            container.transform.SetParent(canvas.transform, false);
            container.layer = 5;

            var containerRt = container.AddComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0f, 0.3f);
            containerRt.anchorMax = new Vector2(0f, 0.7f);
            containerRt.offsetMin = new Vector2(8, 0);
            containerRt.offsetMax = new Vector2(28, 0);

            container.AddComponent<CanvasRenderer>();
            var bgImg = container.AddComponent<Image>();
            bgImg.color = blackColor;

            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(container.transform, false);
            fillObj.layer = 5;
            var fillRt = fillObj.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0.5f);
            fillRt.anchorMax = new Vector2(1f, 0.5f);
            fillRt.offsetMin = new Vector2(0, -10);
            fillRt.offsetMax = new Vector2(0, 10);
            fillObj.AddComponent<CanvasRenderer>();
            _barFill = fillRt;
            _barImage = fillObj.AddComponent<Image>();
            _barImage.color = whiteColor;

            var textObj = new GameObject("EvalText");
            textObj.transform.SetParent(container.transform, false);
            textObj.layer = 5;
            var textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            _evalText = textObj.AddComponent<TextMeshProUGUI>();
            _evalText.fontSize = 10;
            _evalText.fontStyle = FontStyles.Bold;
            _evalText.alignment = TextAlignmentOptions.Center;
            _evalText.color = blackColor;

            UpdateDisplay(0);
        }

        public void UpdateEvaluation(ChessBoard board)
        {
            if (board == null || _evalAI == null) return;

            var eval = _evalAI.EvaluatePosition(board);
            UpdateDisplay(eval);
        }

        private void UpdateDisplay(double eval)
        {
            var normalized = Mathf.Clamp((float)eval / 2000f, -1f, 1f);

            var fillMin = 0.5f;
            var fillMax = 0.5f;

            if (normalized >= 0)
            {
                fillMin = 0.5f;
                fillMax = 0.5f + normalized * 0.5f;
            }
            else
            {
                fillMin = 0.5f + normalized * 0.5f;
                fillMax = 0.5f;
            }

            if (_barFill != null)
            {
                _barFill.anchorMin = new Vector2(0f, fillMin);
                _barFill.anchorMax = new Vector2(1f, fillMax);
                _barFill.offsetMin = Vector2.zero;
                _barFill.offsetMax = Vector2.zero;
            }

            if (_evalText != null)
            {
                var pawns = eval / 100.0;
                if (Math.Abs(pawns) >= 100)
                    _evalText.text = pawns > 0 ? "+M" : "-M";
                else
                    _evalText.text = pawns >= 0 ? $"+{pawns:F1}" : $"{pawns:F1}";

                _evalText.color = normalized >= 0 ? blackColor : whiteColor;
            }
        }
    }
}
