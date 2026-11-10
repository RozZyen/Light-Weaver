using System;
using LightWeaver.Core;
using LightWeaver.Player;
using LightWeaver.Pooling;
using UnityEngine;

namespace LightWeaver.World
{
    public enum ShadowHazardType : byte
    {
        MovingShadowPillar = 0, // Dikey salınımlı gölge sütunu — üstünden/altından geçilir veya Flash-Burst ile dondurulur
        UmbraStalker = 1,       // Işık konisi değdiğinde 0.22s içinde yanarak eriyen gölge avcısı
        VoidLeech = 2           // Işık tutulursa pil emen tuzak küre — ışıktan kaçın veya Flash-Burst ile patlat!
    }

    /// <summary>
    /// Karanlıktan çıkan 3 sistemik gölge engeli (MovingShadowPillar, UmbraStalker, VoidLeech).
    /// Zero-GC arayüzü ve nesne havuzu (IPoolable) ile çalışır.
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed class ShadowHazard : MonoBehaviour, ILightReactive, IPoolable
    {
        public static event Action<ShadowHazard, bool> OnHazardResolved; // (hazard, destroyedByPlayerSkill)

        [Header("Hazard Type & Behavior")]
        [SerializeField] private ShadowHazardType hazardType = ShadowHazardType.UmbraStalker;
        [SerializeField] private float batteryDamageOnHit = 24f;
        [SerializeField] private float meltDurationSeconds = 0.22f;
        [SerializeField] private float leechDrainPerSecond = 11f;

        [Header("Motion Parameters")]
        [SerializeField] private float oscillationAmplitude = 1.6f;
        [SerializeField] private float oscillationSpeed = 2.1f;
        [SerializeField] private float stalkerDriftSpeed = 1.35f;

        [Header("Visual Colors")]
        [SerializeField] private Color pillarColor = new Color(0.18f, 0.04f, 0.28f, 0.96f);
        [SerializeField] private Color stalkerColor = new Color(0.95f, 0.14f, 0.34f, 0.94f);
        [SerializeField] private Color leechColor = new Color(0.55f, 0.08f, 0.85f, 0.96f);

        private Collider2D _collider;
        private SpriteRenderer _renderer;
        private Vector3 _originPosition;
        private Vector3 _baseScale;

        private bool _isNeutralized;
        private float _meltTimer;
        private float _freezeTimer;
        private float _lastExposedTime = -999f;
        private float _phaseOffset;

        public ShadowHazardType HazardType => hazardType;
        public Vector2 WorldPosition => transform.position;
        public HardlightPhase CurrentPhase => _isNeutralized ? HardlightPhase.Phantom : HardlightPhase.Solid;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _collider.isTrigger = true;
            _renderer = GetComponent<SpriteRenderer>();
            _originPosition = transform.position;
            _baseScale = transform.localScale;
            _phaseOffset = (_originPosition.x * 0.55f) % (Mathf.PI * 2f);
        }

        private void OnEnable()
        {
            LightReactiveRegistry.Register(this);
            RunnerMotor2D.OnAnyRunnerRespawned += ResetHazard;
            ResetHazard();
        }

        private void OnDisable()
        {
            LightReactiveRegistry.Unregister(this);
            RunnerMotor2D.OnAnyRunnerRespawned -= ResetHazard;
        }

        public void OnSpawnedFromPool()
        {
            _originPosition = transform.position;
            _baseScale = transform.localScale;
            _phaseOffset = (_originPosition.x * 0.55f) % (Mathf.PI * 2f);
            ResetHazard();
        }

        public void OnReturnedToPool()
        {
            ResetHazard();
        }

        public void ConfigureOrigin(Vector3 worldPos, ShadowHazardType type)
        {
            hazardType = type;
            _originPosition = worldPos;
            transform.position = worldPos;
            ResetHazard();
        }

        public void ResetHazard()
        {
            _isNeutralized = false;
            _meltTimer = 0f;
            _freezeTimer = 0f;
            _lastExposedTime = -999f;
            transform.position = _originPosition;
            transform.localScale = _baseScale;

            if (_collider != null) _collider.enabled = true;
            if (_renderer != null)
            {
                _renderer.enabled = true;
                _renderer.color = GetBaseColor();
            }
        }

        public Vector2 GetClosestLightSamplePoint(Vector2 beamOrigin, Vector2 beamDirection)
        {
            return transform.position;
        }

