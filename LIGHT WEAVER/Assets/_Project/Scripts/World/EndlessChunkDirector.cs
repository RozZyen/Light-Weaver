using System;
using System.Collections.Generic;
using LightWeaver.Player;
using LightWeaver.Pooling;
using UnityEngine;

namespace LightWeaver.World
{
    /// <summary>
    /// 5 Sektör (Level 1..5, The Echo Void dahil) boyunca balistik olarak doğrulanmış
    /// 3 irtifa katmanlı kristal yolları, bataryaları, sıçrama halkalarını, prizmaları ve gölge engellerini
    /// ObjectPooler üzerinden Zero-GC olarak üreten ve geride kalanları geri dönüştüren sonsuz bölüm yönetmeni.
    /// </summary>
    public sealed class EndlessChunkDirector : MonoBehaviour
    {
        public const string PoolCrystalPlatform = "CrystalPlatform";
        public const string PoolEnergyOrb = "EnergyOrb";
        public const string PoolEnergyOrbSuper = "EnergyOrbSuper";
        public const string PoolJumpPad = "PrismaticJumpPad";
        public const string PoolPrismRelay = "PrismRelay";
        public const string PoolShadowPillar = "ShadowPillar";
        public const string PoolUmbraStalker = "UmbraStalker";
        public const string PoolVoidLeech = "VoidLeech";

        public event Action<SectorProfileSO> OnSectorChanged;

        [Header("Core References")]
        [SerializeField] private ObjectPooler pooler;
        [SerializeField] private Transform runnerTransform;
        [SerializeField] private RunnerMotor2D runnerMotor;
        [SerializeField] private BatterySystem batterySystem;
        [SerializeField] private Camera mainCamera;

        [Header("Prefab Templates (Pooled)")]
        [SerializeField] private GameObject crystalPlatformPrefab;
        [SerializeField] private GameObject energyOrbPrefab;
        [SerializeField] private GameObject energyOrbSuperPrefab;
        [SerializeField] private GameObject jumpPadPrefab;
        [SerializeField] private GameObject prismRelayPrefab;
        [SerializeField] private GameObject shadowPillarPrefab;
        [SerializeField] private GameObject umbraStalkerPrefab;
        [SerializeField] private GameObject voidLeechPrefab;

        [Header("5 Sector Profiles (Level 1..5)")]
        [SerializeField] private SectorProfileSO[] sectors;
        [SerializeField] private int startingSectorIndex = 0;

        [Header("Streaming Window")]
        [SerializeField] private float spawnHorizonAhead = 42f;
        [SerializeField] private float despawnBehindDistance = 18f;
        [SerializeField] private float platformThickness = 0.52f;

        private readonly List<GameObject> _activeObjects = new List<GameObject>(128);
        private float _cursorX;
        private float _cursorTopY;
        private float _runStartX;
        private int _currentSectorIndex;
        private int _chunkSequenceIndex;
        private SectorProfileSO _activeSector;

        public SectorProfileSO ActiveSector => _activeSector;
        public float DistanceTraveledMeters => runnerTransform != null ? Mathf.Max(0f, runnerTransform.position.x - _runStartX) : 0f;

        private void Awake()
        {
            if (pooler == null) pooler = ObjectPooler.Instance;
            if (mainCamera == null) mainCamera = Camera.main;
            RegisterAllPools();
        }

        private void Start()
        {
            if (runnerTransform == null && BatterySystem.ActiveInstance != null)
            {
                runnerTransform = BatterySystem.ActiveInstance.transform;
                runnerMotor = runnerTransform.GetComponent<RunnerMotor2D>();
                batterySystem = BatterySystem.ActiveInstance;
            }
            ResetWorldFromStart();
        }

        private void OnEnable()
        {
            RunnerMotor2D.OnAnyRunnerRespawned += ResetWorldFromStart;
        }

        private void OnDisable()
        {
            RunnerMotor2D.OnAnyRunnerRespawned -= ResetWorldFromStart;
        }

