using System;
using System.Collections.Generic;
using System.Linq;

namespace Chess
{
    public class ChessAI
    {
        private readonly int _maxDepth;

        public ChessAI(int maxDepth = 3)
        {
            _maxDepth = maxDepth;
        }

        public Move GetBestMove(ChessBoard board)
        {
            var moves = GenerateMovesSync(board);
            if (moves.Length == 0)
                return null;

            var isMaximizing = board.Turn == PieceColor.White;
            Move bestMove = null;
            int bestScore = isMaximizing ? int.MinValue : int.MaxValue;

            foreach (var move in moves)
            {
                board.Move(new Move(move.OriginalPosition, move.NewPosition));
                int score = Minimax(board, _maxDepth - 1, int.MinValue + 1, int.MaxValue - 1, !isMaximizing);
                board.Cancel();

                if (isMaximizing)
                {
                    if (score > bestScore) { bestScore = score; bestMove = move; }
                }
                else
                {
                    if (score < bestScore) { bestScore = score; bestMove = move; }
                }
            }

            return bestMove;
        }

        private int Minimax(ChessBoard board, int depth, int alpha, int beta, bool isMaximizing)
        {
            if (depth == 0 || board.IsEndGame)
                return Evaluate(board);

            var moves = GenerateMovesSync(board);

            if (isMaximizing)
            {
                int maxEval = int.MinValue;
                foreach (var move in moves)
                {
                    board.Move(new Move(move.OriginalPosition, move.NewPosition));
                    int eval = Minimax(board, depth - 1, alpha, beta, false);
                    board.Cancel();
                    if (eval > maxEval) maxEval = eval;
                    if (eval > alpha) alpha = eval;
                    if (beta <= alpha) break;
                }
                return moves.Length == 0 ? Evaluate(board) : maxEval;
            }
            else
            {
                int minEval = int.MaxValue;
                foreach (var move in moves)
                {
                    board.Move(new Move(move.OriginalPosition, move.NewPosition));
                    int eval = Minimax(board, depth - 1, alpha, beta, true);
                    board.Cancel();
                    if (eval < minEval) minEval = eval;
                    if (eval < beta) beta = eval;
                    if (beta <= alpha) break;
                }
                return moves.Length == 0 ? Evaluate(board) : minEval;
            }
        }

        private static Move[] GenerateMovesSync(ChessBoard board)
        {
            var moves = new List<Move>();
            for (short i = 0; i < 8; i++)
            for (short j = 0; j < 8; j++)
            {
                if (board.pieces[i, j] != null)
                {
                    var pieceMoves = board.Moves(new Position { Y = i, X = j }, generateSan: false);
                    moves.AddRange(pieceMoves);
                }
            }
            return moves.ToArray();
        }

        public int EvaluatePosition(ChessBoard board)
        {
            return Evaluate(board);
        }

        private static int Evaluate(ChessBoard board)
        {
            if (board.IsEndGame)
            {
                var endgame = board.EndGame;
                if (endgame?.EndgameType == EndgameType.Checkmate)
                    return board.Turn == PieceColor.White ? -99999 : 99999;
                return 0;
            }

            int score = 0;
            for (short y = 0; y < 8; y++)
            for (short x = 0; x < 8; x++)
            {
                var piece = board.pieces[y, x];
                if (piece == null) continue;

                int value = GetPieceValue(piece.Type);
                int posBonus = GetPositionBonus(piece, x, y);
                score += piece.Color == PieceColor.White ? value + posBonus : -(value + posBonus);
            }
            return score;
        }

        private static int GetPieceValue(PieceType type)
        {
            if (type == PieceType.Pawn)   return 100;
            if (type == PieceType.Knight) return 320;
            if (type == PieceType.Bishop) return 330;
            if (type == PieceType.Rook)   return 500;
            if (type == PieceType.Queen)  return 900;
            if (type == PieceType.King)   return 20000;
            return 0;
        }

