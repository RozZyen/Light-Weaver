using System;
using System.Collections.Generic;
using LightWeaver.Core;
using UnityEngine;

namespace LightWeaver.Player
{
    /// <summary>
    /// Fenerin volümetrik ışık konisi mesh'ini (Altın Çekirdek + Neon Siyan Huzme) prosedürel olarak çizer
    /// ve LightReactiveRegistry üzerinden koni içindeki tüm kristal platformları / bataryaları
    /// 0 Byte GC tahsisiyle (Zero-GC) aydınlatır.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class LightConeCaster2D : MonoBehaviour
    {
        [Header("Beam Geometry")]
        [SerializeField] private float standardRange = 11.5f;
        [SerializeField] private float focusedRange = 17.5f;
        [SerializeField] private float standardHalfAngleDeg = 22f;
        [SerializeField] private float focusedHalfAngleDeg = 8.5f;
        [SerializeField] private int arcSegments = 20;

        [Header("Visual Colors (GDD 60-30-10 Palette)")]
        [SerializeField] private Color coreOriginColor = new Color(1.0f, 0.90f, 0.45f, 0.92f); // #FFE673 Solar Amber
        [SerializeField] private Color midCyanColor = new Color(0.0f, 0.94f, 1.0f, 0.55f);    // #00F0FF Neon Cyan
        [SerializeField] private Color outerEdgeColor = new Color(0.71f, 0.22f, 0.95f, 0.0f); // #B537F2 Prism Violet Fade

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _beamMesh;

        private Vector3[] _vertices;
        private Color[] _colors;
        private int[] _triangles;

        private float _currentAimAngleDeg;
        private float _targetAimAngleDeg;
        private float _aimAngularVelocity;
        private bool _isFocusedBeam;
        private float _batteryEfficiency = 1.0f;

        public float CurrentAimAngleDeg => _currentAimAngleDeg;
        public bool IsFocusedBeam => _isFocusedBeam;
        public float CurrentRange => (_isFocusedBeam ? focusedRange : standardRange) * Mathf.Lerp(0.55f, 1f, _batteryEfficiency);
        public float CurrentHalfAngleDeg => _isFocusedBeam ? focusedHalfAngleDeg : standardHalfAngleDeg;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            InitializeProceduralMesh();
        }

        private void InitializeProceduralMesh()
        {
            int segmentCount = Mathf.Max(6, arcSegments);
            int vertexCount = segmentCount + 2; // 0 = Origin, 1..N+1 = Arc vertices

            _vertices = new Vector3[vertexCount];
            _colors = new Color[vertexCount];
            _triangles = new int[segmentCount * 3];

            for (int i = 0; i < segmentCount; i++)
            {
                int triIdx = i * 3;
                _triangles[triIdx] = 0;
                _triangles[triIdx + 1] = i + 1;
                _triangles[triIdx + 2] = i + 2;
            }

            _beamMesh = new Mesh
            {
                name = "LightWeaver_VolumetricConeMesh"
            };
            _beamMesh.MarkDynamic();
            _meshFilter.sharedMesh = _beamMesh;

            // Varsayılan Sprites/Default materyal ile additive parlama görünümü
            if (_meshRenderer.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    Material mat = new Material(shader)
                    {
                        name = "LightCone_AdditiveMat"
                    };
                    _meshRenderer.sharedMaterial = mat;
                }
            }
            _meshRenderer.sortingOrder = 15;
        }

        public void SetTargetAngle(float angleDeg, bool focused)
        {
            _targetAimAngleDeg = Mathf.Clamp(angleDeg, -60f, 65f);
            _isFocusedBeam = focused;
        }

        public void SetBatteryEfficiency(float normalizedBattery)
        {
            _batteryEfficiency = Mathf.Clamp01(normalizedBattery);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _currentAimAngleDeg = Mathf.SmoothDampAngle(_currentAimAngleDeg, _targetAimAngleDeg, ref _aimAngularVelocity, 0.045f, 720f, dt);
            transform.localRotation = Quaternion.Euler(0f, 0f, _currentAimAngleDeg);

            UpdateBeamMeshGeometry();
            ScanAndIlluminateTargets(dt);
        }

        private void UpdateBeamMeshGeometry()
        {
            int segmentCount = _triangles.Length / 3;
            float range = CurrentRange;
            float halfAngleRad = CurrentHalfAngleDeg * Mathf.Deg2Rad;

            _vertices[0] = Vector3.zero;
            _colors[0] = coreOriginColor * _batteryEfficiency;

            for (int i = 0; i <= segmentCount; i++)
            {
                float t = (float)i / segmentCount; // 0..1
                float angleRad = Mathf.Lerp(-halfAngleRad, halfAngleRad, t);
                float edgeFactor = 1f - Mathf.Abs((t - 0.5f) * 2f); // Merkezde 1, kenarlarda 0

                // Merkez huzme biraz daha uzun, kenarlar yumuşak kavisli
                float localRadius = range * Mathf.Lerp(0.88f, 1.0f, edgeFactor);
                _vertices[i + 1] = new Vector3(Mathf.Cos(angleRad) * localRadius, Mathf.Sin(angleRad) * localRadius, 0f);

                Color arcCol = Color.Lerp(outerEdgeColor, midCyanColor, edgeFactor * edgeFactor);
                arcCol.a *= _batteryEfficiency * Mathf.Lerp(0.15f, 0.72f, edgeFactor);
                _colors[i + 1] = arcCol;
            }

            _beamMesh.vertices = _vertices;
            _beamMesh.colors = _colors;
            _beamMesh.triangles = _triangles;
            _beamMesh.RecalculateBounds();
        }

        private void ScanAndIlluminateTargets(float dt)
        {
            IReadOnlyList<ILightReactive> targets = LightReactiveRegistry.All;
            int count = targets.Count;
            if (count == 0) return;

            Vector2 origin = transform.position;
            float aimRad = transform.eulerAngles.z * Mathf.Deg2Rad;
            Vector2 beamDir = new Vector2(Mathf.Cos(aimRad), Mathf.Sin(aimRad));

            float maxDist = CurrentRange;
            float maxDistSq = maxDist * maxDist;
            float cosHalfAngle = Mathf.Cos((CurrentHalfAngleDeg + 4.5f) * Mathf.Deg2Rad); // +4.5° oyuncu dostu tolerans

            for (int i = 0; i < count; i++)
            {
                ILightReactive target = targets[i];
                if (target == null) continue;

                Vector2 toTarget = target.WorldPosition - origin;
                float distSq = toTarget.sqrMagnitude;
                if (distSq > maxDistSq || distSq < 0.001f) continue;

                float invDist = 1f / Mathf.Sqrt(distSq);
                Vector2 dirToTarget = toTarget * invDist;
                float dot = Vector2.Dot(beamDir, dirToTarget);

                if (dot >= cosHalfAngle)
                {
                    float distFactor = 1f - ((1f / invDist) / maxDist);
                    float angleFactor = Mathf.InverseLerp(cosHalfAngle, 1f, dot);
                    float intensity = Mathf.Clamp01((distFactor * 0.5f + angleFactor * 0.5f) * _batteryEfficiency);
                    target.OnLightExposure(origin, intensity, _isFocusedBeam, dt);
                }
            }
        }
    }
}
