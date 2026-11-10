using System;
using LightWeaver.Core;
using LightWeaver.Player;
using UnityEngine;

namespace LightWeaver.World
{
    /// <summary>
    /// Ekranın solundan oyuncuyu takip eden Hiçlik Dalgası (The Eclipse).
    /// Yavaşlarsan, kapılara çarparsan veya engellere takılırsan yaklaşır;
    /// Boost kristalleri, Perfect Weave ve Foton Çapalarıyla geri püskürtülür!
    /// </summary>
    public sealed class EclipsePursuitWave : MonoBehaviour
    {
        public event Action OnEclipseEngulfedRunner;

        [Header("References")]
        [SerializeField] private RunnerMotor2D runnerMotor;
        [SerializeField] private BatterySystem batterySystem;
        [SerializeField] private EndlessChunkDirector chunkDirector;

        [Header("Pursuit Tuning")]
        [SerializeField] private float startingLagMeters = 11.5f;
        [SerializeField] private float maxLagMeters = 14.5f;
        [SerializeField] private float baseCreepSpeed = 0.28f;
        [SerializeField] private float boostPushbackPerSec = 3.2f;

        private float _currentLagMeters;
        private bool _isEngulfed;

        public float CurrentLagMeters => _currentLagMeters;
        public float DangerNormalized => 1f - Mathf.Clamp01((_currentLagMeters - 0.5f) / (startingLagMeters - 0.5f));

        private void Awake()
        {
            _currentLagMeters = startingLagMeters;
        }

        private void OnEnable()
        {
            if (batterySystem != null) batterySystem.OnPerfectWeaveTriggered += HandlePerfectWeave;
            PrismLockGate.OnGateResolved += HandleGateResolved;
            PhotonAnchor.OnAnchorLaunched += HandleAnchorLaunched;
            ShadowHazard.OnHazardResolved += HandleHazardResolved;
            RunnerMotor2D.OnAnyRunnerRespawned += ResetPursuit;
        }

        private void OnDisable()
        {
            if (batterySystem != null) batterySystem.OnPerfectWeaveTriggered -= HandlePerfectWeave;
            PrismLockGate.OnGateResolved -= HandleGateResolved;
            PhotonAnchor.OnAnchorLaunched -= HandleAnchorLaunched;
            ShadowHazard.OnHazardResolved -= HandleHazardResolved;
            RunnerMotor2D.OnAnyRunnerRespawned -= ResetPursuit;
        }

        public void ResetPursuit()
        {
            _currentLagMeters = startingLagMeters;
            _isEngulfed = false;
            SyncVisualTransform();
        }

        public void PushBack(float meters)
        {
            _currentLagMeters = Mathf.Clamp(_currentLagMeters + meters, 0.5f, maxLagMeters);
        }

        private void HandlePerfectWeave(Vector2 pos, float bonus) => PushBack(0.65f);
        private void HandleAnchorLaunched(PhotonAnchor anchor) => PushBack(1.65f);
        private void HandleGateResolved(PrismLockGate gate, bool unlocked) => PushBack(unlocked ? 1.1f : -2.6f);
        private void HandleHazardResolved(ShadowHazard hazard, bool bySkill)
        {
            if (bySkill) PushBack(0.75f);
            else _currentLagMeters = Mathf.Max(0.4f, _currentLagMeters - 2.3f);
        }

        private void Update()
        {
            if (runnerMotor == null) return;
            if (GameFlowManager.ActiveInstance != null && GameFlowManager.ActiveInstance.CurrentState != GameState.Playing)
            {
                SyncVisualTransform();
                return;
            }

            if (_isEngulfed) return;

            float dt = Time.deltaTime;
            float sectorFactor = chunkDirector != null && chunkDirector.ActiveSector != null
                ? chunkDirector.ActiveSector.runSpeedMultiplier
                : 1f;

            if (runnerMotor.IsBoosting)
            {
                _currentLagMeters = Mathf.Min(maxLagMeters, _currentLagMeters + boostPushbackPerSec * dt);
            }
            else
            {
                _currentLagMeters -= baseCreepSpeed * sectorFactor * dt;
            }

            SyncVisualTransform();

            if (_currentLagMeters <= 0.35f)
            {
                _isEngulfed = true;
                OnEclipseEngulfedRunner?.Invoke();
            }
        }

        private void SyncVisualTransform()
        {
            if (runnerMotor == null) return;
            Vector3 runnerPos = runnerMotor.transform.position;
            float pulse = Mathf.Sin(Time.time * 4f) * 0.15f;
            transform.position = new Vector3(runnerPos.x - _currentLagMeters + pulse, runnerPos.y + 0.5f, 0f);
        }
    }
}
