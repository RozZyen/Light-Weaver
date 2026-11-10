using System.Collections.Generic;
using LightWeaver.Core;
using UnityEngine;

namespace LightWeaver.Player
{
    /// <summary>
    /// Fenerin volümetrik ışık konisi mesh'ini (Altın/Beyaz Çekirdek + Neon Siyan Huzme + Pürüzsüz Radyal Sönümleme)
    /// prosedürel olarak çizer ve LightReactiveRegistry üzerinden koni içindeki hedefleri 0 Byte GC ile aydınlatır.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class LightConeCaster2D : MonoBehaviour
    {
        [Header("Beam Geometry")]
        [SerializeField] private float standardRange = 12.5f;
        [SerializeField] private float focusedRange = 18.5f;
        [SerializeField] private float standardHalfAngleDeg = 22f;
        [SerializeField] private float focusedHalfAngleDeg = 8.5f;
        [SerializeField] private int arcSegments = 24;

        [Header("Visual Material & Texture")]
        [SerializeField] private Texture2D beamGradientTexture;
        [SerializeField] private Color beamTint = Color.white;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _beamMesh;

        private Vector3[] _vertices;
        private Vector2[] _uvs;
        private Color[] _colors;
        private int[] _triangles;

        private float _currentAimAngleDeg = -14f;
        private float _targetAimAngleDeg = -14f;
        private float _aimAngularVelocity;
        private bool _isFocusedBeam;
        private float _batteryEfficiency = 1.0f;
        private float _rangeMultiplier = 1.0f;
        private float _angleMultiplier = 1.0f;

        public float CurrentAimAngleDeg => _currentAimAngleDeg;
        public bool IsFocusedBeam => _isFocusedBeam;
        public float CurrentRange => (_isFocusedBeam ? focusedRange : standardRange) * _rangeMultiplier * Mathf.Lerp(0.45f, 1f, _batteryEfficiency);
        public float CurrentHalfAngleDeg => (_isFocusedBeam ? focusedHalfAngleDeg : standardHalfAngleDeg) * _angleMultiplier;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            InitializeProceduralMesh();
        }

        private void InitializeProceduralMesh()
        {
            int segmentCount = Mathf.Max(8, arcSegments);
            // Her dilim için ayrı merkez tepe noktası (UV.y = t sayesinde kenarlarda kusursuz açısal sönümleme!)
            int vertexCount = segmentCount * 2 + 1;

            _vertices = new Vector3[vertexCount];
            _uvs = new Vector2[vertexCount];
            _colors = new Color[vertexCount];
            _triangles = new int[segmentCount * 3];

            for (int i = 0; i < segmentCount; i++)
            {
                int originIdx = i;
                int outerA = segmentCount + i;
                int outerB = segmentCount + i + 1;
                int triIdx = i * 3;
                _triangles[triIdx] = originIdx;
                _triangles[triIdx + 1] = outerA;
                _triangles[triIdx + 2] = outerB;
            }

            _beamMesh = new Mesh
            {
                name = "LightWeaver_VolumetricConeMesh"
            };
            _beamMesh.MarkDynamic();
            _meshFilter.sharedMesh = _beamMesh;

            if (_meshRenderer.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    Material mat = new Material(shader)
                    {
                        name = "LightCone_VolumetricMat"
                    };
                    if (beamGradientTexture != null)
                    {
                        mat.mainTexture = beamGradientTexture;
                    }
                    _meshRenderer.sharedMaterial = mat;
                }
            }
            else if (beamGradientTexture != null)
            {
                _meshRenderer.sharedMaterial.mainTexture = beamGradientTexture;
            }

