using System;
using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Data;

namespace TowerDefense.Analytics
{
    public class AnalyticsCollector : MonoBehaviour
    {
        public static AnalyticsCollector Instance { get; private set; }

        private string  _sessionId;
        private string  _playerName;
        private long    _startTimestamp;
        private float   _sessionStart;

        private int     _enemiesSpawned;
        private int     _enemiesKilled;
        private int     _shotsFired;
        private int     _shotsHit;
        private int     _wavesCompleted;

        private readonly Dictionary<int, float> _shotFireTimes = new();
        private float   _totalReactionTime;
        private int     _reactionSamples;

        private float   _angleAccumulator;
        private int     _angleSamples;

        private readonly List<WaveRecord> _waveRecords = new();
        private WaveRecord _currentWave;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
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

        public void RecordEnemySpawned()       => _enemiesSpawned++;

        public void RecordEnemyKilled()
        {
            _enemiesKilled++;
            _currentWave.enemiesKilledInWave++;
        }
        public void RecordShotFired(int projectileId, float cannonAngleDeg)
        {
            _shotsFired++;
            _shotFireTimes[projectileId] = Time.time;

            _angleAccumulator += Mathf.Abs(cannonAngleDeg);
            _angleSamples++;
        }

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
