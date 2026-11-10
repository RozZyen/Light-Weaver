using System.Collections.Generic;
using LightWeaver.Core;
using LightWeaver.Pooling;
using UnityEngine;

namespace LightWeaver.World
{
    /// <summary>
    /// Havada asılı elmas Işık Kırıcı Prizma. Fener ışığı değdiğinde aktifleşir ve
    /// önceden tanımlı açılara ikincil ışık huzmeleri kırarak kör noktalardaki kristal platformları
    /// ve bataryaları aynı anda aydınlatır (Zero-GC).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PrismRelay2D : MonoBehaviour, ILightReactive, IPoolable
    {
        [Header("Refraction Beams")]
        [SerializeField] private float[] refractionAnglesDeg = new[] { -22f, 18f };
        [SerializeField] private float beamRange = 10.5f;
        [SerializeField] private float beamHalfAngleDeg = 13f;
        [SerializeField] private float afterglowDurationSeconds = 0.42f;

        [Header("Visuals")]
        [SerializeField] private Color idleColor = new Color(0.71f, 0.22f, 0.95f, 0.42f);
        [SerializeField] private Color activeColor = new Color(0.92f, 0.98f, 1.0f, 1.0f);
        [SerializeField] private Color beamLineColor = new Color(0.0f, 0.94f, 1.0f, 0.72f);

        private SpriteRenderer _prismRenderer;
        private LineRenderer[] _beamLines;
        private HardlightPhase _currentPhase = HardlightPhase.Phantom;
        private float _afterglowTimer;
        private float _lastExposedTime = -999f;
        private Vector3 _baseScale;

        public Vector2 WorldPosition => transform.position;
        public HardlightPhase CurrentPhase => _currentPhase;

        private void Awake()
        {
            _prismRenderer = GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;
            CreateBeamLineRenderers();
            ApplyVisuals(false, 0f);
        }

        private void OnEnable()
        {
            LightReactiveRegistry.Register(this);
            ResetRelay();
        }

        private void OnDisable()
        {
            LightReactiveRegistry.Unregister(this);
        }

        public void OnSpawnedFromPool() => ResetRelay();
        public void OnReturnedToPool() => ResetRelay();

        public void ResetRelay()
        {
            _currentPhase = HardlightPhase.Phantom;
            _afterglowTimer = 0f;
            _lastExposedTime = -999f;
            ApplyVisuals(false, 0f);
        }

        private void CreateBeamLineRenderers()
        {
            int count = refractionAnglesDeg != null ? refractionAnglesDeg.Length : 0;
            _beamLines = new LineRenderer[count];
            Shader shader = Shader.Find("Sprites/Default");

            for (int i = 0; i < count; i++)
            {
                GameObject lineObj = new GameObject($"RefractedBeam_{i}");
                lineObj.transform.SetParent(transform, false);
                LineRenderer lr = lineObj.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.startWidth = 0.28f;
                lr.endWidth = 1.65f;
                lr.sortingOrder = 14;
                lr.useWorldSpace = true;
                if (shader != null) lr.sharedMaterial = new Material(shader);
                lr.startColor = beamLineColor;
                lr.endColor = new Color(0.71f, 0.22f, 0.95f, 0f);
                lr.enabled = false;
                _beamLines[i] = lr;
            }
        }

        public Vector2 GetClosestLightSamplePoint(Vector2 beamOrigin, Vector2 beamDirection)
        {
            return transform.position;
        }

        public void OnLightExposure(Vector2 beamOrigin, float normalizedIntensity, bool isFocusedBeam, float deltaTime)
        {
            _lastExposedTime = Time.time;
            _currentPhase = HardlightPhase.Solid;
            _afterglowTimer = afterglowDurationSeconds;

            ApplyVisuals(true, 1f);
            CastRefractedBeams(deltaTime, isFocusedBeam);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            transform.Rotate(0f, 0f, (_currentPhase == HardlightPhase.Solid ? 95f : 22f) * dt);

            bool exposedRecently = (Time.time - _lastExposedTime) <= 0.05f;
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
                        ResetRelay();
                    }
                    else
                    {
                        float factor = Mathf.Clamp01(_afterglowTimer / afterglowDurationSeconds);
                        ApplyVisuals(true, factor);
                        CastRefractedBeams(dt, false);
                    }
                }
            }
        }

        private void ApplyVisuals(bool active, float factor)
        {
            if (_prismRenderer != null)
            {
                _prismRenderer.color = active ? Color.Lerp(idleColor, activeColor, factor) : idleColor;
                transform.localScale = active ? _baseScale * (1f + 0.18f * factor) : _baseScale;
            }

            if (_beamLines == null) return;
            Vector2 origin = transform.position;
            for (int i = 0; i < _beamLines.Length; i++)
            {
                LineRenderer lr = _beamLines[i];
                if (lr == null) continue;
                lr.enabled = active;
                if (active)
                {
                    float rad = refractionAnglesDeg[i] * Mathf.Deg2Rad;
                    Vector3 endPos = new Vector3(origin.x + Mathf.Cos(rad) * beamRange, origin.y + Mathf.Sin(rad) * beamRange, 0f);
                    lr.SetPosition(0, origin);
                    lr.SetPosition(1, endPos);
                    lr.startColor = new Color(beamLineColor.r, beamLineColor.g, beamLineColor.b, beamLineColor.a * factor);
                }
            }
        }

        private void CastRefractedBeams(float dt, bool focused)
        {
            if (refractionAnglesDeg == null || refractionAnglesDeg.Length == 0) return;

            IReadOnlyList<ILightReactive> targets = LightReactiveRegistry.All;
            int targetCount = targets.Count;
            if (targetCount == 0) return;

            Vector2 origin = transform.position;
            float maxDistSq = beamRange * beamRange;
            float cosHalf = Mathf.Cos(beamHalfAngleDeg * Mathf.Deg2Rad);

            for (int b = 0; b < refractionAnglesDeg.Length; b++)
            {
                float rad = refractionAnglesDeg[b] * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                for (int i = 0; i < targetCount; i++)
                {
                    ILightReactive target = targets[i];
                    if (target == null || ReferenceEquals(target, this) || target is PrismRelay2D) continue;

                    Vector2 sample = target.GetClosestLightSamplePoint(origin, dir);
                    Vector2 toTarget = sample - origin;
                    float distSq = toTarget.sqrMagnitude;
                    if (distSq > maxDistSq || distSq < 0.01f) continue;

                    float dist = Mathf.Sqrt(distSq);
                    float dot = Vector2.Dot(dir, toTarget / dist);
                    if (dot >= cosHalf)
                    {
                        float intensity = Mathf.Clamp01(1f - (dist / beamRange));
                        target.OnLightExposure(origin, intensity, focused, dt);
                    }
                }
            }
        }
    }
}
