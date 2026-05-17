using System.Collections;
using UnityEngine;

namespace TowerDefense.Core
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GameObject enemyPrefab;

        [Header("Waves")]
        [SerializeField] private float spawnInterval = 2f;
        [SerializeField] private int enemiesPerWave = 5;
        [SerializeField] private float wavePause = 3f;
        [SerializeField] private int maxWaves = 0;

        private int _currentWave;
        private bool _spawning;

        public void StartSpawning()
        {
            _spawning = true;

            StartCoroutine(SpawnRoutine());
        }

        public void StopSpawning()
        {
            _spawning = false;

            StopAllCoroutines();
        }

        private IEnumerator SpawnRoutine()
        {
            while (_spawning)
            {
                _currentWave++;

                if (
                    maxWaves > 0 &&
                    _currentWave > maxWaves
                )
                {
                    yield break;
                }

                GameManager.Instance?.OnWaveStarted(
                    _currentWave,
                    enemiesPerWave
                );

                for (int i = 0; i < enemiesPerWave; i++)
                {
                    if (!_spawning)
                    {
                        yield break;
                    }

                    Instantiate(
                        enemyPrefab,
                        transform.position,
                        Quaternion.identity
                    );

                    yield return new WaitForSeconds(
                        spawnInterval
                    );
                }

                yield return new WaitForSeconds(
                    wavePause
                );

                enemiesPerWave =
                    Mathf.RoundToInt(
                        enemiesPerWave * 1.2f
                    );

                spawnInterval =
                    Mathf.Max(
                        0.5f,
                        spawnInterval * 0.9f
                    );
            }
        }
    }
}