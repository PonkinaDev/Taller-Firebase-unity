using UnityEngine;
using TowerDefense.Analytics;

namespace TowerDefense.Core
{
    /// <summary>
    /// Controla el apuntado y disparo del cañón.
    /// Compatible con Input Manager clásico.
    /// 
    /// IMPORTANTE:
    /// En Player Settings -> Active Input Handling:
    /// usar "Both" o "Input Manager (Old)"
    /// </summary>
    public class CannonController : MonoBehaviour
    {
        [Header("Apuntado")]
        [SerializeField] private Transform pivotPoint;
        [SerializeField] private Camera mainCamera;

        [Header("Restricción de ángulo")]
        [SerializeField] private float minAngleDeg = 0f;
        [SerializeField] private float maxAngleDeg = -80f;

        [Header("Disparo")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 20f;
        [SerializeField] private float fireRateRPM = 60f;

        private float _fireCooldown;
        private float _currentAngle;

        private void Update()
        {
            // Validaciones defensivas
            if (pivotPoint == null)
            {
                Debug.LogError("CannonController: pivotPoint no asignado.");
                return;
            }

            if (mainCamera == null)
            {
                Debug.LogError("CannonController: mainCamera no asignada.");
                return;
            }

            AimAtMouse();
            HandleFireInput();
        }

        // ─────────────────────────────────────────────────────────────
        // APUNTADO
        // ─────────────────────────────────────────────────────────────
        private void AimAtMouse()
        {
            // Rayo desde la cámara hacia el mouse
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            // Plano XY donde está el cañón
            Plane plane = new Plane(Vector3.forward, pivotPoint.position);

            if (plane.Raycast(ray, out float enter))
            {
                // Punto del mundo donde el mouse intersecta el plano
                Vector3 mouseWorld = ray.GetPoint(enter);

                // Dirección desde el pivote al mouse
                Vector2 direction = mouseWorld - pivotPoint.position;

                // Ángulo en grados
                float rawAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                // Limitar ángulo
                _currentAngle = Mathf.Clamp(rawAngle, minAngleDeg, maxAngleDeg);

                // Rotar pivote
                pivotPoint.rotation = Quaternion.Euler(0f, 0f, _currentAngle);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // INPUT DE DISPARO
        // ─────────────────────────────────────────────────────────────
        private void HandleFireInput()
        {
            _fireCooldown -= Time.deltaTime;

            if (Input.GetMouseButton(0) && _fireCooldown <= 0f)
            {
                Fire();
                _fireCooldown = 60f / fireRateRPM;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // DISPARO
        // ─────────────────────────────────────────────────────────────
        private void Fire()
        {
            if (projectilePrefab == null)
            {
                Debug.LogError("CannonController: projectilePrefab no asignado.");
                return;
            }

            if (firePoint == null)
            {
                Debug.LogError("CannonController: firePoint no asignado.");
                return;
            }

            GameObject proj = Instantiate(
                projectilePrefab,
                firePoint.position,
                Quaternion.Euler(0f, 0f, _currentAngle)
            );

            // Dirección basada en el ángulo
            Vector2 dir = new Vector2(
                Mathf.Cos(_currentAngle * Mathf.Deg2Rad),
                Mathf.Sin(_currentAngle * Mathf.Deg2Rad)
            );

            // Inicializar proyectil
            if (proj.TryGetComponent<Projectile>(out var projectile))
            {
                projectile.Initialize(dir, projectileSpeed);
            }
            else
            {
                Debug.LogWarning("El prefab del proyectil no tiene componente Projectile.");
            }

            // Analytics
            AnalyticsCollector.Instance?.RecordShotFired(
                proj.GetInstanceID(),
                _currentAngle
            );
        }
    }
}