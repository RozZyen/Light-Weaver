using System;
using LightWeaver.Core;
using LightWeaver.Player;
using LightWeaver.Pooling;
using UnityEngine;

namespace LightWeaver.World
{
    /// <summary>
    /// Tapınak tavanında asılı Antik Horus Güneş Halkası.
    /// Oyuncu fener ışığını halkaya tuttuğu SÜRECE dedektif diğer elindeki halatı halkaya atar
    /// ve gerçek sarkaç fiziğiyle salınım yapar; ışığı başka yöne çevirdiğinde veya Jump'a
    /// bastığında teğetsel ivmeyle fırlayarak serbest kalır.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PhotonAnchor : MonoBehaviour, ILightReactive, IPoolable
    {
        public static event Action<PhotonAnchor> OnAnchorLaunched;

        [Header("Pendulum Swing Parameters")]
        [SerializeField] private float attachDelaySeconds = 0.025f;
        [SerializeField] private float holdSlipGraceSeconds = 0.18f;
        [SerializeField] private float reattachCooldownSeconds = 0.55f;
        [SerializeField] private float maxHorizontalPullReach = 9.2f;
        [SerializeField] private float maxVerticalReach = 8.8f;

        private const int RopeSegments = 10;

        private SpriteRenderer _sconceRenderer;
        private LineRenderer _ropeLine;
        private readonly Vector3[] _ropePositions = new Vector3[RopeSegments];
        private Vector3 _baseScale;

        private float _chargeTimer;
        private float _lastExposedTime = -999f;
        private float _cooldownTimer;
        private float _whipAnimTimer;
        private float _phaseOffset;
        private bool _isSwinging;
        private RunnerMotor2D _attachedMotor;

        public Vector2 WorldPosition => transform.position;
        public HardlightPhase CurrentPhase => _isSwinging ? HardlightPhase.Solid : HardlightPhase.Phantom;

        private void Awake()
        {
            _sconceRenderer = GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;
            _phaseOffset = (transform.position.x * 0.45f) % (Mathf.PI * 2f);
            CreateRopeLine();
            ResetAnchor();
        }

        private void OnEnable()
        {
            _phaseOffset = (transform.position.x * 0.45f) % (Mathf.PI * 2f);
            LightReactiveRegistry.Register(this);
            RunnerMotor2D.OnAnyRunnerRespawned += ResetAnchor;
            ResetAnchor();
        }

        private void OnDisable()
        {
            DetachIfSwinging();
            LightReactiveRegistry.Unregister(this);
            RunnerMotor2D.OnAnyRunnerRespawned -= ResetAnchor;
        }

        public void OnSpawnedFromPool()
        {
            _baseScale = transform.localScale;
            _phaseOffset = (transform.position.x * 0.45f) % (Mathf.PI * 2f);
            ResetAnchor();
        }

        public void OnReturnedToPool() => ResetAnchor();

        public void ResetAnchor()
        {
            DetachIfSwinging();
            _chargeTimer = 0f;
            _lastExposedTime = -999f;
            _cooldownTimer = 0f;
            _whipAnimTimer = 0f;
            _isSwinging = false;
            _attachedMotor = null;
            if (_ropeLine != null) _ropeLine.enabled = false;
            if (_sconceRenderer != null) _sconceRenderer.color = Color.white;
            transform.localScale = _baseScale;
            transform.rotation = Quaternion.identity;
        }

        private void DetachIfSwinging()
        {
            if (_isSwinging && _attachedMotor != null && _attachedMotor.IsSwinging)
            {
                _attachedMotor.EndPendulumSwing(withJumpBoost: false);
            }
            _isSwinging = false;
            _attachedMotor = null;
            if (_ropeLine != null) _ropeLine.enabled = false;
        }

        private void CreateRopeLine()
        {
            GameObject lineGo = new GameObject("GrappleRopeLine");
            lineGo.transform.SetParent(transform, false);
            _ropeLine = lineGo.AddComponent<LineRenderer>();
            _ropeLine.positionCount = RopeSegments;
            _ropeLine.startWidth = 0.068f;
            _ropeLine.endWidth = 0.056f;
            _ropeLine.sortingOrder = 15;
            _ropeLine.useWorldSpace = true;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) _ropeLine.sharedMaterial = new Material(shader);
            // Örgülü deri/kendir halat rengi + sıcak altın yansıması (çirkin neon çizgi yok!)
            _ropeLine.startColor = new Color(0.82f, 0.64f, 0.36f, 1f);
            _ropeLine.endColor = new Color(0.54f, 0.37f, 0.21f, 1f);
            _ropeLine.enabled = false;
        }

        public Vector2 GetClosestLightSamplePoint(Vector2 beamOrigin, Vector2 beamDirection)
        {
            Vector2 center = transform.position;
            Vector2 toCenter = center - beamOrigin;
            float along = Mathf.Max(0f, Vector2.Dot(toCenter, beamDirection));
            Vector2 closestOnRay = beamOrigin + beamDirection * along;
            float haloRadius = _isSwinging ? 0.98f : 0.56f;
            Vector2 offset = closestOnRay - center;
            float mag = offset.magnitude;
            if (mag <= haloRadius) return closestOnRay;
            return center + (offset / mag) * haloRadius;
        }

