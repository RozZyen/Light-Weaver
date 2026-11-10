using System;
using UnityEngine;

namespace LightWeaver.Player
{
    /// <summary>
    /// Light Weaver karakterinin sıfır tahsisli (Zero-GC) 2D otomatik koşu, eğimli kristal rampa
    /// tırmanışı (30°-45°), otomatik basamak atlama ve boşluğa düşme fizik motoru.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class RunnerMotor2D : MonoBehaviour
    {
        public event Action<float> OnRunnerLanded;
        public event Action OnRunnerFellIntoVoid;

        [Header("Movement & Climbing")]
        [SerializeField] private float baseRunSpeed = 6.6f;
        [SerializeField] private float acceleration = 26f;
        [SerializeField] private float airControlAccel = 14f;
        [SerializeField] private float maxSlopeAngleDeg = 52f;
        [SerializeField] private float stepUpHeight = 0.45f;
        [SerializeField] private float coyoteTimeDuration = 0.12f;

        [Header("Gravity & Void Limits")]
        [SerializeField] private float gravityScale = 2.6f;
        [SerializeField] private float maxFallSpeed = -18f;
        [SerializeField] private float voidFallY = -9.5f;
        [SerializeField] private bool autoRespawnOnVoidFall = true;

        [Header("Ground Detection (Zero-GC)")]
        [SerializeField] private LayerMask groundLayerMask = ~0;
        [SerializeField] private float groundCheckDistance = 0.16f;

        private Rigidbody2D _rb;
        private CapsuleCollider2D _capsule;
        private LightTouchInput _touchInput;

        private readonly RaycastHit2D[] _raycastHits = new RaycastHit2D[4];
        private ContactFilter2D _groundFilter;

        private Vector3 _spawnPosition;
        private float _speedMultiplier = 1f;
        private bool _isGrounded;
        private bool _wasGroundedLastStep;
        private float _coyoteTimer;
        private Vector2 _groundNormal = Vector2.up;
        private float _currentSlopeAngleDeg;
        private float _externalJumpLockTimer;
        private bool _isRunning = true;

        public bool IsGrounded => _isGrounded;
        public bool IsOnCoyoteTime => !_isGrounded && _coyoteTimer > 0f;
        public float CurrentSpeedX => _rb != null ? _rb.linearVelocity.x : 0f;
        public float CurrentSpeedY => _rb != null ? _rb.linearVelocity.y : 0f;
        public float CurrentSlopeAngleDeg => _currentSlopeAngleDeg;
        public float NormalizedSpeed => baseRunSpeed > 0.01f ? Mathf.Clamp01(CurrentSpeedX / (baseRunSpeed * _speedMultiplier)) : 0f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _capsule = GetComponent<CapsuleCollider2D>();
            _touchInput = GetComponent<LightTouchInput>();
            _spawnPosition = transform.position;

            _rb.gravityScale = gravityScale;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            _groundFilter = new ContactFilter2D
            {
                useTriggers = false,
                useLayerMask = true,
                layerMask = groundLayerMask
            };
        }

        public void SetSpawnPoint(Vector3 pos)
        {
            _spawnPosition = pos;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Clamp(multiplier, 0.2f, 2.5f);
        }

        public void SetRunning(bool active)
        {
            _isRunning = active;
            if (!active && _rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
            }
        }

        public void ApplyExternalImpulse(Vector2 impulseVelocity)
        {
            if (_rb == null) return;
            _isGrounded = false;
            _coyoteTimer = 0f;
            _externalJumpLockTimer = 0.14f;
            _rb.linearVelocity = new Vector2(Mathf.Max(_rb.linearVelocity.x, impulseVelocity.x), impulseVelocity.y);
        }

        public void RespawnAtStart()
        {
            if (_rb == null) return;
            _rb.position = _spawnPosition;
            _rb.linearVelocity = Vector2.zero;
            _isGrounded = false;
            _coyoteTimer = 0f;
            _externalJumpLockTimer = 0f;
            if (_touchInput != null)
            {
                _touchInput.ResetAimToInitial();
            }
        }

