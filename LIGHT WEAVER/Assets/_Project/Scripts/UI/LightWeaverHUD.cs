using System.Globalization;
using LightWeaver.Core;
using LightWeaver.Player;
using LightWeaver.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LightWeaver.UI
{
    /// <summary>
    /// Referans görseldeki birebir HUD + Eclipse Tehlike Barı + Mobil Dokunmatik Eylem Butonları
    /// (JUMP, FOCUS, BURST) yönetimini Zero-GC dirty-check ile sağlar.
    /// </summary>
    public sealed class LightWeaverHUD : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private GameFlowManager gameFlow;
        [SerializeField] private RunnerMotor2D runnerMotor;
        [SerializeField] private LightTouchInput touchInput;
        [SerializeField] private BatterySystem batterySystem;
        [SerializeField] private FlashBurstController flashBurst;
        [SerializeField] private EndlessChunkDirector chunkDirector;
        [SerializeField] private EclipsePursuitWave eclipseWave;

        [Header("HUD Containers & Controls")]
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private Button pauseButton;

        [Header("Top-Left Battery & Eclipse")]
        [SerializeField] private Image batteryFillImage;
        [SerializeField] private Text batteryPercentText;
        [SerializeField] private Image flashBurstFillImage;
        [SerializeField] private Text flashBurstLabel;
        [SerializeField] private Image eclipseDangerFillImage;

        [Header("Top-Right Sector & Score")]
        [SerializeField] private Text sectorTitleText;
        [SerializeField] private Text scoreValueText;
        [SerializeField] private Text multiplierBadgeText;

        [Header("Top-Center Feedback Banner")]
        [SerializeField] private CanvasGroup bannerCanvasGroup;
        [SerializeField] private Text bannerText;

        [Header("Mobile Touch Action Buttons")]
        [SerializeField] private Button mobileJumpButton;
        [SerializeField] private Button mobileHighBeamButton;
        [SerializeField] private Image mobileHighBeamBg;
        [SerializeField] private Image mobileHighBeamIcon;
        [SerializeField] private Text mobileHighBeamLabel;
        [SerializeField] private Button mobileBurstButton;
        [SerializeField] private EventTrigger mobileFocusTrigger;

        private int _lastBatteryPercent = -1;
        private int _lastScore = -1;
        private int _lastSectorNum = -1;
        private int _lastDisplayedMeters = -1;
        private string _currentZoneHex = "FFD24C";
        private string _currentZoneName = "GIZA NIGHTFALL";
        private SpectrumPhase _lastPhase = (SpectrumPhase)255;
        private float _bannerTimer;
        private float _scorePunchTimer;

        private void Awake()
        {
            if (pauseButton != null && gameFlow != null) pauseButton.onClick.AddListener(gameFlow.TogglePause);
            if (mobileJumpButton != null && runnerMotor != null) mobileJumpButton.onClick.AddListener(runnerMotor.RequestJump);
            if (mobileBurstButton != null && flashBurst != null) mobileBurstButton.onClick.AddListener(flashBurst.TryTriggerFlashBurst);
            SetupFocusHoldTrigger();
            UpdateHighBeamVisual(touchInput != null && touchInput.IsLongBeamHeld);
            if (bannerCanvasGroup != null) bannerCanvasGroup.alpha = 0f;
        }

        private void UpdateHighBeamVisual(bool isHeld)
        {
            if (mobileHighBeamBg != null)
            {
                mobileHighBeamBg.color = isHeld
                    ? new Color(1.0f, 0.88f, 0.52f, 1.0f)
                    : new Color(0.85f, 0.85f, 0.90f, 0.92f);
            }
            if (mobileHighBeamIcon != null)
            {
                mobileHighBeamIcon.color = isHeld
                    ? new Color(1.0f, 0.88f, 0.30f, 1.0f)
                    : new Color(0.78f, 0.88f, 0.92f, 0.95f);
            }
            if (mobileHighBeamLabel != null)
            {
                mobileHighBeamLabel.text = isHeld ? "LONG: ON" : "LONG [HOLD]";
                mobileHighBeamLabel.color = isHeld
                    ? new Color(1.0f, 0.86f, 0.28f, 1.0f)
                    : new Color(0.94f, 0.88f, 0.76f, 0.95f);
            }
        }

        private void SetupFocusHoldTrigger()
        {
            if (touchInput == null) return;
            EventTrigger targetTrigger = mobileFocusTrigger;
            if (targetTrigger == null && mobileHighBeamButton != null)
            {
                targetTrigger = mobileHighBeamButton.GetComponent<EventTrigger>();
                if (targetTrigger == null) targetTrigger = mobileHighBeamButton.gameObject.AddComponent<EventTrigger>();
            }
            if (targetTrigger == null) return;

            targetTrigger.triggers.Clear();
            var downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            downEntry.callback.AddListener(_ => touchInput.SetUiFocusHeld(true));
            targetTrigger.triggers.Add(downEntry);

            var upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            upEntry.callback.AddListener(_ => touchInput.SetUiFocusHeld(false));
            targetTrigger.triggers.Add(upEntry);
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
            if (chunkDirector != null) chunkDirector.OnSectorChanged += HandleSectorChanged;
            if (touchInput != null) touchInput.OnLongBeamHeldChanged += UpdateHighBeamVisual;
        }

        private void OnDisable()
        {
            if (touchInput != null)
            {
                touchInput.OnLongBeamHeldChanged -= UpdateHighBeamVisual;
                touchInput.SetUiFocusHeld(false);
            }
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
            if (chunkDirector != null) chunkDirector.OnSectorChanged -= HandleSectorChanged;
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (hudRoot != null)
            {
                hudRoot.SetActive(state == GameState.Playing || state == GameState.Paused || state == GameState.BlessingSelect);
            }
            if (state == GameState.Playing)
            {
                UpdateHighBeamVisual(touchInput != null && touchInput.IsLongBeamHeld);
                if (batterySystem != null)
                {
                    HandleBatteryChanged(batterySystem.CurrentBattery, batterySystem.MaxBattery);
                    HandlePhaseChanged(batterySystem.CurrentPhase);
                }
                _lastDisplayedMeters = -1;
                if (chunkDirector != null && chunkDirector.ActiveSector != null) HandleSectorChanged(chunkDirector.ActiveSector);
            }
            else if (touchInput != null)
            {
                touchInput.SetUiFocusHeld(false);
            }
        }

        private void HandleBatteryChanged(float current, float max)
        {
            float norm = max > 0.01f ? Mathf.Clamp01(current / max) : 0f;
            if (batteryFillImage != null) batteryFillImage.fillAmount = norm;

            int pct = Mathf.Clamp(Mathf.RoundToInt(norm * 100f), 0, 100);
            if (pct != _lastBatteryPercent)
            {
                _lastBatteryPercent = pct;
                if (batteryPercentText != null) batteryPercentText.text = $"BATTERY: {pct}%";
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
            if (sector == null) return;
            _lastSectorNum = sector.sectorNumber;
            _currentZoneHex = ColorUtility.ToHtmlStringRGB(sector.sectorAccentColor);
            _currentZoneName = sector.sectorName;
            RefreshDistanceZoneText(force: true);
        }

        private void RefreshDistanceZoneText(bool force = false)
        {
            if (sectorTitleText == null) return;
            int meters = chunkDirector != null ? Mathf.Max(0, Mathf.FloorToInt(chunkDirector.DistanceTraveledMeters)) : 0;
            if (!force && meters == _lastDisplayedMeters) return;
            _lastDisplayedMeters = meters;
            sectorTitleText.text = $"{meters}m  •  <color=#{_currentZoneHex}>{_currentZoneName}</color>";
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
            RefreshDistanceZoneText();

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

            if (eclipseWave != null && eclipseDangerFillImage != null)
            {
                float danger = eclipseWave.DangerNormalized;
                eclipseDangerFillImage.fillAmount = danger;
                eclipseDangerFillImage.color = Color.Lerp(new Color(0.65f, 0.22f, 1f, 0.85f), new Color(1.0f, 0.14f, 0.36f, 1f), danger);
            }

            if (_scorePunchTimer > 0f && scoreValueText != null)
            {
                _scorePunchTimer = Mathf.Max(0f, _scorePunchTimer - dt);
                float t = _scorePunchTimer / 0.16f;
                float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.14f;
                scoreValueText.transform.localScale = new Vector3(s, s, 1f);
            }

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