        private static int GetPositionBonus(Piece piece, short x, short y)
        {
            bool isWhite = piece.Color == PieceColor.White;
            short row = isWhite ? y : (short)(7 - y);
            short col = x;

            if (piece.Type == PieceType.Pawn)
            {
                var table = new short[8, 8]
                {
                    { 0,  0,  0,  0,  0,  0,  0,  0},
                    {50, 50, 50, 50, 50, 50, 50, 50},
                    {10, 10, 20, 30, 30, 20, 10, 10},
                    { 5,  5, 10, 25, 25, 10,  5,  5},
                    { 0,  0,  0, 20, 20,  0,  0,  0},
                    { 5, -5,-10,  0,  0,-10, -5,  5},
                    { 5, 10, 10,-20,-20, 10, 10,  5},
                    { 0,  0,  0,  0,  0,  0,  0,  0}
                };
                return table[row, col];
            }

            if (piece.Type == PieceType.Knight)
            {
                var table = new short[8, 8]
                {
                    {-50,-40,-30,-30,-30,-30,-40,-50},
                    {-40,-20,  0,  0,  0,  0,-20,-40},
                    {-30,  0, 10, 15, 15, 10,  0,-30},
                    {-30,  5, 15, 20, 20, 15,  5,-30},
                    {-30,  0, 15, 20, 20, 15,  0,-30},
                    {-30,  5, 10, 15, 15, 10,  5,-30},
                    {-40,-20,  0,  5,  5,  0,-20,-40},
                    {-50,-40,-30,-30,-30,-30,-40,-50}
                };
                return table[row, col];
            }

            if (piece.Type == PieceType.Bishop)
            {
                var table = new short[8, 8]
                {
                    {-20,-10,-10,-10,-10,-10,-10,-20},
                    {-10,  0,  0,  0,  0,  0,  0,-10},
                    {-10,  0, 10, 10, 10, 10,  0,-10},
                    {-10,  5,  5, 10, 10,  5,  5,-10},
                    {-10,  0, 10, 10, 10, 10,  0,-10},
                    {-10, 10, 10, 10, 10, 10, 10,-10},
                    {-10,  5,  0,  0,  0,  0,  5,-10},
                    {-20,-10,-10,-10,-10,-10,-10,-20}
                };
                return table[row, col];
            }

            if (piece.Type == PieceType.Rook)
            {
                var table = new short[8, 8]
                {
                    { 0,  0,  0,  0,  0,  0,  0,  0},
                    { 5, 10, 10, 10, 10, 10, 10,  5},
                    {-5,  0,  0,  0,  0,  0,  0, -5},
                    {-5,  0,  0,  0,  0,  0,  0, -5},
                    {-5,  0,  0,  0,  0,  0,  0, -5},
                    {-5,  0,  0,  0,  0,  0,  0, -5},
                    {-5,  0,  0,  0,  0,  0,  0, -5},
                    { 0,  0,  0,  5,  5,  0,  0,  0}
                };
                return table[row, col];
            }

            if (piece.Type == PieceType.Queen)
            {
                var table = new short[8, 8]
                {
                    {-20,-10,-10, -5, -5,-10,-10,-20},
                    {-10,  0,  0,  0,  0,  0,  0,-10},
                    {-10,  0,  5,  5,  5,  5,  0,-10},
                    { -5,  0,  5,  5,  5,  5,  0, -5},
                    {  0,  0,  5,  5,  5,  5,  0, -5},
                    {-10,  5,  5,  5,  5,  5,  0,-10},
                    {-10,  0,  5,  0,  0,  0,  0,-10},
                    {-20,-10,-10, -5, -5,-10,-10,-20}
                };
                return table[row, col];
            }

            if (piece.Type == PieceType.King)
            {
                var table = new short[8, 8]
                {
                    {-30,-40,-40,-50,-50,-40,-40,-30},
                    {-30,-40,-40,-50,-50,-40,-40,-30},
                    {-30,-40,-40,-50,-50,-40,-40,-30},
                    {-30,-40,-40,-50,-50,-40,-40,-30},
                    {-20,-30,-30,-40,-40,-30,-30,-20},
                    {-10,-20,-20,-20,-20,-20,-20,-10},
                    { 20, 20,  0,  0,  0,  0, 20, 20},
                    { 20, 30, 10,  0,  0, 10, 30, 20}
                };
                return table[row, col];
            }

            return 0;
        }
    }
}
