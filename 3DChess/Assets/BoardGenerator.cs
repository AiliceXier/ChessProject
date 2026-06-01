using UnityEngine;

// ============================================================
// 棋盘生成器 —— 8×8 格子 + 四边坐标标签
// ============================================================
public class BoardGenerator : MonoBehaviour
{
    [Header("══════ 棋盘边界 ══════")]
    public float boardMinX = 0.21f;
    public float boardMaxX = 9.179f;
    public float boardMinZ = -0.5f;
    public float boardMaxZ = 8.488f;
    public float boardSurfaceY = -0.69f;

    [Header("══════ 格子设置 ══════")]
    public float gridYOffset = 0.01f;
    public Color blackTileColor = new Color(0.15f, 0.15f, 0.15f);
    public Color whiteTileColor = new Color(0.88f, 0.88f, 0.88f);

    [Header("══════ 坐标标签 ══════")]
    public bool showLabels = true;
    public float labelYOffset = 0.02f;       // 标签高度（棋盘表面上方）
    public float labelMargin = 0.55f;         // 标签离棋盘边缘的距离
    public float labelCharSize = 0.30f;       // 文字大致高度（世界单位）
    public Color labelColor = new Color(0.7f, 0.7f, 0.7f);

    // 四边标签开关
    public bool showBottomLabels = true;  // a-h（白方一侧）
    public bool showTopLabels    = true;  // a-h（黑方一侧）
    public bool showLeftLabels   = true;  // 1-8（左侧）
    public bool showRightLabels  = true;  // 1-8（右侧）

    // 存储 TileSize 供外部使用
    [HideInInspector] public float tileSizeX;
    [HideInInspector] public float tileSizeZ;

    void Awake()
    {
        tileSizeX = (boardMaxX - boardMinX) / 8f;
        tileSizeZ = (boardMaxZ - boardMinZ) / 8f;
        float gridY = boardSurfaceY + gridYOffset;

        // ── 生成 64 个格子 ──
        for (int x = 0; x < 8; x++)
        {
            for (int z = 0; z < 8; z++)
            {
                float px = boardMinX + (x + 0.5f) * tileSizeX;
                float pz = boardMinZ + (z + 0.5f) * tileSizeZ;
                Vector3 pos = new Vector3(px, gridY, pz);

                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                tile.name = $"Tile_{x}_{z}";
                tile.transform.position = pos;
                tile.transform.rotation = Quaternion.Euler(90, 0, 0);
                tile.transform.localScale = new Vector3(tileSizeX, tileSizeZ, 1);

                Material mat = new Material(Shader.Find("Standard"));
                mat.color = (x + z) % 2 == 0 ? blackTileColor : whiteTileColor;
                tile.GetComponent<Renderer>().material = mat;

                // 添加 BoxCollider 用于鼠标点击检测
                BoxCollider col = tile.AddComponent<BoxCollider>();
                col.size = new Vector3(1, 1, 0.1f);

                // 添加点击处理脚本
                TileClickHandler handler = tile.AddComponent<TileClickHandler>();
                handler.tileX = x;
                handler.tileZ = z;

                tile.transform.parent = transform;
            }
        }

        // ── 生成四边坐标标签 ──
        if (showLabels)
            GenerateLabels(tileSizeX, tileSizeZ, gridY + labelYOffset);
    }

    // ============================================================
    // 四边坐标标签
    // ============================================================
    void GenerateLabels(float tsX, float tsZ, float yPos)
    {
        // ── 底部：a b c d e f g h（白方视角）──
        if (showBottomLabels)
        {
            float z = boardMinZ - labelMargin;
            for (int x = 0; x < 8; x++)
            {
                float px = boardMinX + (x + 0.5f) * tsX;
                CreateLabel(((char)('a' + x)).ToString(),
                            px, yPos, z);
            }
        }

        // ── 顶部：a b c d e f g h（黑方视角）──
        if (showTopLabels)
        {
            float z = boardMaxZ + labelMargin;
            for (int x = 0; x < 8; x++)
            {
                float px = boardMinX + (x + 0.5f) * tsX;
                CreateLabel(((char)('a' + x)).ToString(),
                            px, yPos, z);
            }
        }

        // ── 左侧：1 2 3 4 5 6 7 8 ──
        if (showLeftLabels)
        {
            float x = boardMinX - labelMargin;
            for (int zPos = 0; zPos < 8; zPos++)
            {
                float pz = boardMinZ + (zPos + 0.5f) * tsZ;
                CreateLabel((zPos + 1).ToString(),
                            x, yPos, pz);
            }
        }

        // ── 右侧：1 2 3 4 5 6 7 8 ──
        if (showRightLabels)
        {
            float x = boardMaxX + labelMargin;
            for (int zPos = 0; zPos < 8; zPos++)
            {
                float pz = boardMinZ + (zPos + 0.5f) * tsZ;
                CreateLabel((zPos + 1).ToString(),
                            x, yPos, pz);
            }
        }
    }

    // ============================================================
    // 创建单个 3D 文字标签
    // ============================================================
    void CreateLabel(string text, float x, float y, float z)
    {
        GameObject labelObj = new GameObject("Label_" + text + "_" +
            (x < boardMinX ? "L" : x > boardMaxX ? "R" : z < boardMinZ ? "B" : "T"));
        labelObj.transform.parent = transform;
        labelObj.transform.position = new Vector3(x, y, z);

        // 平躺，面朝上（和棋盘表面平行）
        labelObj.transform.rotation = Quaternion.Euler(90, 0, 0);

        TextMesh tm = labelObj.AddComponent<TextMesh>();
        tm.text = text;
        tm.font = GetLabelFont();

        // 使用 fontSize（不用 characterSize），保证高质量渲染
        tm.fontSize = 128;
        tm.color = labelColor;
        tm.anchor = TextAnchor.MiddleCenter;

        // fontSize 128 在 scale=1 时字符约 0.04 单位高
        // labelCharSize / 0.04 使得参数 ≈ 字符的世界单位高度
        float scale = labelCharSize / 0.04f;
        labelObj.transform.localScale = new Vector3(scale, scale, 1f);

        // 确保有 MeshRenderer
        MeshRenderer mr = labelObj.GetComponent<MeshRenderer>();
        if (mr == null)
            mr = labelObj.AddComponent<MeshRenderer>();
    }

    // ============================================================
    // 加载可用于 3D TextMesh 的字体（高分辨率）
    // ============================================================
    Font GetLabelFont()
    {
        // 方式1：Unity 内置字体
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f != null) return f;

        // 方式2：系统 Arial（128pt 保证高质量，否则显示为一条线）
        f = Font.CreateDynamicFontFromOSFont("Arial", 128);
        if (f != null) return f;

        // 方式3：降级
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
