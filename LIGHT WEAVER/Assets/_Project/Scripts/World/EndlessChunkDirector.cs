using System;
using System.Collections.Generic;
using LightWeaver.Player;
using LightWeaver.Pooling;
using UnityEngine;

namespace LightWeaver.World
{
    /// <summary>
    /// 8 farklı sistemik chunk örüntüsü (Prism Lock Gate, Umbra Inversion Köprüsü, Photon Anchor
    /// Kanca Uçurumu, Kırılgan Cam Koşusu, Boost Rampa, Sıçrama Çatalı, Prizma Köprü, Tehlike Koridoru)
    /// üreten ve geride kalanları ObjectPooler'a iade eden bölüm yönetmeni.
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
        public const string PoolPrismGate = "PrismLockGate";
        public const string PoolPhotonAnchor = "PhotonAnchor";

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
        [SerializeField] private GameObject prismGatePrefab;
        [SerializeField] private GameObject photonAnchorPrefab;

        [Header("5 Sector Profiles (Level 1..5)")]
        [SerializeField] private SectorProfileSO[] sectors;
        [SerializeField] private int startingSectorIndex = 0;

        [Header("Streaming Window")]
        [SerializeField] private float spawnHorizonAhead = 46f;
        [SerializeField] private float despawnBehindDistance = 18f;
        [SerializeField] private float platformThickness = 0.52f;

