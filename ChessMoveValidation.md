# 国际象棋下棋合法性验证代码详解

本文档详细讲解项目中验证玩家下棋合法性的核心代码，帮助你深入理解每个棋子的移动规则实现。

## 目录

1. [整体架构概述](#1-整体架构概述)
2. [核心验证流程](#2-核心验证流程)
3. [兵的移动验证 (PawnValidation)](#3-兵的移动验证-pawnvalidation)
4. [车的移动验证 (RookValidation)](#4-车的移动验证-rookvalidation)
5. [马的移动验证 (KnightValidation)](#5-马的移动验证-knightvalidation)
6. [象的移动验证 (BishopValidation)](#6-象的移动验证-bishopvalidation)
7. [后的移动验证 (QueenValidation)](#7-后的移动验证-queenvalidation)
8. [王的移动验证 (KingValidation)](#8-王的移动验证-kingvalidation)
9. [将军检测 (IsKingChecked)](#9-将军检测-iskingchecked)
10. [王车易位验证 (HasRightToCastle)](#10-王车易位验证-hasrighttocastle)
11. [吃过路兵验证 (IsValidEnPassant)](#11-吃过路兵验证-isvalidenpassant)
12. [走法生成器 (GeneratePositions)](#12-走法生成器-generatepositions)

---

## 1. 整体架构概述

### 1.1 核心文件位置

| 文件 | 路径 | 职责 |
|------|------|------|
| ChessValidations.cs | `Chess/Assets/Chess/ChessBoard/ChessValidations.cs` | 所有棋子移动合法性验证 |
| ChessGenerations.cs | `Chess/Assets/Chess/ChessBoard/ChessGenerations.cs` | 生成所有可能的走法 |
| ChessBoard.cs | `Chess/Assets/Chess/ChessBoard/ChessBoard.cs` | 棋盘状态管理和走法执行 |
| Move.cs | `Chess/Assets/Chess/Types/Move.cs` | 走法数据结构 |
| Piece.cs | `Chess/Assets/Chess/Types/Piece.cs` | 棋子数据结构 |
| Position.cs | `Chess/Assets/Chess/Types/Position.cs` | 位置坐标数据结构 |
| MoveParameter.cs | `Chess/Assets/Chess/Types/MoveParameter.cs` | 特殊走法参数（升变、易位、吃过路兵） |

### 1.2 关键类关系

```
ChessBoard (部分类，分布在多个文件)
├── pieces[8,8] - 棋盘上的棋子数组
├── executedMoves - 已执行的走法列表
├── Turn - 当前轮到哪方走棋
├── IsValidMove() - 验证走法合法性
├── Move() - 执行走法
└── 各种验证方法...

Move
├── Piece - 移动的棋子
├── OriginalPosition - 起始位置
├── NewPosition - 目标位置
├── CapturedPiece - 被吃的棋子
├── Parameter - 特殊走法参数
├── IsCheck - 是否将军
└── IsMate - 是否将杀

Piece
├── Color - 棋子颜色（白/黑）
└── Type - 棋子类型（兵/车/马/象/后/王）

Position
├── X - 横坐标（0-7，对应a-h）
└── Y - 纵坐标（0-7，对应1-8）
```

---

## 2. 核心验证流程

### 2.1 入口方法

验证走法合法性的入口有三个重载方法：

```csharp
// 入口1：通过SAN字符串验证
public bool IsValidMove(string san)
{
    var (succeeded, exception) = SanBuilder.TryParse(this, san, out var move, false);
    if (!succeeded && exception is not null)
        return false;
    return IsValidMove(move!);
}

// 入口2：通过Move对象验证
public bool IsValidMove(Move move)
{
    return IsValidMove(move, this, false, true);
}

// 入口3：完整的内部验证方法
internal static bool IsValidMove(Move move, ChessBoard board, bool raise, bool checkTurn)
```

### 2.2 完整验证流程

```csharp
internal static bool IsValidMove(Move move, ChessBoard board, bool raise, bool checkTurn)
{
    // 1. 基本检查
    if (move is null || !move.HasValue)
        throw new ArgumentNullException(nameof(move));

    // 2. 获取起始位置的棋子
    var piece = board.pieces[move.OriginalPosition.Y, move.OriginalPosition.X];
    if (piece == null)
        throw new ChessPieceNotFoundException(board, move.OriginalPosition);

    // 3. 检查是否轮到该棋子方走棋
    if (checkTurn && piece.Color != board.Turn)
        return false;

    // 4. 检查起始位置和目标位置是否相同
    if (move.OriginalPosition == move.NewPosition)
        return false;

    // 5. 重置走法属性
    ResetMoveProperties(move, piece);

    // 6. 根据棋子类型进行具体验证
    bool isValid = IsValidMove(move, board);

    // 7. 关键：验证走棋后己方王是否被将军
    bool isChecked = !isValid || IsKingCheckedValidation(move, move.Piece.Color, board);

    if (!isChecked)
    {
        // 8. 设置合法走法的属性（被吃棋子、将军、将杀等）
        ValidMoveSetProperties(move, board, raise, promParams);
        return true;
    }
    else
    {
        // 走法导致己方王被将军，非法
        if (isValid && raise)
        {
            board.OnInvalidMoveKingCheckedEvent(...);
        }
        return false;
    }
}
```

### 2.3 棋子类型分发验证

```csharp
internal static bool IsValidMove(Move move, ChessBoard board)
{
    return move.Piece.Type switch
    {
        var e when e == PieceType.Pawn => PawnValidation(move, board),
        var e when e == PieceType.Rook => RookValidation(move, board.pieces),
        var e when e == PieceType.Knight => KnightValidation(move, board.pieces),
        var e when e == PieceType.Bishop => BishopValidation(move, board.pieces),
        var e when e == PieceType.Queen => QueenValidation(move, board.pieces),
        var e when e == PieceType.King => KingValidation(move, board),
        _ => false
    };
}
```

---

## 3. 兵的移动验证 (PawnValidation)

### 3.1 兵的移动规则

兵是国际象棋中最特殊的棋子，具有以下移动规则：
1. **向前直走**：只能向己方前方移动（白方向上，黑方向下）
2. **一格移动**：每次可以向前移动一格（如果前方无子）
3. **两格起步**：从初始位置可以向前移动两格（如果路径畅通）
4. **斜向吃子**：只能向前斜向一格吃子
5. **升变**：到达对方底线时必须升变为其他棋子（后、车、象、马）
6. **吃过路兵**：对方兵从初始位置前进两格时，可以在特定条件下斜向吃掉它

### 3.2 代码详解

```csharp
private static bool PawnValidation(Move move, ChessBoard board)
{
    bool isValid = false;

    // 计算纵横向差值
    short verticalDifference = (short)(move.NewPosition.Y - move.OriginalPosition.Y);
    short horizontalDifference = (short)(move.NewPosition.X - move.OriginalPosition.X);

    short verticalStep = Math.Abs(verticalDifference);
    short horizontalStep = Math.Abs(horizontalDifference);

    PieceColor pieceColor = move.Piece.Color;

    // 核心判断：兵只能向前移动
    // 白方：Y增加（从下往上）
    // 黑方：Y减少（从上往下）
    if ((pieceColor == PieceColor.White && verticalDifference > 0) || 
        (pieceColor == PieceColor.Black && verticalDifference < 0))
    {
        // 规则1：向前移动一格
        if (horizontalStep == 0 && verticalStep == 1 && 
            board.pieces[move.NewPosition.Y, move.NewPosition.X] == null)
        {
            HandlePotentialPromotion(move);  // 检查是否需要升变
            isValid = true;
        }
        // 规则2：从初始位置向前移动两格
        else if (horizontalStep == 0 && verticalStep == 2
            // 白方从第2行(Y=1)出发，检查中间两格是否为空
            && ((move.OriginalPosition.Y == 1 && 
                 board.pieces[2, move.NewPosition.X] == null &&
                 board.pieces[3, move.NewPosition.X] == null)
            // 黑方从第7行(Y=6)出发，检查中间两格是否为空
             || (move.OriginalPosition.Y == 6 && 
                 board.pieces[5, move.NewPosition.X] == null &&
                 board.pieces[4, move.NewPosition.X] == null)))
        {
            isValid = true;
        }
        // 规则3：斜向吃子
        else if (verticalStep == 1 && horizontalStep == 1
               && board.pieces[move.NewPosition.Y, move.NewPosition.X] != null
               && pieceColor != board.pieces[move.NewPosition.Y, move.NewPosition.X].Color)
        {
            HandlePotentialPromotion(move);
            isValid = true;
        }
        // 规则4：吃过路兵
        else if (IsValidEnPassant(move, board, verticalDifference, horizontalDifference))
        {
            HandleEnPassant(move, verticalDifference, pieceColor);
            isValid = true;
        }
    }

    return isValid;
}
```

### 3.3 升变处理

```csharp
private static void HandlePotentialPromotion(Move move)
{
    // 检查是否到达对方底线
    // MAX_ROWS - 1 = 7（最后一行索引）
    // Y % 7 == 0 表示 Y = 0 或 Y = 7
    if (move.NewPosition.Y % (MAX_ROWS - 1) == 0)
    {
        move.Parameter = new MovePromotion(PromotionType.Default);
    }
}
```

### 3.4 吃过路兵处理

```csharp
private static void HandleEnPassant(Move move, short verticalDifference, PieceColor pieceColor)
{
    move.Parameter = new MoveEnPassant()
    {
        // 被吃过路兵的位置在目标位置的后方一格
        CapturedPawnPosition = new Position()
        {
            Y = (short)(move.NewPosition.Y - verticalDifference),
            X = move.NewPosition.X
        }
    };
    // 记录被吃的棋子（用于后续处理）
    move.CapturedPiece = new Piece(pieceColor.OppositeColor(), PieceType.Pawn);
}
```

---

## 4. 车的移动验证 (RookValidation)

### 4.1 车的移动规则

车可以：
1. 沿横线（行）任意格数移动
2. 沿竖线（列）任意格数移动
3. 不能斜向移动
4. 路径上不能有其他棋子阻挡
5. 可以吃掉路径终点的对方棋子

### 4.2 代码详解

```csharp
private static bool RookValidation(Move move, Piece?[,] pieces)
{
    // 计算纵向和横向差值
    int verticalDiff = move.NewPosition.Y - move.OriginalPosition.Y;
    int horizontalDiff = move.NewPosition.X - move.OriginalPosition.X;

    // 规则1：车只能直线移动（要么横向，要么纵向）
    // 如果纵向和横向都有位移，则非法
    if (verticalDiff != 0 && horizontalDiff != 0)
        return false;

    // 计算移动方向的步长（-1、0 或 1）
    // Math.Sign: 正数返回1，负数返回-1，0返回0
    int stepVertical = Math.Sign(verticalDiff);
    int stepHorizontal = Math.Sign(horizontalDiff);

    // 从起始位置的下一格开始检查
    int i = move.OriginalPosition.Y + stepVertical;
    int j = move.OriginalPosition.X + stepHorizontal;

    // 规则2：检查路径上是否有障碍物
    // 沿着移动方向逐格检查，直到到达目标位置前
    while (i != move.NewPosition.Y || j != move.NewPosition.X)
    {
        // 如果路径上有棋子，则阻挡，非法
        if (pieces[i, j] != null)
            return false;

        // 继续向目标方向移动
        i += stepVertical;
        j += stepHorizontal;
    }

    // 规则3：目标位置要么为空，要么是对方棋子
    // ?.Color != move.Piece.Color 确保不吃己方棋子
    return pieces[i, j]?.Color != move.Piece.Color;
}
```

### 4.3 算法图解

```
车从 e4 (X=4, Y=3) 移动到 e8 (X=4, Y=7):

   a b c d e f g h
8  . . . . ♖ . . .    目标位置 (4,7)
7  . . . . . . . .
6  . . . . . . . .
5  . . . . . . . .
4  . . . . ♜ . . .    起始位置 (4,3)
3  . . . . . . . .
2  . . . . . . . .
1  . . . . . . . .

verticalDiff = 7 - 3 = 4
horizontalDiff = 4 - 4 = 0
stepVertical = 1, stepHorizontal = 0

检查路径：(4,4) -> (4,5) -> (4,6) -> (4,7)
如果 (4,4), (4,5), (4,6) 都为空，则合法
```

---

## 5. 马的移动验证 (KnightValidation)

### 5.1 马的移动规则

马是国际象棋中唯一可以"跳跃"的棋子：
1. 走"日"字形：两格直线加一格垂直（或相反）
2. 可以跳过其他棋子
3. 可以到达8个可能位置中的任意一个（如果在棋盘内）

### 5.2 代码详解

```csharp
private static bool KnightValidation(Move move, Piece?[,] pieces)
{
    // 计算横向和纵向的绝对差值
    int verticalDiff = Math.Abs(move.NewPosition.X - move.OriginalPosition.X);
    int horizontalDiff = Math.Abs(move.NewPosition.Y - move.NewPosition.Y);

    // 规则：马走"日"字形
    // 情况1：横向走2格，纵向走1格
    // 情况2：横向走1格，纵向走2格
    if ((verticalDiff == 2 && horizontalDiff == 1) || 
        (verticalDiff == 1 && horizontalDiff == 2))
    {
        // 目标位置要么为空，要么是对方棋子
        return pieces[move.NewPosition.Y, move.NewPosition.X]?.Color != move.Piece.Color;
    }

    return false;
}
```

### 5.3 马的8个可能位置

```
马在 d4 (X=3, Y=3) 位置时，可以移动到以下8个位置：

   a b c d e f g h
8  . . . . . . . .
7  . . . . . . . .
6  . . ① . ② . . .    ① c6 (2,5)  ② e6 (4,5)
5  . ⑧ . . . ③ . .    ⑧ b5 (1,4)  ③ f5 (5,4)
4  . . . ♞ . . . .
3  . ⑦ . . . ④ . .    ⑦ b3 (1,2)  ④ f3 (5,2)
2  . . ⑥ . ⑤ . . .    ⑥ c2 (2,1)  ⑤ e2 (4,1)
1  . . . . . . . .
```

---

## 6. 象的移动验证 (BishopValidation)

### 6.1 象的移动规则

象只能沿对角线移动：
1. 沿对角线任意格数移动
2. 路径上不能有其他棋子阻挡
3. 象始终停留在同色格子上（白格象或黑格象）
4. 可以吃掉路径终点的对方棋子

### 6.2 代码详解

```csharp
private static bool BishopValidation(Move move, Piece?[,] pieces)
{
    // 计算纵向和横向差值
    var verticalDiff = move.NewPosition.Y - move.OriginalPosition.Y;
    var horizontalDiff = move.NewPosition.X - move.OriginalPosition.X;

    // 规则1：象只能沿对角线移动
    // 纵向和横向移动的格数必须相等
    if (Math.Abs(verticalDiff) != Math.Abs(horizontalDiff))
        return false;

    // 计算移动方向的步长
    var stepVertical = Math.Sign(verticalDiff);
    var stepHorizontal = Math.Sign(horizontalDiff);

    // 从起始位置的下一格开始检查
    int i = move.OriginalPosition.Y + stepVertical;
    int j = move.OriginalPosition.X + stepHorizontal;

    // 规则2：检查路径上是否有障碍物
    // 沿着对角线方向逐格检查
    while (i != move.NewPosition.Y && j != move.NewPosition.X)
    {
        if (pieces[i, j] != null)
            return false;

        i += stepVertical;
        j += stepHorizontal;
    }

    // 规则3：目标位置要么为空，要么是对方棋子
    return pieces[i, j]?.Color != move.Piece.Color;
}
```

### 6.3 算法图解

```
象从 c1 (X=2, Y=0) 移动到 f4 (X=5, Y=3):

   a b c d e f g h
4  . . . . . ♗ . .    目标位置 (5,3)
3  . . . . . . . .
2  . . . . . . . .
1  . . ♝ . . . . .    起始位置 (2,0)

verticalDiff = 3 - 0 = 3
horizontalDiff = 5 - 2 = 3
|3| == |3| ✓ 对角线移动

stepVertical = 1, stepHorizontal = 1
检查路径：(3,1) -> (4,2) -> (5,3)
如果 (3,1) 和 (4,2) 都为空，则合法
```

---

## 7. 后的移动验证 (QueenValidation)

### 7.1 后的移动规则

后是国际象棋中最强大的棋子，结合了车和象的能力：
1. 可以沿横线、竖线、对角线任意格数移动
2. 路径上不能有其他棋子阻挡
3. 可以吃掉路径终点的对方棋子

### 7.2 代码详解

```csharp
private static bool QueenValidation(Move move, Piece?[,] pieces)
{
    // 后的移动 = 车的移动 OR 象的移动
    // 只要满足其中一种移动方式即可
    return BishopValidation(move, pieces) || RookValidation(move, pieces);
}
```

### 7.3 代码设计亮点

这里使用了**代码复用**的设计思想：
- 后不需要单独实现复杂的移动逻辑
- 直接复用象和车的验证方法
- 使用逻辑或(||)连接，只要满足一种即可

---

## 8. 王的移动验证 (KingValidation)

### 8.1 王的移动规则

王是最重要的棋子：
1. 可以向任意方向移动一格（横、竖、对角）
2. 不能移动到被将军的位置
3. **特殊规则：王车易位** - 在特定条件下，王可以向车的方向移动两格，同时车跳到王的另一侧

### 8.2 代码详解

```csharp
private static bool KingValidation(Move move, ChessBoard board)
{
    // 规则1：普通移动 - 王可以向任意方向移动一格
    // 横向和纵向的位移都小于2（即0或1）
    if (Math.Abs(move.NewPosition.X - move.OriginalPosition.X) < 2 && 
        Math.Abs(move.NewPosition.Y - move.OriginalPosition.Y) < 2)
    {
        // 目标位置要么为空，要么是对方棋子
        return board.pieces[move.NewPosition.Y, move.NewPosition.X]?.Color != move.Piece.Color;
    }

    // 规则2：王车易位验证
    
    // 检查是否横向移动
    bool kingMovesHorizontally = move.OriginalPosition.Y == move.NewPosition.Y;
    
    // 检查王是否在初始位置 (e1: X=4,Y=0 或 e8: X=4,Y=7)
    // Y % 7 == 0 表示 Y = 0 或 Y = 7
    bool kingOnBeginPos = move.OriginalPosition.X == 4 && move.OriginalPosition.Y % 7 == 0;

    if (!kingOnBeginPos || !kingMovesHorizontally)
        return false;

    // 检查是否移动两格（标准易位）或直接移动到车的位置
    bool kingMoves2Tiles = Math.Abs(move.NewPosition.X - move.OriginalPosition.X) == 2;
    bool kingMovesOnRook = move.NewPosition.X % 7 == 0;  // X = 0 或 X = 7

    if (!kingMovesOnRook && !kingMoves2Tiles)
        return false;

    // 标准化X坐标用于检查
    // 如果目标是车，则标准化为王的最终位置
    int x = kingMovesOnRook ? (move.NewPosition.X == 0 ? 2 : 6) : move.NewPosition.X;

    // 判断是王翼易位还是后翼易位
    bool isKingSideCastle = x == 6;    // g1 或 g8
    bool isQueenSideCastle = x == 2;   // c1 或 c8

    // 设置易位参数
    MoveCastle moveCastle = isKingSideCastle 
        ? new MoveCastle(CastleType.King) 
        : new MoveCastle(CastleType.Queen);
    move.Parameter = moveCastle;

    int y = move.NewPosition.Y;
    bool hasObstacles = true;

    // 检查王和车之间是否有障碍物
    if (isQueenSideCastle)
        // 后翼：检查 b1, c1, d1 是否为空
        hasObstacles = board.pieces[y, 1] != null || 
                       board.pieces[y, 2] != null || 
                       board.pieces[y, 3] != null;
    else if (isKingSideCastle)
        // 王翼：检查 f1, g1 是否为空
        hasObstacles = board.pieces[y, 5] != null || 
                       board.pieces[y, 6] != null;

    // 验证是否有易位权利
    bool isValid = !hasObstacles && 
                   HasRightToCastle(move.Piece.Color, moveCastle.CastleType, board);

    // 标准化易位目标位置
    if (board.StandardiseCastlingPositions && isValid && kingMovesOnRook)
        move.NewPosition = new Position((short)(move.NewPosition.X == 0 ? 2 : 6), move.NewPosition.Y);

    return isValid;
}
```

---

## 9. 将军检测 (IsKingChecked)

### 9.1 将军的概念

当一方的王被对方的棋子攻击时，称为"将军"。此时：
1. 被将军的一方必须立即应将
2. 不能进行导致己方被将军的走法

### 9.2 核心检测方法

```csharp
private static bool IsKingChecked(PieceColor side, ChessBoard board)
{
    // 获取王的位置
    var kingPos = GetKingPosition(side, board);

    // 如果在验证过程中王被吃掉（特殊情况）
    if (!kingPos.HasValue)
        return false;

    // 遍历整个棋盘，检查对方每个棋子是否能攻击到王
    for (short i = 0; i < MAX_ROWS; i++)
    {
        for (short j = 0; j < MAX_COLS; j++)
        {
            var piece = board.pieces[i, j];
            
            // 跳过空格和己方棋子
            if (piece == null || piece.Color == side)
                continue;
                
            // 跳过王本身的位置
            if (kingPos.X == j && kingPos.Y == i)
                continue;

            // 检查对方棋子是否能合法移动到王的位置
            // 如果能，则说明王被将军
            if (IsValidMove(new Move(new Position { Y = i, X = j }, kingPos) 
                { Piece = piece }, board))
                return true;
        }
    }

    return false;
}
```

### 9.3 走法后的将军验证

```csharp
internal static bool IsKingCheckedValidation(Move move, PieceColor side, ChessBoard board)
{
    // 创建棋盘副本，模拟执行走法
    var newBoard = new ChessBoard(board.pieces, board.executedMoves);

    // 特殊情况：王车易位的将军验证
    if (move.Parameter is MoveCastle castle && move.Piece.Color == side)
        return IsKingCheckedWhileCastling(side, board, castle);

    // 特殊情况：吃过路兵的将军验证
    if (move.Parameter is MoveEnPassant enPassant)
        newBoard.Remove(enPassant.CapturedPawnPosition);

    // 如果起始位置和目标位置相同，直接检查当前状态
    if (move.OriginalPosition == move.NewPosition)
        return IsKingChecked(side, newBoard);

    // 在副本上执行走法
    newBoard.executedMoves.Add(move);
    newBoard.DropPieceToNewPosition(new Move(move));
    newBoard.moveIndex = newBoard.executedMoves.Count - 1;

    // 检查执行走法后王是否被将军
    return IsKingChecked(side, newBoard);
}
```

### 9.4 易位时的将军验证

```csharp
private static bool IsKingCheckedWhileCastling(PieceColor side, ChessBoard board, MoveCastle castle)
{
    bool isCheck = false;
    var kingPos = GetKingPosition(side, board);
    
    // 确定移动方向
    short step = (short)(castle.CastleType == CastleType.King ? 1 : -1);

    short i = kingPos.X;
    // 检查王经过的每个位置是否被将军
    while (i < MAX_COLS - 1 && i > 1 && !isCheck)
    {
        isCheck = IsKingCheckedValidation(
            new Move(kingPos, new Position { Y = kingPos.Y, X = i }), 
            side, board);
        i += step;
    }

    return isCheck;
}
```

**易位规则**：王在易位过程中不能经过被攻击的格子，也不能从被将军的位置开始易位。

---

## 10. 王车易位验证 (HasRightToCastle)

### 10.1 易位的前提条件

王车易位必须满足以下条件：
1. 王和参与易位的车都未移动过
2. 王和车之间没有其他棋子
3. 王不能处于被将军状态
4. 王经过的格子不能被对方攻击

### 10.2 代码详解

```csharp
internal static bool HasRightToCastle(PieceColor side, CastleType castleType, ChessBoard board)
{
    var valid = false;

    // 如果从FEN加载，先检查FEN中的易位权利标记
    if (board.LoadedFromFen)
    {
        if (side == PieceColor.White)
        {
            valid = castleType switch
            {
                CastleType.King => board.FenBuilder!.CastleWK,   // 白方王翼易位权利
                CastleType.Queen => board.FenBuilder!.CastleWQ,  // 白方后翼易位权利
                _ => valid
            };
        }
        else if (side == PieceColor.Black)
        {
            valid = castleType switch
            {
                CastleType.King => board.FenBuilder!.CastleBK,   // 黑方王翼易位权利
                CastleType.Queen => board.FenBuilder!.CastleBQ,  // 黑方后翼易位权利
                _ => valid
            };
        }

        // 如果有权利，进一步验证实际走法记录
        if (valid && board.moveIndex >= 0)
            valid = ValidByMoves();
    }
    else
    {
        // 非FEN加载，直接通过走法记录验证
        valid = ValidByMoves();
    }

    return valid;

    // 通过走法记录验证王和车是否移动过
    bool ValidByMoves()
    {
        // 王的初始位置：e1 (4,0) 或 e8 (4,7)
        Position kingpos = new(4, (short)(side == PieceColor.White ? 0 : 7));

        // 车的初始位置
        var rookpos = castleType switch
        {
            CastleType.King => new Position(7, (short)(side == PieceColor.White ? 0 : 7)),  // h1 或 h8
            CastleType.Queen => new Position(0, (short)(side == PieceColor.White ? 0 : 7)), // a1 或 a8
            _ => throw new ChessArgumentException(board, "Invalid Castle type parameter"),
        };

        // 检查车是否还在原位且是车
        var rook = board.pieces[rookpos.Y, rookpos.X];
        
        // 验证：
        // 1. 车存在
        // 2. 是车类型
        // 3. 是己方车
        // 4. 王从未移动过
        // 5. 车从未移动过
        return rook != null
            && rook.Type == PieceType.Rook
            && rook.Color == side
            && !PieceEverMoved(kingpos, board) 
            && !PieceEverMoved(rookpos, board);
    }
}

// 检查棋子是否曾经移动过
private static bool PieceEverMoved(Position piecePos, ChessBoard board)
{
    return board.DisplayedMoves.Any(p => p.OriginalPosition == piecePos);
}
```

---

## 11. 吃过路兵验证 (IsValidEnPassant)

### 11.1 吃过路兵的规则

吃过路兵是国际象棋的特殊规则：
1. 对方的兵从初始位置向前移动两格
2. 你的兵在横向相邻的位置
3. 你可以立即斜向移动你的兵，吃掉对方刚刚移动的兵
4. 必须在对方移动兵后的立即应着，过期作废

### 11.2 代码详解

```csharp
private static bool IsValidEnPassant(Move move, ChessBoard board, short v, short h)
{
    // 规则1：必须是斜向移动一格（尝试吃子）
    if (Math.Abs(v) == 1 && Math.Abs(h) == 1)
    {
        // 获取目标位置后方一格的棋子（即对方刚移动的兵）
        var piece = board.pieces[move.NewPosition.Y - v, move.NewPosition.X];

        // 规则2：后方必须是对方的兵
        if (piece is not null && 
            piece.Color != move.Piece.Color && 
            piece.Type == PieceType.Pawn)
        {
            // 规则3：对方上一步必须是兵从初始位置前进两格
            return LastMoveEnPassantPosition(board) == move.NewPosition;
        }
    }

    return false;
}
```

### 11.3 获取吃过路兵位置

```csharp
internal static Position LastMoveEnPassantPosition(ChessBoard board)
{
    Position pos = new();

    // 如果有走法记录
    if (board.moveIndex >= 0)
    {
        var lastMove = board.DisplayedMoves.Last();

        // 检查上一步是否是兵移动两格
        bool isPawn = lastMove.Piece.Type == PieceType.Pawn;
        bool moving2Tiles = Math.Abs(lastMove.NewPosition.Y - lastMove.OriginalPosition.Y) == 2;

        if (isPawn && moving2Tiles)
        {
            // 计算吃过路兵的目标位置（两格中间）
            pos = new Position
            {
                X = lastMove.NewPosition.X,
                Y = (short)((lastMove.NewPosition.Y + lastMove.OriginalPosition.Y) / 2)
            };
        }
    }
    // 如果从FEN加载，使用FEN中的吃过路兵标记
    else if (board.LoadedFromFen)
    {
        pos = board.FenBuilder!.EnPassant;
    }

    return pos;
}
```

### 11.4 吃过路兵示例

```
白方兵在 d5，黑方兵从 e7 移动到 e5：

   a b c d e f g h
6  . . . . . . . .
5  . . . ♙ ♟ . . .    白方可以在 d5 的兵吃掉 e5 的兵，移动到 e6
4  . . . . . . . .

吃过路兵后：
   a b c d e f g h
6  . . . . ♙ . . .    白方兵到达 e6
5  . . . . . . . .    黑方 e5 的兵被吃掉
```

---

## 12. 走法生成器 (GeneratePositions)

### 12.1 功能概述

`GeneratePositions` 方法用于生成给定棋子的**所有可能位置**（不考虑将军），用于：
1. AI计算
2. 显示可移动位置提示
3. 快速判断是否有合法走法

### 12.2 代码详解

```csharp
private static Position[] GeneratePositions(Position piecePosition, ChessBoard board)
{
    var positions = new List<Position>();

    switch (board[piecePosition]!.Type)
    {
        case var e when e == PieceType.Pawn:
            GeneratePawnPositions(piecePosition, board, positions);
            break;
        case var e when e == PieceType.Rook:
            GenerateRookPositions(piecePosition, board, positions);
            break;
        case var e when e == PieceType.Knight:
            GenerateKnightPositions(piecePosition, board, positions);
            break;
        case var e when e == PieceType.Bishop:
            GenerateBishopPositions(piecePosition, board, positions);
            break;
        case var e when e == PieceType.Queen:
            GenerateRookPositions(piecePosition, board, positions);
            GenerateBishopPositions(piecePosition, board, positions);
            break;
        case var e when e == PieceType.King:
            GenerateKingPositions(piecePosition, board, positions);
            break;
    }

    return positions.ToArray();
}
```

### 12.3 王的走法生成

```csharp
private static void GenerateKingPositions(Position piecePosition, ChessBoard board, List<Position> positions)
{
    // 王可以向周围8个方向移动一格
    int fromX = Math.Max(0, piecePosition.X - 1);
    int toX = Math.Min(7, piecePosition.X + 1);
    int fromY = Math.Max(0, piecePosition.Y - 1);
    int toY = Math.Min(7, piecePosition.Y + 1);

    for (int x = fromX; x <= toX; x++)
    for (int y = fromY; y <= toY; y++)
        if (x != piecePosition.X || y != piecePosition.Y)
            if (board[x, y] == null || board[x, y]!.Color != board[piecePosition]!.Color)
                positions.Add(new Position((short)x, (short)y));

    // 添加易位选项
    if (piecePosition.Y % 7 == 0 && piecePosition.X == 4)  // 王在初始位置
    {
        // 后翼易位检查
        var rook = board[0, piecePosition.Y];
        if (board[1, piecePosition.Y] is null && 
            board[2, piecePosition.Y] is null && 
            board[3, piecePosition.Y] is null)
            if (rook?.Type == PieceType.Rook && rook.Color == board[piecePosition]!.Color)
            {
                positions.Add(new Position() { X = 2, Y = piecePosition.Y });
                if (!board.StandardiseCastlingPositions)
                    positions.Add(new Position() { X = 0, Y = piecePosition.Y });
            }

        // 王翼易位检查
        rook = board[7, piecePosition.Y];
        if (board[5, piecePosition.Y] is null && 
            board[6, piecePosition.Y] is null)
            if (rook?.Type == PieceType.Rook && rook.Color == board[piecePosition]!.Color)
            {
                positions.Add(new Position() { X = 6, Y = piecePosition.Y });
                if (!board.StandardiseCastlingPositions)
                    positions.Add(new Position() { X = 7, Y = piecePosition.Y });
            }
    }
}
```

### 12.4 马的走法生成

```csharp
private static void GenerateKnightPositions(Position piecePosition, ChessBoard board, List<Position> positions)
{
    short x = piecePosition.X;
    short y = piecePosition.Y;
    
    // 马的8个可能位置
    Position[] possiblePositions =
    {
        new((short)(x + 2), (short)(y + 1)),
        new((short)(x + 2), (short)(y - 1)),
        new((short)(x - 2), (short)(y + 1)),
        new((short)(x - 2), (short)(y - 1)),
        new((short)(x + 1), (short)(y + 2)),
        new((short)(x + 1), (short)(y - 2)),
        new((short)(x - 1), (short)(y + 2)),
        new((short)(x - 1), (short)(y - 2))
    };

    foreach (var pos in possiblePositions)
    {
        // 检查是否在棋盘内
        if (pos.X >= 0 && pos.X < 8 && pos.Y >= 0 && pos.Y < 8)
        {
            // 检查目标位置是否为空或是对方棋子
            if (board[pos] is null || board[pos]!.Color != board[piecePosition]!.Color)
            {
                positions.Add(pos);
            }
        }
    }
}
```

### 12.5 直线移动棋子的走法生成（车、象、后）

```csharp
// 象的4个对角线方向
private static readonly List<(short x, short y)> BishopDirections = new() 
{ 
    (1, 1), (1, -1), (-1, 1), (-1, -1) 
};

// 车的4个直线方向
private static readonly List<(short x, short y)> RookDirections = new() 
{ 
    (0, 1), (1, 0), (0, -1), (-1, 0) 
};

private static void GenerateBishopPositions(Position piecePosition, ChessBoard board, List<Position> positions)
{
    AddPositionsInDirections(BishopDirections, piecePosition, board, positions);
}

private static void GenerateRookPositions(Position piecePosition, ChessBoard board, List<Position> positions)
{
    AddPositionsInDirections(RookDirections, piecePosition, board, positions);
}

// 通用方法：沿指定方向生成所有可能位置
private static void AddPositionsInDirections(
    List<(short x, short y)> directions, 
    Position piecePosition, 
    ChessBoard board, 
    List<Position> positions)
{
    foreach (var direction in directions)
    {
        var currentPosition = (
            x: (short)(piecePosition.X + direction.x), 
            y: (short)(piecePosition.Y + direction.y)
        );

        // 沿方向一直前进，直到出界或遇到棋子
        while (currentPosition.y >= 0 && currentPosition.y < MAX_ROWS && 
               currentPosition.x >= 0 && currentPosition.x < MAX_COLS)
        {
            if (board.pieces[currentPosition.y, currentPosition.x] != null)
            {
                // 遇到对方棋子，可以吃，但不能再前进
                if (board.pieces[currentPosition.y, currentPosition.x]!.Color != 
                    board[piecePosition]!.Color)
                    positions.Add(new Position() { X = currentPosition.x, Y = currentPosition.y });
                
                break;  // 遇到任何棋子都停止
            }

            // 空格，可以移动
            positions.Add(new Position() { X = currentPosition.x, Y = currentPosition.y });

            // 继续沿方向前进
            currentPosition.y += direction.y;
            currentPosition.x += direction.x;
        }
    }
}
```

---

## 13. 总结

### 13.1 代码设计亮点

1. **模块化设计**：每个棋子的验证逻辑独立，便于维护和测试
2. **代码复用**：后复用车和象的验证，减少重复代码
3. **防御式编程**：大量空值检查和边界检查
4. **状态隔离**：使用棋盘副本来验证走法，避免污染实际棋盘状态
5. **职责分离**：验证逻辑和执行逻辑分离

### 13.2 关键算法思想

1. **将军检测**：模拟执行走法后检查王是否被攻击
2. **路径检查**：沿移动方向逐格检查障碍物
3. **方向向量**：使用 (x, y) 向量表示移动方向，代码简洁
4. **位运算思想**：虽然没有直接使用位运算，但坐标计算体现了类似思想

### 13.3 学习建议

1. **从简单到复杂**：先理解马和王的验证，再理解车、象，最后理解兵的特殊规则
2. **画图辅助**：在纸上画出棋盘，手动模拟代码逻辑
3. **调试跟踪**：在关键方法处打断点，观察变量变化
4. **单元测试**：为每个验证方法编写测试用例，加深理解

---

*文档生成时间：2026-05-14*
*基于项目代码版本：Unity Multiplayer Chess Cloud Code Sample*
