using System.Collections;
using LightWeaver.Core;
using LightWeaver.Player;
using LightWeaver.World;
using UnityEngine;

namespace LightWeaver.Feedbacks
{
    /// <summary>
    /// Hit-Stop mikro dondurma, Camera Shake ve mobil dokunsal titreşimi (Haptics)
    /// merkezi ve güvenli şekilde koordine eder.
    /// </summary>
    public sealed class JuiceFeedbackManager : MonoBehaviour
    {
        [SerializeField] private CameraFollow2D cameraFollow;
        [SerializeField] private BatterySystem batterySystem;

        private bool _hitStopActive;

        private void OnEnable()
        {
            PrismaticJumpPad.OnJumpPadLaunched += HandleJumpPadLaunched;
            ShadowHazard.OnHazardResolved += HandleHazardResolved;
            EnergyOrb.OnEnergyCollected += HandleEnergyCollected;
            if (batterySystem != null) batterySystem.OnPerfectWeaveTriggered += HandlePerfectWeave;
        }

        private void OnDisable()
        {
            PrismaticJumpPad.OnJumpPadLaunched -= HandleJumpPadLaunched;
            ShadowHazard.OnHazardResolved -= HandleHazardResolved;
            EnergyOrb.OnEnergyCollected -= HandleEnergyCollected;
            if (batterySystem != null) batterySystem.OnPerfectWeaveTriggered -= HandlePerfectWeave;
        }

        private void HandleJumpPadLaunched(PrismaticJumpPad pad)
        {
            if (cameraFollow != null) cameraFollow.TriggerShake(0.14f, 0.14f);
            TriggerLightHaptic();
        }

        private void HandlePerfectWeave(Vector2 pos, float bonus)
        {
            TriggerHitStop(0.032f);
            TriggerLightHaptic();
        }

        private void HandleEnergyCollected(EnergyOrb orb, float amount)
        {
            TriggerLightHaptic();
        }

        private void HandleHazardResolved(ShadowHazard hazard, bool bySkill)
        {
            if (bySkill)
            {
                if (cameraFollow != null) cameraFollow.TriggerShake(0.12f, 0.12f);
                TriggerLightHaptic();
            }
            else
            {
                if (cameraFollow != null) cameraFollow.TriggerShake(0.28f, 0.22f);
                TriggerHitStop(0.045f);
                TriggerHeavyHaptic();
            }
        }

        private void TriggerHitStop(float unscaledSeconds)
        {
            if (_hitStopActive || Time.timeScale <= 0.01f) return;
            StartCoroutine(HitStopCoroutine(unscaledSeconds));
        }

        private IEnumerator HitStopCoroutine(float duration)
        {
            _hitStopActive = true;
            float prevScale = Time.timeScale;
            Time.timeScale = 0.06f;
            yield return new WaitForSecondsRealtime(duration);
            if (GameFlowManager.ActiveInstance == null || GameFlowManager.ActiveInstance.CurrentState == GameState.Playing)
            {
                Time.timeScale = prevScale > 0.1f ? prevScale : 1f;
            }
            _hitStopActive = false;
        }

        private static void TriggerLightHaptic()
        {
            if (!ProgressManager.HapticsEnabled) return;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        private static void TriggerHeavyHaptic()
        {
            if (!ProgressManager.HapticsEnabled) return;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
