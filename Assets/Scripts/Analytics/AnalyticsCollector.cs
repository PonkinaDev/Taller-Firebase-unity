using System;
using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Data;

namespace TowerDefense.Analytics
{
    /// <summary>
    /// Recolecta silenciosamente todas las métricas durante la partida.
    /// No escribe a Firebase; solo acumula estado local.
    /// Principio: Responsabilidad Única (SRP) — solo mide, no persiste.
    /// </summary>
    public class AnalyticsCollector : MonoBehaviour
    {
        // ── Singleton ligero (solo dentro de la escena de juego) ─────────
        public static AnalyticsCollector Instance { get; private set; }

        // ── Estado interno ───────────────────────────────────────────────
        private string  _sessionId;
        private string  _playerName;
        private long    _startTimestamp;
        private float   _sessionStart;

        private int     _enemiesSpawned;
        private int     _enemiesKilled;
        private int     _shotsFired;
        private int     _shotsHit;
        private int     _wavesCompleted;

        // Para tiempo de reacción: momento en que se disparó cada proyectil
        private readonly Dictionary<int, float> _shotFireTimes = new();
        private float   _totalReactionTime;
        private int     _reactionSamples;

        // Para ángulo promedio del cañón
        private float   _angleAccumulator;
        private int     _angleSamples;

        // Registro por oleada
        private readonly List<WaveRecord> _waveRecords = new();
        private WaveRecord _currentWave;

        // ── Inicialización ───────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>Llama esto desde GameManager al empezar la partida.</summary>
        public void BeginSession(string playerName)
        {
            _sessionId       = Guid.NewGuid().ToString();
            _playerName      = playerName;
            _startTimestamp  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _sessionStart    = Time.time;

            _enemiesSpawned = _enemiesKilled = _shotsFired = _shotsHit = _wavesCompleted = 0;
            _totalReactionTime = _angleAccumulator = 0f;
            _reactionSamples = _angleSamples = 0;
            _shotFireTimes.Clear();
            _waveRecords.Clear();

            BeginWave(1);
        }

        // ── Eventos de juego ─────────────────────────────────────────────

        public void RecordEnemySpawned()       => _enemiesSpawned++;

        public void RecordEnemyKilled()
        {
            _enemiesKilled++;
            _currentWave.enemiesKilledInWave++;
        }

        /// <param name="projectileId">ID único del proyectil (GetInstanceID).</param>
        /// <param name="cannonAngleDeg">Ángulo actual del cañón en grados.</param>
        public void RecordShotFired(int projectileId, float cannonAngleDeg)
        {
            _shotsFired++;
            _shotFireTimes[projectileId] = Time.time;

            _angleAccumulator += Mathf.Abs(cannonAngleDeg);
            _angleSamples++;
        }

        /// <param name="projectileId">ID del proyectil que impactó.</param>
        public void RecordShotHit(int projectileId)
        {
            _shotsHit++;
            if (_shotFireTimes.TryGetValue(projectileId, out float fireTime))
            {
                _totalReactionTime += Time.time - fireTime;
                _reactionSamples++;
                _shotFireTimes.Remove(projectileId);
            }
        }

        public void BeginWave(int waveNumber)
        {
            _currentWave = new WaveRecord
            {
                waveNumber    = waveNumber,
                waveDurationSec = Time.time
            };
        }

        public void EndWave(int enemiesInWave)
        {
            _wavesCompleted++;
            _currentWave.enemiesInWave      = enemiesInWave;
            _currentWave.waveDurationSec    = Time.time - _currentWave.waveDurationSec;
            _waveRecords.Add(_currentWave);
        }

        // ── Construcción del snapshot final ─────────────────────────────

        /// <summary>
        /// Genera el snapshot inmutable de la sesión.
        /// Llamado por GameManager al terminar la partida.
        /// </summary>
        public SessionData BuildSessionData(int finalScore, bool playerWon)
        {
            float duration = Time.time - _sessionStart;
            float acc      = _shotsFired > 0 ? (float)_shotsHit / _shotsFired * 100f : 0f;
            float avgReact = _reactionSamples > 0 ? _totalReactionTime / _reactionSamples : 0f;
            float avgAngle = _angleSamples > 0 ? _angleAccumulator / _angleSamples : 0f;

            return new SessionData
            {
                sessionId        = _sessionId,
                playerName       = _playerName,
                startTimestamp   = _startTimestamp,
                durationSeconds  = duration,
                finalScore       = finalScore,
                playerWon        = playerWon,
                enemiesSpawned   = _enemiesSpawned,
                enemiesKilled    = _enemiesKilled,
                shotsFired       = _shotsFired,
                shotsHit         = _shotsHit,
                accuracy         = acc,
                wavesCompleted   = _wavesCompleted,
                avgReactionTimeSec = avgReact,
                avgCannonAngle   = avgAngle,
                waveRecords      = new List<WaveRecord>(_waveRecords)
            };
        }
    }
}