        private void RegisterAllPools()
        {
            if (pooler == null) return;
            if (crystalPlatformPrefab != null) pooler.RegisterPool(PoolCrystalPlatform, crystalPlatformPrefab, 42);
            if (energyOrbPrefab != null) pooler.RegisterPool(PoolEnergyOrb, energyOrbPrefab, 20);
            if (energyOrbSuperPrefab != null) pooler.RegisterPool(PoolEnergyOrbSuper, energyOrbSuperPrefab, 10);
            if (jumpPadPrefab != null) pooler.RegisterPool(PoolJumpPad, jumpPadPrefab, 8);
            if (prismRelayPrefab != null) pooler.RegisterPool(PoolPrismRelay, prismRelayPrefab, 6);
            if (shadowPillarPrefab != null) pooler.RegisterPool(PoolShadowPillar, shadowPillarPrefab, 8);
            if (umbraStalkerPrefab != null) pooler.RegisterPool(PoolUmbraStalker, umbraStalkerPrefab, 10);
            if (voidLeechPrefab != null) pooler.RegisterPool(PoolVoidLeech, voidLeechPrefab, 8);
        }

        public void SetStartingSector(int sectorIdx)
        {
            if (sectors == null || sectors.Length == 0) return;
            startingSectorIndex = Mathf.Clamp(sectorIdx, 0, sectors.Length - 1);
            ResetWorldFromStart();
        }

        public void ResetWorldFromStart()
        {
            if (pooler != null)
            {
                for (int i = 0; i < _activeObjects.Count; i++)
                {
                    if (_activeObjects[i] != null) pooler.Despawn(_activeObjects[i]);
                }
            }
            _activeObjects.Clear();

            _runStartX = runnerTransform != null ? runnerTransform.position.x : -4.0f;
            _cursorX = _runStartX - 2.0f;
            _cursorTopY = -1.34f;
            _chunkSequenceIndex = 0;

            ApplySector(startingSectorIndex);
            SpawnOpeningRunway();
            FillHorizon();
        }

        private void Update()
        {
            if (runnerTransform == null || pooler == null) return;

            UpdateCurrentSectorProgression();
            FillHorizon();
            RecycleBehindRunner();
        }

        private void UpdateCurrentSectorProgression()
        {
            if (sectors == null || sectors.Length == 0) return;

            float dist = DistanceTraveledMeters;
            float accum = 0f;
            int targetIndex = sectors.Length - 1;

            for (int i = 0; i < sectors.Length; i++)
            {
                if (sectors[i] == null) continue;
                accum += Mathf.Max(40f, sectors[i].sectorDistanceMeters);
                if (dist < accum || i == sectors.Length - 1)
                {
                    targetIndex = i;
                    break;
                }
            }

            int clampedIdx = Mathf.Max(startingSectorIndex, targetIndex);
            if (clampedIdx != _currentSectorIndex || _activeSector == null)
            {
                ApplySector(clampedIdx);
            }
        }

        private void ApplySector(int index)
        {
            if (sectors == null || sectors.Length == 0) return;
            _currentSectorIndex = Mathf.Clamp(index, 0, sectors.Length - 1);
            _activeSector = sectors[_currentSectorIndex];
            if (_activeSector == null) return;

            if (runnerMotor != null) runnerMotor.SetSectorSpeedMultiplier(_activeSector.runSpeedMultiplier);
            if (batterySystem != null) batterySystem.SetSectorDrainMultiplier(_activeSector.batteryDrainMultiplier);
            if (mainCamera != null) mainCamera.backgroundColor = _activeSector.voidBackgroundColor;

            OnSectorChanged?.Invoke(_activeSector);
        }

        private void FillHorizon()
        {
            if (runnerTransform == null) return;
            float targetX = runnerTransform.position.x + spawnHorizonAhead;
            int safetyIterations = 0;
            while (_cursorX < targetX && safetyIterations++ < 16)
            {
                SpawnNextChunk();
            }
        }

