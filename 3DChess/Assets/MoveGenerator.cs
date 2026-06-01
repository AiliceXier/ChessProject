using System.Collections.Generic;

// ============================================================
// 走法生成器 —— 生成所有合法走法
// ============================================================
public static class MoveGenerator
{
    // ============================================================
    // 公开接口
    // ============================================================

    /// 获取单个棋子的所有合法走法（已过滤将军风险）
    public static List<Move> GetLegalMoves(Board board, int x, int z)
    {
        List<Move> raw = GetRawMoves(board, x, z);
        List<Move> legal = new List<Move>();
        Player color = board.GetColor(x, z);

        foreach (Move move in raw)
        {
            Board sim = board.Clone();
            sim.ApplyMove(move);
            if (!IsInCheck(sim, color))
                legal.Add(move);
        }
        return legal;
    }

    /// 获取某方所有棋子的合法走法总和
    public static List<Move> GetAllLegalMoves(Board board, Player color)
    {
        List<Move> all = new List<Move>();
        for (int x = 0; x < 8; x++)
            for (int z = 0; z < 8; z++)
                if (board.GetColor(x, z) == color)
                    all.AddRange(GetLegalMoves(board, x, z));
        return all;
    }

    /// 某方是否存在至少一个合法走法（用于检测将死/逼和）
    public static bool HasLegalMoves(Board board, Player color)
    {
        for (int x = 0; x < 8; x++)
            for (int z = 0; z < 8; z++)
                if (board.GetColor(x, z) == color)
                    if (GetLegalMoves(board, x, z).Count > 0)
                        return true;
        return false;
    }

    /// 某方是否正在被将军
    public static bool IsInCheck(Board board, Player color)
    {
        if (!GetKingPosition(board, color, out int kx, out int kz))
            return false;
        Player enemy = (color == Player.White) ? Player.Black : Player.White;
        return IsSquareAttacked(board, kx, kz, enemy);
    }

    /// 获取某方王的位置
    public static bool GetKingPosition(Board board, Player color,
                                        out int kx, out int kz)
    {
        for (int x = 0; x < 8; x++)
            for (int z = 0; z < 8; z++)
                if (board.GetPiece(x, z) == PieceType.King
                    && board.GetColor(x, z) == color)
                {
                    kx = x; kz = z;
                    return true;
                }
        kx = -1; kz = -1;
        return false;
    }

