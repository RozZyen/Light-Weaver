using System;
using LightWeaver.Core;
using LightWeaver.Player;
using LightWeaver.Pooling;
using UnityEngine;

namespace LightWeaver.World
{
    /// <summary>
    /// Dev uçurumların yukarısında asılı Foton Çapa Düğümü (Light Hook / Tether).
    /// Oyuncu fenerini yukarı kaldırıp düğümü aydınlattığında koşucuyu parlak bir ışık halatıyla
    /// yakalayıp dev uçurumun karşı yakasına fırlatır!
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PhotonAnchor : MonoBehaviour, ILightReactive, IPoolable
    {
        public static event Action<PhotonAnchor> OnAnchorLaunched;

        [Header("Tether & Launch Parameters")]
        [SerializeField] private float chargeDurationSeconds = 0.08f;
        [SerializeField] private float maxHorizontalPullReach = 7.8f;
        [SerializeField] private Vector2 launchVelocity = new Vector2(10.6f, 13.8f);
        [SerializeField] private float tetherVisualDuration = 0.28f;

        private SpriteRenderer _ringRenderer;
        private LineRenderer _tetherLine;
        private Vector3 _baseScale;

        private float _chargeTimer;
        private float _lastExposedTime = -999f;
        private float _cooldownTimer;
        private float _tetherDisplayTimer;
        private Transform _tetherTarget;

        public Vector2 WorldPosition => transform.position;
        public HardlightPhase CurrentPhase => _tetherDisplayTimer > 0f ? HardlightPhase.Solid : HardlightPhase.Phantom;

        private void Awake()
        {
            _ringRenderer = GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;
            CreateTetherLine();
            ResetAnchor();
        }

        private void OnEnable()
        {
            LightReactiveRegistry.Register(this);
            RunnerMotor2D.OnAnyRunnerRespawned += ResetAnchor;
            ResetAnchor();
        }

        private void OnDisable()
        {
            LightReactiveRegistry.Unregister(this);
            RunnerMotor2D.OnAnyRunnerRespawned -= ResetAnchor;
        }

        public void OnSpawnedFromPool()
        {
            _baseScale = transform.localScale;
            ResetAnchor();
        }

        public void OnReturnedToPool() => ResetAnchor();

        public void ResetAnchor()
        {
            _chargeTimer = 0f;
            _lastExposedTime = -999f;
            _cooldownTimer = 0f;
            _tetherDisplayTimer = 0f;
            _tetherTarget = null;
            if (_tetherLine != null) _tetherLine.enabled = false;
            if (_ringRenderer != null) _ringRenderer.color = new Color(0.0f, 0.94f, 1.0f, 0.85f);
            transform.localScale = _baseScale;
        }

        private void CreateTetherLine()
        {
            GameObject lineGo = new GameObject("PhotonTetherLine");
            lineGo.transform.SetParent(transform, false);
            _tetherLine = lineGo.AddComponent<LineRenderer>();
            _tetherLine.positionCount = 2;
            _tetherLine.startWidth = 0.16f;
            _tetherLine.endWidth = 0.08f;
            _tetherLine.sortingOrder = 19;
            _tetherLine.useWorldSpace = true;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) _tetherLine.sharedMaterial = new Material(shader);
            _tetherLine.startColor = new Color(0.0f, 0.96f, 1.0f, 1f);
            _tetherLine.endColor = new Color(1.0f, 0.92f, 0.45f, 1f);
            _tetherLine.enabled = false;
        }

        public Vector2 GetClosestLightSamplePoint(Vector2 beamOrigin, Vector2 beamDirection)
        {
            return transform.position;
        }

        public void OnLightExposure(Vector2 beamOrigin, float normalizedIntensity, bool isFocusedBeam, float deltaTime)
        {
            if (_cooldownTimer > 0f) return;
            _lastExposedTime = Time.time;

            BatterySystem bat = BatterySystem.ActiveInstance;
            if (bat == null) return;

            float dx = transform.position.x - bat.transform.position.x;
            if (dx < -1.8f || dx > maxHorizontalPullReach)
            {
                if (_ringRenderer != null) _ringRenderer.color = new Color(0.35f, 0.96f, 1f, 1f);
                return;
            }

            float mult = isFocusedBeam ? 2.2f : 1.0f;
            _chargeTimer += deltaTime * mult;
            transform.localScale = _baseScale * (1f + Mathf.Clamp01(_chargeTimer / chargeDurationSeconds) * 0.35f);

            if (_chargeTimer >= chargeDurationSeconds)
            {
                TriggerPhotonLaunch(bat.GetComponent<RunnerMotor2D>());
            }
        }

        private void TriggerPhotonLaunch(RunnerMotor2D motor)
        {
            if (motor == null) return;
            _cooldownTimer = 0.95f;
            _chargeTimer = 0f;
            _tetherDisplayTimer = tetherVisualDuration;
            _tetherTarget = motor.transform;

            motor.ApplyExternalImpulse(launchVelocity);
            if (_ringRenderer != null) _ringRenderer.color = new Color(1.0f, 0.92f, 0.35f, 1f);
            OnAnchorLaunched?.Invoke(this);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            transform.Rotate(0f, 0f, -65f * dt);

            if (_cooldownTimer > 0f) _cooldownTimer = Mathf.Max(0f, _cooldownTimer - dt);

            if (_tetherDisplayTimer > 0f && _tetherLine != null && _tetherTarget != null)
            {
                _tetherDisplayTimer -= dt;
                _tetherLine.enabled = _tetherDisplayTimer > 0f;
                _tetherLine.SetPosition(0, transform.position);
                _tetherLine.SetPosition(1, _tetherTarget.position + new Vector3(0.3f, 0.25f, 0f));
            }
            else if (_tetherLine != null)
            {
                _tetherLine.enabled = false;
            }

            if ((Time.time - _lastExposedTime) > 0.05f)
            {
                _chargeTimer = Mathf.Max(0f, _chargeTimer - dt * 2f);
                transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, dt * 10f);
                if (_ringRenderer != null)
                {
                    _ringRenderer.color = Color.Lerp(_ringRenderer.color, new Color(0.0f, 0.94f, 1.0f, 0.85f), dt * 6f);
                }
            }
        }
    }
}
