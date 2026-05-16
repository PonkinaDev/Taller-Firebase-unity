using UnityEngine;
using TowerDefense.Analytics;

namespace TowerDefense.Core
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float lifetimeSec = 5f;

        private Vector2 _direction;
        private float   _speed;
        private int     _instanceId;

        public void Initialize(Vector2 direction, float speed)
        {
            _direction  = direction.normalized;
            _speed      = speed;
            _instanceId = gameObject.GetInstanceID();
            Destroy(gameObject, lifetimeSec);
        }

        private void Update()
        {
            transform.Translate(_direction * _speed * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Enemy")) return;

            AnalyticsCollector.Instance?.RecordShotHit(_instanceId);
            other.GetComponent<Enemy>()?.TakeDamage();
            Destroy(gameObject);
        }
    }
}
