using System.Globalization;
using LightWeaver.Core;
using LightWeaver.Player;
using LightWeaver.World;
using UnityEngine;
using UnityEngine.UI;

namespace LightWeaver.UI
{
    /// <summary>
    /// Referans görseldeki birebir HUD yerleşimini (Sol üst gradyan pil barı + 'BATTERY: 74%',
    /// Sağ üst 'LEVEL 4 - THE ECHO VOID' + '31,500' skor sayacı, Flash-Burst durumu ve bildirim bandı)
    /// sıfır gereksiz string tahsisiyle (Zero-GC dirty check) yönetir.
    /// </summary>
    public sealed class LightWeaverHUD : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private GameFlowManager gameFlow;
        [SerializeField] private BatterySystem batterySystem;
        [SerializeField] private FlashBurstController flashBurst;
        [SerializeField] private EndlessChunkDirector chunkDirector;

        [Header("HUD Containers & Controls")]
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private Button pauseButton;

        [Header("Top-Left Battery Telemetry")]
        [SerializeField] private Image batteryFillImage;
        [SerializeField] private Text batteryPercentText;
        [SerializeField] private Image flashBurstFillImage;
        [SerializeField] private Text flashBurstLabel;

        [Header("Top-Right Sector & Score")]
        [SerializeField] private Text sectorTitleText;
        [SerializeField] private Text scoreValueText;
        [SerializeField] private Text multiplierBadgeText;

        [Header("Top-Center Feedback Banner")]
        [SerializeField] private CanvasGroup bannerCanvasGroup;
        [SerializeField] private Text bannerText;

        private int _lastBatteryPercent = -1;
        private int _lastScore = -1;
        private int _lastSectorNum = -1;
        private SpectrumPhase _lastPhase = (SpectrumPhase)255;
        private float _bannerTimer;
        private float _scorePunchTimer;

        private void Awake()
        {
            if (pauseButton != null && gameFlow != null)
            {
                pauseButton.onClick.AddListener(gameFlow.TogglePause);
            }
            if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            if (gameFlow != null)
            {
                gameFlow.OnGameStateChanged += HandleGameStateChanged;
                gameFlow.OnScoreChanged += HandleScoreChanged;
                gameFlow.OnBannerMessage += ShowBanner;
            }
            if (batterySystem != null)
            {
                batterySystem.OnBatteryChanged += HandleBatteryChanged;
                batterySystem.OnSpectrumPhaseChanged += HandlePhaseChanged;
            }
            if (chunkDirector != null)
            {
                chunkDirector.OnSectorChanged += HandleSectorChanged;
            }
        }

