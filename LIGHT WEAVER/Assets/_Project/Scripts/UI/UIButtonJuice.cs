using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LightWeaver.UI
{
    /// <summary>
    /// Tüm butonlara dokunmatik basma (tactile squash/scale) ve parlama geri bildirimi ekler.
    /// Time.unscaledDeltaTime kullanır; Pause/Ana Menü/Mağaza ekranlarında da pürüzsüz çalışır.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float pressedScale = 0.93f;
        [SerializeField] private float lerpSpeed = 18f;

        private Vector3 _baseScale = Vector3.one;
        private float _targetScaleFactor = 1f;
        private Selectable _selectable;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _selectable = GetComponent<Selectable>();
        }

        private void OnEnable()
        {
            _targetScaleFactor = 1f;
            transform.localScale = _baseScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_selectable != null && !_selectable.interactable) return;
            _targetScaleFactor = pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _targetScaleFactor = 1f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _targetScaleFactor = 1f;
        }

        private void Update()
        {
            Vector3 desired = _baseScale * _targetScaleFactor;
            if ((transform.localScale - desired).sqrMagnitude > 0.00001f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, desired, Mathf.Clamp01(Time.unscaledDeltaTime * lerpSpeed));
            }
        }
    }
}
