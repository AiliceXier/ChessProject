using System.Collections.Generic;
using UnityEngine;

// ============================================================
// 游戏管理器 —— 回合控制、走法执行、胜负判定、升变处理
// ============================================================
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("══════ 选中 / 高亮 ══════")]
    public Material selectedMaterial;
    public GameObject highlightPrefab;
    public float highlightYOffset = 0.02f;
    public float highlightOffsetX = 0f;
    public float highlightOffsetZ = 0f;

    // ── 内部状态 ──
    private Board board;
    private GameState gameState = GameState.Playing;
    private Player currentTurn = Player.White;

    private ChessPiece selectedPiece;
    private List<Move> currentLegalMoves = new List<Move>();
    private List<GameObject> highlightObjects = new List<GameObject>();
    private Dictionary<ChessPiece, Material> originalMaterials =
                                    new Dictionary<ChessPiece, Material>();

    private BoardGenerator boardGen;
    private BoardSetup boardSetup;
    private PromotionUI promotionUI;
    private GameOverUI gameOverUI;

    // ── 升变相关 ──
    private Move pendingMove;
    private bool waitingForPromotion;

    // ============================================================
    // 生命周期
    // ============================================================
    void Awake()
    {
        Instance = this;
        board = new Board();

        // 自动创建升变 UI
        GameObject uiObj = new GameObject("PromotionUI");
        uiObj.transform.SetParent(transform);
        promotionUI = uiObj.AddComponent<PromotionUI>();
        promotionUI.OnPieceSelected += OnPromotionSelected;

        // 自动创建游戏结束 UI
        GameObject goObj = new GameObject("GameOverUI");
        goObj.transform.SetParent(transform);
        gameOverUI = goObj.AddComponent<GameOverUI>();
    }

    /// 由 BoardSetup 在摆放完棋子后调用
    public void InitBoard()
    {
        boardGen   = FindObjectOfType<BoardGenerator>();
        boardSetup = FindObjectOfType<BoardSetup>();
        if (boardGen == null || boardSetup == null)
        {
            Debug.LogError("缺少 BoardGenerator 或 BoardSetup！");
            return;
        }

        ChessPiece[] allPieces = FindObjectsOfType<ChessPiece>();
        Debug.Log($"找到 {allPieces.Length} 个棋子");

        foreach (ChessPiece cp in allPieces)
        {
            board.SetPiece(cp.tileX, cp.tileZ, cp.pieceType,
                           cp.isWhite ? Player.White : Player.Black);
            board.SetHasMoved(cp.tileX, cp.tileZ, cp.hasMoved);

            Renderer rend = cp.GetComponentInChildren<Renderer>();
            if (rend != null)
                originalMaterials[cp] = rend.material;
        }

        board.ClearEnPassant();
        gameState   = GameState.Playing;
        currentTurn = Player.White;

        PrintBoard();
    }

    // ============================================================
    // 点击：棋子
    // ============================================================
    public void OnPieceClicked(ChessPiece piece)
    {
        if (gameState != GameState.Playing) return;
        if (waitingForPromotion) return;   // 升变选择中，忽略点击

        Player color = piece.isWhite ? Player.White : Player.Black;

        if (selectedPiece != null)
        {
            if (color == currentTurn)
            {
                if (selectedPiece != piece) SelectPiece(piece);
                else DeselectPiece();
                return;
            }

            foreach (Move m in currentLegalMoves)
            {
                if (m.toX == piece.tileX && m.toZ == piece.tileZ)
                {
                    ExecuteMove(m);
                    return;
                }
            }
            return;
        }

        if (color != currentTurn)
        {
            Debug.Log("不是你的回合！");
            return;
        }
        SelectPiece(piece);
    }

    // ============================================================
    // 点击：空格子
    // ============================================================
    public void OnTileClicked(int x, int z)
    {
        if (gameState != GameState.Playing) return;
        if (selectedPiece == null) return;
        if (waitingForPromotion) return;   // 升变选择中，忽略点击

        foreach (Move m in currentLegalMoves)
        {
            if (m.toX == x && m.toZ == z)
            {
                ExecuteMove(m);
                return;
            }
        }
        DeselectPiece();
    }

    // ============================================================
    // 选中 / 取消
    // ============================================================
    void SelectPiece(ChessPiece piece)
    {
        DeselectPiece();
        selectedPiece = piece;

        Renderer rend = piece.GetComponentInChildren<Renderer>();
        if (rend != null && selectedMaterial != null)
            rend.material = selectedMaterial;

        currentLegalMoves = MoveGenerator.GetLegalMoves(
                                board, piece.tileX, piece.tileZ);
        ShowLegalMoves();
    }

    void DeselectPiece()
    {
        if (selectedPiece != null)
        {
            Renderer rend = selectedPiece.GetComponentInChildren<Renderer>();
            if (rend != null
                && originalMaterials.ContainsKey(selectedPiece)
                && originalMaterials[selectedPiece] != null)
                rend.material = originalMaterials[selectedPiece];
            selectedPiece = null;
        }
        ClearHighlights();
        currentLegalMoves.Clear();
    }

    // ============================================================
    // 执行走法（入口）
    // ============================================================
    void ExecuteMove(Move move)
    {
        // ── 检测是否需要升变 ──
        PieceType movingPiece = board.GetPiece(move.fromX, move.fromZ);
        Player    movingColor = board.GetColor(move.fromX, move.fromZ);

        if (movingPiece == PieceType.Pawn)
        {
            int promoRow = (movingColor == Player.White) ? 7 : 0;
            if (move.toZ == promoRow)
            {
                // 弹出升变选择面板
                pendingMove = move;
                waitingForPromotion = true;
                promotionUI.Show();
                return;
            }
        }

        // 不需要升变 → 直接执行
        DoExecuteMove(move);
    }

    // ── 升变选择回调（由 PromotionUI 触发）──
    void OnPromotionSelected(PieceType type)
    {
        if (!waitingForPromotion) return;
        waitingForPromotion = false;

        Move finalMove = pendingMove;
        finalMove.promotion = type;
        DoExecuteMove(finalMove);
    }

    // ============================================================
    // 实际执行走法
    // ============================================================
    void DoExecuteMove(Move move)
    {
        PieceType movingType  = board.GetPiece(move.fromX, move.fromZ);
        Player    movingColor = board.GetColor(move.fromX, move.fromZ);

        // ── 先找到所有涉及的 ChessPiece ──
        ChessPiece movedCp    = FindChessPieceAt(move.fromX, move.fromZ);
        ChessPiece capturedCp = null;
        ChessPiece rookCp     = null;
        int rookToX = -1, rookToZ = -1;

        if (move.isEnPassant)
        {
            capturedCp = FindChessPieceAt(move.toX, move.fromZ);
        }
        else if (!board.IsEmpty(move.toX, move.toZ)
                 && board.GetColor(move.toX, move.toZ) != movingColor)
        {
            capturedCp = FindChessPieceAt(move.toX, move.toZ);
        }

        if (move.isCastling)
        {
            int backRow = move.fromZ;
            if (move.toX == 6)
            {
                rookCp  = FindChessPieceAt(7, backRow);
                rookToX = 5; rookToZ = backRow;
            }
            else if (move.toX == 2)
            {
                rookCp  = FindChessPieceAt(0, backRow);
                rookToX = 3; rookToZ = backRow;
            }
        }

        // ── 更新数据模型 ──
        board.ApplyMove(move);

        // ── 移动己方棋子 ──
        if (movedCp != null)
        {
            movedCp.tileX    = move.toX;
            movedCp.tileZ    = move.toZ;
            movedCp.hasMoved = true;

            // 升变：切换模型 + 更新类型
            if (move.promotion != PieceType.None)
            {
                SwapPieceModel(movedCp, move.promotion);
            }

            MoveGameObject(movedCp.gameObject, move.toX, move.toZ,
                           boardSetup.pieceHeightOffset);
        }

        // ── 移动车（王车易位）──
        if (rookCp != null)
        {
            rookCp.tileX    = rookToX;
            rookCp.tileZ    = rookToZ;
            rookCp.hasMoved = true;
            MoveGameObject(rookCp.gameObject, rookToX, rookToZ,
                           boardSetup.pieceHeightOffset);
        }

        // ── 移除被吃棋子 ──
        if (capturedCp != null)
        {
            originalMaterials.Remove(capturedCp);
            Destroy(capturedCp.gameObject);
        }

        // ── 回合切换 & 胜负检测 ──
        DeselectPiece();
        SwitchTurn();
    }

    // ============================================================
    // 升变模型切换
    // ============================================================
    void SwapPieceModel(ChessPiece cp, PieceType newType)
    {
        GameObject prefab = boardSetup.GetPromotionPrefab(newType, cp.isWhite);
        if (prefab == null)
        {
            // 降级：至少更新类型
            cp.pieceType = newType;
            return;
        }

        // ── 替换 Mesh ──
        MeshFilter prefabMF = prefab.GetComponent<MeshFilter>();
        MeshFilter targetMF = cp.GetComponent<MeshFilter>();
        if (targetMF != null && prefabMF != null)
            targetMF.mesh = prefabMF.sharedMesh;

        // ── 替换 Material ──
        MeshRenderer prefabMR = prefab.GetComponent<MeshRenderer>();
        MeshRenderer targetMR = cp.GetComponent<MeshRenderer>();
        if (targetMR != null && prefabMR != null)
        {
            targetMR.materials = prefabMR.sharedMaterials;
            // 更新材质缓存（用于取消选中时恢复）
            originalMaterials[cp] = targetMR.material;
        }

        // ── 更新数据 ──
        cp.pieceType = newType;

        Debug.Log($"升变：{(cp.isWhite ? "白" : "黑")}兵 → {newType}");
    }

    // ============================================================
    // 回合切换 + 将军 / 将死 / 逼和 检测
    // ============================================================
    void SwitchTurn()
    {
        currentTurn = (currentTurn == Player.White)
                      ? Player.Black : Player.White;
        Player justMoved = (currentTurn == Player.White)
                           ? Player.Black : Player.White;

        bool inCheck  = MoveGenerator.IsInCheck(board, currentTurn);
        bool hasMoves = MoveGenerator.HasLegalMoves(board, currentTurn);

        if (!hasMoves)
        {
            if (inCheck)
            {
                gameState = (currentTurn == Player.White)
                            ? GameState.BlackWon : GameState.WhiteWon;
                Debug.Log($"【将死】{justMoved} 获胜！");
            }
            else
            {
                gameState = GameState.Stalemate;
                Debug.Log("【逼和】无子可动，平局！");
            }
            gameOverUI.Show(gameState);  // ← 弹出游戏结束画面
        }
        else if (inCheck)
        {
            Debug.Log($"【将军】{currentTurn} 的王正被攻击！");
        }

        string status = gameState != GameState.Playing ? "（游戏已结束）" : "";
        Debug.Log($"轮到 {currentTurn} {status}");
    }

    // ============================================================
    // 合法走法高亮
    // ============================================================
    void ShowLegalMoves()
    {
        ClearHighlights();
        float tsX = (boardGen.boardMaxX - boardGen.boardMinX) / 8f;
        float tsZ = (boardGen.boardMaxZ - boardGen.boardMinZ) / 8f;
        float hy  = boardGen.boardSurfaceY + highlightYOffset;
        Vector3 off = boardSetup.pieceOffset;

        foreach (Move m in currentLegalMoves)
        {
            float px = boardGen.boardMinX + (m.toX + 0.5f) * tsX
                       + off.x + highlightOffsetX;
            float pz = boardGen.boardMinZ + (m.toZ + 0.5f) * tsZ
                       + off.z + highlightOffsetZ;
            Vector3 pos = new Vector3(px, hy + off.y, pz);
            GameObject obj = Instantiate(highlightPrefab, pos,
                                         Quaternion.Euler(90, 0, 0));
            highlightObjects.Add(obj);
        }
    }

    void ClearHighlights()
    {
        foreach (GameObject obj in highlightObjects)
            Destroy(obj);
        highlightObjects.Clear();
    }

    // ============================================================
    // 辅助
    // ============================================================
    ChessPiece FindChessPieceAt(int x, int z)
    {
        ChessPiece[] all = FindObjectsOfType<ChessPiece>();
        foreach (ChessPiece cp in all)
            if (cp.tileX == x && cp.tileZ == z)
                return cp;
        return null;
    }

    void MoveGameObject(GameObject obj, int tx, int tz, float hOff)
    {
        float tsX = (boardGen.boardMaxX - boardGen.boardMinX) / 8f;
        float tsZ = (boardGen.boardMaxZ - boardGen.boardMinZ) / 8f;
        float px = boardGen.boardMinX + (tx + 0.5f) * tsX
                   + boardSetup.pieceOffset.x;
        float pz = boardGen.boardMinZ + (tz + 0.5f) * tsZ
                   + boardSetup.pieceOffset.z;
        float py = boardGen.boardSurfaceY + hOff
                   + boardSetup.pieceOffset.y;
        obj.transform.position = new Vector3(px, py, pz);
    }

    void PrintBoard()
    {
        Debug.Log("===== 棋盘 =====");
        for (int z = 7; z >= 0; z--)
        {
            string row = "";
            for (int x = 0; x < 8; x++)
            {
                PieceType p = board.GetPiece(x, z);
                if (p == PieceType.None) { row += ". "; continue; }
                char c = p.ToString()[0];
                row += board.GetColor(x, z) == Player.White
                       ? char.ToUpper(c) : char.ToLower(c);
                row += " ";
            }
            Debug.Log(row);
        }
    }
}
