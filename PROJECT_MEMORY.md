# 🧠 LIGHT WEAVER: Prism Runner - Canlı Proje Hafızası (Cross-Device Memory)

> **Son Güncelleme:** 2026-11-10  
> **Durum:** Faz 4 Tamamlandı — Sonsuz Bölüm Üretici (`EndlessChunkDirector`), 5 Sektör Profili, 3 Gölge Tehlikesi (`ShadowHazard`) & `FlashBurst` Aktif (Faz 5'e Hazır)  
> **Cihaz Eşitleme:** Git Push/Pull (`main` dalı) ile Masaüstü & Laptop senkronizasyonu aktif.  

---

### 📌 1. Temel Proje Kimliği & Hedef
- **Proje:** LIGHT WEAVER: Prism Runner
- **Motor / Teknoloji:** Unity 6 (`6000.3.23f1`, URP 2D)
- **Hedef Platform:** Mobil (Android & iOS — 60/120 FPS Zero-GC) + PC Editör Kontrolleri
- **Tür & Mekanik:** 2D Endless Light-Runner / Hardlight Platformer (Fener ışığıyla boşluğu kristalleştirme)
- **Aktif Çalışma Dizini:** `C:/Users/Yusuf/Documents/GitHub/Light-Weaver` (Unity projesi: `LIGHT WEAVER/`)
- **Tasarım & Takip Referansları:** `Docs/GDD.md`, `Docs/PROJECT_TRACKER.md`, `Docs/plans/task.md`

---

### 🗺️ 2. Mimari Bilgi Haritası (Architecture Graph)
<!-- Token koruyucu: Kod yazmadan önce bu şema incelenir; var olan sınıflar kullanılır, mükerrer sistem üretilmez. -->
```mermaid
graph TD
    subgraph Input_UI ["📱 Girdi & Arayüz (UI)"]
        Touch["LightTouchInput (Tek Parmak Yay, W/S Klavye & Hareketli Fare)"] --> Aim["FlashlightController2D"]
        HUD["LightWeaverHUD (BatteryBar, ScoreCounter, SectorTitle)"]
        Menus["MainMenuUI / PauseModalUI / GameOverReviveUI"]
    end

    subgraph Player_System ["🏃‍♂️ Light Weaver Karakteri"]
        Aim --> Cone["LightConeCaster2D (Zero-GC Tarama & Volümetrik UV Huzme)"]
        Motor["RunnerMotor2D (Otomatik Koşu, Rampa Teğet Tırmanışı, Exact Step-Up)"]
        Battery["BatterySystem (Tüketim, Overcharge/Ember Fazları, Perfect Weave, Hazard Şoku)"]
        Burst["FlashBurstController (Çift Dokunuş / F / E Şok Dalgası Parry)"]
        Anim["RunnerProceduralAnimator (Adım, Diegetik Sırt Pili, Kıvılcım, Trail)"]
        Cam["CameraFollow2D (İleri Görüş Look-Ahead, Respawn Snap & Shake)"]
    end

    subgraph World_Prisms ["💎 Kristal Dünya & Havuz"]
        Director["EndlessChunkDirector (6 Balistik Chunk, 5 Sektör SO, Geri Dönüşüm)"] --> Pool["ObjectPooler (IPoolable)"]
        Pool --> Platform["CrystalPlatform : ILightReactive (Phantom -> Solid -> Afterglow)"]
        Pool --> JumpPad["PrismaticJumpPad : ILightReactive (Işıkla Şarj Olan Sıçrama Halkası)"]
        Pool --> Orb["EnergyOrb : ILightReactive (+ENERGY Manyetik Şarj Hücresi)"]
        Pool --> Prism["PrismRelay2D : ILightReactive (Çift Huzmeli Işık Kırıcı)"]
        Pool --> Hazard["ShadowHazard : ILightReactive (MovingPillar / UmbraStalker / VoidLeech)"]
        Stars["ParallaxStarfield (Sonsuz Uzay Derinliği)"]
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
- [x] **Faz 0 & Faz 1 (Düzeltmelerle Tamamlandı):**
  - `CrystalPlatform.cs`: Işık çekildiğinde `0.40s` *Afterglow* sonrası tamamen görünmez (`SpriteRenderer.enabled = false`, `Phantom` tam zifiri karanlık), 4-noktalı analitik kenar ışık yakalama ve 1:1 elmas parlamaları.
  - `LightConeCaster2D.cs`: Altın kenar ışınlı & neon siyan çekirdekli UV gradyan volümetrik koni mesh'i (`volumetric_beam_gradient.png`).
- [x] **Faz 2 (Karakter Fiziği, Dikey Tırmanış & Kontroller):**
  - `LightTouchInput.cs` & `FlashlightController2D.cs`: Mobil yay sürükleme, sabit fare imlecinin klavyeyi ezmesini önleyen akıllı `W`/`S`/Ok tuşu ve sürükle/hareketli fare kontrolü.
  - `RunnerMotor2D.cs`: Zero-GC `RaycastNonAlloc` tabanlı otomatik koşu, `30°-45°` kristal rampa teğet tırmanışı, otomatik basamak atlama (*Auto Step-Up*), Coyote Time ve boşluk düşüşü yenilemesi.
  - `RunnerProceduralAnimator.cs` & `CameraFollow2D.cs`: Neon vizörlü parçalı koşucu, adım/süzülme animasyonu, ayak altı kıvılcımları, hız izi (`TrailRenderer`) ve look-ahead kamera takibi.
  - `LightWeaverSetupUtility.cs`: Katmanlar (`Player`, `HardlightWorld`), sıfır sürtünme materyali ve ~80m kristal rampa test parkuru kuruldu.

- [x] **Faz 3 (Pil Ekonomisi, Enerji Hücreleri & Prizmatik Mekanikler + Akış Cila):**
  - `BatterySystem.cs`: Mesafe/Odak Lazeri bazlı pil tüketimi, 3 Spektrum Fazı (`Normal`, `Overcharge >%95 / 6x Streak` geniş prizma huzmesi + 3x skor, `CriticalEmber <%20` titreme + %15 Adrenalin hızı + 2x şarj ödülü) ve *Perfect Weave* (`+%3` pil geri kazanımı).
  - `EnergyOrb.cs`: Altıgen siyan çerçeveli altın yıldırım `+ENERGY` (`+12%` / `+24%` Super) hücreleri, `0.14s` ışık odaklanmasında manyetik çekim.
  - `PrismaticJumpPad.cs`: Işıkla şarj olan dairesel neon turuncu/siyan sıçrama halkaları (*Starlight Canopy* yüksek katman çıkışı).
  - `PrismRelay2D.cs`: Fener ışığını çift ikincil huzmeye kırarak kör noktadaki platform/bataryaları aydınlatan elmas prizmalar.
  - `RunnerMotor2D.cs` & `LightWeaverSetupUtility.cs`: Eğimli rampa burunlarında pürüzsüz step-up, kamera respawn snap, diegetik sırt pil göstergesi ve kesintisiz ~70m parkur + 5 yeni prefab kaydedildi.

- [x] **Faz 4 (Sonsuz Bölüm Üretici, 5 Sektör & Gölge Tehlikeleri + Step-Up Düzeltmesi):**
  - `RunnerMotor2D.cs`: Tam yüzey probe'lu `TryAutoStepUp` (platform eklem yerlerinde takılma tamamen giderildi) + sektörel hız çarpanı (`SetSectorSpeedMultiplier`).
  - `SectorProfileSO.cs`: Level 1 (*The Silent Abyss*) – Level 5 (*The Singularity Core*, *Level 4 - The Echo Void* dahil) 5 ScriptableObject sektörü oluşturuldu.
  - `EndlessChunkDirector.cs`: `ObjectPooler` üzerinden 6 balistik-doğrulanmış ritmik chunk (3 irtifa katmanlı Jump Pad çatalı, Prizma köprü, rampa, süzülme, tehlike koridoru) üreten ve geride kalanları geri dönüştüren sonsuz yönetmen.
  - `ShadowHazard.cs` & `FlashBurstController.cs`: 3 sistemik gölge engeli (`MovingShadowPillar`, ışıktan eriyen `UmbraStalker`, ışık emen `VoidLeech`) + Çift Dokunuş/`F`/`E` ile radyal Parlama Patlaması (Flash-Burst Parry) + `ParallaxStarfield.cs`.

---

### 🎯 6. Sırada Ne Var? (Next Sprint)
- [ ] **Faz 5.1:** Referans görseldeki birebir `LightWeaverHUD` (`BATTERY: 74%` gradyan pil barı, `LEVEL 4 - THE ECHO VOID`, `31,500` skor sayacı, *Perfect Weave* / `+ENERGY` bildirimleri).
- [ ] **Faz 5.2:** Ana Menü, Sektör Seçimi, Duraklatma (ESC / Pause) ve Oyun Sonu / Solar Canlanma akışı (`ProgressManager`).
- [ ] **Faz 5.3:** `HarmonicAudioManager.cs` (Do Minör Pentatonik kristal cam/arp ASMR ses sentezleyicisi) + Hit-Stop / görsel cila.
