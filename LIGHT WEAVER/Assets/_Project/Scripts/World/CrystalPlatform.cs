using System;
using LightWeaver.Core;
using LightWeaver.Player;
using LightWeaver.Pooling;
using UnityEngine;

namespace LightWeaver.World
{
    public enum CrystalVariant : byte
    {
        Standard = 0,     // Işıkta katılaşır, ışık gidince Afterglow sonrası kaybolur
        KineticBoost = 1, // Işıkta katılaşır + basana hız fırlatması verir
        FragileShard = 2, // Işıkta katılaşır + basıldıktan 0.24s sonra parçalanır
        UmbraBridge = 3   // TERS KURAL: Karanlıkta katıdır! Fener ışığı değerse ERİR!
    }

    /// <summary>
    /// 4 sistemik varyanta sahip prizmatik kristal platform:
    /// Standart, Hız Akımı (KineticBoost), Kırılgan Cam (FragileShard) ve Ters Faz Gölge Köprüsü (UmbraBridge).
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
    public sealed class CrystalPlatform : MonoBehaviour, ILightReactive, IPoolable
    {
        public static event Action<CrystalPlatform, float> OnPlatformSolidified;
        public static event Action<CrystalPlatform, CrystalVariant> OnVariantTriggered;

        [Header("Variant & Timings")]
        [SerializeField] private CrystalVariant variant = CrystalVariant.Standard;
        [SerializeField] private float solidifyThresholdSeconds = 0.008f;
        [SerializeField] private float afterglowDurationSeconds = 0.42f;
        [SerializeField] private float fragileBreakDelay = 0.24f;
        [SerializeField] private float umbraMeltCooldown = 0.65f;

        [Header("Visual Palette")]
        [SerializeField] private Color solidCyanColor = new Color(0.0f, 0.94f, 1.0f, 0.96f);
        [SerializeField] private Color boostGoldCyanColor = new Color(0.25f, 1.0f, 0.72f, 0.98f);
        [SerializeField] private Color fragileIceColor = new Color(0.78f, 0.96f, 1.0f, 0.95f);
        [SerializeField] private Color umbraDarkVioletColor = new Color(0.58f, 0.12f, 0.92f, 0.92f);
        [SerializeField] private Color afterglowVioletColor = new Color(0.71f, 0.22f, 0.95f, 0.78f);

        [Header("Variant Sprites & References")]
        [SerializeField] private Sprite standardCrystalSprite;
        [SerializeField] private Sprite boostCrystalSprite;
        [SerializeField] private Sprite fragileCrystalSprite;
        [SerializeField] private Sprite umbraCrystalSprite;
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
        private float _graceSolidTimer;
        private float _fragileCountdown = -1f;
        private float _umbraMeltTimer;
        private bool _steppedOnThisSpawn;

        public CrystalVariant Variant => variant;
        public Vector2 WorldPosition => transform.position;
        public HardlightPhase CurrentPhase => _currentPhase;
        public bool IsPhysicallySolid => _boxCollider != null && _boxCollider.enabled;

        private void Awake()
        {
            _boxCollider = GetComponent<BoxCollider2D>();
            _mainRenderer = GetComponent<SpriteRenderer>();
            _baseLocalScale = transform.localScale;
            ResetToDefaultState();
        }

        private void OnEnable()
        {
            LightReactiveRegistry.Register(this);
            ResetToDefaultState();
        }

        private void OnDisable()
        {
            LightReactiveRegistry.Unregister(this);
        }

        public void OnSpawnedFromPool()
        {
            _baseLocalScale = transform.localScale;
            ResetToDefaultState();
        }

        public void OnReturnedToPool()
        {
            variant = CrystalVariant.Standard;
            ResetToDefaultState();
        }

