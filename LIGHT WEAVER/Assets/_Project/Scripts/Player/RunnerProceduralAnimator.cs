using UnityEngine;

namespace LightWeaver.Player
{
    /// <summary>
    /// Modüler parçalı Trençkotlu Dedektif karakterini prosedürel olarak canlandırır:
    /// Çift eklemli diz/bacak koşu adımı, arka kol dirsek salınımı, rüzgârda dalgalanan trençkot etekleri,
    /// zıplama pozu, iniş Squash & Stretch ve sıcak kum/ışık kıvılcımları.
    /// </summary>
    public sealed class RunnerProceduralAnimator : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private RunnerMotor2D motor;
        [SerializeField] private BatterySystem batterySystem;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform bodyPivot;
        [SerializeField] private Transform frontLegPivot;
        [SerializeField] private Transform frontShinPivot;
        [SerializeField] private Transform backLegPivot;
        [SerializeField] private Transform backShinPivot;
        [SerializeField] private Transform backArmPivot;
        [SerializeField] private Transform backForearmPivot;
        [SerializeField] private Transform coatTailFrontPivot;
        [SerializeField] private Transform coatTailBackPivot;
        [SerializeField] private Transform suitBatteryGaugeFill;
        [SerializeField] private SpriteRenderer suitBatteryGaugeRenderer;

        [Header("VFX & Trails")]
        [SerializeField] private ParticleSystem footstepSparks;
        [SerializeField] private TrailRenderer speedTrail;

        [Header("Animation Tuning")]
        [SerializeField] private float strideFrequency = 13.5f;
        [SerializeField] private float legSwingAngleDeg = 46f;
        [SerializeField] private float forwardLeanAngleDeg = -14f;

        private float _strideTimer;
        private float _squashTimer;
        private Vector3 _baseRootScale = Vector3.one;

        private void Awake()
        {
            if (motor == null) motor = GetComponent<RunnerMotor2D>();
            if (batterySystem == null) batterySystem = GetComponent<BatterySystem>();
            if (visualRoot != null) _baseRootScale = visualRoot.localScale;
        }

        private void OnEnable()
        {
            if (motor != null) motor.OnRunnerLanded += HandleRunnerLanded;
        }

        private void OnDisable()
        {
            if (motor != null) motor.OnRunnerLanded -= HandleRunnerLanded;
        }

        private void HandleRunnerLanded(float impactSpeed)
        {
            _squashTimer = 0.18f;
            if (footstepSparks != null)
            {
                footstepSparks.Emit(10);
            }
        }

