using System.Collections;
using Chess;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.UI
{
    public class HintSystem : MonoBehaviour
    {
        public Player player;
        public GameObject boardPivot;

        public Color hintFromColor = new Color(0.2f, 0.8f, 0.2f, 0.4f);
        public Color hintToColor = new Color(0.9f, 0.9f, 0.2f, 0.5f);
        public float highlightDuration = 3f;

        private GameObject _fromHighlight;
        private GameObject _toHighlight;
        private Coroutine _hintCoroutine;
        private ChessAI _hintAI;

        private void Awake()
        {
            _hintAI = new ChessAI(maxDepth: 3);
            BuildUI();
        }

        private void BuildUI()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            var btnObj = new GameObject("HintBtn");
            btnObj.transform.SetParent(canvas.transform, false);
            btnObj.layer = 5;

            var btnRt = btnObj.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0f, 0f);
            btnRt.anchorMax = new Vector2(0f, 0f);
            btnRt.offsetMin = new Vector2(10, 10);
            btnRt.offsetMax = new Vector2(110, 44);

            btnObj.AddComponent<CanvasRenderer>();
            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.4f, 0.2f, 0.9f);
            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(ShowHint);

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            txtObj.layer = 5;
            txtObj.AddComponent<RectTransform>();
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "走法提示";
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        public void ShowHint()
        {
            var board = player?.GetLocalBoard();
            if (board == null || board.IsEndGame) return;

            var bestMove = _hintAI.GetBestMove(board);
            if (bestMove == null) return;

            ClearHighlights();

            var pivot = boardPivot != null ? boardPivot.transform : transform;

            _fromHighlight = CreateHighlight("HintFrom", bestMove.OriginalPosition, hintFromColor, pivot);
            _toHighlight = CreateHighlight("HintTo", bestMove.NewPosition, hintToColor, pivot);

            if (_hintCoroutine != null)
                StopCoroutine(_hintCoroutine);
            _hintCoroutine = StartCoroutine(AutoClearHighlights());
        }

        private GameObject CreateHighlight(string name, Position pos, Color color, Transform parent)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(pos.X, 0.02f, pos.Y);
            obj.transform.localRotation = Quaternion.Euler(90, 0, 0);
            obj.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

            var renderer = obj.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Unlit/Transparent"));
            mat.color = color;
            renderer.material = mat;

            obj.GetComponent<Collider>().enabled = false;

            return obj;
        }

        private IEnumerator AutoClearHighlights()
        {
            yield return new WaitForSeconds(highlightDuration);
            ClearHighlights();
            _hintCoroutine = null;
        }

        private void ClearHighlights()
        {
            if (_fromHighlight != null)
            {
                Destroy(_fromHighlight);
                _fromHighlight = null;
            }
            if (_toHighlight != null)
            {
                Destroy(_toHighlight);
                _toHighlight = null;
            }
        }
    }
}
