using LightWeaver.Core;
using LightWeaver.Player;
using LightWeaver.World;
using UnityEngine;

namespace LightWeaver.Feedbacks
{
    /// <summary>
    /// Sıfır tahsisli (Zero-GC EmitParams) dünya parçacık patlamaları:
    /// Zıplama halkası, Çift Zıplama kıvılcımı, Enerji toplama, Kırılan Cam parçaları,
    /// Erime dumanı, Kilit Kırma ve Gölge patlaması efektlerini üretir.
    /// </summary>
    public sealed class WorldVfxManager : MonoBehaviour
    {
        [SerializeField] private RunnerMotor2D runnerMotor;
        [SerializeField] private BatterySystem batterySystem;
        [SerializeField] private ParticleSystem burstParticleSystem;

        private ParticleSystem.EmitParams _emitParams;

        private void OnEnable()
        {
            CrystalPlatform.OnPlatformSolidified += HandleCrystalSolidified;
            CrystalPlatform.OnVariantTriggered += HandleVariantTriggered;
            EnergyOrb.OnEnergyCollected += HandleOrbCollected;
            PrismLockGate.OnGateResolved += HandleGateResolved;
            PhotonAnchor.OnAnchorLaunched += HandleAnchorLaunched;
            ShadowHazard.OnHazardResolved += HandleHazardResolved;
            if (runnerMotor != null)
            {
                runnerMotor.OnRunnerJumped += HandleRunnerJumped;
                runnerMotor.OnRunnerFellIntoVoid += HandleRunnerDefeat;
            }
        }

        private void OnDisable()
        {
            CrystalPlatform.OnPlatformSolidified -= HandleCrystalSolidified;
            CrystalPlatform.OnVariantTriggered -= HandleVariantTriggered;
            EnergyOrb.OnEnergyCollected -= HandleOrbCollected;
            PrismLockGate.OnGateResolved -= HandleGateResolved;
            PhotonAnchor.OnAnchorLaunched -= HandleAnchorLaunched;
            ShadowHazard.OnHazardResolved -= HandleHazardResolved;
            if (runnerMotor != null)
            {
                runnerMotor.OnRunnerJumped -= HandleRunnerJumped;
                runnerMotor.OnRunnerFellIntoVoid -= HandleRunnerDefeat;
            }
        }

        private void HandleCrystalSolidified(CrystalPlatform plat, float intensity)
        {
            if (plat != null) EmitBurst(plat.WorldPosition, new Color(1.0f, 0.84f, 0.36f, 0.85f), 6, 3.2f, 0.14f);
        }

        private void HandleVariantTriggered(CrystalPlatform plat, CrystalVariant variant)
        {
            if (plat == null) return;
            if (variant == CrystalVariant.KineticBoost)
            {
                EmitBurst(plat.WorldPosition, new Color(0.12f, 0.92f, 0.82f, 1f), 14, 5.5f, 0.18f);
            }
            else if (variant == CrystalVariant.FragileShard)
            {
                EmitBurst(plat.WorldPosition, new Color(0.90f, 0.78f, 0.56f, 0.95f), 16, 4.8f, 0.16f);
            }
        }

        private void HandleOrbCollected(EnergyOrb orb, float amount)
        {
            if (orb != null) EmitBurst(orb.WorldPosition, new Color(1.0f, 0.88f, 0.22f, 1f), 16, 5.2f, 0.20f);
        }

        private void HandleGateResolved(PrismLockGate gate, bool unlocked)
        {
            if (gate == null) return;
            Color col = unlocked ? new Color(0.0f, 0.96f, 1.0f, 1f) : new Color(1.0f, 0.18f, 0.42f, 1f);
            EmitBurst(gate.WorldPosition, col, 22, 6.8f, 0.22f);
        }

        private void HandleAnchorLaunched(PhotonAnchor anchor)
        {
            if (anchor != null) EmitBurst(anchor.WorldPosition, new Color(1.0f, 0.92f, 0.42f, 1f), 18, 6.0f, 0.20f);
        }

        private void HandleHazardResolved(ShadowHazard hazard, bool bySkill)
        {
            if (hazard == null) return;
            Color col = bySkill ? new Color(0.75f, 0.45f, 1.0f, 1f) : new Color(1.0f, 0.16f, 0.36f, 1f);
            EmitBurst(hazard.WorldPosition, col, 20, 6.2f, 0.20f);
        }

        private void HandleRunnerJumped(bool isDoubleJump)
        {
            if (runnerMotor == null) return;
            Vector3 feetPos = runnerMotor.transform.position + new Vector3(0f, -0.65f, 0f);
            Color col = isDoubleJump ? new Color(0.10f, 0.92f, 0.84f, 1f) : new Color(0.92f, 0.76f, 0.45f, 0.85f);
            EmitBurst(feetPos, col, isDoubleJump ? 14 : 8, isDoubleJump ? 4.8f : 3.0f, isDoubleJump ? 0.16f : 0.12f);
        }

        private void HandleRunnerDefeat()
        {
            if (runnerMotor == null) return;
            EmitBurst(runnerMotor.transform.position, new Color(0.95f, 0.72f, 0.28f, 1f), 16, 7.5f, 0.24f);
            EmitBurst(runnerMotor.transform.position, new Color(0.85f, 0.30f, 0.18f, 1f), 12, 6.2f, 0.20f);
        }

        public void EmitBurst(Vector3 worldPos, Color color, int count, float speed, float size)
        {
            if (burstParticleSystem == null) return;

            _emitParams.applyShapeToPosition = false;
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
                float spd = speed * Random.Range(0.55f, 1.25f);
                _emitParams.position = worldPos;
                _emitParams.velocity = new Vector3(Mathf.Cos(angle) * spd, Mathf.Sin(angle) * spd, 0f);
                _emitParams.startColor = color;
                _emitParams.startSize = size * Random.Range(0.65f, 1.35f);
                _emitParams.startLifetime = Random.Range(0.24f, 0.48f);
                burstParticleSystem.Emit(_emitParams, 1);
            }
        }
    }
}
