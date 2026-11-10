using LightWeaver.Core;
using LightWeaver.Player;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LightWeaver.World
{
    /// <summary>
    /// Mesafe (metre) ilerledikçe ana Mısır temasından kopmadan gökyüzünü, piramit ışığını,
    /// kum fırtınasını (Hamsin), lanet sisini ve tutulma küllerini YUMUŞAK (SmoothStep)
    /// geçişlerle dönüştürerek zorluğu hissettiren Zero-GC atmosfer yöneticisi.
    /// </summary>
    public sealed class DesertAtmosphereController : MonoBehaviour
    {
        private readonly struct WeatherStage
        {
            public readonly float StartMeters;
            public readonly Color SkyColor;
            public readonly Color PanoramaTint;
            public readonly Color DuneTint;
            public readonly Color HazeTint;
            public readonly Color WindColor;
            public readonly float WindRate;
            public readonly float WindSpeed;
            public readonly Color MoteColor;
            public readonly float MoteRate;
            public readonly Color VignetteColor;
            public readonly float VignetteIntensity;
            public readonly float BloomIntensity;

            public WeatherStage(
                float startMeters,
                Color skyColor,
                Color panoramaTint,
                Color duneTint,
                Color hazeTint,
                Color windColor,
                float windRate,
                float windSpeed,
                Color moteColor,
                float moteRate,
                Color vignetteColor,
                float vignetteIntensity,
                float bloomIntensity)
            {
                StartMeters = startMeters;
                SkyColor = skyColor;
                PanoramaTint = panoramaTint;
                DuneTint = duneTint;
                HazeTint = hazeTint;
                WindColor = windColor;
                WindRate = windRate;
                WindSpeed = windSpeed;
                MoteColor = moteColor;
                MoteRate = moteRate;
                VignetteColor = vignetteColor;
                VignetteIntensity = vignetteIntensity;
                BloomIntensity = bloomIntensity;
            }
        }

        private static readonly WeatherStage[] Stages =
        {
            // 0m+: Gize Dolunay Gecesi (Temiz gece göğü, hafif çöl ateşi tozları)
            new WeatherStage(
                0f,
                new Color(0.035f, 0.045f, 0.102f, 1f),
                new Color(0.92f, 0.94f, 1.00f, 1f),
                new Color(0.14f, 0.11f, 0.17f, 0.94f),
                new Color(0.05f, 0.07f, 0.16f, 0.00f),
                new Color(0.88f, 0.78f, 0.54f, 0.18f),
                2f,
                5.5f,
                new Color(1.00f, 0.88f, 0.56f, 0.34f),
                6f,
                new Color(0.01f, 0.02f, 0.06f, 1f),
                0.26f,
                0.72f),

            // 240m+: Kızıl Hamsin Kum Fırtınası (Yumuşak kehribar pus ve hızlı uçuşan kum şeritleri)
            new WeatherStage(
                240f,
                new Color(0.125f, 0.072f, 0.095f, 1f),
                new Color(0.98f, 0.78f, 0.56f, 1f),
                new Color(0.24f, 0.15f, 0.12f, 0.96f),
                new Color(0.58f, 0.34f, 0.16f, 0.18f),
                new Color(0.94f, 0.72f, 0.44f, 0.42f),
                38f,
                14.5f,
                new Color(1.00f, 0.78f, 0.38f, 0.48f),
                14f,
                new Color(0.12f, 0.05f, 0.03f, 1f),
                0.34f,
                0.78f),

            // 520m+: Batık Tapınak Lanet Sisi (Serin turkuaz-yeşim ay ışığı, esrarengiz sis sporları)
            new WeatherStage(
                520f,
                new Color(0.032f, 0.105f, 0.135f, 1f),
                new Color(0.68f, 0.96f, 0.88f, 1f),
                new Color(0.07f, 0.17f, 0.18f, 0.96f),
                new Color(0.08f, 0.32f, 0.34f, 0.22f),
                new Color(0.55f, 0.92f, 0.84f, 0.28f),
                24f,
                11.0f,
                new Color(0.24f, 0.94f, 0.84f, 0.54f),
                26f,
                new Color(0.02f, 0.09f, 0.10f, 1f),
                0.36f,
                0.86f),

            // 860m+: Kızıl Ay Tutulması (Yakıcı kızıl gök, savrulan kor küller)
            new WeatherStage(
                860f,
                new Color(0.125f, 0.038f, 0.078f, 1f),
                new Color(0.96f, 0.58f, 0.54f, 1f),
                new Color(0.20f, 0.07f, 0.10f, 0.96f),
                new Color(0.42f, 0.10f, 0.14f, 0.24f),
                new Color(0.95f, 0.52f, 0.38f, 0.44f),
                48f,
                16.5f,
                new Color(1.00f, 0.44f, 0.22f, 0.70f),
                32f,
                new Color(0.16f, 0.03f, 0.05f, 1f),
                0.40f,
                0.90f),

            // 1250m+: Firavun'un Fırtınası / Duat Kalbi (Derin mor-altın mistik kum fırtınası)
            new WeatherStage(
                1250f,
                new Color(0.070f, 0.038f, 0.138f, 1f),
                new Color(0.84f, 0.72f, 1.00f, 1f),
                new Color(0.12f, 0.07f, 0.20f, 0.98f),
                new Color(0.24f, 0.14f, 0.38f, 0.24f),
                new Color(0.82f, 0.70f, 1.00f, 0.46f),
                56f,
                18.5f,
                new Color(1.00f, 0.84f, 0.36f, 0.78f),
                38f,
                new Color(0.08f, 0.03f, 0.14f, 1f),
                0.42f,
                0.96f)
        };

        [Header("Core References")]
        [SerializeField] private EndlessChunkDirector chunkDirector;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private Transform cameraFollowAnchor;
        [SerializeField] private SpriteRenderer weatherHazeRenderer;
        [SerializeField] private SpriteRenderer[] panoramaRenderers;
        [SerializeField] private SpriteRenderer[] duneRenderers;

        [Header("Weather Particle Systems")]
        [SerializeField] private ParticleSystem sandWindParticles;
        [SerializeField] private ParticleSystem ambientMoteParticles;

        [Header("Transition Tuning")]
        [SerializeField] private float blendBandMeters = 75f;

        private Vignette _vignette;
        private Bloom _bloom;
        private float _smoothedDistance;

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (postProcessVolume != null && postProcessVolume.sharedProfile != null)
            {
                postProcessVolume.sharedProfile.TryGet(out _vignette);
                postProcessVolume.sharedProfile.TryGet(out _bloom);
            }
            ApplyWeatherForDistance(0f);
        }

        private void OnEnable()
        {
            RunnerMotor2D.OnAnyRunnerRespawned += HandleWorldReset;
        }

        private void OnDisable()
        {
            RunnerMotor2D.OnAnyRunnerRespawned -= HandleWorldReset;
        }

        private void HandleWorldReset()
        {
            _smoothedDistance = 0f;
            ApplyWeatherForDistance(0f);
        }

        private void LateUpdate()
        {
            if (cameraFollowAnchor != null && targetCamera != null)
            {
                Vector3 camPos = targetCamera.transform.position;
                cameraFollowAnchor.position = new Vector3(camPos.x, camPos.y, 0f);
            }

            float rawDistance = chunkDirector != null ? chunkDirector.DistanceTraveledMeters : 0f;
            _smoothedDistance = Mathf.MoveTowards(_smoothedDistance, rawDistance, Mathf.Max(25f, Time.deltaTime * 45f));
            ApplyWeatherForDistance(_smoothedDistance);
        }

        private void ApplyWeatherForDistance(float distMeters)
        {
            EvaluateBlendedStage(
                distMeters,
                out Color skyColor,
                out Color panoTint,
                out Color duneTint,
                out Color hazeTint,
                out Color windColor,
                out float windRate,
                out float windSpeed,
                out Color moteColor,
                out float moteRate,
                out Color vigColor,
                out float vigIntensity,
                out float bloomIntensity);

            if (targetCamera != null)
            {
                targetCamera.backgroundColor = skyColor;
            }

            if (panoramaRenderers != null)
            {
                for (int i = 0; i < panoramaRenderers.Length; i++)
                {
                    if (panoramaRenderers[i] != null) panoramaRenderers[i].color = panoTint;
                }
            }

            if (duneRenderers != null)
            {
                for (int i = 0; i < duneRenderers.Length; i++)
                {
                    if (duneRenderers[i] != null) duneRenderers[i].color = duneTint;
                }
            }

            if (weatherHazeRenderer != null)
            {
                weatherHazeRenderer.color = hazeTint;
            }

            if (sandWindParticles != null)
            {
                var main = sandWindParticles.main;
                main.startColor = windColor;
                main.startSpeed = windSpeed;
                var emission = sandWindParticles.emission;
                emission.rateOverTime = windRate;
            }

            if (ambientMoteParticles != null)
            {
                var main = ambientMoteParticles.main;
                main.startColor = moteColor;
                var emission = ambientMoteParticles.emission;
                emission.rateOverTime = moteRate;
            }

            if (_vignette != null)
            {
                _vignette.color.Override(vigColor);
                _vignette.intensity.Override(vigIntensity);
            }

            if (_bloom != null)
            {
                _bloom.intensity.Override(bloomIntensity);
            }
        }

        private void EvaluateBlendedStage(
            float distMeters,
            out Color skyColor,
            out Color panoTint,
            out Color duneTint,
            out Color hazeTint,
            out Color windColor,
            out float windRate,
            out float windSpeed,
            out Color moteColor,
            out float moteRate,
            out Color vigColor,
            out float vigIntensity,
            out float bloomIntensity)
        {
            int idxA = 0;
            int idxB = 0;
            float blendT = 0f;
            float halfBand = Mathf.Max(10f, blendBandMeters * 0.5f);

            for (int i = 1; i < Stages.Length; i++)
            {
                float boundary = Stages[i].StartMeters;
                if (distMeters >= boundary + halfBand)
                {
                    idxA = i;
                    idxB = i;
                    blendT = 0f;
                }
                else if (distMeters > boundary - halfBand)
                {
                    idxA = i - 1;
                    idxB = i;
                    float raw = Mathf.InverseLerp(boundary - halfBand, boundary + halfBand, distMeters);
                    blendT = raw * raw * (3f - 2f * raw); // Yumuşak SmoothStep eğrisi
                    break;
                }
                else
                {
                    break;
                }
            }

            WeatherStage a = Stages[idxA];
            WeatherStage b = Stages[idxB];

            skyColor = Color.Lerp(a.SkyColor, b.SkyColor, blendT);
            panoTint = Color.Lerp(a.PanoramaTint, b.PanoramaTint, blendT);
            duneTint = Color.Lerp(a.DuneTint, b.DuneTint, blendT);
            hazeTint = Color.Lerp(a.HazeTint, b.HazeTint, blendT);
            windColor = Color.Lerp(a.WindColor, b.WindColor, blendT);
            windRate = Mathf.Lerp(a.WindRate, b.WindRate, blendT);
            windSpeed = Mathf.Lerp(a.WindSpeed, b.WindSpeed, blendT);
            moteColor = Color.Lerp(a.MoteColor, b.MoteColor, blendT);
            moteRate = Mathf.Lerp(a.MoteRate, b.MoteRate, blendT);
            vigColor = Color.Lerp(a.VignetteColor, b.VignetteColor, blendT);
            vigIntensity = Mathf.Lerp(a.VignetteIntensity, b.VignetteIntensity, blendT);
            bloomIntensity = Mathf.Lerp(a.BloomIntensity, b.BloomIntensity, blendT);
        }
    }
}
