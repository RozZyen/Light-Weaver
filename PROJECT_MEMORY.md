# 🧠 LIGHT WEAVER: Prism Runner - Canlı Proje Hafızası (Cross-Device Memory)

> **Son Güncelleme:** 2026-11-10  
> **Durum:** Faz 0 — GDD, Mimari Altyapı & GitHub Kurulumu Tamamlandı (Unity 6 & MCP Bekleniyor)  
> **Cihaz Eşitleme:** Git Push/Pull (`main` dalı) ile Masaüstü & Laptop senkronizasyonu aktif.  

---

### 📌 1. Temel Proje Kimliği & Hedef
- **Proje:** LIGHT WEAVER: Prism Runner
- **Motor / Teknoloji:** Unity 6 (`6000.x`, URP 2D)
- **Hedef Platform:** Mobil (Android & iOS — 60/120 FPS Zero-GC) + PC Editör Kontrolleri
- **Tür & Mekanik:** 2D Endless Light-Runner / Hardlight Platformer (Fener ışığıyla boşluğu kristalleştirme)
- **Aktif Çalışma Dizini:** `C:/Users/Yusuf/Documents/GitHub/Light-Weaver`
- **Tasarım & Takip Referansları:** `Docs/GDD.md`, `Docs/PROJECT_TRACKER.md`, `docs/plans/task.md`

---

### 🗺️ 2. Mimari Bilgi Haritası (Architecture Graph)
<!-- Token koruyucu: Kod yazmadan önce bu şema incelenir; var olan sınıflar kullanılır, mükerrer sistem üretilmez. -->
```mermaid
graph TD
    subgraph Input_UI ["📱 Girdi & Arayüz (UI)"]
        Touch["LightTouchInput (Tek Parmak Yay & Çift Dokunuş)"] --> Aim["FlashlightController2D"]
        HUD["LightWeaverHUD (BatteryBar, ScoreCounter, SectorTitle)"]
        Menus["MainMenuUI / PauseModalUI / GameOverReviveUI"]
    end

    subgraph Player_System ["🏃‍♂️ Light Weaver Karakteri"]
        Aim --> Cone["LightConeCaster2D (Zero-GC NonAlloc Tarama & Beam Mesh)"]
        Motor["RunnerMotor2D (Otomatik Koşu, Rampa Tırmanış, Coyote Time)"]
        Battery["BatterySystem (Tüketim, Şarj, Overcharge & Kritik Fazlar)"]
        Anim["RunnerProceduralAnimator (Squash & Stretch, Hız İzi)"]
    end

    subgraph World_Prisms ["💎 Kristal Dünya & Havuz"]
        Director["EndlessChunkDirector (5 Sektör & Sonsuz Örüntü)"] --> Pool["ObjectPooler (IPoolable)"]
        Pool --> Platform["CrystalPlatform : ILightReactive (Phantom -> Solid -> Afterglow)"]
        Pool --> JumpPad["PrismaticJumpPad : ILightReactive"]
        Pool --> Orb["EnergyOrb : ILightReactive (+ENERGY Şarj Hücresi)"]
        Pool --> Prism["PrismRelay2D : ILightReactive (Işık Kırıcı)"]
        Pool --> Hazard["ShadowHazard : ILightReactive (UmbraStalker / MovingPillar)"]
    end

    subgraph Juice_Services ["✨ ASMR Juice, Ses & Servisler"]
        Audio["HarmonicAudioManager (Pentatonik Cam/Çan ASMR + Müzik)"]
        Juice["JuiceFeedbackManager (HitStop 0.04s, CameraShake2D, Haptics)"]
        Progress["ProgressManager (Seviye, Skor, PlayerPrefs)"]
        Ads["AdManager (Rewarded Solar Revive / 2x Score)"]
    end

    Cone ==>|Işık Konisi Taraması| Platform
    Cone ==>|Işık Konisi Taraması| JumpPad
    Cone ==>|Odak & Manyetik Çekim| Orb
    Cone ==>|Erime / Savuşturma| Hazard
    Orb -.->|+Şarj| Battery
    Platform -.->|Fiziksel Zemin| Motor
    JumpPad -.->|Parabolik İtki| Motor
    Battery -.->|Telemetri| HUD
    Platform -.->|Pentatonik Tını| Audio
```

---

### 🎨 3. Sanat Yönetimi & Tasarım Dili
- **Görsel Tarz:** High-Contrast Neon Prismatic / Hardlight Sci-Fi 2D
- **Renk Paleti (60-30-10 Kuralı):**
  - Dominant Boşluk (%60): `#05070D` (Zifiri Boşluk) -> `#0B1021` (Derin İndigo)
  - İkincil Kristal Yapı (%30): `#00F0FF` (Neon Siyan), `#B537F2` (Prizmatik Eflatun), `#1A8CFF` (Kristal Mavi)
  - Vurgu / Enerji / Tehlike (%10): `#FFD000` (Fener & Batarya Altını), `#FF6B00` (Sıçrama Halkası Turuncusu), `#FF1E56` (Void Tehlike Kızılı)
- **Juice & ASMR Parametreleri:**
  - Pentatonik cam/arp ses gamı (`C4 - Eb5`), Pitch modülasyonu (`0.95 - 1.35`)
  - *Perfect Weave* mikro Hit-Stop (`0.04s`), Camera Shake (`CameraShake2D`), Squash & Stretch

---

### 🔒 4. Asla Çiğnenmeyecek Kurallar & Kırmızı Çizgiler
- [x] **Kullanıcı Kırmızı Çizgisi:** Keystore / key alias asla otomatik üretilmeyecek; API anahtarları veya `.env` dosyaları asla Git'e commitlenmeyecek.
- [x] **Motor & Performans Kuralı:** `Update()` / `FixedUpdate()` içinde `new`, `GetComponent<T>()`, `FindObjectsOfType`, `Camera.main`, LINQ ve string birleştirme kesinlikle yasaktır (Zero-GC).
- [x] **Unity MCP & Sahne Kuralı:** Sahne ve UI yerleşimleri Editor kurulum araçlarıyla (`LightWeaverSetupUtility`) kalıcı olarak inşa edilir.

---

### ✅ 5. Tamamlanan Son Aşamalar
- [x] **Faz 0.1:** Kapsamlı Master GDD (`Docs/GDD.md`) 6 DDA sütunu ve referans görsel analizine göre hazırlandı.
- [x] **Faz 0.2:** `PROJECT_MEMORY.md`, `Docs/PROJECT_TRACKER.md`, `docs/plans/task.md`, `.gitignore` ve `.gitattributes` oluşturuldu.
- [x] **Faz 0.3:** Proje `C:\Users\Yusuf\Documents\GitHub\Light-Weaver` klasörüne taşındı ve GitHub (`RozZyen/Light-Weaver`) uzak deposuna bağlandı.

---

### 🎯 6. Sırada Ne Var? (Next Sprint)
- [ ] **Faz 0.4:** Kullanıcının Unity 6 projesini oluşturmasının ardından `Packages/manifest.json` içine `com.coplaydev.unity-mcp` paketinin eklenmesi ve MCP bağlantısının doğrulanması.
- [ ] **Faz 1.1:** `ILightReactive`, `ObjectPooler` ve Zero-GC `LightConeCaster2D` (fener konisi + açısal tarama) sistemlerinin yazılması.
- [ ] **Faz 1.2:** `CrystalPlatform` (Phantom $\rightarrow$ Solid $\rightarrow$ Afterglow) ve prosedürel neon-kristal görsel üreticisinin kurulması.
