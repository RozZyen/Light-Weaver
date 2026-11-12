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
        private const string EgyptSpritesFolder = "Assets/_Project/Art/Sprites/Egypt";
        private const string PrefabsFolder = "Assets/_Project/Prefabs";
        private const string PhysicsFolder = "Assets/_Project/Physics";
        private const string SectorsFolder = "Assets/_Project/Data/Sectors";
        private const string DataFolder = "Assets/_Project/Data";

        private const int PlayerLayer = 6;
        private const int HardlightWorldLayer = 7;

        [MenuItem("LightWeaver/Build Complete Game Scene (Egypt Mystery & 210 Aim)")]
        public static void BuildPlayableScene()
        {
            EnsureDirectories();
            EnsureProjectLayers();
            ConfigureMobileLandscapeSettings();
            PhysicsMaterial2D zeroFrictionMat = CreateOrLoadZeroFrictionMaterial();

            // 1. Özgün Boyalı Mısır & Modüler Dedektif Sanat Seti İçe Aktarımı
            Sprite crystalStandardSprite = LoadAndImportEgyptSprite("egypt_plat_standard.png", 528f, new Vector2(0.5f, 0.90f));
            Sprite crystalBoostSprite = LoadAndImportEgyptSprite("egypt_plat_boost.png", 521f, new Vector2(0.5f, 0.895f));
            Sprite crystalFragileSprite = LoadAndImportEgyptSprite("egypt_plat_fragile.png", 524f, new Vector2(0.5f, 0.89f));
            Sprite crystalUmbraSprite = LoadAndImportEgyptSprite("egypt_plat_umbra.png", 526f, new Vector2(0.5f, 0.90f));

            Sprite rimGlowSprite = GenerateCrystalRimSprite(Path.Combine(SpritesFolder, "prism_crystal_rim.png"));
            Sprite starburstSprite = GenerateStarburstSprite(Path.Combine(SpritesFolder, "diamond_starburst.png"));
            Sprite emitterCoreSprite = GenerateEmitterCoreSprite(Path.Combine(SpritesFolder, "flashlight_emitter_core.png"));

            // Modüler Dedektif Uzuvları (Uniform 640/720 PPU & Tam Eklem Pivotları)
            Sprite detTorsoSprite = LoadAndImportEgyptSprite("det_torso.png", 640f, new Vector2(0.50f, 0.50f));
            Sprite detHeadHatSprite = LoadAndImportEgyptSprite("det_head_hat.png", 640f, new Vector2(0.48f, 0.08f));
            Sprite detCoatTailSprite = LoadAndImportEgyptSprite("det_coat_tail.png", 640f, new Vector2(0.80f, 0.88f));
            Sprite detUpperArmSprite = LoadAndImportEgyptSprite("det_upper_arm.png", 640f, new Vector2(0.50f, 0.82f));
            Sprite detForearmTorchSprite = LoadAndImportEgyptSprite("det_forearm_flashlight.png", 640f, new Vector2(0.106f, 0.612f));
            Sprite detBackArmSprite = LoadAndImportEgyptSprite("det_back_arm_rope.png", 720f, new Vector2(0.213f, 0.858f));
            Sprite detThighSprite = LoadAndImportEgyptSprite("det_thigh.png", 640f, new Vector2(0.50f, 0.86f));
            Sprite detShinBootSprite = LoadAndImportEgyptSprite("det_shin_boot.png", 640f, new Vector2(0.38f, 0.90f));

            Sprite runnerSparkSprite = GenerateSoftDotSprite(Path.Combine(SpritesFolder, "runner_spark_dot.png"));
            Sprite heroAuraSprite = GenerateSoftDotSprite(Path.Combine(SpritesFolder, "hero_aura_glow.png"));
            Texture2D beamGradientTex = GenerateBeamGradientTexture(Path.Combine(SpritesFolder, "volumetric_beam_gradient.png"));

            // Uyumlu Mısır Dünya Nesneleri & Düşman Engelleri
            Sprite batteryCellSprite = LoadAndImportEgyptSprite("egypt_battery_cell.png", 310f);
            Sprite goldCoinSprite = LoadAndImportEgyptSprite("egypt_gold_coin.png", 390f);
            Sprite jumpPadSprite = LoadAndImportEgyptSprite("egypt_jump_vent_altar.png", 310f, new Vector2(0.50f, 0.08f));
            Sprite jumpCoreSprite = GenerateSoftDotSprite(Path.Combine(SpritesFolder, "jump_pad_core.png"));
            Sprite lockSealSprite = LoadAndImportEgyptSprite("egypt_lock_seal.png", 300f);
            Sprite solidBarSprite = GenerateSolidWhiteRectSprite(Path.Combine(SpritesFolder, "diegetic_bar_pixel.png"));

            Sprite shadowPillarSprite = LoadAndImportEgyptSprite("egypt_shadow_obelisk.png", 140f, new Vector2(0.50f, 0.36f));
            Sprite umbraStalkerSprite = LoadAndImportEgyptSprite("egypt_umbra_stalker.png", 260f, new Vector2(0.50f, 0.56f));
            Sprite voidLeechSprite = LoadAndImportEgyptSprite("egypt_void_leech.png", 280f);
            Sprite burstRingSprite = GenerateHollowRingSprite(Path.Combine(SpritesFolder, "flash_burst_ring.png"));

            Sprite gateBarrierSprite = LoadAndImportEgyptSprite("egypt_gate_barrier.png", 128f);
            Sprite anchorRingSprite = LoadAndImportEgyptSprite("egypt_swing_sconce.png", 260f, new Vector2(0.500f, 0.280f));
            Sprite eclipseWallSprite = GenerateEclipseWaveSprite(Path.Combine(SpritesFolder, "eclipse_wave_wall.png"));

            Sprite pyramidPanoramaSprite = LoadAndImportEgyptSprite("egypt_bg_panorama.png", 66.2f);
            Sprite sandDuneSprite = GenerateSandDuneSprite(Path.Combine(SpritesFolder, "egypt_dune_mid.png"));

            // Antik Mısır UI/UX Seti (9-Slice Oyma Taş, Art-Deco Altın & Obsidian Cam)
            Sprite batteryOutlineSprite = LoadAndImportEgyptSprite("egypt_ui_battery_frame.png", 100f);
            Sprite batteryGradientSprite = LoadAndImportEgyptSprite("egypt_battery_fill.png", 100f);
            Sprite uiPanelSprite = LoadAndImportEgyptSprite("egypt_ui_panel_dark.png", 100f, null, new Vector4(64f, 64f, 64f, 64f));
            Sprite btnGoldSprite = LoadAndImportEgyptSprite("egypt_ui_btn_gold.png", 100f, null, new Vector4(68f, 32f, 68f, 32f));
            Sprite btnDarkSprite = LoadAndImportEgyptSprite("egypt_ui_btn_dark.png", 100f, null, new Vector4(44f, 28f, 44f, 28f));
            Sprite glassPillSprite = LoadAndImportEgyptSprite("egypt_ui_glass_pill.png", 100f, null, new Vector4(24f, 24f, 24f, 24f));
            Sprite titleLogoSprite = LoadAndImportEgyptSprite("egypt_title_logo.png", 100f);
            Sprite mainMenuKeyArtSprite = LoadAndImportEgyptSprite("egypt_main_menu_keyart.png", 100f);
            Sprite vignetteSprite = LoadAndImportEgyptSprite("egypt_ui_vignette.png", 100f);
            Sprite stickRingSprite = GenerateHollowRingSprite(Path.Combine(SpritesFolder, "ui_joystick_ring.png"));
            Sprite highBeamIconSprite = GenerateHighBeamIconSprite(Path.Combine(SpritesFolder, "ui_highbeam_icon.png"));
            Sprite jumpArrowIconSprite = GenerateJumpArrowIconSprite(Path.Combine(SpritesFolder, "ui_jump_icon.png"));

            AssetDatabase.Refresh();

            SectorProfileSO[] sectorProfiles = CreateOrUpdateSectorProfiles();

            // 2. Kamera & Mısır Çöl Gecesi Post-Process
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
            mainCam.backgroundColor = new Color(0.035f, 0.042f, 0.095f, 1f);

            UniversalAdditionalCameraData urpCamData = mainCam.GetUniversalAdditionalCameraData();
            if (urpCamData != null) urpCamData.renderPostProcessing = true;

            CameraFollow2D camFollow = mainCam.GetComponent<CameraFollow2D>();
            if (camFollow == null) camFollow = mainCam.gameObject.AddComponent<CameraFollow2D>();

            // Eski kökleri temizle
            DestroyExistingIfFound("=== LIGHT WEAVER SYSTEMS ===");
            DestroyExistingIfFound("=== LIGHT WEAVER RUNNER ===");
            DestroyExistingIfFound("=== VOID STARFIELD ===");
            DestroyExistingIfFound("=== VOID HORIZON SILHOUETTES ===");
            DestroyExistingIfFound("=== EGYPT NIGHT SKY ===");
            DestroyExistingIfFound("=== EGYPT PANORAMA PARALLAX ===");
            DestroyExistingIfFound("=== EGYPT PYRAMIDS FAR ===");
            DestroyExistingIfFound("=== EGYPT DUNES & RUINS ===");
            DestroyExistingIfFound("=== EGYPT WEATHER & ATMOSPHERE ===");
            DestroyExistingIfFound("=== THE ECLIPSE WAVE ===");
            DestroyExistingIfFound("=== LIGHT WEAVER POSTPROCESS ===");
            DestroyExistingIfFound("=== LIGHT WEAVER UI CANVAS ===");
            DestroyExistingIfFound("EventSystem");

            Volume postProcessVol = SetupPostProcessVolume();

            // 3. Havuz Prefab'ları (Tam Uyumlu Mısır Görselleri ve Ölçekleri)
            GameObject platPrefab = SaveCrystalPrefab(crystalStandardSprite, crystalBoostSprite, crystalFragileSprite, crystalUmbraSprite, rimGlowSprite, starburstSprite);
            GameObject orbPrefab = SaveOrbPrefab("PF_EnergyOrb.prefab", false, batteryCellSprite, heroAuraSprite);
            GameObject superOrbPrefab = SaveOrbPrefab("PF_EnergyOrb_Super.prefab", true, batteryCellSprite, heroAuraSprite);
            GameObject coinPrefab = SaveScarabCoinPrefab("PF_ScarabCoin.prefab", goldCoinSprite, heroAuraSprite);
            GameObject jumpPadPrefab = SaveJumpPadPrefab(jumpPadSprite, jumpCoreSprite);
            GameObject prismRelayPrefab = SavePrismRelayPrefab(lockSealSprite);
            GameObject pillarPrefab = SaveHazardPrefab("PF_ShadowPillar.prefab", ShadowHazardType.MovingShadowPillar, shadowPillarSprite, new Vector2(0.86f, 0.90f));
            GameObject stalkerPrefab = SaveHazardPrefab("PF_UmbraStalker.prefab", ShadowHazardType.UmbraStalker, umbraStalkerSprite, new Vector2(0.86f, 0.86f));
            GameObject leechPrefab = SaveHazardPrefab("PF_VoidLeech.prefab", ShadowHazardType.VoidLeech, voidLeechSprite, new Vector2(0.80f, 0.80f));
            GameObject gatePrefab = SavePrismGatePrefab(gateBarrierSprite, lockSealSprite);
            GameObject anchorPrefab = SavePhotonAnchorPrefab(anchorRingSprite);

            // 4. Modüler Parçalı Trençkotlu Dedektif Karakteri
            GameObject runnerGo = BuildRunnerCharacter(
                mainCam,
                camFollow,
                zeroFrictionMat,
                detTorsoSprite,
                detHeadHatSprite,
                detCoatTailSprite,
                detUpperArmSprite,
                detForearmTorchSprite,
                detBackArmSprite,
                detThighSprite,
                detShinBootSprite,
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

            // 5. Kesintisiz Gize Piramitleri Panorama, Ön Çöl Kumul Paralaksı & Dinamik Hava Sistemi
            BuildEgyptParallaxLayers(
                mainCam.transform,
                pyramidPanoramaSprite,
                sandDuneSprite,
                out SpriteRenderer[] panoRenderers,
                out SpriteRenderer[] duneRenderers);

            // 6. Merkezi Sistemler + WorldVfxManager + Eclipse Dalgası
            GameObject systemsRoot = new GameObject("=== LIGHT WEAVER SYSTEMS ===");
            ObjectPooler pooler = systemsRoot.AddComponent<ObjectPooler>();
            EndlessChunkDirector director = systemsRoot.AddComponent<EndlessChunkDirector>();
            PrismUpgradeManager upgradeMgr = systemsRoot.AddComponent<PrismUpgradeManager>();
            GameFlowManager gameFlow = systemsRoot.AddComponent<GameFlowManager>();
            HarmonicAudioManager audioMgr = systemsRoot.AddComponent<HarmonicAudioManager>();
            JuiceFeedbackManager juiceMgr = systemsRoot.AddComponent<JuiceFeedbackManager>();
            WorldVfxManager worldVfx = systemsRoot.AddComponent<WorldVfxManager>();

            BuildDesertWeatherSystem(director, mainCam, postProcessVol, panoRenderers, duneRenderers, solidBarSprite, runnerSparkSprite);

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
            dirSo.FindProperty("scarabCoinPrefab").objectReferenceValue = coinPrefab;
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
                jumpArrowIconSprite,
                batteryCellSprite,
                goldCoinSprite,
                btnGoldSprite,
                btnDarkSprite,
                glassPillSprite,
                titleLogoSprite,
                mainMenuKeyArtSprite,
                vignetteSprite);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[LightWeaver Setup] v4.0 Egypt Pendulum & Weather Complete: Grapple Rope Swing, Hold Long-Beam, Cohesive Art & Continuous Distance Weather Built!");
        }

        private static void ConfigureMobileLandscapeSettings()
        {
            PlayerSettings.runInBackground = true;
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

        private static Volume SetupPostProcessVolume()
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
            bloom.intensity.Override(0.72f);
            bloom.scatter.Override(0.65f);

            if (!profile.TryGet(out Vignette vignette)) vignette = profile.Add<Vignette>(true);
            vignette.active = true;
            vignette.color.Override(new Color(0.01f, 0.02f, 0.06f, 1f));
            vignette.intensity.Override(0.26f);

            EditorUtility.SetDirty(profile);

            GameObject volGo = new GameObject("=== LIGHT WEAVER POSTPROCESS ===");
            Volume vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = profile;
            return vol;
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
            Sprite jumpArrowIconSprite,
            Sprite batteryCellSprite,
            Sprite goldCoinSprite,
            Sprite btnGoldSprite,
            Sprite btnDarkSprite,
            Sprite glassPillSprite,
            Sprite titleLogoSprite,
            Sprite mainMenuKeyArtSprite,
            Sprite vignetteSprite)
        {
            Font builtinFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Art/Fonts/LW_UISans.ttf");
            if (builtinFont == null) builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Font displayFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Art/Fonts/LW_DisplaySerif.ttf");
            if (displayFont == null) displayFont = builtinFont;

            GameObject canvasGo = new GameObject("=== LIGHT WEAVER UI CANVAS ===");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1.0f;
            canvasGo.AddComponent<ResponsiveCanvasFit>();
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject safeRoot = CreateUIObject("SafeAreaRoot", canvasGo.transform);
            StretchFull(safeRoot.GetComponent<RectTransform>());
            safeRoot.AddComponent<SafeAreaFit>();

            // =================================================================
            // 1. IN-GAME HUD
            // =================================================================
            GameObject hudRoot = CreateUIObject("InGameHUD", safeRoot.transform);
            StretchFull(hudRoot.GetComponent<RectTransform>());

            GameObject batContainer = CreateObsidianPill("BatteryPanel", hudRoot.transform, glassPillSprite, new Vector2(384f, 188f));
            RectTransform batRt = batContainer.GetComponent<RectTransform>();
            batRt.anchorMin = new Vector2(0f, 1f);
            batRt.anchorMax = new Vector2(0f, 1f);
            batRt.pivot = new Vector2(0f, 1f);
            batRt.anchoredPosition = new Vector2(38f, -28f);

            GameObject batFrameObj = CreateUIObject("BatteryFrame", batContainer.transform);
            RectTransform frameRt = batFrameObj.GetComponent<RectTransform>();
            frameRt.anchorMin = new Vector2(0f, 1f);
            frameRt.anchorMax = new Vector2(0f, 1f);
            frameRt.pivot = new Vector2(0f, 1f);
            frameRt.anchoredPosition = new Vector2(12f, -8f);
            frameRt.sizeDelta = new Vector2(356f, 78f);
            Image frameImg = batFrameObj.AddComponent<Image>();
            frameImg.sprite = batteryOutlineSprite;
            frameImg.color = Color.white;

            GameObject batFillObj = CreateUIObject("BatteryFill", batFrameObj.transform);
            RectTransform fillRt = batFillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0.208f, 0.285f);
            fillRt.anchorMax = new Vector2(0.788f, 0.660f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            Image fillImg = batFillObj.AddComponent<Image>();
            fillImg.sprite = batteryGradientSprite;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 1f;

            Text batText = CreateUIText("BatteryLabel", batContainer.transform, builtinFont, "BATTERY: 100%", 21, FontStyle.Bold, TextAnchor.UpperLeft, new Color(1.0f, 0.92f, 0.72f, 0.98f));
            RectTransform batTextRt = batText.GetComponent<RectTransform>();
            batTextRt.anchorMin = new Vector2(0f, 1f);
            batTextRt.anchorMax = new Vector2(0f, 1f);
            batTextRt.pivot = new Vector2(0f, 1f);
            batTextRt.anchoredPosition = new Vector2(20f, -88f);
            batTextRt.sizeDelta = new Vector2(320f, 28f);

            GameObject burstBg = CreateUIObject("FlashBurstBarBg", batContainer.transform);
            RectTransform burstBgRt = burstBg.GetComponent<RectTransform>();
            burstBgRt.anchorMin = new Vector2(0f, 1f);
            burstBgRt.anchorMax = new Vector2(0f, 1f);
            burstBgRt.pivot = new Vector2(0f, 1f);
            burstBgRt.anchoredPosition = new Vector2(20f, -118f);
            burstBgRt.sizeDelta = new Vector2(336f, 9f);
            Image burstBgImg = burstBg.AddComponent<Image>();
            burstBgImg.sprite = solidSprite;
            burstBgImg.color = new Color(0.14f, 0.11f, 0.10f, 0.90f);

            GameObject burstFill = CreateUIObject("FlashBurstBarFill", burstBg.transform);
            StretchFull(burstFill.GetComponent<RectTransform>());
            Image burstFillImg = burstFill.AddComponent<Image>();
            burstFillImg.sprite = solidSprite;
            burstFillImg.color = new Color(0.10f, 0.88f, 0.80f, 1f);
            burstFillImg.type = Image.Type.Filled;
            burstFillImg.fillMethod = Image.FillMethod.Horizontal;

            Text burstLbl = CreateUIText("FlashBurstLabel", batContainer.transform, builtinFont, "SOLAR BURST", 13, FontStyle.Bold, TextAnchor.UpperLeft, new Color(1.0f, 0.84f, 0.36f, 0.94f));
            RectTransform burstLblRt = burstLbl.GetComponent<RectTransform>();
            burstLblRt.anchorMin = new Vector2(0f, 1f);
            burstLblRt.anchorMax = new Vector2(0f, 1f);
            burstLblRt.pivot = new Vector2(0f, 1f);
            burstLblRt.anchoredPosition = new Vector2(20f, -129f);
            burstLblRt.sizeDelta = new Vector2(260f, 20f);

            GameObject eclipseBg = CreateUIObject("EclipseBarBg", batContainer.transform);
            RectTransform ecBgRt = eclipseBg.GetComponent<RectTransform>();
            ecBgRt.anchorMin = new Vector2(0f, 1f);
            ecBgRt.anchorMax = new Vector2(0f, 1f);
            ecBgRt.pivot = new Vector2(0f, 1f);
            ecBgRt.anchoredPosition = new Vector2(20f, -152f);
            ecBgRt.sizeDelta = new Vector2(336f, 9f);
            Image ecBgImg = eclipseBg.AddComponent<Image>();
            ecBgImg.sprite = solidSprite;
            ecBgImg.color = new Color(0.16f, 0.08f, 0.10f, 0.90f);

            GameObject eclipseFill = CreateUIObject("EclipseBarFill", eclipseBg.transform);
            StretchFull(eclipseFill.GetComponent<RectTransform>());
            Image ecFillImg = eclipseFill.AddComponent<Image>();
            ecFillImg.sprite = solidSprite;
            ecFillImg.color = new Color(0.92f, 0.26f, 0.22f, 1f);
            ecFillImg.type = Image.Type.Filled;
            ecFillImg.fillMethod = Image.FillMethod.Horizontal;

            Text ecLbl = CreateUIText("EclipseLabel", batContainer.transform, builtinFont, "CURSE STORM PROXIMITY", 13, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.95f, 0.45f, 0.32f, 0.94f));
            RectTransform ecLblRt = ecLbl.GetComponent<RectTransform>();
            ecLblRt.anchorMin = new Vector2(0f, 1f);
            ecLblRt.anchorMax = new Vector2(0f, 1f);
            ecLblRt.pivot = new Vector2(0f, 1f);
            ecLblRt.anchoredPosition = new Vector2(20f, -163f);
            ecLblRt.sizeDelta = new Vector2(260f, 20f);

            GameObject scoreContainer = CreateObsidianPill("ScorePanel", hudRoot.transform, glassPillSprite, new Vector2(340f, 108f));
            RectTransform scoreRt = scoreContainer.GetComponent<RectTransform>();
            scoreRt.anchorMin = new Vector2(1f, 1f);
            scoreRt.anchorMax = new Vector2(1f, 1f);
            scoreRt.pivot = new Vector2(1f, 1f);
            scoreRt.anchoredPosition = new Vector2(-124f, -28f);

            Text sectorText = CreateUIText("SectorTitle", scoreContainer.transform, builtinFont, "0m  •  <color=#FFD24C>GIZA NIGHTFALL</color>", 19, FontStyle.Bold, TextAnchor.UpperRight, new Color(0.95f, 0.90f, 0.80f, 1f));
            sectorText.supportRichText = true;
            RectTransform secRt = sectorText.GetComponent<RectTransform>();
            secRt.anchorMin = new Vector2(1f, 1f);
            secRt.anchorMax = new Vector2(1f, 1f);
            secRt.pivot = new Vector2(1f, 1f);
            secRt.anchoredPosition = new Vector2(-16f, -10f);
            secRt.sizeDelta = new Vector2(310f, 28f);

            Text scoreText = CreateUIText("ScoreValue", scoreContainer.transform, builtinFont, "0", 44, FontStyle.Bold, TextAnchor.UpperRight, Color.white);
            RectTransform scValRt = scoreText.GetComponent<RectTransform>();
            scValRt.anchorMin = new Vector2(1f, 1f);
            scValRt.anchorMax = new Vector2(1f, 1f);
            scValRt.pivot = new Vector2(1f, 1f);
            scValRt.anchoredPosition = new Vector2(-16f, -36f);
            scValRt.sizeDelta = new Vector2(300f, 52f);

            Text multText = CreateUIText("MultiplierBadge", scoreContainer.transform, builtinFont, "OVERCHARGE 3X", 15, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.28f, 0.96f, 0.86f, 1f));
            RectTransform multRt = multText.GetComponent<RectTransform>();
            multRt.anchorMin = new Vector2(0f, 0f);
            multRt.anchorMax = new Vector2(0f, 0f);
            multRt.pivot = new Vector2(0f, 0f);
            multRt.anchoredPosition = new Vector2(16f, 12f);
            multRt.sizeDelta = new Vector2(170f, 22f);
            multText.gameObject.SetActive(false);

            Button pauseBtn = CreateNeonButton("PauseButton", hudRoot.transform, btnDarkSprite, displayFont, "II", 26, Color.white, new Color(1.0f, 0.88f, 0.48f, 1f), new Vector2(74f, 74f));
            RectTransform pauseRt = pauseBtn.GetComponent<RectTransform>();
            pauseRt.anchorMin = new Vector2(1f, 1f);
            pauseRt.anchorMax = new Vector2(1f, 1f);
            pauseRt.pivot = new Vector2(1f, 1f);
            pauseRt.anchoredPosition = new Vector2(-38f, -28f);

            // HUD Scarab Coin Counter Pill (Top-Right cleanly below ScorePill)
            GameObject hudCoinPill = CreateObsidianPill("HudCoinPill", hudRoot.transform, glassPillSprite, new Vector2(176f, 44f));
            RectTransform hudCoinRt = hudCoinPill.GetComponent<RectTransform>();
            hudCoinRt.anchorMin = new Vector2(1f, 1f);
            hudCoinRt.anchorMax = new Vector2(1f, 1f);
            hudCoinRt.pivot = new Vector2(1f, 1f);
            hudCoinRt.anchoredPosition = new Vector2(-124f, -144f);

            GameObject hudCoinIconGo = CreateUIObject("CoinIcon", hudCoinPill.transform);
            RectTransform hudCoinIconRt = hudCoinIconGo.GetComponent<RectTransform>();
            hudCoinIconRt.anchorMin = new Vector2(0f, 0.5f);
            hudCoinIconRt.anchorMax = new Vector2(0f, 0.5f);
            hudCoinIconRt.pivot = new Vector2(0f, 0.5f);
            hudCoinIconRt.anchoredPosition = new Vector2(10f, 0f);
            hudCoinIconRt.sizeDelta = new Vector2(30f, 30f);
            Image hudCoinIconImg = hudCoinIconGo.AddComponent<Image>();
            hudCoinIconImg.sprite = goldCoinSprite;
            hudCoinIconImg.preserveAspect = true;
            hudCoinIconImg.raycastTarget = false;

            Text hudCoinValueText = CreateUIText("CoinValue", hudCoinPill.transform, builtinFont, "0", 23, FontStyle.Bold, TextAnchor.MiddleRight, new Color(1.0f, 0.86f, 0.34f, 1f));
            RectTransform hudCoinValRt = hudCoinValueText.GetComponent<RectTransform>();
            hudCoinValRt.anchorMin = new Vector2(0f, 0f);
            hudCoinValRt.anchorMax = new Vector2(1f, 1f);
            hudCoinValRt.offsetMin = new Vector2(44f, 0f);
            hudCoinValRt.offsetMax = new Vector2(-14f, 0f);

            Text aimHintTxt = CreateUIText("AimHintText", hudRoot.transform, builtinFont, "DRAG RIGHT SIDE TO AIM 210°", 15, FontStyle.Bold, TextAnchor.LowerRight, new Color(0.95f, 0.88f, 0.72f, 0.60f));
            RectTransform aimHintRt = aimHintTxt.GetComponent<RectTransform>();
            aimHintRt.anchorMin = new Vector2(1f, 0f);
            aimHintRt.anchorMax = new Vector2(1f, 0f);
            aimHintRt.pivot = new Vector2(1f, 0f);
            aimHintRt.anchoredPosition = new Vector2(-48f, 32f);
            aimHintRt.sizeDelta = new Vector2(320f, 26f);

            // Sol Alt: Antik Mısır Taş Çerçeveli JUMP + LONG BEAM İkon Butonları + Üstte BURST
            Button jumpBtn = CreateTactileIconButton(
                "MobileJumpBtn",
                hudRoot.transform,
                panelSprite,
                jumpArrowIconSprite,
                builtinFont,
                "JUMP",
                new Vector2(46f, 38f),
                new Vector2(168f, 128f),
                new Vector2(54f, 54f),
                new Color(0.92f, 0.94f, 0.98f, 0.92f),
                new Color(0.18f, 0.90f, 0.84f, 1f),
                new Color(1.0f, 0.92f, 0.72f, 1f),
                out Image _,
                out Image _,
                out Text _);

            Button highBeamBtn = CreateTactileIconButton(
                "MobileHighBeamBtn",
                hudRoot.transform,
                panelSprite,
                highBeamIconSprite,
                builtinFont,
                "LONG [HOLD]",
                new Vector2(228f, 38f),
                new Vector2(154f, 128f),
                new Vector2(72f, 56f),
                new Color(0.85f, 0.85f, 0.90f, 0.92f),
                new Color(0.78f, 0.88f, 0.92f, 0.95f),
                new Color(0.94f, 0.88f, 0.76f, 0.95f),
                out Image highBeamBgImg,
                out Image highBeamIconImg,
                out Text highBeamLabelTxt);

            Button burstBtn = CreateNeonButton("MobileBurstBtn", hudRoot.transform, btnGoldSprite, displayFont, "SOLAR BURST", 16, Color.white, new Color(1.0f, 0.92f, 0.64f, 1f), new Vector2(168f, 54f));
            RectTransform brsRt = burstBtn.GetComponent<RectTransform>();
            brsRt.anchorMin = new Vector2(0f, 0f);
            brsRt.anchorMax = new Vector2(0f, 0f);
            brsRt.pivot = new Vector2(0f, 0f);
            brsRt.anchoredPosition = new Vector2(46f, 180f);

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
            hudSo.FindProperty("coinValueText").objectReferenceValue = hudCoinValueText;
            hudSo.FindProperty("aimHintText").objectReferenceValue = aimHintTxt;
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

            // Edit Mode'da ve açılış karesinde HUD'ın Ana Menü üzerine binmesini önle
            hudRoot.SetActive(false);

            // =================================================================
            // 2. SCENIC FULL-SCREEN MAIN MENU (Noir Dedektif Key Art + Art-Deco UI)
            // =================================================================
            GameObject mainMenuPanel = CreateUIObject("MainMenuPanel", safeRoot.transform);
            StretchFull(mainMenuPanel.GetComponent<RectTransform>());

            // Tam Ekran Ana Menü Key Art
            GameObject keyArtGo = CreateUIObject("MainMenuKeyArt", mainMenuPanel.transform);
            StretchFull(keyArtGo.GetComponent<RectTransform>());
            Image keyArtImg = keyArtGo.AddComponent<Image>();
            keyArtImg.sprite = mainMenuKeyArtSprite;
            keyArtImg.color = Color.white;
            keyArtImg.raycastTarget = false;
            AspectRatioFitter keyArtFitter = keyArtGo.AddComponent<AspectRatioFitter>();
            keyArtFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            keyArtFitter.aspectRatio = 16f / 9f;

            // Sinematik Sol & Alt Vinyet (UI kontrastını artırır)
            GameObject vigGo = CreateUIObject("CinematicVignette", mainMenuPanel.transform);
            StretchFull(vigGo.GetComponent<RectTransform>());
            Image vigImg = vigGo.AddComponent<Image>();
            vigImg.sprite = vignetteSprite;
            vigImg.color = new Color(1f, 1f, 1f, 0.68f);
            vigImg.raycastTarget = false;

            // Sol Üst: Özel Boyama LIGHT WEAVER Logo Arması & Rekor Rozeti
            GameObject titleBlock = CreateUIObject("TitleBlock", mainMenuPanel.transform);
            RectTransform titleBlockRt = titleBlock.GetComponent<RectTransform>();
            titleBlockRt.anchorMin = new Vector2(0f, 1f);
            titleBlockRt.anchorMax = new Vector2(0f, 1f);
            titleBlockRt.pivot = new Vector2(0f, 1f);
            titleBlockRt.anchoredPosition = new Vector2(38f, -26f);
            titleBlockRt.sizeDelta = new Vector2(460f, 220f);

            GameObject logoGo = CreateUIObject("TitleLogo", titleBlock.transform);
            RectTransform logoRt = logoGo.GetComponent<RectTransform>();
            logoRt.anchorMin = new Vector2(0f, 1f);
            logoRt.anchorMax = new Vector2(0f, 1f);
            logoRt.pivot = new Vector2(0f, 1f);
            logoRt.anchoredPosition = Vector2.zero;
            logoRt.sizeDelta = new Vector2(440f, 148f);
            Image logoImg = logoGo.AddComponent<Image>();
            logoImg.sprite = titleLogoSprite;
            logoImg.preserveAspect = true;
            logoImg.raycastTarget = false;

            GameObject bestPill = CreateObsidianPill("BestScorePill", titleBlock.transform, glassPillSprite, new Vector2(360f, 44f));
            RectTransform bestPillRt = bestPill.GetComponent<RectTransform>();
            bestPillRt.anchorMin = new Vector2(0f, 1f);
            bestPillRt.anchorMax = new Vector2(0f, 1f);
            bestPillRt.pivot = new Vector2(0f, 1f);
            bestPillRt.anchoredPosition = new Vector2(10f, -158f);

            Text bestTxt = CreateUIText("BestScoreText", bestPill.transform, builtinFont, "BEST: 0   •   0m", 20, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.22f, 0.94f, 0.86f, 1f));
            StretchFull(bestTxt.GetComponent<RectTransform>());

            // Sağ Üst: Scarab Altın Bakiyesi & Ses Kontrolleri
            GameObject topRightBar = CreateUIObject("TopRightUtilityBar", mainMenuPanel.transform);
            RectTransform trbRt = topRightBar.GetComponent<RectTransform>();
            trbRt.anchorMin = new Vector2(1f, 1f);
            trbRt.anchorMax = new Vector2(1f, 1f);
            trbRt.pivot = new Vector2(1f, 1f);
            trbRt.anchoredPosition = new Vector2(-46f, -36f);
            trbRt.sizeDelta = new Vector2(560f, 60f);

            GameObject mainCoinPill = CreateObsidianPill("MainCoinsPill", topRightBar.transform, glassPillSprite, new Vector2(180f, 52f));
            RectTransform mcRt = mainCoinPill.GetComponent<RectTransform>();
            mcRt.anchorMin = new Vector2(1f, 1f);
            mcRt.anchorMax = new Vector2(1f, 1f);
            mcRt.pivot = new Vector2(1f, 1f);
            mcRt.anchoredPosition = new Vector2(0f, 0f);

            GameObject mcIconGo = CreateUIObject("CoinIcon", mainCoinPill.transform);
            RectTransform mcIconRt = mcIconGo.GetComponent<RectTransform>();
            mcIconRt.anchorMin = new Vector2(0f, 0.5f);
            mcIconRt.anchorMax = new Vector2(0f, 0.5f);
            mcIconRt.pivot = new Vector2(0f, 0.5f);
            mcIconRt.anchoredPosition = new Vector2(12f, 0f);
            mcIconRt.sizeDelta = new Vector2(34f, 34f);
            Image mcIconImg = mcIconGo.AddComponent<Image>();
            mcIconImg.sprite = goldCoinSprite;
            mcIconImg.preserveAspect = true;
            mcIconImg.raycastTarget = false;

            Text mainCoinsTxt = CreateUIText("MainCoinsText", mainCoinPill.transform, builtinFont, "0", 24, FontStyle.Bold, TextAnchor.MiddleRight, new Color(1.0f, 0.86f, 0.34f, 1f));
            RectTransform mctRt = mainCoinsTxt.GetComponent<RectTransform>();
            mctRt.anchorMin = Vector2.zero;
            mctRt.anchorMax = Vector2.one;
            mctRt.offsetMin = new Vector2(50f, 0f);
            mctRt.offsetMax = new Vector2(-16f, 0f);

            Button musicBtn = CreateNeonButton("MusicToggleBtn", topRightBar.transform, btnDarkSprite, builtinFont, "MUSIC: ON", 17, Color.white, new Color(0.95f, 0.90f, 0.78f, 1f), new Vector2(164f, 52f));
            RectTransform muRt = musicBtn.GetComponent<RectTransform>();
            muRt.anchorMin = new Vector2(1f, 1f);
            muRt.anchorMax = new Vector2(1f, 1f);
            muRt.pivot = new Vector2(1f, 1f);
            muRt.anchoredPosition = new Vector2(-366f, 0f);

            Button sfxBtn = CreateNeonButton("SfxToggleBtn", topRightBar.transform, btnDarkSprite, builtinFont, "SFX: ON", 17, Color.white, new Color(0.95f, 0.90f, 0.78f, 1f), new Vector2(164f, 52f));
            RectTransform sxRt = sfxBtn.GetComponent<RectTransform>();
            sxRt.anchorMin = new Vector2(1f, 1f);
            sxRt.anchorMax = new Vector2(1f, 1f);
            sxRt.pivot = new Vector2(1f, 1f);
            sxRt.anchoredPosition = new Vector2(-190f, 0f);

            // Sağ Alt: Ana Aksiyon İskelesi (EXPEDITION + RELIC SHOP)
            GameObject actionDock = CreateUIObject("BottomRightActionDock", mainMenuPanel.transform);
            RectTransform dockRt = actionDock.GetComponent<RectTransform>();
            dockRt.anchorMin = new Vector2(1f, 0f);
            dockRt.anchorMax = new Vector2(1f, 0f);
            dockRt.pivot = new Vector2(1f, 0f);
            dockRt.anchoredPosition = new Vector2(-48f, 42f);
            dockRt.sizeDelta = new Vector2(380f, 186f);

            Button playBtn = CreateNeonButton("PlayButton", actionDock.transform, btnGoldSprite, displayFont, "EXPEDITION", 28, Color.white, new Color(1.0f, 0.95f, 0.78f, 1f), new Vector2(368f, 94f));
            RectTransform plRt = playBtn.GetComponent<RectTransform>();
            plRt.anchorMin = new Vector2(1f, 0f);
            plRt.anchorMax = new Vector2(1f, 0f);
            plRt.pivot = new Vector2(1f, 0f);
            plRt.anchoredPosition = new Vector2(0f, 84f);

            Button openShopBtn = CreateNeonButton("OpenShopButton", actionDock.transform, btnDarkSprite, displayFont, "RELIC SHOP", 21, Color.white, new Color(0.24f, 0.94f, 0.86f, 1f), new Vector2(368f, 70f));
            RectTransform osRt = openShopBtn.GetComponent<RectTransform>();
            osRt.anchorMin = new Vector2(1f, 0f);
            osRt.anchorMax = new Vector2(1f, 0f);
            osRt.pivot = new Vector2(1f, 0f);
            osRt.anchoredPosition = Vector2.zero;

            GameObject shopBadgeObj = CreateObsidianPill("ShopAffordableBadge", openShopBtn.transform, btnGoldSprite, new Vector2(28f, 28f));
            RectTransform sbRt = shopBadgeObj.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1f, 1f);
            sbRt.anchorMax = new Vector2(1f, 1f);
            sbRt.pivot = new Vector2(1f, 1f);
            sbRt.anchoredPosition = new Vector2(-10f, -10f);
            shopBadgeObj.GetComponent<Image>().color = new Color(1.0f, 0.78f, 0.16f, 1f);
            Text sbTxt = CreateUIText("Bang", shopBadgeObj.transform, builtinFont, "!", 20, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.10f, 0.08f, 0.04f, 1f));
            StretchFull(sbTxt.GetComponent<RectTransform>());
            shopBadgeObj.SetActive(false);

            // =================================================================
            // 2B. RELIC WORKSHOP (KALICI BATARYA & MIĞNATIS MAĞAZASI)
            // =================================================================
            GameObject shopPanel = CreateModalCard("ShopPanel", safeRoot.transform, panelSprite, new Vector2(1020f, 560f), Color.white);
            Text shopTitle = CreateUIText("ShopTitle", shopPanel.transform, displayFont, "RELIC WORKSHOP", 36, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1.0f, 0.84f, 0.34f, 1f));
            SetAnchoredPos(shopTitle.rectTransform, new Vector2(-235f, 214f), new Vector2(450f, 54f));

            GameObject shopCoinPill = CreateObsidianPill("ShopCoinsPill", shopPanel.transform, glassPillSprite, new Vector2(190f, 48f));
            SetAnchoredPos(shopCoinPill.GetComponent<RectTransform>(), new Vector2(210f, 214f), new Vector2(190f, 48f));
            GameObject scIconGo = CreateUIObject("CoinIcon", shopCoinPill.transform);
            RectTransform scIconRt = scIconGo.GetComponent<RectTransform>();
            scIconRt.anchorMin = new Vector2(0f, 0.5f);
            scIconRt.anchorMax = new Vector2(0f, 0.5f);
            scIconRt.pivot = new Vector2(0f, 0.5f);
            scIconRt.anchoredPosition = new Vector2(12f, 0f);
            scIconRt.sizeDelta = new Vector2(32f, 32f);
            Image scIconImg = scIconGo.AddComponent<Image>();
            scIconImg.sprite = goldCoinSprite;
            scIconImg.preserveAspect = true;
            scIconImg.raycastTarget = false;

            Text shopCoinsTxt = CreateUIText("ShopCoinsText", shopCoinPill.transform, builtinFont, "0", 23, FontStyle.Bold, TextAnchor.MiddleRight, new Color(1.0f, 0.86f, 0.34f, 1f));
            RectTransform sctRt = shopCoinsTxt.GetComponent<RectTransform>();
            sctRt.anchorMin = Vector2.zero;
            sctRt.anchorMax = Vector2.one;
            sctRt.offsetMin = new Vector2(48f, 0f);
            sctRt.offsetMax = new Vector2(-14f, 0f);

            Button closeShopBtn = CreateNeonButton("CloseShopBtn", shopPanel.transform, btnDarkSprite, displayFont, "BACK", 19, Color.white, Color.white, new Vector2(130f, 48f));
            SetAnchoredPos(closeShopBtn.GetComponent<RectTransform>(), new Vector2(395f, 214f), new Vector2(130f, 48f));

            BuildShopUpgradeCard(
                "ShopCard_BatteryCap", shopPanel.transform, btnGoldSprite, glassPillSprite, batteryCellSprite, builtinFont,
                new Vector2(-315f, -20f), "SUN-CELL TANK", "Permanent Max Battery",
                out Text capLvlTxt, out Text capStatTxt, out Button capBuyBtn, out Text capBuyLbl);

            BuildShopUpgradeCard(
                "ShopCard_Efficiency", shopPanel.transform, btnGoldSprite, glassPillSprite, highBeamIconSprite, builtinFont,
                new Vector2(0f, -20f), "SOLAR LENS", "Reduces Battery Drain",
                out Text effLvlTxt, out Text effStatTxt, out Button effBuyBtn, out Text effBuyLbl);

            BuildShopUpgradeCard(
                "ShopCard_Magnet", shopPanel.transform, btnGoldSprite, glassPillSprite, goldCoinSprite, builtinFont,
                new Vector2(315f, -20f), "SCARAB MAGNET", "Pulls Coins Farther",
                out Text magLvlTxt, out Text magStatTxt, out Button magBuyBtn, out Text magBuyLbl);

            shopPanel.SetActive(false);

            // =================================================================
            // 3. PAUSE, GAME OVER & BLESSING CARDS
            // =================================================================
            GameObject pausePanel = CreateModalCard("PausePanel", safeRoot.transform, panelSprite, new Vector2(520f, 440f), Color.white);
            Text pauseTitle = CreateUIText("PauseTitle", pausePanel.transform, displayFont, "PAUSED", 40, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1.0f, 0.84f, 0.34f, 1f));
            SetAnchoredPos(pauseTitle.rectTransform, new Vector2(0f, 138f), new Vector2(440f, 60f));

            Button resumeBtn = CreateNeonButton("ResumeBtn", pausePanel.transform, btnGoldSprite, displayFont, "RESUME", 24, Color.white, new Color(1.0f, 0.96f, 0.78f, 1f), new Vector2(310f, 70f));
            SetAnchoredPos(resumeBtn.GetComponent<RectTransform>(), new Vector2(0f, 38f), new Vector2(310f, 70f));

            Button pauseRestartBtn = CreateNeonButton("PauseRestartBtn", pausePanel.transform, btnDarkSprite, displayFont, "RESTART", 21, Color.white, Color.white, new Vector2(310f, 60f));
            SetAnchoredPos(pauseRestartBtn.GetComponent<RectTransform>(), new Vector2(0f, -44f), new Vector2(310f, 60f));

            Button pauseMenuBtn = CreateNeonButton("PauseMenuBtn", pausePanel.transform, btnDarkSprite, displayFont, "MAIN MENU", 20, Color.white, new Color(0.84f, 0.88f, 0.94f, 1f), new Vector2(310f, 58f));
            SetAnchoredPos(pauseMenuBtn.GetComponent<RectTransform>(), new Vector2(0f, -120f), new Vector2(310f, 58f));
            pausePanel.SetActive(false);

            GameObject gameOverPanel = CreateModalCard("GameOverPanel", safeRoot.transform, panelSprite, new Vector2(660f, 540f), Color.white);
            Text goTitle = CreateUIText("GameOverTitle", gameOverPanel.transform, displayFont, "SWALLOWED BY THE SANDS", 33, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1.0f, 0.38f, 0.32f, 1f));
            SetAnchoredPos(goTitle.rectTransform, new Vector2(0f, 185f), new Vector2(580f, 58f));

            Text goScoreTxt = CreateUIText("GameOverScore", gameOverPanel.transform, builtinFont, "SCORE: 0", 34, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1.0f, 0.94f, 0.80f, 1f));
            SetAnchoredPos(goScoreTxt.rectTransform, new Vector2(0f, 115f), new Vector2(540f, 46f));

            Text goDistTxt = CreateUIText("GameOverDistance", gameOverPanel.transform, builtinFont, "DISTANCE: 0m  |  GIZA NIGHTFALL", 21, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.22f, 0.92f, 0.84f, 0.98f));
            SetAnchoredPos(goDistTxt.rectTransform, new Vector2(0f, 66f), new Vector2(540f, 34f));

            Text newBestObj = CreateUIText("NewBestBadge", gameOverPanel.transform, displayFont, "★ NEW RECORD! ★", 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1.0f, 0.85f, 0.15f, 1f));
            SetAnchoredPos(newBestObj.rectTransform, new Vector2(0f, 26f), new Vector2(360f, 30f));
            newBestObj.gameObject.SetActive(false);

            Button solarReviveBtn = CreateNeonButton("SolarReviveBtn", gameOverPanel.transform, btnGoldSprite, displayFont, "SOLAR REVIVE (+100% ENERGY)", 19, Color.white, new Color(1.0f, 0.95f, 0.72f, 1f), new Vector2(460f, 74f));
            SetAnchoredPos(solarReviveBtn.GetComponent<RectTransform>(), new Vector2(0f, -45f), new Vector2(460f, 74f));

            Button retryBtn = CreateNeonButton("RetryBtn", gameOverPanel.transform, btnDarkSprite, displayFont, "RETRY", 23, Color.white, new Color(0.22f, 0.94f, 0.86f, 1f), new Vector2(218f, 64f));
            SetAnchoredPos(retryBtn.GetComponent<RectTransform>(), new Vector2(-118f, -142f), new Vector2(218f, 64f));

            Button goMenuBtn = CreateNeonButton("GameOverMenuBtn", gameOverPanel.transform, btnDarkSprite, displayFont, "MENU", 21, Color.white, Color.white, new Vector2(218f, 64f));
            SetAnchoredPos(goMenuBtn.GetComponent<RectTransform>(), new Vector2(118f, -142f), new Vector2(218f, 64f));
            gameOverPanel.SetActive(false);

            GameObject blessingPanel = CreateModalCard("BlessingPanel", safeRoot.transform, panelSprite, new Vector2(1140f, 540f), Color.white);
            Text blessHdr = CreateUIText("BlessingHeader", blessingPanel.transform, displayFont, "CHAMBER CLEARED! CHOOSE AN ANCIENT BLESSING", 29, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1.0f, 0.84f, 0.34f, 1f));
            SetAnchoredPos(blessHdr.rectTransform, new Vector2(0f, 185f), new Vector2(980f, 52f));

            Button[] cardBtns = new Button[3];
            Text[] cardTitles = new Text[3];
            Text[] cardDescs = new Text[3];

            for (int i = 0; i < 3; i++)
            {
                Button cardBtn = CreateNeonButton($"BlessingCard_{i}", blessingPanel.transform, glassPillSprite, builtinFont, "", 22, Color.white, Color.white, new Vector2(325f, 285f));
                SetAnchoredPos(cardBtn.GetComponent<RectTransform>(), new Vector2((i - 1) * 350f, -30f), new Vector2(325f, 285f));

                Text cTitle = CreateUIText("CardTitle", cardBtn.transform, displayFont, "BLESSING", 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1.0f, 0.88f, 0.22f, 1f));
                SetAnchoredPos(cTitle.rectTransform, new Vector2(0f, 55f), new Vector2(280f, 70f));

                Text cDesc = CreateUIText("CardDesc", cardBtn.transform, builtinFont, "Description", 19, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.88f, 0.96f, 1f, 0.95f));
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
            menuSo.FindProperty("shopPanel").objectReferenceValue = shopPanel;
            menuSo.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            menuSo.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            menuSo.FindProperty("blessingPanel").objectReferenceValue = blessingPanel;

            menuSo.FindProperty("playButton").objectReferenceValue = playBtn;
            menuSo.FindProperty("openShopButton").objectReferenceValue = openShopBtn;
            menuSo.FindProperty("shopAffordableBadge").objectReferenceValue = shopBadgeObj;
            menuSo.FindProperty("mainBestScoreText").objectReferenceValue = bestTxt;
            menuSo.FindProperty("mainCoinsText").objectReferenceValue = mainCoinsTxt;
            menuSo.FindProperty("musicToggleButton").objectReferenceValue = musicBtn;
            menuSo.FindProperty("musicToggleLabel").objectReferenceValue = musicBtn.GetComponentInChildren<Text>();
            menuSo.FindProperty("sfxToggleButton").objectReferenceValue = sfxBtn;
            menuSo.FindProperty("sfxToggleLabel").objectReferenceValue = sfxBtn.GetComponentInChildren<Text>();

            menuSo.FindProperty("closeShopButton").objectReferenceValue = closeShopBtn;
            menuSo.FindProperty("shopCoinsText").objectReferenceValue = shopCoinsTxt;
            menuSo.FindProperty("capLevelText").objectReferenceValue = capLvlTxt;
            menuSo.FindProperty("capStatText").objectReferenceValue = capStatTxt;
            menuSo.FindProperty("capBuyButton").objectReferenceValue = capBuyBtn;
            menuSo.FindProperty("capBuyLabel").objectReferenceValue = capBuyLbl;
            menuSo.FindProperty("effLevelText").objectReferenceValue = effLvlTxt;
            menuSo.FindProperty("effStatText").objectReferenceValue = effStatTxt;
            menuSo.FindProperty("effBuyButton").objectReferenceValue = effBuyBtn;
            menuSo.FindProperty("effBuyLabel").objectReferenceValue = effBuyLbl;
            menuSo.FindProperty("magLevelText").objectReferenceValue = magLvlTxt;
            menuSo.FindProperty("magStatText").objectReferenceValue = magStatTxt;
            menuSo.FindProperty("magBuyButton").objectReferenceValue = magBuyBtn;
            menuSo.FindProperty("magBuyLabel").objectReferenceValue = magBuyLbl;

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
            bg.pixelsPerUnitMultiplier = 1.05f;
            bg.color = bgColor;
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
            img.pixelsPerUnitMultiplier = 2.4f;
            img.color = bgColor;

            Button btn = go.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            btn.colors = cb;
            go.AddComponent<UIButtonJuice>();

            Text txt = CreateUIText("Label", go.transform, font, label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, textColor);
            StretchFull(txt.GetComponent<RectTransform>());
            return btn;
        }

        private static void BuildShopUpgradeCard(
            string name,
            Transform parent,
            Sprite panelSprite,
            Sprite solidSprite,
            Sprite iconSprite,
            Font font,
            Vector2 pos,
            string title,
            string desc,
            out Text levelText,
            out Text statText,
            out Button buyButton,
            out Text buyLabel)
        {
            GameObject card = CreateObsidianPill(name, parent, solidSprite, new Vector2(288f, 360f));
            SetAnchoredPos(card.GetComponent<RectTransform>(), pos, new Vector2(288f, 360f));

            GameObject iconGo = CreateUIObject("Icon", card.transform);
            SetAnchoredPos(iconGo.GetComponent<RectTransform>(), new Vector2(0f, 112f), new Vector2(78f, 78f));
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            Text titleTxt = CreateUIText("Title", card.transform, font, title, 21, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1.0f, 0.86f, 0.34f, 1f));
            SetAnchoredPos(titleTxt.rectTransform, new Vector2(0f, 44f), new Vector2(260f, 32f));

            Text descTxt = CreateUIText("Desc", card.transform, font, desc, 16, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.86f, 0.90f, 0.94f, 0.88f));
            SetAnchoredPos(descTxt.rectTransform, new Vector2(0f, 14f), new Vector2(260f, 26f));

            levelText = CreateUIText("Level", card.transform, font, "LV 0/5", 18, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.18f, 0.92f, 0.84f, 1f));
            SetAnchoredPos(levelText.rectTransform, new Vector2(0f, -24f), new Vector2(240f, 28f));

            statText = CreateUIText("Stat", card.transform, font, "MAX: 100 -> 120", 17, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            SetAnchoredPos(statText.rectTransform, new Vector2(0f, -58f), new Vector2(260f, 30f));

            buyButton = CreateNeonButton("BuyBtn", card.transform, panelSprite, font, "50 COINS", 20, Color.white, new Color(1.0f, 0.96f, 0.80f, 1f), new Vector2(226f, 62f));
            SetAnchoredPos(buyButton.GetComponent<RectTransform>(), new Vector2(0f, -124f), new Vector2(226f, 62f));
            buyLabel = buyButton.GetComponentInChildren<Text>();
        }

        private static GameObject CreateObsidianPill(string name, Transform parent, Sprite pillSprite, Vector2 size)
        {
            GameObject pill = CreateUIObject(name, parent);
            RectTransform rt = pill.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            Image img = pill.AddComponent<Image>();
            img.sprite = pillSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1.6f;
            img.color = Color.white;
            img.raycastTarget = false;
            return pill;
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
            go.AddComponent<UIButtonJuice>();

            bgImage = go.AddComponent<Image>();
            bgImage.sprite = bgSprite;
            bgImage.type = Image.Type.Sliced;
            bgImage.pixelsPerUnitMultiplier = 2.2f;
            bgImage.color = bgColor;

            Button btn = go.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            btn.colors = cb;

            GameObject iconObj = CreateUIObject("Icon", go.transform);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            SetAnchoredPos(iconRt, new Vector2(0f, 14f), iconSize);
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
            lblRt.anchoredPosition = new Vector2(0f, 14f);
            lblRt.sizeDelta = new Vector2(-16f, 34f);

            return btn;
        }

        #endregion

        private static SectorProfileSO[] CreateOrUpdateSectorProfiles()
        {
            var specs = new (int num, string title, float dist, Color bg, Color accent, Color sec, float speed, float drain, float haz, float prism)[]
            {
                (1, "GIZA NIGHTFALL",     240f, new Color(0.035f, 0.045f, 0.102f, 1f), new Color(1.00f, 0.82f, 0.30f, 1f), new Color(0.00f, 0.85f, 0.78f, 1f), 1.00f, 0.88f, 0.16f, 0.16f),
                (2, "KHAMSIN SANDSTORM",  280f, new Color(0.125f, 0.072f, 0.095f, 1f), new Color(0.96f, 0.60f, 0.24f, 1f), new Color(0.95f, 0.72f, 0.38f, 1f), 1.08f, 1.02f, 0.26f, 0.28f),
                (3, "NECROPOLIS MIST",    340f, new Color(0.032f, 0.105f, 0.135f, 1f), new Color(0.18f, 0.90f, 0.78f, 1f), new Color(0.92f, 0.82f, 0.42f, 1f), 1.15f, 1.15f, 0.36f, 0.24f),
                (4, "BLOOD ECLIPSE",      390f, new Color(0.125f, 0.038f, 0.078f, 1f), new Color(0.96f, 0.30f, 0.36f, 1f), new Color(1.00f, 0.76f, 0.28f, 1f), 1.23f, 1.28f, 0.45f, 0.28f),
                (5, "HEART OF THE DUAT",  450f, new Color(0.070f, 0.038f, 0.138f, 1f), new Color(0.78f, 0.58f, 1.00f, 1f), new Color(1.00f, 0.84f, 0.28f, 1f), 1.32f, 1.40f, 0.52f, 0.30f),
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
            Sprite headHatSprite,
            Sprite coatTailSprite,
            Sprite upperArmSprite,
            Sprite forearmTorchSprite,
            Sprite backArmSprite,
            Sprite thighSprite,
            Sprite shinBootSprite,
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

            GameObject auraObj = new GameObject("DetectiveLanternAura");
            auraObj.transform.SetParent(visualRoot.transform, false);
            auraObj.transform.localPosition = new Vector3(0.05f, 0.14f, 0f);
            auraObj.transform.localScale = new Vector3(2.4f, 2.8f, 1f);
            SpriteRenderer auraSr = auraObj.AddComponent<SpriteRenderer>();
            auraSr.sprite = auraSprite;
            auraSr.color = new Color(1.0f, 0.80f, 0.36f, 0.16f);
            auraSr.sortingOrder = 9;

            // Arka Bacak (Kalça Pivotu -> Uyluk -> Diz Pivotu -> Baldır & Bot, 1:1 Oran)
            Transform backLegPivot = CreateLimbPivot("BackLegPivot", visualRoot.transform, new Vector3(-0.04f, -0.05f, 0f), new Vector3(0.95f, 0.95f, 1f), Vector3.zero, thighSprite, new Color(0.80f, 0.74f, 0.68f, 1f), 11);
            Transform backShinPivot = CreateLimbPivot("BackShinPivot", backLegPivot, new Vector3(0f, -0.27f, 0f), new Vector3(0.88f, 0.88f, 1f), Vector3.zero, shinBootSprite, new Color(0.80f, 0.74f, 0.68f, 1f), 11);

            // Gövde Pivotu
            GameObject bodyPivot = new GameObject("BodyPivot");
            bodyPivot.transform.SetParent(visualRoot.transform, false);

            // Trençkot Arka ve Ön Etek Kanatları (Özel kemer-üstü pivotlu)
            Transform coatTailBackPivot = CreateLimbPivot("CoatTailBackPivot", bodyPivot.transform, new Vector3(-0.08f, -0.02f, 0f), new Vector3(0.76f, 0.76f, 1f), Vector3.zero, coatTailSprite, new Color(0.80f, 0.74f, 0.68f, 1f), 10);
            Transform coatTailFrontPivot = CreateLimbPivot("CoatTailFrontPivot", bodyPivot.transform, new Vector3(-0.01f, -0.04f, 0f), new Vector3(0.78f, 0.78f, 1f), Vector3.zero, coatTailSprite, Color.white, 15);

            // Arka Kol (Gövde arkasında halat/kırbaç rulosu tutan diğer el)
            Transform backArmPivot = CreateLimbPivot("BackArmPivot", bodyPivot.transform, new Vector3(0.11f, 0.34f, 0f), new Vector3(0.84f, 0.84f, 1f), Vector3.zero, backArmSprite, new Color(0.84f, 0.78f, 0.72f, 1f), 10);
            GameObject ropeHandTip = new GameObject("RopeHandTip");
            ropeHandTip.transform.SetParent(backArmPivot, false);
            ropeHandTip.transform.localPosition = new Vector3(0.216f, -0.224f, 0f);

            // Trençkot Gövde (1:1 orantılı el boyaması)
            GameObject torsoGo = new GameObject("DetectiveTorsoSprite");
            torsoGo.transform.SetParent(bodyPivot.transform, false);
            torsoGo.transform.localPosition = new Vector3(0f, 0.21f, 0f);
            torsoGo.transform.localScale = new Vector3(0.94f, 0.94f, 1f);
            SpriteRenderer torsoSr = torsoGo.AddComponent<SpriteRenderer>();
            torsoSr.sprite = torsoSprite;
            torsoSr.sortingOrder = 13;

            // Ayrı Modüler Kafa + Fedora Şapka (Boyun pivotu sayesinde yakaya tam oturur)
            GameObject headPivot = new GameObject("HeadPivot");
            headPivot.transform.SetParent(bodyPivot.transform, false);
            headPivot.transform.localPosition = new Vector3(0.03f, 0.45f, 0f);
            GameObject headGo = new GameObject("HeadHatSprite");
            headGo.transform.SetParent(headPivot.transform, false);
            headGo.transform.localPosition = Vector3.zero;
            headGo.transform.localScale = new Vector3(0.84f, 0.84f, 1f);
            SpriteRenderer headSr = headGo.AddComponent<SpriteRenderer>();
            headSr.sprite = headHatSprite;
            headSr.sortingOrder = 16;

            // Diegetik Sırt Fener Enerji Göstergesi (Pirinç çanta kapsülü)
            GameObject gaugeBg = new GameObject("SuitBatteryGaugeBg");
            gaugeBg.transform.SetParent(bodyPivot.transform, false);
            gaugeBg.transform.localPosition = new Vector3(-0.20f, 0.14f, 0f);
            gaugeBg.transform.localScale = new Vector3(0.052f, 0.28f, 1f);
            SpriteRenderer gaugeBgSr = gaugeBg.AddComponent<SpriteRenderer>();
            gaugeBgSr.sprite = barSprite;
            gaugeBgSr.color = new Color(0.16f, 0.11f, 0.08f, 0.95f);
            gaugeBgSr.sortingOrder = 11;

            GameObject gaugeFillPivot = new GameObject("SuitBatteryGaugeFillPivot");
            gaugeFillPivot.transform.SetParent(gaugeBg.transform, false);
            gaugeFillPivot.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            GameObject gaugeFillBar = new GameObject("FillBar");
            gaugeFillBar.transform.SetParent(gaugeFillPivot.transform, false);
            gaugeFillBar.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            gaugeFillBar.transform.localScale = new Vector3(0.74f, 0.96f, 1f);
            SpriteRenderer gaugeFillSr = gaugeFillBar.AddComponent<SpriteRenderer>();
            gaugeFillSr.sprite = barSprite;
            gaugeFillSr.color = new Color(1.0f, 0.82f, 0.24f, 1f);
            gaugeFillSr.sortingOrder = 12;

            // Ön Bacak (Kalça -> Uyluk -> Diz/Baldır & Deri Bot)
            Transform frontLegPivot = CreateLimbPivot("FrontLegPivot", visualRoot.transform, new Vector3(0.04f, -0.05f, 0f), new Vector3(0.95f, 0.95f, 1f), Vector3.zero, thighSprite, Color.white, 14);
            Transform frontShinPivot = CreateLimbPivot("FrontShinPivot", frontLegPivot, new Vector3(0f, -0.27f, 0f), new Vector3(0.88f, 0.88f, 1f), Vector3.zero, shinBootSprite, Color.white, 14);

            // Modüler 2-Eklemli Ön Fener Kolu (Gövde kol yuvası -> Üst Kol -> Dirsek -> Önkol+El+Pirinç Fener)
            GameObject flashlightArmPivot = new GameObject("FlashlightArmPivot");
            flashlightArmPivot.transform.SetParent(visualRoot.transform, false);
            flashlightArmPivot.transform.localPosition = new Vector3(-0.13f, 0.31f, 0f);

            GameObject upperArmObj = new GameObject("UpperArmSprite");
            upperArmObj.transform.SetParent(flashlightArmPivot.transform, false);
            upperArmObj.transform.localPosition = Vector3.zero;
            upperArmObj.transform.localScale = new Vector3(0.92f, 0.92f, 1f);
            SpriteRenderer upperArmSr = upperArmObj.AddComponent<SpriteRenderer>();
            upperArmSr.sprite = upperArmSprite;
            upperArmSr.sortingOrder = 17;

            GameObject forearmPivot = new GameObject("ForearmPivot");
            forearmPivot.transform.SetParent(flashlightArmPivot.transform, false);
            forearmPivot.transform.localPosition = new Vector3(0.01f, -0.21f, 0f);

            GameObject forearmSpriteObj = new GameObject("ForearmFlashlightSprite");
            forearmSpriteObj.transform.SetParent(forearmPivot.transform, false);
            forearmSpriteObj.transform.localPosition = Vector3.zero;
            forearmSpriteObj.transform.localScale = new Vector3(0.94f, 0.94f, 1f);
            SpriteRenderer forearmSr = forearmSpriteObj.AddComponent<SpriteRenderer>();
            forearmSr.sprite = forearmTorchSprite;
            forearmSr.sortingOrder = 18;

            GameObject emitterTip = new GameObject("FlashlightEmitterTip");
            emitterTip.transform.SetParent(forearmPivot.transform, false);
            emitterTip.transform.localPosition = new Vector3(0.45f, -0.035f, 0f);
            emitterTip.transform.localScale = Vector3.one;

            GameObject emitterCoreObj = new GameObject("EmitterCoreGlow");
            emitterCoreObj.transform.SetParent(emitterTip.transform, false);
            emitterCoreObj.transform.localScale = new Vector3(0.24f, 0.24f, 1f);
            SpriteRenderer emitterSr = emitterCoreObj.AddComponent<SpriteRenderer>();
            emitterSr.sprite = emitterCoreSprite;
            emitterSr.color = new Color(1f, 0.92f, 0.56f, 0.92f);
            emitterSr.sortingOrder = 21;

            GameObject coneObj = new GameObject("LightConeCaster2D");
            coneObj.transform.SetParent(emitterTip.transform, false);
            coneObj.transform.localScale = Vector3.one;
            LightConeCaster2D coneCaster = coneObj.AddComponent<LightConeCaster2D>();
            SerializedObject coneSo = new SerializedObject(coneCaster);
            coneSo.FindProperty("beamGradientTexture").objectReferenceValue = beamGradientTex;
            coneSo.ApplyModifiedPropertiesWithoutUndo();

            // Huzme içi parlayan sıcak çöl tozu partikülleri
            GameObject beamMotes = new GameObject("BeamDustMotes");
            beamMotes.transform.SetParent(coneObj.transform, false);
            beamMotes.transform.localPosition = new Vector3(3.8f, 0f, 0f);
            CreateBeamDustParticleSystem(beamMotes, sparkSprite);

            FlashlightController2D flashlightCtrl = flashlightArmPivot.AddComponent<FlashlightController2D>();
            SerializedObject ctrlSo = new SerializedObject(flashlightCtrl);
            ctrlSo.FindProperty("armPivot").objectReferenceValue = flashlightArmPivot.transform;
            ctrlSo.FindProperty("forearmPivot").objectReferenceValue = forearmPivot.transform;
            ctrlSo.FindProperty("armVisualTransform").objectReferenceValue = forearmSpriteObj.transform;
            ctrlSo.FindProperty("headLookPivot").objectReferenceValue = headPivot.transform;
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
            animSo.FindProperty("frontShinPivot").objectReferenceValue = frontShinPivot;
            animSo.FindProperty("backLegPivot").objectReferenceValue = backLegPivot;
            animSo.FindProperty("backShinPivot").objectReferenceValue = backShinPivot;
            animSo.FindProperty("backArmPivot").objectReferenceValue = backArmPivot;
            animSo.FindProperty("backForearmPivot").objectReferenceValue = null;
            animSo.FindProperty("ropeHandTip").objectReferenceValue = ropeHandTip.transform;
            animSo.FindProperty("coatTailFrontPivot").objectReferenceValue = coatTailFrontPivot;
            animSo.FindProperty("coatTailBackPivot").objectReferenceValue = coatTailBackPivot;
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
            main.startColor = new Color(1.0f, 0.84f, 0.36f, 0.92f);
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
            main.startColor = new Color(1.0f, 0.90f, 0.56f, 0.42f);
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
                    new GradientColorKey(new Color(1.0f, 0.84f, 0.30f), 0f),
                    new GradientColorKey(new Color(0.82f, 0.58f, 0.25f), 0.55f),
                    new GradientColorKey(new Color(0.10f, 0.85f, 0.78f), 1f)
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

        private static void BuildEgyptParallaxLayers(
            Transform camTransform,
            Sprite panoramaSprite,
            Sprite duneSprite,
            out SpriteRenderer[] panoRenderers,
            out SpriteRenderer[] duneRenderers)
        {
            // Katman 1 (Uzak): El Boyaması Kesintisiz Gize Piramitleri, Sfenks & Çöl Dolunayı Panoraması
            GameObject panoRoot = new GameObject("=== EGYPT PANORAMA PARALLAX ===");
            panoRenderers = new SpriteRenderer[3];
            float tileW = 19.33f;
            for (int i = -1; i <= 1; i++)
            {
                GameObject tile = new GameObject($"PanoramaTile_{i + 1:00}");
                tile.transform.SetParent(panoRoot.transform, false);
                tile.transform.position = new Vector3(i * tileW, 0.8f, 8f);
                tile.transform.localScale = new Vector3(1.12f, 1.12f, 1f);
                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = panoramaSprite;
                sr.sortingOrder = -25;
                panoRenderers[i + 1] = sr;
            }

            ParallaxStarfield panoParallax = panoRoot.AddComponent<ParallaxStarfield>();
            SerializedObject pso = new SerializedObject(panoParallax);
            pso.FindProperty("cameraTransform").objectReferenceValue = camTransform;
            pso.FindProperty("fieldWidth").floatValue = tileW * 3f;
            pso.FindProperty("fieldHeight").floatValue = 42f;
            pso.FindProperty("parallaxFactor").floatValue = 0.88f;
            pso.ApplyModifiedPropertiesWithoutUndo();

            // Katman 2 (Yakın-Orta): İnce Ön Çöl Kumul Silüetleri (Derinlik Ayırıcı)
            GameObject midRoot = new GameObject("=== EGYPT DUNES & RUINS ===");
            duneRenderers = new SpriteRenderer[8];
            for (int i = 0; i < 8; i++)
            {
                GameObject dune = new GameObject($"DesertDune_{i:00}");
                dune.transform.SetParent(midRoot.transform, false);
                dune.transform.position = new Vector3(-20f + i * 6.0f, -5.55f - (i % 2) * 0.25f, 3f);
                dune.transform.localScale = new Vector3(11.0f, 3.8f, 1f);

                SpriteRenderer sr = dune.AddComponent<SpriteRenderer>();
                sr.sprite = duneSprite;
                sr.color = new Color(0.14f, 0.11f, 0.17f, 0.94f);
                sr.sortingOrder = -16;
                duneRenderers[i] = sr;
            }

            ParallaxStarfield midParallax = midRoot.AddComponent<ParallaxStarfield>();
            SerializedObject midSo = new SerializedObject(midParallax);
            midSo.FindProperty("cameraTransform").objectReferenceValue = camTransform;
            midSo.FindProperty("fieldWidth").floatValue = 48f;
            midSo.FindProperty("fieldHeight").floatValue = 36f;
            midSo.FindProperty("parallaxFactor").floatValue = 0.60f;
            midSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildDesertWeatherSystem(
            EndlessChunkDirector director,
            Camera mainCam,
            Volume postProcessVol,
            SpriteRenderer[] panoRenderers,
            SpriteRenderer[] duneRenderers,
            Sprite solidSprite,
            Sprite dotSprite)
        {
            GameObject weatherRoot = new GameObject("=== EGYPT WEATHER & ATMOSPHERE ===");
            Vector3 camPos = mainCam != null ? mainCam.transform.position : Vector3.zero;
            weatherRoot.transform.position = new Vector3(camPos.x, camPos.y, 0f);

            // Yumuşak Atmosferik Sis / Kum Fırtınası Pus Perdesi
            GameObject hazeGo = new GameObject("WeatherHazeOverlay");
            hazeGo.transform.SetParent(weatherRoot.transform, false);
            hazeGo.transform.localScale = new Vector3(36f, 20f, 1f);
            SpriteRenderer hazeSr = hazeGo.AddComponent<SpriteRenderer>();
            hazeSr.sprite = solidSprite;
            hazeSr.color = new Color(0.05f, 0.07f, 0.16f, 0f);
            hazeSr.sortingOrder = -12;

            // Hamsin Kum Rüzgarı Şeritleri
            GameObject windGo = new GameObject("SandWindParticles");
            windGo.transform.SetParent(weatherRoot.transform, false);
            windGo.transform.localPosition = new Vector3(13f, 0f, 0f);
            ParticleSystem windPs = windGo.AddComponent<ParticleSystem>();
            var wMain = windPs.main;
            wMain.startLifetime = 2.6f;
            wMain.startSpeed = 5.5f;
            wMain.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            wMain.startColor = new Color(0.88f, 0.78f, 0.54f, 0.18f);
            wMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            wMain.maxParticles = 160;
            var wEmit = windPs.emission;
            wEmit.rateOverTime = 2f;
            var wShape = windPs.shape;
            wShape.shapeType = ParticleSystemShapeType.Box;
            wShape.scale = new Vector3(1f, 13f, 0.1f);
            var wVel = windPs.velocityOverLifetime;
            wVel.enabled = false;
            windGo.transform.localRotation = Quaternion.Euler(0f, 0f, 184f); // Sağdan sola hafif rüzgar
            var wRen = windGo.GetComponent<ParticleSystemRenderer>();
            wRen.renderMode = ParticleSystemRenderMode.Stretch;
            wRen.lengthScale = 6.5f;
            wRen.sortingOrder = 19;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) wRen.sharedMaterial = new Material(shader) { mainTexture = dotSprite.texture };

            // Mistik Çöl Ateşböceği / Tapınak Sisi / Tutulma Külü Parçacıkları
            GameObject moteGo = new GameObject("AmbientMoteParticles");
            moteGo.transform.SetParent(weatherRoot.transform, false);
            ParticleSystem motePs = moteGo.AddComponent<ParticleSystem>();
            var mMain = motePs.main;
            mMain.startLifetime = 4.2f;
            mMain.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 1.1f);
            mMain.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.17f);
            mMain.startColor = new Color(1.0f, 0.88f, 0.56f, 0.34f);
            mMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            mMain.maxParticles = 140;
            var mEmit = motePs.emission;
            mEmit.rateOverTime = 6f;
            var mShape = motePs.shape;
            mShape.shapeType = ParticleSystemShapeType.Box;
            mShape.scale = new Vector3(24f, 12.5f, 0.1f);
            var mRen = moteGo.GetComponent<ParticleSystemRenderer>();
            mRen.sortingOrder = 19;
            if (shader != null) mRen.sharedMaterial = new Material(shader) { mainTexture = dotSprite.texture };

            DesertAtmosphereController atmo = weatherRoot.AddComponent<DesertAtmosphereController>();
            SerializedObject atmoSo = new SerializedObject(atmo);
            atmoSo.FindProperty("chunkDirector").objectReferenceValue = director;
            atmoSo.FindProperty("targetCamera").objectReferenceValue = mainCam;
            atmoSo.FindProperty("postProcessVolume").objectReferenceValue = postProcessVol;
            atmoSo.FindProperty("cameraFollowAnchor").objectReferenceValue = weatherRoot.transform;
            atmoSo.FindProperty("weatherHazeRenderer").objectReferenceValue = hazeSr;
            SerializedProperty panoProp = atmoSo.FindProperty("panoramaRenderers");
            panoProp.arraySize = panoRenderers.Length;
            for (int i = 0; i < panoRenderers.Length; i++) panoProp.GetArrayElementAtIndex(i).objectReferenceValue = panoRenderers[i];
            SerializedProperty duneProp = atmoSo.FindProperty("duneRenderers");
            duneProp.arraySize = duneRenderers.Length;
            for (int i = 0; i < duneRenderers.Length; i++) duneProp.GetArrayElementAtIndex(i).objectReferenceValue = duneRenderers[i];
            atmoSo.FindProperty("sandWindParticles").objectReferenceValue = windPs;
            atmoSo.FindProperty("ambientMoteParticles").objectReferenceValue = motePs;
            atmoSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject SaveCrystalPrefab(Sprite stdSprite, Sprite boostSprite, Sprite fragileSprite, Sprite umbraSprite, Sprite rimSprite, Sprite starSprite)
        {
            GameObject go = new GameObject("PF_CrystalPlatform");
            go.layer = HardlightWorldLayer;
            go.transform.localScale = new Vector3(2.4f, 0.52f, 1f);

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            // Kalın Mısır Taşı görseli: yürünür üst kenar tam BoxCollider topY (+0.50)'ye kilitli!
            GameObject artObj = new GameObject("PlatformArt");
            artObj.transform.SetParent(go.transform, false);
            artObj.transform.localPosition = new Vector3(0f, 0.50f, 0f);
            artObj.transform.localScale = new Vector3(1f, 2.85f, 1f);
            SpriteRenderer artSr = artObj.AddComponent<SpriteRenderer>();
            artSr.sprite = stdSprite;
            artSr.sortingOrder = 5;

            GameObject leftSparkle = new GameObject("SparkleLeft");
            leftSparkle.transform.SetParent(go.transform, false);
            leftSparkle.transform.localPosition = new Vector3(-0.48f, 0.48f, 0f);
            SpriteRenderer leftSr = leftSparkle.AddComponent<SpriteRenderer>();
            leftSr.sprite = starSprite;
            leftSr.color = new Color(1.0f, 0.86f, 0.42f, 0.98f);
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
            platSo.FindProperty("visualRenderer").objectReferenceValue = artSr;
            platSo.FindProperty("standardCrystalSprite").objectReferenceValue = stdSprite;
            platSo.FindProperty("boostCrystalSprite").objectReferenceValue = boostSprite;
            platSo.FindProperty("fragileCrystalSprite").objectReferenceValue = fragileSprite;
            platSo.FindProperty("umbraCrystalSprite").objectReferenceValue = umbraSprite;
            platSo.FindProperty("rimGlowRenderer").objectReferenceValue = null;
            platSo.FindProperty("cornerSparkleLeft").objectReferenceValue = leftSparkle.transform;
            platSo.FindProperty("cornerSparkleRight").objectReferenceValue = rightSparkle.transform;
            platSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, Path.Combine(PrefabsFolder, "PF_CrystalPlatform.prefab"));
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject SaveOrbPrefab(string fileName, bool isSuper, Sprite wispRelicSprite, Sprite auraSprite)
        {
            GameObject go = new GameObject(fileName.Replace(".prefab", ""));
            float s = isSuper ? 0.94f : 0.78f;
            go.transform.localScale = new Vector3(s, s, 1f);

            SpriteRenderer frameSr = go.AddComponent<SpriteRenderer>();
            frameSr.sprite = wispRelicSprite;
            frameSr.sortingOrder = 17;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.56f;

            GameObject auraObj = new GameObject("OrbHalo");
            auraObj.transform.SetParent(go.transform, false);
            auraObj.transform.localScale = new Vector3(1.38f, 1.38f, 1f);
            SpriteRenderer auraSr = auraObj.AddComponent<SpriteRenderer>();
            auraSr.sprite = auraSprite;
            auraSr.color = isSuper ? new Color(0.28f, 0.92f, 0.84f, 0.32f) : new Color(1f, 0.82f, 0.34f, 0.24f);
            auraSr.sortingOrder = 16;

            EnergyOrb orb = go.AddComponent<EnergyOrb>();
            SerializedObject orbSo = new SerializedObject(orb);
            orbSo.FindProperty("isSuperCharge").boolValue = isSuper;
            orbSo.FindProperty("innerBoltRenderer").objectReferenceValue = null;
            orbSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, Path.Combine(PrefabsFolder, fileName));
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject SaveScarabCoinPrefab(string fileName, Sprite coinSprite, Sprite auraSprite)
        {
            GameObject go = new GameObject(fileName.Replace(".prefab", ""));
            go.transform.localScale = new Vector3(0.68f, 0.68f, 1f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = coinSprite;
            sr.sortingOrder = 17;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.52f;

            GameObject auraObj = new GameObject("CoinHalo");
            auraObj.transform.SetParent(go.transform, false);
            auraObj.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
            SpriteRenderer auraSr = auraObj.AddComponent<SpriteRenderer>();
            auraSr.sprite = auraSprite;
            auraSr.color = new Color(1.0f, 0.84f, 0.25f, 0.24f);
            auraSr.sortingOrder = 16;

            CoinCollectible coin = go.AddComponent<CoinCollectible>();
            SerializedObject coinSo = new SerializedObject(coin);
            coinSo.FindProperty("coinRenderer").objectReferenceValue = sr;
            coinSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, Path.Combine(PrefabsFolder, fileName));
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject SaveJumpPadPrefab(Sprite jumpPadSprite, Sprite coreSprite)
        {
            GameObject go = new GameObject("PF_PrismaticJumpPad");
            go.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

            SpriteRenderer ringSr = go.AddComponent<SpriteRenderer>();
            ringSr.sprite = jumpPadSprite;
            ringSr.sortingOrder = 12;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.offset = new Vector2(0f, 0.34f);
            col.size = new Vector2(1.48f, 0.60f);

            GameObject coreObj = new GameObject("InnerWindGlow");
            coreObj.transform.SetParent(go.transform, false);
            coreObj.transform.localPosition = new Vector3(0f, 0.44f, 0f);
            coreObj.transform.localScale = new Vector3(0.95f, 0.32f, 1f);
            SpriteRenderer coreSr = coreObj.AddComponent<SpriteRenderer>();
            coreSr.sprite = coreSprite;
            coreSr.color = new Color(0.24f, 0.88f, 0.80f, 0.42f);
            coreSr.sortingOrder = 11;

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
            go.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

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
                box.offset = new Vector2(0f, -0.04f);
                box.size = new Vector2(0.76f, 2.05f);
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
            // ÖNEMLİ: Kök ölçeği (1,1,1) tutulur; üstteki mühür asla ezilmez/büzülmez!
            GameObject go = new GameObject("PF_PrismLockGate");
            go.transform.localScale = Vector3.one;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.56f, 3.20f);

            GameObject barrierVisualObj = new GameObject("BarrierVisual");
            barrierVisualObj.transform.SetParent(go.transform, false);
            barrierVisualObj.transform.localScale = new Vector3(0.76f, 0.84f, 1f);
            SpriteRenderer barrierSr = barrierVisualObj.AddComponent<SpriteRenderer>();
            barrierSr.sprite = barrierSprite;
            barrierSr.sortingOrder = 16;

            GameObject lockObj = new GameObject("DiamondLockSwitch");
            lockObj.transform.SetParent(go.transform, false);
            lockObj.transform.localPosition = new Vector3(0f, 2.28f, 0f);
            lockObj.transform.localScale = new Vector3(0.84f, 0.84f, 1f);
            SpriteRenderer lockSr = lockObj.AddComponent<SpriteRenderer>();
            lockSr.sprite = lockDiamondSprite;
            lockSr.sortingOrder = 19;

            // Bariyer ile üst Eye-of-Ra kilidi arasındaki antik mühür bağ ışığı
            LineRenderer linkLine = go.AddComponent<LineRenderer>();
            linkLine.useWorldSpace = false;
            linkLine.positionCount = 2;
            linkLine.SetPosition(0, new Vector3(0f, 1.56f, 0f));
            linkLine.SetPosition(1, new Vector3(0f, 2.28f, 0f));
            linkLine.startWidth = 0.06f;
            linkLine.endWidth = 0.06f;
            linkLine.sortingOrder = 17;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                string lineMatPath = Path.Combine(DataFolder, "TempleWardLineMat.mat").Replace("\\", "/");
                Material lineMat = AssetDatabase.LoadAssetAtPath<Material>(lineMatPath);
                if (lineMat == null)
                {
                    lineMat = new Material(shader);
                    AssetDatabase.CreateAsset(lineMat, lineMatPath);
                }
                linkLine.sharedMaterial = lineMat;
            }
            linkLine.startColor = new Color(0.88f, 0.42f, 0.22f, 0.82f);
            linkLine.endColor = new Color(1.0f, 0.84f, 0.36f, 0.92f);

            PrismLockGate gate = go.AddComponent<PrismLockGate>();
            SerializedObject so = new SerializedObject(gate);
            so.FindProperty("barrierVisualRenderer").objectReferenceValue = barrierSr;
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
            go.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

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

            Color darkStone = new Color(0.24f, 0.18f, 0.14f, 0.94f);
            Color warmSand = new Color(0.58f, 0.44f, 0.30f, 0.94f);
            Color goldRune = new Color(1.0f, 0.82f, 0.30f, 0.98f);
            Color scarabRim = new Color(0.10f, 0.86f, 0.78f, 0.92f);

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    bool isTopEdge = y >= h - 6;
                    bool isBorder = x < 4 || x >= w - 4 || y < 5;

                    // Antik Mısır kumtaşı blok derzi ve hiyeroglif zikzak bandı
                    float blockU = Mathf.Repeat(nx * 4f, 1f);
                    bool isStoneJoint = (blockU < 0.03f && ny > 0.25f) || Mathf.Abs(ny - 0.74f) < 0.025f || Mathf.Abs(ny - 0.24f) < 0.025f;
                    float hieroWave = Mathf.Abs((ny - 0.49f) - Mathf.Sin(nx * Mathf.PI * 16f) * 0.09f);
                    bool isHieroglyph = hieroWave < 0.026f && nx > 0.06f && nx < 0.94f;

                    Color c;
                    if (isTopEdge) c = new Color(1.0f, 0.94f, 0.72f, 1f);
                    else if (isBorder) c = goldRune;
                    else if (isHieroglyph) c = scarabRim;
                    else if (isStoneJoint) c = goldRune * new Color(0.85f, 0.85f, 0.85f, 0.9f);
                    else c = Color.Lerp(darkStone, warmSand, ny * 0.85f);

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
                    bool isChevron = chevronRepeat < 0.25f && ny > 0.18f && ny < 0.82f;

                    if (isBorder) pixels[y * w + x] = new Color(1.0f, 0.86f, 0.34f, 1f);
                    else if (isChevron) pixels[y * w + x] = new Color(1.0f, 0.92f, 0.45f, 0.98f);
                    else pixels[y * w + x] = Color.Lerp(new Color(0.06f, 0.38f, 0.38f, 0.92f), new Color(0.10f, 0.78f, 0.72f, 0.94f), ny);
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
                    float crack = Mathf.Abs(Mathf.Sin(nx * 18f + ny * 9f) * 0.13f - (ny - 0.5f));
                    bool isCrack = crack < 0.028f || Mathf.Abs(nx - 0.34f) < 0.015f || Mathf.Abs(nx - 0.68f) < 0.015f;

                    if (isBorder) pixels[y * w + x] = new Color(0.96f, 0.86f, 0.66f, 0.98f);
                    else if (isCrack) pixels[y * w + x] = new Color(0.16f, 0.11f, 0.09f, 0.96f);
                    else pixels[y * w + x] = Color.Lerp(new Color(0.64f, 0.51f, 0.34f, 0.88f), new Color(0.88f, 0.76f, 0.58f, 0.90f), ny);
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
                    bool isCurseSeal = stripe < 0.24f;

                    if (isBorder) pixels[y * w + x] = new Color(0.95f, 0.28f, 0.62f, 1f);
                    else if (isCurseSeal) pixels[y * w + x] = new Color(0.68f, 0.18f, 0.84f, 0.90f);
                    else pixels[y * w + x] = new Color(0.11f, 0.05f, 0.16f, 0.96f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 256f);
        }

        private static Sprite GeneratePyramidSprite(string assetPath)
        {
            const int w = 256;
            const int h = 160;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    float dxFromCenter = nx - 0.5f;
                    float halfSpan = (1f - ny) * 0.49f;

                    if (Mathf.Abs(dxFromCenter) <= halfSpan)
                    {
                        bool isApexCap = ny > 0.87f;
                        bool isLeftLit = dxFromCenter < 0.01f;
                        bool isRim = Mathf.Abs(Mathf.Abs(dxFromCenter) - halfSpan) < 0.016f && isLeftLit;
                        // Yatay antik taş katman dokusu
                        bool isLayer = (y % 14) == 0 && ny < 0.87f;

                        if (isApexCap) pixels[y * w + x] = new Color(1.0f, 0.84f, 0.36f, 0.95f);
                        else if (isRim) pixels[y * w + x] = new Color(0.86f, 0.82f, 0.95f, 0.95f);
                        else if (isLeftLit) pixels[y * w + x] = isLayer ? new Color(0.65f, 0.60f, 0.78f, 0.95f) : new Color(0.76f, 0.70f, 0.88f, 0.98f);
                        else pixels[y * w + x] = isLayer ? new Color(0.32f, 0.28f, 0.42f, 0.96f) : new Color(0.40f, 0.36f, 0.52f, 0.98f);
                    }
                    else
                    {
                        pixels[y * w + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 160f);
        }

        private static Sprite GenerateObeliskSilhouetteSprite(string assetPath)
        {
            const int w = 64;
            const int h = 192;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    float dx = nx - 0.5f;
                    float halfW = ny > 0.86f
                        ? Mathf.Lerp(0.28f, 0.01f, (ny - 0.86f) / 0.14f)
                        : Mathf.Lerp(0.42f, 0.28f, ny / 0.86f);

                    if (Mathf.Abs(dx) <= halfW)
                    {
                        bool isGlyph = Mathf.Abs(dx) < 0.05f && (y % 18) < 6 && ny < 0.82f && ny > 0.15f;
                        if (isGlyph) pixels[y * w + x] = new Color(0.98f, 0.80f, 0.34f, 0.85f);
                        else if (dx < 0f) pixels[y * w + x] = new Color(0.74f, 0.68f, 0.86f, 0.98f);
                        else pixels[y * w + x] = new Color(0.44f, 0.38f, 0.54f, 0.98f);
                    }
                    else pixels[y * w + x] = Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 128f);
        }

        private static Sprite GenerateSandDuneSprite(string assetPath)
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
                    float crest = 0.52f + Mathf.Sin(nx * Mathf.PI) * 0.42f + Mathf.Sin(nx * Mathf.PI * 2f) * 0.06f;
                    if (ny <= crest)
                    {
                        float distRidge = crest - ny;
                        Color c = distRidge < 0.035f
                            ? new Color(0.84f, 0.72f, 0.62f, 1f)
                            : Color.Lerp(new Color(0.26f, 0.20f, 0.24f, 1f), new Color(0.56f, 0.44f, 0.46f, 1f), ny / Mathf.Max(0.01f, crest));
                        pixels[y * w + x] = c;
                    }
                    else pixels[y * w + x] = Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 128f);
        }

        private static Sprite GenerateDesertMoonSprite(string assetPath)
        {
            const int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            float c = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - c) / c;
                    float ny = (y - c) / c;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    if (r <= 0.55f)
                    {
                        float crater = Mathf.Sin(nx * 8f) * Mathf.Cos(ny * 7f) * 0.05f;
                        pixels[y * size + x] = new Color(1.0f, 0.94f + crater, 0.78f + crater, 0.96f);
                    }
                    else if (r <= 1.0f)
                    {
                        float halo = Mathf.Pow(Mathf.Clamp01(1f - (r - 0.55f) / 0.45f), 2.2f) * 0.35f;
                        pixels[y * size + x] = new Color(1.0f, 0.90f, 0.68f, halo);
                    }
                    else pixels[y * size + x] = Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 128f);
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
                        Color darkVoid = new Color(0.08f, 0.04f, 0.05f, 0.95f);
                        Color crimsonEdge = new Color(0.95f, 0.34f, 0.16f, 0.98f);
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

        private static Sprite GenerateDetectiveTorsoSprite(string assetPath)
        {
            const int w = 160;
            const int h = 160;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            // Referans Dedektif Renk Paleti
            Color coatBase = new Color(0.737f, 0.580f, 0.373f, 1f);    // #BC945F Deve tüyü trençkot
            Color coatLight = new Color(0.875f, 0.745f, 0.576f, 1f);   // #DFBE93 Açık yaka/vurgu
            Color coatShadow = new Color(0.659f, 0.518f, 0.325f, 1f);  // #A88453 Gölge kıvrım
            Color darkBrown = new Color(0.357f, 0.251f, 0.224f, 1f);   // #5B4039 Yaka tersi & kemer
            Color outline = new Color(0.122f, 0.090f, 0.102f, 1f);     // #1F171A Keskin dış kontur
            Color shirtCream = new Color(0.961f, 0.886f, 0.604f, 1f);  // #F5E29A Gömlek kremi
            Color brassGold = new Color(1.0f, 0.824f, 0.298f, 1f);     // #FFD24C Pirinç toka/düğme

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    Color c = Color.clear;

                    if (ny >= 0.06f && ny <= 0.90f)
                    {
                        float torsoT = (ny - 0.06f) / 0.84f;
                        float halfW = Mathf.Lerp(0.24f, 0.28f, torsoT);
                        float centerX = Mathf.Lerp(0.47f, 0.51f, torsoT);
                        float dx = nx - centerX;
                        float absDx = Mathf.Abs(dx);

                        if (absDx <= halfW)
                        {
                            bool isOuterRim = absDx > halfW - 0.032f || ny < 0.09f || (ny > 0.86f && absDx > 0.12f);
                            bool isBelt = Mathf.Abs(ny - 0.26f) < 0.055f;
                            bool isBeltBuckle = isBelt && Mathf.Abs(dx - 0.08f) < 0.045f;
                            bool isShirtV = ny > 0.62f && absDx < (ny - 0.60f) * 0.42f;
                            bool isTie = ny > 0.63f && Mathf.Abs(dx - 0.01f) < 0.025f;
                            bool isCollarWing = ny > 0.70f && !isShirtV && absDx < 0.19f && ny < 0.90f;
                            bool isButtons = (Mathf.Abs(ny - 0.48f) < 0.022f || Mathf.Abs(ny - 0.36f) < 0.022f) && Mathf.Abs(dx - 0.08f) < 0.022f;

                            if (isOuterRim) c = outline;
                            else if (isBeltBuckle) c = brassGold;
                            else if (isBelt) c = darkBrown;
                            else if (isTie) c = outline;
                            else if (isShirtV) c = shirtCream;
                            else if (isCollarWing) c = (ny > 0.81f ? darkBrown : coatLight);
                            else if (isButtons) c = darkBrown;
                            else c = dx < -0.06f ? coatShadow : (dx > 0.11f ? coatLight : coatBase);
                        }
                    }

                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 145f);
        }

        private static Sprite GenerateDetectiveHeadHatSprite(string assetPath)
        {
            const int w = 160;
            const int h = 160;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            Color hatCrown = new Color(0.357f, 0.251f, 0.224f, 1f);    // #5B4039
            Color hatHighlight = new Color(0.514f, 0.424f, 0.329f, 1f);// #836C54
            Color outline = new Color(0.122f, 0.090f, 0.102f, 1f);     // #1F171A
            Color skin = new Color(0.875f, 0.745f, 0.576f, 1f);        // #DFBE93
            Color skinShadow = new Color(0.737f, 0.580f, 0.373f, 1f);  // #BC945F
            Color eyeGlow = new Color(1.0f, 0.92f, 0.56f, 1f);         // Keskin dedektif bakışı

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    Color c = Color.clear;

                    // Yüz silüeti (sağa bakan profil)
                    Vector2 headCenter = new Vector2(0.49f, 0.36f);
                    float dHead = Vector2.Distance(new Vector2((nx - headCenter.x) * 1.05f, (ny - headCenter.y) * 0.92f), Vector2.zero);
                    bool nose = nx >= 0.65f && nx <= 0.73f && ny >= 0.31f && ny <= 0.42f && (nx - 0.65f) <= (0.42f - ny) * 0.8f;

                    if (dHead <= 0.21f || nose)
                    {
                        bool rim = (!nose && dHead > 0.18f) || (nose && (nx > 0.705f || ny < 0.325f));
                        bool eye = nx >= 0.55f && nx <= 0.63f && ny >= 0.37f && ny <= 0.43f;
                        if (rim) c = outline;
                        else if (eye) c = eyeGlow;
                        else c = nx < 0.42f ? skinShadow : skin;
                    }

                    // Fedora şapka siperliği (sağa hafif aşağı açılı stilize gölge)
                    float brimCenterY = 0.53f - (nx - 0.50f) * 0.10f;
                    if (nx >= 0.18f && nx <= 0.84f && Mathf.Abs(ny - brimCenterY) <= 0.042f)
                    {
                        bool rim = Mathf.Abs(ny - brimCenterY) > 0.025f || nx < 0.21f || nx > 0.81f;
                        c = rim ? outline : hatCrown;
                    }

                    // Fedora şapka tacı
                    if (ny >= 0.54f && ny <= 0.86f)
                    {
                        float crownT = (ny - 0.54f) / 0.32f;
                        float halfW = Mathf.Lerp(0.22f, 0.15f, crownT);
                        float dx = nx - 0.48f;
                        float notchDip = Mathf.Abs(dx) < 0.06f && ny > 0.80f ? 0.04f : 0f;
                        if (Mathf.Abs(dx) <= halfW && ny <= 0.86f - notchDip)
                        {
                            bool rim = Mathf.Abs(dx) > halfW - 0.025f || ny > 0.83f - notchDip;
                            bool hatBand = ny >= 0.55f && ny <= 0.61f;
                            if (rim) c = outline;
                            else if (hatBand) c = outline;
                            else c = dx > 0.03f ? hatHighlight : hatCrown;
                        }
                    }

                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 145f);
        }

        private static Sprite GenerateDetectiveCoatTailSprite(string assetPath)
        {
            const int w = 96;
            const int h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            Color coatBase = new Color(0.737f, 0.580f, 0.373f, 1f);
            Color coatShadow = new Color(0.659f, 0.518f, 0.325f, 1f);
            Color outline = new Color(0.122f, 0.090f, 0.102f, 1f);

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    float centerX = Mathf.Lerp(0.36f, 0.54f, ny);
                    float halfW = Mathf.Lerp(0.28f, 0.22f, ny);
                    float dx = Mathf.Abs(nx - centerX);
                    if (ny >= 0.06f && ny <= 0.96f && dx <= halfW)
                    {
                        bool rim = dx > halfW - 0.045f || ny < 0.10f;
                        pixels[y * w + x] = rim ? outline : (nx < centerX ? coatShadow : coatBase);
                    }
                    else pixels[y * w + x] = Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 110f);
        }

        private static Sprite GenerateDetectiveArmSprite(string assetPath)
        {
            const int w = 48;
            const int h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            Color coatBase = new Color(0.737f, 0.580f, 0.373f, 1f);
            Color coatShadow = new Color(0.659f, 0.518f, 0.325f, 1f);
            Color darkBrown = new Color(0.357f, 0.251f, 0.224f, 1f);
            Color skinHand = new Color(0.875f, 0.745f, 0.576f, 1f);
            Color outline = new Color(0.122f, 0.090f, 0.102f, 1f);

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    float dx = Mathf.Abs(nx - 0.5f);
                    float halfWidth = Mathf.Lerp(0.28f, 0.38f, ny);
                    if (dx <= halfWidth && ny >= 0.04f && ny <= 0.96f)
                    {
                        bool rim = dx > halfWidth - 0.07f || ny < 0.08f || ny > 0.92f;
                        if (rim) pixels[y * w + x] = outline;
                        else if (ny < 0.16f) pixels[y * w + x] = skinHand;
                        else if (ny < 0.28f) pixels[y * w + x] = darkBrown;
                        else pixels[y * w + x] = nx < 0.45f ? coatShadow : coatBase;
                    }
                    else pixels[y * w + x] = Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 96f);
        }

        private static Sprite GenerateDetectiveThighSprite(string assetPath)
        {
            const int w = 48;
            const int h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            Color pants = new Color(0.514f, 0.424f, 0.329f, 1f);     // #836C54
            Color dark = new Color(0.357f, 0.251f, 0.224f, 1f);      // #5B4039
            Color outline = new Color(0.122f, 0.090f, 0.102f, 1f);

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    float dx = Mathf.Abs(nx - 0.5f);
                    float halfW = Mathf.Lerp(0.28f, 0.38f, ny);
                    if (dx <= halfW && ny >= 0.04f && ny <= 0.96f)
                    {
                        bool rim = dx > halfW - 0.075f;
                        pixels[y * w + x] = rim ? outline : (nx < 0.44f ? dark : pants);
                    }
                    else pixels[y * w + x] = Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 96f);
        }

        private static Sprite GenerateDetectiveShinBootSprite(string assetPath)
        {
            const int w = 64;
            const int h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            Color pants = new Color(0.514f, 0.424f, 0.329f, 1f);
            Color boot = new Color(0.145f, 0.105f, 0.115f, 1f);
            Color bootHighlight = new Color(0.357f, 0.251f, 0.224f, 1f);
            Color outline = new Color(0.08f, 0.06f, 0.07f, 1f);

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    bool inBootFoot = ny >= 0.04f && ny <= 0.28f && nx >= 0.24f && nx <= Mathf.Lerp(0.88f, 0.62f, (ny - 0.04f) / 0.24f);
                    bool inLegCol = ny > 0.24f && ny <= 0.96f && Mathf.Abs(nx - 0.42f) <= Mathf.Lerp(0.20f, 0.24f, ny);

                    if (inBootFoot)
                    {
                        pixels[y * w + x] = ny > 0.20f && nx > 0.56f ? bootHighlight : boot;
                    }
                    else if (inLegCol)
                    {
                        bool rim = Mathf.Abs(nx - 0.42f) > Mathf.Lerp(0.20f, 0.24f, ny) - 0.055f;
                        if (rim) pixels[y * w + x] = outline;
                        else pixels[y * w + x] = ny < 0.42f ? boot : pants;
                    }
                    else pixels[y * w + x] = Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return SaveAndImportSprite(tex, assetPath, 96f);
        }

        private static Sprite GenerateVintageFlashlightSprite(string assetPath)
        {
            const int w = 96;
            const int h = 56;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];

            Color handle = new Color(0.357f, 0.251f, 0.224f, 1f);
            Color brass = new Color(0.95f, 0.74f, 0.24f, 1f);
            Color lens = new Color(1.0f, 0.96f, 0.76f, 1f);
            Color outline = new Color(0.122f, 0.090f, 0.102f, 1f);

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1);
                float dy = Mathf.Abs(ny - 0.5f);
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1);
                    float halfH = nx < 0.58f ? 0.19f : Mathf.Lerp(0.19f, 0.42f, (nx - 0.58f) / 0.34f);
                    if (nx >= 0.08f && nx <= 0.94f && dy <= halfH)
                    {
                        bool rim = dy > halfH - 0.06f || nx < 0.12f || nx > 0.90f;
                        if (rim) pixels[y * w + x] = outline;
                        else if (nx > 0.84f) pixels[y * w + x] = lens;
                        else if (nx >= 0.56f) pixels[y * w + x] = brass;
                        else pixels[y * w + x] = handle;
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

            Color hotCore = new Color(1.0f, 0.97f, 0.84f, 1f);      // Sıcak dedektif feneri beyaz-altın çekirdek
            Color goldBeam = new Color(1.0f, 0.78f, 0.24f, 1f);     // Kehribar-altın huzme gövdesi
            Color scarabRim = new Color(0.08f, 0.88f, 0.80f, 1f);   // Antik Mısır turkuazı keşif kenarı

            for (int y = 0; y < h; y++)
            {
                float v = (float)y / (h - 1);
                float distFromCenter = Mathf.Abs(v - 0.5f) * 2f;
                float angularAlpha = Mathf.Pow(Mathf.Clamp01(1f - distFromCenter), 1.45f);
                float scarabRimBand = Mathf.Exp(-Mathf.Pow((distFromCenter - 0.68f) / 0.18f, 2f));

                for (int x = 0; x < w; x++)
                {
                    float u = (float)x / (w - 1);
                    float radialAlpha = Mathf.Pow(Mathf.Clamp01(1f - u), 1.35f);

                    Color baseCol = Color.Lerp(hotCore, goldBeam, Mathf.Clamp01(u * 1.35f + distFromCenter * 0.45f));
                    baseCol = Color.Lerp(baseCol, scarabRim, scarabRimBand * 0.45f);

                    float finalAlpha = radialAlpha * angularAlpha * 0.86f;
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

        private static Sprite LoadAndImportEgyptSprite(string fileName, float ppu, Vector2? customPivot = null, Vector4? spriteBorder = null)
        {
            string assetPath = Path.Combine(EgyptSpritesFolder, fileName).Replace("\\", "/");
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = ppu;
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaIsTransparency = true;
                if (customPivot.HasValue)
                {
                    TextureImporterSettings settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = customPivot.Value;
                    importer.SetTextureSettings(settings);
                }
                if (spriteBorder.HasValue)
                {
                    importer.spriteBorder = spriteBorder.Value;
                }
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        #endregion
    }
}
