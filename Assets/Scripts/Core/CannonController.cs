using UnityEngine;
using TowerDefense.Analytics;

namespace TowerDefense.Core
{
    public class CannonController : MonoBehaviour
    {
        [Header("Aiming")]
        [SerializeField] private Transform pivotPoint;

        [SerializeField] private Camera mainCamera;

        [Header("Angle Restrictions")]
        [SerializeField] private float minAngleDeg = 0f;

        [SerializeField] private float maxAngleDeg = -80f;

        [Header("Shooting")]
        [SerializeField] private Transform firePoint;

        [SerializeField] private GameObject projectilePrefab;

        [SerializeField] private float projectileSpeed = 20f;

        [SerializeField] private float fireRateRPM = 60f;

        private float _fireCooldown;

        private float _currentAngle;

        private void Update()
        {
            if (pivotPoint == null)
            {
                Debug.LogError(
                    "CannonController: pivotPoint is not assigned."
                );

                return;
            }

            if (mainCamera == null)
            {
                Debug.LogError(
                    "CannonController: mainCamera is not assigned."
                );

                return;
            }

            AimAtMouse();

            HandleFireInput();
        }

        private void AimAtMouse()
        {
            Ray ray =
                mainCamera.ScreenPointToRay(
                    Input.mousePosition
                );

            Plane plane =
                new Plane(
                    Vector3.forward,
                    pivotPoint.position
                );

            if (plane.Raycast(ray, out float enter))
            {
                Vector3 mouseWorld =
                    ray.GetPoint(enter);

                Vector2 direction =
                    mouseWorld - pivotPoint.position;

                float rawAngle =
                    Mathf.Atan2(
                        direction.y,
                        direction.x
                    ) * Mathf.Rad2Deg;

                _currentAngle =
                    Mathf.Clamp(
                        rawAngle,
                        minAngleDeg,
                        maxAngleDeg
                    );

                pivotPoint.rotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        _currentAngle
                    );
            }
        }

        private void HandleFireInput()
        {
            _fireCooldown -= Time.deltaTime;

            if (
                Input.GetMouseButton(0) &&
                _fireCooldown <= 0f
            )
            {
                Fire();

                _fireCooldown =
                    60f / fireRateRPM;
            }
        }

        private void Fire()
        {
            if (projectilePrefab == null)
            {
                Debug.LogError(
                    "CannonController: projectilePrefab is not assigned."
                );

                return;
            }

            if (firePoint == null)
            {
                Debug.LogError(
                    "CannonController: firePoint is not assigned."
                );

                return;
            }

            GameObject proj =
                Instantiate(
                    projectilePrefab,
                    firePoint.position,
                    Quaternion.Euler(
                        0f,
                        0f,
                        _currentAngle
                    )
                );

            Vector2 dir =
                new Vector2(
                    Mathf.Cos(
                        _currentAngle * Mathf.Deg2Rad
                    ),
                    Mathf.Sin(
                        _currentAngle * Mathf.Deg2Rad
                    )
                );

            if (
                proj.TryGetComponent<Projectile>(
                    out var projectile
                )
            )
            {
                projectile.Initialize(
                    dir,
                    projectileSpeed
                );
            }
            else
            {
                Debug.LogWarning(
                    "Projectile prefab does not contain a Projectile component."
                );
            }

            AnalyticsCollector.Instance?.RecordShotFired(
                proj.GetInstanceID(),
                _currentAngle
            );
        }
    }
}