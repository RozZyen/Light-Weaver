using System;
using LightWeaver.Core;
using LightWeaver.Player;
using LightWeaver.Pooling;
using UnityEngine;

namespace LightWeaver.World
{
    /// <summary>
    /// Havada süzülen altıgen '+ENERGY' batarya hücresi.
    /// Fener ışığı 0.14s (Odak Lazeri ile 0.07s) odaklandığında veya koşucu temas ettiğinde
    /// manyetik olarak koşucuya çekilir ve pili şarj eder.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public sealed class EnergyOrb : MonoBehaviour, ILightReactive, IPoolable
    {
        public static event Action<EnergyOrb, float> OnEnergyCollected;

        [Header("Charge Configuration")]
        [SerializeField] private float chargeAmount = 12f;
        [SerializeField] private bool isSuperCharge;
        [SerializeField] private float lockOnDurationSeconds = 0.14f;
        [SerializeField] private float magnetFlySpeed = 19f;

        [Header("Visuals & Animation")]
        [SerializeField] private SpriteRenderer innerBoltRenderer;
        [SerializeField] private float bobAmplitude = 0.22f;
        [SerializeField] private float bobFrequency = 2.4f;
        [SerializeField] private Color standardFrameColor = Color.white;
        [SerializeField] private Color superFrameColor = new Color(1.0f, 0.94f, 0.72f, 1f);

        private CircleCollider2D _triggerCollider;
        private SpriteRenderer _frameRenderer;
        private Vector3 _originPosition;
        private Vector3 _baseScale;

        private bool _isCollected;
        private bool _isMagnetized;
        private float _chargeProgress;
        private float _lastExposedTime = -999f;
        private Transform _magnetTarget;
        private BatterySystem _cachedBattery;
        private float _bobPhase;

        public Vector2 WorldPosition => transform.position;
        public HardlightPhase CurrentPhase => _isMagnetized ? HardlightPhase.Solid : HardlightPhase.Phantom;

        private void Awake()
        {
            _triggerCollider = GetComponent<CircleCollider2D>();
            _triggerCollider.isTrigger = true;
            _frameRenderer = GetComponent<SpriteRenderer>();
            _originPosition = transform.position;
            _baseScale = transform.localScale;
            _bobPhase = (_originPosition.x * 0.7f) % (Mathf.PI * 2f);
        }

        private void OnEnable()
        {
            _originPosition = transform.position;
            _bobPhase = (_originPosition.x * 0.7f) % (Mathf.PI * 2f);
            LightReactiveRegistry.Register(this);
            RunnerMotor2D.OnAnyRunnerRespawned += ResetOrbState;
            ResetOrbState();
        }

        private void OnDisable()
        {
            LightReactiveRegistry.Unregister(this);
            RunnerMotor2D.OnAnyRunnerRespawned -= ResetOrbState;
        }

        public void OnSpawnedFromPool()
        {
            _originPosition = transform.position;
            _baseScale = transform.localScale;
            ResetOrbState();
        }

        public void OnReturnedToPool()
        {
            ResetOrbState();
        }

        public void ResetOrbState()
        {
            _isCollected = false;
            _isMagnetized = false;
            _chargeProgress = 0f;
            _lastExposedTime = -999f;
            _magnetTarget = null;
            transform.position = _originPosition;
            transform.localScale = _baseScale;

            if (_triggerCollider != null) _triggerCollider.enabled = true;
            if (_frameRenderer != null)
            {
                _frameRenderer.enabled = true;
                _frameRenderer.color = isSuperCharge ? superFrameColor : standardFrameColor;
            }
            if (innerBoltRenderer != null) innerBoltRenderer.enabled = true;
        }

        public Vector2 GetClosestLightSamplePoint(Vector2 beamOrigin, Vector2 beamDirection)
        {
            return transform.position;
        }

        public void OnLightExposure(Vector2 beamOrigin, float normalizedIntensity, bool isFocusedBeam, float deltaTime)
        {
            if (_isCollected || _isMagnetized) return;

            _lastExposedTime = Time.time;
            float speedMult = isFocusedBeam ? 2.2f : 1.0f;
            _chargeProgress += deltaTime * speedMult;

            if (_chargeProgress >= lockOnDurationSeconds)
            {
                BeginMagnetPull();
            }
        }

        private void BeginMagnetPull()
        {
            _isMagnetized = true;
            if (_cachedBattery == null)
            {
                _cachedBattery = BatterySystem.ActiveInstance;
            }

            if (_cachedBattery != null)
            {
                _magnetTarget = _cachedBattery.transform;
            }
            else
            {
                CollectOrb(null);
            }
        }

        private void Update()
        {
            if (_isCollected) return;

            float dt = Time.deltaTime;
            if (_isMagnetized && _magnetTarget != null)
            {
                Vector3 targetPos = _magnetTarget.position + new Vector3(0f, 0.25f, 0f);
                transform.position = Vector3.MoveTowards(transform.position, targetPos, magnetFlySpeed * dt);
                transform.localScale = _baseScale * 1.18f;

                if (Vector3.SqrMagnitude(transform.position - targetPos) <= 0.32f)
                {
                    CollectOrb(_cachedBattery);
                }
                return;
            }

            // Süzülme & Parlama Animasyonu (Işık değdiğinde %20 büyür)
            float bobY = Mathf.Sin(Time.time * bobFrequency + _bobPhase) * bobAmplitude;
            transform.position = new Vector3(_originPosition.x, _originPosition.y + bobY, _originPosition.z);

            bool beingLit = (Time.time - _lastExposedTime) <= 0.05f;
            if (beingLit)
            {
                float chargeRatio = Mathf.Clamp01(_chargeProgress / lockOnDurationSeconds);
                float pulse = 1f + chargeRatio * 0.25f;
                transform.localScale = _baseScale * pulse;
            }
            else
            {
                _chargeProgress = Mathf.Max(0f, _chargeProgress - dt * 1.5f);
                float idlePulse = 1f + Mathf.Sin(Time.time * 3.5f + _bobPhase) * 0.05f;
                transform.localScale = _baseScale * idlePulse;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isCollected || !other.CompareTag("Player")) return;
            BatterySystem battery = other.GetComponent<BatterySystem>();
            CollectOrb(battery);
        }

        private void CollectOrb(BatterySystem battery)
        {
            if (_isCollected) return;
            _isCollected = true;

            float amount = isSuperCharge ? chargeAmount * 2f : chargeAmount;
            if (battery != null)
            {
                battery.AddCharge(amount);
            }
            else if (_cachedBattery != null)
            {
                _cachedBattery.AddCharge(amount);
            }

            if (_triggerCollider != null) _triggerCollider.enabled = false;
            if (_frameRenderer != null) _frameRenderer.enabled = false;
            if (innerBoltRenderer != null) innerBoltRenderer.enabled = false;

            OnEnergyCollected?.Invoke(this, amount);
        }
    }
}
