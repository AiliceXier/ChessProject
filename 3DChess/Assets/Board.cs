// ============================================================
// 棋子类型
// ============================================================
public enum PieceType
{
    None,
    Pawn,
    Rook,
    Knight,
    Bishop,
    Queen,
    King
}

// ============================================================
// 玩家
// ============================================================
public enum Player
{
    White,
    Black,
    None
}

// ============================================================
// 游戏状态
// ============================================================
public enum GameState
{
    Playing,        // 游戏进行中
    WhiteWon,       // 白方胜利（将死黑方）
    BlackWon,       // 黑方胜利（将死白方）
    Stalemate       // 逼和（无子可动且未被将军）
}

// ============================================================
// 走法数据结构
// ============================================================
public struct Move
{
    public int fromX, fromZ;        // 起始坐标
    public int toX, toZ;            // 目标坐标
    public PieceType promotion;     // 升变兵种（None 表示不升变）
    public bool isCastling;         // 是否是王车易位
    public bool isEnPassant;        // 是否是吃过路兵

    public Move(int fx, int fz, int tx, int tz,
                PieceType prom = PieceType.None,
                bool castling = false,
                bool enPassant = false)
    {
        fromX = fx; fromZ = fz;
        toX = tx; toZ = tz;
        promotion = prom;
        isCastling = castling;
        isEnPassant = enPassant;
    }
}

// ============================================================
// 棋盘数据模型
// ============================================================
public class Board
{
    public PieceType[,] pieces = new PieceType[8, 8];
    public Player[,] colors = new Player[8, 8];
    public bool[,] hasMoved = new bool[8, 8];

    // 吃过路兵目标格（上一手兵走两格时，记录其"经过"的格子）
    // 值为 (-1, -1) 表示没有过路兵机会
    public int enPassantX = -1;
    public int enPassantZ = -1;

    // ----------------------------------------------------------
    // 构造函数：初始化空棋盘
    // ----------------------------------------------------------
    public Board()
    {
        for (int x = 0; x < 8; x++)
            for (int z = 0; z < 8; z++)
            {
                pieces[x, z] = PieceType.None;
                colors[x, z] = Player.None;
            }
    }

    // ----------------------------------------------------------
    // 深拷贝（用于走法合法性模拟）
    // ----------------------------------------------------------
    public Board Clone()
    {
        Board b = new Board();
        for (int x = 0; x < 8; x++)
        {
            for (int z = 0; z < 8; z++)
            {
                b.pieces[x, z] = pieces[x, z];
                b.colors[x, z] = colors[x, z];
                b.hasMoved[x, z] = hasMoved[x, z];
            }
        }
        b.enPassantX = enPassantX;
        b.enPassantZ = enPassantZ;
        return b;
    }

    // ----------------------------------------------------------
    // 基础操作
    // ----------------------------------------------------------
    public void SetPiece(int x, int z, PieceType type, Player color)
    {
        pieces[x, z] = type;
        colors[x, z] = color;
        hasMoved[x, z] = false;
    }

    public void RemovePiece(int x, int z)
    {
        pieces[x, z] = PieceType.None;
        colors[x, z] = Player.None;
    }

    public PieceType GetPiece(int x, int z) => pieces[x, z];
    public Player GetColor(int x, int z) => colors[x, z];

    public bool IsValidCell(int x, int z) =>
        x >= 0 && x < 8 && z >= 0 && z < 8;

    public bool IsEmpty(int x, int z) =>
        pieces[x, z] == PieceType.None;

    public void SetHasMoved(int x, int z, bool moved) =>
        hasMoved[x, z] = moved;

    public bool GetHasMoved(int x, int z) => hasMoved[x, z];

    // ----------------------------------------------------------
    // 清空过路兵标记
    // ----------------------------------------------------------
    public void ClearEnPassant()
    {
        enPassantX = -1;
        enPassantZ = -1;
    }

    // ----------------------------------------------------------
    // 在模拟棋盘上执行走法（不校验合法性，仅移动棋子）
    // 用于 MoveGenerator 中模拟走法后检查是否被将军
    // ----------------------------------------------------------
    public void ApplyMove(Move move)
    {
        PieceType movingPiece = GetPiece(move.fromX, move.fromZ);
        Player movingColor = GetColor(move.fromX, move.fromZ);

        // 1. 移除起始位置的棋子
        RemovePiece(move.fromX, move.fromZ);

        // 2. 处理特殊吃子
        if (move.isEnPassant)
        {
            // 过路兵：被吃的兵在目标格子的同一列、起始行
            RemovePiece(move.toX, move.fromZ);
        }
        else if (!IsEmpty(move.toX, move.toZ))
        {
            // 普通吃子：移除目标位置的对方棋子
            RemovePiece(move.toX, move.toZ);
        }

        // 3. 放置棋子到目标位置（处理升变）
        PieceType finalType = (move.promotion != PieceType.None)
                              ? move.promotion : movingPiece;
        SetPiece(move.toX, move.toZ, finalType, movingColor);
        SetHasMoved(move.toX, move.toZ, true);

        // 4. 王车易位：移动车
        if (move.isCastling)
        {
            int backRow = move.fromZ; // 王所在行
            if (move.toX == 6)
            {
                // 王翼易位：车从 (7, backRow) 移到 (5, backRow)
                RemovePiece(7, backRow);
                SetPiece(5, backRow, PieceType.Rook, movingColor);
                SetHasMoved(5, backRow, true);
            }
            else if (move.toX == 2)
            {
                // 后翼易位：车从 (0, backRow) 移到 (3, backRow)
                RemovePiece(0, backRow);
                SetPiece(3, backRow, PieceType.Rook, movingColor);
                SetHasMoved(3, backRow, true);
            }
        }

        // 5. 更新过路兵标记
        ClearEnPassant();
        if (movingPiece == PieceType.Pawn &&
            System.Math.Abs(move.toZ - move.fromZ) == 2)
        {
            // 兵走了两格，记录"经过"的格子
            enPassantX = move.fromX;
            enPassantZ = (move.fromZ + move.toZ) / 2;
        }
    }
}
