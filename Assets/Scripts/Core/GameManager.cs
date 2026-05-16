using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TowerDefense.Analytics;
using TowerDefense.Services;

namespace TowerDefense.Core
{
    /// <summary>
    /// Orquesta el flujo completo de la partida.
    /// SRP: coordina subsistemas, no los implementa.
    /// DIP: depende de abstracciones (AnalyticsCollector, FirebaseService).
    /// 
    /// INSPECTOR ──────────────────────────────────────────────
    ///   • Asignar en la escena "Game":
    ///     - spawnerRef   → GameObject con EnemySpawner
    ///     - baseRef      → GameObject con BaseDefense
    ///     - scorePerKill → puntos por enemigo (ej. 10)
    ///     - scorePerWave → puntos por oleada completada (ej. 50)
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Referencias de escena")]
        [SerializeField] private EnemySpawner spawnerRef;
        [SerializeField] private BaseDefense   baseRef;

        [Header("Puntuación")]
        [SerializeField] private int scorePerKill = 10;
        [SerializeField] private int scorePerWave = 50;

        // Estado público
        public int   CurrentScore  { get; private set; }
        public bool  GameOver      { get; private set; }

        private bool _resultsSaved;

        // ── Lifecycle ────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            string pName = PlayerPrefs.GetString("PlayerName", "Anónimo");
            AnalyticsCollector.Instance.BeginSession(pName);
            spawnerRef.StartSpawning();
        }

        // ── Eventos de juego (llamados por otros scripts) ────────────────

        /// <summary>Llamado por cada Enemy al ser destruido.</summary>
        public void OnEnemyKilled()
        {
            if (GameOver) return;
            CurrentScore += scorePerKill;
            AnalyticsCollector.Instance.RecordEnemyKilled();
            UIManager.Instance?.UpdateScore(CurrentScore);
        }

        /// <summary>Llamado por EnemySpawner al completar una oleada.</summary>
        public void OnWaveCompleted(int waveNumber, int enemiesInWave)
        {
            if (GameOver) return;
            CurrentScore += scorePerWave;
            AnalyticsCollector.Instance.EndWave(enemiesInWave);
            AnalyticsCollector.Instance.BeginWave(waveNumber + 1);
            UIManager.Instance?.UpdateScore(CurrentScore);
            UIManager.Instance?.ShowWaveMessage(waveNumber);
        }

        /// <summary>Llamado por BaseDefense cuando un enemigo la toca.</summary>
        public void OnBaseReached()
        {
            if (GameOver) return;
            GameOver = true;
            spawnerRef.StopSpawning();
            StartCoroutine(EndGameRoutine(playerWon: false));
        }

        // ── Finalización ─────────────────────────────────────────────────
        private IEnumerator EndGameRoutine(bool playerWon)
        {
            // 1. Construir snapshot
            var session = AnalyticsCollector.Instance.BuildSessionData(CurrentScore, playerWon);

            // 2. Guardar en Firebase (async; esperamos sin bloquear el hilo)
            _resultsSaved = false;
            StartCoroutine(SaveToFirebase(session));

            // 3. Mostrar breve animación de fin de partida
            UIManager.Instance?.ShowGameOver(playerWon);
            yield return new WaitForSeconds(1.5f);

            // 4. Esperar a que Firebase termine (máx 5 seg)
            float waited = 0f;
            while (!_resultsSaved && waited < 5f) { waited += Time.deltaTime; yield return null; }

            // 5. Guardar datos para la escena de resultados
            PlayerPrefs.SetInt("FinalScore", CurrentScore);
            PlayerPrefs.SetInt("WavesCompleted", session.wavesCompleted);
            PlayerPrefs.SetFloat("Accuracy", session.accuracy);

            // 6. Ir a resultados
            SceneManager.LoadScene("Results");
        }

        private IEnumerator SaveToFirebase(TowerDefense.Data.SessionData session)
        {
            var saveSession = FirebaseService.Instance.SaveSessionAsync(session);
            var saveHighscore = FirebaseService.Instance.SaveHighscoreAsync(
                session.playerName, session.finalScore);

            yield return new WaitUntil(() => saveSession.IsCompleted && saveHighscore.IsCompleted);
            _resultsSaved = true;
        }
    }
}
