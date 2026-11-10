using System;
using LightWeaver.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LightWeaver.Player
{
    /// <summary>
    /// Mobil dokunmatik yay sürükleme + butonlar ve PC Klavye/Fare girdilerini yönetir.
    /// Space -> Zıplama/Çift Zıplama, Sağ Tık / Shift -> Odak Lazeri, F/E / Çift Dokunuş -> Flash-Burst.
    /// </summary>
    public sealed class LightTouchInput : MonoBehaviour
    {
        public event Action OnFlashBurstTriggered;

        [Header("References")]
        [SerializeField] private FlashlightController2D flashlightController;
        [SerializeField] private RunnerMotor2D runnerMotor;
        [SerializeField] private Camera targetCamera;

        [Header("Aim Sensitivity & Limits")]
        [SerializeField] private float minAngleDeg = -62f;
        [SerializeField] private float maxAngleDeg = 70f;
        [SerializeField] private float initialAngleDeg = -14f;
        [SerializeField] private float touchDragSensitivity = 0.28f;
        [SerializeField] private float mouseDragSensitivity = 0.32f;
        [SerializeField] private float keyboardAimSpeedDegPerSec = 135f;
        [SerializeField] private float doubleTapMaxInterval = 0.28f;

        private float _targetAngleDeg;
        private bool _isTouchDragging;
        private bool _isMouseDragging;
        private bool _mouseAimActive;
        private bool _externalUiFocusHeld;
        private Vector2 _lastPointerPos;
        private float _lastTapTime = -10f;

        public float TargetAngleDeg => _targetAngleDeg;

        private void Awake()
        {
            _targetAngleDeg = initialAngleDeg;
            if (flashlightController == null) flashlightController = GetComponentInChildren<FlashlightController2D>();
            if (runnerMotor == null) runnerMotor = GetComponent<RunnerMotor2D>();
            if (targetCamera == null) targetCamera = Camera.main;
        }

        public void SetUiFocusHeld(bool held) => _externalUiFocusHeld = held;

        public void ResetAimToInitial()
        {
            _targetAngleDeg = initialAngleDeg;
            _mouseAimActive = false;
            _isTouchDragging = false;
            _isMouseDragging = false;
            _externalUiFocusHeld = false;
            if (flashlightController != null) flashlightController.SetAimInput(_targetAngleDeg, false, snapImmediate: true);
        }

        private void Update()
        {
            if (flashlightController == null) return;
            if (GameFlowManager.ActiveInstance != null && GameFlowManager.ActiveInstance.CurrentState != GameState.Playing)
            {
                _isTouchDragging = false;
                _isMouseDragging = false;
                _externalUiFocusHeld = false;
                return;
            }

            bool isFocused = _externalUiFocusHeld;
            float dt = Time.deltaTime;

            // Zıplama & Flash-Burst Klavye Kısayolları (UI üstünde değilse fare drag/aim devam eder)
            Keyboard kb = Keyboard.current;
            bool keyboardAimedThisFrame = false;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame && runnerMotor != null)
                {
                    runnerMotor.RequestJump();
                }

                float keyAxis = 0f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) keyAxis += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) keyAxis -= 1f;
                if (Mathf.Abs(keyAxis) > 0.01f)
                {
                    keyboardAimedThisFrame = true;
                    _mouseAimActive = false;
                    _targetAngleDeg = Mathf.Clamp(_targetAngleDeg + keyAxis * keyboardAimSpeedDegPerSec * dt, minAngleDeg, maxAngleDeg);
                }

                if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) isFocused = true;
                if (kb.fKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame) OnFlashBurstTriggered?.Invoke();
            }

            // UI elemanlarının üzerine dokunulduğunda yalnızca aim sürüklemesini pas geç
            bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            // 1. Mobil Dokunmatik
            Touchscreen touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed && !pointerOverUi)
            {
                _mouseAimActive = false;
                if (touch.primaryTouch.press.wasPressedThisFrame)
                {
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

                Vector2 touchPos = touch.primaryTouch.position.ReadValue();
                if (!_isTouchDragging)
                {
                    _isTouchDragging = true;
                    _lastPointerPos = touchPos;
                }
                else
                {
                    float deltaY = touchPos.y - _lastPointerPos.y;
                    _targetAngleDeg = Mathf.Clamp(_targetAngleDeg + deltaY * touchDragSensitivity, minAngleDeg, maxAngleDeg);
                    _lastPointerPos = touchPos;
                }
            }
            else
            {
                _isTouchDragging = false;
            }

            // 2. PC Fare (UI üstünde değilken)
            Mouse mouse = Mouse.current;
            if (mouse != null && !_isTouchDragging && !keyboardAimedThisFrame && !pointerOverUi)
            {
                if (mouse.leftButton.isPressed)
                {
                    _mouseAimActive = false;
                    Vector2 mouseScreen = mouse.position.ReadValue();
                    if (!_isMouseDragging)
                    {
                        _isMouseDragging = true;
                        _lastPointerPos = mouseScreen;
                    }
                    else
                    {
                        float deltaY = mouseScreen.y - _lastPointerPos.y;
                        _targetAngleDeg = Mathf.Clamp(_targetAngleDeg + deltaY * mouseDragSensitivity, minAngleDeg, maxAngleDeg);
                        _lastPointerPos = mouseScreen;
                    }
                }
                else
                {
                    _isMouseDragging = false;
                    if (mouse.delta.ReadValue().sqrMagnitude > 2.25f) _mouseAimActive = true;

                    if (_mouseAimActive && targetCamera != null)
                    {
                        Vector2 mouseScreen = mouse.position.ReadValue();
                        float camDepth = Mathf.Abs(targetCamera.transform.position.z);
                        Vector3 worldPt = targetCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, camDepth));
                        Vector2 dir = (Vector2)worldPt - flashlightController.EmitterWorldPosition;
                        if (dir.sqrMagnitude > 0.04f)
                        {
                            float rawAngle = Mathf.Atan2(dir.y, Mathf.Max(0.35f, dir.x)) * Mathf.Rad2Deg;
                            _targetAngleDeg = Mathf.Clamp(rawAngle, minAngleDeg, maxAngleDeg);
                        }
                    }
                }

                if (mouse.rightButton.isPressed) isFocused = true;
            }

            flashlightController.SetAimInput(_targetAngleDeg, isFocused);
        }
    }
}
