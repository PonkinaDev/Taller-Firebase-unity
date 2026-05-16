using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Firestore;
using TowerDefense.Data;

namespace TowerDefense.Services
{
    /// <summary>
    /// Único responsable de hablar con Firebase Firestore.
    /// SRP: solo persiste / lee datos. No conoce la lógica de juego.
    /// OCP: agregar nuevas colecciones no modifica los métodos existentes.
    /// </summary>
    public class FirebaseService : MonoBehaviour
    {
        public static FirebaseService Instance { get; private set; }

        private FirebaseFirestore _db;
        public  bool IsReady { get; private set; }

        // ── Lifecycle ────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
            yield return new WaitUntil(() => dependencyTask.IsCompleted);

            if (dependencyTask.Result == DependencyStatus.Available)
            {
                _db     = FirebaseFirestore.DefaultInstance;
                IsReady = true;
                Debug.Log("[Firebase] Firestore listo.");
            }
            else
            {
                Debug.LogError($"[Firebase] Error de dependencias: {dependencyTask.Result}");
            }
        }

        // ── Escritura de sesión ──────────────────────────────────────────

        /// <summary>
        /// Guarda la sesión completa en la colección <c>sessions</c>.
        /// Llámalo solo al finalizar la partida (no durante el juego).
        /// </summary>
        public async Task SaveSessionAsync(SessionData data)
        {
            if (!IsReady) { Debug.LogWarning("[Firebase] No está listo."); return; }

            var doc = new Dictionary<string, object>
            {
                ["sessionId"]           = data.sessionId,
                ["playerName"]          = data.playerName,
                ["startTimestamp"]      = data.startTimestamp,
                ["durationSeconds"]     = data.durationSeconds,
                ["finalScore"]          = data.finalScore,
                ["playerWon"]           = data.playerWon,
                ["enemiesSpawned"]      = data.enemiesSpawned,
                ["enemiesKilled"]       = data.enemiesKilled,
                ["shotsFired"]          = data.shotsFired,
                ["shotsHit"]            = data.shotsHit,
                ["accuracy"]            = data.accuracy,
                ["wavesCompleted"]      = data.wavesCompleted,
                ["avgReactionTimeSec"]  = data.avgReactionTimeSec,
                ["avgCannonAngle"]      = data.avgCannonAngle,
            };

            // Sub-colección de oleadas
            try
            {
                await _db.Collection("sessions").Document(data.sessionId).SetAsync(doc);

                foreach (var wave in data.waveRecords)
                {
                    var waveDoc = new Dictionary<string, object>
                    {
                        ["waveNumber"]           = wave.waveNumber,
                        ["enemiesInWave"]        = wave.enemiesInWave,
                        ["enemiesKilledInWave"]  = wave.enemiesKilledInWave,
                        ["waveDurationSec"]      = wave.waveDurationSec,
                    };
                    await _db.Collection("sessions").Document(data.sessionId)
                             .Collection("waves").Document($"wave_{wave.waveNumber}")
                             .SetAsync(waveDoc);
                }
                Debug.Log($"[Firebase] Sesión {data.sessionId} guardada.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Firebase] SaveSession error: {e.Message}");
            }
        }

        // ── Escritura de highscore ───────────────────────────────────────

        /// <summary>Upsert del puntaje del jugador en la colección <c>highscores</c>.</summary>
        public async Task SaveHighscoreAsync(string playerName, int score)
        {
            if (!IsReady) return;

            // Usamos el nombre como ID para facilitar el upsert
            string docId = playerName.ToLower().Trim().Replace(" ", "_");
            var docRef   = _db.Collection("highscores").Document(docId);

            try
            {
                var snap = await docRef.GetSnapshotAsync();
                if (snap.Exists)
                {
                    int existing = snap.GetValue<int>("score");
                    if (score <= existing) return; // No sobreescribir si es peor
                }

                await docRef.SetAsync(new Dictionary<string, object>
                {
                    ["playerName"] = playerName,
                    ["score"]      = score,
                    ["timestamp"]  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                Debug.Log($"[Firebase] Highscore de {playerName} actualizado: {score}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Firebase] SaveHighscore error: {e.Message}");
            }
        }

        // ── Lectura de ranking ───────────────────────────────────────────

        /// <summary>Devuelve los top N puntajes ordenados de mayor a menor.</summary>
        public async Task<List<HighscoreEntry>> GetHighscoresAsync(int limit = 10)
        {
            var result = new List<HighscoreEntry>();
            if (!IsReady) return result;

            try
            {
                var query = await _db.Collection("highscores")
                                     .OrderByDescending("score")
                                     .Limit(limit)
                                     .GetSnapshotAsync();

                foreach (var doc in query.Documents)
                {
                    result.Add(new HighscoreEntry
                    {
                        playerName = doc.GetValue<string>("playerName"),
                        score      = doc.GetValue<int>("score"),
                        timestamp  = doc.GetValue<long>("timestamp")
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Firebase] GetHighscores error: {e.Message}");
            }
            return result;
        }

        // ── Lectura de sesiones para dashboard ──────────────────────────

        /// <summary>Devuelve las últimas N sesiones para visualizar en el dashboard.</summary>
        public async Task<List<SessionData>> GetRecentSessionsAsync(int limit = 50)
        {
            var result = new List<SessionData>();
            if (!IsReady) return result;

            try
            {
                var query = await _db.Collection("sessions")
                                     .OrderByDescending("startTimestamp")
                                     .Limit(limit)
                                     .GetSnapshotAsync();

                foreach (var doc in query.Documents)
                {
                    var s = new SessionData
                    {
                        sessionId        = doc.GetValue<string>("sessionId"),
                        playerName       = doc.GetValue<string>("playerName"),
                        startTimestamp   = doc.GetValue<long>("startTimestamp"),
                        durationSeconds  = doc.GetValue<float>("durationSeconds"),
                        finalScore       = doc.GetValue<int>("finalScore"),
                        playerWon        = doc.GetValue<bool>("playerWon"),
                        enemiesSpawned   = doc.GetValue<int>("enemiesSpawned"),
                        enemiesKilled    = doc.GetValue<int>("enemiesKilled"),
                        shotsFired       = doc.GetValue<int>("shotsFired"),
                        shotsHit         = doc.GetValue<int>("shotsHit"),
                        accuracy         = doc.GetValue<float>("accuracy"),
                        wavesCompleted   = doc.GetValue<int>("wavesCompleted"),
                        avgReactionTimeSec = doc.GetValue<float>("avgReactionTimeSec"),
                        avgCannonAngle   = doc.GetValue<float>("avgCannonAngle"),
                    };
                    result.Add(s);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Firebase] GetRecentSessions error: {e.Message}");
            }
            return result;
        }
    }
}
