using UnityEngine;

namespace LightWeaver.Player
{
    /// <summary>
    /// Referans konsept görselindeki atletik cyber-koşucuyu prosedürel olarak canlandırır:
    /// Bacak koşu adımı, havada süzülme pozu, rampa eğilmesi, inişte Squash & Stretch
    /// ve kristal zemine bastığında çıkan ayak altı kıvılcımları.
    /// </summary>
    public sealed class RunnerProceduralAnimator : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private RunnerMotor2D motor;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform bodyPivot;
        [SerializeField] private Transform frontLegPivot;
        [SerializeField] private Transform backLegPivot;
        [SerializeField] private Transform backArmPivot;

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

            // 3. Bacak ve Arka Kol Adım Döngüsü
            if (grounded)
            {
                _strideTimer += dt * strideFrequency * Mathf.Max(0.4f, speedNorm);
                float swing = Mathf.Sin(_strideTimer) * legSwingAngleDeg;

                if (frontLegPivot != null) frontLegPivot.localRotation = Quaternion.Euler(0f, 0f, swing);
                if (backLegPivot != null) backLegPivot.localRotation = Quaternion.Euler(0f, 0f, -swing);
                if (backArmPivot != null) backArmPivot.localRotation = Quaternion.Euler(0f, 0f, swing * 0.75f - 20f);
            }
            else
            {
                // Havada atletik sıçrama / süzülme pozu
                float airPitch = Mathf.Clamp(motor.CurrentSpeedY * 2.2f, -24f, 24f);
                if (frontLegPivot != null) frontLegPivot.localRotation = Quaternion.Lerp(frontLegPivot.localRotation, Quaternion.Euler(0f, 0f, 28f + airPitch), dt * 10f);
                if (backLegPivot != null) backLegPivot.localRotation = Quaternion.Lerp(backLegPivot.localRotation, Quaternion.Euler(0f, 0f, -34f + airPitch * 0.5f), dt * 10f);
                if (backArmPivot != null) backArmPivot.localRotation = Quaternion.Lerp(backArmPivot.localRotation, Quaternion.Euler(0f, 0f, -42f), dt * 10f);
            }

            // 4. Ayak Altı Kristal Kıvılcımları & Hız İzi
            if (footstepSparks != null)
            {
                var emission = footstepSparks.emission;
                emission.enabled = grounded && speedNorm > 0.25f;
            }
            if (speedTrail != null)
            {
                speedTrail.emitting = speedNorm > 0.35f;
            }
        }
    }
}