        private void RecycleBehindRunner()
        {
            float cullX = runnerTransform.position.x - despawnBehindDistance;
            for (int i = _activeObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = _activeObjects[i];
                if (obj == null)
                {
                    _activeObjects.RemoveAt(i);
                    continue;
                }

                if (obj.transform.position.x < cullX)
                {
                    pooler.Despawn(obj);
                    _activeObjects.RemoveAt(i);
                }
            }
        }

        private void SpawnOpeningRunway()
        {
            // İlk 9 metre kesintisiz düz başlangıç pisti + kolay rampa
            SpawnFlatCrystal(_cursorX, _cursorTopY, 5.4f);
            _cursorX += 5.2f;
            SpawnOrb(_cursorX + 1.8f, _cursorTopY + 1.7f, false);
            SpawnSlopedCrystal(_cursorX, _cursorTopY, 4.6f, 22f, out float endX, out float endTopY);
            _cursorX = endX;
            _cursorTopY = endTopY;
        }

        private void SpawnNextChunk()
        {
            _chunkSequenceIndex++;
            int pattern = PickNextPattern();

            switch (pattern)
            {
                case 0: SpawnChunkFlatPulse(); break;
                case 1: SpawnChunkClimbRamp(); break;
                case 2: SpawnChunkJumpCanopyFork(); break;
                case 3: SpawnChunkPrismBridge(); break;
                case 4: SpawnChunkHazardGauntlet(); break;
                default: SpawnChunkDescentGlide(); break;
            }
        }

        private int PickNextPattern()
        {
            // Yükseklik kontrolü — sonsuz dünyanın kameradan çok yukarı/aşağı kaçmasını önler
            if (_cursorTopY > 3.6f) return 5;  // Aşağı süzülme (DescentGlide)
            if (_cursorTopY < -1.8f) return 1; // Yukarı tırmanma (ClimbRamp)

            float hazardChance = _activeSector != null ? _activeSector.hazardFrequency : 0.25f;
            float prismChance = _activeSector != null ? _activeSector.prismRelayFrequency : 0.20f;
            float roll = UnityEngine.Random.value;

            if (roll < prismChance && _chunkSequenceIndex > 2) return 3;
            if (roll < prismChance + hazardChance && _chunkSequenceIndex > 1) return 4;

            int[] rhythmicCycle = { 0, 1, 2, 5 };
            return rhythmicCycle[(_chunkSequenceIndex + UnityEngine.Random.Range(0, 2)) % rhythmicCycle.Length];
        }

        private void SpawnChunkFlatPulse()
        {
            SpawnFlatCrystal(_cursorX - 0.15f, _cursorTopY, 4.4f);
            float midX = _cursorX + 2.2f;
            if (ShouldSpawnOrb()) SpawnOrb(midX, _cursorTopY + 1.85f, false);

            // 1.35m güvenli boşluk sonrası -0.55m alçak platform (balistik iniş garantili)
            float nextX = _cursorX + 4.25f + 1.35f;
            float nextY = _cursorTopY - 0.55f;
            SpawnFlatCrystal(nextX, nextY, 4.2f);

            if (ShouldSpawnHazard(0.55f))
            {
                SpawnHazard(nextX + 2.5f, nextY + 1.15f, ShadowHazardType.UmbraStalker);
            }

            _cursorX = nextX + 4.2f;
            _cursorTopY = nextY;
        }

        private void SpawnChunkClimbRamp()
        {
            // Kısa düz giriş + 30° kristal tırmanış rampası + pürüzsüz düz çıkış
            SpawnFlatCrystal(_cursorX - 0.15f, _cursorTopY, 3.0f);
            float rampStartX = _cursorX + 2.75f;
            SpawnSlopedCrystal(rampStartX, _cursorTopY, 4.8f, 30f, out float endX, out float endTopY);
            SpawnOrb((rampStartX + endX) * 0.5f, (_cursorTopY + endTopY) * 0.5f + 1.6f, false);

            SpawnFlatCrystal(endX - 0.15f, endTopY - 0.08f, 3.6f);
            _cursorX = endX + 3.45f;
            _cursorTopY = endTopY - 0.08f;
        }

