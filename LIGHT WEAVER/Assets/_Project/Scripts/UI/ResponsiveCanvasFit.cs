using UnityEngine;
using UnityEngine.UI;

namespace LightWeaver.UI
{
    /// <summary>
    /// Tüm telefon ve tablet çözünürlüklerinde (16:9, 19.5:9, 20:9, 21:9, 4:3)
    /// CanvasScaler'ın matchWidthOrHeight oranını dinamik ayarlayarak taşma veya ezilmeyi önler.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class ResponsiveCanvasFit : MonoBehaviour
    {
        [SerializeField] private float referenceAspect = 16f / 9f;

        private CanvasScaler _scaler;
        private int _lastWidth;
        private int _lastHeight;

        private void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
            ApplyAspectMatch();
        }

        private void Update()
        {
            if (Screen.width != _lastWidth || Screen.height != _lastHeight)
            {
                ApplyAspectMatch();
            }
        }

        private void ApplyAspectMatch()
        {
            if (_scaler == null || Screen.width <= 0 || Screen.height <= 0) return;
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            float aspect = (float)Screen.width / Screen.height;
            // Geniş telefonlarda (16:9..21:9) yüksekliği koru (1.0), dar/tablet ekranlarda (4:3) genişliği koru (0.0)
            _scaler.matchWidthOrHeight = aspect >= referenceAspect - 0.02f ? 1.0f : 0.0f;
        }
    }
}
