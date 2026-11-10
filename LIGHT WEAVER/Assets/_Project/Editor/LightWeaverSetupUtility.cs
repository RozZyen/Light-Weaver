using System.IO;
using LightWeaver.Player;
using LightWeaver.Pooling;
using LightWeaver.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LightWeaver.EditorTools
{
    /// <summary>
    /// Light Weaver için katmanları, sıfır sürtünmeli fizik materyalini, prosedürel kristal/koşucu/batarya/prizma
    /// sprite'larını, prefab'ları ve oynanabilir sahneyi deterministik olarak inşa eder.
    /// </summary>
    public static class LightWeaverSetupUtility
    {
        private const string SpritesFolder = "Assets/_Project/Art/Sprites";
        private const string PrefabsFolder = "Assets/_Project/Prefabs";
        private const string PhysicsFolder = "Assets/_Project/Physics";

        private const int PlayerLayer = 6;
        private const int HardlightWorldLayer = 7;

        [MenuItem("LightWeaver/Build Playable Scene (Phase 1 - Phase 3)")]
        public static void BuildPlayableScene()
        {
            EnsureDirectories();
            EnsureProjectLayers();
            PhysicsMaterial2D zeroFrictionMat = CreateOrLoadZeroFrictionMaterial();

            // 1. Prosedürel Kristal, Koşucu & Faz 3 Sprite Üretimi
            Sprite crystalTileSprite = GenerateCrystalPlatformSprite(Path.Combine(SpritesFolder, "prism_crystal_block.png"));
            Sprite rimGlowSprite = GenerateCrystalRimSprite(Path.Combine(SpritesFolder, "prism_crystal_rim.png"));
            Sprite starburstSprite = GenerateStarburstSprite(Path.Combine(SpritesFolder, "diamond_starburst.png"));
            Sprite emitterCoreSprite = GenerateEmitterCoreSprite(Path.Combine(SpritesFolder, "flashlight_emitter_core.png"));

            Sprite runnerTorsoSprite = GenerateRunnerTorsoHeadSprite(Path.Combine(SpritesFolder, "runner_torso_head.png"));
            Sprite runnerLimbSprite = GenerateRunnerLimbSprite(Path.Combine(SpritesFolder, "runner_limb.png"));
            Sprite runnerSparkSprite = GenerateSoftDotSprite(Path.Combine(SpritesFolder, "runner_spark_dot.png"));
            Texture2D beamGradientTex = GenerateBeamGradientTexture(Path.Combine(SpritesFolder, "volumetric_beam_gradient.png"));

            // Faz 3 Sprite'ları
            Sprite hexFrameSprite = GenerateHexFrameSprite(Path.Combine(SpritesFolder, "energy_hex_frame.png"));
            Sprite lightningBoltSprite = GenerateLightningBoltSprite(Path.Combine(SpritesFolder, "energy_bolt_icon.png"));
            Sprite jumpRingSprite = GenerateJumpPadRingSprite(Path.Combine(SpritesFolder, "jump_pad_ring.png"));
            Sprite jumpCoreSprite = GenerateSoftDotSprite(Path.Combine(SpritesFolder, "jump_pad_core.png"));
            Sprite prismDiamondSprite = GeneratePrismRelayDiamondSprite(Path.Combine(SpritesFolder, "prism_relay_diamond.png"));
            Sprite solidBarSprite = GenerateSolidWhiteRectSprite(Path.Combine(SpritesFolder, "diegetic_bar_pixel.png"));

            AssetDatabase.Refresh();

            // 2. Kamera Kurulumu (#05070D Zifiri Karanlık Boşluk + CameraFollow2D)
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
            }
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5.8f;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.02f, 0.027f, 0.051f, 1f); // #05070D

            CameraFollow2D camFollow = mainCam.GetComponent<CameraFollow2D>();
            if (camFollow == null) camFollow = mainCam.gameObject.AddComponent<CameraFollow2D>();

            // Eski kökleri temizle
            DestroyExistingIfFound("=== LIGHT WEAVER SYSTEMS ===");
            DestroyExistingIfFound("=== PLAYER FLASHLIGHT RIG ===");
            DestroyExistingIfFound("=== LIGHT WEAVER RUNNER ===");
            DestroyExistingIfFound("=== PRISM CRYSTAL WORLD ===");
            DestroyExistingIfFound("=== VOID STARFIELD ===");

            // 3. Merkezi Sistemler & ObjectPooler
            GameObject systemsRoot = new GameObject("=== LIGHT WEAVER SYSTEMS ===");
            systemsRoot.AddComponent<ObjectPooler>();

            // 4. Neon Cyber-Runner, Pil Sistemi & Fener Rig Kurulumu
            GameObject runnerGo = BuildRunnerCharacter(
                mainCam,
                zeroFrictionMat,
                runnerTorsoSprite,
                runnerLimbSprite,
                emitterCoreSprite,
                runnerSparkSprite,
                solidBarSprite,
                beamGradientTex);

            SerializedObject camSo = new SerializedObject(camFollow);
            camSo.FindProperty("target").objectReferenceValue = runnerGo.transform;
            camSo.ApplyModifiedPropertiesWithoutUndo();
            mainCam.transform.position = new Vector3(runnerGo.transform.position.x + 4.6f, runnerGo.transform.position.y + 0.9f, -10f);

            PrefabUtility.SaveAsPrefabAsset(runnerGo, Path.Combine(PrefabsFolder, "PF_LightWeaverRunner.prefab"));

            // 5. İnce Uzay Tozu & Yıldız Noktaları
            BuildSubtleVoidStarfield(runnerSparkSprite);

            // 6. Faz 3 Kristal Parkur Dünyası, +ENERGY Hücreleri, Jump Pad'ler & Prizmalar
            BuildPhase3ParkourWorld(
                crystalTileSprite,
                rimGlowSprite,
                starburstSprite,
                hexFrameSprite,
                lightningBoltSprite,
                jumpRingSprite,
                jumpCoreSprite,
                prismDiamondSprite);

            // 7. Prefab Şablonlarını Kaydet
            SaveWorldPrefabs(
                crystalTileSprite,
                rimGlowSprite,
                starburstSprite,
                hexFrameSprite,
                lightningBoltSprite,
                jumpRingSprite,
                jumpCoreSprite,
                prismDiamondSprite);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[LightWeaver Setup] Phase 3 Complete: BatterySystem, +ENERGY Orbs, PrismaticJumpPads, PrismRelays & Seamless Ramp Course built!");
        }

        private static GameObject BuildRunnerCharacter(
            Camera mainCam,
            PhysicsMaterial2D zeroFrictionMat,
            Sprite torsoSprite,
            Sprite limbSprite,
            Sprite emitterCoreSprite,
            Sprite sparkSprite,
            Sprite barSprite,
            Texture2D beamGradientTex)
        {
            GameObject runner = new GameObject("=== LIGHT WEAVER RUNNER ===");
            runner.layer = PlayerLayer;
            runner.tag = "Player";
            runner.transform.position = new Vector3(-4.0f, -0.45f, 0f);

            Rigidbody2D rb = runner.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2.6f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            CapsuleCollider2D capsule = runner.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.54f, 1.44f);
            capsule.offset = new Vector2(0f, 0.02f);
            capsule.sharedMaterial = zeroFrictionMat;

            RunnerMotor2D motor = runner.AddComponent<RunnerMotor2D>();
            SerializedObject motorSo = new SerializedObject(motor);
            motorSo.FindProperty("groundLayerMask").intValue = 1 << HardlightWorldLayer;
            motorSo.ApplyModifiedPropertiesWithoutUndo();

            // Görsel Hiyerarşi
            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(runner.transform, false);

            Transform backArmPivot = CreateLimbPivot("BackArmPivot", visualRoot.transform, new Vector3(-0.08f, 0.25f, 0f), new Vector3(0.18f, 0.48f, 1f), new Vector3(0f, -0.20f, 0f), limbSprite, new Color(0.08f, 0.45f, 0.68f, 1f), 9);
            Transform backLegPivot = CreateLimbPivot("BackLegPivot", visualRoot.transform, new Vector3(-0.04f, -0.14f, 0f), new Vector3(0.24f, 0.56f, 1f), new Vector3(0f, -0.24f, 0f), limbSprite, new Color(0.08f, 0.48f, 0.72f, 1f), 9);

            GameObject bodyPivot = new GameObject("BodyPivot");
            bodyPivot.transform.SetParent(visualRoot.transform, false);
            GameObject torsoGo = new GameObject("TorsoHeadSprite");
            torsoGo.transform.SetParent(bodyPivot.transform, false);
            torsoGo.transform.localPosition = new Vector3(0f, 0.30f, 0f);
            torsoGo.transform.localScale = new Vector3(0.86f, 1.02f, 1f);
            SpriteRenderer torsoSr = torsoGo.AddComponent<SpriteRenderer>();
            torsoSr.sprite = torsoSprite;
            torsoSr.sortingOrder = 11;

            // Diegetik Sırt Pil Göstergesi (Dead Space tarzı enerji çekirdeği)
            GameObject gaugeBg = new GameObject("SuitBatteryGaugeBg");
            gaugeBg.transform.SetParent(bodyPivot.transform, false);
            gaugeBg.transform.localPosition = new Vector3(-0.24f, 0.24f, 0f);
            gaugeBg.transform.localScale = new Vector3(0.07f, 0.42f, 1f);
            SpriteRenderer gaugeBgSr = gaugeBg.AddComponent<SpriteRenderer>();
            gaugeBgSr.sprite = barSprite;
            gaugeBgSr.color = new Color(0.04f, 0.08f, 0.14f, 0.9f);
            gaugeBgSr.sortingOrder = 12;

            GameObject gaugeFillPivot = new GameObject("SuitBatteryGaugeFillPivot");
            gaugeFillPivot.transform.SetParent(gaugeBg.transform, false);
            gaugeFillPivot.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            GameObject gaugeFillBar = new GameObject("FillBar");
            gaugeFillBar.transform.SetParent(gaugeFillPivot.transform, false);
            gaugeFillBar.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            gaugeFillBar.transform.localScale = new Vector3(0.72f, 0.96f, 1f);
            SpriteRenderer gaugeFillSr = gaugeFillBar.AddComponent<SpriteRenderer>();
            gaugeFillSr.sprite = barSprite;
            gaugeFillSr.color = new Color(0f, 0.94f, 1f, 1f);
            gaugeFillSr.sortingOrder = 13;

            Transform frontLegPivot = CreateLimbPivot("FrontLegPivot", visualRoot.transform, new Vector3(0.06f, -0.14f, 0f), new Vector3(0.25f, 0.58f, 1f), new Vector3(0f, -0.25f, 0f), limbSprite, new Color(0.22f, 0.94f, 1f, 1f), 12);

            // Fener Kolu Pivotu + Emitter + LightConeCaster2D
            GameObject flashlightArmPivot = new GameObject("FlashlightArmPivot");
            flashlightArmPivot.transform.SetParent(visualRoot.transform, false);
            flashlightArmPivot.transform.localPosition = new Vector3(0.08f, 0.28f, 0f);

            GameObject frontArmSpriteObj = new GameObject("FrontArmSprite");
            frontArmSpriteObj.transform.SetParent(flashlightArmPivot.transform, false);
            frontArmSpriteObj.transform.localPosition = new Vector3(0.22f, 0f, 0f);
            frontArmSpriteObj.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            frontArmSpriteObj.transform.localScale = new Vector3(0.18f, 0.48f, 1f);
            SpriteRenderer frontArmSr = frontArmSpriteObj.AddComponent<SpriteRenderer>();
            frontArmSr.sprite = limbSprite;
            frontArmSr.color = new Color(0.32f, 0.98f, 1f, 1f);
            frontArmSr.sortingOrder = 13;

            GameObject emitterTip = new GameObject("FlashlightEmitterTip");
            emitterTip.transform.SetParent(flashlightArmPivot.transform, false);
            emitterTip.transform.localPosition = new Vector3(0.46f, 0f, 0f);

            SpriteRenderer emitterSr = emitterTip.AddComponent<SpriteRenderer>();
            emitterSr.sprite = emitterCoreSprite;
            emitterSr.color = new Color(1f, 0.94f, 0.65f, 0.92f);
            emitterSr.sortingOrder = 20;
            emitterTip.transform.localScale = new Vector3(0.32f, 0.32f, 1f);

            GameObject coneObj = new GameObject("LightConeCaster2D");
            coneObj.transform.SetParent(emitterTip.transform, false);
            LightConeCaster2D coneCaster = coneObj.AddComponent<LightConeCaster2D>();
            SerializedObject coneSo = new SerializedObject(coneCaster);
            coneSo.FindProperty("beamGradientTexture").objectReferenceValue = beamGradientTex;
            coneSo.ApplyModifiedPropertiesWithoutUndo();

            FlashlightController2D flashlightCtrl = flashlightArmPivot.AddComponent<FlashlightController2D>();
            SerializedObject ctrlSo = new SerializedObject(flashlightCtrl);
            ctrlSo.FindProperty("armPivot").objectReferenceValue = flashlightArmPivot.transform;
            ctrlSo.FindProperty("coneCaster").objectReferenceValue = coneCaster;
            ctrlSo.ApplyModifiedPropertiesWithoutUndo();

            // Pil Sistemi (Faz 3)
            BatterySystem batterySystem = runner.AddComponent<BatterySystem>();
            SerializedObject batSo = new SerializedObject(batterySystem);
            batSo.FindProperty("flashlightController").objectReferenceValue = flashlightCtrl;
            batSo.FindProperty("runnerMotor").objectReferenceValue = motor;
            batSo.ApplyModifiedPropertiesWithoutUndo();

            LightTouchInput touchInput = runner.AddComponent<LightTouchInput>();
            SerializedObject inputSo = new SerializedObject(touchInput);
            inputSo.FindProperty("flashlightController").objectReferenceValue = flashlightCtrl;
            inputSo.FindProperty("targetCamera").objectReferenceValue = mainCam;
            inputSo.ApplyModifiedPropertiesWithoutUndo();

            // Ayak Altı Kıvılcım Partikülü
            GameObject sparkObj = new GameObject("FootstepSparks");
            sparkObj.transform.SetParent(visualRoot.transform, false);
            sparkObj.transform.localPosition = new Vector3(-0.08f, -0.68f, 0f);
            ParticleSystem ps = CreateFootstepParticleSystem(sparkObj, sparkSprite);

            // Hız İzi
            GameObject trailObj = new GameObject("SpeedTrail");
            trailObj.transform.SetParent(bodyPivot.transform, false);
            trailObj.transform.localPosition = new Vector3(-0.12f, 0.25f, 0f);
            TrailRenderer trail = CreateRunnerSpeedTrail(trailObj);

            // Prosedürel Koşu Animatörü
            RunnerProceduralAnimator procAnim = runner.AddComponent<RunnerProceduralAnimator>();
            SerializedObject animSo = new SerializedObject(procAnim);
            animSo.FindProperty("motor").objectReferenceValue = motor;
            animSo.FindProperty("batterySystem").objectReferenceValue = batterySystem;
            animSo.FindProperty("visualRoot").objectReferenceValue = visualRoot.transform;
            animSo.FindProperty("bodyPivot").objectReferenceValue = bodyPivot.transform;
            animSo.FindProperty("frontLegPivot").objectReferenceValue = frontLegPivot;
            animSo.FindProperty("backLegPivot").objectReferenceValue = backLegPivot;
            animSo.FindProperty("backArmPivot").objectReferenceValue = backArmPivot;
            animSo.FindProperty("suitBatteryGaugeFill").objectReferenceValue = gaugeFillPivot.transform;
            animSo.FindProperty("suitBatteryGaugeRenderer").objectReferenceValue = gaugeFillSr;
            animSo.FindProperty("footstepSparks").objectReferenceValue = ps;
            animSo.FindProperty("speedTrail").objectReferenceValue = trail;
            animSo.ApplyModifiedPropertiesWithoutUndo();

            return runner;
        }

        private static Transform CreateLimbPivot(
            string name,
            Transform parent,
            Vector3 pivotLocalPos,
            Vector3 spriteScale,
            Vector3 spriteOffset,
            Sprite sprite,
            Color tint,
            int sortingOrder)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = pivotLocalPos;

            GameObject spriteObj = new GameObject("Sprite");
            spriteObj.transform.SetParent(pivot.transform, false);
            spriteObj.transform.localPosition = spriteOffset;
            spriteObj.transform.localScale = spriteScale;
            SpriteRenderer sr = spriteObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tint;
            sr.sortingOrder = sortingOrder;

            return pivot.transform;
        }

        private static ParticleSystem CreateFootstepParticleSystem(GameObject target, Sprite sparkSprite)
        {
            ParticleSystem ps = target.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.28f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 3.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new Color(0.25f, 0.96f, 1.0f, 0.95f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.6f;
            main.maxParticles = 40;

            var emission = ps.emission;
            emission.rateOverTime = 26f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 28f;
            shape.rotation = new Vector3(0f, 0f, 135f);

            var renderer = target.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 14;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                Material mat = new Material(shader) { mainTexture = sparkSprite.texture };
                renderer.sharedMaterial = mat;
            }
            return ps;
        }

        private static TrailRenderer CreateRunnerSpeedTrail(GameObject target)
        {
            TrailRenderer tr = target.AddComponent<TrailRenderer>();
            tr.time = 0.26f;
            tr.startWidth = 0.28f;
            tr.endWidth = 0.02f;
            tr.minVertexDistance = 0.08f;
            tr.sortingOrder = 8;

            Gradient grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.0f, 0.94f, 1.0f), 0f),
                    new GradientColorKey(new Color(1.0f, 0.65f, 0.12f), 0.55f),
                    new GradientColorKey(new Color(0.71f, 0.22f, 0.95f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.65f, 0f),
                    new GradientAlphaKey(0.30f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                });
            tr.colorGradient = grad;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) tr.sharedMaterial = new Material(shader);
            return tr;
        }

        private static void BuildSubtleVoidStarfield(Sprite dotSprite)
        {
            GameObject starRoot = new GameObject("=== VOID STARFIELD ===");
            Random.InitState(42);
            for (int i = 0; i < 95; i++)
            {
                GameObject star = new GameObject($"Star_{i:00}");
                star.transform.SetParent(starRoot.transform, false);
                star.transform.position = new Vector3(Random.Range(-12f, 95f), Random.Range(-7f, 14f), 2f);
                float scale = Random.Range(0.03f, 0.09f);
                star.transform.localScale = new Vector3(scale, scale, 1f);

                SpriteRenderer sr = star.AddComponent<SpriteRenderer>();
                sr.sprite = dotSprite;
                sr.color = new Color(0.45f, 0.85f, 1f, Random.Range(0.08f, 0.24f));
                sr.sortingOrder = -10;
            }
        }

        private static void BuildPhase3ParkourWorld(
            Sprite bodySprite,
            Sprite rimSprite,
            Sprite starSprite,
            Sprite hexFrameSprite,
            Sprite boltSprite,
            Sprite jumpRingSprite,
            Sprite jumpCoreSprite,
            Sprite prismSprite)
        {
            GameObject worldRoot = new GameObject("=== PRISM CRYSTAL WORLD ===");

            // BÖLGE 1: Kusursuz Başlangıç & 30° Rampa Tırmanışı (Boşluksuz / Örtüşen Eklemeler)
            CreateCrystalPlatformInstance("Crystal_01_LaunchA", worldRoot.transform, new Vector3(-2.4f, -1.6f, 0f), new Vector2(4.4f, 0.52f), 0f, bodySprite, rimSprite, starSprite);
            CreateCrystalPlatformInstance("Crystal_02_LaunchB", worldRoot.transform, new Vector3(1.6f, -1.6f, 0f), new Vector2(3.6f, 0.52f), 0f, bodySprite, rimSprite, starSprite);
            CreateCrystalPlatformInstance("Crystal_03_Bridge", worldRoot.transform, new Vector3(5.0f, -1.25f, 0f), new Vector2(3.4f, 0.50f), 12f, bodySprite, rimSprite, starSprite);
            CreateCrystalPlatformInstance("Crystal_04_RampMain", worldRoot.transform, new Vector3(8.6f, 0.25f, 0f), new Vector2(4.8f, 0.50f), 30f, bodySprite, rimSprite, starSprite);

            // Rampa üstüne uçan +ENERGY bataryası
            CreateEnergyOrbInstance("Orb_01_Ramp", worldRoot.transform, new Vector3(8.5f, 2.1f, 0f), false, hexFrameSprite, boltSprite);

            // BÖLGE 2: Orta Platform + Jump Pad (Üst Canopy vs Alt Abyss Seçimi)
            CreateCrystalPlatformInstance("Crystal_05_MidPad", worldRoot.transform, new Vector3(12.8f, 1.30f, 0f), new Vector2(3.8f, 0.50f), 0f, bodySprite, rimSprite, starSprite);
            CreateJumpPadInstance("JumpPad_01_ToCanopy", worldRoot.transform, new Vector3(14.0f, 1.82f, 0f), new Vector2(8.2f, 13.0f), jumpRingSprite, jumpCoreSprite);

            // Üst Rota (Starlight Canopy - Jump Pad ile ulaşılır)
            CreateEnergyOrbInstance("Orb_02_SuperCanopy", worldRoot.transform, new Vector3(16.8f, 4.4f, 0f), true, hexFrameSprite, boltSprite);
            CreateCrystalPlatformInstance("Crystal_06_CanopyA", worldRoot.transform, new Vector3(19.5f, 4.10f, 0f), new Vector2(4.2f, 0.52f), 0f, bodySprite, rimSprite, starSprite);
            CreateEnergyOrbInstance("Orb_03_CanopyHigh", worldRoot.transform, new Vector3(22.6f, 5.4f, 0f), false, hexFrameSprite, boltSprite);

            // Alt Rota (Abyss Edge - Jump Pad kaçırılırsa güvenli iniş)
            CreateCrystalPlatformInstance("Crystal_06B_LowA", worldRoot.transform, new Vector3(17.2f, -1.20f, 0f), new Vector2(4.4f, 0.50f), 0f, bodySprite, rimSprite, starSprite);
            CreateEnergyOrbInstance("Orb_03B_LowAbyss", worldRoot.transform, new Vector3(18.0f, 0.35f, 0f), false, hexFrameSprite, boltSprite);
            CreateCrystalPlatformInstance("Crystal_07B_LowRamp", worldRoot.transform, new Vector3(21.4f, -0.45f, 0f), new Vector2(4.4f, 0.50f), 22f, bodySprite, rimSprite, starSprite);

            // BÖLGE 3: Işık Kırıcı Prizma & Birleşim Köprüsü
            CreateCrystalPlatformInstance("Crystal_08_MergeBridge", worldRoot.transform, new Vector3(26.5f, 0.20f, 0f), new Vector2(5.4f, 0.52f), 0f, bodySprite, rimSprite, starSprite);
            CreatePrismRelayInstance("PrismRelay_01", worldRoot.transform, new Vector3(27.5f, 2.35f, 0f), new[] { -20f, 12f }, prismSprite);
            CreateEnergyOrbInstance("Orb_04_PrismTop", worldRoot.transform, new Vector3(31.8f, 3.25f, 0f), false, hexFrameSprite, boltSprite);
            CreateCrystalPlatformInstance("Crystal_09_GapBridge", worldRoot.transform, new Vector3(33.2f, -0.40f, 0f), new Vector2(4.6f, 0.50f), 0f, bodySprite, rimSprite, starSprite);
            CreateEnergyOrbInstance("Orb_05_BridgeHigh", worldRoot.transform, new Vector3(34.5f, 2.1f, 0f), true, hexFrameSprite, boltSprite);

            // BÖLGE 4: İkinci Rampa & Uçurum Sıçrayışı
            CreateCrystalPlatformInstance("Crystal_10_RampSecond", worldRoot.transform, new Vector3(37.6f, 0.90f, 0f), new Vector2(4.8f, 0.50f), 32f, bodySprite, rimSprite, starSprite);
            CreateCrystalPlatformInstance("Crystal_11_Peak", worldRoot.transform, new Vector3(41.8f, 2.00f, 0f), new Vector2(4.2f, 0.52f), 0f, bodySprite, rimSprite, starSprite);
            CreateJumpPadInstance("JumpPad_02_Chasm", worldRoot.transform, new Vector3(43.2f, 2.52f, 0f), new Vector2(8.8f, 12.6f), jumpRingSprite, jumpCoreSprite);

            CreateEnergyOrbInstance("Orb_06_ChasmMid", worldRoot.transform, new Vector3(46.2f, 5.1f, 0f), false, hexFrameSprite, boltSprite);
            CreateCrystalPlatformInstance("Crystal_12_FinalCanopy", worldRoot.transform, new Vector3(49.5f, 3.60f, 0f), new Vector2(5.5f, 0.52f), 0f, bodySprite, rimSprite, starSprite);
            CreateCrystalPlatformInstance("Crystal_13_FinalRun", worldRoot.transform, new Vector3(55.5f, 3.20f, 0f), new Vector2(6.0f, 0.52f), -4f, bodySprite, rimSprite, starSprite);
            CreateEnergyOrbInstance("Orb_07_FinalSuper", worldRoot.transform, new Vector3(58.5f, 4.7f, 0f), true, hexFrameSprite, boltSprite);
            CreateCrystalPlatformInstance("Crystal_14_VictoryPeak", worldRoot.transform, new Vector3(62.2f, 2.60f, 0f), new Vector2(7.0f, 0.54f), 0f, bodySprite, rimSprite, starSprite);
        }

        private static void SaveWorldPrefabs(
            Sprite bodySprite,
            Sprite rimSprite,
            Sprite starSprite,
            Sprite hexFrameSprite,
            Sprite boltSprite,
            Sprite jumpRingSprite,
            Sprite jumpCoreSprite,
            Sprite prismSprite)
        {
            CrystalPlatform platPrefab = CreateCrystalPlatformInstance("PF_CrystalPlatform", null, Vector3.zero, new Vector2(2.4f, 0.5f), 0f, bodySprite, rimSprite, starSprite);
            PrefabUtility.SaveAsPrefabAsset(platPrefab.gameObject, Path.Combine(PrefabsFolder, "PF_CrystalPlatform.prefab"));
            Object.DestroyImmediate(platPrefab.gameObject);

            EnergyOrb orbPrefab = CreateEnergyOrbInstance("PF_EnergyOrb", null, Vector3.zero, false, hexFrameSprite, boltSprite);
            PrefabUtility.SaveAsPrefabAsset(orbPrefab.gameObject, Path.Combine(PrefabsFolder, "PF_EnergyOrb.prefab"));
            Object.DestroyImmediate(orbPrefab.gameObject);

            EnergyOrb superOrbPrefab = CreateEnergyOrbInstance("PF_EnergyOrb_Super", null, Vector3.zero, true, hexFrameSprite, boltSprite);
            PrefabUtility.SaveAsPrefabAsset(superOrbPrefab.gameObject, Path.Combine(PrefabsFolder, "PF_EnergyOrb_Super.prefab"));
            Object.DestroyImmediate(superOrbPrefab.gameObject);

            PrismaticJumpPad padPrefab = CreateJumpPadInstance("PF_PrismaticJumpPad", null, Vector3.zero, new Vector2(8.2f, 13.0f), jumpRingSprite, jumpCoreSprite);
            PrefabUtility.SaveAsPrefabAsset(padPrefab.gameObject, Path.Combine(PrefabsFolder, "PF_PrismaticJumpPad.prefab"));
            Object.DestroyImmediate(padPrefab.gameObject);

            PrismRelay2D relayPrefab = CreatePrismRelayInstance("PF_PrismRelay", null, Vector3.zero, new[] { -22f, 18f }, prismSprite);
            PrefabUtility.SaveAsPrefabAsset(relayPrefab.gameObject, Path.Combine(PrefabsFolder, "PF_PrismRelay.prefab"));
            Object.DestroyImmediate(relayPrefab.gameObject);
        }

        private static CrystalPlatform CreateCrystalPlatformInstance(
            string name,
            Transform parent,
            Vector3 position,
            Vector2 size,
            float angleDeg,
            Sprite bodySprite,
            Sprite rimSprite,
            Sprite starSprite)
        {
            GameObject go = new GameObject(name);
            go.layer = HardlightWorldLayer;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer mainSr = go.AddComponent<SpriteRenderer>();
            mainSr.sprite = bodySprite;
            mainSr.sortingOrder = 5;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            GameObject rimObj = new GameObject("RimGlow");
            rimObj.transform.SetParent(go.transform, false);
            SpriteRenderer rimSr = rimObj.AddComponent<SpriteRenderer>();
            rimSr.sprite = rimSprite;
            rimSr.sortingOrder = 6;

            GameObject leftSparkle = new GameObject("SparkleLeft");
            leftSparkle.transform.SetParent(go.transform, false);
            leftSparkle.transform.localPosition = new Vector3(-0.48f, 0.48f, 0f);
            SpriteRenderer leftSr = leftSparkle.AddComponent<SpriteRenderer>();
            leftSr.sprite = starSprite;
            leftSr.color = new Color(0.85f, 0.98f, 1f, 0.95f);
            leftSr.sortingOrder = 8;

            GameObject rightSparkle = new GameObject("SparkleRight");
            rightSparkle.transform.SetParent(go.transform, false);
            rightSparkle.transform.localPosition = new Vector3(0.48f, 0.48f, 0f);
            SpriteRenderer rightSr = rightSparkle.AddComponent<SpriteRenderer>();
            rightSr.sprite = starSprite;
            rightSr.color = new Color(1f, 0.92f, 0.65f, 0.95f);
            rightSr.sortingOrder = 8;

            CrystalPlatform plat = go.AddComponent<CrystalPlatform>();
            SerializedObject platSo = new SerializedObject(plat);
            platSo.FindProperty("rimGlowRenderer").objectReferenceValue = rimSr;
            platSo.FindProperty("cornerSparkleLeft").objectReferenceValue = leftSparkle.transform;
            platSo.FindProperty("cornerSparkleRight").objectReferenceValue = rightSparkle.transform;
            platSo.ApplyModifiedPropertiesWithoutUndo();

            return plat;
        }

        private static EnergyOrb CreateEnergyOrbInstance(
            string name,
            Transform parent,
            Vector3 position,
            bool isSuper,
            Sprite hexFrameSprite,
            Sprite boltSprite)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            float s = isSuper ? 1.15f : 0.92f;
            go.transform.localScale = new Vector3(s, s, 1f);

            SpriteRenderer frameSr = go.AddComponent<SpriteRenderer>();
            frameSr.sprite = hexFrameSprite;
            frameSr.sortingOrder = 10;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.55f;

            GameObject boltObj = new GameObject("InnerBolt");
            boltObj.transform.SetParent(go.transform, false);
            boltObj.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
            SpriteRenderer boltSr = boltObj.AddComponent<SpriteRenderer>();
            boltSr.sprite = boltSprite;
            boltSr.color = isSuper ? new Color(1f, 0.92f, 0.45f, 1f) : new Color(1f, 0.84f, 0.15f, 1f);
            boltSr.sortingOrder = 11;

            EnergyOrb orb = go.AddComponent<EnergyOrb>();
            SerializedObject orbSo = new SerializedObject(orb);
            orbSo.FindProperty("isSuperCharge").boolValue = isSuper;
            orbSo.FindProperty("innerBoltRenderer").objectReferenceValue = boltSr;
            orbSo.ApplyModifiedPropertiesWithoutUndo();

            return orb;
        }

        private static PrismaticJumpPad CreateJumpPadInstance(
            string name,
            Transform parent,
            Vector3 position,
            Vector2 launchVel,
            Sprite ringSprite,
            Sprite coreSprite)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(1.45f, 0.52f, 1f);

            SpriteRenderer ringSr = go.AddComponent<SpriteRenderer>();
            ringSr.sprite = ringSprite;
            ringSr.sortingOrder = 7;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.0f, 0.85f);

            GameObject coreObj = new GameObject("InnerOrangeCore");
            coreObj.transform.SetParent(go.transform, false);
            coreObj.transform.localScale = new Vector3(0.62f, 0.62f, 1f);
            SpriteRenderer coreSr = coreObj.AddComponent<SpriteRenderer>();
            coreSr.sprite = coreSprite;
            coreSr.color = new Color(1f, 0.55f, 0.1f, 0.95f);
            coreSr.sortingOrder = 8;

            PrismaticJumpPad pad = go.AddComponent<PrismaticJumpPad>();
            SerializedObject padSo = new SerializedObject(pad);
            padSo.FindProperty("launchVelocity").vector2Value = launchVel;
            padSo.FindProperty("innerCoreRenderer").objectReferenceValue = coreSr;
            padSo.ApplyModifiedPropertiesWithoutUndo();

            return pad;
        }

        private static PrismRelay2D CreatePrismRelayInstance(
            string name,
            Transform parent,
            Vector3 position,
            float[] anglesDeg,
            Sprite prismSprite)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = prismSprite;
            sr.sortingOrder = 10;

            PrismRelay2D relay = go.AddComponent<PrismRelay2D>();
            SerializedObject relaySo = new SerializedObject(relay);
            SerializedProperty arr = relaySo.FindProperty("refractionAnglesDeg");
            arr.arraySize = anglesDeg.Length;
            for (int i = 0; i < anglesDeg.Length; i++)
            {
                arr.GetArrayElementAtIndex(i).floatValue = anglesDeg[i];
            }
            relaySo.ApplyModifiedPropertiesWithoutUndo();

            return relay;
        }

        private static PhysicsMaterial2D CreateOrLoadZeroFrictionMaterial()
        {
            string matPath = Path.Combine(PhysicsFolder, "RunnerZeroFriction.physicsMaterial2D").Replace("\\", "/");
            PhysicsMaterial2D mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(matPath);
            if (mat == null)
            {
                mat = new PhysicsMaterial2D("RunnerZeroFriction") { friction = 0f, bounciness = 0f };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.friction = 0f;
                mat.bounciness = 0f;
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static void EnsureProjectLayers()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers == null || !layers.isArray) return;

            SetLayerName(layers, PlayerLayer, "Player");
            SetLayerName(layers, HardlightWorldLayer, "HardlightWorld");
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLayerName(SerializedProperty layers, int index, string layerName)
        {
            if (index >= layers.arraySize) return;
            SerializedProperty element = layers.GetArrayElementAtIndex(index);
            if (element != null && element.stringValue != layerName) element.stringValue = layerName;
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(SpritesFolder)) Directory.CreateDirectory(SpritesFolder);
            if (!Directory.Exists(PrefabsFolder)) Directory.CreateDirectory(PrefabsFolder);
            if (!Directory.Exists(PhysicsFolder)) Directory.CreateDirectory(PhysicsFolder);
        }

        private static void DestroyExistingIfFound(string objName)
        {
            GameObject existing = GameObject.Find(objName);
            if (existing != null) Object.DestroyImmediate(existing);
        }

        #region Procedural High-Contrast Art Generators

        private static Sprite GenerateCrystalPlatformSprite(string assetPath)
        {
            const int w = 128;
            const int h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    float diag1 = Mathf.Abs((nx - ny * 0.5f) - 0.25f);
                    float diag2 = Mathf.Abs((nx + ny * 0.5f) - 0.75f);
                    float centerFacet = Mathf.Abs(ny - 0.55f);

                    bool isBorder = x < 3 || x >= w - 3 || y < 3 || y >= h - 3;
                    bool isFacetLine = diag1 < 0.018f || diag2 < 0.018f || centerFacet < 0.02f;

                    Color c;
                    if (isBorder) c = new Color(0.88f, 0.99f, 1.0f, 1.0f);
                    else if (isFacetLine) c = new Color(0.55f, 0.92f, 1.0f, 0.88f);
                    else
                    {
                        Color topTint = new Color(0.12f, 0.78f, 1.0f, 0.72f);
                        Color bottomTint = new Color(0.48f, 0.18f, 0.88f, 0.62f);
                        c = Color.Lerp(bottomTint, topTint, ny);
                    }
                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 128f);
        }

        private static Sprite GenerateCrystalRimSprite(string assetPath)
        {
            const int w = 128;
            const int h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int distEdge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y));
                    float alpha = distEdge < 5 ? Mathf.Clamp01(1f - distEdge / 5f) : 0f;
                    pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 128f);
        }

        private static Sprite GenerateStarburstSprite(string assetPath)
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center) / center;
                    float dy = Mathf.Abs(y - center) / center;
                    float radial = Mathf.Sqrt(dx * dx + dy * dy);
                    float cross = Mathf.Max(Mathf.Exp(-dx * 12f) * Mathf.Exp(-dy * 2.2f), Mathf.Exp(-dy * 12f) * Mathf.Exp(-dx * 2.2f));
                    float core = Mathf.Exp(-radial * 5.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(cross + core));
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 64f);
        }

        private static Sprite GenerateEmitterCoreSprite(string assetPath)
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    float a = Mathf.Exp(-d * d * 4.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 64f);
        }

        private static Sprite GenerateRunnerTorsoHeadSprite(string assetPath)
        {
            const int w = 128;
            const int h = 160;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            Color armorDark = new Color(0.08f, 0.13f, 0.22f, 1f);
            Color armorMid = new Color(0.14f, 0.24f, 0.38f, 1f);
            Color neonCyan = new Color(0.0f, 0.94f, 1.0f, 1f);
            Color visorGlow = new Color(0.82f, 1.0f, 1.0f, 1f);

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    Color c = Color.clear;

                    // Atletik V-form gövde (ny: 0.08..0.64)
                    if (ny >= 0.08f && ny <= 0.64f)
                    {
                        float torsoT = (ny - 0.08f) / 0.56f;
                        float torsoHalfW = Mathf.Lerp(0.14f, 0.26f, torsoT);
                        float centerX = Mathf.Lerp(0.46f, 0.52f, torsoT); // Hafif ileri atletik eğim
                        float dx = Mathf.Abs(nx - centerX);
                        if (dx <= torsoHalfW)
                        {
                            bool isOuterRim = dx > torsoHalfW - 0.035f;
                            bool isChestCore = Mathf.Abs(ny - 0.46f) < 0.045f && dx < 0.14f;
                            c = (isOuterRim || isChestCore) ? neonCyan : Color.Lerp(armorDark, armorMid, torsoT);
                        }
                    }

                    // Kask & Parlak İleri Vizör (ny: 0.66..0.93)
                    Vector2 headCenter = new Vector2(0.54f, 0.79f);
                    float headDist = Vector2.Distance(new Vector2(nx, ny), headCenter);
                    if (headDist <= 0.145f)
                    {
                        c = armorMid;
                        if (nx >= 0.52f && nx <= 0.69f && ny >= 0.76f && ny <= 0.84f)
                        {
                            c = visorGlow;
                        }
                        else if (headDist >= 0.12f)
                        {
                            c = neonCyan;
                        }
                    }

                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 128f);
        }

        private static Sprite GenerateRunnerLimbSprite(string assetPath)
        {
            const int w = 32;
            const int h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    float dx = Mathf.Abs(nx - 0.5f);
                    float halfWidth = Mathf.Lerp(0.24f, 0.38f, ny);
                    if (dx <= halfWidth)
                    {
                        bool isBootOrStripe = ny < 0.18f || dx > halfWidth - 0.09f;
                        pixels[y * w + x] = isBootOrStripe ? Color.white : new Color(0.18f, 0.28f, 0.42f, 1f);
                    }
                    else
                    {
                        pixels[y * w + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 64f);
        }

        private static Sprite GenerateSoftDotSprite(string assetPath)
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            float c = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                    float a = Mathf.Clamp01(1f - d);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 64f);
        }

        private static Sprite GenerateHexFrameSprite(string assetPath)
        {
            const int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float ny = ((float)y / (size - 1)) * 2f - 1f;
                for (int x = 0; x < size; x++)
                {
                    float nx = ((float)x / (size - 1)) * 2f - 1f;
                    // Düzgün Altıgen SDF (Pointy-top hexagon)
                    float ax = Mathf.Abs(nx);
                    float ay = Mathf.Abs(ny);
                    float hexDist = Mathf.Max(ax, ax * 0.5f + ay * 0.8660254f);

                    if (hexDist >= 0.72f && hexDist <= 0.86f)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                    else if (hexDist < 0.72f)
                    {
                        pixels[y * size + x] = new Color(0.08f, 0.35f, 0.55f, 0.35f);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 128f);
        }

        private static Sprite GenerateLightningBoltSprite(string assetPath)
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float ny = (float)y / (size - 1);
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / (size - 1);
                    bool inside = false;
                    // Üst üçgen (ny: 0.45..0.92)
                    if (ny >= 0.45f && ny <= 0.92f)
                    {
                        float t = (ny - 0.45f) / 0.47f;
                        float left = Mathf.Lerp(0.28f, 0.58f, t);
                        float right = Mathf.Lerp(0.60f, 0.64f, t);
                        if (nx >= left && nx <= right) inside = true;
                    }
                    // Alt üçgen (ny: 0.08..0.55)
                    if (ny >= 0.08f && ny <= 0.55f)
                    {
                        float t = (ny - 0.08f) / 0.47f;
                        float left = Mathf.Lerp(0.36f, 0.42f, t);
                        float right = Mathf.Lerp(0.44f, 0.72f, t);
                        if (nx >= left && nx <= right) inside = true;
                    }

                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 64f);
        }

        private static Sprite GenerateJumpPadRingSprite(string assetPath)
        {
            const int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float ny = ((float)y / (size - 1)) * 2f - 1f;
                for (int x = 0; x < size; x++)
                {
                    float nx = ((float)x / (size - 1)) * 2f - 1f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    if (r >= 0.74f && r <= 0.92f)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                    else if (r >= 0.48f && r <= 0.62f)
                    {
                        pixels[y * size + x] = new Color(1f, 0.62f, 0.25f, 0.9f);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 128f);
        }

        private static Sprite GeneratePrismRelayDiamondSprite(string assetPath)
        {
            const int size = 96;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float ny = Mathf.Abs(((float)y / (size - 1)) * 2f - 1f);
                for (int x = 0; x < size; x++)
                {
                    float nx = Mathf.Abs(((float)x / (size - 1)) * 2f - 1f);
                    float diamond = nx + ny;
                    if (diamond <= 0.90f)
                    {
                        bool isBorder = diamond >= 0.76f || Mathf.Abs(nx - ny) < 0.04f;
                        pixels[y * size + x] = isBorder ? Color.white : new Color(0.75f, 0.35f, 1.0f, 0.78f);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 96f);
        }

        private static Sprite GenerateSolidWhiteRectSprite(string assetPath)
        {
            Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[256];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 16f);
        }

        private static Texture2D GenerateBeamGradientTexture(string assetPath)
        {
            const int w = 128;
            const int h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            Color hotCore = new Color(0.75f, 1.0f, 1.0f, 1f);
            Color cyanBeam = new Color(0.0f, 0.94f, 1.0f, 1f);
            Color goldenRim = new Color(1.0f, 0.72f, 0.18f, 1f);

            for (int y = 0; y < h; y++)
            {
                float v = (float)y / (h - 1);
                float distFromCenter = Mathf.Abs(v - 0.5f) * 2f;
                float angularAlpha = Mathf.Pow(Mathf.Clamp01(1f - distFromCenter), 1.45f);
                float goldenRimBand = Mathf.Exp(-Mathf.Pow((distFromCenter - 0.62f) / 0.18f, 2f));

                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / (w - 1);
                    float radialAlpha = Mathf.Pow(Mathf.Clamp01(1f - u), 1.35f);

                    Color baseCol = Color.Lerp(hotCore, cyanBeam, Mathf.Clamp01(u * 1.4f + distFromCenter * 0.5f));
                    baseCol = Color.Lerp(baseCol, goldenRim, goldenRimBand * 0.75f);

                    float finalAlpha = radialAlpha * angularAlpha * 0.85f;
                    pixels[y * w + x] = new Color(baseCol.r, baseCol.g, baseCol.b, finalAlpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            File.WriteAllBytes(assetPath, png);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static Sprite SaveAndImportSprite(Texture2D tex, string assetPath, float ppu)
        {
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            File.WriteAllBytes(assetPath, png);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = ppu;
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        #endregion
    }
}
