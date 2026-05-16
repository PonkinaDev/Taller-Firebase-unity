using UnityEngine;
using TowerDefense.Analytics;
using TowerDefense.Core;

namespace TowerDefense.Core
{
    public class Enemy : MonoBehaviour
    {
        [SerializeField] private float speed = 2f;

        private void Start()
        {
            AnalyticsCollector.Instance?.RecordEnemySpawned();
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.GameOver) return;
            transform.Translate(Vector3.left * speed * Time.deltaTime);
        }

        public void TakeDamage()
        {
            GameManager.Instance?.OnEnemyKilled();
            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Base"))
            {
                GameManager.Instance?.OnBaseReached();
                Destroy(gameObject);
            }
        }
    }
}
