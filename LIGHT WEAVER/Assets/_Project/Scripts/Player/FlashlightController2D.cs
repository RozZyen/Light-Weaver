using UnityEngine;

namespace LightWeaver.Player
{
    /// <summary>
    /// Dedektifin modüler 2-eklemli fener kolunu (Üst Kol + Dirsek/Ön Kol + Fener) -105°..+105° (210°) yayda
    /// doğal dirsek bükülmesiyle döndürür ve uçtaki LightConeCaster2D konisini birebir hizalar.
    /// </summary>
    public sealed class FlashlightController2D : MonoBehaviour
    {
        [SerializeField] private Transform armPivot;
        [SerializeField] private Transform forearmPivot;
        [SerializeField] private Transform armVisualTransform;
        [SerializeField] private Transform headLookPivot;
        [SerializeField] private LightConeCaster2D coneCaster;

        public float CurrentAimAngleDeg => coneCaster != null ? coneCaster.CurrentAimAngleDeg : 0f;
        public bool IsFocusedBeam => coneCaster != null && coneCaster.IsFocusedBeam;
        public Vector2 EmitterWorldPosition => coneCaster != null ? (Vector2)coneCaster.transform.position : (Vector2)transform.position;

        private void Awake()
        {
            if (coneCaster == null)
            {
                coneCaster = GetComponentInChildren<LightConeCaster2D>();
            }
            if (armPivot == null)
            {
                armPivot = transform;
            }
        }

        public void SetAimInput(float targetAngleDeg, bool focusedBeam, bool snapImmediate = false)
        {
            if (coneCaster != null)
            {
                coneCaster.SetTargetAngle(targetAngleDeg, focusedBeam, snapImmediate);
            }
        }

        public void SetBatteryNormalized(float normalizedBattery)
        {
            if (coneCaster != null)
            {
                coneCaster.SetBatteryEfficiency(normalizedBattery);
            }
        }

        public void SetSpectrumVisuals(float efficiency, float rangeMult, float angleMult, Color tint)
        {
            if (coneCaster != null)
            {
                coneCaster.SetSpectrumVisuals(efficiency, rangeMult, angleMult, tint);
            }
        }

        private void LateUpdate()
        {
            if (armPivot == null || coneCaster == null) return;

            float angle = coneCaster.CurrentAimAngleDeg;
            armPivot.localScale = Vector3.one;

            if (forearmPivot != null)
            {
                // 2-eklemli modüler kol: dikey üst kol (-Y yönlü sprite) 0°'de hafif öne (+14°), +105°'de +132°, -105°'de -24° eğilir;
                // önkol ise dirsekten hedef fener açısını birebir kilitler!
                float shoulderDeg = angle >= 0f
                    ? Mathf.Lerp(14f, 132f, Mathf.InverseLerp(0f, 105f, angle))
                    : Mathf.Lerp(14f, -26f, Mathf.InverseLerp(0f, -105f, angle));
                armPivot.rotation = Quaternion.Euler(0f, 0f, shoulderDeg);
                forearmPivot.rotation = Quaternion.Euler(0f, 0f, angle);
            }
            else
            {
                armPivot.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (headLookPivot != null)
            {
                float headPitch = Mathf.Clamp(angle * 0.16f, -15f, 20f);
                headLookPivot.localRotation = Quaternion.Euler(0f, 0f, headPitch);
            }
        }
    }
}
