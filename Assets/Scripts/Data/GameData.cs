using System;
using System.Collections.Generic;

namespace TowerDefense.Data
{
    [Serializable]
    public class SessionData
    {
        public string sessionId;          
        public string playerName;

        public long   startTimestamp;     
        public float  durationSeconds;

        public int    finalScore;
        public bool   playerWon;         

        public int    enemiesSpawned;
        public int    enemiesKilled;
        public int    shotsFired;
        public int    shotsHit;
        public float  accuracy;          
        public int    wavesCompleted;

        public float  avgReactionTimeSec;

        public float  avgCannonAngle;

        public List<WaveRecord> waveRecords = new();
    }

    [Serializable]
    public class WaveRecord
    {
        public int   waveNumber;
        public int   enemiesInWave;
        public int   enemiesKilledInWave;
        public float waveDurationSec;
    }

    [Serializable]
    public class HighscoreEntry
    {
        public string playerName;
        public int    score;
        public long   timestamp;
    }
}
