using UnityEngine;

namespace LightWeaver.Player
{
    /// <summary>
    /// Light Weaver karakterini ileri görüş payıyla (Look-Ahead) akıcı şekilde takip eden
    /// ve çarpışma/parlama anlarında mikro sarsıntı (Camera Shake) üreten 2D kamera kontrolcüsü.
    /// </summary>
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [Header("Target & Framing")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 followOffset = new Vector2(4.6f, 0.9f);
        [SerializeField] private float smoothTimeX = 0.08f;
        [SerializeField] private float smoothTimeY = 0.22f;
        [SerializeField] private float minCameraY = -3.0f;

        private float _velX;
        private float _velY;
        private float _shakeIntensity;
        private float _shakeDuration;
        private float _shakeTimer;
        private RunnerMotor2D _targetMotor;

        private void Awake()
        {
            CacheTargetMotor();
        }

        private void OnEnable()
        {
            if (_targetMotor != null) _targetMotor.OnRunnerRespawned += HandleTargetRespawned;
        }

        private void OnDisable()
        {
            if (_targetMotor != null) _targetMotor.OnRunnerRespawned -= HandleTargetRespawned;
        }

        private void CacheTargetMotor()
        {
            if (target != null)
            {
                _targetMotor = target.GetComponent<RunnerMotor2D>();
            }
        }

        private void HandleTargetRespawned()
        {
            _velX = 0f;
            _velY = 0f;
            SnapToTarget();
        }

        public void SetTarget(Transform newTarget)
        {
            if (_targetMotor != null) _targetMotor.OnRunnerRespawned -= HandleTargetRespawned;
            target = newTarget;
            CacheTargetMotor();
            if (_targetMotor != null && isActiveAndEnabled) _targetMotor.OnRunnerRespawned += HandleTargetRespawned;
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            transform.position = new Vector3(
                target.position.x + followOffset.x,
                Mathf.Max(minCameraY, target.position.y + followOffset.y),
                transform.position.z);
        }

        public void TriggerShake(float intensity, float duration)
        {
            _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
            _shakeDuration = Mathf.Max(0.01f, duration);
            _shakeTimer = duration;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float dt = Time.deltaTime;
            float desiredX = target.position.x + followOffset.x;
            float desiredY = Mathf.Max(minCameraY, target.position.y + followOffset.y);

            float nextX = Mathf.SmoothDamp(transform.position.x, desiredX, ref _velX, smoothTimeX, Mathf.Infinity, dt);
            float nextY = Mathf.SmoothDamp(transform.position.y, desiredY, ref _velY, smoothTimeY, Mathf.Infinity, dt);

            Vector2 shakeOffset = Vector2.zero;
            if (_shakeTimer > 0f)
            {
                _shakeTimer -= dt;
                float fade = Mathf.Clamp01(_shakeTimer / _shakeDuration);
                float mag = _shakeIntensity * fade;
                shakeOffset = new Vector2(Random.Range(-mag, mag), Random.Range(-mag, mag));
                if (_shakeTimer <= 0f) _shakeIntensity = 0f;
            }

            transform.position = new Vector3(nextX + shakeOffset.x, nextY + shakeOffset.y, transform.position.z);
        }
    }
}
