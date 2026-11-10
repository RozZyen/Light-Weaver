using System;
using LightWeaver.Core;
using LightWeaver.Player;
using LightWeaver.Pooling;
using UnityEngine;

namespace LightWeaver.World
{
    /// <summary>
    /// Referans görseldeki dairesel neon turuncu/siyan Rezonans Sıçrama Halkası.
    /// Yalnızca fener ışığı ile aydınlatıldığında katılaşıp şarj olur ve üzerine basan koşucuyu
    /// yüksek katmanlara (Starlight Canopy) veya geniş uçurumların ötesine fırlatır.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public sealed class PrismaticJumpPad : MonoBehaviour, ILightReactive, IPoolable
    {
        public static event Action<PrismaticJumpPad> OnJumpPadLaunched;

        [Header("Launch Parameters")]
        [SerializeField] private Vector2 launchVelocity = new Vector2(8.2f, 13.5f);
        [SerializeField] private float solidifyThresholdSeconds = 0.015f;
        [SerializeField] private float afterglowDurationSeconds = 0.45f;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer innerCoreRenderer;
        [SerializeField] private Color activeRingColor = Color.white;
        [SerializeField] private Color afterglowColor = new Color(0.88f, 0.84f, 0.78f, 0.85f);

        private BoxCollider2D _triggerCollider;
        private SpriteRenderer _outerRingRenderer;
        private Vector3 _baseScale;

        private HardlightPhase _currentPhase = HardlightPhase.Phantom;
        private float _chargeTimer;
        private float _afterglowTimer;
        private float _lastExposedTime = -999f;
        private float _launchCooldownTimer;
        private float _bounceAnimTimer;

        public Vector2 WorldPosition => transform.position;
        public HardlightPhase CurrentPhase => _currentPhase;

        private void Awake()
        {
            _triggerCollider = GetComponent<BoxCollider2D>();
            _triggerCollider.isTrigger = true;
            _outerRingRenderer = GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;
            ApplyPhaseVisuals(HardlightPhase.Phantom, 0f);
        }

        private void OnEnable()
        {
            LightReactiveRegistry.Register(this);
            ResetToPhantom();
        }

        private void OnDisable()
        {
            LightReactiveRegistry.Unregister(this);
        }

        public void OnSpawnedFromPool()
        {
            _baseScale = transform.localScale;
            ResetToPhantom();
        }

        public void OnReturnedToPool()
        {
            ResetToPhantom();
        }

        public void ResetToPhantom()
        {
            _currentPhase = HardlightPhase.Phantom;
            _chargeTimer = 0f;
            _afterglowTimer = 0f;
            _lastExposedTime = -999f;
            _launchCooldownTimer = 0f;
            _bounceAnimTimer = 0f;
            if (_triggerCollider != null) _triggerCollider.enabled = false;
            ApplyPhaseVisuals(HardlightPhase.Phantom, 0f);
        }

        public void SetLaunchVelocity(Vector2 velocity)
        {
            launchVelocity = velocity;
        }

        public Vector2 GetClosestLightSamplePoint(Vector2 beamOrigin, Vector2 beamDirection)
        {
            return transform.position;
        }

        public void OnLightExposure(Vector2 beamOrigin, float normalizedIntensity, bool isFocusedBeam, float deltaTime)
        {
            _lastExposedTime = Time.time;
            float speedMult = isFocusedBeam ? 2.2f : 1.0f;

            if (_currentPhase == HardlightPhase.Phantom)
            {
                _chargeTimer += deltaTime * speedMult;
                if (_chargeTimer >= solidifyThresholdSeconds)
                {
                    _currentPhase = HardlightPhase.Solid;
                    _afterglowTimer = afterglowDurationSeconds;
                    if (_triggerCollider != null) _triggerCollider.enabled = true;
                    ApplyPhaseVisuals(HardlightPhase.Solid, normalizedIntensity);
                }
            }
            else
            {
                _currentPhase = HardlightPhase.Solid;
                _afterglowTimer = afterglowDurationSeconds;
                ApplyPhaseVisuals(HardlightPhase.Solid, normalizedIntensity);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_launchCooldownTimer > 0f) _launchCooldownTimer -= dt;

            if (_bounceAnimTimer > 0f)
            {
                _bounceAnimTimer -= dt;
                float t = Mathf.Clamp01(_bounceAnimTimer / 0.22f);
                float scaleBoost = 1f + Mathf.Sin(t * Mathf.PI) * 0.35f;
                transform.localScale = new Vector3(_baseScale.x * scaleBoost, _baseScale.y * scaleBoost, 1f);
            }
            else
            {
                transform.localScale = _baseScale;
            }

            bool exposedRecently = (Time.time - _lastExposedTime) <= 0.045f;
            if (!exposedRecently)
            {
                if (_currentPhase == HardlightPhase.Solid)
                {
                    _currentPhase = HardlightPhase.Afterglow;
                }

                if (_currentPhase == HardlightPhase.Afterglow)
                {
                    _afterglowTimer -= dt;
                    if (_afterglowTimer <= 0f)
                    {
                        ResetToPhantom();
                    }
                    else
                    {
                        ApplyPhaseVisuals(HardlightPhase.Afterglow, Mathf.Clamp01(_afterglowTimer / afterglowDurationSeconds));
                    }
                }
                else if (_currentPhase == HardlightPhase.Phantom && _chargeTimer > 0f)
                {
                    _chargeTimer = Mathf.Max(0f, _chargeTimer - dt * 3f);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_currentPhase == HardlightPhase.Phantom || _launchCooldownTimer > 0f) return;
            if (!other.CompareTag("Player")) return;

            if (other.TryGetComponent(out RunnerMotor2D motor))
            {
                motor.ApplyExternalImpulse(launchVelocity);
                _launchCooldownTimer = 0.25f;
                _bounceAnimTimer = 0.22f;
                OnJumpPadLaunched?.Invoke(this);
            }
        }

        private void ApplyPhaseVisuals(HardlightPhase phase, float factor)
        {
            if (_outerRingRenderer == null) return;

            switch (phase)
            {
                case HardlightPhase.Phantom:
                    _outerRingRenderer.enabled = false;
                    if (innerCoreRenderer != null) innerCoreRenderer.enabled = false;
                    break;

                case HardlightPhase.Solid:
                    _outerRingRenderer.enabled = true;
                    _outerRingRenderer.color = activeRingColor;
                    if (innerCoreRenderer != null)
                    {
                        innerCoreRenderer.enabled = true;
                        innerCoreRenderer.color = new Color(0.24f, 0.88f, 0.80f, 0.48f);
                    }
                    break;

                case HardlightPhase.Afterglow:
                    _outerRingRenderer.enabled = true;
                    _outerRingRenderer.color = new Color(afterglowColor.r, afterglowColor.g, afterglowColor.b, factor * afterglowColor.a);
                    if (innerCoreRenderer != null)
                    {
                        innerCoreRenderer.enabled = factor > 0.15f;
                        innerCoreRenderer.color = new Color(0.24f, 0.88f, 0.80f, factor * 0.36f);
                    }
                    break;
            }
        }
    }
}