        private void SpawnChunkJumpCanopyFork()
        {
            // 3 İrtifa Katmanlı Çatallanma: JumpPad -> Starlight Canopy vs Alt Abyss Edge
            SpawnFlatCrystal(_cursorX - 0.15f, _cursorTopY, 3.8f);
            SpawnJumpPad(_cursorX + 3.1f, _cursorTopY + 0.24f, new Vector2(8.5f, 13.4f));

            // Üst Canopy Rotası (+2.6m yüksek)
            float canopyX = _cursorX + 6.4f;
            float canopyY = _cursorTopY + 2.65f;
            SpawnOrb(_cursorX + 5.1f, _cursorTopY + 3.2f, true);
            SpawnFlatCrystal(canopyX, canopyY, 4.6f);

            // Alt Güvenli Rota (-2.4m aşağı, JumpPad kaçırılırsa kurtarma rotası)
            float lowX = _cursorX + 4.8f;
            float lowY = _cursorTopY - 2.35f;
            SpawnFlatCrystal(lowX, lowY, 4.4f);
            SpawnOrb(lowX + 2.1f, lowY + 1.45f, false);
            SpawnSlopedCrystal(lowX + 4.25f, lowY, 4.4f, 24f, out float lowEndX, out float lowEndY);

            // Her iki rota da ileri birleşim köprüsüne bağlanır
            float mergeX = Mathf.Max(canopyX + 5.4f, lowEndX + 1.1f);
            float mergeY = lowEndY - 0.45f;
            SpawnFlatCrystal(mergeX, mergeY, 4.8f);

            _cursorX = mergeX + 4.8f;
            _cursorTopY = mergeY;
        }

        private void SpawnChunkPrismBridge()
        {
            // Prizma Kırıcı Köprüsü
            SpawnFlatCrystal(_cursorX - 0.15f, _cursorTopY, 3.8f);
            SpawnPrismRelay(_cursorX + 3.2f, _cursorTopY + 2.4f);

            float bridgeX = _cursorX + 5.1f;
            float bridgeY = _cursorTopY - 0.50f;
            SpawnFlatCrystal(bridgeX, bridgeY, 5.2f);
            SpawnOrb(bridgeX + 2.5f, bridgeY + 2.45f, true);

            _cursorX = bridgeX + 5.2f;
            _cursorTopY = bridgeY;
        }

        private void SpawnChunkHazardGauntlet()
        {
            SpawnFlatCrystal(_cursorX - 0.15f, _cursorTopY, 5.8f);

            float hazardRoll = UnityEngine.Random.value;
            if (hazardRoll < 0.36f)
            {
                // Hareketli Gölge Sütunu (zamanlamalı geçiş / Flash-Burst dondurma)
                SpawnHazard(_cursorX + 3.1f, _cursorTopY + 1.55f, ShadowHazardType.MovingShadowPillar);
            }
            else if (hazardRoll < 0.72f)
            {
                // Void Leech: Yolun üstünde asılı durur -> oyuncu ışığı aşağı eğip yolu yakmalı ama leech'i aydınlatmamalı!
                SpawnHazard(_cursorX + 3.0f, _cursorTopY + 2.4f, ShadowHazardType.VoidLeech);
                SpawnOrb(_cursorX + 4.6f, _cursorTopY + 1.1f, false);
            }
            else
            {
                // İki Umbra Stalker: Feneri üzerine tutup eriterek +batarya al!
                SpawnHazard(_cursorX + 3.8f, _cursorTopY + 1.1f, ShadowHazardType.UmbraStalker);
            }

            float nextX = _cursorX + 6.9f;
            float nextY = _cursorTopY - 0.50f;
            SpawnFlatCrystal(nextX, nextY, 4.2f);
            _cursorX = nextX + 4.2f;
            _cursorTopY = nextY;
        }

