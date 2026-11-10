using System;
using LightWeaver.Core;
using LightWeaver.Pooling;
using UnityEngine;

namespace LightWeaver.World
{
    /// <summary>
    /// Fener ışığı değdiğinde katılaşan (Hardlight Crystallization) ve ışık çekildiğinde
    /// 0.45 saniyelik "Akkor Soğuma" (Afterglow Coyote Time) süresi tanıyan prizmatik kristal platform.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public sealed class CrystalPlatform : MonoBehaviour, ILightReactive, IPoolable
    {
        public static event Action<CrystalPlatform, float> OnPlatformSolidified;

        [Header("Phase Timings (DDA Hardlight Standard)")]
        [SerializeField] private float solidifyThresholdSeconds = 0.04f;
        [SerializeField] private float afterglowDurationSeconds = 0.48f;

        [Header("Visual Palette")]
        [SerializeField] private Color phantomColor = new Color(0.10f, 0.55f, 1.0f, 0.09f);     // %9 Hayalet İzi
        [SerializeField] private Color solidCyanColor = new Color(0.0f, 0.94f, 1.0f, 0.96f);    // #00F0FF Parlak Kristal
        [SerializeField] private Color afterglowVioletColor = new Color(0.71f, 0.22f, 0.95f, 0.78f); // #B537F2 Akkor Soğuma

        [Header("Child Sparkle References")]
        [SerializeField] private SpriteRenderer rimGlowRenderer;
        [SerializeField] private Transform cornerSparkleLeft;
        [SerializeField] private Transform cornerSparkleRight;

        private BoxCollider2D _boxCollider;
        private SpriteRenderer _mainRenderer;
        private Vector3 _baseLocalScale;

        private HardlightPhase _currentPhase = HardlightPhase.Phantom;
        private float _lightChargeTimer;
        private float _afterglowTimer;
        private float _lastExposedFrameTime;
        private float _popScaleAnimTimer;

        public Vector2 WorldPosition => transform.position;
        public HardlightPhase CurrentPhase => _currentPhase;
        public bool IsPhysicallySolid => _boxCollider != null && _boxCollider.enabled;

        private void Awake()
        {
            _boxCollider = GetComponent<BoxCollider2D>();
            _mainRenderer = GetComponent<SpriteRenderer>();
            _baseLocalScale = transform.localScale;
            ApplyPhaseVisuals(HardlightPhase.Phantom, 0f);
        }

        private void OnEnable()
        {
            LightReactiveRegistry.Register(this);
            ResetToPhantom();
        }

        private void OnDisable()
        {
            LightReactiveRegistry.Unregister(this);
        }

        public void OnSpawnedFromPool()
        {
            _baseLocalScale = transform.localScale;
            ResetToPhantom();
        }

        public void OnReturnedToPool()
        {
            ResetToPhantom();
        }

        public void ResetToPhantom()
        {
            _currentPhase = HardlightPhase.Phantom;
            _lightChargeTimer = 0f;
            _afterglowTimer = 0f;
            _popScaleAnimTimer = 0f;
            if (_boxCollider != null) _boxCollider.enabled = false;
            ApplyPhaseVisuals(HardlightPhase.Phantom, 0f);
        }

        public void ForcePermanentSolid()
        {
            _currentPhase = HardlightPhase.Solid;
            _afterglowTimer = 999f;
            _lastExposedFrameTime = Time.time + 999f;
            if (_boxCollider != null) _boxCollider.enabled = true;
            ApplyPhaseVisuals(HardlightPhase.Solid, 1f);
        }

        public void OnLightExposure(Vector2 beamOrigin, float normalizedIntensity, bool isFocusedBeam, float deltaTime)
        {
            _lastExposedFrameTime = Time.time;
            float speedMultiplier = isFocusedBeam ? 2.0f : 1.0f;

            if (_currentPhase == HardlightPhase.Phantom)
            {
                _lightChargeTimer += deltaTime * speedMultiplier;
                if (_lightChargeTimer >= solidifyThresholdSeconds)
                {
                    TransitionToSolid(normalizedIntensity);
                }
            }
            else
            {
                _currentPhase = HardlightPhase.Solid;
                _afterglowTimer = afterglowDurationSeconds;
                ApplyPhaseVisuals(HardlightPhase.Solid, normalizedIntensity);
            }
        }

        private void TransitionToSolid(float intensity)
        {
            _currentPhase = HardlightPhase.Solid;
            _afterglowTimer = afterglowDurationSeconds;
            _popScaleAnimTimer = 0.16f;

            if (_boxCollider != null)
            {
                _boxCollider.enabled = true;
            }

            ApplyPhaseVisuals(HardlightPhase.Solid, intensity);
            OnPlatformSolidified?.Invoke(this, intensity);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Squash & Stretch Kristalleşme Büyüme Animasyonu
            if (_popScaleAnimTimer > 0f)
            {
                _popScaleAnimTimer -= dt;
                float t = Mathf.Clamp01(_popScaleAnimTimer / 0.16f);
                float punch = 1f + Mathf.Sin(t * Mathf.PI) * 0.12f;
                transform.localScale = new Vector3(_baseLocalScale.x * punch, _baseLocalScale.y * punch, 1f);
            }
            else
            {
                transform.localScale = _baseLocalScale;
            }

            // Bu karede ışık değmediyse Afterglow (Akkor Soğuma) sayacını çalıştır
            bool exposedRecently = (Time.time - _lastExposedFrameTime) <= 0.05f;
            if (!exposedRecently)
            {
                if (_currentPhase == HardlightPhase.Solid)
                {
                    _currentPhase = HardlightPhase.Afterglow;
                }

                if (_currentPhase == HardlightPhase.Afterglow)
                {
                    _afterglowTimer -= dt;
                    if (_afterglowTimer <= 0f)
                    {
                        ResetToPhantom();
                    }
                    else
                    {
                        float remainingRatio = Mathf.Clamp01(_afterglowTimer / afterglowDurationSeconds);
                        ApplyPhaseVisuals(HardlightPhase.Afterglow, remainingRatio);
                    }
                }
                else if (_currentPhase == HardlightPhase.Phantom && _lightChargeTimer > 0f)
                {
                    _lightChargeTimer = Mathf.Max(0f, _lightChargeTimer - dt * 2f);
                }
            }
        }

        private void ApplyPhaseVisuals(HardlightPhase phase, float factor)
        {
            if (_mainRenderer == null) return;

            switch (phase)
            {
                case HardlightPhase.Phantom:
                    _mainRenderer.color = phantomColor;
                    if (rimGlowRenderer != null) rimGlowRenderer.enabled = false;
                    SetSparklesActive(false, 0f);
                    break;

                case HardlightPhase.Solid:
                    _mainRenderer.color = Color.Lerp(solidCyanColor, Color.white, factor * 0.35f);
                    if (rimGlowRenderer != null)
                    {
                        rimGlowRenderer.enabled = true;
                        rimGlowRenderer.color = new Color(1f, 0.92f, 0.55f, 0.85f); // Altın/Beyaz prizma kenarı
                    }
                    SetSparklesActive(true, 1f);
                    break;

                case HardlightPhase.Afterglow:
                    Color glowCol = Color.Lerp(phantomColor, afterglowVioletColor, factor);
                    _mainRenderer.color = glowCol;
                    if (rimGlowRenderer != null)
                    {
                        rimGlowRenderer.enabled = true;
                        rimGlowRenderer.color = new Color(0.85f, 0.35f, 1f, factor * 0.65f);
                    }
                    SetSparklesActive(factor > 0.3f, factor);
                    break;
            }
        }

        private void SetSparklesActive(bool active, float scaleRatio)
        {
            float rot = Time.time * 90f;
            if (cornerSparkleLeft != null)
            {
                cornerSparkleLeft.gameObject.SetActive(active);
                if (active)
                {
                    cornerSparkleLeft.localScale = Vector3.one * (0.45f * scaleRatio);
                    cornerSparkleLeft.localRotation = Quaternion.Euler(0f, 0f, rot);
                }
            }

            if (cornerSparkleRight != null)
            {
                cornerSparkleRight.gameObject.SetActive(active);
                if (active)
                {
                    cornerSparkleRight.localScale = Vector3.one * (0.45f * scaleRatio);
                    cornerSparkleRight.localRotation = Quaternion.Euler(0f, 0f, -rot);
                }
            }
        }
    }
}
