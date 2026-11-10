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
    /// Light Weaver için katmanları, sıfır sürtünmeli fizik materyalini, prosedürel kristal ve
    /// neon koşucu sprite'larını, prefab'ları ve oynanabilir sahneyi deterministik olarak inşa eder.
    /// </summary>
    public static class LightWeaverSetupUtility
    {
        private const string SpritesFolder = "Assets/_Project/Art/Sprites";
        private const string PrefabsFolder = "Assets/_Project/Prefabs";
        private const string PhysicsFolder = "Assets/_Project/Physics";

        private const int PlayerLayer = 6;
        private const int HardlightWorldLayer = 7;

        [MenuItem("LightWeaver/Build Playable Scene (Phase 1 + Phase 2)")]
        public static void BuildPlayableScene()
        {
            EnsureDirectories();
            EnsureProjectLayers();
            PhysicsMaterial2D zeroFrictionMat = CreateOrLoadZeroFrictionMaterial();

            // 1. Prosedürel Kristal & Koşucu Sprite Üretimi
            Sprite crystalTileSprite = GenerateCrystalPlatformSprite(Path.Combine(SpritesFolder, "prism_crystal_block.png"));
            Sprite rimGlowSprite = GenerateCrystalRimSprite(Path.Combine(SpritesFolder, "prism_crystal_rim.png"));
            Sprite starburstSprite = GenerateStarburstSprite(Path.Combine(SpritesFolder, "diamond_starburst.png"));
            Sprite emitterCoreSprite = GenerateEmitterCoreSprite(Path.Combine(SpritesFolder, "flashlight_emitter_core.png"));

            Sprite runnerTorsoSprite = GenerateRunnerTorsoHeadSprite(Path.Combine(SpritesFolder, "runner_torso_head.png"));
            Sprite runnerLimbSprite = GenerateRunnerLimbSprite(Path.Combine(SpritesFolder, "runner_limb.png"));
            Sprite runnerSparkSprite = GenerateSoftDotSprite(Path.Combine(SpritesFolder, "runner_spark_dot.png"));
            Texture2D beamGradientTex = GenerateBeamGradientTexture(Path.Combine(SpritesFolder, "volumetric_beam_gradient.png"));

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
            mainCam.orthographicSize = 5.6f;
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

            // 4. Neon Cyber-Runner & Fener Rig Kurulumu
            GameObject runnerGo = BuildRunnerCharacter(
                mainCam,
                zeroFrictionMat,
                runnerTorsoSprite,
                runnerLimbSprite,
                emitterCoreSprite,
                runnerSparkSprite,
                beamGradientTex);

            // Kamerayı karaktere bağla ve başlangıç kadrajına oturt
            SerializedObject camSo = new SerializedObject(camFollow);
            camSo.FindProperty("target").objectReferenceValue = runnerGo.transform;
            camSo.ApplyModifiedPropertiesWithoutUndo();
            mainCam.transform.position = new Vector3(runnerGo.transform.position.x + 4.6f, runnerGo.transform.position.y + 0.9f, -10f);

            // Runner Prefab kaydet
            PrefabUtility.SaveAsPrefabAsset(runnerGo, Path.Combine(PrefabsFolder, "PF_LightWeaverRunner.prefab"));

            // 5. İnce Uzay Tozu & Yıldız Noktaları (Hareket Hissi İçin Minimal Referans)
            BuildSubtleVoidStarfield(runnerSparkSprite);

            // 6. Kristal Parkur Dünyası (~80m Rampa, Merdiven & İkili Rota Test Parkuru)
            BuildPhase2ParkourWorld(crystalTileSprite, rimGlowSprite, starburstSprite);

            // Kristal Platform Prefab'ını da Layer 7 ile güncelle
            CrystalPlatform prefabTemplate = CreateCrystalPlatformInstance(
                "PF_CrystalPlatform",
                null,
                Vector3.zero,
                new Vector2(2.4f, 0.5f),
                0f,
                crystalTileSprite,
                rimGlowSprite,
                starburstSprite);
            PrefabUtility.SaveAsPrefabAsset(prefabTemplate.gameObject, Path.Combine(PrefabsFolder, "PF_CrystalPlatform.prefab"));
            Object.DestroyImmediate(prefabTemplate.gameObject);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[LightWeaver Setup] Phase 1 Fixes + Phase 2 Neon Runner & 80m Crystal Ramp Course built and saved!");
        }

        private static GameObject BuildRunnerCharacter(
            Camera mainCam,
            PhysicsMaterial2D zeroFrictionMat,
            Sprite torsoSprite,
            Sprite limbSprite,
            Sprite emitterCoreSprite,
            Sprite sparkSprite,
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
            capsule.size = new Vector2(0.56f, 1.45f);
            capsule.offset = new Vector2(0f, 0.02f);
            capsule.sharedMaterial = zeroFrictionMat;

            RunnerMotor2D motor = runner.AddComponent<RunnerMotor2D>();
            SerializedObject motorSo = new SerializedObject(motor);
            motorSo.FindProperty("groundLayerMask").intValue = 1 << HardlightWorldLayer;
            motorSo.ApplyModifiedPropertiesWithoutUndo();

            // Görsel Hiyerarşi
            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(runner.transform, false);

            // Arka Kol & Arka Bacak
            Transform backArmPivot = CreateLimbPivot("BackArmPivot", visualRoot.transform, new Vector3(-0.05f, 0.25f, 0f), new Vector3(0.18f, 0.48f, 1f), new Vector3(0f, -0.20f, 0f), limbSprite, new Color(0.08f, 0.42f, 0.65f, 1f), 9);
            Transform backLegPivot = CreateLimbPivot("BackLegPivot", visualRoot.transform, new Vector3(-0.04f, -0.14f, 0f), new Vector3(0.22f, 0.56f, 1f), new Vector3(0f, -0.24f, 0f), limbSprite, new Color(0.08f, 0.45f, 0.70f, 1f), 9);

            // Gövde & Vizörlü Kafa
            GameObject bodyPivot = new GameObject("BodyPivot");
            bodyPivot.transform.SetParent(visualRoot.transform, false);
            GameObject torsoGo = new GameObject("TorsoHeadSprite");
            torsoGo.transform.SetParent(bodyPivot.transform, false);
            torsoGo.transform.localPosition = new Vector3(0f, 0.30f, 0f);
            torsoGo.transform.localScale = new Vector3(0.75f, 0.95f, 1f);
            SpriteRenderer torsoSr = torsoGo.AddComponent<SpriteRenderer>();
            torsoSr.sprite = torsoSprite;
            torsoSr.sortingOrder = 11;

            // Ön Bacak
            Transform frontLegPivot = CreateLimbPivot("FrontLegPivot", visualRoot.transform, new Vector3(0.05f, -0.14f, 0f), new Vector3(0.24f, 0.58f, 1f), new Vector3(0f, -0.25f, 0f), limbSprite, new Color(0.15f, 0.92f, 1.0f, 1f), 12);

            // Fener Kolu Pivotu + Emitter + LightConeCaster2D
            GameObject flashlightArmPivot = new GameObject("FlashlightArmPivot");
            flashlightArmPivot.transform.SetParent(visualRoot.transform, false);
            flashlightArmPivot.transform.localPosition = new Vector3(0.08f, 0.26f, 0f);

            GameObject frontArmSpriteObj = new GameObject("FrontArmSprite");
            frontArmSpriteObj.transform.SetParent(flashlightArmPivot.transform, false);
            frontArmSpriteObj.transform.localPosition = new Vector3(0.24f, 0f, 0f);
            frontArmSpriteObj.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            frontArmSpriteObj.transform.localScale = new Vector3(0.18f, 0.50f, 1f);
            SpriteRenderer frontArmSr = frontArmSpriteObj.AddComponent<SpriteRenderer>();
            frontArmSr.sprite = limbSprite;
            frontArmSr.color = new Color(0.25f, 0.96f, 1f, 1f);
            frontArmSr.sortingOrder = 13;

            GameObject emitterTip = new GameObject("FlashlightEmitterTip");
            emitterTip.transform.SetParent(flashlightArmPivot.transform, false);
            emitterTip.transform.localPosition = new Vector3(0.50f, 0f, 0f);

            SpriteRenderer emitterSr = emitterTip.AddComponent<SpriteRenderer>();
            emitterSr.sprite = emitterCoreSprite;
            emitterSr.color = new Color(1f, 0.92f, 0.45f, 1f);
            emitterSr.sortingOrder = 20;
            emitterTip.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

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

            LightTouchInput touchInput = runner.AddComponent<LightTouchInput>();
            SerializedObject inputSo = new SerializedObject(touchInput);
            inputSo.FindProperty("flashlightController").objectReferenceValue = flashlightCtrl;
            inputSo.FindProperty("targetCamera").objectReferenceValue = mainCam;
            inputSo.ApplyModifiedPropertiesWithoutUndo();

            // Ayak Altı Kıvılcım Partikülü (ParticleSystem)
            GameObject sparkObj = new GameObject("FootstepSparks");
            sparkObj.transform.SetParent(visualRoot.transform, false);
            sparkObj.transform.localPosition = new Vector3(-0.08f, -0.68f, 0f);
            ParticleSystem ps = CreateFootstepParticleSystem(sparkObj, sparkSprite);

            // Hız İzi (TrailRenderer)
            GameObject trailObj = new GameObject("SpeedTrail");
            trailObj.transform.SetParent(bodyPivot.transform, false);
            trailObj.transform.localPosition = new Vector3(-0.12f, 0.25f, 0f);
            TrailRenderer trail = CreateRunnerSpeedTrail(trailObj);

            // Prosedürel Koşu Animatörü
            RunnerProceduralAnimator procAnim = runner.AddComponent<RunnerProceduralAnimator>();
            SerializedObject animSo = new SerializedObject(procAnim);
            animSo.FindProperty("motor").objectReferenceValue = motor;
            animSo.FindProperty("visualRoot").objectReferenceValue = visualRoot.transform;
            animSo.FindProperty("bodyPivot").objectReferenceValue = bodyPivot.transform;
            animSo.FindProperty("frontLegPivot").objectReferenceValue = frontLegPivot;
            animSo.FindProperty("backLegPivot").objectReferenceValue = backLegPivot;
            animSo.FindProperty("backArmPivot").objectReferenceValue = backArmPivot;
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
                Material mat = new Material(shader);
                mat.mainTexture = sparkSprite.texture;
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
            if (shader != null)
            {
                tr.sharedMaterial = new Material(shader);
            }
            return tr;
        }

        private static void BuildSubtleVoidStarfield(Sprite dotSprite)
        {
            GameObject starRoot = new GameObject("=== VOID STARFIELD ===");
            Random.InitState(42);
            for (int i = 0; i < 85; i++)
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

        private static void BuildPhase2ParkourWorld(Sprite bodySprite, Sprite rimSprite, Sprite starSprite)
        {
            GameObject worldRoot = new GameObject("=== PRISM CRYSTAL WORLD ===");

            // Başlangıç Koşu Pisti (Fener -14° ile anında aydınlanır, ışığı kaçırırsan burası da kaybolur!)
            CreateCrystalPlatformInstance("Crystal_01_LaunchA", worldRoot.transform, new Vector3(-2.4f, -1.6f, 0f), new Vector2(4.4f, 0.52f), 0f, bodySprite, rimSprite, starSprite);
            CreateCrystalPlatformInstance("Crystal_02_LaunchB", worldRoot.transform, new Vector3(1.5f, -1.6f, 0f), new Vector2(3.2f, 0.52f), 0f, bodySprite, rimSprite, starSprite);
            CreateCrystalPlatformInstance("Crystal_03_Bridge", worldRoot.transform, new Vector3(5.2f, -1.35f, 0f), new Vector2(3.0f, 0.50f), 6f, bodySprite, rimSprite, starSprite);

            // İlk Kademeli Tırmanış (Step + 34° Rampa -> Üst Starlight Canopy)
            CreateCrystalPlatformInstance("Crystal_04_StepA", worldRoot.transform, new Vector3(7.9f, -0.7f, 0f), new Vector2(2.6f, 0.48f), 14f, bodySprite, rimSprite, starSprite);
            CreateCrystalPlatformInstance("Crystal_05_RampMain", worldRoot.transform, new Vector3(11.3f, 1.0f, 0f), new Vector2(4.2f, 0.50f), 34f, bodySprite, rimSprite, starSprite);
            CreateCrystalPlatformInstance("Crystal_06_CanopyA", worldRoot.transform, new Vector3(15.2f, 2.35f, 0f), new Vector2(3.2f, 0.50f), 0f, bodySprite, rimSprite, starSprite);
            CreateCrystalPlatformInstance("Crystal_07_CanopyB", worldRoot.transform, new Vector3(19.6f, 2.55f, 0f), new Vector2(3.0f, 0.48f), 0f, bodySprite, rimSprite, starSprite);

            // Alt Alternatif Rota (Abyss Edge)
            CreateCrystalPlatformInstance("Crystal_05B_LowA", worldRoot.transform, new Vector3(11.5f, -2.1f, 0f), new Vector2(3.2f, 0.50f), -4f, bodySprite, rimSprite, starSprite);
            CreateCrystalPlatformInstance("Crystal_06B_LowB", worldRoot.transform, new Vector3(16.0f, -2.3f, 0f), new Vector2(3.4f, 0.50f), 0f, bodySprite, rimSprite, starSprite);
            CreateCrystalPlatformInstance("Crystal_07B_LowRamp", worldRoot.transform, new Vector3(20.3f, -1.3f, 0f), new Vector2(3.6f, 0.50f), 26f, bodySprite, rimSprite, starSprite);

            // Birleşim Platformu & İkinci Rampa Zinciri
            CreateCrystalPlatformInstance("Crystal_08_Merge", worldRoot.transform, new Vector3(24.5f, 0.2f, 0f), new Vector2(3.8f, 0.52f), 0f, bodySprite, rimSprite, starSprite);
            CreateCrystalPlatformInstance("Crystal_09_GapA", worldRoot.transform, new Vector3(29.8f, 0.5f, 0f), new Vector2(2.8f, 0.48f), 8f, bodySprite, rimSprite, starSprite);
            CreateCrystalPlatformInstance("Crystal_10_SteepRamp", worldRoot.transform, new Vector3(34.2f, 2.2f, 0f), new Vector2(4.4f, 0.50f), 38f, bodySprite, rimSprite, starSprite);
            CreateCrystalPlatformInstance("Crystal_11_SkyWalkA", worldRoot.transform, new Vector3(38.6f, 3.7f, 0f), new Vector2(3.4f, 0.50f), 0f, bodySprite, rimSprite, starSprite);
            CreateCrystalPlatformInstance("Crystal_12_SkyWalkB", worldRoot.transform, new Vector3(43.5f, 3.3f, 0f), new Vector2(3.2f, 0.50f), -10f, bodySprite, rimSprite, starSprite);
            CreateCrystalPlatformInstance("Crystal_13_DownGlide", worldRoot.transform, new Vector3(48.5f, 1.8f, 0f), new Vector2(4.0f, 0.50f), -18f, bodySprite, rimSprite, starSprite);
            CreateCrystalPlatformInstance("Crystal_14_FinalBridge", worldRoot.transform, new Vector3(54.0f, 0.8f, 0f), new Vector2(4.8f, 0.52f), 0f, bodySprite, rimSprite, starSprite);
            CreateCrystalPlatformInstance("Crystal_15_FinalRamp", worldRoot.transform, new Vector3(59.8f, 2.2f, 0f), new Vector2(4.5f, 0.52f), 32f, bodySprite, rimSprite, starSprite);
            CreateCrystalPlatformInstance("Crystal_16_EndPeak", worldRoot.transform, new Vector3(65.2f, 3.6f, 0f), new Vector2(5.2f, 0.54f), 0f, bodySprite, rimSprite, starSprite);
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

        private static PhysicsMaterial2D CreateOrLoadZeroFrictionMaterial()
        {
            string matPath = Path.Combine(PhysicsFolder, "RunnerZeroFriction.physicsMaterial2D").Replace("\\", "/");
            PhysicsMaterial2D mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(matPath);
            if (mat == null)
            {
                mat = new PhysicsMaterial2D("RunnerZeroFriction")
                {
                    friction = 0f,
                    bounciness = 0f
                };
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
            if (element != null && string.IsNullOrEmpty(element.stringValue))
            {
                element.stringValue = layerName;
            }
            else if (element != null && element.stringValue != layerName)
            {
                element.stringValue = layerName;
            }
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
                    float a = Mathf.Clamp01(1f - d * d);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 64f);
        }

        private static Sprite GenerateRunnerTorsoHeadSprite(string assetPath)
        {
            const int w = 96;
            const int h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            Color armorDark = new Color(0.09f, 0.14f, 0.24f, 1f);
            Color neonCyan = new Color(0.0f, 0.94f, 1.0f, 1f);
            Color visorGlow = new Color(0.75f, 1.0f, 1.0f, 1f);

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    Color c = Color.clear;

                    // Gövde kapsülü (ny: 0.08..0.64)
                    if (ny >= 0.08f && ny <= 0.64f)
                    {
                        float torsoHalfW = Mathf.Lerp(0.16f, 0.25f, (ny - 0.08f) / 0.56f);
                        float dx = Mathf.Abs(nx - 0.48f);
                        if (dx <= torsoHalfW)
                        {
                            bool isTrim = dx > torsoHalfW - 0.045f || Mathf.Abs(ny - 0.45f) < 0.04f;
                            c = isTrim ? neonCyan : armorDark;
                        }
                    }

                    // Kafa & Fütüristik Vizör (ny: 0.66..0.94)
                    Vector2 headCenter = new Vector2(0.50f, 0.79f);
                    float headDist = Vector2.Distance(new Vector2(nx, ny), headCenter);
                    if (headDist <= 0.145f)
                    {
                        c = armorDark;
                        // İleri bakan parlayan vizör
                        if (nx >= 0.48f && nx <= 0.64f && ny >= 0.76f && ny <= 0.84f)
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
            return SaveAndImportSprite(tex, assetPath, 96f);
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
                        pixels[y * w + x] = isBootOrStripe ? new Color(1f, 1f, 1f, 1f) : new Color(0.18f, 0.28f, 0.42f, 1f);
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
            const int size = 32;
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
            return SaveAndImportSprite(tex, assetPath, 32f);
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
                float v = (float)y / (h - 1); // 0..1 açısal eksen (0.5 merkez)
                float distFromCenter = Mathf.Abs(v - 0.5f) * 2f; // 0 merkez, 1 kenar
                float angularAlpha = Mathf.Pow(Mathf.Clamp01(1f - distFromCenter), 1.45f);
                float goldenRimBand = Mathf.Exp(-Mathf.Pow((distFromCenter - 0.62f) / 0.18f, 2f));

                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / (w - 1); // 0 fener ucu, 1 uzak uç
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
