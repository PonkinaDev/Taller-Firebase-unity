using UnityEngine;
using TowerDefense.Analytics;

namespace TowerDefense.Core
{
    /// <summary>
    /// Controla la mira y el disparo del cañón montado sobre la pirámide.
    /// El cañón NO puede apuntar hacia la izquierda (< minAngle).
    ///
    /// INSPECTOR ──────────────────────────────────────────────────────────
    ///   • Adjuntar a: GameObject "Cannon" (hijo de la pirámide/prisma)
    ///   • pivotPoint  → Transform del pivote del cañón (gira este objeto)
    ///   • firePoint   → Transform vacío en la boca del cañón
    ///   • projectilePrefab → Prefab de proyectil (esfera/círculo)
    ///   • projectileSpeed  → velocidad (ej. 20)
    ///   • minAngleDeg  → ángulo mínimo (ej. 0 = horizontal derecha)
    ///   • maxAngleDeg  → ángulo máximo (ej. 80 = casi vertical)
    ///   • fireRateRPM  → disparos por minuto (ej. 60)
    ///   • mainCamera   → referencia a la cámara principal
    /// </summary>
    public class CannonController : MonoBehaviour
    {
        [Header("Apuntado")]
        [SerializeField] private Transform pivotPoint;
        [SerializeField] private Camera    mainCamera;

        [Header("Restricción de ángulo (grados desde horizontal derecha)")]
        [SerializeField] private float minAngleDeg =  0f;   // no dispara a la izquierda
        [SerializeField] private float maxAngleDeg = 80f;

        [Header("Disparo")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float      projectileSpeed = 20f;
        [SerializeField] private float      fireRateRPM     = 60f;

        private float _fireCooldown;
        private float _currentAngle;

        private void Update()
        {
            AimAtMouse();
            HandleFireInput();
        }

        // ── Apuntado ─────────────────────────────────────────────────────
        private void AimAtMouse()
        {
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f));
            mouseWorld.z = 0f;

            Vector2 direction = mouseWorld - pivotPoint.position;
            float   rawAngle  = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Clamp: solo permite apuntar dentro del arco derecho
            _currentAngle = Mathf.Clamp(rawAngle, minAngleDeg, maxAngleDeg);
            pivotPoint.rotation = Quaternion.Euler(0f, 0f, _currentAngle);
        }

        // ── Disparo ───────────────────────────────────────────────────────
        private void HandleFireInput()
        {
            _fireCooldown -= Time.deltaTime;
            if (Input.GetMouseButton(0) && _fireCooldown <= 0f)
            {
                Fire();
                _fireCooldown = 60f / fireRateRPM;
            }
        }

        private void Fire()
        {
            GameObject proj = Instantiate(projectilePrefab,
                                          firePoint.position,
                                          Quaternion.Euler(0f, 0f, _currentAngle));

            // Dirección basada en el ángulo actual
            Vector2 dir = new Vector2(Mathf.Cos(_currentAngle * Mathf.Deg2Rad),
                                      Mathf.Sin(_currentAngle * Mathf.Deg2Rad));

            if (proj.TryGetComponent<Projectile>(out var p))
                p.Initialize(dir, projectileSpeed);

            // Reportar al colector
            AnalyticsCollector.Instance?.RecordShotFired(proj.GetInstanceID(), _currentAngle);
        }
    }
}
