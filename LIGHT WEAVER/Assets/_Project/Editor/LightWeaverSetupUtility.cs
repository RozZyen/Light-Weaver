using System.IO;
using LightWeaver.Audio;
using LightWeaver.Core;
using LightWeaver.Feedbacks;
using LightWeaver.Player;
using LightWeaver.Pooling;
using LightWeaver.UI;
using LightWeaver.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace LightWeaver.EditorTools
{
    /// <summary>
    /// Light Weaver v2.1 (360° Aim + Mobile Floating Stick + 4 Görsel Kristal Varyantı +
    /// WorldVfxManager + Parallax Horizon + Landscape Mobil Ayarları) sahnesini kurar.
    /// </summary>
    public static class LightWeaverSetupUtility
    {
        private const string SpritesFolder = "Assets/_Project/Art/Sprites";
        private const string PrefabsFolder = "Assets/_Project/Prefabs";
        private const string PhysicsFolder = "Assets/_Project/Physics";
        private const string SectorsFolder = "Assets/_Project/Data/Sectors";
        private const string DataFolder = "Assets/_Project/Data";

        private const int PlayerLayer = 6;
        private const int HardlightWorldLayer = 7;

        [MenuItem("LightWeaver/Build Complete Game Scene (360 Aim & Mobile Ready)")]
        public static void BuildPlayableScene()
        {
            EnsureDirectories();
            EnsureProjectLayers();
            ConfigureMobileLandscapeSettings();
            PhysicsMaterial2D zeroFrictionMat = CreateOrLoadZeroFrictionMaterial();

            // 1. Prosedürel Sprite Üretimi (4 Kristal Varyantı + Koşucu + UI + VFX)
            Sprite crystalStandardSprite = GenerateCrystalPlatformSprite(Path.Combine(SpritesFolder, "prism_crystal_block.png"));
            Sprite crystalBoostSprite = GenerateBoostCrystalSprite(Path.Combine(SpritesFolder, "crystal_boost.png"));
            Sprite crystalFragileSprite = GenerateFragileCrystalSprite(Path.Combine(SpritesFolder, "crystal_fragile.png"));
            Sprite crystalUmbraSprite = GenerateUmbraCrystalSprite(Path.Combine(SpritesFolder, "crystal_umbra.png"));

            Sprite rimGlowSprite = GenerateCrystalRimSprite(Path.Combine(SpritesFolder, "prism_crystal_rim.png"));
            Sprite starburstSprite = GenerateStarburstSprite(Path.Combine(SpritesFolder, "diamond_starburst.png"));
            Sprite emitterCoreSprite = GenerateEmitterCoreSprite(Path.Combine(SpritesFolder, "flashlight_emitter_core.png"));

            Sprite runnerTorsoSprite = GenerateRunnerTorsoHeadSprite(Path.Combine(SpritesFolder, "runner_torso_head.png"));
            Sprite runnerLimbSprite = GenerateRunnerLimbSprite(Path.Combine(SpritesFolder, "runner_limb.png"));
            Sprite runnerSparkSprite = GenerateSoftDotSprite(Path.Combine(SpritesFolder, "runner_spark_dot.png"));
            Sprite heroAuraSprite = GenerateSoftDotSprite(Path.Combine(SpritesFolder, "hero_aura_glow.png"));
            Texture2D beamGradientTex = GenerateBeamGradientTexture(Path.Combine(SpritesFolder, "volumetric_beam_gradient.png"));

            Sprite hexFrameSprite = GenerateHexFrameSprite(Path.Combine(SpritesFolder, "energy_hex_frame.png"));
            Sprite lightningBoltSprite = GenerateLightningBoltSprite(Path.Combine(SpritesFolder, "energy_bolt_icon.png"));
            Sprite jumpRingSprite = GenerateJumpPadRingSprite(Path.Combine(SpritesFolder, "jump_pad_ring.png"));
            Sprite jumpCoreSprite = GenerateSoftDotSprite(Path.Combine(SpritesFolder, "jump_pad_core.png"));
            Sprite prismDiamondSprite = GeneratePrismRelayDiamondSprite(Path.Combine(SpritesFolder, "prism_relay_diamond.png"));
            Sprite solidBarSprite = GenerateSolidWhiteRectSprite(Path.Combine(SpritesFolder, "diegetic_bar_pixel.png"));

            Sprite shadowPillarSprite = GenerateShadowPillarSprite(Path.Combine(SpritesFolder, "shadow_pillar.png"));
            Sprite umbraStalkerSprite = GenerateUmbraStalkerSprite(Path.Combine(SpritesFolder, "umbra_stalker.png"));
            Sprite voidLeechSprite = GenerateVoidLeechSprite(Path.Combine(SpritesFolder, "void_leech.png"));
            Sprite burstRingSprite = GenerateHollowRingSprite(Path.Combine(SpritesFolder, "flash_burst_ring.png"));

            Sprite gateBarrierSprite = GenerateLaserBarrierSprite(Path.Combine(SpritesFolder, "prism_gate_barrier.png"));
            Sprite anchorRingSprite = GeneratePhotonAnchorSprite(Path.Combine(SpritesFolder, "photon_anchor_ring.png"));
            Sprite eclipseWallSprite = GenerateEclipseWaveSprite(Path.Combine(SpritesFolder, "eclipse_wave_wall.png"));
            Sprite voidMonolithSprite = GenerateHorizonMonolithSprite(Path.Combine(SpritesFolder, "void_horizon_monolith.png"));

            Sprite batteryOutlineSprite = GenerateBatteryFrameSprite(Path.Combine(SpritesFolder, "ui_battery_frame.png"));
            Sprite batteryGradientSprite = GenerateBatteryGradientFillSprite(Path.Combine(SpritesFolder, "ui_battery_gradient.png"));
            Sprite uiPanelSprite = GenerateRoundedRectSprite(Path.Combine(SpritesFolder, "ui_rounded_panel.png"));
            Sprite stickRingSprite = GenerateHollowRingSprite(Path.Combine(SpritesFolder, "ui_joystick_ring.png"));
            Sprite highBeamIconSprite = GenerateHighBeamIconSprite(Path.Combine(SpritesFolder, "ui_highbeam_icon.png"));
            Sprite jumpArrowIconSprite = GenerateJumpArrowIconSprite(Path.Combine(SpritesFolder, "ui_jump_icon.png"));

            AssetDatabase.Refresh();

            SectorProfileSO[] sectorProfiles = CreateOrUpdateSectorProfiles();

            // 2. Kamera & URP Post-Process Bloom
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

            UniversalAdditionalCameraData urpCamData = mainCam.GetUniversalAdditionalCameraData();
            if (urpCamData != null) urpCamData.renderPostProcessing = true;

            CameraFollow2D camFollow = mainCam.GetComponent<CameraFollow2D>();
            if (camFollow == null) camFollow = mainCam.gameObject.AddComponent<CameraFollow2D>();

            // Eski kökleri temizle
            DestroyExistingIfFound("=== LIGHT WEAVER SYSTEMS ===");
            DestroyExistingIfFound("=== LIGHT WEAVER RUNNER ===");
            DestroyExistingIfFound("=== VOID STARFIELD ===");
            DestroyExistingIfFound("=== VOID HORIZON SILHOUETTES ===");
            DestroyExistingIfFound("=== THE ECLIPSE WAVE ===");
            DestroyExistingIfFound("=== LIGHT WEAVER POSTPROCESS ===");
            DestroyExistingIfFound("=== LIGHT WEAVER UI CANVAS ===");
            DestroyExistingIfFound("EventSystem");

            SetupPostProcessVolume();

            // 3. Havuz Prefab'ları
            GameObject platPrefab = SaveCrystalPrefab(crystalStandardSprite, crystalBoostSprite, crystalFragileSprite, crystalUmbraSprite, rimGlowSprite, starburstSprite);
            GameObject orbPrefab = SaveOrbPrefab("PF_EnergyOrb.prefab", false, hexFrameSprite, lightningBoltSprite, heroAuraSprite);
            GameObject superOrbPrefab = SaveOrbPrefab("PF_EnergyOrb_Super.prefab", true, hexFrameSprite, lightningBoltSprite, heroAuraSprite);
            GameObject jumpPadPrefab = SaveJumpPadPrefab(jumpRingSprite, jumpCoreSprite);
            GameObject prismRelayPrefab = SavePrismRelayPrefab(prismDiamondSprite);
            GameObject pillarPrefab = SaveHazardPrefab("PF_ShadowPillar.prefab", ShadowHazardType.MovingShadowPillar, shadowPillarSprite, new Vector2(0.85f, 3.2f));
            GameObject stalkerPrefab = SaveHazardPrefab("PF_UmbraStalker.prefab", ShadowHazardType.UmbraStalker, umbraStalkerSprite, new Vector2(1.1f, 1.1f));
            GameObject leechPrefab = SaveHazardPrefab("PF_VoidLeech.prefab", ShadowHazardType.VoidLeech, voidLeechSprite, new Vector2(1.05f, 1.05f));
            GameObject gatePrefab = SavePrismGatePrefab(gateBarrierSprite, prismDiamondSprite);
            GameObject anchorPrefab = SavePhotonAnchorPrefab(anchorRingSprite);

            // 4. Neon Cyber-Runner
            GameObject runnerGo = BuildRunnerCharacter(
                mainCam,
                camFollow,
                zeroFrictionMat,
                runnerTorsoSprite,
                runnerLimbSprite,
                emitterCoreSprite,
                runnerSparkSprite,
                heroAuraSprite,
                solidBarSprite,
                burstRingSprite,
                beamGradientTex);

            RunnerMotor2D motor = runnerGo.GetComponent<RunnerMotor2D>();
            BatterySystem battery = runnerGo.GetComponent<BatterySystem>();
            LightTouchInput touchInput = runnerGo.GetComponent<LightTouchInput>();
            FlashBurstController flashBurst = runnerGo.GetComponent<FlashBurstController>();
            LightConeCaster2D coneCaster = runnerGo.GetComponentInChildren<LightConeCaster2D>();

            SerializedObject camSo = new SerializedObject(camFollow);
            camSo.FindProperty("target").objectReferenceValue = runnerGo.transform;
            camSo.ApplyModifiedPropertiesWithoutUndo();
            mainCam.transform.position = new Vector3(runnerGo.transform.position.x + 4.6f, runnerGo.transform.position.y + 0.9f, -10f);
            PrefabUtility.SaveAsPrefabAsset(runnerGo, Path.Combine(PrefabsFolder, "PF_LightWeaverRunner.prefab"));

            // 5. Sonsuz Yıldız Alanı & Uzak Monolit Silüetleri
            BuildInfiniteStarfield(mainCam.transform, runnerSparkSprite);
            BuildHorizonSilhouettes(mainCam.transform, voidMonolithSprite);

            // 6. Merkezi Sistemler + WorldVfxManager + Eclipse Dalgası
            GameObject systemsRoot = new GameObject("=== LIGHT WEAVER SYSTEMS ===");
            ObjectPooler pooler = systemsRoot.AddComponent<ObjectPooler>();
            EndlessChunkDirector director = systemsRoot.AddComponent<EndlessChunkDirector>();
            PrismUpgradeManager upgradeMgr = systemsRoot.AddComponent<PrismUpgradeManager>();
            GameFlowManager gameFlow = systemsRoot.AddComponent<GameFlowManager>();
            HarmonicAudioManager audioMgr = systemsRoot.AddComponent<HarmonicAudioManager>();
            JuiceFeedbackManager juiceMgr = systemsRoot.AddComponent<JuiceFeedbackManager>();
            WorldVfxManager worldVfx = systemsRoot.AddComponent<WorldVfxManager>();

            // Zero-GC Global Burst ParticleSystem
            GameObject vfxObj = new GameObject("WorldBurstParticles");
            vfxObj.transform.SetParent(systemsRoot.transform, false);
            ParticleSystem burstPs = CreateGlobalBurstParticleSystem(vfxObj, runnerSparkSprite);
            SerializedObject vfxSo = new SerializedObject(worldVfx);
            vfxSo.FindProperty("runnerMotor").objectReferenceValue = motor;
            vfxSo.FindProperty("batterySystem").objectReferenceValue = battery;
            vfxSo.FindProperty("burstParticleSystem").objectReferenceValue = burstPs;
            vfxSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject eclipseGo = new GameObject("=== THE ECLIPSE WAVE ===");
            eclipseGo.transform.localScale = new Vector3(5.2f, 18.0f, 1f);
            SpriteRenderer eclipseSr = eclipseGo.AddComponent<SpriteRenderer>();
            eclipseSr.sprite = eclipseWallSprite;
            eclipseSr.sortingOrder = 25;
            EclipsePursuitWave eclipseWave = eclipseGo.AddComponent<EclipsePursuitWave>();
            SerializedObject ecSo = new SerializedObject(eclipseWave);
            ecSo.FindProperty("runnerMotor").objectReferenceValue = motor;
            ecSo.FindProperty("batterySystem").objectReferenceValue = battery;
            ecSo.FindProperty("chunkDirector").objectReferenceValue = director;
            ecSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject dirSo = new SerializedObject(director);
            dirSo.FindProperty("pooler").objectReferenceValue = pooler;
            dirSo.FindProperty("runnerTransform").objectReferenceValue = runnerGo.transform;
            dirSo.FindProperty("runnerMotor").objectReferenceValue = motor;
            dirSo.FindProperty("batterySystem").objectReferenceValue = battery;
            dirSo.FindProperty("mainCamera").objectReferenceValue = mainCam;
            dirSo.FindProperty("crystalPlatformPrefab").objectReferenceValue = platPrefab;
            dirSo.FindProperty("energyOrbPrefab").objectReferenceValue = orbPrefab;
            dirSo.FindProperty("energyOrbSuperPrefab").objectReferenceValue = superOrbPrefab;
            dirSo.FindProperty("jumpPadPrefab").objectReferenceValue = jumpPadPrefab;
            dirSo.FindProperty("prismRelayPrefab").objectReferenceValue = prismRelayPrefab;
            dirSo.FindProperty("shadowPillarPrefab").objectReferenceValue = pillarPrefab;
            dirSo.FindProperty("umbraStalkerPrefab").objectReferenceValue = stalkerPrefab;
            dirSo.FindProperty("voidLeechPrefab").objectReferenceValue = leechPrefab;
            dirSo.FindProperty("prismGatePrefab").objectReferenceValue = gatePrefab;
            dirSo.FindProperty("photonAnchorPrefab").objectReferenceValue = anchorPrefab;
            SerializedProperty sectorsProp = dirSo.FindProperty("sectors");
            sectorsProp.arraySize = sectorProfiles.Length;
            for (int i = 0; i < sectorProfiles.Length; i++) sectorsProp.GetArrayElementAtIndex(i).objectReferenceValue = sectorProfiles[i];
            dirSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject upgSo = new SerializedObject(upgradeMgr);
            upgSo.FindProperty("coneCaster").objectReferenceValue = coneCaster;
            upgSo.FindProperty("batterySystem").objectReferenceValue = battery;
            upgSo.FindProperty("runnerMotor").objectReferenceValue = motor;
            upgSo.FindProperty("flashBurst").objectReferenceValue = flashBurst;
            upgSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject flowSo = new SerializedObject(gameFlow);
            flowSo.FindProperty("runnerMotor").objectReferenceValue = motor;
            flowSo.FindProperty("batterySystem").objectReferenceValue = battery;
            flowSo.FindProperty("touchInput").objectReferenceValue = touchInput;
            flowSo.FindProperty("chunkDirector").objectReferenceValue = director;
            flowSo.FindProperty("eclipseWave").objectReferenceValue = eclipseWave;
            flowSo.FindProperty("upgradeManager").objectReferenceValue = upgradeMgr;
            flowSo.FindProperty("cameraFollow").objectReferenceValue = camFollow;
            flowSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject audioSo = new SerializedObject(audioMgr);
            audioSo.FindProperty("runnerMotor").objectReferenceValue = motor;
            audioSo.FindProperty("batterySystem").objectReferenceValue = battery;
            audioSo.FindProperty("flashBurst").objectReferenceValue = flashBurst;
            audioSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject juiceSo = new SerializedObject(juiceMgr);
            juiceSo.FindProperty("cameraFollow").objectReferenceValue = camFollow;
            juiceSo.FindProperty("batterySystem").objectReferenceValue = battery;
            juiceSo.ApplyModifiedPropertiesWithoutUndo();

            // 7. EventSystem & UI Canvas (360° Floating Stick UI + Mobil Butonlar + Aim Mod Seçimi)
            BuildEventSystem();
            BuildUICanvas(
                gameFlow,
                motor,
                touchInput,
                battery,
                flashBurst,
                director,
                eclipseWave,
                audioMgr,
                batteryOutlineSprite,
                batteryGradientSprite,
                uiPanelSprite,
                solidBarSprite,
                stickRingSprite,
                runnerSparkSprite,
                highBeamIconSprite,
                jumpArrowIconSprite);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[LightWeaver Setup] v2.1 Complete: 360° Aim, Floating Mobile Stick, 4 Distinct Crystal Variants, Burst VFX & Parallax Depth Built!");
        }

        private static void ConfigureMobileLandscapeSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
        }

        private static ParticleSystem CreateGlobalBurstParticleSystem(GameObject target, Sprite dotSprite)
        {
            ParticleSystem ps = target.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.55f;
            main.maxParticles = 250;

            var emission = ps.emission;
            emission.enabled = false;

            var renderer = target.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 22;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                Material mat = new Material(shader) { mainTexture = dotSprite.texture };
                renderer.sharedMaterial = mat;
            }
            return ps;
        }

        private static void SetupPostProcessVolume()
        {
            string profilePath = Path.Combine(DataFolder, "LightWeaverPostProcess.asset").Replace("\\", "/");
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.threshold.Override(0.75f);
            bloom.intensity.Override(0.75f);
            bloom.scatter.Override(0.65f);

            if (!profile.TryGet(out Vignette vignette)) vignette = profile.Add<Vignette>(true);
            vignette.active = true;
            vignette.color.Override(new Color(0.01f, 0.02f, 0.06f, 1f));
            vignette.intensity.Override(0.28f);

            EditorUtility.SetDirty(profile);

            GameObject volGo = new GameObject("=== LIGHT WEAVER POSTPROCESS ===");
            Volume vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = profile;
        }

        private static void BuildEventSystem()
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        private static void BuildUICanvas(
            GameFlowManager gameFlow,
            RunnerMotor2D motor,
            LightTouchInput touchInput,
            BatterySystem battery,
            FlashBurstController flashBurst,
            EndlessChunkDirector director,
            EclipsePursuitWave eclipseWave,
            HarmonicAudioManager audioMgr,
            Sprite batteryOutlineSprite,
            Sprite batteryGradientSprite,
            Sprite panelSprite,
            Sprite solidSprite,
            Sprite stickRingSprite,
            Sprite knobDotSprite,
            Sprite highBeamIconSprite,
            Sprite jumpArrowIconSprite)
        {
            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject canvasGo = new GameObject("=== LIGHT WEAVER UI CANVAS ===");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject safeRoot = CreateUIObject("SafeAreaRoot", canvasGo.transform);
            StretchFull(safeRoot.GetComponent<RectTransform>());
            safeRoot.AddComponent<SafeAreaFit>();

            // =================================================================
            // 1. IN-GAME HUD
            // =================================================================
            GameObject hudRoot = CreateUIObject("InGameHUD", safeRoot.transform);
            StretchFull(hudRoot.GetComponent<RectTransform>());

            GameObject batContainer = CreateUIObject("BatteryPanel", hudRoot.transform);
            RectTransform batRt = batContainer.GetComponent<RectTransform>();
            batRt.anchorMin = new Vector2(0f, 1f);
            batRt.anchorMax = new Vector2(0f, 1f);
            batRt.pivot = new Vector2(0f, 1f);
            batRt.anchoredPosition = new Vector2(54f, -40f);
            batRt.sizeDelta = new Vector2(360f, 158f);

            GameObject batFrameObj = CreateUIObject("BatteryFrame", batContainer.transform);
            RectTransform frameRt = batFrameObj.GetComponent<RectTransform>();
            frameRt.anchorMin = new Vector2(0f, 1f);
            frameRt.anchorMax = new Vector2(0f, 1f);
            frameRt.pivot = new Vector2(0f, 1f);
            frameRt.anchoredPosition = Vector2.zero;
            frameRt.sizeDelta = new Vector2(276f, 44f);
            Image frameImg = batFrameObj.AddComponent<Image>();
            frameImg.sprite = batteryOutlineSprite;
            frameImg.color = new Color(0.25f, 0.96f, 1.0f, 0.95f);

            GameObject batFillObj = CreateUIObject("BatteryFill", batFrameObj.transform);
            RectTransform fillRt = batFillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(10f, 8f);
            fillRt.offsetMax = new Vector2(-24f, -8f);
            Image fillImg = batFillObj.AddComponent<Image>();
            fillImg.sprite = batteryGradientSprite;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 1f;

            Text batText = CreateUIText("BatteryLabel", batContainer.transform, builtinFont, "BATTERY: 100%", 23, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.88f, 0.97f, 1f, 0.96f));
            RectTransform batTextRt = batText.GetComponent<RectTransform>();
            batTextRt.anchorMin = new Vector2(0f, 1f);
            batTextRt.anchorMax = new Vector2(0f, 1f);
            batTextRt.pivot = new Vector2(0f, 1f);
            batTextRt.anchoredPosition = new Vector2(2f, -48f);
            batTextRt.sizeDelta = new Vector2(300f, 32f);

            GameObject burstBg = CreateUIObject("FlashBurstBarBg", batContainer.transform);
            RectTransform burstBgRt = burstBg.GetComponent<RectTransform>();
            burstBgRt.anchorMin = new Vector2(0f, 1f);
            burstBgRt.anchorMax = new Vector2(0f, 1f);
            burstBgRt.pivot = new Vector2(0f, 1f);
            burstBgRt.anchoredPosition = new Vector2(2f, -84f);
            burstBgRt.sizeDelta = new Vector2(175f, 8f);
            Image burstBgImg = burstBg.AddComponent<Image>();
            burstBgImg.sprite = solidSprite;
            burstBgImg.color = new Color(0.12f, 0.18f, 0.30f, 0.85f);

            GameObject burstFill = CreateUIObject("FlashBurstBarFill", burstBg.transform);
            StretchFull(burstFill.GetComponent<RectTransform>());
            Image burstFillImg = burstFill.AddComponent<Image>();
            burstFillImg.sprite = solidSprite;
            burstFillImg.color = new Color(0.0f, 0.94f, 1.0f, 1f);
            burstFillImg.type = Image.Type.Filled;
            burstFillImg.fillMethod = Image.FillMethod.Horizontal;

            Text burstLbl = CreateUIText("FlashBurstLabel", batContainer.transform, builtinFont, "FLASH BURST [F]", 13, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.0f, 0.94f, 1.0f, 0.9f));
            RectTransform burstLblRt = burstLbl.GetComponent<RectTransform>();
            burstLblRt.anchorMin = new Vector2(0f, 1f);
            burstLblRt.anchorMax = new Vector2(0f, 1f);
            burstLblRt.pivot = new Vector2(0f, 1f);
            burstLblRt.anchoredPosition = new Vector2(2f, -95f);
            burstLblRt.sizeDelta = new Vector2(260f, 20f);

            GameObject eclipseBg = CreateUIObject("EclipseBarBg", batContainer.transform);
            RectTransform ecBgRt = eclipseBg.GetComponent<RectTransform>();
            ecBgRt.anchorMin = new Vector2(0f, 1f);
            ecBgRt.anchorMax = new Vector2(0f, 1f);
            ecBgRt.pivot = new Vector2(0f, 1f);
            ecBgRt.anchoredPosition = new Vector2(2f, -120f);
            ecBgRt.sizeDelta = new Vector2(175f, 8f);
            Image ecBgImg = eclipseBg.AddComponent<Image>();
            ecBgImg.sprite = solidSprite;
            ecBgImg.color = new Color(0.18f, 0.06f, 0.22f, 0.85f);

            GameObject eclipseFill = CreateUIObject("EclipseBarFill", eclipseBg.transform);
            StretchFull(eclipseFill.GetComponent<RectTransform>());
            Image ecFillImg = eclipseFill.AddComponent<Image>();
            ecFillImg.sprite = solidSprite;
            ecFillImg.color = new Color(0.85f, 0.15f, 0.45f, 1f);
            ecFillImg.type = Image.Type.Filled;
            ecFillImg.fillMethod = Image.FillMethod.Horizontal;

            Text ecLbl = CreateUIText("EclipseLabel", batContainer.transform, builtinFont, "THE ECLIPSE PROXIMITY", 13, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.95f, 0.35f, 0.65f, 0.9f));
            RectTransform ecLblRt = ecLbl.GetComponent<RectTransform>();
            ecLblRt.anchorMin = new Vector2(0f, 1f);
            ecLblRt.anchorMax = new Vector2(0f, 1f);
            ecLblRt.pivot = new Vector2(0f, 1f);
            ecLblRt.anchoredPosition = new Vector2(2f, -131f);
            ecLblRt.sizeDelta = new Vector2(260f, 20f);

            GameObject scoreContainer = CreateUIObject("ScorePanel", hudRoot.transform);
            RectTransform scoreRt = scoreContainer.GetComponent<RectTransform>();
            scoreRt.anchorMin = new Vector2(1f, 1f);
            scoreRt.anchorMax = new Vector2(1f, 1f);
            scoreRt.pivot = new Vector2(1f, 1f);
            scoreRt.anchoredPosition = new Vector2(-132f, -40f);
            scoreRt.sizeDelta = new Vector2(540f, 130f);

            Text sectorText = CreateUIText("SectorTitle", scoreContainer.transform, builtinFont, "LEVEL 1 - <color=#00F0FF>THE SILENT ABYSS</color>", 23, FontStyle.Bold, TextAnchor.UpperRight, new Color(0.86f, 0.90f, 0.96f, 1f));
            sectorText.supportRichText = true;
            RectTransform secRt = sectorText.GetComponent<RectTransform>();
            secRt.anchorMin = new Vector2(1f, 1f);
            secRt.anchorMax = new Vector2(1f, 1f);
            secRt.pivot = new Vector2(1f, 1f);
            secRt.anchoredPosition = Vector2.zero;
            secRt.sizeDelta = new Vector2(540f, 34f);

            Text scoreText = CreateUIText("ScoreValue", scoreContainer.transform, builtinFont, "0", 54, FontStyle.Bold, TextAnchor.UpperRight, Color.white);
            RectTransform scValRt = scoreText.GetComponent<RectTransform>();
            scValRt.anchorMin = new Vector2(1f, 1f);
            scValRt.anchorMax = new Vector2(1f, 1f);
            scValRt.pivot = new Vector2(1f, 1f);
            scValRt.anchoredPosition = new Vector2(0f, -34f);
            scValRt.sizeDelta = new Vector2(460f, 68f);

            Text multText = CreateUIText("MultiplierBadge", scoreContainer.transform, builtinFont, "PRISM OVERCHARGE 3X", 18, FontStyle.Bold, TextAnchor.UpperRight, new Color(0.45f, 1f, 0.85f, 1f));
            RectTransform multRt = multText.GetComponent<RectTransform>();
            multRt.anchorMin = new Vector2(1f, 1f);
            multRt.anchorMax = new Vector2(1f, 1f);
            multRt.pivot = new Vector2(1f, 1f);
            multRt.anchoredPosition = new Vector2(0f, -104f);
            multRt.sizeDelta = new Vector2(360f, 26f);
            multText.gameObject.SetActive(false);

            Button pauseBtn = CreateNeonButton("PauseButton", hudRoot.transform, panelSprite, builtinFont, "II", 26, new Color(0.10f, 0.16f, 0.28f, 0.88f), Color.white, new Vector2(64f, 64f));
            RectTransform pauseRt = pauseBtn.GetComponent<RectTransform>();
            pauseRt.anchorMin = new Vector2(1f, 1f);
            pauseRt.anchorMax = new Vector2(1f, 1f);
            pauseRt.pivot = new Vector2(1f, 1f);
            pauseRt.anchoredPosition = new Vector2(-46f, -40f);

            // Sol Alt: Büyük Başparmak JUMP + Yanında Uzunu Gösteren Fener (LONG BEAM) + Üstte BURST (Sağ ekran tamamen 360° Nişan!)
            Button jumpBtn = CreateTactileIconButton(
                "MobileJumpBtn",
                hudRoot.transform,
                panelSprite,
                jumpArrowIconSprite,
                builtinFont,
                "JUMP",
                new Vector2(46f, 38f),
                new Vector2(168f, 128f),
                new Vector2(56f, 56f),
                new Color(0.06f, 0.24f, 0.36f, 0.62f),
                new Color(0.0f, 0.94f, 1.0f, 0.72f),
                new Color(0.75f, 0.98f, 1f, 1f),
                Color.white,
                out Image _,
                out Image _,
                out Text _);

            Button highBeamBtn = CreateTactileIconButton(
                "MobileHighBeamBtn",
                hudRoot.transform,
                panelSprite,
                highBeamIconSprite,
                builtinFont,
                "LONG: OFF",
                new Vector2(228f, 38f),
                new Vector2(152f, 128f),
                new Vector2(72f, 56f),
                new Color(0.09f, 0.16f, 0.28f, 0.56f),
                new Color(1.0f, 0.78f, 0.18f, 0.65f),
                new Color(0.45f, 0.94f, 1.0f, 0.92f),
                new Color(0.90f, 0.96f, 1.0f, 0.95f),
                out Image highBeamBgImg,
                out Image highBeamIconImg,
                out Text highBeamLabelTxt);

            Button burstBtn = CreateNeonButton("MobileBurstBtn", hudRoot.transform, panelSprite, builtinFont, "BURST [F]", 17, new Color(0.32f, 0.12f, 0.48f, 0.56f), Color.white, new Vector2(168f, 52f));
            RectTransform brsRt = burstBtn.GetComponent<RectTransform>();
            brsRt.anchorMin = new Vector2(0f, 0f);
            brsRt.anchorMax = new Vector2(0f, 0f);
            brsRt.pivot = new Vector2(0f, 0f);
            brsRt.anchoredPosition = new Vector2(46f, 180f);
            Outline burstOutline = burstBtn.gameObject.AddComponent<Outline>();
            burstOutline.effectColor = new Color(0.76f, 0.30f, 1.0f, 0.65f);
            burstOutline.effectDistance = new Vector2(2f, -2f);

            // Mobil 360° Floating Virtual Stick UI (Sağ elde parmak dokununca çıkar)
            GameObject stickBaseObj = CreateUIObject("FloatingAimStickBase", hudRoot.transform);
            RectTransform stickBaseRt = stickBaseObj.GetComponent<RectTransform>();
            stickBaseRt.sizeDelta = new Vector2(190f, 190f);
            Image stickBaseImg = stickBaseObj.AddComponent<Image>();
            stickBaseImg.sprite = stickRingSprite;
            stickBaseImg.color = new Color(0.0f, 0.94f, 1.0f, 0.38f);
            stickBaseImg.raycastTarget = false;

            GameObject stickKnobObj = CreateUIObject("FloatingAimStickKnob", stickBaseObj.transform);
            RectTransform stickKnobRt = stickKnobObj.GetComponent<RectTransform>();
            stickKnobRt.sizeDelta = new Vector2(68f, 68f);
            Image stickKnobImg = stickKnobObj.AddComponent<Image>();
            stickKnobImg.sprite = knobDotSprite;
            stickKnobImg.color = new Color(0.25f, 0.98f, 1.0f, 0.85f);
            stickKnobImg.raycastTarget = false;
            stickBaseObj.SetActive(false);

            SerializedObject touchSo = new SerializedObject(touchInput);
            touchSo.FindProperty("joystickBaseRect").objectReferenceValue = stickBaseRt;
            touchSo.FindProperty("joystickKnobRect").objectReferenceValue = stickKnobRt;
            touchSo.ApplyModifiedPropertiesWithoutUndo();

            // Orta-Üst Bildirim Bandı
            GameObject bannerObj = CreateUIObject("BannerOverlay", hudRoot.transform);
            RectTransform bannerRt = bannerObj.GetComponent<RectTransform>();
            bannerRt.anchorMin = new Vector2(0.5f, 1f);
            bannerRt.anchorMax = new Vector2(0.5f, 1f);
            bannerRt.pivot = new Vector2(0.5f, 1f);
            bannerRt.anchoredPosition = new Vector2(0f, -54f);
            bannerRt.sizeDelta = new Vector2(720f, 48f);
            CanvasGroup bannerCg = bannerObj.AddComponent<CanvasGroup>();
            bannerCg.alpha = 0f;
            Text bannerTxt = CreateUIText("BannerText", bannerObj.transform, builtinFont, "", 26, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0f, 0.94f, 1f, 1f));
            StretchFull(bannerTxt.GetComponent<RectTransform>());

            LightWeaverHUD hudComp = canvasGo.AddComponent<LightWeaverHUD>();
            SerializedObject hudSo = new SerializedObject(hudComp);
            hudSo.FindProperty("gameFlow").objectReferenceValue = gameFlow;
            hudSo.FindProperty("runnerMotor").objectReferenceValue = motor;
            hudSo.FindProperty("touchInput").objectReferenceValue = touchInput;
            hudSo.FindProperty("batterySystem").objectReferenceValue = battery;
            hudSo.FindProperty("flashBurst").objectReferenceValue = flashBurst;
            hudSo.FindProperty("chunkDirector").objectReferenceValue = director;
            hudSo.FindProperty("eclipseWave").objectReferenceValue = eclipseWave;
            hudSo.FindProperty("hudRoot").objectReferenceValue = hudRoot;
            hudSo.FindProperty("pauseButton").objectReferenceValue = pauseBtn;
            hudSo.FindProperty("batteryFillImage").objectReferenceValue = fillImg;
            hudSo.FindProperty("batteryPercentText").objectReferenceValue = batText;
            hudSo.FindProperty("flashBurstFillImage").objectReferenceValue = burstFillImg;
            hudSo.FindProperty("flashBurstLabel").objectReferenceValue = burstLbl;
            hudSo.FindProperty("eclipseDangerFillImage").objectReferenceValue = ecFillImg;
            hudSo.FindProperty("sectorTitleText").objectReferenceValue = sectorText;
            hudSo.FindProperty("scoreValueText").objectReferenceValue = scoreText;
            hudSo.FindProperty("multiplierBadgeText").objectReferenceValue = multText;
            hudSo.FindProperty("bannerCanvasGroup").objectReferenceValue = bannerCg;
            hudSo.FindProperty("bannerText").objectReferenceValue = bannerTxt;
            hudSo.FindProperty("mobileJumpButton").objectReferenceValue = jumpBtn;
            hudSo.FindProperty("mobileHighBeamButton").objectReferenceValue = highBeamBtn;
            hudSo.FindProperty("mobileHighBeamBg").objectReferenceValue = highBeamBgImg;
            hudSo.FindProperty("mobileHighBeamIcon").objectReferenceValue = highBeamIconImg;
            hudSo.FindProperty("mobileHighBeamLabel").objectReferenceValue = highBeamLabelTxt;
            hudSo.FindProperty("mobileBurstButton").objectReferenceValue = burstBtn;
            hudSo.FindProperty("mobileFocusTrigger").objectReferenceValue = null;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            // =================================================================
            // 2. ANA MENÜ (360° Kontroller & Mobil Aim Modu Seçimi Dahil)
            // =================================================================
            GameObject mainMenuPanel = CreateModalCard("MainMenuPanel", safeRoot.transform, panelSprite, new Vector2(860f, 690f), new Color(0.03f, 0.05f, 0.11f, 0.90f));
            Text titleTxt = CreateUIText("Title", mainMenuPanel.transform, builtinFont, "LIGHT WEAVER", 62, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.0f, 0.94f, 1.0f, 1f));
            SetAnchoredPos(titleTxt.rectTransform, new Vector2(0f, 250f), new Vector2(720f, 76f));

            Text subTxt = CreateUIText("Subtitle", mainMenuPanel.transform, builtinFont, "360° LIGHT AIM: MOUSE / WASD / STICK  •  JUMP: SPACE  •  BURST: F", 17, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.85f, 0.92f, 1f, 0.88f));
            SetAnchoredPos(subTxt.rectTransform, new Vector2(0f, 192f), new Vector2(800f, 34f));

            Text bestTxt = CreateUIText("BestScoreText", mainMenuPanel.transform, builtinFont, "BEST: 0   |   0m", 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1.0f, 0.84f, 0.18f, 1f));
            SetAnchoredPos(bestTxt.rectTransform, new Vector2(0f, 138f), new Vector2(640f, 36f));

            Button[] sectorBtns = new Button[5];
            for (int i = 0; i < 5; i++)
            {
                Button sBtn = CreateNeonButton($"SectorBtn_{i + 1}", mainMenuPanel.transform, panelSprite, builtinFont, $"L{i + 1}", 22, new Color(0.10f, 0.16f, 0.28f, 0.85f), Color.white, new Vector2(96f, 52f));
                SetAnchoredPos(sBtn.GetComponent<RectTransform>(), new Vector2((i - 2) * 112f, 62f), new Vector2(96f, 52f));
                sectorBtns[i] = sBtn;
            }

            Text secInfoTxt = CreateUIText("SelectedSectorInfo", mainMenuPanel.transform, builtinFont, "LEVEL 1 - THE SILENT ABYSS", 20, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.0f, 0.94f, 1.0f, 0.95f));
            SetAnchoredPos(secInfoTxt.rectTransform, new Vector2(0f, 6f), new Vector2(620f, 32f));

            Button playBtn = CreateNeonButton("PlayButton", mainMenuPanel.transform, panelSprite, builtinFont, "PLAY", 34, new Color(0.0f, 0.82f, 0.95f, 1f), new Color(0.02f, 0.05f, 0.10f, 1f), new Vector2(360f, 82f));
            SetAnchoredPos(playBtn.GetComponent<RectTransform>(), new Vector2(0f, -82f), new Vector2(360f, 82f));

            Button musicBtn = CreateNeonButton("MusicToggleBtn", mainMenuPanel.transform, panelSprite, builtinFont, "MUSIC: ON", 19, new Color(0.10f, 0.16f, 0.28f, 0.9f), Color.white, new Vector2(215f, 54f));
            SetAnchoredPos(musicBtn.GetComponent<RectTransform>(), new Vector2(-240f, -205f), new Vector2(215f, 54f));

            Button sfxBtn = CreateNeonButton("SfxToggleBtn", mainMenuPanel.transform, panelSprite, builtinFont, "ASMR SFX: ON", 19, new Color(0.10f, 0.16f, 0.28f, 0.9f), Color.white, new Vector2(215f, 54f));
            SetAnchoredPos(sfxBtn.GetComponent<RectTransform>(), new Vector2(0f, -205f), new Vector2(215f, 54f));

            Button aimBtn = CreateNeonButton("AimModeToggleBtn", mainMenuPanel.transform, panelSprite, builtinFont, "AIM: 360 STICK", 19, new Color(0.10f, 0.20f, 0.34f, 0.9f), new Color(0.45f, 0.96f, 1f, 1f), new Vector2(230f, 54f));
            SetAnchoredPos(aimBtn.GetComponent<RectTransform>(), new Vector2(245f, -205f), new Vector2(230f, 54f));

            // =================================================================
            // 3. PAUSE, GAME OVER & BLESSING CARDS
            // =================================================================
            GameObject pausePanel = CreateModalCard("PausePanel", safeRoot.transform, panelSprite, new Vector2(520f, 440f), new Color(0.03f, 0.05f, 0.11f, 0.94f));
            Text pauseTitle = CreateUIText("PauseTitle", pausePanel.transform, builtinFont, "PAUSED", 46, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.0f, 0.94f, 1.0f, 1f));
            SetAnchoredPos(pauseTitle.rectTransform, new Vector2(0f, 145f), new Vector2(440f, 60f));

            Button resumeBtn = CreateNeonButton("ResumeBtn", pausePanel.transform, panelSprite, builtinFont, "RESUME", 26, new Color(0.0f, 0.82f, 0.95f, 1f), new Color(0.02f, 0.05f, 0.10f, 1f), new Vector2(300f, 66f));
            SetAnchoredPos(resumeBtn.GetComponent<RectTransform>(), new Vector2(0f, 40f), new Vector2(300f, 66f));

            Button pauseRestartBtn = CreateNeonButton("PauseRestartBtn", pausePanel.transform, panelSprite, builtinFont, "RESTART", 24, new Color(0.14f, 0.22f, 0.36f, 1f), Color.white, new Vector2(300f, 60f));
            SetAnchoredPos(pauseRestartBtn.GetComponent<RectTransform>(), new Vector2(0f, -45f), new Vector2(300f, 60f));

            Button pauseMenuBtn = CreateNeonButton("PauseMenuBtn", pausePanel.transform, panelSprite, builtinFont, "MAIN MENU", 22, new Color(0.12f, 0.14f, 0.24f, 1f), Color.white, new Vector2(300f, 56f));
            SetAnchoredPos(pauseMenuBtn.GetComponent<RectTransform>(), new Vector2(0f, -125f), new Vector2(300f, 56f));
            pausePanel.SetActive(false);

            GameObject gameOverPanel = CreateModalCard("GameOverPanel", safeRoot.transform, panelSprite, new Vector2(640f, 520f), new Color(0.04f, 0.03f, 0.09f, 0.94f));
            Text goTitle = CreateUIText("GameOverTitle", gameOverPanel.transform, builtinFont, "LOST IN THE VOID", 42, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1.0f, 0.22f, 0.40f, 1f));
            SetAnchoredPos(goTitle.rectTransform, new Vector2(0f, 185f), new Vector2(560f, 58f));

            Text goScoreTxt = CreateUIText("GameOverScore", gameOverPanel.transform, builtinFont, "SCORE: 0", 34, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            SetAnchoredPos(goScoreTxt.rectTransform, new Vector2(0f, 115f), new Vector2(540f, 46f));

            Text goDistTxt = CreateUIText("GameOverDistance", gameOverPanel.transform, builtinFont, "DISTANCE: 0m  |  LEVEL 1", 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.0f, 0.94f, 1.0f, 0.95f));
            SetAnchoredPos(goDistTxt.rectTransform, new Vector2(0f, 66f), new Vector2(540f, 34f));

            Text newBestObj = CreateUIText("NewBestBadge", gameOverPanel.transform, builtinFont, "★ NEW RECORD! ★", 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1.0f, 0.85f, 0.15f, 1f));
            SetAnchoredPos(newBestObj.rectTransform, new Vector2(0f, 26f), new Vector2(360f, 30f));
            newBestObj.gameObject.SetActive(false);

            Button solarReviveBtn = CreateNeonButton("SolarReviveBtn", gameOverPanel.transform, panelSprite, builtinFont, "SOLAR REVIVE (+100% ENERGY)", 22, new Color(1.0f, 0.72f, 0.08f, 1f), new Color(0.05f, 0.04f, 0.08f, 1f), new Vector2(450f, 68f));
            SetAnchoredPos(solarReviveBtn.GetComponent<RectTransform>(), new Vector2(0f, -45f), new Vector2(450f, 68f));

            Button retryBtn = CreateNeonButton("RetryBtn", gameOverPanel.transform, panelSprite, builtinFont, "RETRY", 26, new Color(0.0f, 0.82f, 0.95f, 1f), new Color(0.02f, 0.05f, 0.10f, 1f), new Vector2(210f, 62f));
            SetAnchoredPos(retryBtn.GetComponent<RectTransform>(), new Vector2(-114f, -142f), new Vector2(210f, 62f));

            Button goMenuBtn = CreateNeonButton("GameOverMenuBtn", gameOverPanel.transform, panelSprite, builtinFont, "MENU", 24, new Color(0.14f, 0.20f, 0.34f, 1f), Color.white, new Vector2(210f, 62f));
            SetAnchoredPos(goMenuBtn.GetComponent<RectTransform>(), new Vector2(114f, -142f), new Vector2(210f, 62f));
            gameOverPanel.SetActive(false);

            GameObject blessingPanel = CreateModalCard("BlessingPanel", safeRoot.transform, panelSprite, new Vector2(1120f, 520f), new Color(0.03f, 0.05f, 0.12f, 0.95f));
            Text blessHdr = CreateUIText("BlessingHeader", blessingPanel.transform, builtinFont, "SECTOR CLEARED! CHOOSE A PRISM BLESSING", 34, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.0f, 0.96f, 1.0f, 1f));
            SetAnchoredPos(blessHdr.rectTransform, new Vector2(0f, 185f), new Vector2(960f, 52f));

            Button[] cardBtns = new Button[3];
            Text[] cardTitles = new Text[3];
            Text[] cardDescs = new Text[3];

            for (int i = 0; i < 3; i++)
            {
                Button cardBtn = CreateNeonButton($"BlessingCard_{i}", blessingPanel.transform, panelSprite, builtinFont, "", 22, new Color(0.09f, 0.15f, 0.28f, 0.95f), Color.white, new Vector2(315f, 280f));
                SetAnchoredPos(cardBtn.GetComponent<RectTransform>(), new Vector2((i - 1) * 345f, -30f), new Vector2(315f, 280f));

                Text cTitle = CreateUIText("CardTitle", cardBtn.transform, builtinFont, "BLESSING", 23, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1.0f, 0.88f, 0.22f, 1f));
                SetAnchoredPos(cTitle.rectTransform, new Vector2(0f, 55f), new Vector2(280f, 70f));

                Text cDesc = CreateUIText("CardDesc", cardBtn.transform, builtinFont, "Description", 20, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.88f, 0.96f, 1f, 0.95f));
                SetAnchoredPos(cDesc.rectTransform, new Vector2(0f, -40f), new Vector2(275f, 120f));

                cardBtns[i] = cardBtn;
                cardTitles[i] = cTitle;
                cardDescs[i] = cDesc;
            }
            blessingPanel.SetActive(false);

            MenuScreensUI menus = canvasGo.AddComponent<MenuScreensUI>();
            SerializedObject menuSo = new SerializedObject(menus);
            menuSo.FindProperty("gameFlow").objectReferenceValue = gameFlow;
            menuSo.FindProperty("audioManager").objectReferenceValue = audioMgr;
            menuSo.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanel;
            menuSo.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            menuSo.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            menuSo.FindProperty("blessingPanel").objectReferenceValue = blessingPanel;

            menuSo.FindProperty("playButton").objectReferenceValue = playBtn;
            menuSo.FindProperty("mainBestScoreText").objectReferenceValue = bestTxt;
            SerializedProperty secBtnsProp = menuSo.FindProperty("sectorSelectButtons");
            secBtnsProp.arraySize = sectorBtns.Length;
            for (int i = 0; i < sectorBtns.Length; i++) secBtnsProp.GetArrayElementAtIndex(i).objectReferenceValue = sectorBtns[i];
            menuSo.FindProperty("selectedSectorInfoText").objectReferenceValue = secInfoTxt;
            menuSo.FindProperty("musicToggleButton").objectReferenceValue = musicBtn;
            menuSo.FindProperty("musicToggleLabel").objectReferenceValue = musicBtn.GetComponentInChildren<Text>();
            menuSo.FindProperty("sfxToggleButton").objectReferenceValue = sfxBtn;
            menuSo.FindProperty("sfxToggleLabel").objectReferenceValue = sfxBtn.GetComponentInChildren<Text>();
            menuSo.FindProperty("aimModeToggleButton").objectReferenceValue = aimBtn;
            menuSo.FindProperty("aimModeToggleLabel").objectReferenceValue = aimBtn.GetComponentInChildren<Text>();

            menuSo.FindProperty("resumeButton").objectReferenceValue = resumeBtn;
            menuSo.FindProperty("pauseRestartButton").objectReferenceValue = pauseRestartBtn;
            menuSo.FindProperty("pauseMenuButton").objectReferenceValue = pauseMenuBtn;

            menuSo.FindProperty("gameOverScoreText").objectReferenceValue = goScoreTxt;
            menuSo.FindProperty("gameOverDistanceText").objectReferenceValue = goDistTxt;
            menuSo.FindProperty("newBestBadge").objectReferenceValue = newBestObj.gameObject;
            menuSo.FindProperty("solarReviveButton").objectReferenceValue = solarReviveBtn;
            menuSo.FindProperty("retryButton").objectReferenceValue = retryBtn;
            menuSo.FindProperty("gameOverMenuButton").objectReferenceValue = goMenuBtn;

            SerializedProperty cbProp = menuSo.FindProperty("blessingCardButtons");
            SerializedProperty ctProp = menuSo.FindProperty("blessingCardTitles");
            SerializedProperty cdProp = menuSo.FindProperty("blessingCardDescriptions");
            cbProp.arraySize = 3;
            ctProp.arraySize = 3;
            cdProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                cbProp.GetArrayElementAtIndex(i).objectReferenceValue = cardBtns[i];
                ctProp.GetArrayElementAtIndex(i).objectReferenceValue = cardTitles[i];
                cdProp.GetArrayElementAtIndex(i).objectReferenceValue = cardDescs[i];
            }
            menuSo.ApplyModifiedPropertiesWithoutUndo();
        }

        #region UI Builder Helpers

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchoredPos(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static Text CreateUIText(string name, Transform parent, Font font, string content, int fontSize, FontStyle style, TextAnchor align, Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            Text txt = go.AddComponent<Text>();
            txt.font = font;
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = align;
            txt.color = color;
            txt.raycastTarget = false;

            Shadow shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return txt;
        }

        private static GameObject CreateModalCard(string name, Transform parent, Sprite bgSprite, Vector2 size, Color bgColor)
        {
            GameObject card = CreateUIObject(name, parent);
            RectTransform rt = card.GetComponent<RectTransform>();
            SetAnchoredPos(rt, Vector2.zero, size);

            Image bg = card.AddComponent<Image>();
            bg.sprite = bgSprite;
            bg.type = Image.Type.Sliced;
            bg.color = bgColor;

            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0.0f, 0.94f, 1.0f, 0.45f);
            outline.effectDistance = new Vector2(2f, -2f);
            return card;
        }

        private static Button CreateNeonButton(string name, Transform parent, Sprite bgSprite, Font font, string label, int fontSize, Color bgColor, Color textColor, Vector2 size)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            Image img = go.AddComponent<Image>();
            img.sprite = bgSprite;
            img.type = Image.Type.Sliced;
            img.color = bgColor;

            Button btn = go.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            btn.colors = cb;

            Text txt = CreateUIText("Label", go.transform, font, label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, textColor);
            StretchFull(txt.GetComponent<RectTransform>());
            return btn;
        }

        private static Button CreateTactileIconButton(
            string name,
            Transform parent,
            Sprite bgSprite,
            Sprite iconSprite,
            Font font,
            string label,
            Vector2 anchoredPos,
            Vector2 buttonSize,
            Vector2 iconSize,
            Color bgColor,
            Color outlineColor,
            Color iconColor,
            Color textColor,
            out Image bgImage,
            out Image iconImage,
            out Text labelText)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = buttonSize;

            bgImage = go.AddComponent<Image>();
            bgImage.sprite = bgSprite;
            bgImage.type = Image.Type.Sliced;
            bgImage.color = bgColor;

            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(2.5f, -2.5f);

            Button btn = go.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            btn.colors = cb;

            GameObject iconObj = CreateUIObject("Icon", go.transform);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            SetAnchoredPos(iconRt, new Vector2(0f, 16f), iconSize);
            iconImage = iconObj.AddComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.preserveAspect = true;
            iconImage.color = iconColor;
            iconImage.raycastTarget = false;

            labelText = CreateUIText("Label", go.transform, font, label, 18, FontStyle.Bold, TextAnchor.LowerCenter, textColor);
            RectTransform lblRt = labelText.GetComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = new Vector2(1f, 0f);
            lblRt.pivot = new Vector2(0.5f, 0f);
            lblRt.anchoredPosition = new Vector2(0f, 12f);
            lblRt.sizeDelta = new Vector2(-12f, 34f);

            return btn;
        }

        #endregion

        private static SectorProfileSO[] CreateOrUpdateSectorProfiles()
        {
            var specs = new (int num, string title, float dist, Color bg, Color accent, Color sec, float speed, float drain, float haz, float prism)[]
            {
                (1, "THE SILENT ABYSS",     90f,  new Color(0.020f, 0.027f, 0.051f, 1f), new Color(0.00f, 0.94f, 1.00f, 1f), new Color(0.35f, 0.55f, 1.00f, 1f), 1.00f, 0.85f, 0.14f, 0.15f),
                (2, "THE PRISM CAVERNS",    115f, new Color(0.025f, 0.020f, 0.065f, 1f), new Color(0.00f, 0.94f, 1.00f, 1f), new Color(0.71f, 0.22f, 0.95f, 1f), 1.07f, 1.00f, 0.25f, 0.30f),
                (3, "THE UMBRA SWARM",      125f, new Color(0.035f, 0.022f, 0.055f, 1f), new Color(1.00f, 0.72f, 0.00f, 1f), new Color(0.55f, 0.15f, 0.85f, 1f), 1.14f, 1.15f, 0.36f, 0.24f),
                (4, "THE ECHO VOID",        135f, new Color(0.018f, 0.030f, 0.062f, 1f), new Color(0.00f, 0.96f, 0.92f, 1f), new Color(1.00f, 0.78f, 0.18f, 1f), 1.22f, 1.28f, 0.44f, 0.28f),
                (5, "THE SINGULARITY CORE", 150f, new Color(0.045f, 0.015f, 0.040f, 1f), new Color(1.00f, 0.12f, 0.34f, 1f), new Color(0.00f, 0.94f, 1.00f, 1f), 1.32f, 1.42f, 0.52f, 0.30f),
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
                profile.orbDensity = 0.85f;

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
            Sprite auraSprite,
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

            GameObject auraObj = new GameObject("HeroCyanAura");
            auraObj.transform.SetParent(visualRoot.transform, false);
            auraObj.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            auraObj.transform.localScale = new Vector3(2.2f, 2.6f, 1f);
            SpriteRenderer auraSr = auraObj.AddComponent<SpriteRenderer>();
            auraSr.sprite = auraSprite;
            auraSr.color = new Color(0.0f, 0.88f, 1.0f, 0.22f);
            auraSr.sortingOrder = 9;

            Transform backArmPivot = CreateLimbPivot("BackArmPivot", visualRoot.transform, new Vector3(-0.08f, 0.26f, 0f), new Vector3(0.20f, 0.52f, 1f), new Vector3(0f, -0.22f, 0f), limbSprite, new Color(0.10f, 0.52f, 0.78f, 1f), 11);
            Transform backLegPivot = CreateLimbPivot("BackLegPivot", visualRoot.transform, new Vector3(-0.04f, -0.14f, 0f), new Vector3(0.26f, 0.58f, 1f), new Vector3(0f, -0.24f, 0f), limbSprite, new Color(0.10f, 0.54f, 0.82f, 1f), 11);

            GameObject bodyPivot = new GameObject("BodyPivot");
            bodyPivot.transform.SetParent(visualRoot.transform, false);
            GameObject torsoGo = new GameObject("TorsoHeadSprite");
            torsoGo.transform.SetParent(bodyPivot.transform, false);
            torsoGo.transform.localPosition = new Vector3(0f, 0.30f, 0f);
            torsoGo.transform.localScale = new Vector3(0.95f, 1.08f, 1f);
            SpriteRenderer torsoSr = torsoGo.AddComponent<SpriteRenderer>();
            torsoSr.sprite = torsoSprite;
            torsoSr.sortingOrder = 13;

            GameObject gaugeBg = new GameObject("SuitBatteryGaugeBg");
            gaugeBg.transform.SetParent(bodyPivot.transform, false);
            gaugeBg.transform.localPosition = new Vector3(-0.25f, 0.26f, 0f);
            gaugeBg.transform.localScale = new Vector3(0.075f, 0.44f, 1f);
            SpriteRenderer gaugeBgSr = gaugeBg.AddComponent<SpriteRenderer>();
            gaugeBgSr.sprite = barSprite;
            gaugeBgSr.color = new Color(0.04f, 0.08f, 0.14f, 0.92f);
            gaugeBgSr.sortingOrder = 14;

            GameObject gaugeFillPivot = new GameObject("SuitBatteryGaugeFillPivot");
            gaugeFillPivot.transform.SetParent(gaugeBg.transform, false);
            gaugeFillPivot.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            GameObject gaugeFillBar = new GameObject("FillBar");
            gaugeFillBar.transform.SetParent(gaugeFillPivot.transform, false);
            gaugeFillBar.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            gaugeFillBar.transform.localScale = new Vector3(0.74f, 0.96f, 1f);
            SpriteRenderer gaugeFillSr = gaugeFillBar.AddComponent<SpriteRenderer>();
            gaugeFillSr.sprite = barSprite;
            gaugeFillSr.color = new Color(0f, 0.96f, 1f, 1f);
            gaugeFillSr.sortingOrder = 15;

            Transform frontLegPivot = CreateLimbPivot("FrontLegPivot", visualRoot.transform, new Vector3(0.06f, -0.14f, 0f), new Vector3(0.28f, 0.60f, 1f), new Vector3(0f, -0.25f, 0f), limbSprite, new Color(0.28f, 0.96f, 1f, 1f), 14);

            GameObject flashlightArmPivot = new GameObject("FlashlightArmPivot");
            flashlightArmPivot.transform.SetParent(visualRoot.transform, false);
            flashlightArmPivot.transform.localPosition = new Vector3(0.08f, 0.28f, 0f);

            GameObject frontArmSpriteObj = new GameObject("FrontArmSprite");
            frontArmSpriteObj.transform.SetParent(flashlightArmPivot.transform, false);
            frontArmSpriteObj.transform.localPosition = new Vector3(0.23f, 0f, 0f);
            frontArmSpriteObj.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            frontArmSpriteObj.transform.localScale = new Vector3(0.20f, 0.52f, 1f);
            SpriteRenderer frontArmSr = frontArmSpriteObj.AddComponent<SpriteRenderer>();
            frontArmSr.sprite = limbSprite;
            frontArmSr.color = new Color(0.35f, 0.98f, 1f, 1f);
            frontArmSr.sortingOrder = 15;

            GameObject emitterTip = new GameObject("FlashlightEmitterTip");
            emitterTip.transform.SetParent(flashlightArmPivot.transform, false);
            emitterTip.transform.localPosition = new Vector3(0.48f, 0f, 0f);
            emitterTip.transform.localScale = Vector3.one;

            GameObject emitterCoreObj = new GameObject("EmitterCoreGlow");
            emitterCoreObj.transform.SetParent(emitterTip.transform, false);
            emitterCoreObj.transform.localScale = new Vector3(0.34f, 0.34f, 1f);
            SpriteRenderer emitterSr = emitterCoreObj.AddComponent<SpriteRenderer>();
            emitterSr.sprite = emitterCoreSprite;
            emitterSr.color = new Color(1f, 0.96f, 0.72f, 0.95f);
            emitterSr.sortingOrder = 21;

            GameObject coneObj = new GameObject("LightConeCaster2D");
            coneObj.transform.SetParent(emitterTip.transform, false);
            coneObj.transform.localScale = Vector3.one;
            LightConeCaster2D coneCaster = coneObj.AddComponent<LightConeCaster2D>();
            SerializedObject coneSo = new SerializedObject(coneCaster);
            coneSo.FindProperty("beamGradientTexture").objectReferenceValue = beamGradientTex;
            coneSo.ApplyModifiedPropertiesWithoutUndo();

            // Huzme içi parlayan toz partikülleri (gerçek 1:1 dünya ölçeğinde)
            GameObject beamMotes = new GameObject("BeamDustMotes");
            beamMotes.transform.SetParent(coneObj.transform, false);
            beamMotes.transform.localPosition = new Vector3(3.8f, 0f, 0f);
            CreateBeamDustParticleSystem(beamMotes, sparkSprite);

            FlashlightController2D flashlightCtrl = flashlightArmPivot.AddComponent<FlashlightController2D>();
            SerializedObject ctrlSo = new SerializedObject(flashlightCtrl);
            ctrlSo.FindProperty("armPivot").objectReferenceValue = flashlightArmPivot.transform;
            ctrlSo.FindProperty("armVisualTransform").objectReferenceValue = frontArmSpriteObj.transform;
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
            inputSo.FindProperty("runnerMotor").objectReferenceValue = motor;
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
            renderer.sortingOrder = 16;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) renderer.sharedMaterial = new Material(shader) { mainTexture = sparkSprite.texture };
            return ps;
        }

        private static void CreateBeamDustParticleSystem(GameObject target, Sprite dotSprite)
        {
            ParticleSystem ps = target.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.85f;
            main.startSpeed = 0.35f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
            main.startColor = new Color(0.85f, 0.98f, 1.0f, 0.45f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 28;

            var emission = ps.emission;
            emission.rateOverTime = 18f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(7.0f, 2.2f, 0.1f);

            var renderer = target.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 11;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) renderer.sharedMaterial = new Material(shader) { mainTexture = dotSprite.texture };
        }

        private static TrailRenderer CreateRunnerSpeedTrail(GameObject target)
        {
            TrailRenderer tr = target.AddComponent<TrailRenderer>();
            tr.time = 0.28f;
            tr.startWidth = 0.34f;
            tr.endWidth = 0.02f;
            tr.minVertexDistance = 0.08f;
            tr.sortingOrder = 11;

            Gradient grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.0f, 0.94f, 1.0f), 0f),
                    new GradientColorKey(new Color(1.0f, 0.68f, 0.14f), 0.55f),
                    new GradientColorKey(new Color(0.71f, 0.22f, 0.95f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.70f, 0f),
                    new GradientAlphaKey(0.34f, 0.6f),
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
            for (int i = 0; i < 70; i++)
            {
                GameObject star = new GameObject($"Star_{i:00}");
                star.transform.SetParent(starRoot.transform, false);
                star.transform.position = new Vector3(Random.Range(-17f, 17f), Random.Range(-9f, 9f), 2f);
                float scale = Random.Range(0.03f, 0.09f);
                star.transform.localScale = new Vector3(scale, scale, 1f);

                SpriteRenderer sr = star.AddComponent<SpriteRenderer>();
                sr.sprite = dotSprite;
                sr.color = new Color(0.45f, 0.85f, 1f, Random.Range(0.08f, 0.26f));
                sr.sortingOrder = -10;
            }

            ParallaxStarfield parallax = starRoot.AddComponent<ParallaxStarfield>();
            SerializedObject so = new SerializedObject(parallax);
            so.FindProperty("cameraTransform").objectReferenceValue = camTransform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildHorizonSilhouettes(Transform camTransform, Sprite monolithSprite)
        {
            GameObject root = new GameObject("=== VOID HORIZON SILHOUETTES ===");
            Random.InitState(108);
            for (int i = 0; i < 14; i++)
            {
                GameObject pillar = new GameObject($"HorizonMonolith_{i:00}");
                pillar.transform.SetParent(root.transform, false);
                pillar.transform.position = new Vector3(-16f + i * 2.6f, Random.Range(-5.5f, -2.5f), 4f);
                pillar.transform.localScale = new Vector3(Random.Range(1.2f, 2.5f), Random.Range(5.0f, 11.0f), 1f);

                SpriteRenderer sr = pillar.AddComponent<SpriteRenderer>();
                sr.sprite = monolithSprite;
                sr.color = new Color(0.09f, 0.17f, 0.34f, Random.Range(0.38f, 0.58f));
                sr.sortingOrder = -15;
            }

            ParallaxStarfield parallax = root.AddComponent<ParallaxStarfield>();
            SerializedObject so = new SerializedObject(parallax);
            so.FindProperty("cameraTransform").objectReferenceValue = camTransform;
            so.FindProperty("fieldWidth").floatValue = 36f;
            so.FindProperty("parallaxFactor").floatValue = 0.65f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject SaveCrystalPrefab(Sprite stdSprite, Sprite boostSprite, Sprite fragileSprite, Sprite umbraSprite, Sprite rimSprite, Sprite starSprite)
        {
            GameObject go = new GameObject("PF_CrystalPlatform");
            go.layer = HardlightWorldLayer;
            go.transform.localScale = new Vector3(2.4f, 0.52f, 1f);

            SpriteRenderer mainSr = go.AddComponent<SpriteRenderer>();
            mainSr.sprite = stdSprite;
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
            leftSr.color = new Color(0.85f, 0.98f, 1f, 0.98f);
            leftSr.sortingOrder = 9;

            GameObject rightSparkle = new GameObject("SparkleRight");
            rightSparkle.transform.SetParent(go.transform, false);
            rightSparkle.transform.localPosition = new Vector3(0.48f, 0.48f, 0f);
            SpriteRenderer rightSr = rightSparkle.AddComponent<SpriteRenderer>();
            rightSr.sprite = starSprite;
            rightSr.color = new Color(1f, 0.94f, 0.68f, 0.98f);
            rightSr.sortingOrder = 9;

            CrystalPlatform plat = go.AddComponent<CrystalPlatform>();
            SerializedObject platSo = new SerializedObject(plat);
            platSo.FindProperty("standardCrystalSprite").objectReferenceValue = stdSprite;
            platSo.FindProperty("boostCrystalSprite").objectReferenceValue = boostSprite;
            platSo.FindProperty("fragileCrystalSprite").objectReferenceValue = fragileSprite;
            platSo.FindProperty("umbraCrystalSprite").objectReferenceValue = umbraSprite;
            platSo.FindProperty("rimGlowRenderer").objectReferenceValue = rimSr;
            platSo.FindProperty("cornerSparkleLeft").objectReferenceValue = leftSparkle.transform;
            platSo.FindProperty("cornerSparkleRight").objectReferenceValue = rightSparkle.transform;
            platSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, Path.Combine(PrefabsFolder, "PF_CrystalPlatform.prefab"));
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject SaveOrbPrefab(string fileName, bool isSuper, Sprite hexFrameSprite, Sprite boltSprite, Sprite auraSprite)
        {
            GameObject go = new GameObject(fileName.Replace(".prefab", ""));
            float s = isSuper ? 1.18f : 0.95f;
            go.transform.localScale = new Vector3(s, s, 1f);

            SpriteRenderer frameSr = go.AddComponent<SpriteRenderer>();
            frameSr.sprite = hexFrameSprite;
            frameSr.sortingOrder = 17;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.55f;

            GameObject auraObj = new GameObject("OrbHalo");
            auraObj.transform.SetParent(go.transform, false);
            auraObj.transform.localScale = new Vector3(1.65f, 1.65f, 1f);
            SpriteRenderer auraSr = auraObj.AddComponent<SpriteRenderer>();
            auraSr.sprite = auraSprite;
            auraSr.color = isSuper ? new Color(1f, 0.85f, 0.25f, 0.32f) : new Color(0f, 0.94f, 1f, 0.28f);
            auraSr.sortingOrder = 16;

            GameObject boltObj = new GameObject("InnerBolt");
            boltObj.transform.SetParent(go.transform, false);
            boltObj.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
            SpriteRenderer boltSr = boltObj.AddComponent<SpriteRenderer>();
            boltSr.sprite = boltSprite;
            boltSr.color = isSuper ? new Color(1f, 0.95f, 0.55f, 1f) : new Color(1f, 0.86f, 0.15f, 1f);
            boltSr.sortingOrder = 18;

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
            ringSr.sortingOrder = 12;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.0f, 0.85f);

            GameObject coreObj = new GameObject("InnerOrangeCore");
            coreObj.transform.SetParent(go.transform, false);
            coreObj.transform.localScale = new Vector3(0.62f, 0.62f, 1f);
            SpriteRenderer coreSr = coreObj.AddComponent<SpriteRenderer>();
            coreSr.sprite = coreSprite;
            coreSr.color = new Color(1f, 0.55f, 0.1f, 0.95f);
            coreSr.sortingOrder = 13;

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
            sr.sortingOrder = 17;

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
            sr.sortingOrder = 17;

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

        private static GameObject SavePrismGatePrefab(Sprite barrierSprite, Sprite lockDiamondSprite)
        {
            GameObject go = new GameObject("PF_PrismLockGate");
            go.transform.localScale = new Vector3(0.62f, 3.4f, 1f);

            SpriteRenderer barrierSr = go.AddComponent<SpriteRenderer>();
            barrierSr.sprite = barrierSprite;
            barrierSr.sortingOrder = 16;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.85f, 1.0f);

            GameObject lockObj = new GameObject("DiamondLockSwitch");
            lockObj.transform.SetParent(go.transform, false);
            lockObj.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            lockObj.transform.localScale = new Vector3(1.75f, 0.32f, 1f);
            SpriteRenderer lockSr = lockObj.AddComponent<SpriteRenderer>();
            lockSr.sprite = lockDiamondSprite;
            lockSr.sortingOrder = 19;

            // Bariyer ile Elmas Kilit arasındaki bağlantı ışığı
            LineRenderer linkLine = go.AddComponent<LineRenderer>();
            linkLine.useWorldSpace = false;
            linkLine.positionCount = 2;
            linkLine.SetPosition(0, new Vector3(0f, 0.45f, 0f));
            linkLine.SetPosition(1, new Vector3(0f, 0.72f, 0f));
            linkLine.startWidth = 0.08f;
            linkLine.endWidth = 0.08f;
            linkLine.sortingOrder = 17;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) linkLine.sharedMaterial = new Material(shader);
            linkLine.startColor = new Color(1.0f, 0.22f, 0.48f, 0.9f);
            linkLine.endColor = new Color(0.0f, 0.96f, 1.0f, 0.95f);

            PrismLockGate gate = go.AddComponent<PrismLockGate>();
            SerializedObject so = new SerializedObject(gate);
            so.FindProperty("diamondLockRenderer").objectReferenceValue = lockSr;
            so.FindProperty("diamondLockTransform").objectReferenceValue = lockObj.transform;
            so.FindProperty("lockTetherLine").objectReferenceValue = linkLine;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, Path.Combine(PrefabsFolder, "PF_PrismLockGate.prefab"));
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject SavePhotonAnchorPrefab(Sprite anchorSprite)
        {
            GameObject go = new GameObject("PF_PhotonAnchor");
            go.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = anchorSprite;
            sr.sortingOrder = 18;
            go.AddComponent<PhotonAnchor>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, Path.Combine(PrefabsFolder, "PF_PhotonAnchor.prefab"));
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
            if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);
        }

        private static void DestroyExistingIfFound(string objName)
        {
            GameObject existing = GameObject.Find(objName);
            if (existing != null) Object.DestroyImmediate(existing);
        }

        #region Procedural High-Craft Art & Variant Generators

        private static Sprite GenerateCrystalPlatformSprite(string assetPath)
        {
            const int w = 256;
            const int h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    float diag1 = Mathf.Abs((nx - ny * 0.45f) - 0.18f);
                    float diag2 = Mathf.Abs((nx + ny * 0.45f) - 0.82f);
                    float diag3 = Mathf.Abs((nx - ny * 0.35f) - 0.55f);
                    float centerFacet = Mathf.Abs(ny - 0.62f);

                    bool isTopEdge = y >= h - 5;
                    bool isSideEdge = x < 4 || x >= w - 4 || y < 4;
                    bool isFacetLine = diag1 < 0.012f || diag2 < 0.012f || diag3 < 0.012f || centerFacet < 0.016f;

                    Color c;
                    if (isTopEdge) c = new Color(0.95f, 1.0f, 1.0f, 1f);
                    else if (isSideEdge) c = new Color(0.65f, 0.96f, 1.0f, 0.98f);
                    else if (isFacetLine) c = new Color(0.70f, 0.96f, 1.0f, 0.90f);
                    else
                    {
                        Color topTint = new Color(0.15f, 0.86f, 1.0f, 0.80f);
                        Color midTint = new Color(0.12f, 0.52f, 0.95f, 0.72f);
                        Color bottomTint = new Color(0.54f, 0.20f, 0.92f, 0.70f);
                        c = ny > 0.5f ? Color.Lerp(midTint, topTint, (ny - 0.5f) * 2f) : Color.Lerp(bottomTint, midTint, ny * 2f);
                    }
                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 256f);
        }

        private static Sprite GenerateBoostCrystalSprite(string assetPath)
        {
            const int w = 256;
            const int h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                float chevronShift = Mathf.Abs(ny - 0.5f) * 0.22f;
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    bool isBorder = y >= h - 5 || y < 4 || x < 4 || x >= w - 4;
                    float chevronRepeat = Mathf.Repeat((nx - chevronShift) * 4.5f, 1f);
                    bool isChevron = chevronRepeat < 0.25f && ny > 0.2f && ny < 0.8f;

                    if (isBorder) pixels[y * w + x] = new Color(1.0f, 0.96f, 0.55f, 1f);
                    else if (isChevron) pixels[y * w + x] = new Color(1.0f, 0.92f, 0.35f, 0.98f);
                    else pixels[y * w + x] = Color.Lerp(new Color(0.05f, 0.65f, 0.55f, 0.78f), new Color(0.15f, 0.98f, 0.72f, 0.85f), ny);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 256f);
        }

        private static Sprite GenerateFragileCrystalSprite(string assetPath)
        {
            const int w = 256;
            const int h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    bool isBorder = y >= h - 4 || y < 4 || x < 4 || x >= w - 4;
                    float crack = Mathf.Abs(Mathf.Sin(nx * 18f + ny * 9f) * 0.12f - (ny - 0.5f));
                    bool isCrack = crack < 0.024f || Mathf.Abs(nx - 0.35f) < 0.012f || Mathf.Abs(nx - 0.70f) < 0.012f;

                    if (isBorder) pixels[y * w + x] = new Color(0.92f, 0.98f, 1f, 0.96f);
                    else if (isCrack) pixels[y * w + x] = new Color(1f, 1f, 1f, 0.92f);
                    else pixels[y * w + x] = new Color(0.65f, 0.88f, 1.0f, 0.58f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 256f);
        }

        private static Sprite GenerateUmbraCrystalSprite(string assetPath)
        {
            const int w = 256;
            const int h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    bool isBorder = y >= h - 5 || y < 4 || x < 4 || x >= w - 4;
                    float stripe = Mathf.Repeat((nx * 5f) + (ny * 1.4f), 1f);
                    bool isHazardStripe = stripe < 0.24f;

                    if (isBorder) pixels[y * w + x] = new Color(1.0f, 0.22f, 0.72f, 1f);
                    else if (isHazardStripe) pixels[y * w + x] = new Color(0.75f, 0.14f, 0.95f, 0.88f);
                    else pixels[y * w + x] = new Color(0.14f, 0.03f, 0.24f, 0.92f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 256f);
        }

        private static Sprite GenerateHorizonMonolithSprite(string assetPath)
        {
            const int w = 64;
            const int h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    bool isEdge = x < 2 || x >= w - 2 || y >= h - 3;
                    float alpha = Mathf.Clamp01(ny * 0.85f);
                    pixels[y * w + x] = isEdge
                        ? new Color(0.15f, 0.48f, 0.75f, alpha)
                        : new Color(0.05f, 0.09f, 0.18f, alpha * 0.9f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 64f);
        }

        private static Sprite GenerateLaserBarrierSprite(string assetPath)
        {
            const int w = 48;
            const int h = 192;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = Mathf.Abs(((float)x / (w - 1)) * 2f - 1f);
                    bool isStripe = (y / 12) % 2 == 0;
                    float coreAlpha = Mathf.Exp(-nx * nx * 4.5f);
                    Color c = nx < 0.28f ? Color.white : new Color(1f, 0.16f, 0.42f, 1f);
                    pixels[y * w + x] = new Color(c.r, c.g, c.b, coreAlpha * (isStripe ? 0.95f : 0.65f));
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 48f);
        }

        private static Sprite GeneratePhotonAnchorSprite(string assetPath)
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
                    float angle = Mathf.Atan2(ny, nx);
                    bool isRing = r >= 0.65f && r <= 0.85f && Mathf.Sin(angle * 4f) > -0.35f;
                    bool isCore = r <= 0.28f;

                    if (isCore) pixels[y * size + x] = new Color(1.0f, 0.94f, 0.55f, 1f);
                    else if (isRing) pixels[y * size + x] = Color.white;
                    else pixels[y * size + x] = Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 128f);
        }

        private static Sprite GenerateEclipseWaveSprite(string assetPath)
        {
            const int w = 128;
            const int h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                float edgeWave = Mathf.Sin(ny * Mathf.PI * 6f) * 0.06f;
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    float front = 0.88f + edgeWave;
                    if (nx > front) pixels[y * w + x] = Color.clear;
                    else
                    {
                        float distFromFront = front - nx;
                        float rim = Mathf.Exp(-distFromFront * 12f);
                        Color darkVoid = new Color(0.06f, 0.01f, 0.12f, 0.95f);
                        Color crimsonEdge = new Color(1.0f, 0.14f, 0.45f, 0.98f);
                        Color c = Color.Lerp(darkVoid, crimsonEdge, rim);
                        c.a *= Mathf.Clamp01(nx * 2.5f + 0.3f);
                        pixels[y * w + x] = c;
                    }
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
            const int w = 160;
            const int h = 192;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            Color armorDark = new Color(0.08f, 0.14f, 0.25f, 1f);
            Color armorMid = new Color(0.16f, 0.30f, 0.48f, 1f);
            Color neonCyan = new Color(0.05f, 0.96f, 1.0f, 1f);
            Color visorGlow = new Color(0.90f, 1.0f, 1.0f, 1f);

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    Color c = Color.clear;

                    if (ny >= 0.08f && ny <= 0.65f)
                    {
                        float torsoT = (ny - 0.08f) / 0.57f;
                        float torsoHalfW = Mathf.Lerp(0.15f, 0.28f, torsoT);
                        float centerX = Mathf.Lerp(0.45f, 0.53f, torsoT);
                        float dx = Mathf.Abs(nx - centerX);
                        if (dx <= torsoHalfW)
                        {
                            bool isOuterRim = dx > torsoHalfW - 0.035f;
                            bool isChestCore = Mathf.Abs(ny - 0.46f) < 0.05f && dx < 0.16f;
                            bool isShoulderStripe = ny > 0.54f && dx > 0.12f;
                            c = (isOuterRim || isChestCore || isShoulderStripe) ? neonCyan : Color.Lerp(armorDark, armorMid, torsoT);
                        }
                    }

                    Vector2 headCenter = new Vector2(0.55f, 0.80f);
                    float headDist = Vector2.Distance(new Vector2(nx, ny), headCenter);
                    if (headDist <= 0.145f)
                    {
                        c = armorMid;
                        if (nx >= 0.53f && nx <= 0.70f && ny >= 0.77f && ny <= 0.85f) c = visorGlow;
                        else if (headDist >= 0.118f) c = neonCyan;
                    }

                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 160f);
        }

        private static Sprite GenerateRunnerLimbSprite(string assetPath)
        {
            const int w = 48;
            const int h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    float dx = Mathf.Abs(nx - 0.5f);
                    float halfWidth = Mathf.Lerp(0.24f, 0.39f, ny);
                    if (dx <= halfWidth)
                    {
                        bool isBootOrStripe = ny < 0.18f || dx > halfWidth - 0.09f;
                        pixels[y * w + x] = isBootOrStripe ? Color.white : new Color(0.18f, 0.30f, 0.46f, 1f);
                    }
                    else pixels[y * w + x] = Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 96f);
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

                    if (hexDist >= 0.72f && hexDist <= 0.88f) pixels[y * size + x] = Color.white;
                    else if (hexDist < 0.72f) pixels[y * size + x] = new Color(0.06f, 0.38f, 0.58f, 0.42f);
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
                    else if (r >= 0.48f && r <= 0.62f) pixels[y * size + x] = new Color(1f, 0.62f, 0.25f, 0.95f);
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
                        pixels[y * size + x] = isBorder ? Color.white : new Color(0.75f, 0.35f, 1.0f, 0.85f);
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
                    if (isRim) pixels[y * w + x] = new Color(1.0f, 0.22f, 0.45f, 0.98f);
                    else if (isInnerRunes) pixels[y * w + x] = new Color(0.85f, 0.15f, 0.55f, 0.80f);
                    else pixels[y * w + x] = new Color(0.08f, 0.03f, 0.12f, 0.94f);
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
                        if (isEye) pixels[y * size + x] = new Color(1.0f, 0.92f, 0.52f, 1f);
                        else if (isRim) pixels[y * size + x] = new Color(1.0f, 0.18f, 0.36f, 1f);
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
                        if (r >= 0.56f + teeth) pixels[y * size + x] = new Color(0.75f, 0.18f, 1.0f, 1f);
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

        private static Sprite GenerateBatteryFrameSprite(string assetPath)
        {
            const int w = 256;
            const int h = 48;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool inBody = x < w - 16;
                    bool inNub = x >= w - 16 && y >= 12 && y < h - 12;
                    bool borderBody = inBody && (x < 4 || x >= w - 20 || y < 4 || y >= h - 4);
                    bool borderNub = inNub && (x >= w - 5 || y < 16 || y >= h - 16);

                    if (borderBody || borderNub) pixels[y * w + x] = Color.white;
                    else if (inBody) pixels[y * w + x] = new Color(0.05f, 0.12f, 0.20f, 0.55f);
                    else pixels[y * w + x] = Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 100f);
        }

        private static Sprite GenerateBatteryGradientFillSprite(string assetPath)
        {
            const int w = 256;
            const int h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            Color cyan = new Color(0.0f, 0.94f, 1.0f, 1f);
            Color green = new Color(0.32f, 1.0f, 0.58f, 1f);
            Color gold = new Color(1.0f, 0.88f, 0.15f, 1f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float t = (float)x / (w - 1);
                    Color c = t < 0.55f ? Color.Lerp(cyan, green, t / 0.55f) : Color.Lerp(green, gold, (t - 0.55f) / 0.45f);
                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 100f);
        }

        private static Sprite GenerateRoundedRectSprite(string assetPath)
        {
            const int size = 64;
            const int radius = 12;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int cx = Mathf.Clamp(x, radius, size - 1 - radius);
                    int cy = Mathf.Clamp(y, radius, size - 1 - radius);
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float a = Mathf.Clamp01(radius + 0.5f - dist);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
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
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 64f;
                importer.spriteBorder = new Vector4(16, 16, 16, 16);
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
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

        private static Sprite GenerateHighBeamIconSprite(string assetPath)
        {
            const int w = 96;
            const int h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float a = 0f;

                    // Sol tarafta fener gövdesi (x: 10..33, y: 38..58) ve genişleyen başlık (x: 33..45)
                    if (x >= 10 && x <= 33 && y >= 39 && y <= 57)
                    {
                        float capDist = x < 16 ? Vector2.Distance(new Vector2(x, y), new Vector2(16, 48)) : 0f;
                        if (x >= 16 || capDist <= 9.2f) a = 1f;
                    }
                    else if (x > 33 && x <= 46)
                    {
                        float t = (x - 33f) / 13f;
                        float halfH = Mathf.Lerp(9.5f, 17f, t);
                        if (Mathf.Abs(y - 48f) <= halfH) a = 1f;
                    }

                    // Sağ tarafta 5 adet uzun paralel ışın çizgisi ("uzunu gösteren fener" göstergesi!)
                    if (x >= 52 && x <= 88)
                    {
                        for (int b = -2; b <= 2; b++)
                        {
                            float beamY = 48f + b * 10.5f;
                            float endX = b == 0 ? 88f : (Mathf.Abs(b) == 1 ? 84f : 78f);
                            if (x <= endX && Mathf.Abs(y - beamY) <= 2.3f)
                            {
                                a = Mathf.Max(a, 1f);
                            }
                        }
                    }

                    pixels[y * w + x] = new Color(1f, 1f, 1f, a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 100f);
        }

        private static Sprite GenerateJumpArrowIconSprite(string assetPath)
        {
            const int w = 96;
            const int h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float a = 0f;
                    float dx = Mathf.Abs(x - 48f);

                    // Çift yukarı ok (Double Jump silüeti)
                    if (y >= 46 && y <= 82 && dx <= 28f)
                    {
                        float targetY = 78f - dx * 0.95f;
                        if (Mathf.Abs(y - targetY) <= 4.2f) a = 1f;
                    }
                    if (y >= 24 && y <= 60 && dx <= 24f)
                    {
                        float targetY = 56f - dx * 0.95f;
                        if (Mathf.Abs(y - targetY) <= 3.8f) a = 1f;
                    }

                    // Zemin çizgisi
                    if (y >= 16 && y <= 21 && dx <= 28f) a = Mathf.Max(a, 1f);

                    pixels[y * w + x] = new Color(1f, 1f, 1f, a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 100f);
        }

        #endregion
    }
}
