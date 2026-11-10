using UnityEngine;

namespace LightWeaver.Core
{
    /// <summary>
    /// Faz durumlarını tanımlar: Hayalet (Görünmez), Katılaşmış (Kristal) ve Akkor Soğuma (Afterglow Coyote Time).
    /// </summary>
    public enum HardlightPhase : byte
    {
        Phantom = 0,
        Solid = 1,
        Afterglow = 2
    }

    /// <summary>
    /// Fenerin ışık konisi (LightConeCaster2D) veya kırıcı prizmalar (PrismRelay2D) tarafından
    /// aydınlatılan tüm dünya nesnelerinin (Kristal Platform, Sıçrama Halkası, Enerji Hücresi, Gölge Engeli)
    /// uyguladığı sıfır tahsisli (Zero-GC) arayüz.
    /// </summary>
    public interface ILightReactive
    {
        /// <summary>
        /// Nesnenin dünya üzerindeki merkez referans noktası.
        /// </summary>
        Vector2 WorldPosition { get; }

        /// <summary>
        /// Nesnenin şu anki ışık fazı.
        /// </summary>
        HardlightPhase CurrentPhase { get; }

        /// <summary>
        /// Fenerin ışık konisi bu nesneye değdiği her karede çağrılır.
        /// </summary>
        /// <param name="beamOrigin">Işığın çıktığı fener noktası.</param>
        /// <param name="normalizedIntensity">0..1 arası ışık yoğunluğu (merkeze yakınlık ve pil gücüne göre).</param>
        /// <param name="isFocusedBeam">Odaklanmış lazer modunda mı?</param>
        /// <param name="deltaTime">Kare süresi.</param>
        void OnLightExposure(Vector2 beamOrigin, float normalizedIntensity, bool isFocusedBeam, float deltaTime);
    }
}
