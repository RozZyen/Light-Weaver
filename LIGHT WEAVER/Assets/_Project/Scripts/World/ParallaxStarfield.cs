using UnityEngine;

namespace LightWeaver.World
{
    /// <summary>
    /// Sonsuz koşu boyunca kamerayla birlikte akıp sarmalanan (wrap-around) sıfır tahsisli
    /// uzay tozu & yıldız derinlik alanı.
    /// </summary>
    public sealed class ParallaxStarfield : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float fieldWidth = 34f;
        [SerializeField] private float fieldHeight = 18f;
        [SerializeField] private float parallaxFactor = 0.85f;

        private Transform[] _stars;
        private Vector3 _lastCamPos;

        private void Awake()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            int count = transform.childCount;
            _stars = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                _stars[i] = transform.GetChild(i);
            }

            if (cameraTransform != null) _lastCamPos = cameraTransform.position;
        }

        private void LateUpdate()
        {
            if (cameraTransform == null || _stars == null) return;

            Vector3 camPos = cameraTransform.position;
            Vector3 delta = camPos - _lastCamPos;
            _lastCamPos = camPos;

            float halfW = fieldWidth * 0.5f;
            float halfH = fieldHeight * 0.5f;

            for (int i = 0; i < _stars.Length; i++)
            {
                Transform star = _stars[i];
                if (star == null) continue;

                Vector3 pos = star.position + delta * parallaxFactor;
                float dx = pos.x - camPos.x;
                float dy = pos.y - camPos.y;

                if (dx < -halfW) pos.x += fieldWidth;
                else if (dx > halfW) pos.x -= fieldWidth;

                if (dy < -halfH) pos.y += fieldHeight;
                else if (dy > halfH) pos.y -= fieldHeight;

                star.position = pos;
            }
        }
    }
}