        private void FixedUpdate()
        {
            if (!_isRunning || _rb == null) return;

            float dt = Time.fixedDeltaTime;
            if (_externalJumpLockTimer > 0f)
            {
                _externalJumpLockTimer -= dt;
            }

            float preVelocityY = _rb.linearVelocity.y;
            DetectGround();

            if (_isGrounded && !_wasGroundedLastStep && preVelocityY < -1.5f)
            {
                OnRunnerLanded?.Invoke(Mathf.Abs(preVelocityY));
            }
            _wasGroundedLastStep = _isGrounded;

            if (_isGrounded)
            {
                _coyoteTimer = coyoteTimeDuration;
                ApplySlopeAlignedRun(dt);
                TryAutoStepUp();
            }
            else
            {
                _coyoteTimer = Mathf.Max(0f, _coyoteTimer - dt);
                ApplyAirborneMotion(dt);
            }

            if (transform.position.y < voidFallY)
            {
                OnRunnerFellIntoVoid?.Invoke();
                if (autoRespawnOnVoidFall)
                {
                    RespawnAtStart();
                }
            }
        }

        private void DetectGround()
        {
            if (_externalJumpLockTimer > 0f)
            {
                _isGrounded = false;
                _groundNormal = Vector2.up;
                _currentSlopeAngleDeg = 0f;
                return;
            }

            Bounds b = _capsule.bounds;
            Vector2 footCenter = new Vector2(b.center.x, b.min.y + 0.08f);
            Vector2 boxSize = new Vector2(b.size.x * 0.72f, 0.12f);

            int hitCount = Physics2D.BoxCast(footCenter, boxSize, 0f, Vector2.down, _groundFilter, _raycastHits, groundCheckDistance);
            bool foundValidGround = false;
            Vector2 bestNormal = Vector2.up;
            float bestAngle = 0f;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = _raycastHits[i];
                if (hit.collider == null || hit.collider == _capsule) continue;

                float angle = Vector2.Angle(hit.normal, Vector2.up);
                if (angle <= maxSlopeAngleDeg && hit.normal.y > 0.25f)
                {
                    foundValidGround = true;
                    bestNormal = hit.normal;
                    bestAngle = angle;
                    break;
                }
            }

            _isGrounded = foundValidGround;
            _groundNormal = bestNormal;
            _currentSlopeAngleDeg = bestAngle;
        }

        private void ApplySlopeAlignedRun(float dt)
        {
            float targetSpeed = baseRunSpeed * _speedMultiplier;
            // Yüzey normaline dik sağa doğru teğet vektör (30°-45° kristal rampalarda kayıpsız tırmanış)
            Vector2 slopeTangent = new Vector2(_groundNormal.y, -_groundNormal.x).normalized;
            if (slopeTangent.x < 0f) slopeTangent = -slopeTangent;

            Vector2 currentVel = _rb.linearVelocity;
            float currentSpeedAlongSlope = Vector2.Dot(currentVel, slopeTangent);
            float nextSpeed = Mathf.MoveTowards(currentSpeedAlongSlope, targetSpeed, acceleration * dt);

            // Rampa zirvesinden sıçramayı engellemek için hafif aşağı yüzey yapışma kuvveti
            Vector2 desiredVel = slopeTangent * nextSpeed - _groundNormal * 1.15f;
            _rb.linearVelocity = desiredVel;
        }

        private void TryAutoStepUp()
        {
            Bounds b = _capsule.bounds;
            Vector2 lowOrigin = new Vector2(b.max.x + 0.02f, b.min.y + 0.08f);
            Vector2 highOrigin = new Vector2(b.max.x + 0.02f, b.min.y + stepUpHeight + 0.12f);

            int lowHits = Physics2D.Raycast(lowOrigin, Vector2.right, _groundFilter, _raycastHits, 0.24f);
            bool hasLowStepObstacle = false;
            for (int i = 0; i < lowHits; i++)
            {
                Collider2D col = _raycastHits[i].collider;
                if (col != null && col != _capsule && Mathf.Abs(_raycastHits[i].normal.x) > 0.6f)
                {
                    hasLowStepObstacle = true;
                    break;
                }
            }

            if (!hasLowStepObstacle) return;

            int highHits = Physics2D.Raycast(highOrigin, Vector2.right, _groundFilter, _raycastHits, 0.34f);
            for (int i = 0; i < highHits; i++)
            {
                if (_raycastHits[i].collider != null && _raycastHits[i].collider != _capsule)
                {
                    return; // Üstte duvar var, basamak değil
                }
            }

            // Basamağın üzerine pürüzsüzce çık
            _rb.position += new Vector2(0.10f, stepUpHeight * 0.72f);
        }

        private void ApplyAirborneMotion(float dt)
        {
            float targetSpeed = baseRunSpeed * _speedMultiplier;
            Vector2 vel = _rb.linearVelocity;
            vel.x = Mathf.MoveTowards(vel.x, targetSpeed, airControlAccel * dt);
            vel.y = Mathf.Max(vel.y, maxFallSpeed);
            _rb.linearVelocity = vel;
        }
    }
}