    /// 某个格子是否被某方攻击
    public static bool IsSquareAttacked(Board board, int sx, int sz,
                                         Player byColor)
    {
        // ── 兵的攻击 ──
        // byColor 的白兵在 (sx±1, sz-1) 可攻击 (sx, sz)
        // byColor 的黑兵在 (sx±1, sz+1) 可攻击 (sx, sz)
        int pawnDir = (byColor == Player.White) ? -1 : 1;
        for (int dx = -1; dx <= 1; dx += 2)
        {
            int px = sx + dx;
            int pz = sz + pawnDir;
            if (board.IsValidCell(px, pz)
                && board.GetPiece(px, pz) == PieceType.Pawn
                && board.GetColor(px, pz) == byColor)
                return true;
        }

        // ── 马的攻击 ──
        int[] kj = { 2,1, 2,-1, -2,1, -2,-1, 1,2, 1,-2, -1,2, -1,-2 };
        for (int i = 0; i < kj.Length; i += 2)
        {
            int nx = sx + kj[i];
            int nz = sz + kj[i + 1];
            if (board.IsValidCell(nx, nz)
                && board.GetPiece(nx, nz) == PieceType.Knight
                && board.GetColor(nx, nz) == byColor)
                return true;
        }

        // ── 王的攻击 ──
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0) continue;
                int kx = sx + dx, kz = sz + dz;
                if (board.IsValidCell(kx, kz)
                    && board.GetPiece(kx, kz) == PieceType.King
                    && board.GetColor(kx, kz) == byColor)
                    return true;
            }

        // ── 远射程棋子（车/象/后）──
        int[] dirs = { 1,0, -1,0, 0,1, 0,-1, 1,1, -1,-1, 1,-1, -1,1 };
        for (int d = 0; d < dirs.Length; d += 2)
        {
            int dx = dirs[d], dz = dirs[d + 1];
            bool isOrth = (dx == 0 || dz == 0);
            int cx = sx + dx, cz = sz + dz;
            while (board.IsValidCell(cx, cz))
            {
                if (!board.IsEmpty(cx, cz))
                {
                    PieceType p = board.GetPiece(cx, cz);
                    Player   c = board.GetColor(cx, cz);
                    if (c == byColor)
                    {
                        if (p == PieceType.Queen) return true;
                        if (isOrth && p == PieceType.Rook)   return true;
                        if (!isOrth && p == PieceType.Bishop) return true;
                    }
                    break;   // 被棋子阻挡，这个方向不用再看了
                }
                cx += dx; cz += dz;
            }
        }

        return false;
    }

    // ============================================================
    // 内部：生成伪合法走法（符合棋子走法规则，但可能让己方王暴露）
    // ============================================================
    static List<Move> GetRawMoves(Board board, int x, int z)
    {
        List<Move> moves = new List<Move>();
        PieceType piece = board.GetPiece(x, z);
        Player color = board.GetColor(x, z);

        switch (piece)
        {
            case PieceType.Pawn:   PawnMoves(board, x, z, color, moves);   break;
            case PieceType.Rook:   SlidingMoves(board, x, z, color, moves,
                new int[] { 1,0, -1,0, 0,1, 0,-1 });                      break;
            case PieceType.Knight: KnightMoves(board, x, z, color, moves);  break;
            case PieceType.Bishop: SlidingMoves(board, x, z, color, moves,
                new int[] { 1,1, -1,-1, 1,-1, -1,1 });                    break;
            case PieceType.Queen:  SlidingMoves(board, x, z, color, moves,
                new int[] { 1,0, -1,0, 0,1, 0,-1, 1,1, -1,-1, 1,-1, -1,1 }); break;
            case PieceType.King:   KingMoves(board, x, z, color, moves);    break;
        }
        return moves;
    }

    // ── 兵 ──
    static void PawnMoves(Board board, int x, int z, Player color,
                          List<Move> moves)
    {
        int dir   = (color == Player.White) ? 1 : -1;
        int start = (color == Player.White) ? 1 : 6;
        int promo = (color == Player.White) ? 7 : 0;

        // 前进一步
        int fwd = z + dir;
        if (board.IsValidCell(x, fwd) && board.IsEmpty(x, fwd))
        {
            Move m = new Move(x, z, x, fwd);
            if (fwd == promo) m.promotion = PieceType.Queen;
            moves.Add(m);

            // 前进两步（仅在起始行，且中间格为空）
            int fwd2 = z + 2 * dir;
            if (z == start && board.IsEmpty(x, fwd2))
                moves.Add(new Move(x, z, x, fwd2));
        }

        // 斜吃（包括过路兵）
        for (int dx = -1; dx <= 1; dx += 2)
        {
            int nx = x + dx;
            int nz = z + dir;
            if (!board.IsValidCell(nx, nz)) continue;

            // 普通斜吃
            if (!board.IsEmpty(nx, nz) && board.GetColor(nx, nz) != color)
            {
                Move m = new Move(x, z, nx, nz);
                if (nz == promo) m.promotion = PieceType.Queen;
                moves.Add(m);
            }

            // 吃过路兵
            if (nx == board.enPassantX && nz == board.enPassantZ)
                moves.Add(new Move(x, z, nx, nz,
                                   PieceType.None, false, true));
        }
    }

    // ── 远射程棋子（车 / 象 / 后共享）──
    static void SlidingMoves(Board board, int x, int z, Player color,
                             List<Move> moves, int[] dirs)
    {
        for (int d = 0; d < dirs.Length; d += 2)
        {
            int dx = dirs[d], dz = dirs[d + 1];
            int nx = x + dx, nz = z + dz;
            while (board.IsValidCell(nx, nz))
            {
                if (board.IsEmpty(nx, nz))
                    moves.Add(new Move(x, z, nx, nz));
                else
                {
                    if (board.GetColor(nx, nz) != color)
                        moves.Add(new Move(x, z, nx, nz));
                    break;   // 碰到棋子就停
                }
                nx += dx; nz += dz;
            }
        }
    }

    // ── 马 ──
    static void KnightMoves(Board board, int x, int z, Player color,
                            List<Move> moves)
    {
        int[] jumps = { 2,1, 2,-1, -2,1, -2,-1, 1,2, 1,-2, -1,2, -1,-2 };
        for (int i = 0; i < jumps.Length; i += 2)
        {
            int nx = x + jumps[i], nz = z + jumps[i + 1];
            if (board.IsValidCell(nx, nz)
                && (board.IsEmpty(nx, nz) || board.GetColor(nx, nz) != color))
                moves.Add(new Move(x, z, nx, nz));
        }
    }

    // ── 王（含王车易位）──
    static void KingMoves(Board board, int x, int z, Player color,
                          List<Move> moves)
    {
        // 普通走动
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0) continue;
                int nx = x + dx, nz = z + dz;
                if (board.IsValidCell(nx, nz)
                    && (board.IsEmpty(nx, nz) || board.GetColor(nx, nz) != color))
                    moves.Add(new Move(x, z, nx, nz));
            }

        // 王车易位
        if (board.GetHasMoved(x, z)) return;   // 王动过就不能易位

        int backRow = (color == Player.White) ? 0 : 7;
        Player enemy = (color == Player.White) ? Player.Black : Player.White;

        // ── 王翼易位 ──
        if (board.GetPiece(7, backRow) == PieceType.Rook
            && board.GetColor(7, backRow) == color
            && !board.GetHasMoved(7, backRow)
            && board.IsEmpty(5, backRow)
            && board.IsEmpty(6, backRow)
            && !IsSquareAttacked(board, 4, backRow, enemy)   // 王当前不被将军
            && !IsSquareAttacked(board, 5, backRow, enemy)   // 不穿过被攻击格
            && !IsSquareAttacked(board, 6, backRow, enemy))  // 不落入被攻击格
        {
            moves.Add(new Move(x, z, 6, backRow,
                               PieceType.None, true, false));
        }

        // ── 后翼易位 ──
        if (board.GetPiece(0, backRow) == PieceType.Rook
            && board.GetColor(0, backRow) == color
            && !board.GetHasMoved(0, backRow)
            && board.IsEmpty(1, backRow)
            && board.IsEmpty(2, backRow)
            && board.IsEmpty(3, backRow)
            && !IsSquareAttacked(board, 4, backRow, enemy)
            && !IsSquareAttacked(board, 3, backRow, enemy)
            && !IsSquareAttacked(board, 2, backRow, enemy))
        {
            moves.Add(new Move(x, z, 2, backRow,
                               PieceType.None, true, false));
        }
    }
}
