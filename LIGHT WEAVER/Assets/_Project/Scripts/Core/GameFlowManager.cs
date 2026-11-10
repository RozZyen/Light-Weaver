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
        GameOver = 3,
        BlessingSelect = 4
    }

    /// <summary>
    /// Oyunun tüm akış durumlarını (MainMenu -> Playing -> BlessingSelect -> Paused -> GameOver / Solar Revive),
    /// The Eclipse takip dalgasını, skor birikimini ve sektör ilerlemesini koordine eder.
    /// </summary>
    public sealed class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager ActiveInstance { get; private set; }

        public event Action<GameState> OnGameStateChanged;
        public event Action<int> OnScoreChanged;
        public event Action<string, Color> OnBannerMessage;
        public event Action<PrismBlessingDefinition[]> OnBlessingsOffered;
        public event Action<int, float, int, bool> OnRunEnded;

        [Header("Core Scene References")]
        [SerializeField] private RunnerMotor2D runnerMotor;
        [SerializeField] private BatterySystem batterySystem;
        [SerializeField] private LightTouchInput touchInput;
        [SerializeField] private EndlessChunkDirector chunkDirector;
        [SerializeField] private EclipsePursuitWave eclipseWave;
        [SerializeField] private PrismUpgradeManager upgradeManager;
        [SerializeField] private CameraFollow2D cameraFollow;

        [Header("Score Tuning")]
        [SerializeField] private float scorePerMeter = 18f;
        [SerializeField] private int crystalSolidifyBonus = 25;
        [SerializeField] private int perfectWeaveBonus = 150;
        [SerializeField] private int energyOrbBonus = 200;
        [SerializeField] private int hazardSkillBonus = 300;
        [SerializeField] private int gateUnlockBonus = 250;
        [SerializeField] private int anchorSwingBonus = 180;

        private GameState _currentState = GameState.MainMenu;
        private int _currentScore;
        private float _accumulatedDistanceScore;
        private float _lastRecordedDistance;
        private bool _hasUsedSolarRevive;
        private int _selectedStartSectorIndex;
        private int _highestSectorReachedThisRun = 1;
        private readonly PrismBlessingDefinition[] _offeredBlessings = new PrismBlessingDefinition[3];

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
            if (runnerMotor != null) runnerMotor.OnRunnerFellIntoVoid += HandleRunnerDefeat;
            if (eclipseWave != null) eclipseWave.OnEclipseEngulfedRunner += HandleRunnerDefeat;

            CrystalPlatform.OnPlatformSolidified += HandlePlatformSolidified;
            CrystalPlatform.OnVariantTriggered += HandleCrystalVariantTriggered;
            EnergyOrb.OnEnergyCollected += HandleEnergyCollected;
            ShadowHazard.OnHazardResolved += HandleHazardResolved;
            PrismLockGate.OnGateResolved += HandleGateResolved;
            PhotonAnchor.OnAnchorLaunched += HandleAnchorLaunched;

            if (batterySystem != null)
            {
                batterySystem.OnPerfectWeaveTriggered += HandlePerfectWeave;
                batterySystem.OnSpectrumPhaseChanged += HandleSpectrumPhaseChanged;
            }
            if (chunkDirector != null) chunkDirector.OnSectorChanged += HandleSectorChanged;
        }

        private void OnDisable()
        {
            if (runnerMotor != null) runnerMotor.OnRunnerFellIntoVoid -= HandleRunnerDefeat;
            if (eclipseWave != null) eclipseWave.OnEclipseEngulfedRunner -= HandleRunnerDefeat;

            CrystalPlatform.OnPlatformSolidified -= HandlePlatformSolidified;
            CrystalPlatform.OnVariantTriggered -= HandleCrystalVariantTriggered;
            EnergyOrb.OnEnergyCollected -= HandleEnergyCollected;
            ShadowHazard.OnHazardResolved -= HandleHazardResolved;
            PrismLockGate.OnGateResolved -= HandleGateResolved;
            PhotonAnchor.OnAnchorLaunched -= HandleAnchorLaunched;

            if (batterySystem != null)
            {
                batterySystem.OnPerfectWeaveTriggered -= HandlePerfectWeave;
                batterySystem.OnSpectrumPhaseChanged -= HandleSpectrumPhaseChanged;
            }
            if (chunkDirector != null) chunkDirector.OnSectorChanged -= HandleSectorChanged;
        }

        private void Start() => EnterMainMenu();

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
            _selectedStartSectorIndex = 0;
            _highestSectorReachedThisRun = 1;

            if (upgradeManager != null) upgradeManager.ResetAllBlessings();
            if (runnerMotor != null)
            {
                runnerMotor.ResetBlessings();
                runnerMotor.SetAutoRespawnOnVoidFall(false);
                runnerMotor.SetRunning(false);
                runnerMotor.RespawnAtStart();
            }
            if (batterySystem != null)
            {
                batterySystem.ResetBattery();
                batterySystem.SetSimulationPaused(true);
            }
            if (chunkDirector != null) chunkDirector.SetStartingSector(0);
            if (eclipseWave != null) eclipseWave.ResetPursuit();
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
            _selectedStartSectorIndex = 0;
            _highestSectorReachedThisRun = 1;

            if (upgradeManager != null) upgradeManager.ResetAllBlessings();
            if (chunkDirector != null) chunkDirector.SetStartingSector(0);
            if (runnerMotor != null)
            {
                runnerMotor.ResetBlessings();
                runnerMotor.SetAutoRespawnOnVoidFall(false);
                runnerMotor.RespawnAtStart();
                runnerMotor.SetRunning(true);
            }
            if (batterySystem != null)
            {
                batterySystem.ResetBattery();
                batterySystem.SetSimulationPaused(false);
            }
            if (eclipseWave != null) eclipseWave.ResetPursuit();
            if (cameraFollow != null) cameraFollow.SnapToTarget();

            _currentState = GameState.Playing;
            OnScoreChanged?.Invoke(0);
            OnGameStateChanged?.Invoke(_currentState);

            if (chunkDirector != null && chunkDirector.ActiveSector != null)
            {
                SectorProfileSO sec = chunkDirector.ActiveSector;
                OnBannerMessage?.Invoke(sec.sectorName, sec.sectorAccentColor);
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

        public void SelectPrismBlessing(int cardIndex)
        {
            if (_currentState != GameState.BlessingSelect) return;
            int idx = Mathf.Clamp(cardIndex, 0, 2);
            PrismBlessingDefinition chosen = _offeredBlessings[idx];

            if (upgradeManager != null) upgradeManager.ApplyBlessing(chosen.Id);
            if (chosen.Id == PrismBlessingId.EclipseRepulsor && eclipseWave != null)
            {
                eclipseWave.PushBack(6.0f);
            }

            Time.timeScale = 1f;
            _currentState = GameState.Playing;
            OnGameStateChanged?.Invoke(_currentState);
            OnBannerMessage?.Invoke($"BLESSING: {chosen.Title}", new Color(0.55f, 1.0f, 0.85f, 1f));
        }

        public void PerformSolarRevive()
        {
            if (_currentState != GameState.GameOver || _hasUsedSolarRevive) return;
            _hasUsedSolarRevive = true;

            float reviveX = runnerMotor != null ? runnerMotor.transform.position.x : 0f;
            float safeTopY = 0.35f;

            if (chunkDirector != null) chunkDirector.SpawnReviveSafetyBridge(reviveX, safeTopY);
            if (runnerMotor != null)
            {
                runnerMotor.ReviveAtPosition(new Vector3(reviveX, safeTopY + 1.15f, 0f));
                runnerMotor.SetRunning(true);
            }
            if (batterySystem != null)
            {
                batterySystem.ResetBattery();
                batterySystem.SetSimulationPaused(false);
            }
            if (eclipseWave != null) eclipseWave.ResetPursuit();
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

        private void HandleCrystalVariantTriggered(CrystalPlatform plat, CrystalVariant variant)
        {
            if (_currentState != GameState.Playing) return;
            if (variant == CrystalVariant.KineticBoost)
            {
                OnBannerMessage?.Invoke("KINETIC BOOST!", new Color(0.25f, 1.0f, 0.72f, 1f));
            }
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

        private void HandleGateResolved(PrismLockGate gate, bool unlockedByLight)
        {
            if (_currentState != GameState.Playing) return;
            if (unlockedByLight)
            {
                float mult = batterySystem != null ? batterySystem.ScoreMultiplier : 1f;
                AddScore(Mathf.RoundToInt(gateUnlockBonus * mult));
                OnBannerMessage?.Invoke("PRISM GATE BREACHED! +250", new Color(0.0f, 0.94f, 1.0f, 1f));
            }
            else
            {
                OnBannerMessage?.Invoke("GATE IMPACT! -22% BATTERY", new Color(1.0f, 0.16f, 0.42f, 1f));
            }
        }

        private void HandleAnchorLaunched(PhotonAnchor anchor)
        {
            if (_currentState != GameState.Playing) return;
            float mult = batterySystem != null ? batterySystem.ScoreMultiplier : 1f;
            AddScore(Mathf.RoundToInt(anchorSwingBonus * mult));
            OnBannerMessage?.Invoke("ROPE GRAPPLE SWING!", new Color(1.0f, 0.88f, 0.42f, 1f));
        }

        private void HandleSpectrumPhaseChanged(SpectrumPhase phase)
        {
            if (_currentState != GameState.Playing) return;
            if (phase == SpectrumPhase.Overcharge) OnBannerMessage?.Invoke("PRISM OVERCHARGE! 3X SCORE", new Color(0.55f, 1.0f, 0.85f, 1f));
            else if (phase == SpectrumPhase.CriticalEmber) OnBannerMessage?.Invoke("CRITICAL BATTERY! 2X CHARGE", new Color(1.0f, 0.35f, 0.25f, 1f));
        }

        private void HandleSectorChanged(SectorProfileSO sector)
        {
            if (_currentState != GameState.Playing || sector == null) return;

            if (sector.sectorNumber > _highestSectorReachedThisRun && upgradeManager != null)
            {
                _highestSectorReachedThisRun = sector.sectorNumber;
                upgradeManager.RollThreeOptions(_offeredBlessings);
                _currentState = GameState.BlessingSelect;
                Time.timeScale = 0f;
                OnGameStateChanged?.Invoke(_currentState);
                OnBlessingsOffered?.Invoke(_offeredBlessings);
            }
            else
            {
                OnBannerMessage?.Invoke(sector.sectorName, sector.sectorAccentColor);
            }
        }

        private void HandleRunnerDefeat()
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
