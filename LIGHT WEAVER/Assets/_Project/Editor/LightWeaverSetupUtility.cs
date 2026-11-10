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
    /// Light Weaver için katmanları, fizik materyalini, prosedürel sprite'ları, 5 Sektör profilini,
    /// tüm havuz prefab'larını (kristal, enerji, sıçrama, prizma, gölge engelleri) ve sonsuz
    /// bölüm direktörü sahnesini deterministik olarak inşa eder.
    /// </summary>
    public static class LightWeaverSetupUtility
    {
        private const string SpritesFolder = "Assets/_Project/Art/Sprites";
        private const string PrefabsFolder = "Assets/_Project/Prefabs";
        private const string PhysicsFolder = "Assets/_Project/Physics";
        private const string SectorsFolder = "Assets/_Project/Data/Sectors";

        private const int PlayerLayer = 6;
        private const int HardlightWorldLayer = 7;

        [MenuItem("LightWeaver/Build Playable Scene (Phase 1 - Phase 4)")]
        public static void BuildPlayableScene()
        {
            EnsureDirectories();
            EnsureProjectLayers();
            PhysicsMaterial2D zeroFrictionMat = CreateOrLoadZeroFrictionMaterial();

            // 1. Prosedürel Sprite Üretimi
            Sprite crystalTileSprite = GenerateCrystalPlatformSprite(Path.Combine(SpritesFolder, "prism_crystal_block.png"));
            Sprite rimGlowSprite = GenerateCrystalRimSprite(Path.Combine(SpritesFolder, "prism_crystal_rim.png"));
            Sprite starburstSprite = GenerateStarburstSprite(Path.Combine(SpritesFolder, "diamond_starburst.png"));
            Sprite emitterCoreSprite = GenerateEmitterCoreSprite(Path.Combine(SpritesFolder, "flashlight_emitter_core.png"));

            Sprite runnerTorsoSprite = GenerateRunnerTorsoHeadSprite(Path.Combine(SpritesFolder, "runner_torso_head.png"));
            Sprite runnerLimbSprite = GenerateRunnerLimbSprite(Path.Combine(SpritesFolder, "runner_limb.png"));
            Sprite runnerSparkSprite = GenerateSoftDotSprite(Path.Combine(SpritesFolder, "runner_spark_dot.png"));
            Texture2D beamGradientTex = GenerateBeamGradientTexture(Path.Combine(SpritesFolder, "volumetric_beam_gradient.png"));

            Sprite hexFrameSprite = GenerateHexFrameSprite(Path.Combine(SpritesFolder, "energy_hex_frame.png"));
            Sprite lightningBoltSprite = GenerateLightningBoltSprite(Path.Combine(SpritesFolder, "energy_bolt_icon.png"));
            Sprite jumpRingSprite = GenerateJumpPadRingSprite(Path.Combine(SpritesFolder, "jump_pad_ring.png"));
            Sprite jumpCoreSprite = GenerateSoftDotSprite(Path.Combine(SpritesFolder, "jump_pad_core.png"));
            Sprite prismDiamondSprite = GeneratePrismRelayDiamondSprite(Path.Combine(SpritesFolder, "prism_relay_diamond.png"));
            Sprite solidBarSprite = GenerateSolidWhiteRectSprite(Path.Combine(SpritesFolder, "diegetic_bar_pixel.png"));

            // Faz 4 Sprite'ları (Gölge Engelleri & FlashBurst Halkası)
            Sprite shadowPillarSprite = GenerateShadowPillarSprite(Path.Combine(SpritesFolder, "shadow_pillar.png"));
            Sprite umbraStalkerSprite = GenerateUmbraStalkerSprite(Path.Combine(SpritesFolder, "umbra_stalker.png"));
            Sprite voidLeechSprite = GenerateVoidLeechSprite(Path.Combine(SpritesFolder, "void_leech.png"));
            Sprite burstRingSprite = GenerateHollowRingSprite(Path.Combine(SpritesFolder, "flash_burst_ring.png"));

            AssetDatabase.Refresh();

            // 2. 5 Sektör ScriptableObject Profili (Level 1..5)
            SectorProfileSO[] sectorProfiles = CreateOrUpdateSectorProfiles();

            // 3. Kamera Kurulumu (#05070D Zifiri Karanlık Boşluk + CameraFollow2D)
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
            mainCam.backgroundColor = new Color(0.02f, 0.027f, 0.051f, 1f);

            CameraFollow2D camFollow = mainCam.GetComponent<CameraFollow2D>();
            if (camFollow == null) camFollow = mainCam.gameObject.AddComponent<CameraFollow2D>();

            // Eski kökleri temizle
            DestroyExistingIfFound("=== LIGHT WEAVER SYSTEMS ===");
            DestroyExistingIfFound("=== PLAYER FLASHLIGHT RIG ===");
            DestroyExistingIfFound("=== LIGHT WEAVER RUNNER ===");
            DestroyExistingIfFound("=== PRISM CRYSTAL WORLD ===");
            DestroyExistingIfFound("=== VOID STARFIELD ===");

            // 4. Prefab'ları Üret & Kaydet
            GameObject platPrefab = SaveCrystalPrefab(crystalTileSprite, rimGlowSprite, starburstSprite);
            GameObject orbPrefab = SaveOrbPrefab("PF_EnergyOrb.prefab", false, hexFrameSprite, lightningBoltSprite);
            GameObject superOrbPrefab = SaveOrbPrefab("PF_EnergyOrb_Super.prefab", true, hexFrameSprite, lightningBoltSprite);
            GameObject jumpPadPrefab = SaveJumpPadPrefab(jumpRingSprite, jumpCoreSprite);
            GameObject prismRelayPrefab = SavePrismRelayPrefab(prismDiamondSprite);
            GameObject pillarPrefab = SaveHazardPrefab("PF_ShadowPillar.prefab", ShadowHazardType.MovingShadowPillar, shadowPillarSprite, new Vector2(0.85f, 3.2f));
            GameObject stalkerPrefab = SaveHazardPrefab("PF_UmbraStalker.prefab", ShadowHazardType.UmbraStalker, umbraStalkerSprite, new Vector2(1.1f, 1.1f));
            GameObject leechPrefab = SaveHazardPrefab("PF_VoidLeech.prefab", ShadowHazardType.VoidLeech, voidLeechSprite, new Vector2(1.05f, 1.05f));

            // 5. Neon Cyber-Runner, Pil Sistemi & FlashBurst Kurulumu
            GameObject runnerGo = BuildRunnerCharacter(
                mainCam,
                camFollow,
                zeroFrictionMat,
                runnerTorsoSprite,
                runnerLimbSprite,
                emitterCoreSprite,
                runnerSparkSprite,
                solidBarSprite,
                burstRingSprite,
                beamGradientTex);

            SerializedObject camSo = new SerializedObject(camFollow);
            camSo.FindProperty("target").objectReferenceValue = runnerGo.transform;
            camSo.ApplyModifiedPropertiesWithoutUndo();
            mainCam.transform.position = new Vector3(runnerGo.transform.position.x + 4.6f, runnerGo.transform.position.y + 0.9f, -10f);

            PrefabUtility.SaveAsPrefabAsset(runnerGo, Path.Combine(PrefabsFolder, "PF_LightWeaverRunner.prefab"));

            // 6. Sonsuz Parallax Yıldız Alanı
            BuildInfiniteStarfield(mainCam.transform, runnerSparkSprite);

            // 7. Merkezi Sistemler: ObjectPooler + EndlessChunkDirector
            GameObject systemsRoot = new GameObject("=== LIGHT WEAVER SYSTEMS ===");
            ObjectPooler pooler = systemsRoot.AddComponent<ObjectPooler>();
            EndlessChunkDirector director = systemsRoot.AddComponent<EndlessChunkDirector>();

            SerializedObject dirSo = new SerializedObject(director);
            dirSo.FindProperty("pooler").objectReferenceValue = pooler;
            dirSo.FindProperty("runnerTransform").objectReferenceValue = runnerGo.transform;
            dirSo.FindProperty("runnerMotor").objectReferenceValue = runnerGo.GetComponent<RunnerMotor2D>();
            dirSo.FindProperty("batterySystem").objectReferenceValue = runnerGo.GetComponent<BatterySystem>();
            dirSo.FindProperty("mainCamera").objectReferenceValue = mainCam;

            dirSo.FindProperty("crystalPlatformPrefab").objectReferenceValue = platPrefab;
            dirSo.FindProperty("energyOrbPrefab").objectReferenceValue = orbPrefab;
            dirSo.FindProperty("energyOrbSuperPrefab").objectReferenceValue = superOrbPrefab;
            dirSo.FindProperty("jumpPadPrefab").objectReferenceValue = jumpPadPrefab;
            dirSo.FindProperty("prismRelayPrefab").objectReferenceValue = prismRelayPrefab;
            dirSo.FindProperty("shadowPillarPrefab").objectReferenceValue = pillarPrefab;
            dirSo.FindProperty("umbraStalkerPrefab").objectReferenceValue = stalkerPrefab;
            dirSo.FindProperty("voidLeechPrefab").objectReferenceValue = leechPrefab;

            SerializedProperty sectorsProp = dirSo.FindProperty("sectors");
            sectorsProp.arraySize = sectorProfiles.Length;
            for (int i = 0; i < sectorProfiles.Length; i++)
            {
                sectorsProp.GetArrayElementAtIndex(i).objectReferenceValue = sectorProfiles[i];
            }
            dirSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[LightWeaver Setup] Phase 4 Complete: EndlessChunkDirector, 5 Sector Profiles, 3 ShadowHazards & FlashBurst Parry built!");
        }

        private static SectorProfileSO[] CreateOrUpdateSectorProfiles()
        {
            var specs = new (int num, string title, float dist, Color bg, Color accent, Color sec, float speed, float drain, float haz, float prism)[]
            {
                (1, "THE SILENT ABYSS",     100f, new Color(0.020f, 0.027f, 0.051f, 1f), new Color(0.00f, 0.94f, 1.00f, 1f), new Color(0.35f, 0.55f, 1.00f, 1f), 1.00f, 0.85f, 0.12f, 0.15f),
                (2, "THE PRISM CAVERNS",    120f, new Color(0.025f, 0.020f, 0.065f, 1f), new Color(0.00f, 0.94f, 1.00f, 1f), new Color(0.71f, 0.22f, 0.95f, 1f), 1.07f, 1.00f, 0.24f, 0.30f),
                (3, "THE UMBRA SWARM",      130f, new Color(0.035f, 0.022f, 0.055f, 1f), new Color(1.00f, 0.72f, 0.00f, 1f), new Color(0.55f, 0.15f, 0.85f, 1f), 1.14f, 1.15f, 0.36f, 0.24f),
                (4, "THE ECHO VOID",        140f, new Color(0.018f, 0.030f, 0.062f, 1f), new Color(0.00f, 0.96f, 0.92f, 1f), new Color(1.00f, 0.78f, 0.18f, 1f), 1.22f, 1.28f, 0.44f, 0.28f),
                (5, "THE SINGULARITY CORE", 160f, new Color(0.045f, 0.015f, 0.040f, 1f), new Color(1.00f, 0.12f, 0.34f, 1f), new Color(0.00f, 0.94f, 1.00f, 1f), 1.32f, 1.42f, 0.52f, 0.30f),
            };

            SectorProfileSO[] result = new SectorProfileSO[specs.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                var s = specs[i];
                string assetPath = Path.Combine(SectorsFolder, $"Sector_0{s.num}.asset").Replace("\\", "/");
                SectorProfileSO profile = AssetDatabase.LoadAssetAtPath<SectorProfileSO>(assetPath);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<SectorProfileSO>();
                    AssetDatabase.CreateAsset(profile, assetPath);
                }

                profile.sectorNumber = s.num;
                profile.sectorName = s.title;
                profile.sectorDistanceMeters = s.dist;
                profile.voidBackgroundColor = s.bg;
                profile.sectorAccentColor = s.accent;
                profile.sectorSecondaryColor = s.sec;
                profile.runSpeedMultiplier = s.speed;
                profile.batteryDrainMultiplier = s.drain;
                profile.hazardFrequency = s.haz;
                profile.prismRelayFrequency = s.prism;
                profile.orbDensity = 0.82f;

                EditorUtility.SetDirty(profile);
                result[i] = profile;
            }
            return result;
        }

        private static GameObject BuildRunnerCharacter(
            Camera mainCam,
            CameraFollow2D camFollow,
            PhysicsMaterial2D zeroFrictionMat,
            Sprite torsoSprite,
            Sprite limbSprite,
            Sprite emitterCoreSprite,
            Sprite sparkSprite,
            Sprite barSprite,
            Sprite burstRingSprite,
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

            // Diegetik Sırt Pil Göstergesi
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

            GameObject burstRingObj = new GameObject("FlashBurstShockwave");
            burstRingObj.transform.SetParent(runner.transform, false);
            SpriteRenderer burstRingSr = burstRingObj.AddComponent<SpriteRenderer>();
            burstRingSr.sprite = burstRingSprite;
            burstRingSr.sortingOrder = 22;
            burstRingSr.enabled = false;

            FlashBurstController flashBurst = runner.AddComponent<FlashBurstController>();
            SerializedObject burstSo = new SerializedObject(flashBurst);
            burstSo.FindProperty("touchInput").objectReferenceValue = touchInput;
            burstSo.FindProperty("batterySystem").objectReferenceValue = batterySystem;
            burstSo.FindProperty("cameraFollow").objectReferenceValue = camFollow;
            burstSo.FindProperty("shockwaveRingRenderer").objectReferenceValue = burstRingSr;
            burstSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject sparkObj = new GameObject("FootstepSparks");
            sparkObj.transform.SetParent(visualRoot.transform, false);
            sparkObj.transform.localPosition = new Vector3(-0.08f, -0.68f, 0f);
            ParticleSystem ps = CreateFootstepParticleSystem(sparkObj, sparkSprite);

            GameObject trailObj = new GameObject("SpeedTrail");
            trailObj.transform.SetParent(bodyPivot.transform, false);
            trailObj.transform.localPosition = new Vector3(-0.12f, 0.25f, 0f);
            TrailRenderer trail = CreateRunnerSpeedTrail(trailObj);

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

        private static void BuildInfiniteStarfield(Transform camTransform, Sprite dotSprite)
        {
            GameObject starRoot = new GameObject("=== VOID STARFIELD ===");
            Random.InitState(42);
            for (int i = 0; i < 65; i++)
            {
                GameObject star = new GameObject($"Star_{i:00}");
                star.transform.SetParent(starRoot.transform, false);
                star.transform.position = new Vector3(Random.Range(-17f, 17f), Random.Range(-9f, 9f), 2f);
                float scale = Random.Range(0.03f, 0.085f);
                star.transform.localScale = new Vector3(scale, scale, 1f);

                SpriteRenderer sr = star.AddComponent<SpriteRenderer>();
                sr.sprite = dotSprite;
                sr.color = new Color(0.45f, 0.85f, 1f, Random.Range(0.08f, 0.25f));
                sr.sortingOrder = -10;
            }

            ParallaxStarfield parallax = starRoot.AddComponent<ParallaxStarfield>();
            SerializedObject so = new SerializedObject(parallax);
            so.FindProperty("cameraTransform").objectReferenceValue = camTransform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject SaveCrystalPrefab(Sprite bodySprite, Sprite rimSprite, Sprite starSprite)
        {
            GameObject go = new GameObject("PF_CrystalPlatform");
            go.layer = HardlightWorldLayer;
            go.transform.localScale = new Vector3(2.4f, 0.52f, 1f);

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

            string path = Path.Combine(PrefabsFolder, "PF_CrystalPlatform.prefab");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject SaveOrbPrefab(string fileName, bool isSuper, Sprite hexFrameSprite, Sprite boltSprite)
        {
            GameObject go = new GameObject(fileName.Replace(".prefab", ""));
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

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, Path.Combine(PrefabsFolder, fileName));
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject SaveJumpPadPrefab(Sprite ringSprite, Sprite coreSprite)
        {
            GameObject go = new GameObject("PF_PrismaticJumpPad");
            go.transform.localScale = new Vector3(1.55f, 0.52f, 1f);

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
            padSo.FindProperty("innerCoreRenderer").objectReferenceValue = coreSr;
            padSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, Path.Combine(PrefabsFolder, "PF_PrismaticJumpPad.prefab"));
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject SavePrismRelayPrefab(Sprite prismSprite)
        {
            GameObject go = new GameObject("PF_PrismRelay");
            go.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = prismSprite;
            sr.sortingOrder = 10;

            PrismRelay2D relay = go.AddComponent<PrismRelay2D>();
            SerializedObject relaySo = new SerializedObject(relay);
            SerializedProperty arr = relaySo.FindProperty("refractionAnglesDeg");
            arr.arraySize = 2;
            arr.GetArrayElementAtIndex(0).floatValue = -20f;
            arr.GetArrayElementAtIndex(1).floatValue = 14f;
            relaySo.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, Path.Combine(PrefabsFolder, "PF_PrismRelay.prefab"));
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject SaveHazardPrefab(string fileName, ShadowHazardType type, Sprite sprite, Vector2 scale)
        {
            GameObject go = new GameObject(fileName.Replace(".prefab", ""));
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 12;

            if (type == ShadowHazardType.MovingShadowPillar)
            {
                BoxCollider2D box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(0.82f, 0.92f);
            }
            else
            {
                CircleCollider2D circle = go.AddComponent<CircleCollider2D>();
                circle.isTrigger = true;
                circle.radius = 0.42f;
            }

            ShadowHazard hazard = go.AddComponent<ShadowHazard>();
            SerializedObject so = new SerializedObject(hazard);
            so.FindProperty("hazardType").enumValueIndex = (int)type;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, Path.Combine(PrefabsFolder, fileName));
            Object.DestroyImmediate(go);
            return prefab;
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
            if (!Directory.Exists(SectorsFolder)) Directory.CreateDirectory(SectorsFolder);
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

                    if (ny >= 0.08f && ny <= 0.64f)
                    {
                        float torsoT = (ny - 0.08f) / 0.56f;
                        float torsoHalfW = Mathf.Lerp(0.14f, 0.26f, torsoT);
                        float centerX = Mathf.Lerp(0.46f, 0.52f, torsoT);
                        float dx = Mathf.Abs(nx - centerX);
                        if (dx <= torsoHalfW)
                        {
                            bool isOuterRim = dx > torsoHalfW - 0.035f;
                            bool isChestCore = Mathf.Abs(ny - 0.46f) < 0.045f && dx < 0.14f;
                            c = (isOuterRim || isChestCore) ? neonCyan : Color.Lerp(armorDark, armorMid, torsoT);
                        }
                    }

                    Vector2 headCenter = new Vector2(0.54f, 0.79f);
                    float headDist = Vector2.Distance(new Vector2(nx, ny), headCenter);
                    if (headDist <= 0.145f)
                    {
                        c = armorMid;
                        if (nx >= 0.52f && nx <= 0.69f && ny >= 0.76f && ny <= 0.84f) c = visorGlow;
                        else if (headDist >= 0.12f) c = neonCyan;
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
                    else pixels[y * w + x] = Color.clear;
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
                    float ax = Mathf.Abs(nx);
                    float ay = Mathf.Abs(ny);
                    float hexDist = Mathf.Max(ax, ax * 0.5f + ay * 0.8660254f);

                    if (hexDist >= 0.72f && hexDist <= 0.86f) pixels[y * size + x] = Color.white;
                    else if (hexDist < 0.72f) pixels[y * size + x] = new Color(0.08f, 0.35f, 0.55f, 0.35f);
                    else pixels[y * size + x] = Color.clear;
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
                    if (ny >= 0.45f && ny <= 0.92f)
                    {
                        float t = (ny - 0.45f) / 0.47f;
                        if (nx >= Mathf.Lerp(0.28f, 0.58f, t) && nx <= Mathf.Lerp(0.60f, 0.64f, t)) inside = true;
                    }
                    if (ny >= 0.08f && ny <= 0.55f)
                    {
                        float t = (ny - 0.08f) / 0.47f;
                        if (nx >= Mathf.Lerp(0.36f, 0.42f, t) && nx <= Mathf.Lerp(0.44f, 0.72f, t)) inside = true;
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
                    if (r >= 0.74f && r <= 0.92f) pixels[y * size + x] = Color.white;
                    else if (r >= 0.48f && r <= 0.62f) pixels[y * size + x] = new Color(1f, 0.62f, 0.25f, 0.9f);
                    else pixels[y * size + x] = Color.clear;
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
                    else pixels[y * size + x] = Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 96f);
        }

        private static Sprite GenerateShadowPillarSprite(string assetPath)
        {
            const int w = 64;
            const int h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    bool isRim = x < 4 || x >= w - 4 || y < 4 || y >= h - 4;
                    bool isInnerRunes = Mathf.Abs(nx - 0.5f) < 0.06f && (y / 12) % 2 == 0;
                    if (isRim) pixels[y * w + x] = new Color(1.0f, 0.22f, 0.45f, 0.95f);
                    else if (isInnerRunes) pixels[y * w + x] = new Color(0.85f, 0.15f, 0.55f, 0.75f);
                    else pixels[y * w + x] = new Color(0.08f, 0.03f, 0.12f, 0.92f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 64f);
        }

        private static Sprite GenerateUmbraStalkerSprite(string assetPath)
        {
            const int size = 96;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float ny = ((float)y / (size - 1)) * 2f - 1f;
                for (int x = 0; x < size; x++)
                {
                    float nx = ((float)x / (size - 1)) * 2f - 1f;
                    float spike = Mathf.Abs(nx) + Mathf.Abs(ny) * 0.65f;
                    if (spike <= 0.85f)
                    {
                        bool isRim = spike >= 0.70f;
                        bool isEye = Vector2.Distance(new Vector2(nx, ny), new Vector2(-0.12f, 0.05f)) < 0.14f;
                        if (isEye) pixels[y * size + x] = new Color(1.0f, 0.90f, 0.50f, 1.0f);
                        else if (isRim) pixels[y * size + x] = new Color(1.0f, 0.18f, 0.36f, 1.0f);
                        else pixels[y * size + x] = new Color(0.16f, 0.03f, 0.08f, 0.96f);
                    }
                    else pixels[y * size + x] = Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 96f);
        }

        private static Sprite GenerateVoidLeechSprite(string assetPath)
        {
            const int size = 96;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float ny = ((float)y / (size - 1)) * 2f - 1f;
                for (int x = 0; x < size; x++)
                {
                    float nx = ((float)x / (size - 1)) * 2f - 1f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float angle = Mathf.Atan2(ny, nx);
                    float teeth = Mathf.Sin(angle * 8f) * 0.09f;

                    if (r <= 0.76f + teeth)
                    {
                        if (r >= 0.56f + teeth) pixels[y * size + x] = new Color(0.75f, 0.18f, 1.0f, 1.0f);
                        else pixels[y * size + x] = new Color(0.03f, 0.01f, 0.06f, 0.98f);
                    }
                    else pixels[y * size + x] = Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 96f);
        }

        private static Sprite GenerateHollowRingSprite(string assetPath)
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
                    if (r >= 0.80f && r <= 0.95f) pixels[y * size + x] = Color.white;
                    else pixels[y * size + x] = Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 128f);
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
