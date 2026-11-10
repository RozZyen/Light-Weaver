using System;
using LightWeaver.World;
using UnityEngine;

namespace LightWeaver.Player
{
    public enum SpectrumPhase : byte
    {
        Normal = 0,
        Overcharge = 1,
        CriticalEmber = 2
    }

    /// <summary>
    /// Fener pil ekonomisi, mesafe bazlı tüketim eğrisi, 3 Spektrum Fazı (Normal, Overcharge, Kritik Kor)
    /// ve Son An Kristalleştirmesi (Perfect Weave) ödül mekaniklerini yönetir.
    /// </summary>
    public sealed class BatterySystem : MonoBehaviour
    {
        public static BatterySystem ActiveInstance { get; private set; }

        public event Action<float, float> OnBatteryChanged; // (current, max)
        public event Action<SpectrumPhase> OnSpectrumPhaseChanged;
        public event Action<Vector2, float> OnPerfectWeaveTriggered; // (worldPos, bonusCharge)
        public event Action OnBatteryDepleted;

        [Header("Core References")]
        [SerializeField] private FlashlightController2D flashlightController;
        [SerializeField] private RunnerMotor2D runnerMotor;

        [Header("Battery Economy")]
        [SerializeField] private float maxBattery = 100f;
        [SerializeField] private float baseDrainPerSecond = 2.4f;
        [SerializeField] private float maxDistanceDrainPerSecond = 4.6f;
        [SerializeField] private float distanceForMaxDrain = 350f;
        [SerializeField] private float focusedBeamDrainMultiplier = 1.45f;

        [Header("Spectrum Phase Thresholds & Multipliers")]
        [SerializeField] private float overchargeThreshold = 95f;
        [SerializeField] private float criticalThreshold = 20f;
        [SerializeField] private int perfectWeaveStreakForOvercharge = 6;
        [SerializeField] private float perfectWeaveMaxLeadDistance = 3.4f;
        [SerializeField] private float perfectWeaveChargeReward = 3.0f;

        [Header("Phase Visual Tints")]
        [SerializeField] private Color normalBeamTint = Color.white;
        [SerializeField] private Color overchargeBeamTint = new Color(0.85f, 1.0f, 1.0f, 1f);
        [SerializeField] private Color criticalBeamTint = new Color(1.0f, 0.58f, 0.25f, 1f);

        private float _currentBattery;
        private float _startX;
        private float _spawnTime;
        private int _perfectWeaveStreak;
        private SpectrumPhase _currentPhase = SpectrumPhase.Normal;
        private bool _isDepleted;
        private float _hazardInvulnTimer;
        private float _distanceDrainMultiplier = 1.0f;
        private float _blessingDrainMultiplier = 1.0f;
        private float _blessingChargeMultiplier = 1.0f;
        private bool _isSimulationPaused;

        public void SetBlessingModifiers(float drainMult, float chargeMult)
        {
            _blessingDrainMultiplier = Mathf.Clamp(drainMult, 0.5f, 1.5f);
            _blessingChargeMultiplier = Mathf.Clamp(chargeMult, 1.0f, 2.0f);
        }

        public void SetSimulationPaused(bool paused)
        {
            _isSimulationPaused = paused;
        }

        public float CurrentBattery => _currentBattery;
        public float MaxBattery => maxBattery;
        public float NormalizedBattery => maxBattery > 0.01f ? Mathf.Clamp01(_currentBattery / maxBattery) : 0f;
        public SpectrumPhase CurrentPhase => _currentPhase;
        public int PerfectWeaveStreak => _perfectWeaveStreak;
        public float ScoreMultiplier => _currentPhase == SpectrumPhase.Overcharge ? 3.0f : 1.0f;

        private void Awake()
        {
            ActiveInstance = this;
            if (flashlightController == null) flashlightController = GetComponentInChildren<FlashlightController2D>();
            if (runnerMotor == null) runnerMotor = GetComponent<RunnerMotor2D>();
            ResetBattery();
        }

        private void OnDestroy()
        {
            if (ActiveInstance == this) ActiveInstance = null;
        }

        private void OnEnable()
        {
            CrystalPlatform.OnPlatformSolidified += HandlePlatformSolidified;
            if (runnerMotor != null) runnerMotor.OnRunnerFellIntoVoid += ResetBattery;
        }

        private void OnDisable()
        {
            CrystalPlatform.OnPlatformSolidified -= HandlePlatformSolidified;
            if (runnerMotor != null) runnerMotor.OnRunnerFellIntoVoid -= ResetBattery;
        }

        public void ResetBattery()
        {
            _currentBattery = maxBattery;
            _startX = transform.position.x;
            _spawnTime = Time.time;
            _perfectWeaveStreak = 0;
            _isDepleted = false;
            _hazardInvulnTimer = 0f;
            EvaluatePhase(forceNotify: true);
            OnBatteryChanged?.Invoke(_currentBattery, maxBattery);
        }

        public void DrainExternal(float amount)
        {
            if (amount <= 0f || _isDepleted) return;
            _currentBattery = Mathf.Max(0f, _currentBattery - amount);
            if (_currentBattery <= 0.001f)
            {
                _currentBattery = 0f;
                _isDepleted = true;
                OnBatteryDepleted?.Invoke();
            }
            EvaluatePhase(forceNotify: false);
            OnBatteryChanged?.Invoke(_currentBattery, maxBattery);
        }

        public void TakeHazardShock(float batteryPenalty)
        {
            if (_hazardInvulnTimer > 0f) return;
            _hazardInvulnTimer = 0.85f;
            _perfectWeaveStreak = 0;
            DrainExternal(batteryPenalty);
        }