        private void Update()
        {
            if (motor == null) return;

            float dt = Time.deltaTime;
            bool grounded = motor.IsGrounded;
            float speedNorm = motor.NormalizedSpeed;

            // 1. İniş Squash & Stretch
            if (visualRoot != null)
            {
                if (_squashTimer > 0f)
                {
                    _squashTimer = Mathf.Max(0f, _squashTimer - dt);
                    float t = _squashTimer / 0.18f;
                    float sq = Mathf.Sin(t * Mathf.PI);
                    visualRoot.localScale = new Vector3(
                        _baseRootScale.x * (1f + sq * 0.22f),
                        _baseRootScale.y * (1f - sq * 0.22f),
                        1f);
                }
                else
                {
                    visualRoot.localScale = _baseRootScale;
                }
            }

            // 2. Gövde Koşu Eğimi & Hafif Yaylanma (Bob)
            if (bodyPivot != null)
            {
                float targetLean = grounded ? forwardLeanAngleDeg + motor.CurrentSlopeAngleDeg * 0.35f : -8f;
                float bobOffset = grounded ? Mathf.Abs(Mathf.Sin(_strideTimer * 2f)) * 0.05f : 0f;
                bodyPivot.localRotation = Quaternion.Lerp(bodyPivot.localRotation, Quaternion.Euler(0f, 0f, targetLean), dt * 14f);
                bodyPivot.localPosition = new Vector3(0f, bobOffset, 0f);
            }

            // 3. Modüler Çift Eklemli Bacak + Diz + Arka Kol + Trençkot Etek Animasyonu
            if (grounded)
            {
                _strideTimer += dt * strideFrequency * Mathf.Max(0.4f, speedNorm);
                float swing = Mathf.Sin(_strideTimer) * legSwingAngleDeg;
                float frontKnee = Mathf.Clamp((-Mathf.Cos(_strideTimer) * 0.5f + 0.5f) * -58f, -62f, 4f);
                float backKnee = Mathf.Clamp((Mathf.Cos(_strideTimer) * 0.5f + 0.5f) * -58f, -62f, 4f);

                if (frontLegPivot != null) frontLegPivot.localRotation = Quaternion.Euler(0f, 0f, swing);
                if (frontShinPivot != null) frontShinPivot.localRotation = Quaternion.Euler(0f, 0f, frontKnee);
                if (backLegPivot != null) backLegPivot.localRotation = Quaternion.Euler(0f, 0f, -swing);
                if (backShinPivot != null) backShinPivot.localRotation = Quaternion.Euler(0f, 0f, backKnee);
                if (backArmPivot != null) backArmPivot.localRotation = Quaternion.Euler(0f, 0f, -swing * 0.72f - 12f);
                if (backForearmPivot != null) backForearmPivot.localRotation = Quaternion.Euler(0f, 0f, 26f + Mathf.Max(0f, -swing * 0.35f));
            }
            else
            {
                // Havada atletik sıçrama / süzülme pozu (dizler kırık)
                float airPitch = Mathf.Clamp(motor.CurrentSpeedY * 2.2f, -24f, 24f);
                if (frontLegPivot != null) frontLegPivot.localRotation = Quaternion.Lerp(frontLegPivot.localRotation, Quaternion.Euler(0f, 0f, 34f + airPitch), dt * 11f);
                if (frontShinPivot != null) frontShinPivot.localRotation = Quaternion.Lerp(frontShinPivot.localRotation, Quaternion.Euler(0f, 0f, -48f), dt * 11f);
                if (backLegPivot != null) backLegPivot.localRotation = Quaternion.Lerp(backLegPivot.localRotation, Quaternion.Euler(0f, 0f, -32f + airPitch * 0.5f), dt * 11f);
                if (backShinPivot != null) backShinPivot.localRotation = Quaternion.Lerp(backShinPivot.localRotation, Quaternion.Euler(0f, 0f, -24f), dt * 11f);
                if (backArmPivot != null) backArmPivot.localRotation = Quaternion.Lerp(backArmPivot.localRotation, Quaternion.Euler(0f, 0f, -46f), dt * 11f);
                if (backForearmPivot != null) backForearmPivot.localRotation = Quaternion.Lerp(backForearmPivot.localRotation, Quaternion.Euler(0f, 0f, 38f), dt * 11f);
            }

            // 3.5 Rüzgârda Dalgalanan Trençkot Etekleri
            float windFlutter = Mathf.Sin(Time.time * 13f) * (3.5f + speedNorm * 6.5f);
            float baseFlap = -10f - speedNorm * 22f;
            if (coatTailFrontPivot != null) coatTailFrontPivot.localRotation = Quaternion.Euler(0f, 0f, baseFlap + windFlutter);
            if (coatTailBackPivot != null) coatTailBackPivot.localRotation = Quaternion.Euler(0f, 0f, baseFlap * 1.15f - windFlutter * 0.85f);

            // 4. Ayak Altı Kıvılcımları & Hız İzi
            if (footstepSparks != null)
            {
                var emission = footstepSparks.emission;
                emission.enabled = grounded && speedNorm > 0.25f;
            }
            if (speedTrail != null)
            {
                speedTrail.emitting = speedNorm > 0.35f;
            }

            // 5. Diegetik Pirinç Çanta Pil Göstergesi (Zero-GC)
            if (batterySystem != null && suitBatteryGaugeFill != null)
            {
                float norm = batterySystem.NormalizedBattery;
                suitBatteryGaugeFill.localScale = new Vector3(1f, Mathf.Max(0.02f, norm), 1f);
                if (suitBatteryGaugeRenderer != null)
                {
                    suitBatteryGaugeRenderer.color = batterySystem.CurrentPhase switch
                    {
                        SpectrumPhase.Overcharge => new Color(0.10f, 0.92f, 0.82f, 1f),
                        SpectrumPhase.CriticalEmber => new Color(1.0f, 0.20f, 0.28f, 1f),
                        _ => Color.Lerp(new Color(0.95f, 0.38f, 0.14f, 1f), new Color(1.0f, 0.84f, 0.24f, 1f), norm)
                    };
                }
            }
        }
    }
}