        private void OnDisable()
        {
            if (gameFlow != null)
            {
                gameFlow.OnGameStateChanged -= HandleGameStateChanged;
                gameFlow.OnScoreChanged -= HandleScoreChanged;
                gameFlow.OnBannerMessage -= ShowBanner;
            }
            if (batterySystem != null)
            {
                batterySystem.OnBatteryChanged -= HandleBatteryChanged;
                batterySystem.OnSpectrumPhaseChanged -= HandlePhaseChanged;
            }
            if (chunkDirector != null)
            {
                chunkDirector.OnSectorChanged -= HandleSectorChanged;
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (hudRoot != null)
            {
                hudRoot.SetActive(state == GameState.Playing || state == GameState.Paused);
            }
            if (state == GameState.Playing && batterySystem != null)
            {
                HandleBatteryChanged(batterySystem.CurrentBattery, batterySystem.MaxBattery);
                HandlePhaseChanged(batterySystem.CurrentPhase);
                if (chunkDirector != null && chunkDirector.ActiveSector != null)
                {
                    HandleSectorChanged(chunkDirector.ActiveSector);
                }
            }
        }

        private void HandleBatteryChanged(float current, float max)
        {
            float norm = max > 0.01f ? Mathf.Clamp01(current / max) : 0f;
            if (batteryFillImage != null)
            {
                batteryFillImage.fillAmount = norm;
            }

            int pct = Mathf.Clamp(Mathf.RoundToInt(norm * 100f), 0, 100);
            if (pct != _lastBatteryPercent)
            {
                _lastBatteryPercent = pct;
                if (batteryPercentText != null)
                {
                    batteryPercentText.text = $"BATTERY: {pct}%";
                }
            }
        }

        private void HandlePhaseChanged(SpectrumPhase phase)
        {
            if (phase == _lastPhase) return;
            _lastPhase = phase;

            if (multiplierBadgeText != null)
            {
                if (phase == SpectrumPhase.Overcharge)
                {
                    multiplierBadgeText.gameObject.SetActive(true);
                    multiplierBadgeText.text = "PRISM OVERCHARGE  3X";
                    multiplierBadgeText.color = new Color(0.55f, 1.0f, 0.85f, 1f);
                }
                else if (phase == SpectrumPhase.CriticalEmber)
                {
                    multiplierBadgeText.gameObject.SetActive(true);
                    multiplierBadgeText.text = "ADRENALINE  2X CHARGE";
                    multiplierBadgeText.color = new Color(1.0f, 0.25f, 0.35f, 1f);
                }
                else
                {
                    multiplierBadgeText.gameObject.SetActive(false);
                }
            }
        }

        private void HandleSectorChanged(SectorProfileSO sector)
        {
            if (sector == null || sector.sectorNumber == _lastSectorNum) return;
            _lastSectorNum = sector.sectorNumber;

            if (sectorTitleText != null)
            {
                string hex = ColorUtility.ToHtmlStringRGB(sector.sectorAccentColor);
                sectorTitleText.text = $"LEVEL {sector.sectorNumber} - <color=#{hex}>{sector.sectorName}</color>";
            }
        }

        private void HandleScoreChanged(int score)
        {
            if (score == _lastScore) return;
            bool bigJump = (score - _lastScore) >= 100;
            _lastScore = score;

            if (scoreValueText != null)
            {
                scoreValueText.text = score.ToString("N0", CultureInfo.InvariantCulture);
                if (bigJump) _scorePunchTimer = 0.16f;
            }
        }

        private void ShowBanner(string message, Color accentColor)
        {
            if (bannerText == null || bannerCanvasGroup == null) return;
            bannerText.text = message;
            bannerText.color = accentColor;
            bannerCanvasGroup.alpha = 1f;
            _bannerTimer = 1.6f;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // Kritik Pil Nabız Efekti (<%20)
            if (batteryPercentText != null && _lastBatteryPercent >= 0)
            {
                if (_lastBatteryPercent < 20)
                {
                    float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 10f);
                    batteryPercentText.color = Color.Lerp(Color.white, new Color(1f, 0.16f, 0.32f, 1f), pulse);
                }
                else
                {
                    batteryPercentText.color = new Color(0.85f, 0.96f, 1f, 0.95f);
                }
            }

            // Flash-Burst Dolum Çubuğu
            if (flashBurst != null && flashBurstFillImage != null)
            {
                float ready = flashBurst.CooldownNormalized;
                flashBurstFillImage.fillAmount = ready;
                if (flashBurstLabel != null)
                {
                    flashBurstLabel.color = ready >= 0.999f
                        ? new Color(0.0f, 0.94f, 1.0f, 1f)
                        : new Color(0.55f, 0.65f, 0.78f, 0.7f);
                }
            }

            // Skor Punch Animasyonu
            if (_scorePunchTimer > 0f && scoreValueText != null)
            {
                _scorePunchTimer = Mathf.Max(0f, _scorePunchTimer - dt);
                float t = _scorePunchTimer / 0.16f;
                float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.14f;
                scoreValueText.transform.localScale = new Vector3(s, s, 1f);
            }

            // Bildirim Bandı Sönümlemesi
            if (_bannerTimer > 0f && bannerCanvasGroup != null)
            {
                _bannerTimer -= dt;
                if (_bannerTimer < 0.45f)
                {
                    bannerCanvasGroup.alpha = Mathf.Clamp01(_bannerTimer / 0.45f);
                }
            }
        }
    }
}