        public void AddCharge(float amount)
        {
            if (amount <= 0f) return;

            // Kritik Kor (<%20) fazında yüksek risk/ödül: alınan enerji hücreleri 2 kat şarj verir!
            float multiplier = (_currentPhase == SpectrumPhase.CriticalEmber ? 2.0f : 1.0f) * _blessingChargeMultiplier;
            _currentBattery = Mathf.Clamp(_currentBattery + amount * multiplier, 0f, maxBattery);

            if (_currentBattery > 0f) _isDepleted = false;

            EvaluatePhase(forceNotify: false);
            OnBatteryChanged?.Invoke(_currentBattery, maxBattery);
        }

        private void HandlePlatformSolidified(CrystalPlatform platform, float intensity)
        {
            if (platform == null || (Time.time - _spawnTime) < 0.45f || _isDepleted) return;

            float deltaX = platform.WorldPosition.x - transform.position.x;
            if (deltaX >= 0.4f && deltaX <= perfectWeaveMaxLeadDistance)
            {
                _perfectWeaveStreak++;
                _currentBattery = Mathf.Clamp(_currentBattery + perfectWeaveChargeReward, 0f, maxBattery);
                OnPerfectWeaveTriggered?.Invoke(platform.WorldPosition, perfectWeaveChargeReward);
                OnBatteryChanged?.Invoke(_currentBattery, maxBattery);
                EvaluatePhase(forceNotify: false);
            }
            else if (deltaX > perfectWeaveMaxLeadDistance + 2.5f)
            {
                _perfectWeaveStreak = 0;
            }
        }

        public void SetSectorDrainMultiplier(float multiplier)
        {
            _distanceDrainMultiplier = Mathf.Clamp(multiplier, 0.5f, 2.5f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_isSimulationPaused)
            {
                ApplyConeVisuals(dt);
                return;
            }
            if (_hazardInvulnTimer > 0f) _hazardInvulnTimer -= dt;

            if (_isDepleted)
            {
                ApplyConeVisuals(0f);
                return;
            }

            float distanceTraveled = Mathf.Max(0f, transform.position.x - _startX);
            float distFactor = Mathf.Clamp01(distanceTraveled / Mathf.Max(1f, distanceForMaxDrain));
            float drainRate = Mathf.Lerp(baseDrainPerSecond, maxDistanceDrainPerSecond, distFactor) * _distanceDrainMultiplier * _blessingDrainMultiplier;

            if (flashlightController != null && flashlightController.IsFocusedBeam)
            {
                drainRate *= focusedBeamDrainMultiplier;
            }

            _currentBattery = Mathf.Max(0f, _currentBattery - drainRate * dt);
            if (_currentBattery <= 0.001f)
            {
                _currentBattery = 0f;
                _isDepleted = true;
                OnBatteryDepleted?.Invoke();
            }

            EvaluatePhase(forceNotify: false);
            ApplyConeVisuals(dt);
            OnBatteryChanged?.Invoke(_currentBattery, maxBattery);
        }

        private void EvaluatePhase(bool forceNotify)
        {
            SpectrumPhase nextPhase;
            if (_currentBattery < criticalThreshold)
            {
                nextPhase = SpectrumPhase.CriticalEmber;
            }
            else if (_currentBattery >= overchargeThreshold || _perfectWeaveStreak >= perfectWeaveStreakForOvercharge)
            {
                nextPhase = SpectrumPhase.Overcharge;
            }
            else
            {
                nextPhase = SpectrumPhase.Normal;
            }

            if (forceNotify || nextPhase != _currentPhase)
            {
                _currentPhase = nextPhase;
                if (runnerMotor != null)
                {
                    float speedMult = _currentPhase switch
                    {
                        SpectrumPhase.Overcharge => 1.12f,
                        SpectrumPhase.CriticalEmber => 1.15f, // Adrenalin hızı!
                        _ => 1.0f
                    };
                    runnerMotor.SetSpeedMultiplier(speedMult);
                }
                OnSpectrumPhaseChanged?.Invoke(_currentPhase);
            }
        }

        private void ApplyConeVisuals(float dt)
        {
            if (flashlightController == null) return;

            float norm = NormalizedBattery;
            if (_isDepleted)
            {
                flashlightController.SetSpectrumVisuals(0f, 0.4f, 0.6f, criticalBeamTint);
                return;
            }

            switch (_currentPhase)
            {
                case SpectrumPhase.Overcharge:
                    float rainbowHue = Mathf.Repeat(Time.time * 0.45f, 1f);
                    Color prismTint = Color.Lerp(overchargeBeamTint, Color.HSVToRGB(rainbowHue, 0.35f, 1f), 0.45f);
                    flashlightController.SetSpectrumVisuals(1.0f, 1.18f, 1.32f, prismTint);
                    break;

                case SpectrumPhase.CriticalEmber:
                    // Kritik pilde belirgin titreme (Ember Flicker)
                    float flicker = 0.62f + Mathf.PerlinNoise(Time.time * 22f, 0f) * 0.35f;
                    float eff = Mathf.Clamp(norm * 2.2f * flicker, 0.25f, 0.72f);
                    flashlightController.SetSpectrumVisuals(eff, 0.85f, 0.82f, criticalBeamTint);
                    break;

                default:
                    float smoothEff = Mathf.Lerp(0.68f, 1.0f, norm);
                    flashlightController.SetSpectrumVisuals(smoothEff, 1.0f, 1.0f, normalBeamTint);
                    break;
            }
        }
    }
}
