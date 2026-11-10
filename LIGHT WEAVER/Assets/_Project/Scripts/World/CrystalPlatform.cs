using System;
using LightWeaver.Core;
using LightWeaver.Pooling;
using UnityEngine;

namespace LightWeaver.World
{
    /// <summary>
    /// Fener ışığı değdiğinde katılaşan (Hardlight Crystallization) ve ışık çekildiğinde
    /// 0.40 saniyelik "Akkor Soğuma" (Afterglow Coyote Time) sonrası zifiri karanlığa tamamen
    /// karışarak kaybolan prizmatik kristal platform.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public sealed class CrystalPlatform : MonoBehaviour, ILightReactive, IPoolable
    {
        public static event Action<CrystalPlatform, float> OnPlatformSolidified;

        [Header("Phase Timings (DDA Hardlight Standard)")]
        [SerializeField] private float solidifyThresholdSeconds = 0.008f;
        [SerializeField] private float afterglowDurationSeconds = 0.40f;

        [Header("Visual Palette")]
        [SerializeField] private Color solidCyanColor = new Color(0.0f, 0.94f, 1.0f, 0.96f);       // #00F0FF Parlak Kristal
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
        private float _lastExposedFrameTime = -999f;
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

        public Vector2 GetClosestLightSamplePoint(Vector2 beamOrigin, Vector2 beamDirection)
        {
            Vector2 center = transform.position;
            Vector2 rightAxis = transform.right;
            float halfWidth = Mathf.Abs(transform.lossyScale.x) * 0.48f;

            // Sol uç, orta, sağ uç ve ışın-eksen kesişim noktalarından koni eksenine en iyi hizalananı seç (Zero-GC)
            Vector2 bestPoint = center;
            float bestDot = ComputeAlignmentScore(center, beamOrigin, beamDirection);

            Vector2 leftPoint = center - rightAxis * halfWidth;
            float leftDot = ComputeAlignmentScore(leftPoint, beamOrigin, beamDirection);
            if (leftDot > bestDot) { bestDot = leftDot; bestPoint = leftPoint; }

            Vector2 rightPoint = center + rightAxis * halfWidth;
            float rightDot = ComputeAlignmentScore(rightPoint, beamOrigin, beamDirection);
            if (rightDot > bestDot) { bestDot = rightDot; bestPoint = rightPoint; }

            float cross = beamDirection.x * rightAxis.y - beamDirection.y * rightAxis.x;
            if (Mathf.Abs(cross) > 0.01f)
            {
                Vector2 diff = center - beamOrigin;
                float tRay = (diff.x * rightAxis.y - diff.y * rightAxis.x) / cross;
                if (tRay > 0f)
                {
                    Vector2 intersectPt = beamOrigin + beamDirection * tRay;
                    float localX = Mathf.Clamp(Vector2.Dot(intersectPt - center, rightAxis), -halfWidth, halfWidth);
                    Vector2 clampedIntersect = center + rightAxis * localX;
                    float interDot = ComputeAlignmentScore(clampedIntersect, beamOrigin, beamDirection);
                    if (interDot > bestDot) bestPoint = clampedIntersect;
                }
            }

            return bestPoint;
        }

        private static float ComputeAlignmentScore(Vector2 point, Vector2 origin, Vector2 dir)
        {
            Vector2 v = point - origin;
            float sqr = v.sqrMagnitude;
            if (sqr < 0.0001f) return 1f;
            return Vector2.Dot(v / Mathf.Sqrt(sqr), dir);
        }

        public void ResetToPhantom()
        {
            _currentPhase = HardlightPhase.Phantom;
            _lightChargeTimer = 0f;
            _afterglowTimer = 0f;
            _popScaleAnimTimer = 0f;
            _lastExposedFrameTime = -999f;

            if (_boxCollider != null) _boxCollider.enabled = false;
            ApplyPhaseVisuals(HardlightPhase.Phantom, 0f);
        }

        public void OnLightExposure(Vector2 beamOrigin, float normalizedIntensity, bool isFocusedBeam, float deltaTime)
        {
            _lastExposedFrameTime = Time.time;
            float speedMultiplier = isFocusedBeam ? 2.2f : 1.0f;

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
            _popScaleAnimTimer = 0.14f;

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
                float t = Mathf.Clamp01(_popScaleAnimTimer / 0.14f);
                float punch = 1f + Mathf.Sin(t * Mathf.PI) * 0.10f;
                transform.localScale = new Vector3(_baseLocalScale.x * punch, _baseLocalScale.y * punch, 1f);
            }
            else
            {
                transform.localScale = _baseLocalScale;
            }

            // Bu karede ışık değmediyse Afterglow (Akkor Soğuma) sonrası zifiri karanlıkta tamamen kaybol
            bool exposedRecently = (Time.time - _lastExposedFrameTime) <= 0.045f;
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
                    _lightChargeTimer = Mathf.Max(0f, _lightChargeTimer - dt * 3f);
                }
            }
        }

        private void ApplyPhaseVisuals(HardlightPhase phase, float factor)
        {
            if (_mainRenderer == null) return;

            switch (phase)
            {
                case HardlightPhase.Phantom:
                    // Işık yoksa platform %100 görünmez — zifiri karanlık uçurum!
                    _mainRenderer.enabled = false;
                    if (rimGlowRenderer != null) rimGlowRenderer.enabled = false;
                    SetSparklesActive(false, 0f);
                    break;

                case HardlightPhase.Solid:
                    _mainRenderer.enabled = true;
                    _mainRenderer.color = Color.Lerp(solidCyanColor, Color.white, factor * 0.35f);
                    if (rimGlowRenderer != null)
                    {
                        rimGlowRenderer.enabled = true;
                        rimGlowRenderer.color = new Color(1f, 0.92f, 0.55f, 0.88f);
                    }
                    SetSparklesActive(true, 1f);
                    break;

                case HardlightPhase.Afterglow:
                    _mainRenderer.enabled = true;
                    Color fadeOutCol = new Color(afterglowVioletColor.r, afterglowVioletColor.g, afterglowVioletColor.b, factor * afterglowVioletColor.a);
                    _mainRenderer.color = fadeOutCol;
                    if (rimGlowRenderer != null)
                    {
                        rimGlowRenderer.enabled = factor > 0.05f;
                        rimGlowRenderer.color = new Color(0.85f, 0.35f, 1f, factor * 0.6f);
                    }
                    SetSparklesActive(factor > 0.25f, factor);
                    break;
            }
        }

        private void SetSparklesActive(bool active, float scaleRatio)
        {
            float rot = Time.time * 90f;
            float invX = _baseLocalScale.x > 0.01f ? 1f / _baseLocalScale.x : 1f;
            float invY = _baseLocalScale.y > 0.01f ? 1f / _baseLocalScale.y : 1f;
            float worldSparkleSize = 0.42f * scaleRatio;

            if (cornerSparkleLeft != null)
            {
                cornerSparkleLeft.gameObject.SetActive(active);
                if (active)
                {
                    cornerSparkleLeft.localScale = new Vector3(worldSparkleSize * invX, worldSparkleSize * invY, 1f);
                    cornerSparkleLeft.localRotation = Quaternion.Euler(0f, 0f, rot);
                }
            }

            if (cornerSparkleRight != null)
            {
                cornerSparkleRight.gameObject.SetActive(active);
                if (active)
                {
                    cornerSparkleRight.localScale = new Vector3(worldSparkleSize * invX, worldSparkleSize * invY, 1f);
                    cornerSparkleRight.localRotation = Quaternion.Euler(0f, 0f, -rot);
                }
            }
        }
    }
}