        public void SetVariant(CrystalVariant newVariant)
        {
            variant = newVariant;
            if (_mainRenderer != null)
            {
                Sprite targetSprite = variant switch
                {
                    CrystalVariant.KineticBoost => boostCrystalSprite != null ? boostCrystalSprite : standardCrystalSprite,
                    CrystalVariant.FragileShard => fragileCrystalSprite != null ? fragileCrystalSprite : standardCrystalSprite,
                    CrystalVariant.UmbraBridge => umbraCrystalSprite != null ? umbraCrystalSprite : standardCrystalSprite,
                    _ => standardCrystalSprite
                };
                if (targetSprite != null) _mainRenderer.sprite = targetSprite;
            }
            ResetToDefaultState();
        }

        public void ForceGraceSolid(float durationSeconds)
        {
            variant = CrystalVariant.Standard;
            _graceSolidTimer = durationSeconds;
            _currentPhase = HardlightPhase.Solid;
            _afterglowTimer = afterglowDurationSeconds;
            if (_boxCollider != null) _boxCollider.enabled = true;
            ApplyPhaseVisuals(HardlightPhase.Solid, 1f);
        }

        public void NotifyRunnerSteppedOn(RunnerMotor2D motor)
        {
            if (_steppedOnThisSpawn || motor == null) return;
            _steppedOnThisSpawn = true;

            if (variant == CrystalVariant.KineticBoost)
            {
                motor.ApplyKineticBoost(1.42f, 1.30f);
                _popScaleAnimTimer = 0.16f;
                OnVariantTriggered?.Invoke(this, variant);
            }
            else if (variant == CrystalVariant.FragileShard)
            {
                _fragileCountdown = fragileBreakDelay;
                OnVariantTriggered?.Invoke(this, variant);
            }
        }

