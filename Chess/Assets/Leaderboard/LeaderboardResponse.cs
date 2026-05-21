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
        public string created_at;
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
}
