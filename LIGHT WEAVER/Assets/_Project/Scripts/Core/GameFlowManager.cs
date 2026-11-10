using System;
using LightWeaver.Player;
using LightWeaver.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LightWeaver.Core
{
    public enum GameState : byte
    {
        MainMenu = 0,
        Playing = 1,
        Paused = 2,
        GameOver = 3
    }

    /// <summary>
    /// Oyunun tüm akış durumlarını (MainMenu -> Playing -> Paused -> GameOver / Solar Revive),
    /// skor birikimini ve sektör ilerlemesini koordine eden merkezi akış yöneticisi.
    /// </summary>
    public sealed class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager ActiveInstance { get; private set; }

        public event Action<GameState> OnGameStateChanged;
        public event Action<int> OnScoreChanged;
        public event Action<string, Color> OnBannerMessage;
        public event Action<int, float, int, bool> OnRunEnded; // (score, dist, sector, isNewBest)

        [Header("Core Scene References")]
        [SerializeField] private RunnerMotor2D runnerMotor;
        [SerializeField] private BatterySystem batterySystem;
        [SerializeField] private LightTouchInput touchInput;
        [SerializeField] private EndlessChunkDirector chunkDirector;
        [SerializeField] private CameraFollow2D cameraFollow;

        [Header("Score Tuning")]
        [SerializeField] private float scorePerMeter = 18f;
        [SerializeField] private int crystalSolidifyBonus = 25;
        [SerializeField] private int perfectWeaveBonus = 150;
        [SerializeField] private int energyOrbBonus = 200;
        [SerializeField] private int hazardSkillBonus = 300;

        private GameState _currentState = GameState.MainMenu;
        private int _currentScore;
        private float _accumulatedDistanceScore;
        private float _lastRecordedDistance;
        private bool _hasUsedSolarRevive;
        private int _selectedStartSectorIndex;

        public GameState CurrentState => _currentState;
        public int CurrentScore => _currentScore;
        public bool CanSolarRevive => !_hasUsedSolarRevive;
        public int SelectedStartSectorIndex => _selectedStartSectorIndex;

        private void Awake()
        {
            ActiveInstance = this;
            ProgressManager.EnsureLoaded();
        }

        private void OnDestroy()
        {
            if (ActiveInstance == this) ActiveInstance = null;
        }

        private void OnEnable()
        {
            if (runnerMotor != null) runnerMotor.OnRunnerFellIntoVoid += HandleRunnerFellIntoVoid;
            CrystalPlatform.OnPlatformSolidified += HandlePlatformSolidified;
            EnergyOrb.OnEnergyCollected += HandleEnergyCollected;
            ShadowHazard.OnHazardResolved += HandleHazardResolved;
            if (batterySystem != null)
            {
                batterySystem.OnPerfectWeaveTriggered += HandlePerfectWeave;
                batterySystem.OnSpectrumPhaseChanged += HandleSpectrumPhaseChanged;
            }
            if (chunkDirector != null) chunkDirector.OnSectorChanged += HandleSectorChanged;
        }

        private void OnDisable()
        {
            if (runnerMotor != null) runnerMotor.OnRunnerFellIntoVoid -= HandleRunnerFellIntoVoid;
            CrystalPlatform.OnPlatformSolidified -= HandlePlatformSolidified;
            EnergyOrb.OnEnergyCollected -= HandleEnergyCollected;
            ShadowHazard.OnHazardResolved -= HandleHazardResolved;
            if (batterySystem != null)
            {
                batterySystem.OnPerfectWeaveTriggered -= HandlePerfectWeave;
                batterySystem.OnSpectrumPhaseChanged -= HandleSpectrumPhaseChanged;
            }
            if (chunkDirector != null) chunkDirector.OnSectorChanged -= HandleSectorChanged;
        }

        private void Start()
        {
            EnterMainMenu();
        }

        public void SelectStartSector(int sectorIndex)
        {
            _selectedStartSectorIndex = Mathf.Clamp(sectorIndex, 0, 4);
            if (_currentState == GameState.MainMenu && chunkDirector != null)
            {
                chunkDirector.SetStartingSector(_selectedStartSectorIndex);
            }
        }

        public void EnterMainMenu()
        {
            Time.timeScale = 1f;
            _currentState = GameState.MainMenu;
            _currentScore = 0;
            _accumulatedDistanceScore = 0f;
            _lastRecordedDistance = 0f;
            _hasUsedSolarRevive = false;

            if (runnerMotor != null)
            {
                runnerMotor.SetAutoRespawnOnVoidFall(false);
                runnerMotor.SetRunning(false);
                runnerMotor.RespawnAtStart();
            }
            if (batterySystem != null)
            {
                batterySystem.ResetBattery();
                batterySystem.SetSimulationPaused(true);
            }
            if (chunkDirector != null) chunkDirector.SetStartingSector(_selectedStartSectorIndex);
            if (cameraFollow != null) cameraFollow.SnapToTarget();

            OnScoreChanged?.Invoke(0);
            OnGameStateChanged?.Invoke(_currentState);
        }

        public void StartNewRun()
        {
            Time.timeScale = 1f;
            _currentScore = 0;
            _accumulatedDistanceScore = 0f;
            _lastRecordedDistance = 0f;
            _hasUsedSolarRevive = false;

            if (chunkDirector != null) chunkDirector.SetStartingSector(_selectedStartSectorIndex);
            if (runnerMotor != null)
            {
                runnerMotor.SetAutoRespawnOnVoidFall(false);
                runnerMotor.RespawnAtStart();
                runnerMotor.SetRunning(true);
            }
            if (batterySystem != null)
            {
                batterySystem.ResetBattery();
                batterySystem.SetSimulationPaused(false);
            }
            if (cameraFollow != null) cameraFollow.SnapToTarget();

            _currentState = GameState.Playing;
            OnScoreChanged?.Invoke(0);
            OnGameStateChanged?.Invoke(_currentState);

            if (chunkDirector != null && chunkDirector.ActiveSector != null)
            {
                SectorProfileSO sec = chunkDirector.ActiveSector;
                OnBannerMessage?.Invoke($"LEVEL {sec.sectorNumber} - {sec.sectorName}", sec.sectorAccentColor);
            }
        }

        public void TogglePause()
        {
            if (_currentState == GameState.Playing)
            {
                _currentState = GameState.Paused;
                Time.timeScale = 0f;
                OnGameStateChanged?.Invoke(_currentState);
            }
            else if (_currentState == GameState.Paused)
            {
                _currentState = GameState.Playing;
                Time.timeScale = 1f;
                OnGameStateChanged?.Invoke(_currentState);
            }
        }

        public void PerformSolarRevive()
        {
            if (_currentState != GameState.GameOver || _hasUsedSolarRevive) return;
            _hasUsedSolarRevive = true;

            float reviveX = runnerMotor != null ? runnerMotor.transform.position.x : 0f;
            float safeTopY = 0.25f;

            if (chunkDirector != null)
            {
                chunkDirector.SpawnReviveSafetyBridge(reviveX, safeTopY);
            }
            if (runnerMotor != null)
            {
                runnerMotor.ReviveAtPosition(new Vector3(reviveX, safeTopY + 1.35f, 0f));
                runnerMotor.SetRunning(true);
            }
            if (batterySystem != null)
            {
                batterySystem.ResetBattery();
                batterySystem.SetSimulationPaused(false);
            }
            if (cameraFollow != null) cameraFollow.SnapToTarget();

            Time.timeScale = 1f;
            _currentState = GameState.Playing;
            OnGameStateChanged?.Invoke(_currentState);
            OnBannerMessage?.Invoke("SOLAR REVIVE! +100% ENERGY", new Color(1.0f, 0.85f, 0.15f, 1f));
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (_currentState == GameState.Playing || _currentState == GameState.Paused)
                {
                    TogglePause();
                }
            }

            if (_currentState != GameState.Playing || chunkDirector == null) return;

            float dist = chunkDirector.DistanceTraveledMeters;
            float deltaDist = dist - _lastRecordedDistance;
            if (deltaDist > 0.01f)
            {
                _lastRecordedDistance = dist;
                float mult = batterySystem != null ? batterySystem.ScoreMultiplier : 1f;
                _accumulatedDistanceScore += deltaDist * scorePerMeter * mult;
                int addInt = Mathf.FloorToInt(_accumulatedDistanceScore);
                if (addInt > 0)
                {
                    _accumulatedDistanceScore -= addInt;
                    AddScore(addInt);
                }
            }
        }

        private void AddScore(int rawPoints)
        {
            if (rawPoints <= 0) return;
            _currentScore += rawPoints;
            OnScoreChanged?.Invoke(_currentScore);
        }

        private void HandlePlatformSolidified(CrystalPlatform platform, float intensity)
        {
            if (_currentState != GameState.Playing) return;
            float mult = batterySystem != null ? batterySystem.ScoreMultiplier : 1f;
            AddScore(Mathf.RoundToInt(crystalSolidifyBonus * mult));
        }

        private void HandlePerfectWeave(Vector2 worldPos, float bonusCharge)
        {
            if (_currentState != GameState.Playing) return;
            float mult = batterySystem != null ? batterySystem.ScoreMultiplier : 1f;
            AddScore(Mathf.RoundToInt(perfectWeaveBonus * mult));
            OnBannerMessage?.Invoke("PERFECT WEAVE! +3%", new Color(0.0f, 0.94f, 1.0f, 1f));
        }

        private void HandleEnergyCollected(EnergyOrb orb, float amount)
        {
            if (_currentState != GameState.Playing) return;
            float mult = batterySystem != null ? batterySystem.ScoreMultiplier : 1f;
            AddScore(Mathf.RoundToInt(energyOrbBonus * mult));
            OnBannerMessage?.Invoke($"+ENERGY (+{Mathf.RoundToInt(amount)}%)", new Color(1.0f, 0.85f, 0.2f, 1f));
        }

        private void HandleHazardResolved(ShadowHazard hazard, bool bySkill)
        {
            if (_currentState != GameState.Playing) return;
            if (bySkill)
            {
                float mult = batterySystem != null ? batterySystem.ScoreMultiplier : 1f;
                AddScore(Mathf.RoundToInt(hazardSkillBonus * mult));
                OnBannerMessage?.Invoke("SHADOW SHATTERED! +300", new Color(0.75f, 0.45f, 1.0f, 1f));
            }
            else
            {
                OnBannerMessage?.Invoke("SHADOW HIT! -24% BATTERY", new Color(1.0f, 0.22f, 0.38f, 1f));
            }
        }

        private void HandleSpectrumPhaseChanged(SpectrumPhase phase)
        {
            if (_currentState != GameState.Playing) return;
            if (phase == SpectrumPhase.Overcharge)
            {
                OnBannerMessage?.Invoke("PRISM OVERCHARGE! 3X SCORE", new Color(0.55f, 1.0f, 0.85f, 1f));
            }
            else if (phase == SpectrumPhase.CriticalEmber)
            {
                OnBannerMessage?.Invoke("CRITICAL BATTERY! 2X CHARGE BOOST", new Color(1.0f, 0.35f, 0.25f, 1f));
            }
        }

        private void HandleSectorChanged(SectorProfileSO sector)
        {
            if (_currentState != GameState.Playing || sector == null) return;
            OnBannerMessage?.Invoke($"LEVEL {sector.sectorNumber} - {sector.sectorName}", sector.sectorAccentColor);
        }

        private void HandleRunnerFellIntoVoid()
        {
            if (_currentState != GameState.Playing) return;

            _currentState = GameState.GameOver;
            Time.timeScale = 0f;
            if (runnerMotor != null) runnerMotor.SetRunning(false);
            if (batterySystem != null) batterySystem.SetSimulationPaused(true);

            float dist = chunkDirector != null ? chunkDirector.DistanceTraveledMeters : 0f;
            int sectorNum = chunkDirector != null && chunkDirector.ActiveSector != null ? chunkDirector.ActiveSector.sectorNumber : 1;
            bool isNewBest = ProgressManager.TryRecordRunResult(_currentScore, dist, sectorNum);

            OnGameStateChanged?.Invoke(_currentState);
            OnRunEnded?.Invoke(_currentScore, dist, sectorNum, isNewBest);
        }
    }
}
