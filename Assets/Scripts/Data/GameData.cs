using System;
using System.Collections.Generic;

namespace TowerDefense.Data
{
    /// <summary>
    /// Inmutable snapshot de una sesión completa.
    /// Se construye al finalizar el juego y se envía a Firebase.
    /// </summary>
    [Serializable]
    public class SessionData
    {
        // ── Identificadores ─────────────────────────────────────────────
        public string sessionId;          // GUID generado automáticamente
        public string playerName;

        // ── Tiempo ──────────────────────────────────────────────────────
        public long   startTimestamp;     // Unix ms al iniciar
        public float  durationSeconds;

        // ── Resultado ────────────────────────────────────────────────────
        public int    finalScore;
        public bool   playerWon;         // false → enemigo llegó a la base

        // ── Métricas de mecánica (≥4 requeridas) ────────────────────────
        public int    enemiesSpawned;
        public int    enemiesKilled;
        public int    shotsFired;
        public int    shotsHit;
        public float  accuracy;          // shotsHit / shotsFired * 100
        public int    wavesCompleted;

        // ── Métrica de comportamiento no trivial ────────────────────────
        // Tiempo promedio (segundos) entre disparo y confirmación de impacto.
        // Mide la "agudeza de puntería" del jugador con un margen de reacción.
        public float  avgReactionTimeSec;

        // ── Métricas de uso de pantalla ──────────────────────────────────
        // Ángulo promedio del cañón (grados). Revela si el jugador dispara
        // temprano (ángulos bajos) o espera a que los enemigos se acerquen.
        public float  avgCannonAngle;

        // ── Registro por oleada ──────────────────────────────────────────
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

    /// <summary>Entrada del ranking global.</summary>
    [Serializable]
    public class HighscoreEntry
    {
        public string playerName;
        public int    score;
        public long   timestamp;
    }
}