        public void OnLightExposure(Vector2 beamOrigin, float normalizedIntensity, bool isFocusedBeam, float deltaTime)
        {
            _lastExposedTime = Time.time;

            if (_isSwinging)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, _baseScale * 1.14f, deltaTime * 12f);
                if (_sconceRenderer != null) _sconceRenderer.color = new Color(1.0f, 0.94f, 0.74f, 1f);
                return;
            }

            if (_cooldownTimer > 0f) return;

            BatterySystem bat = BatterySystem.ActiveInstance;
            if (bat == null) return;
            RunnerMotor2D motor = bat.GetComponent<RunnerMotor2D>();
            if (motor == null || motor.IsSwinging) return;

            Vector2 toAnchor = (Vector2)transform.position - (Vector2)motor.transform.position;
            if (toAnchor.x < -2.2f || toAnchor.x > maxHorizontalPullReach || Mathf.Abs(toAnchor.y) > maxVerticalReach)
            {
                if (_sconceRenderer != null) _sconceRenderer.color = new Color(1.0f, 0.95f, 0.80f, 1f);
                return;
            }

            float mult = isFocusedBeam ? 2.4f : 1.0f;
            _chargeTimer += deltaTime * mult;
            transform.localScale = _baseScale * (1f + Mathf.Clamp01(_chargeTimer / Mathf.Max(0.01f, attachDelaySeconds)) * 0.16f);
            if (_sconceRenderer != null) _sconceRenderer.color = new Color(1.0f, 0.95f, 0.78f, 1f);

            if (_chargeTimer >= attachDelaySeconds)
            {
                AttachSwing(motor);
            }
        }

        private void AttachSwing(RunnerMotor2D motor)
        {
            _isSwinging = true;
            _attachedMotor = motor;
            _chargeTimer = 0f;
            _whipAnimTimer = 0.14f;
            _lastExposedTime = Time.time;

            motor.BeginPendulumSwing(transform.position, HandleMotorSwingEnded);
            if (_ropeLine != null)
            {
                _ropeLine.enabled = true;
                UpdateRopeGeometry(0f);
            }

            OnAnchorLaunched?.Invoke(this);
        }

        private void HandleMotorSwingEnded()
        {
            _isSwinging = false;
            _attachedMotor = null;
            _cooldownTimer = reattachCooldownSeconds;
            if (_ropeLine != null) _ropeLine.enabled = false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_cooldownTimer > 0f) _cooldownTimer = Mathf.Max(0f, _cooldownTimer - dt);

            if (_isSwinging)
            {
                if (_attachedMotor == null || !_attachedMotor.IsSwinging)
                {
                    HandleMotorSwingEnded();
                    return;
                }

                // Oyuncu fener ışığını halkadan çekerse (tolerans penceresinden sonra) halat serbest kalır!
                if ((Time.time - _lastExposedTime) > holdSlipGraceSeconds)
                {
                    _attachedMotor.EndPendulumSwing(withJumpBoost: false);
                    return;
                }

                if (_whipAnimTimer > 0f) _whipAnimTimer = Mathf.Max(0f, _whipAnimTimer - dt);
                UpdateRopeGeometry(dt);

                float pullTilt = Mathf.Clamp((_attachedMotor.transform.position.x - transform.position.x) * 4.2f, -15f, 15f);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 0f, pullTilt), dt * 10f);
                return;
            }

            // Hafif rüzgârda asılı zincir salınımı (dönme değil!)
            float windSway = Mathf.Sin(Time.time * 1.85f + _phaseOffset) * 2.4f;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 0f, windSway), dt * 6f);

            if ((Time.time - _lastExposedTime) > 0.06f)
            {
                _chargeTimer = Mathf.Max(0f, _chargeTimer - dt * 2.5f);
                transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, dt * 10f);
                if (_sconceRenderer != null)
                {
                    _sconceRenderer.color = Color.Lerp(_sconceRenderer.color, Color.white, dt * 8f);
                }
            }
        }

        private void UpdateRopeGeometry(float dt)
        {
            if (_ropeLine == null) return;

            Vector3 handPos = RunnerProceduralAnimator.ActiveInstance != null
                ? RunnerProceduralAnimator.ActiveInstance.RopeHandWorldPosition
                : (_attachedMotor != null ? _attachedMotor.transform.position + new Vector3(0.2f, 0.25f, 0f) : transform.position);
            Vector3 anchorPos = transform.position + new Vector3(0f, -0.22f, 0f);
            Vector3 delta = anchorPos - handPos;
            Vector3 perp = new Vector3(-delta.y, delta.x, 0f);
            if (perp.sqrMagnitude > 0.0001f) perp.Normalize();

            float whipDecay = _whipAnimTimer > 0f ? (_whipAnimTimer / 0.14f) : 0f;
            for (int i = 0; i < RopeSegments; i++)
            {
                float t = (float)i / (RopeSegments - 1);
                Vector3 pt = Vector3.Lerp(handPos, anchorPos, t);
                if (whipDecay > 0.001f)
                {
                    float wave = Mathf.Sin(t * Mathf.PI * 2.5f + whipDecay * 14f) * Mathf.Sin(t * Mathf.PI) * 0.28f * whipDecay;
                    pt += perp * wave;
                }
                _ropePositions[i] = pt;
            }
            _ropeLine.SetPositions(_ropePositions);
        }
    }
}
