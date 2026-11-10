using System;
using LightWeaver.Core;
using LightWeaver.Player;
using LightWeaver.Pooling;
using UnityEngine;

namespace LightWeaver.World
{
    /// <summary>
    /// Yolu kesen kilitli lazer bariyeri ve tepesindeki Elmas Kilit anahtarı.
    /// Fener ışığını bariyerin üzerindeki kilide doğrultup kırmadan geçersen ağır pil şoku + sendeleme uygular!
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class PrismLockGate : MonoBehaviour, ILightReactive, IPoolable
    {
        public static event Action<PrismLockGate, bool> OnGateResolved; // (gate, unlockedByLight)

        [Header("Lock & Barrier Setup")]
        [SerializeField] private SpriteRenderer barrierVisualRenderer;
        [SerializeField] private SpriteRenderer diamondLockRenderer;
        [SerializeField] private Transform diamondLockTransform;
        [SerializeField] private LineRenderer lockTetherLine;
        [SerializeField] private float unlockChargeSeconds = 0.16f;
        [SerializeField] private float barrierPenaltyBattery = 22f;

        private BoxCollider2D _barrierCollider;
        private SpriteRenderer _barrierRenderer;
        private bool _isUnlocked;
        private float _unlockProgress;
        private float _lastExposedTime = -999f;
        private Vector3 _baseLockScale = Vector3.one;
        private Vector3 _baseLockPos = new Vector3(0f, 2.25f, 0f);

        public Vector2 WorldPosition => diamondLockTransform != null ? (Vector2)diamondLockTransform.position : (Vector2)transform.position;
        public HardlightPhase CurrentPhase => _isUnlocked ? HardlightPhase.Phantom : HardlightPhase.Solid;

        private void Awake()
        {
            _barrierCollider = GetComponent<BoxCollider2D>();
            _barrierCollider.isTrigger = true;
            _barrierRenderer = barrierVisualRenderer != null ? barrierVisualRenderer : GetComponent<SpriteRenderer>();
            if (lockTetherLine != null && lockTetherLine.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null) lockTetherLine.sharedMaterial = new Material(shader);
            }
            if (diamondLockTransform != null)
            {
                _baseLockScale = diamondLockTransform.localScale;
                _baseLockPos = diamondLockTransform.localPosition;
            }
            ResetGate();
        }

        private void OnEnable()
        {
            LightReactiveRegistry.Register(this);
            RunnerMotor2D.OnAnyRunnerRespawned += ResetGate;
            ResetGate();
        }

        private void OnDisable()
        {
            LightReactiveRegistry.Unregister(this);
            RunnerMotor2D.OnAnyRunnerRespawned -= ResetGate;
        }

        public void OnSpawnedFromPool() => ResetGate();
        public void OnReturnedToPool() => ResetGate();

        public void ResetGate()
        {
            _isUnlocked = false;
            _unlockProgress = 0f;
            _lastExposedTime = -999f;

            if (_barrierCollider != null) _barrierCollider.enabled = true;
            if (_barrierRenderer != null)
            {
                _barrierRenderer.enabled = true;
                _barrierRenderer.color = Color.white;
            }
            if (diamondLockRenderer != null)
            {
                diamondLockRenderer.enabled = true;
                diamondLockRenderer.color = Color.white;
            }
            if (diamondLockTransform != null)
            {
                diamondLockTransform.localScale = _baseLockScale;
            }
            if (lockTetherLine != null)
            {
                lockTetherLine.enabled = true;
            }
        }

        public Vector2 GetClosestLightSamplePoint(Vector2 beamOrigin, Vector2 beamDirection)
        {
            // Yalnızca kapının tepesindeki Elmas Kilit noktası fenerle kırılabilir!
            return WorldPosition;
        }

        public void OnLightExposure(Vector2 beamOrigin, float normalizedIntensity, bool isFocusedBeam, float deltaTime)
        {
            if (_isUnlocked) return;
            _lastExposedTime = Time.time;

            float mult = isFocusedBeam ? 2.4f : 1.0f;
            _unlockProgress += deltaTime * mult;

            float ratio = Mathf.Clamp01(_unlockProgress / unlockChargeSeconds);
            if (diamondLockTransform != null)
            {
                diamondLockTransform.localScale = _baseLockScale * (1f + ratio * 0.28f);
            }
            if (diamondLockRenderer != null)
            {
                diamondLockRenderer.color = Color.Lerp(Color.white, new Color(1.0f, 0.94f, 0.72f, 1f), ratio);
            }

            if (_unlockProgress >= unlockChargeSeconds)
            {
                UnlockGate(byLightSkill: true);
            }
        }

        public void ForceUnlockByBurst()
        {
            if (!_isUnlocked) UnlockGate(byLightSkill: true);
        }

        private void UnlockGate(bool byLightSkill)
        {
            _isUnlocked = true;
            if (_barrierCollider != null) _barrierCollider.enabled = false;
            if (_barrierRenderer != null) _barrierRenderer.enabled = false;
            if (diamondLockRenderer != null) diamondLockRenderer.enabled = false;
            if (lockTetherLine != null) lockTetherLine.enabled = false;
            OnGateResolved?.Invoke(this, byLightSkill);
        }

        private void Update()
        {
            if (_isUnlocked) return;
            float dt = Time.deltaTime;

            if (diamondLockTransform != null)
            {
                float hoverY = Mathf.Sin(Time.time * 3.2f) * 0.08f;
                diamondLockTransform.localPosition = new Vector3(_baseLockPos.x, _baseLockPos.y + hoverY, _baseLockPos.z);
                diamondLockTransform.localRotation = Quaternion.identity;
            }

            if ((Time.time - _lastExposedTime) > 0.05f && _unlockProgress > 0f)
            {
                _unlockProgress = Mathf.Max(0f, _unlockProgress - dt * 2.0f);
                if (diamondLockTransform != null)
                {
                    diamondLockTransform.localScale = Vector3.Lerp(diamondLockTransform.localScale, _baseLockScale, dt * 12f);
                }
                if (diamondLockRenderer != null)
                {
                    diamondLockRenderer.color = Color.Lerp(diamondLockRenderer.color, Color.white, dt * 10f);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isUnlocked || !other.CompareTag("Player")) return;

            if (other.TryGetComponent(out RunnerMotor2D motor))
            {
                motor.ApplyStumbleSlow(0.55f);
            }
            if (BatterySystem.ActiveInstance != null)
            {
                BatterySystem.ActiveInstance.TakeHazardShock(barrierPenaltyBattery);
            }
            UnlockGate(byLightSkill: false);
        }
    }
}