        private readonly List<GameObject> _activeObjects = new List<GameObject>(160);
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
            if (crystalPlatformPrefab != null) pooler.RegisterPool(PoolCrystalPlatform, crystalPlatformPrefab, 48);
            if (energyOrbPrefab != null) pooler.RegisterPool(PoolEnergyOrb, energyOrbPrefab, 24);
            if (energyOrbSuperPrefab != null) pooler.RegisterPool(PoolEnergyOrbSuper, energyOrbSuperPrefab, 12);
            if (jumpPadPrefab != null) pooler.RegisterPool(PoolJumpPad, jumpPadPrefab, 8);
            if (prismRelayPrefab != null) pooler.RegisterPool(PoolPrismRelay, prismRelayPrefab, 6);
            if (shadowPillarPrefab != null) pooler.RegisterPool(PoolShadowPillar, shadowPillarPrefab, 8);
            if (umbraStalkerPrefab != null) pooler.RegisterPool(PoolUmbraStalker, umbraStalkerPrefab, 10);
            if (voidLeechPrefab != null) pooler.RegisterPool(PoolVoidLeech, voidLeechPrefab, 8);
            if (prismGatePrefab != null) pooler.RegisterPool(PoolPrismGate, prismGatePrefab, 8);
            if (photonAnchorPrefab != null) pooler.RegisterPool(PoolPhotonAnchor, photonAnchorPrefab, 8);
        }

        public void SetStartingSector(int sectorIdx)
        {
            if (sectors == null || sectors.Length == 0) return;
            startingSectorIndex = Mathf.Clamp(sectorIdx, 0, sectors.Length - 1);
            ResetWorldFromStart();
        }

        public void SpawnReviveSafetyBridge(float centerX, float topY)
        {
            if (pooler == null) return;

            // Yeniden doğuş noktası civarındaki tüm aktif nesneleri ve ileriyi temizleyip sıfırdan güvenli köprü kur
            for (int i = _activeObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = _activeObjects[i];
                if (obj != null && obj.transform.position.x >= centerX - 6f)
                {
                    pooler.Despawn(obj);
                    _activeObjects.RemoveAt(i);
                }
            }

            float width = 9.5f;
            float startX = centerX - 2.2f;
            Vector3 pos = new Vector3(startX + width * 0.5f, topY - platformThickness * 0.5f, 0f);
            GameObject go = pooler.Spawn(PoolCrystalPlatform, pos, Quaternion.identity);
            if (go != null)
            {
                go.transform.localScale = new Vector3(width, platformThickness, 1f);
                if (go.TryGetComponent(out CrystalPlatform plat))
                {
                    plat.OnSpawnedFromPool();
                    plat.ForceGraceSolid(2.4f);
                }
                _activeObjects.Add(go);
            }

            _cursorX = startX + width;
            _cursorTopY = topY;
            FillHorizon();
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
            SpawnCrystal(_cursorX, _cursorTopY, 5.4f, CrystalVariant.Standard);
            _cursorX += 5.2f;
            SpawnOrb(_cursorX + 1.8f, _cursorTopY + 1.7f, false);
            SpawnSlopedCrystal(_cursorX, _cursorTopY, 4.4f, 20f, CrystalVariant.Standard, out float endX, out float endTopY);
            _cursorX = endX;
            _cursorTopY = endTopY;
        }

        private void SpawnNextChunk()
        {
            _chunkSequenceIndex++;
            int pattern = PickNextPattern();

            switch (pattern)
            {
                case 0: SpawnChunkFlatBoost(); break;
                case 1: SpawnChunkClimbRamp(); break;
                case 2: SpawnChunkJumpCanopyFork(); break;
                case 3: SpawnChunkPrismGateBreach(); break;
                case 4: SpawnChunkUmbraInversion(); break;
                case 5: SpawnChunkPhotonAnchorAbyss(); break;
                case 6: SpawnChunkFragileSprint(); break;
                default: SpawnChunkHazardGauntlet(); break;
            }
        }

        private int PickNextPattern()
        {
            // Yükseklik kontrolü
            if (_cursorTopY > 3.6f) return 0; // Aşağı adım
            if (_cursorTopY < -1.8f) return 1; // Yukarı tırmanış

            int sectorNum = _activeSector != null ? _activeSector.sectorNumber : 1;

            // İlk birkaç chunk sonrası zorunlu yön değiştirme mekaniklerini yoğunlaştır!
            if (_chunkSequenceIndex % 6 == 2) return 3; // PrismLockGate (Yukarı kilidi vur!)
            if (_chunkSequenceIndex % 6 == 4) return 4; // UmbraBridge (Işığı kaçır yoksa düşersin!)
            if (_chunkSequenceIndex % 6 == 5 && sectorNum >= 1) return 5; // PhotonAnchorAbyss (Işık kancasıyla uçurum aş!)

            int[] cycle = sectorNum >= 2
                ? new[] { 0, 1, 2, 3, 4, 5, 6, 7 }
                : new[] { 0, 1, 2, 3, 4, 5, 7 };
            return cycle[(_chunkSequenceIndex + UnityEngine.Random.Range(0, 3)) % cycle.Length];
        }

        private void SpawnChunkFlatBoost()
        {
            SpawnCrystal(_cursorX - 0.15f, _cursorTopY, 4.2f, CrystalVariant.Standard);
            float boostX = _cursorX + 4.0f + 1.35f;
            float boostY = _cursorTopY - 0.55f;
            SpawnCrystal(boostX, boostY, 3.6f, CrystalVariant.KineticBoost); // Hız akımı!
            SpawnOrb(boostX + 2.0f, boostY + 2.2f, false);

            _cursorX = boostX + 3.6f;
            _cursorTopY = boostY;
        }

        private void SpawnChunkClimbRamp()
        {
            SpawnCrystal(_cursorX - 0.15f, _cursorTopY, 3.0f, CrystalVariant.Standard);
            float rampStartX = _cursorX + 2.75f;
            SpawnSlopedCrystal(rampStartX, _cursorTopY, 4.8f, 30f, CrystalVariant.Standard, out float endX, out float endTopY);
            SpawnOrb((rampStartX + endX) * 0.5f, (_cursorTopY + endTopY) * 0.5f + 1.6f, false);

            SpawnCrystal(endX - 0.15f, endTopY - 0.08f, 3.6f, CrystalVariant.KineticBoost);
            _cursorX = endX + 3.45f;
            _cursorTopY = endTopY - 0.08f;
        }

        private void SpawnChunkJumpCanopyFork()
        {
            SpawnCrystal(_cursorX - 0.15f, _cursorTopY, 3.8f, CrystalVariant.Standard);
            SpawnJumpPad(_cursorX + 3.1f, _cursorTopY + 0.24f, new Vector2(8.6f, 13.5f));

            float canopyX = _cursorX + 6.4f;
            float canopyY = _cursorTopY + 2.65f;
            SpawnOrb(_cursorX + 5.1f, _cursorTopY + 3.2f, true);
            SpawnCrystal(canopyX, canopyY, 4.8f, CrystalVariant.KineticBoost);

            float lowX = _cursorX + 4.8f;
            float lowY = _cursorTopY - 2.35f;
            SpawnCrystal(lowX, lowY, 4.4f, CrystalVariant.Standard);
            SpawnOrb(lowX + 2.1f, lowY + 1.45f, false);
            SpawnSlopedCrystal(lowX + 4.25f, lowY, 4.4f, 24f, CrystalVariant.Standard, out float lowEndX, out float lowEndY);

            float mergeX = Mathf.Max(canopyX + 5.5f, lowEndX + 1.1f);
            float mergeY = lowEndY - 0.45f;
            SpawnCrystal(mergeX, mergeY, 4.6f, CrystalVariant.Standard);

            _cursorX = mergeX + 4.6f;
            _cursorTopY = mergeY;
        }

        private void SpawnChunkPrismGateBreach()
        {
            // Yolu kesen Elmas Kilitli Bariyer -> Feneri YUKARI kilide tutmadan geçersen çarpıp sendelersin!
            SpawnCrystal(_cursorX - 0.15f, _cursorTopY, 7.6f, CrystalVariant.Standard);
            SpawnPrismGate(_cursorX + 5.6f, _cursorTopY + 1.65f);
            SpawnOrb(_cursorX + 6.6f, _cursorTopY + 2.6f, true);

            _cursorX += 7.45f;
        }

        private void SpawnChunkUmbraInversion()
        {
            // TERS KURAL: Feneri aşağıda tutmaya devam edersen Umbra Köprüsü ERİR! Işığı yukarı kaldırarak geç!
            SpawnCrystal(_cursorX - 0.15f, _cursorTopY, 3.4f, CrystalVariant.Standard);
            float umbraX = _cursorX + 3.35f;
            SpawnCrystal(umbraX, _cursorTopY, 5.4f, CrystalVariant.UmbraBridge);
            SpawnOrb(umbraX + 2.6f, _cursorTopY + 2.8f, true); // Yukarı ışık tutarken yakalanacak yüksek batarya!

            float exitX = umbraX + 5.5f;
            SpawnCrystal(exitX, _cursorTopY, 3.6f, CrystalVariant.Standard);
            _cursorX = exitX + 3.6f;
        }

        private void SpawnChunkPhotonAnchorAbyss()
        {
            // DEV UÇURUM (Zemin yok!) -> Yukarıdaki PhotonAnchor'a fener tutarak ışık kancasıyla karşıya fırlat!
            SpawnCrystal(_cursorX - 0.15f, _cursorTopY, 3.8f, CrystalVariant.Standard);
            float anchorX = _cursorX + 5.8f;
            float anchorY = _cursorTopY + 3.8f;
            SpawnPhotonAnchor(anchorX, anchorY);
            SpawnOrb(anchorX + 2.4f, _cursorTopY + 2.9f, true);

            float landX = _cursorX + 10.6f;
            float landY = _cursorTopY - 0.2f;
            SpawnCrystal(landX, landY, 5.4f, CrystalVariant.KineticBoost);

            _cursorX = landX + 5.4f;
            _cursorTopY = landY;
        }

        private void SpawnChunkFragileSprint()
        {
            // Kırılgan Cam basamakları: basınca 0.24s sonra kırılır -> sürekli zıplama veya hız ister!
            SpawnCrystal(_cursorX - 0.15f, _cursorTopY, 3.0f, CrystalVariant.Standard);
            float x1 = _cursorX + 3.8f;
            float y1 = _cursorTopY - 0.35f;
            SpawnCrystal(x1, y1, 2.8f, CrystalVariant.FragileShard);
            SpawnOrb(x1 + 1.4f, y1 + 1.9f, false);

            float x2 = x1 + 4.1f;
            float y2 = y1 - 0.35f;
            SpawnCrystal(x2, y2, 2.8f, CrystalVariant.FragileShard);

            float x3 = x2 + 4.1f;
            float y3 = y2 - 0.35f;
            SpawnCrystal(x3, y3, 4.0f, CrystalVariant.Standard);

            _cursorX = x3 + 4.0f;
            _cursorTopY = y3;
        }

        private void SpawnChunkHazardGauntlet()
        {
            SpawnCrystal(_cursorX - 0.15f, _cursorTopY, 6.2f, CrystalVariant.Standard);

            float roll = UnityEngine.Random.value;
            if (roll < 0.35f)
            {
                SpawnHazard(_cursorX + 3.2f, _cursorTopY + 1.55f, ShadowHazardType.MovingShadowPillar);
            }
            else if (roll < 0.70f)
            {
                SpawnHazard(_cursorX + 3.1f, _cursorTopY + 2.4f, ShadowHazardType.VoidLeech);
                SpawnOrb(_cursorX + 4.8f, _cursorTopY + 1.15f, false);
            }
            else
            {
                SpawnHazard(_cursorX + 4.0f, _cursorTopY + 1.1f, ShadowHazardType.UmbraStalker);
            }

            float nextX = _cursorX + 7.2f;
            float nextY = _cursorTopY - 0.50f;
            SpawnCrystal(nextX, nextY, 4.2f, CrystalVariant.Standard);
            _cursorX = nextX + 4.2f;
            _cursorTopY = nextY;
        }

        private void SpawnCrystal(float startX, float topY, float width, CrystalVariant variant)
        {
            Vector3 pos = new Vector3(startX + width * 0.5f, topY - platformThickness * 0.5f, 0f);
            GameObject go = pooler.Spawn(PoolCrystalPlatform, pos, Quaternion.identity);
            if (go == null) return;
            go.transform.localScale = new Vector3(width, platformThickness, 1f);
            if (go.TryGetComponent(out CrystalPlatform plat))
            {
                plat.OnSpawnedFromPool();
                plat.SetVariant(variant);
            }
            _activeObjects.Add(go);
        }

        private void SpawnSlopedCrystal(float startX, float startTopY, float length, float angleDeg, CrystalVariant variant, out float endX, out float endTopY)
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
            if (go.TryGetComponent(out CrystalPlatform plat))
            {
                plat.OnSpawnedFromPool();
                plat.SetVariant(variant);
            }
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

        private void SpawnPrismGate(float x, float y)
        {
            GameObject go = pooler.Spawn(PoolPrismGate, new Vector3(x, y, 0f), Quaternion.identity);
            if (go != null) _activeObjects.Add(go);
        }

        private void SpawnPhotonAnchor(float x, float y)
        {
            GameObject go = pooler.Spawn(PoolPhotonAnchor, new Vector3(x, y, 0f), Quaternion.identity);
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
