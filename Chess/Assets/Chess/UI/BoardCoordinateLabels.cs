using TMPro;
using UnityEngine;

namespace Chess.UI
{
    public class BoardCoordinateLabels : MonoBehaviour
    {
        public GameObject board;
        public float labelOffset = 0.4f;
        public float labelY = 0.15f;
        public int fontSize = 10;
        public Color labelColor = new Color(0.85f, 0.85f, 0.85f, 0.9f);

        private void Start()
        {
            GenerateLabels();
        }

        private void GenerateLabels()
        {
            var parent = board != null ? board.transform : transform;

            string[] files = { "a", "b", "c", "d", "e", "f", "g", "h" };
            string[] ranks = { "1", "2", "3", "4", "5", "6", "7", "8" };

            for (int i = 0; i < 8; i++)
            {
                CreateLabel($"File_{files[i]}", new Vector3(i, labelY, -labelOffset), files[i], parent);
                CreateLabel($"FileB_{files[i]}", new Vector3(i, labelY, 7 + labelOffset), files[i], parent);

                CreateLabel($"Rank_{ranks[i]}", new Vector3(-labelOffset, labelY, i), ranks[i], parent);
                CreateLabel($"RankR_{ranks[i]}", new Vector3(7 + labelOffset, labelY, i), ranks[i], parent);
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
            tmp.sortingOrder = 10;

            tmp.renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tmp.renderer.receiveShadows = false;
        }
    }
}
