using System;
using System.Collections.Generic;

namespace Chess.Leaderboard
{
    [Serializable]
    public class ScoreSubmitResponse
    {
        public bool success;
        public string message;
        public ScoreSubmitData data;
    }

    [Serializable]
    public class ScoreSubmitData
    {
        public int rank;
    }

    [Serializable]
    public class LeaderboardResponse
    {
        public bool success;
        public List<ScoreEntry> data;
    }

    [Serializable]
    public class ScoreEntry
    {
        public int rank;
        public string player_name;
        public int score;
        public string game_mode;
        public string created_at;
    }

    [Serializable]
    public class ModesResponse
    {
        public bool success;
        public List<string> data;
    }

    [Serializable]
    public class AllModesLeaderboardResponse
    {
        public bool success;
        public List<ModeLeaderboard> data;
    }

    [Serializable]
    public class ModeLeaderboard
    {
        public string game_mode;
        public List<ScoreEntry> entries;
    }

    [Serializable]
    public class PlayerRankResponse
    {
        public bool success;
        public string message;
        public PlayerRankData data;
    }

    [Serializable]
    public class PlayerRankData
    {
        public int rank;
        public string player_name;
        public int score;
    }

    [Serializable]
    public class DeleteResponse
    {
        public bool success;
        public string message;
    }

    [Serializable]
    public class PingResponse
    {
        public string status;
        public string time;
    }

    [Serializable]
    public class PlayerRenameResponse
    {
        public bool success;
        public string message;
    }
}
