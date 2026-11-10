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
    /// Faz 1 (Çekirdek Işık & Kristalleşme Motoru) için prosedürel yüksek kontrastlı neon-prizmatik
    /// kristal dokularını üretir ve sahneyi kalıcı olarak inşa eder.
    /// </summary>
    public static class LightWeaverPhase1Setup
    {
        private const string SpritesFolder = "Assets/_Project/Art/Sprites";
        private const string PrefabsFolder = "Assets/_Project/Prefabs";

        [MenuItem("LightWeaver/Phase 1/1. Generate Crystal Sprites & Build Phase 1 Scene")]
        public static void BuildPhase1Scene()
        {
            EnsureDirectories();

            Sprite crystalTileSprite = GenerateCrystalPlatformSprite(Path.Combine(SpritesFolder, "prism_crystal_block.png"));
            Sprite rimGlowSprite = GenerateCrystalRimSprite(Path.Combine(SpritesFolder, "prism_crystal_rim.png"));
            Sprite starburstSprite = GenerateStarburstSprite(Path.Combine(SpritesFolder, "diamond_starburst.png"));
            Sprite emitterCoreSprite = GenerateEmitterCoreSprite(Path.Combine(SpritesFolder, "flashlight_emitter_core.png"));

            AssetDatabase.Refresh();

            // Kamera Ayarları (Zifiri Karanlık Uzay #05070D)
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
            mainCam.backgroundColor = new Color(0.02f, 0.027f, 0.051f, 1f); // #05070D The Void
            mainCam.transform.position = new Vector3(3.5f, 0.6f, -10f);

            // Eski kurulum köklerini temizle
            DestroyExistingIfFound("=== LIGHT WEAVER SYSTEMS ===");
            DestroyExistingIfFound("=== PLAYER FLASHLIGHT RIG ===");
            DestroyExistingIfFound("=== PRISM CRYSTAL WORLD ===");

            // 1. Sistemler & ObjectPooler
            GameObject systemsRoot = new GameObject("=== LIGHT WEAVER SYSTEMS ===");
            systemsRoot.AddComponent<ObjectPooler>();

            // 2. Fener Rig & Volümetrik Işık Konisi
            GameObject playerRig = new GameObject("=== PLAYER FLASHLIGHT RIG ===");
            playerRig.transform.position = new Vector3(-4.2f, -0.5f, 0f);

            GameObject emitterCore = new GameObject("FlashlightEmitterCore");
            emitterCore.transform.SetParent(playerRig.transform, false);
            SpriteRenderer coreSr = emitterCore.AddComponent<SpriteRenderer>();
            coreSr.sprite = emitterCoreSprite;
            coreSr.color = new Color(1f, 0.92f, 0.45f, 1f);
            coreSr.sortingOrder = 20;

            GameObject coneObj = new GameObject("LightConeCaster2D");
            coneObj.transform.SetParent(playerRig.transform, false);
            LightConeCaster2D caster = coneObj.AddComponent<LightConeCaster2D>();

            FlashlightInputPreview inputPreview = playerRig.AddComponent<FlashlightInputPreview>();
            SerializedObject inputSo = new SerializedObject(inputPreview);
            inputSo.FindProperty("coneCaster").objectReferenceValue = caster;
            inputSo.FindProperty("targetCamera").objectReferenceValue = mainCam;
            inputSo.ApplyModifiedPropertiesWithoutUndo();

            // 3. Kristal Platform Prefab'ı ve Sahne Dizilimi (Görsel Referanstaki Kademeli Yükseliş)
            GameObject worldRoot = new GameObject("=== PRISM CRYSTAL WORLD ===");

            // Başlangıç platformu (Oyuncunun altında daima katı)
            CrystalPlatform startPlat = CreateCrystalPlatformInstance(
                "CrystalPlatform_StartBase",
                worldRoot.transform,
                new Vector3(-4.2f, -1.6f, 0f),
                new Vector2(2.6f, 0.55f),
                0f,
                crystalTileSprite,
                rimGlowSprite,
                starburstSprite);
            startPlat.ForcePermanentSolid();

            // Görsel referanstaki orta ve yukarı tırmanan kristal basamaklar & rampalar
            CreateCrystalPlatformInstance("CrystalPlatform_Mid_01", worldRoot.transform, new Vector3(-0.8f, -1.4f, 0f), new Vector2(2.4f, 0.5f), 4f, crystalTileSprite, rimGlowSprite, starburstSprite);
            CreateCrystalPlatformInstance("CrystalPlatform_Mid_02", worldRoot.transform, new Vector3(1.8f, -1.1f, 0f), new Vector2(2.2f, 0.5f), 6f, crystalTileSprite, rimGlowSprite, starburstSprite);
            CreateCrystalPlatformInstance("CrystalPlatform_Step_03", worldRoot.transform, new Vector3(4.6f, -0.2f, 0f), new Vector2(2.1f, 0.48f), 14f, crystalTileSprite, rimGlowSprite, starburstSprite);
            CreateCrystalPlatformInstance("CrystalPlatform_Ramp_04", worldRoot.transform, new Vector3(7.1f, 1.35f, 0f), new Vector2(2.5f, 0.48f), 34f, crystalTileSprite, rimGlowSprite, starburstSprite);
            CreateCrystalPlatformInstance("CrystalPlatform_Canopy_05", worldRoot.transform, new Vector3(9.6f, 2.75f, 0f), new Vector2(2.2f, 0.48f), 8f, crystalTileSprite, rimGlowSprite, starburstSprite);

            // Alt uçurum alternatif rota kristalleri
            CreateCrystalPlatformInstance("CrystalPlatform_Low_01", worldRoot.transform, new Vector3(3.2f, -2.6f, 0f), new Vector2(2.3f, 0.48f), -5f, crystalTileSprite, rimGlowSprite, starburstSprite);
            CreateCrystalPlatformInstance("CrystalPlatform_Low_02", worldRoot.transform, new Vector3(6.4f, -2.8f, 0f), new Vector2(2.5f, 0.48f), 0f, crystalTileSprite, rimGlowSprite, starburstSprite);

            // Prefab olarak da kaydet
            CrystalPlatform prefabTemplate = CreateCrystalPlatformInstance("PF_CrystalPlatform", null, Vector3.zero, new Vector2(2.4f, 0.5f), 0f, crystalTileSprite, rimGlowSprite, starburstSprite);
            PrefabUtility.SaveAsPrefabAsset(prefabTemplate.gameObject, Path.Combine(PrefabsFolder, "PF_CrystalPlatform.prefab"));
            Object.DestroyImmediate(prefabTemplate.gameObject);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[LightWeaver Phase 1] Hardlight Engine, Volumetric Cone & Faceted Crystal Platforms built and saved to scene!");
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
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer mainSr = go.AddComponent<SpriteRenderer>();
            mainSr.sprite = bodySprite;
            mainSr.sortingOrder = 5;

            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            // Rim Glow (Üst ve Alt Kenar Parlaması)
            GameObject rimObj = new GameObject("RimGlow");
            rimObj.transform.SetParent(go.transform, false);
            SpriteRenderer rimSr = rimObj.AddComponent<SpriteRenderer>();
            rimSr.sprite = rimSprite;
            rimSr.sortingOrder = 6;

            // Köşe Elmas Parlamaları (Starbursts)
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

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(SpritesFolder)) Directory.CreateDirectory(SpritesFolder);
            if (!Directory.Exists(PrefabsFolder)) Directory.CreateDirectory(PrefabsFolder);
        }

        private static void DestroyExistingIfFound(string objName)
        {
            GameObject existing = GameObject.Find(objName);
            if (existing != null) Object.DestroyImmediate(existing);
        }

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

                    // Low-poly fasetli kristal diyagonal çizgileri
                    float diag1 = Mathf.Abs((nx - ny * 0.5f) - 0.25f);
                    float diag2 = Mathf.Abs((nx + ny * 0.5f) - 0.75f);
                    float centerFacet = Mathf.Abs(ny - 0.55f);

                    bool isBorder = x < 3 || x >= w - 3 || y < 3 || y >= h - 3;
                    bool isFacetLine = diag1 < 0.018f || diag2 < 0.018f || centerFacet < 0.02f;

                    Color c;
                    if (isBorder)
                    {
                        c = new Color(0.85f, 0.98f, 1.0f, 1.0f);
                    }
                    else if (isFacetLine)
                    {
                        c = new Color(0.55f, 0.92f, 1.0f, 0.88f);
                    }
                    else
                    {
                        // Prizmatik mavi-eflatun iç gradyan
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
                    float a = Mathf.Clamp01(cross + core);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
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
    }
}
