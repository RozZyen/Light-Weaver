using UnityEngine;

namespace LightWeaver.Player
{
    /// <summary>
    /// Koşucu karakterin fener tutan omuz/kol pivotunu hedef açıya pürüzsüzce döndürür ve
    /// uçtaki LightConeCaster2D ışık konisini senkronize eder.
    /// </summary>
    public sealed class FlashlightController2D : MonoBehaviour
    {
        [SerializeField] private Transform armPivot;
        [SerializeField] private Transform armVisualTransform;
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
            if (armPivot != null && coneCaster != null)
            {
                float angle = coneCaster.CurrentAimAngleDeg;
                armPivot.rotation = Quaternion.Euler(0f, 0f, angle);
                armPivot.localScale = Vector3.one;

                // 360° geriye çevrildiğinde yalnızca kol sprite'ını aynala; LightConeCaster2D ölçeğini (1,1,1) koru!
                if (armVisualTransform != null)
                {
                    float norm = Mathf.Repeat(angle + 180f, 360f) - 180f;
                    float flipSign = Mathf.Abs(norm) > 90f ? -1f : 1f;
                    Vector3 s = armVisualTransform.localScale;
                    armVisualTransform.localScale = new Vector3(Mathf.Abs(s.x) * flipSign, Mathf.Abs(s.y), s.z);
                }
            }
        }
    }
}
