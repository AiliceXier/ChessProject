# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Server-authoritative asynchronous multiplayer chess game using Unity Gaming Services (UGS). No dedicated game server — game state is stored in Cloud Save and synchronized via push messages to clients. Move validation runs server-side in Cloud Code using the [Gera Chess Library](https://github.com/Geras1mleo/Chess).

## Project Structure

```
Chess/                   # Unity client project — open this folder as the Unity project
├── Assets/
│   ├── Player.cs        # Main game controller: init, lobby, moves, board rendering, input
│   ├── Piece.cs         # Simple component storing piece initial position
│   ├── Resources/       # Piece prefabs (Bishop, King, Knight, Pawn, Queen, Rook, Board)
│   ├── Scenes/ChessDemo.unity  # Main/only scene
│   ├── Setup/ChessCloudCode.ccmr  # Cloud Code module reference
│   └── Setup/EloRatings.lb       # Leaderboard config
ChessCloudCode/          # Cloud Code server module (C# .NET project)
├── Chess.cs             # Cloud Code endpoints: HostGame, JoinGame, MakeMove, Resign
├── ModuleConfig.cs      # DI setup: IGameApiClient, IPushClient, Random
└── ChessCloudCode.sln   # Solution for deploying Cloud Code module
```

## Build & Deploy

```bash
# Deploy Cloud Code module via UGS CLI
ugs deploy ChessCloudCode/ChessCloudCode.sln --services cloud-code-modules

# Deploy leaderboard
ugs deploy Chess/Assets/Setup/EloRatings.lb --services leaderboards

# Or use the deploy script
./deploy.sh
```

In Unity Editor: open `Chess/` as a Unity project, open `ChessDemo.unity`, deploy via `Services > Deployment` (2022+) or `Window > Deployment` (2021-).

.NET SDK must be installed and configured at `Edit > Preferences… > Cloud Code > .NET path`.

## Key Architecture Details

### Data Flow

1. **HostGame** creates a Lobby (max 2 players), saves initial FEN + whitePlayerId to Cloud Save (keyed by lobby ID)
2. **JoinGame** joins the lobby, randomly assigns colors, pushes `opponentJoined` message to the creator, saves full player assignments
3. **MakeMove** loads game state from Cloud Save, validates turn + move legality via ChessBoard library, saves new FEN, pushes `boardUpdated` to opponent
4. **Resign** loads state, calls `chessBoard.Resign()`, saves new FEN, pushes `boardUpdated` to opponent
5. Push messages are the real-time backbone — client subscribes via `SubscribeToPlayerMessagesAsync` on startup

### Board State

Board state is a FEN string (e.g., `rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1`). Client parses FEN → coordinate dictionary → instantiates piece GameObjects. Piece prefabs are Resources-loaded and cached in `_prefabs` dictionary.

### Coordinate System

- X: 0-7 (columns a-h), Y/Z: 0-7 (rows 1-8)
- `PosToFen(Vector3)`: `(char)(pos.x + 97)` + `((char)pos.z + 1)` → e.g., "e4"
- White pieces face 0°, black pieces face 180° on the 3D board

### Color Assignment

Random (`_rng.NextDouble() >= 0.5`) — no ELO-based matching. The joining player's color is randomized and the creator gets the opposite. Camera pivots 180° for black players (`SetPov()`).

### Rejoin Logic

If `JoinLobbyByCode` throws (e.g., player already in lobby), the server checks `GetJoinedLobbiesAsync`. If found, calls `Rejoin()` which reads saved Cloud Save state and returns it to the client — handles page refresh / disconnect.

### Input Handling

Uses Unity Input System (`PlayerInteract` via `InputAction.CallbackContext`). Raycast from camera → detect Board or piece click → select/move. Only allows interaction when `_currentSession != null` (in a game).

## Key Commands

```bash
# Run two clients for local testing: one in Editor, one via Build and Run
# File > Build and Run (creates standalone build for second player)

# Debug Cloud Code logs in Unity Dashboard
```

## Notes

- The `ElovRatings` leaderboard (spelled "Elov" in some config — watch for this inconsistency) uses initial score 1500, K=30
- Client code references `WebSocketSharp` but doesn't appear to use it directly (likely a transitive dependency)
- There's a `Chess.inputactions` Input System asset defining the player interaction bindings
- Lobby codes may have invisible characters — client sanitizes with `Regex.Replace(..., @"\s", "").Replace("​", "")`
