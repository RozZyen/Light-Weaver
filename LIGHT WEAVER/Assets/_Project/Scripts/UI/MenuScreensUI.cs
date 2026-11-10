using System.Globalization;
using LightWeaver.Audio;
using LightWeaver.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LightWeaver.UI
{
    /// <summary>
    /// Ana Menü, Pause Modalı, 3'lü Prizma Yükseltme Kartları (BlessingSelect) ve
    /// Oyun Sonu / Solar Revive ekranlarını yönetir.
    /// </summary>
    public sealed class MenuScreensUI : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField] private GameFlowManager gameFlow;
        [SerializeField] private HarmonicAudioManager audioManager;

        [Header("Screen Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject blessingPanel;

        [Header("Main Menu Controls")]
        [SerializeField] private Button playButton;
        [SerializeField] private Text mainBestScoreText;
        [SerializeField] private Button musicToggleButton;
        [SerializeField] private Text musicToggleLabel;
        [SerializeField] private Button sfxToggleButton;
        [SerializeField] private Text sfxToggleLabel;

        [Header("Pause Controls")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private Button pauseMenuButton;

        [Header("Game Over & Revive Controls")]
        [SerializeField] private Text gameOverScoreText;
        [SerializeField] private Text gameOverDistanceText;
        [SerializeField] private GameObject newBestBadge;
        [SerializeField] private Button solarReviveButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button gameOverMenuButton;

        [Header("Prism Blessing Cards")]
        [SerializeField] private Button[] blessingCardButtons;
        [SerializeField] private Text[] blessingCardTitles;
        [SerializeField] private Text[] blessingCardDescriptions;

        private static readonly string[] ZoneDisplayNames =
        {
            "GIZA NIGHTFALL",
            "KHAMSIN SANDSTORM",
            "NECROPOLIS MIST",
            "BLOOD ECLIPSE",
            "HEART OF THE DUAT"
        };

        private void Awake()
        {
            BindButtons();
            RefreshAudioLabels();
        }

        private void OnEnable()
        {
            if (gameFlow != null)
            {
                gameFlow.OnGameStateChanged += HandleGameStateChanged;
                gameFlow.OnRunEnded += HandleRunEnded;
                gameFlow.OnBlessingsOffered += HandleBlessingsOffered;
            }
        }

        private void OnDisable()
        {
            if (gameFlow != null)
            {
                gameFlow.OnGameStateChanged -= HandleGameStateChanged;
                gameFlow.OnRunEnded -= HandleRunEnded;
                gameFlow.OnBlessingsOffered -= HandleBlessingsOffered;
            }
        }

        private void BindButtons()
        {
            if (playButton != null && gameFlow != null) playButton.onClick.AddListener(gameFlow.StartNewRun);
            if (resumeButton != null && gameFlow != null) resumeButton.onClick.AddListener(gameFlow.TogglePause);
            if (pauseRestartButton != null && gameFlow != null) pauseRestartButton.onClick.AddListener(gameFlow.StartNewRun);
            if (pauseMenuButton != null && gameFlow != null) pauseMenuButton.onClick.AddListener(gameFlow.EnterMainMenu);

            if (solarReviveButton != null && gameFlow != null) solarReviveButton.onClick.AddListener(gameFlow.PerformSolarRevive);
            if (retryButton != null && gameFlow != null) retryButton.onClick.AddListener(gameFlow.StartNewRun);
            if (gameOverMenuButton != null && gameFlow != null) gameOverMenuButton.onClick.AddListener(gameFlow.EnterMainMenu);

            if (musicToggleButton != null) musicToggleButton.onClick.AddListener(ToggleMusic);
            if (sfxToggleButton != null) sfxToggleButton.onClick.AddListener(ToggleSfx);

            if (blessingCardButtons != null)
            {
                for (int i = 0; i < blessingCardButtons.Length; i++)
                {
                    int idx = i;
                    if (blessingCardButtons[i] != null && gameFlow != null)
                    {
                        blessingCardButtons[i].onClick.AddListener(() => gameFlow.SelectPrismBlessing(idx));
                    }
                }
            }
        }

        private void Update()
        {
            if (gameFlow != null && gameFlow.CurrentState == GameState.BlessingSelect && Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame) gameFlow.SelectPrismBlessing(0);
                else if (Keyboard.current.digit2Key.wasPressedThisFrame) gameFlow.SelectPrismBlessing(1);
                else if (Keyboard.current.digit3Key.wasPressedThisFrame) gameFlow.SelectPrismBlessing(2);
            }
        }

        private void ToggleMusic()
        {
            ProgressManager.SetMusicEnabled(!ProgressManager.MusicEnabled);
            if (audioManager != null) audioManager.RefreshMuteStates();
            RefreshAudioLabels();
        }

        private void ToggleSfx()
        {
            ProgressManager.SetSfxEnabled(!ProgressManager.SfxEnabled);
            if (audioManager != null) audioManager.RefreshMuteStates();
            RefreshAudioLabels();
        }

        private void RefreshAudioLabels()
        {
            ProgressManager.EnsureLoaded();
            if (musicToggleLabel != null) musicToggleLabel.text = ProgressManager.MusicEnabled ? "MUSIC: ON" : "MUSIC: OFF";
            if (sfxToggleLabel != null) sfxToggleLabel.text = ProgressManager.SfxEnabled ? "ASMR SFX: ON" : "ASMR SFX: OFF";
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(state == GameState.MainMenu);
            if (pausePanel != null) pausePanel.SetActive(state == GameState.Paused);
            if (gameOverPanel != null) gameOverPanel.SetActive(state == GameState.GameOver);
            if (blessingPanel != null) blessingPanel.SetActive(state == GameState.BlessingSelect);

            if (state == GameState.MainMenu)
            {
                ProgressManager.EnsureLoaded();
                if (mainBestScoreText != null)
                {
                    mainBestScoreText.text = $"BEST: {ProgressManager.HighScore.ToString("N0", CultureInfo.InvariantCulture)}   |   {ProgressManager.BestDistanceMeters:0}m";
                }
                RefreshAudioLabels();
            }
        }

        private void HandleBlessingsOffered(PrismBlessingDefinition[] options)
        {
            if (options == null) return;
            for (int i = 0; i < 3 && i < options.Length; i++)
            {
                if (blessingCardTitles != null && i < blessingCardTitles.Length && blessingCardTitles[i] != null)
                {
                    blessingCardTitles[i].text = $"[{i + 1}] {options[i].Title}";
                }
                if (blessingCardDescriptions != null && i < blessingCardDescriptions.Length && blessingCardDescriptions[i] != null)
                {
                    blessingCardDescriptions[i].text = options[i].Description;
                }
            }
        }

        private void HandleRunEnded(int score, float distanceMeters, int sectorReached, bool isNewBest)
        {
            if (gameOverScoreText != null)
            {
                gameOverScoreText.text = $"SCORE: {score.ToString("N0", CultureInfo.InvariantCulture)}";
            }
            if (gameOverDistanceText != null)
            {
                int zIdx = Mathf.Clamp(sectorReached - 1, 0, ZoneDisplayNames.Length - 1);
                gameOverDistanceText.text = $"DISTANCE: {distanceMeters:0}m  |  {ZoneDisplayNames[zIdx]}";
            }
            if (newBestBadge != null) newBestBadge.SetActive(isNewBest);
            if (solarReviveButton != null && gameFlow != null)
            {
                solarReviveButton.interactable = gameFlow.CanSolarRevive;
                solarReviveButton.gameObject.SetActive(gameFlow.CanSolarRevive);
            }
        }
    }
}
