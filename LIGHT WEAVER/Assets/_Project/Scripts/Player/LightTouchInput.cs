using System;
using LightWeaver.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace LightWeaver.Player
{
    /// <summary>
    /// Tam 360 derece fener kontrolcüsü:
    /// - PC: Fare 360° imleç takibi + WASD / Ok tuşları 8-yönlü 360° nişan + Space Zıplama + Shift/SağTık Odak + F Burst.
    /// - Mobil Mod 0 (Floating 360° Stick): Sağ yarı ekranda dokunulan yerde açılan şeffaf sanal joystick ile parmağın ekranı kapatmadan 360° nişan.
    /// - Mobil Mod 1 (Direct Touch): Ekranda dokunulan/sürüklenen koordinata doğrudan 360° ışık tutma.
    /// </summary>
    public sealed class LightTouchInput : MonoBehaviour
    {
        public event Action OnFlashBurstTriggered;

        [Header("References")]
        [SerializeField] private FlashlightController2D flashlightController;
        [SerializeField] private RunnerMotor2D runnerMotor;
        [SerializeField] private Camera targetCamera;

        [Header("Floating 360 Joystick UI (Optional)")]
        [SerializeField] private RectTransform joystickBaseRect;
        [SerializeField] private RectTransform joystickKnobRect;
        [SerializeField] private float joystickMaxRadiusPx = 95f;
        [SerializeField] private float joystickDeadzonePx = 10f;

        [Header("Aim Tuning")]
        [SerializeField] private float initialAngleDeg = -10f;
        [SerializeField] private float doubleTapMaxInterval = 0.28f;

        private float _targetAngleDeg;
        private bool _mouseAimActive;
        private bool _externalUiFocusHeld;

        private bool _isStickActive;
        private int _aimTouchId = -1;
        private Vector2 _stickOriginScreenPos;
        private float _lastTapTime = -10f;

        public float TargetAngleDeg => _targetAngleDeg;

        private void Awake()
        {
            ProgressManager.EnsureLoaded();
            _targetAngleDeg = initialAngleDeg;
            if (flashlightController == null) flashlightController = GetComponentInChildren<FlashlightController2D>();
            if (runnerMotor == null) runnerMotor = GetComponent<RunnerMotor2D>();
            if (targetCamera == null) targetCamera = Camera.main;
            HideJoystickUi();
        }

        private void OnEnable()
        {
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            HideJoystickUi();
        }

        public void SetUiFocusHeld(bool held) => _externalUiFocusHeld = held;

        public void ResetAimToInitial()
        {
            _targetAngleDeg = initialAngleDeg;
            _mouseAimActive = false;
            _isStickActive = false;
            _aimTouchId = -1;
            _externalUiFocusHeld = false;
            HideJoystickUi();
            if (flashlightController != null) flashlightController.SetAimInput(_targetAngleDeg, false, snapImmediate: true);
        }

        private void Update()
        {
            if (flashlightController == null) return;
            if (GameFlowManager.ActiveInstance != null && GameFlowManager.ActiveInstance.CurrentState != GameState.Playing)
            {
                _isStickActive = false;
                _aimTouchId = -1;
                _externalUiFocusHeld = false;
                HideJoystickUi();
                return;
            }

            bool isFocused = _externalUiFocusHeld;
            float dt = Time.deltaTime;

            // 1. Klavye (Space Zıplama, WASD/Ok 8-yön 360° Nişan, Shift Focus, F/E Burst)
            Keyboard kb = Keyboard.current;
            bool keyboardAimed = false;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame && runnerMotor != null) runnerMotor.RequestJump();

                float kx = 0f;
                float ky = 0f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) kx += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) kx -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) ky += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) ky -= 1f;

                if (kx != 0f || ky != 0f)
                {
                    keyboardAimed = true;
                    _mouseAimActive = false;
                    // Yalnızca W veya S'ye basılırsa mevcut yatay yöne göre yumuşak açısal dönüş;
                    // hem X hem Y basılırsa doğrudan 360° vektör açısı!
                    if (kx != 0f)
                    {
                        float desiredAngle = Mathf.Atan2(ky, kx) * Mathf.Rad2Deg;
                        _targetAngleDeg = Mathf.MoveTowardsAngle(_targetAngleDeg, desiredAngle, 420f * dt);
                    }
                    else
                    {
                        _targetAngleDeg += ky * 165f * dt;
                    }
                }

                if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) isFocused = true;
                if (kb.fKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame) OnFlashBurstTriggered?.Invoke();
            }

            // 2. Mobil Çoklu Dokunma (Sol el UI butonuna basarken sağ el serbestçe 360° nişan alabilir!)
            bool handledTouchAim = ProcessMultiTouchAim(ref isFocused);

            // 3. PC Fare (Dokunmatik & Klavye aktif değilken, UI üzerinde değilken 360° imleç takibi)
            Mouse mouse = Mouse.current;
            bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (!handledTouchAim && !keyboardAimed && mouse != null && !pointerOverUi)
            {
                if (mouse.delta.ReadValue().sqrMagnitude > 2.25f || mouse.leftButton.isPressed)
                {
                    _mouseAimActive = true;
                }

                if (_mouseAimActive && targetCamera != null)
                {
                    Vector2 mouseScreen = mouse.position.ReadValue();
                    AimTowardScreenPoint(mouseScreen);
                }

                if (mouse.rightButton.isPressed) isFocused = true;
            }

            flashlightController.SetAimInput(_targetAngleDeg, isFocused);
        }

        private bool ProcessMultiTouchAim(ref bool isFocused)
        {
            var activeTouches = Touch.activeTouches;
            int count = activeTouches.Count;
            if (count == 0)
            {
                if (_isStickActive)
                {
                    _isStickActive = false;
                    _aimTouchId = -1;
                    HideJoystickUi();
                }
                return false;
            }

            // Aktif nişan parmağını takip et veya yeni dokunma yakala
            for (int i = 0; i < count; i++)
            {
                Touch t = activeTouches[i];
                if (_aimTouchId == -1 && t.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    // UI butonunun üstüne dokunulmadıysa bu parmağı nişan parmağı yap
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.touchId))
                        continue;

                    _aimTouchId = t.touchId;
                    _stickOriginScreenPos = t.screenPosition;
                    _isStickActive = true;

                    if ((Time.unscaledTime - _lastTapTime) <= doubleTapMaxInterval)
                    {
                        OnFlashBurstTriggered?.Invoke();
                        _lastTapTime = -10f;
                    }
                    else
                    {
                        _lastTapTime = Time.unscaledTime;
                    }
                }

                if (t.touchId == _aimTouchId)
                {
                    if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended || t.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                    {
                        _aimTouchId = -1;
                        _isStickActive = false;
                        HideJoystickUi();
                        return true;
                    }

                    if (ProgressManager.MobileAimMode == 0)
                    {
                        // MOD 0: 360° Floating Virtual Stick (Parmağın ekranı kapatmadan tam kontrol!)
                        Vector2 delta = t.screenPosition - _stickOriginScreenPos;
                        float dist = delta.magnitude;
                        if (dist > joystickDeadzonePx)
                        {
                            _targetAngleDeg = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                            // Parmağı çok uzağa çekince otomatik Focus Lazeri (veya UI Focus butonu)
                            if (dist > joystickMaxRadiusPx * 1.15f) isFocused = true;
                        }
                        UpdateJoystickUi(_stickOriginScreenPos, Vector2.ClampMagnitude(delta, joystickMaxRadiusPx));
                    }
                    else
                    {
                        // MOD 1: Direct Touch Aim (Dokunduğun noktaya doğrudan 360° ışık tut!)
                        HideJoystickUi();
                        AimTowardScreenPoint(t.screenPosition);
                    }
                    return true;
                }
            }

            return _isStickActive;
        }

        private void AimTowardScreenPoint(Vector2 screenPoint)
        {
            if (targetCamera == null || flashlightController == null) return;
            float camDepth = Mathf.Abs(targetCamera.transform.position.z);
            Vector3 worldPt = targetCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, camDepth));
            Vector2 dir = (Vector2)worldPt - flashlightController.EmitterWorldPosition;
            if (dir.sqrMagnitude > 0.04f)
            {
                _targetAngleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            }
        }

        private void UpdateJoystickUi(Vector2 originScreen, Vector2 clampedOffset)
        {
            if (joystickBaseRect == null || joystickKnobRect == null) return;
            if (!joystickBaseRect.gameObject.activeSelf) joystickBaseRect.gameObject.SetActive(true);
            joystickBaseRect.position = originScreen;
            joystickKnobRect.anchoredPosition = clampedOffset;
        }

        private void HideJoystickUi()
        {
            if (joystickBaseRect != null && joystickBaseRect.gameObject.activeSelf)
            {
                joystickBaseRect.gameObject.SetActive(false);
            }
        }
    }
}
