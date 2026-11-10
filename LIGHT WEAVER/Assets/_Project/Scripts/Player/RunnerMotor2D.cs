using System;
using LightWeaver.World;
using UnityEngine;

namespace LightWeaver.Player
{
    /// <summary>
    /// Light Weaver karakterinin sıfır tahsisli (Zero-GC) 2D otomatik koşu, aktif Zıplama/Çift Zıplama,
    /// KineticBoost hız fırlatması, sendeleme (Stumble), eğimli kristal rampa tırmanışı ve
    /// güvenli Solar Revive fizik motoru.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class RunnerMotor2D : MonoBehaviour
    {
        public static event Action OnAnyRunnerRespawned;

        public event Action<float> OnRunnerLanded;
        public event Action<bool> OnRunnerJumped; // (isDoubleJump)
        public event Action<float> OnKineticBoostActivated;
        public event Action OnRunnerFellIntoVoid;
        public event Action OnRunnerRespawned;

        [Header("Movement & Climbing")]
        [SerializeField] private float baseRunSpeed = 6.8f;
        [SerializeField] private float acceleration = 28f;
        [SerializeField] private float airControlAccel = 16f;
        [SerializeField] private float maxSlopeAngleDeg = 52f;
        [SerializeField] private float stepUpHeight = 0.48f;
        [SerializeField] private float coyoteTimeDuration = 0.13f;

        [Header("Photon Jump & Double Jump")]
        [SerializeField] private float jumpVelocity = 11.4f;
        [SerializeField] private float doubleJumpVelocity = 10.4f;
        [SerializeField] private float doubleJumpBatteryCost = 3.5f;
        [SerializeField] private float jumpBufferDuration = 0.13f;

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
        private BatterySystem _batterySystem;

        private readonly RaycastHit2D[] _raycastHits = new RaycastHit2D[4];
        private ContactFilter2D _groundFilter;

        private Vector3 _spawnPosition;
        private float _speedMultiplier = 1f;
        private float _sectorSpeedMultiplier = 1f;
        private float _boostSpeedMultiplier = 1f;
        private float _boostTimer;
        private float _stumbleTimer;

        private float _jumpPowerBonusMultiplier = 1f;
        private bool _freeDoubleJump;

        private bool _isGrounded;
        private bool _wasGroundedLastStep;
        private Collider2D _lastGroundCollider;
        private CrystalPlatform _lastGroundPlatform;

        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _hasDoubleJump;
        private Vector2 _groundNormal = Vector2.up;
        private float _currentSlopeAngleDeg;
        private float _externalJumpLockTimer;
        private float _voidGraceTimer;
        private bool _isRunning = true;

        private bool _isSwinging;
        private Vector2 _swingAnchorPos;
        private float _swingRopeLength;
        private float _swingTargetLength;
        private float _swingAngleRad;
        private float _swingAngularVel;
        private Action _onSwingDetachedCallback;

        public bool IsGrounded => _isGrounded;
        public bool IsOnCoyoteTime => !_isGrounded && _coyoteTimer > 0f;
        public bool IsBoosting => _boostTimer > 0f;
        public bool IsSwinging => _isSwinging;
        public Vector2 SwingAnchorPosition => _swingAnchorPos;
        public float CurrentSpeedX => _rb != null ? _rb.linearVelocity.x : 0f;
        public float CurrentSpeedY => _rb != null ? _rb.linearVelocity.y : 0f;
        public float CurrentSlopeAngleDeg => _currentSlopeAngleDeg;
        public float NormalizedSpeed => baseRunSpeed > 0.01f ? Mathf.Clamp01(CurrentSpeedX / (baseRunSpeed * _speedMultiplier * _sectorSpeedMultiplier)) : 0f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _capsule = GetComponent<CapsuleCollider2D>();
            _touchInput = GetComponent<LightTouchInput>();
            _batterySystem = GetComponent<BatterySystem>();
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

        public void SetSpawnPoint(Vector3 pos) => _spawnPosition = pos;
        public void SetSpeedMultiplier(float multiplier) => _speedMultiplier = Mathf.Clamp(multiplier, 0.2f, 2.5f);
        public void SetSectorSpeedMultiplier(float multiplier) => _sectorSpeedMultiplier = Mathf.Clamp(multiplier, 0.5f, 2.0f);
        public void SetJumpBlessing(float powerMult, bool freeDoubleJump)
        {
            _jumpPowerBonusMultiplier = Mathf.Clamp(powerMult, 0.8f, 1.6f);
            _freeDoubleJump = freeDoubleJump;
        }

        public void ResetBlessings()
        {
            _jumpPowerBonusMultiplier = 1f;
            _freeDoubleJump = false;
        }

        public void SetRunning(bool active)
        {
            _isRunning = active;
            if (!active && _rb != null)
            {
                ClearSwingStateOnly();
                _rb.linearVelocity = Vector2.zero;
                _rb.simulated = false;
            }
            else if (active && _rb != null)
            {
                _rb.gravityScale = gravityScale;
                _rb.simulated = true;
            }
        }

        public void BeginPendulumSwing(Vector2 anchorPos, Action onDetached = null)
        {
            if (_rb == null || !_isRunning || _isSwinging) return;

            Vector2 offset = _rb.position - anchorPos;
            float dist = offset.magnitude;
            if (dist < 0.8f) offset = new Vector2(-0.2f, -2.5f);

            _isSwinging = true;
            _swingAnchorPos = anchorPos;
            _onSwingDetachedCallback = onDetached;
            _swingRopeLength = Mathf.Max(2.1f, dist);
            _swingTargetLength = Mathf.Clamp(dist, 2.55f, 4.15f);
            _swingAngleRad = Mathf.Atan2(offset.y, offset.x);

            // Doğal sarkaç giriş teğet hızı (CCW teğeti = alt noktada +X sağa doğru!)
            Vector2 tangentDir = new Vector2(-Mathf.Sin(_swingAngleRad), Mathf.Cos(_swingAngleRad));
            float tangentialSpeed = Vector2.Dot(_rb.linearVelocity, tangentDir);
            float minEntrySpeed = GetEffectiveTargetSpeed() * 0.92f;
            if (tangentialSpeed < minEntrySpeed) tangentialSpeed = minEntrySpeed;
            _swingAngularVel = tangentialSpeed / Mathf.Max(0.8f, _swingRopeLength);

            _isGrounded = false;
            _coyoteTimer = 0f;
            _hasDoubleJump = true;
            _externalJumpLockTimer = 0f;
            _jumpBufferTimer = 0f;
            _rb.gravityScale = 0f;
        }

        public void EndPendulumSwing(bool withJumpBoost)
        {
            if (!_isSwinging) return;

            Action detachCb = _onSwingDetachedCallback;
            float angle = _swingAngleRad;
            float tangentialSpeed = _swingAngularVel * _swingRopeLength;
            ClearSwingStateOnly();

            if (_rb != null)
            {
                Vector2 tangentDir = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle));
                Vector2 releaseVel = tangentDir * tangentialSpeed;

                if (withJumpBoost)
                {
                    releaseVel.x = Mathf.Max(releaseVel.x + 1.6f, GetEffectiveTargetSpeed() * 1.32f);
                    releaseVel.y = Mathf.Max(releaseVel.y + 3.2f, 11.2f * _jumpPowerBonusMultiplier);
                    _externalJumpLockTimer = 0.14f;
                    OnRunnerJumped?.Invoke(false);
                }
                else
                {
                    // Yukarı-ileri salınım çıkışında teğetsel ivmeyi koru ve hafif sapan artışı ekle
                    if (releaseVel.x > 1f)
                    {
                        releaseVel.x = Mathf.Max(releaseVel.x, GetEffectiveTargetSpeed() * 1.12f);
                    }
                    if (releaseVel.y > 0.25f)
                    {
                        releaseVel.y += 1.45f;
                    }
                }

                _rb.linearVelocity = releaseVel;
            }

