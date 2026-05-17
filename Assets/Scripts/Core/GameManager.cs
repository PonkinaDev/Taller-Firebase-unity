using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TowerDefense.Analytics;
using TowerDefense.Services;

namespace TowerDefense.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scene References")]
        [SerializeField] private EnemySpawner spawnerRef;
        [SerializeField] private BaseDefense baseRef;

        [Header("Score")]
        [SerializeField] private int scorePerKill = 10;
        [SerializeField] private int scorePerWave = 50;

        public int CurrentScore { get; private set; }
        public bool GameOver { get; private set; }

        private bool _resultsSaved;

        private int _currentWave;
        private int _enemiesThisWave;
        private int _killsThisWave;

        // ─────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            string playerName =
                PlayerPrefs.GetString("PlayerName", "Anonymous");

            AnalyticsCollector.Instance.BeginSession(playerName);

            spawnerRef.StartSpawning();
        }

        public void OnWaveStarted(int waveNumber, int enemiesInWave)
        {
            _currentWave = waveNumber;
            _enemiesThisWave = enemiesInWave;
            _killsThisWave = 0;

            Debug.Log(
                $"Wave {_currentWave} started with {_enemiesThisWave} enemies."
            );
        }


        public void OnEnemyKilled()
        {
            if (GameOver)
                return;

            CurrentScore += scorePerKill;

            _killsThisWave++;

            AnalyticsCollector.Instance.RecordEnemyKilled();

            UIManager.Instance?.UpdateScore(CurrentScore);


            if (_killsThisWave >= _enemiesThisWave)
            {
                CurrentScore += scorePerWave;

                AnalyticsCollector.Instance.EndWave(
                    _enemiesThisWave
                );

                AnalyticsCollector.Instance.BeginWave(
                    _currentWave + 1
                );

                UIManager.Instance?.UpdateScore(CurrentScore);

                UIManager.Instance?.ShowWaveMessage(_currentWave);

                Debug.Log($"Wave {_currentWave} completed.");
            }
        }

        public void OnBaseReached()
        {
            if (GameOver)
                return;

            GameOver = true;

            spawnerRef.StopSpawning();

            StartCoroutine(
                EndGameRoutine(playerWon: false)
            );
        }

        private IEnumerator EndGameRoutine(bool playerWon)
        {
            var session =
                AnalyticsCollector.Instance.BuildSessionData(
                    CurrentScore,
                    playerWon
                );

            _resultsSaved = false;

            StartCoroutine(
                SaveToFirebase(session)
            );

            UIManager.Instance?.ShowGameOver(playerWon);

            yield return new WaitForSeconds(1.5f);

            float waited = 0f;

            while (!_resultsSaved && waited < 5f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            PlayerPrefs.SetInt(
                "FinalScore",
                CurrentScore
            );

            PlayerPrefs.SetInt(
                "WavesCompleted",
                session.wavesCompleted
            );

            PlayerPrefs.SetFloat(
                "Accuracy",
                session.accuracy
            );

            SceneManager.LoadScene("Results");
        }

        private IEnumerator SaveToFirebase(
            TowerDefense.Data.SessionData session
        )
        {
            var saveSession =
                FirebaseService.Instance.SaveSessionAsync(
                    session
                );

            var saveHighscore =
                FirebaseService.Instance.SaveHighscoreAsync(
                    session.playerName,
                    session.finalScore
                );

            yield return new WaitUntil(
                () =>
                    saveSession.IsCompleted &&
                    saveHighscore.IsCompleted
            );

            _resultsSaved = true;
        }
    }
}