            _meshRenderer.sortingOrder = 15;
        }

        public void SetTargetAngle(float angleDeg, bool focused, bool snapImmediate = false)
        {
            _targetAimAngleDeg = Mathf.Clamp(angleDeg, -60f, 65f);
            _isFocusedBeam = focused;
            if (snapImmediate)
            {
                _currentAimAngleDeg = _targetAimAngleDeg;
                _aimAngularVelocity = 0f;
            }
        }

        public void SetBatteryEfficiency(float normalizedBattery)
        {
            _batteryEfficiency = Mathf.Clamp01(normalizedBattery);
        }

        public void SetSpectrumVisuals(float efficiency, float rangeMult, float angleMult, Color tint)
        {
            _batteryEfficiency = Mathf.Clamp01(efficiency);
            _rangeMultiplier = Mathf.Clamp(rangeMult, 0.4f, 1.8f);
            _angleMultiplier = Mathf.Clamp(angleMult, 0.5f, 1.6f);
            beamTint = tint;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _currentAimAngleDeg = Mathf.SmoothDampAngle(_currentAimAngleDeg, _targetAimAngleDeg, ref _aimAngularVelocity, 0.04f, 720f, dt);
            transform.rotation = Quaternion.Euler(0f, 0f, _currentAimAngleDeg);

            UpdateBeamMeshGeometry();
            ScanAndIlluminateTargets(dt);
        }

        private void UpdateBeamMeshGeometry()
        {
            int segmentCount = _triangles.Length / 3;
            float range = CurrentRange;
            float halfAngleRad = CurrentHalfAngleDeg * Mathf.Deg2Rad;
            Color tint = new Color(beamTint.r, beamTint.g, beamTint.b, beamTint.a * _batteryEfficiency);

            for (int i = 0; i < segmentCount; i++)
            {
                float tMid = (i + 0.5f) / segmentCount;
                _vertices[i] = Vector3.zero;
                _uvs[i] = new Vector2(0.02f, tMid);
                _colors[i] = tint;
            }

            for (int i = 0; i <= segmentCount; i++)
            {
                float t = (float)i / segmentCount;
                float angleRad = Mathf.Lerp(-halfAngleRad, halfAngleRad, t);
                int idx = segmentCount + i;

                _vertices[idx] = new Vector3(Mathf.Cos(angleRad) * range, Mathf.Sin(angleRad) * range, 0f);
                _uvs[idx] = new Vector2(0.98f, t);
                _colors[idx] = tint;
            }

            _beamMesh.vertices = _vertices;
            _beamMesh.uv = _uvs;
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
            float aimRad = _currentAimAngleDeg * Mathf.Deg2Rad;
            Vector2 beamDir = new Vector2(Mathf.Cos(aimRad), Mathf.Sin(aimRad));

            float maxDist = CurrentRange;
            float maxDistSq = maxDist * maxDist;
            float cosHalfAngle = Mathf.Cos((CurrentHalfAngleDeg + 3.0f) * Mathf.Deg2Rad);

            for (int i = 0; i < count; i++)
            {
                ILightReactive target = targets[i];
                if (target == null) continue;

                Vector2 samplePoint = target.GetClosestLightSamplePoint(origin, beamDir);
                Vector2 toTarget = samplePoint - origin;
                float distSq = toTarget.sqrMagnitude;
                if (distSq > maxDistSq) continue;

                float dist = Mathf.Sqrt(distSq);
                if (dist < 0.001f)
                {
                    target.OnLightExposure(origin, _batteryEfficiency, _isFocusedBeam, dt);
                    continue;
                }

                Vector2 dirToTarget = toTarget / dist;
                float dot = Vector2.Dot(beamDir, dirToTarget);

                if (dot >= cosHalfAngle)
                {
                    float distFactor = 1f - (dist / maxDist);
                    float angleFactor = Mathf.InverseLerp(cosHalfAngle, 1f, dot);
                    float intensity = Mathf.Clamp01((distFactor * 0.45f + angleFactor * 0.55f) * _batteryEfficiency);
                    target.OnLightExposure(origin, intensity, _isFocusedBeam, dt);
                }
            }
        }
    }
}
