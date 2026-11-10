using UnityEngine;

namespace LightWeaver.World
{
    /// <summary>
    /// Light Weaver'daki 5 Sektörün (Level 1..5) hız, pil tüketimi, renk atmosferi
    /// ve gölge tehlike yoğunluğunu tanımlayan veri güdümlü ScriptableObject profili.
    /// </summary>
    [CreateAssetMenu(menuName = "LightWeaver/Sector Profile", fileName = "SectorProfile")]
    public sealed class SectorProfileSO : ScriptableObject
    {
        [Header("Sector Identity")]
        public int sectorNumber = 1;
        public string sectorName = "THE SILENT ABYSS";
        public float sectorDistanceMeters = 110f;

        [Header("Visual Atmosphere")]
        public Color voidBackgroundColor = new Color(0.02f, 0.027f, 0.051f, 1f);
        public Color sectorAccentColor = new Color(0.0f, 0.94f, 1.0f, 1f);
        public Color sectorSecondaryColor = new Color(0.71f, 0.22f, 0.95f, 1f);

        [Header("Pacing & Difficulty Scaling")]
        [Range(0.8f, 1.6f)] public float runSpeedMultiplier = 1.0f;
        [Range(0.7f, 1.8f)] public float batteryDrainMultiplier = 1.0f;
        [Range(0f, 1f)] public float hazardFrequency = 0.25f;
        [Range(0f, 1f)] public float prismRelayFrequency = 0.20f;
        [Range(0f, 1f)] public float orbDensity = 0.75f;
    }
}