        public Vector2 GetClosestLightSamplePoint(Vector2 beamOrigin, Vector2 beamDirection)
        {
            Vector2 center = transform.position;
            Vector2 rightAxis = transform.right;
            float halfWidth = Mathf.Abs(transform.lossyScale.x) * 0.48f;

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

        public void ResetToDefaultState()
        {
            _lightChargeTimer = 0f;
            _afterglowTimer = 0f;
            _popScaleAnimTimer = 0f;
            _graceSolidTimer = 0f;
            _fragileCountdown = -1f;
            _umbraMeltTimer = 0f;
            _lastExposedFrameTime = -999f;
            _steppedOnThisSpawn = false;

            if (variant == CrystalVariant.UmbraBridge)
            {
                // TERS KURAL: Gölge köprüsü karanlıkta KATI başlar!
                _currentPhase = HardlightPhase.Solid;
                if (_boxCollider != null) _boxCollider.enabled = true;
                ApplyPhaseVisuals(HardlightPhase.Solid, 1f);
            }
            else
            {
                _currentPhase = HardlightPhase.Phantom;
                if (_boxCollider != null) _boxCollider.enabled = false;
                ApplyPhaseVisuals(HardlightPhase.Phantom, 0f);
            }
        }

        public void OnLightExposure(Vector2 beamOrigin, float normalizedIntensity, bool isFocusedBeam, float deltaTime)
        {
            if (_fragileCountdown == 0f) return; // Kırılmış cam
            _lastExposedFrameTime = Time.time;

            if (variant == CrystalVariant.UmbraBridge)
            {
                // Işık değerse UmbraBridge 0.07s içinde erir ve yok olur!
                _lightChargeTimer += deltaTime * (isFocusedBeam ? 2.0f : 1.0f);
                if (_lightChargeTimer >= 0.07f && _currentPhase != HardlightPhase.Phantom)
                {
                    _currentPhase = HardlightPhase.Phantom;
                    _umbraMeltTimer = umbraMeltCooldown;
                    if (_boxCollider != null) _boxCollider.enabled = false;
                    ApplyPhaseVisuals(HardlightPhase.Phantom, 0f);
                }
                return;
            }

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

            if (_boxCollider != null) _boxCollider.enabled = true;

            ApplyPhaseVisuals(HardlightPhase.Solid, intensity);
            OnPlatformSolidified?.Invoke(this, intensity);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Kırılgan Cam geri sayımı
            if (_fragileCountdown > 0f)
            {
                _fragileCountdown -= dt;
                float shake = Mathf.Sin(Time.time * 65f) * 0.04f;
                transform.localScale = new Vector3(_baseLocalScale.x + shake, _baseLocalScale.y - shake, 1f);
                if (_fragileCountdown <= 0f)
                {
                    _fragileCountdown = 0f;
                    _currentPhase = HardlightPhase.Phantom;
                    if (_boxCollider != null) _boxCollider.enabled = false;
                    ApplyPhaseVisuals(HardlightPhase.Phantom, 0f);
                    return;
                }
            }
            else if (_popScaleAnimTimer > 0f)
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

            if (_graceSolidTimer > 0f)
            {
                _graceSolidTimer -= dt;
                return;
            }

            bool exposedRecently = (Time.time - _lastExposedFrameTime) <= 0.045f;
            if (variant == CrystalVariant.UmbraBridge)
            {
                if (!exposedRecently)
                {
                    _lightChargeTimer = 0f;
                    if (_currentPhase == HardlightPhase.Phantom)
                    {
                        _umbraMeltTimer -= dt;
                        if (_umbraMeltTimer <= 0f)
                        {
                            _currentPhase = HardlightPhase.Solid;
                            if (_boxCollider != null) _boxCollider.enabled = true;
                            ApplyPhaseVisuals(HardlightPhase.Solid, 1f);
                        }
                    }
                }
                return;
            }

            if (!exposedRecently && _fragileCountdown != 0f)
            {
                if (_currentPhase == HardlightPhase.Solid) _currentPhase = HardlightPhase.Afterglow;

                if (_currentPhase == HardlightPhase.Afterglow)
                {
                    _afterglowTimer -= dt;
                    if (_afterglowTimer <= 0f)
                    {
                        _currentPhase = HardlightPhase.Phantom;
                        _lightChargeTimer = 0f;
                        if (_boxCollider != null) _boxCollider.enabled = false;
                        ApplyPhaseVisuals(HardlightPhase.Phantom, 0f);
                    }
                    else
                    {
                        ApplyPhaseVisuals(HardlightPhase.Afterglow, Mathf.Clamp01(_afterglowTimer / afterglowDurationSeconds));
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
                    _mainRenderer.enabled = false;
                    if (rimGlowRenderer != null) rimGlowRenderer.enabled = false;
                    SetSparklesActive(false, 0f);
                    break;

                case HardlightPhase.Solid:
                    _mainRenderer.enabled = true;
                    Color baseSolid = variant switch
                    {
                        CrystalVariant.KineticBoost => boostGoldCyanColor,
                        CrystalVariant.FragileShard => fragileIceColor,
                        CrystalVariant.UmbraBridge => umbraDarkVioletColor,
                        _ => solidCyanColor
                    };
                    _mainRenderer.color = Color.Lerp(baseSolid, Color.white, variant == CrystalVariant.UmbraBridge ? 0f : factor * 0.30f);

                    if (rimGlowRenderer != null)
                    {
                        rimGlowRenderer.enabled = true;
                        rimGlowRenderer.color = variant switch
                        {
                            CrystalVariant.KineticBoost => new Color(1.0f, 0.88f, 0.18f, 0.95f),
                            CrystalVariant.UmbraBridge => new Color(1.0f, 0.20f, 0.65f, 0.95f),
                            _ => new Color(1f, 0.92f, 0.55f, 0.88f)
                        };
                    }
                    SetSparklesActive(variant != CrystalVariant.UmbraBridge, 1f);
                    break;

                case HardlightPhase.Afterglow:
                    _mainRenderer.enabled = true;
                    _mainRenderer.color = new Color(afterglowVioletColor.r, afterglowVioletColor.g, afterglowVioletColor.b, factor * afterglowVioletColor.a);
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
