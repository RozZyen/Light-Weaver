using UnityEngine;
using UnityEngine.InputSystem;

namespace LightWeaver.Player
{
    /// <summary>
    /// Mobil tek parmak yay sürükleme (Arc Drag) ve PC Fare/Klavye ile
    /// fenerin ışık konisini (-60° ile +65° arası) yönlendiren girdi kontrolcüsü.
    /// Sağ tık veya ikinci parmak basılı tutulduğunda Odak Lazeri (Focus Beam) modunu açar.
    /// </summary>
    public sealed class FlashlightInputPreview : MonoBehaviour
    {
        [SerializeField] private LightConeCaster2D coneCaster;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float dragSensitivity = 0.28f;

        private float _targetAngleDeg = 8f;
        private bool _isDragging;
        private Vector2 _lastPointerPos;

        private void Awake()
        {
            if (coneCaster == null)
            {
                coneCaster = GetComponentInChildren<LightConeCaster2D>();
            }
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (coneCaster == null) return;

            bool isFocused = false;

            // 1. Mobil Dokunmatik (Touchscreen) Kontrolü
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
                if (!_isDragging)
                {
                    _isDragging = true;
                    _lastPointerPos = touchPos;
                }
                else
                {
                    float deltaY = touchPos.y - _lastPointerPos.y;
                    _targetAngleDeg = Mathf.Clamp(_targetAngleDeg + deltaY * dragSensitivity, -60f, 65f);
                    _lastPointerPos = touchPos;
                }

                // İkinci parmak dokunuyorsa Focus Beam aktif
                if (Touchscreen.current.touches.Count > 1 && Touchscreen.current.touches[1].press.isPressed)
                {
                    isFocused = true;
                }
            }
            // 2. PC Fare (Mouse) Kontrolü (İmleç konumuna doğrudan açısal bakış veya sürükleme)
            else if (Mouse.current != null)
            {
                _isDragging = false;
                Vector2 mouseScreen = Mouse.current.position.ReadValue();
                if (targetCamera != null)
                {
                    Vector3 worldPt = targetCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, Mathf.Abs(targetCamera.transform.position.z)));
                    Vector2 dir = (Vector2)worldPt - (Vector2)coneCaster.transform.position;
                    if (dir.sqrMagnitude > 0.01f)
                    {
                        float rawAngle = Mathf.Atan2(dir.y, Mathf.Max(0.25f, dir.x)) * Mathf.Rad2Deg;
                        _targetAngleDeg = Mathf.Clamp(rawAngle, -60f, 65f);
                    }
                }

                isFocused = Mouse.current.rightButton.isPressed;
            }

            // 3. Klavye Ok / W-S Tuşları Desteği
            if (Keyboard.current != null)
            {
                float keyAxis = 0f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) keyAxis += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) keyAxis -= 1f;
                if (Mathf.Abs(keyAxis) > 0.01f)
                {
                    _targetAngleDeg = Mathf.Clamp(_targetAngleDeg + keyAxis * 95f * Time.deltaTime, -60f, 65f);
                }

                if (Keyboard.current.spaceKey.isPressed || Keyboard.current.leftShiftKey.isPressed)
                {
                    isFocused = true;
                }
            }

            coneCaster.SetTargetAngle(_targetAngleDeg, isFocused);
        }
    }
}