            _hasDoubleJump = true;
            detachCb?.Invoke();
        }

        private void ClearSwingStateOnly()
        {
            _isSwinging = false;
            _onSwingDetachedCallback = null;
            if (_rb != null) _rb.gravityScale = gravityScale;
        }

        public void SetAutoRespawnOnVoidFall(bool enabled) => autoRespawnOnVoidFall = enabled;

        public void RequestJump()
        {
            if (!_isRunning) return;
            _jumpBufferTimer = jumpBufferDuration;
        }

        public void ApplyKineticBoost(float multiplier, float durationSeconds)
        {
            _boostSpeedMultiplier = Mathf.Max(_boostSpeedMultiplier, multiplier);
            _boostTimer = Mathf.Max(_boostTimer, durationSeconds);
            OnKineticBoostActivated?.Invoke(durationSeconds);
        }

        public void ApplyStumbleSlow(float durationSeconds)
        {
            _stumbleTimer = Mathf.Max(_stumbleTimer, durationSeconds);
            _boostTimer = 0f;
            _boostSpeedMultiplier = 1f;
        }

        public void ApplyExternalImpulse(Vector2 impulseVelocity)
        {
            if (_rb == null) return;
            _isGrounded = false;
            _coyoteTimer = 0f;
            _hasDoubleJump = true;
            _externalJumpLockTimer = 0.14f;
            _rb.linearVelocity = new Vector2(Mathf.Max(_rb.linearVelocity.x, impulseVelocity.x), impulseVelocity.y);
        }

        public void ReviveAtPosition(Vector3 worldPos)
        {
            if (_rb == null) return;
            ClearSwingStateOnly();
            _rb.simulated = true;
            transform.position = worldPos;
            _rb.position = worldPos;
            Physics2D.SyncTransforms();

            _rb.linearVelocity = Vector2.zero;
            _isGrounded = false;
            _coyoteTimer = 0f;
            _hasDoubleJump = true;
            _externalJumpLockTimer = 0f;
            _voidGraceTimer = 1.25f;
            _stumbleTimer = 0f;
            if (_touchInput != null) _touchInput.ResetAimToInitial();
        }

        public void RespawnAtStart()
        {
            if (_rb == null) return;
            ClearSwingStateOnly();
            transform.position = _spawnPosition;
            _rb.position = _spawnPosition;
            Physics2D.SyncTransforms();

            _rb.linearVelocity = Vector2.zero;
            _isGrounded = false;
            _coyoteTimer = 0f;
            _hasDoubleJump = true;
            _externalJumpLockTimer = 0f;
            _voidGraceTimer = 0.6f;
            _boostTimer = 0f;
            _boostSpeedMultiplier = 1f;
            _stumbleTimer = 0f;
            if (_touchInput != null) _touchInput.ResetAimToInitial();
            OnRunnerRespawned?.Invoke();
            OnAnyRunnerRespawned?.Invoke();
        }

        private void FixedUpdate()
        {
            if (!_isRunning || _rb == null) return;

            float dt = Time.fixedDeltaTime;
            if (_externalJumpLockTimer > 0f) _externalJumpLockTimer -= dt;
            if (_voidGraceTimer > 0f) _voidGraceTimer -= dt;
            if (_jumpBufferTimer > 0f) _jumpBufferTimer -= dt;

            if (_boostTimer > 0f)
            {
                _boostTimer -= dt;
                if (_boostTimer <= 0f) _boostSpeedMultiplier = 1f;
            }
            if (_stumbleTimer > 0f) _stumbleTimer -= dt;

            if (_isSwinging)
            {
                if (_jumpBufferTimer > 0f)
                {
                    _jumpBufferTimer = 0f;
                    EndPendulumSwing(withJumpBoost: true);
                }
                else
                {
                    IntegratePendulumSwing(dt);
                }

                if (_voidGraceTimer <= 0f && transform.position.y < voidFallY)
                {
                    ClearSwingStateOnly();
                    OnRunnerFellIntoVoid?.Invoke();
                    if (autoRespawnOnVoidFall) RespawnAtStart();
                }
                return;
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
                _hasDoubleJump = true;
            }
            else
            {
                _coyoteTimer = Mathf.Max(0f, _coyoteTimer - dt);
            }

            // Zıplama / Çift Zıplama Tüketimi
            if (_jumpBufferTimer > 0f)
            {
                if (_isGrounded || _coyoteTimer > 0f)
                {
                    ExecuteJump(isDoubleJump: false);
                }
                else if (_hasDoubleJump && CanPayDoubleJumpCost())
                {
                    ExecuteJump(isDoubleJump: true);
                }
            }

            if (_isGrounded)
            {
                ApplySlopeAlignedRun(dt);
                TryAutoStepUp();
            }
            else
            {
                ApplyAirborneMotion(dt);
            }

            if (_voidGraceTimer <= 0f && transform.position.y < voidFallY)
            {
                OnRunnerFellIntoVoid?.Invoke();
                if (autoRespawnOnVoidFall) RespawnAtStart();
            }
        }

        private bool CanPayDoubleJumpCost()
        {
            if (_freeDoubleJump || _batterySystem == null) return true;
            return _batterySystem.CurrentBattery > doubleJumpBatteryCost + 1f;
        }

        private void ExecuteJump(bool isDoubleJump)
        {
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            _isGrounded = false;
            _externalJumpLockTimer = 0.10f;

            if (isDoubleJump)
            {
                _hasDoubleJump = false;
                if (!_freeDoubleJump && _batterySystem != null)
                {
                    _batterySystem.DrainExternal(doubleJumpBatteryCost);
                }
            }

            float vy = (isDoubleJump ? doubleJumpVelocity : jumpVelocity) * _jumpPowerBonusMultiplier;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, vy);
            OnRunnerJumped?.Invoke(isDoubleJump);
        }

        private float GetEffectiveTargetSpeed()
        {
            float stumbleMult = _stumbleTimer > 0f ? 0.45f : 1.0f;
            return baseRunSpeed * _speedMultiplier * _sectorSpeedMultiplier * _boostSpeedMultiplier * stumbleMult;
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
            Collider2D hitCol = null;

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
                    hitCol = hit.collider;
                    break;
                }
            }

            _isGrounded = foundValidGround;
            _groundNormal = bestNormal;
            _currentSlopeAngleDeg = bestAngle;

            if (foundValidGround && hitCol != null)
            {
                if (hitCol != _lastGroundCollider)
                {
                    _lastGroundCollider = hitCol;
                    hitCol.TryGetComponent(out _lastGroundPlatform);
                }
                if (_lastGroundPlatform != null)
                {
                    _lastGroundPlatform.NotifyRunnerSteppedOn(this);
                }
            }
            else
            {
                _lastGroundCollider = null;
                _lastGroundPlatform = null;
            }
        }

        private void ApplySlopeAlignedRun(float dt)
        {
            float targetSpeed = GetEffectiveTargetSpeed();
            Vector2 slopeTangent = new Vector2(_groundNormal.y, -_groundNormal.x).normalized;
            if (slopeTangent.x < 0f) slopeTangent = -slopeTangent;

            Vector2 currentVel = _rb.linearVelocity;
            float currentSpeedAlongSlope = Vector2.Dot(currentVel, slopeTangent);
            float nextSpeed = Mathf.MoveTowards(currentSpeedAlongSlope, targetSpeed, acceleration * dt);

            _rb.linearVelocity = slopeTangent * nextSpeed - _groundNormal * 1.15f;
        }

        private void TryAutoStepUp()
        {
            Bounds b = _capsule.bounds;
            Vector2 lowOrigin = new Vector2(b.max.x - 0.04f, b.min.y + 0.05f);
            Vector2 highOrigin = new Vector2(b.max.x - 0.04f, b.min.y + stepUpHeight + 0.16f);

            int lowHits = Physics2D.Raycast(lowOrigin, Vector2.right, _groundFilter, _raycastHits, 0.34f);
            bool hasLowStepObstacle = false;
            float obstaclePointX = b.max.x + 0.12f;
            for (int i = 0; i < lowHits; i++)
            {
                Collider2D col = _raycastHits[i].collider;
                if (col != null && col != _capsule && _raycastHits[i].normal.x < -0.15f)
                {
                    hasLowStepObstacle = true;
                    obstaclePointX = _raycastHits[i].point.x + 0.08f;
                    break;
                }
            }

            if (!hasLowStepObstacle) return;

            int highHits = Physics2D.Raycast(highOrigin, Vector2.right, _groundFilter, _raycastHits, 0.38f);
            for (int i = 0; i < highHits; i++)
            {
                if (_raycastHits[i].collider != null && _raycastHits[i].collider != _capsule) return;
            }

            Vector2 topProbeOrigin = new Vector2(obstaclePointX, b.min.y + stepUpHeight + 0.18f);
            int topHits = Physics2D.Raycast(topProbeOrigin, Vector2.down, _groundFilter, _raycastHits, stepUpHeight + 0.25f);
            for (int i = 0; i < topHits; i++)
            {
                RaycastHit2D hit = _raycastHits[i];
                if (hit.collider != null && hit.collider != _capsule && hit.normal.y > 0.35f)
                {
                    float liftDelta = (hit.point.y + 0.02f) - b.min.y;
                    if (liftDelta > 0f && liftDelta <= stepUpHeight + 0.12f)
                    {
                        _rb.position += new Vector2(0.06f, liftDelta);
                        if (_rb.linearVelocity.y < 0f) _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
                    }
                    break;
                }
            }
        }

        private void IntegratePendulumSwing(float dt)
        {
            _swingRopeLength = Mathf.MoveTowards(_swingRopeLength, _swingTargetLength, 5.2f * dt);

            float g = gravityScale * 9.81f;
            float angularAccel = -(g / Mathf.Max(0.8f, _swingRopeLength)) * Mathf.Cos(_swingAngleRad);

            // İniş yarım yayında doğal momentum koruma / hafif salınım desteği
            if (Mathf.Cos(_swingAngleRad) < 0f && _swingAngularVel >= -0.35f)
            {
                angularAccel += 0.95f;
            }

            _swingAngularVel += angularAccel * dt;
            _swingAngularVel *= Mathf.Clamp01(1f - 0.04f * dt);
            float maxOmega = 16.5f / Mathf.Max(0.8f, _swingRopeLength);
            _swingAngularVel = Mathf.Clamp(_swingAngularVel, -maxOmega, maxOmega);

            float nextAngle = _swingAngleRad + _swingAngularVel * dt;
            float minAngle = -Mathf.PI * 0.90f; // -162° (arka sınır)
            float maxAngle = -Mathf.PI * 0.12f; // -21.6° (ön üst salınım sınırı)
            if (nextAngle <= minAngle)
            {
                nextAngle = minAngle;
                _swingAngularVel = Mathf.Abs(_swingAngularVel) * 0.85f;
            }
            else if (nextAngle >= maxAngle)
            {
                nextAngle = maxAngle;
                _swingAngularVel = -Mathf.Abs(_swingAngularVel) * 0.85f;
            }

            _swingAngleRad = nextAngle;
            Vector2 nextPos = _swingAnchorPos + new Vector2(Mathf.Cos(nextAngle), Mathf.Sin(nextAngle)) * _swingRopeLength;
            _rb.linearVelocity = (nextPos - _rb.position) / Mathf.Max(0.0001f, dt);
        }

        private void ApplyAirborneMotion(float dt)
        {
            float targetSpeed = GetEffectiveTargetSpeed();
            Vector2 vel = _rb.linearVelocity;
            // Sarkaç çıkışı / Boost momentumunu havada hemen frenleme; yavaşça rüzgâr direncine yaklaş
            float accelRate = vel.x > targetSpeed ? 2.4f : airControlAccel;
            vel.x = Mathf.MoveTowards(vel.x, targetSpeed, accelRate * dt);
            vel.y = Mathf.Max(vel.y, maxFallSpeed);
            _rb.linearVelocity = vel;
        }
    }
}
