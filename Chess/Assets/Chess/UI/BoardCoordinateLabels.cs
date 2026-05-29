using TMPro;
using UnityEngine;

namespace Chess.UI
{
    public class BoardCoordinateLabels : MonoBehaviour
    {
        public GameObject boardPivot;
        public float labelOffset = 0.5f;
        public float labelY = 0.05f;
        public int fontSize = 24;
        public Color labelColor = new Color(0.85f, 0.85f, 0.85f, 0.9f);

        private void Start()
        {
            GenerateLabels();
        }

        private void GenerateLabels()
        {
            var pivot = boardPivot != null ? boardPivot.transform : transform;

            string[] files = { "a", "b", "c", "d", "e", "f", "g", "h" };
            string[] ranks = { "1", "2", "3", "4", "5", "6", "7", "8" };

            for (int i = 0; i < 8; i++)
            {
                CreateLabel($"File_{files[i]}", new Vector3(i, labelY, -labelOffset), files[i], pivot);
                CreateLabel($"FileB_{files[i]}", new Vector3(i, labelY, 8 + labelOffset), files[i], pivot);

                CreateLabel($"Rank_{ranks[i]}", new Vector3(-labelOffset, labelY, i), ranks[i], pivot);
                CreateLabel($"RankR_{ranks[i]}", new Vector3(8 + labelOffset, labelY, i), ranks[i], pivot);
            }
        }

        private void CreateLabel(string name, Vector3 localPos, string text, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = Quaternion.Euler(90, 0, 0);

            var tmp = obj.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = labelColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            tmp.enableAutoSizing = false;
            tmp.fontSizeMin = fontSize;
            tmp.fontSizeMax = fontSize;

            tmp.renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tmp.renderer.receiveShadows = false;
        }
    }
}
