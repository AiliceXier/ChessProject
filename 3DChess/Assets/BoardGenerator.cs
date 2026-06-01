using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    [Header("══════ 棋盘边界（你的正确值）══════")]
    public float boardMinX = 0.21f;
    public float boardMaxX = 9.179f;
    public float boardMinZ = -0.5f;
    public float boardMaxZ = 8.488f;
    public float boardSurfaceY = -0.69f;

    [Header("══════ 格子设置 ══════")]
    public float gridYOffset = 0.01f;        // 格子比表面高一点点

    [Header("══════ 坐标标签 ══════")]
    public bool showLabels = true;
    public float labelYOffset = 0.02f;       // 标签比格子再高一点
    public float bottomMargin = 0.5f;        // 字母离棋盘下边缘距离
    public float leftMargin = 0.5f;          // 数字离棋盘左边缘距离
    public float labelCharSize = 0.06f;      // 文字尺寸（调小变精致）
    public Color labelColor = Color.white;

    void Awake()
    {
        float tileSizeX = (boardMaxX - boardMinX) / 8;
        float tileSizeZ = (boardMaxZ - boardMinZ) / 8;
        float gridY = boardSurfaceY + gridYOffset;

        // 生成 64 个黑白格子
        for (int x = 0; x < 8; x++)
        {
            for (int z = 0; z < 8; z++)
            {
                float posX = boardMinX + (x + 0.5f) * tileSizeX;
                float posZ = boardMinZ + (z + 0.5f) * tileSizeZ;
                Vector3 pos = new Vector3(posX, gridY, posZ);

                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                tile.name = $"Tile_{x}_{z}";
                tile.transform.position = pos;
                tile.transform.rotation = Quaternion.Euler(90, 0, 0);
                tile.transform.localScale = new Vector3(tileSizeX, tileSizeZ, 1);

                Material mat = new Material(Shader.Find("Standard"));
                mat.color = (x + z) % 2 == 0 ? Color.black : Color.white;
                tile.GetComponent<Renderer>().material = mat;

                BoxCollider col = tile.AddComponent<BoxCollider>();
                col.size = new Vector3(1, 1, 0.1f);

                TileClickHandler handler = tile.AddComponent<TileClickHandler>();
                handler.tileX = x;
                handler.tileZ = z;

                tile.transform.parent = transform;
            }
        }

        // 生成坐标标签
        if (showLabels)
            GenerateLabels(tileSizeX, tileSizeZ, gridY + labelYOffset);
    }

    void GenerateLabels(float tileSizeX, float tileSizeZ, float yPos)
    {
        // 底部字母 a~h
        for (int x = 0; x < 8; x++)
        {
            float posX = boardMinX + (x + 0.5f) * tileSizeX;
            float posZ = boardMinZ - bottomMargin;   // 下方
            CreateLabel(((char)('a' + x)).ToString(), posX, yPos, posZ);
        }

        // 左侧数字 1~8
        for (int z = 0; z < 8; z++)
        {
            float posX = boardMinX - leftMargin;     // 左侧
            float posZ = boardMinZ + (z + 0.5f) * tileSizeZ;
            CreateLabel((z + 1).ToString(), posX, yPos, posZ);
        }
    }

    void CreateLabel(string text, float x, float y, float z)
    {
        GameObject labelObj = new GameObject("Label_" + text);
        labelObj.transform.parent = transform;
        labelObj.transform.position = new Vector3(x, y, z);
        // 平躺，面朝上（和棋盘平行）
        labelObj.transform.rotation = Quaternion.Euler(90, 0, 0);

        TextMesh tm = labelObj.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 0;                  // 禁用内置字体大小，完全由 characterSize 控制
        tm.characterSize = labelCharSize;
        tm.color = labelColor;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.lineSpacing = 1.0f;
    }
}