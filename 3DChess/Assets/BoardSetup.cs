using UnityEngine;

// ============================================================
// 棋盘初始化 —— 在棋盘上摆放 32 颗棋子
// ============================================================
public class BoardSetup : MonoBehaviour
{
    [Header("══════ 棋子预制体 ══════")]
    [Tooltip("前 8 个为后排（从左到右：车马象后王象马车），第 9 个为兵")]
    public GameObject[] whitePieces;
    public GameObject[] blackPieces;

    [Header("══════ 摆放参数 ══════")]
    public float pieceHeightOffset = 0.1f;
    public Vector3 pieceOffset = Vector3.zero;

    // 后排棋子类型（按 x=0..7 顺序）
    private static readonly PieceType[] BackRowTypes =
    {
        PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Queen,
        PieceType.King, PieceType.Bishop, PieceType.Knight, PieceType.Rook
    };

    private BoardGenerator boardGen;

    // ============================================================
    // 获取升变用的预制体（按兵种 + 颜色返回对应模型）
    // ============================================================
    public GameObject GetPromotionPrefab(PieceType type, bool isWhite)
    {
        GameObject[] pieces = isWhite ? whitePieces : blackPieces;
        int idx = type switch
        {
            PieceType.Queen  => 3,
            PieceType.Rook   => 0,
            PieceType.Bishop => 2,
            PieceType.Knight => 1,
            _ => 3
        };
        if (pieces == null || idx >= pieces.Length || pieces[idx] == null)
        {
            Debug.LogError($"找不到升变预制体: {type} ({(isWhite ? "白" : "黑")})");
            return null;
        }
        return pieces[idx];
    }

    void Start()
    {
        boardGen = FindObjectOfType<BoardGenerator>();
        if (boardGen == null)
        {
            Debug.LogError("场景中未找到 BoardGenerator！");
            return;
        }

        float tsX = (boardGen.boardMaxX - boardGen.boardMinX) / 8f;
        float tsZ = (boardGen.boardMaxZ - boardGen.boardMinZ) / 8f;
        float baseY = boardGen.boardSurfaceY + pieceHeightOffset;

        // 白方
        PlaceBackRow(whitePieces, 0, true,  tsX, tsZ, baseY);
        PlacePawns(whitePieces[8], 1, true,  tsX, tsZ, baseY);

        // 黑方
        PlaceBackRow(blackPieces, 7, false, tsX, tsZ, baseY);
        PlacePawns(blackPieces[8], 6, false, tsX, tsZ, baseY);

        // 通知 GameManager 初始化棋盘数据
        if (GameManager.Instance != null)
            GameManager.Instance.InitBoard();
        else
            Debug.LogError("GameManager.Instance 为空！");
    }

    /// 摆放后排（车马象后王象马车）
    void PlaceBackRow(GameObject[] prefabs, int rowZ, bool isWhite,
                      float tsX, float tsZ, float y)
    {
        for (int x = 0; x < 8; x++)
        {
            float px = boardGen.boardMinX + (x + 0.5f) * tsX + pieceOffset.x;
            float pz = boardGen.boardMinZ + (rowZ + 0.5f) * tsZ + pieceOffset.z;
            Vector3 pos = new Vector3(px, y + pieceOffset.y, pz);

            // prefabs[x] 对应棋盘的 x 列
            GameObject obj = Instantiate(prefabs[x], pos,
                                         Quaternion.identity, transform);
            ChessPiece cp = obj.GetComponent<ChessPiece>();
            if (cp != null)
            {
                cp.tileX    = x;
                cp.tileZ    = rowZ;
                cp.isWhite  = isWhite;
                cp.pieceType = BackRowTypes[x];
            }
        }
    }

    /// 摆放一排兵
    void PlacePawns(GameObject pawnPrefab, int rowZ, bool isWhite,
                    float tsX, float tsZ, float y)
    {
        for (int x = 0; x < 8; x++)
        {
            float px = boardGen.boardMinX + (x + 0.5f) * tsX + pieceOffset.x;
            float pz = boardGen.boardMinZ + (rowZ + 0.5f) * tsZ + pieceOffset.z;
            Vector3 pos = new Vector3(px, y + pieceOffset.y, pz);

            GameObject obj = Instantiate(pawnPrefab, pos,
                                         Quaternion.identity, transform);
            ChessPiece cp = obj.GetComponent<ChessPiece>();
            if (cp != null)
            {
                cp.tileX    = x;
                cp.tileZ    = rowZ;
                cp.isWhite  = isWhite;
                cp.pieceType = PieceType.Pawn;
            }
        }
    }
}