        public void OnLightExposure(Vector2 beamOrigin, float normalizedIntensity, bool isFocusedBeam, float deltaTime)
        {
            if (_isNeutralized) return;
            _lastExposedTime = Time.time;

            switch (hazardType)
            {
                case ShadowHazardType.UmbraStalker:
                    float meltMult = isFocusedBeam ? 2.4f : 1.0f;
                    _meltTimer += deltaTime * meltMult;
                    float ratio = Mathf.Clamp01(_meltTimer / meltDurationSeconds);
                    if (_renderer != null)
                    {
                        _renderer.color = Color.Lerp(stalkerColor, new Color(1f, 0.92f, 0.55f, 1f), ratio);
                    }
                    transform.localScale = _baseScale * (1f - ratio * 0.35f);
                    if (_meltTimer >= meltDurationSeconds)
                    {
                        NeutralizeBySkill(rewardBattery: 3.5f);
                    }
                    break;

                case ShadowHazardType.VoidLeech:
                    // Işık tutulursa şişer ve fener pilini emer! (Oyuncuyu ışığı çekmeye veya Flash-Burst atmaya zorlar)
                    transform.localScale = _baseScale * (1.28f + Mathf.Sin(Time.time * 24f) * 0.08f);
                    if (_renderer != null)
                    {
                        _renderer.color = new Color(1f, 0.12f, 0.45f, 1f);
                    }
                    BatterySystem bat = BatterySystem.ActiveInstance;
                    if (bat != null)
                    {
                        bat.DrainExternal(leechDrainPerSecond * deltaTime);
                    }
                    break;
            }
        }

        public void ApplyFlashBurstEffect()
        {
            if (_isNeutralized) return;

            if (hazardType == ShadowHazardType.MovingShadowPillar)
            {
                _freezeTimer = 1.4f;
                if (_renderer != null) _renderer.color = new Color(0.45f, 0.85f, 1f, 0.55f);
            }
            else
            {
                NeutralizeBySkill(rewardBattery: 6.0f);
            }
        }

        private void NeutralizeBySkill(float rewardBattery)
        {
            _isNeutralized = true;
            if (_collider != null) _collider.enabled = false;
            if (_renderer != null) _renderer.enabled = false;

            if (rewardBattery > 0f && BatterySystem.ActiveInstance != null)
            {
                BatterySystem.ActiveInstance.AddCharge(rewardBattery);
            }

            OnHazardResolved?.Invoke(this, true);
        }

        private void Update()
        {
            if (_isNeutralized) return;

            float dt = Time.deltaTime;
            if (_freezeTimer > 0f)
            {
                _freezeTimer -= dt;
                if (_freezeTimer <= 0f && _renderer != null) _renderer.color = GetBaseColor();
                return;
            }

            bool beingLit = (Time.time - _lastExposedTime) <= 0.05f;
            if (!beingLit)
            {
                if (_meltTimer > 0f) _meltTimer = Mathf.Max(0f, _meltTimer - dt * 1.5f);
                transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, dt * 10f);
                if (_renderer != null) _renderer.color = GetBaseColor();
            }

            switch (hazardType)
            {
                case ShadowHazardType.MovingShadowPillar:
                    float offsetY = Mathf.Sin(Time.time * oscillationSpeed + _phaseOffset) * oscillationAmplitude;
                    transform.position = new Vector3(_originPosition.x, _originPosition.y + offsetY, _originPosition.z);
                    break;

                case ShadowHazardType.UmbraStalker:
                    float hoverY = Mathf.Sin(Time.time * 3.2f + _phaseOffset) * 0.45f;
                    BatterySystem player = BatterySystem.ActiveInstance;
                    float targetX = transform.position.x;
                    if (player != null)
                    {
                        float distX = transform.position.x - player.transform.position.x;
                        if (distX > 0.5f && distX < 13f)
                        {
                            targetX -= stalkerDriftSpeed * dt;
                        }
                    }
                    transform.position = new Vector3(targetX, _originPosition.y + hoverY, _originPosition.z);
                    break;

                case ShadowHazardType.VoidLeech:
                    float pulse = 1f + Mathf.Sin(Time.time * 4.5f + _phaseOffset) * 0.08f;
                    if (!beingLit) transform.localScale = _baseScale * pulse;
                    transform.Rotate(0f, 0f, -45f * dt);
                    break;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isNeutralized || !other.CompareTag("Player")) return;

            BatterySystem bat = BatterySystem.ActiveInstance;
            if (bat != null)
            {
                bat.TakeHazardShock(batteryDamageOnHit);
            }

            _isNeutralized = true;
            if (_collider != null) _collider.enabled = false;
            if (_renderer != null) _renderer.enabled = false;
            OnHazardResolved?.Invoke(this, false);
        }

        private Color GetBaseColor()
        {
            return hazardType switch
            {
                ShadowHazardType.MovingShadowPillar => pillarColor,
                ShadowHazardType.UmbraStalker => stalkerColor,
                ShadowHazardType.VoidLeech => leechColor,
                _ => stalkerColor
            };
        }
    }
}