        private void SpawnChunkDescentGlide()
        {
            SpawnSlopedCrystal(_cursorX - 0.15f, _cursorTopY, 5.0f, -15f, out float endX, out float endTopY);
            SpawnOrb((_cursorX + endX) * 0.5f, (_cursorTopY + endTopY) * 0.5f + 1.4f, false);
            SpawnFlatCrystal(endX - 0.15f, endTopY, 4.0f);

            _cursorX = endX + 3.85f;
            _cursorTopY = endTopY;
        }

        private bool ShouldSpawnOrb()
        {
            float chance = _activeSector != null ? _activeSector.orbDensity : 0.75f;
            return UnityEngine.Random.value <= chance;
        }

        private bool ShouldSpawnHazard(float scale)
        {
            float chance = _activeSector != null ? _activeSector.hazardFrequency * scale : 0.2f;
            return UnityEngine.Random.value <= chance;
        }

        private void SpawnFlatCrystal(float startX, float topY, float width)
        {
            Vector3 pos = new Vector3(startX + width * 0.5f, topY - platformThickness * 0.5f, 0f);
            GameObject go = pooler.Spawn(PoolCrystalPlatform, pos, Quaternion.identity);
            if (go == null) return;
            go.transform.localScale = new Vector3(width, platformThickness, 1f);
            if (go.TryGetComponent(out CrystalPlatform plat)) plat.OnSpawnedFromPool();
            _activeObjects.Add(go);
        }

        private void SpawnSlopedCrystal(float startX, float startTopY, float length, float angleDeg, out float endX, out float endTopY)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            float halfT = platformThickness * 0.5f;

            float cx = startX + (length * 0.5f) * cos + halfT * sin;
            float cy = startTopY + (length * 0.5f) * sin - halfT * cos;

            endX = startX + length * cos;
            endTopY = startTopY + length * sin;

            GameObject go = pooler.Spawn(PoolCrystalPlatform, new Vector3(cx, cy, 0f), Quaternion.Euler(0f, 0f, angleDeg));
            if (go == null) return;
            go.transform.localScale = new Vector3(length, platformThickness, 1f);
            if (go.TryGetComponent(out CrystalPlatform plat)) plat.OnSpawnedFromPool();
            _activeObjects.Add(go);
        }

        private void SpawnOrb(float x, float y, bool super)
        {
            string key = super ? PoolEnergyOrbSuper : PoolEnergyOrb;
            GameObject go = pooler.Spawn(key, new Vector3(x, y, 0f), Quaternion.identity);
            if (go != null) _activeObjects.Add(go);
        }

        private void SpawnJumpPad(float x, float y, Vector2 launchVel)
        {
            GameObject go = pooler.Spawn(PoolJumpPad, new Vector3(x, y, 0f), Quaternion.identity);
            if (go == null) return;
            if (go.TryGetComponent(out PrismaticJumpPad pad)) pad.SetLaunchVelocity(launchVel);
            _activeObjects.Add(go);
        }

        private void SpawnPrismRelay(float x, float y)
        {
            GameObject go = pooler.Spawn(PoolPrismRelay, new Vector3(x, y, 0f), Quaternion.identity);
            if (go != null) _activeObjects.Add(go);
        }

        private void SpawnHazard(float x, float y, ShadowHazardType type)
        {
            string key = type switch
            {
                ShadowHazardType.MovingShadowPillar => PoolShadowPillar,
                ShadowHazardType.VoidLeech => PoolVoidLeech,
                _ => PoolUmbraStalker
            };
            GameObject go = pooler.Spawn(key, new Vector3(x, y, 0f), Quaternion.identity);
            if (go == null) return;
            if (go.TryGetComponent(out ShadowHazard hazard)) hazard.ConfigureOrigin(new Vector3(x, y, 0f), type);
            _activeObjects.Add(go);
        }
    }
}
