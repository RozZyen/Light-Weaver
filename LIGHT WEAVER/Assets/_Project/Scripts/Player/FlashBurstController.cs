using System;
using System.Collections.Generic;
using LightWeaver.Core;
using LightWeaver.World;
using UnityEngine;

namespace LightWeaver.Player
{
    /// <summary>
    /// Çift dokunuş veya F/E tuşu ile tetiklenen radyal Parlama Patlaması (Flash-Burst Parry).
    /// Yakındaki gölge tehlikelerini parçalar/dondurur ve acil durumda yakındaki kristal platformları
    /// geçici olarak katılaştırır.
    /// </summary>
    public sealed class FlashBurstController : MonoBehaviour
    {
        public event Action OnBurstExecuted;

        [Header("Core References")]
        [SerializeField] private LightTouchInput touchInput;
        [SerializeField] private BatterySystem batterySystem;
        [SerializeField] private CameraFollow2D cameraFollow;
        [SerializeField] private SpriteRenderer shockwaveRingRenderer;

        [Header("Burst Parameters")]
        [SerializeField] private float burstRadius = 8.0f;
        [SerializeField] private float batteryCost = 7.5f;
        [SerializeField] private float cooldownSeconds = 4.0f;
        [SerializeField] private float ringAnimDuration = 0.32f;

        private float _cooldownTimer;
        private float _ringAnimTimer;
        private float _cooldownMultiplier = 1.0f;

        public float CooldownRemaining => _cooldownTimer;
        public float CooldownNormalized => (cooldownSeconds * _cooldownMultiplier) > 0.01f
            ? Mathf.Clamp01(1f - (_cooldownTimer / (cooldownSeconds * _cooldownMultiplier)))
            : 1f;

        public void SetCooldownMultiplier(float mult)
        {
            _cooldownMultiplier = Mathf.Clamp(mult, 0.25f, 1.5f);
        }

        private void Awake()
        {
            if (touchInput == null) touchInput = GetComponent<LightTouchInput>();
            if (batterySystem == null) batterySystem = GetComponent<BatterySystem>();
            if (shockwaveRingRenderer != null) shockwaveRingRenderer.enabled = false;
        }

        private void OnEnable()
        {
            if (touchInput != null) touchInput.OnFlashBurstTriggered += TryTriggerFlashBurst;
            RunnerMotor2D.OnAnyRunnerRespawned += ResetCooldown;
        }

        private void OnDisable()
        {
            if (touchInput != null) touchInput.OnFlashBurstTriggered -= TryTriggerFlashBurst;
            RunnerMotor2D.OnAnyRunnerRespawned -= ResetCooldown;
        }

        private void ResetCooldown()
        {
            _cooldownTimer = 0f;
            _ringAnimTimer = 0f;
            if (shockwaveRingRenderer != null) shockwaveRingRenderer.enabled = false;
        }

        public void TryTriggerFlashBurst()
        {
            if (_cooldownTimer > 0f || batterySystem == null || batterySystem.CurrentBattery <= batteryCost) return;

            batterySystem.DrainExternal(batteryCost);
            _cooldownTimer = cooldownSeconds * _cooldownMultiplier;
            _ringAnimTimer = ringAnimDuration;

            Vector2 origin = transform.position;
            float radiusSq = burstRadius * burstRadius;
            IReadOnlyList<ILightReactive> all = LightReactiveRegistry.All;
            int count = all.Count;

            for (int i = 0; i < count; i++)
            {
                ILightReactive reactive = all[i];
                if (reactive == null) continue;

                float distSq = (reactive.WorldPosition - origin).sqrMagnitude;
                if (distSq > radiusSq) continue;

                if (reactive is ShadowHazard hazard)
                {
                    hazard.ApplyFlashBurstEffect();
                }
                else if (reactive is PrismLockGate gate)
                {
                    gate.ForceUnlockByBurst();
                }
                else if (reactive is CrystalPlatform plat && plat.Variant != CrystalVariant.UmbraBridge)
                {
                    plat.OnLightExposure(origin, 1.0f, true, 0.25f);
                }
                else if (reactive is PrismaticJumpPad)
                {
                    reactive.OnLightExposure(origin, 1.0f, true, 0.25f);
                }
            }

            if (cameraFollow != null)
            {
                cameraFollow.TriggerShake(0.18f, 0.16f);
            }

            OnBurstExecuted?.Invoke();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_cooldownTimer > 0f) _cooldownTimer = Mathf.Max(0f, _cooldownTimer - dt);

            if (_ringAnimTimer > 0f && shockwaveRingRenderer != null)
            {
                _ringAnimTimer -= dt;
                float progress = 1f - Mathf.Clamp01(_ringAnimTimer / ringAnimDuration);
                shockwaveRingRenderer.enabled = _ringAnimTimer > 0f;
                float scale = Mathf.Lerp(0.6f, burstRadius * 2f, progress);
                shockwaveRingRenderer.transform.localScale = new Vector3(scale, scale, 1f);
                shockwaveRingRenderer.color = new Color(0.55f, 0.96f, 1.0f, (1f - progress) * 0.78f);
            }
        }
    }
}
