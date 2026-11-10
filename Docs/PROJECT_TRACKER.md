# 📋 LIGHT WEAVER: Prism Runner — Geliştirme Takip ve İlerleme Dökümanı (Project Tracker)

> 🔒 **KATI GÜVENLİK VE API KURALI (STRICT SECURITY RULE):**
> Projede kullanılacak hiçbir API anahtarı, secret, token veya keystore bilgisi Git commitlerinde veya versiyon kontrolüne açık dosyalarda **ASLA YER ALMAYACAKTIR**.

---

## 📊 Genel İlerleme Durumu

- **Aktif Faz:** Faz 3 Tamamlandı — Pil Ekonomisi, `+ENERGY` Hücreleri, Sıçrama Halkaları & Prizma Kırıcıları Aktif (Faz 4 Bekleniyor) 🔋⚡💎
- **Hedef Motor:** Unity 6 (URP 2D) + Unity MCP (`com.coplaydev.unity-mcp`)
- **Depo Konumu:** `C:\Users\Yusuf\Documents\GitHub\Light-Weaver` (`https://github.com/RozZyen/Light-Weaver`)
- **Son Güncelleme:** 2026-11-10

---

## 🗂️ Aşamalı Yol Haritası & Görev Takip Tablosu

### 🏗️ Faz 0: Altyapı, GDD, GitHub & Unity MCP Kurulumu
| Görev Kodu | Görev / Özellik | Detay | Durum |
| :---: | :--- | :--- | :---: |
| **F0.1** | **Master GDD Hazırlığı (`Docs/GDD.md`)** | 6 DDA Sütunu, Bakış Açısı İkilemi, 5 Sektör, Sanat & ASMR Ses Biblosu | ✅ Tamamlandı |
| **F0.2** | **Proje Hafızası & Git Kalkanı** | `PROJECT_MEMORY.md`, `.gitignore`, `.gitattributes`, `README.md` | ✅ Tamamlandı |
| **F0.3** | **GitHub Bağlantısı & Taşıma** | `Documents\GitHub\Light-Weaver` dizinine aktarım ve `RozZyen/Light-Weaver` push | ✅ Tamamlandı |
| **F0.4** | **Unity 6 & Unity MCP Entegrasyonu** | `Packages/manifest.json` içine `com.coplaydev.unity-mcp` eklenmesi ve köprü testi | ✅ Tamamlandı |

### 🔦 Faz 1: Çekirdek Işık & Kristalleşme Motoru (Core Hardlight Engine)
| Görev Kodu | Görev / Özellik | Detay | Durum |
| :---: | :--- | :--- | :---: |
| **F1.1** | **`ILightReactive` & `ObjectPooler`** | Zero-GC nesne havuzu ve `LightReactiveRegistry` merkezi kayıt sistemi | ✅ Tamamlandı |
| **F1.2** | **`LightConeCaster2D` (Fener Konisi)** | Prosedürel ışık huzmesi mesh'i + açısal tarama + Odak Lazeri (Focus Beam) | ✅ Tamamlandı |
| **F1.3** | **`CrystalPlatform` Durum Makinesi** | Phantom (%9 iz) $\rightarrow$ Solid (Neon Kristal) $\rightarrow$ 0.48s Afterglow $\rightarrow$ Çözülme | ✅ Tamamlandı |
| **F1.4** | **Prosedürel Neon-Kristal Sanat Üreticisi** | Fasetli kristal platformlar, elmas parlamaları ve `PF_CrystalPlatform.prefab` | ✅ Tamamlandı |

### 🏃‍♂️ Faz 2: Karakter Fiziği, Dikey Tırmanış & Kontroller
| Görev Kodu | Görev / Özellik | Detay | Durum |
| :---: | :--- | :--- | :---: |
| **F2.1** | **`RunnerMotor2D` (Otomatik Koşu & Rampa)** | Zero-GC rampa teğet tırmanışı (`30°-45°`), Auto Step-Up ve Coyote Time | ✅ Tamamlandı |
| **F2.2** | **`LightTouchInput` & `FlashlightController2D`** | Mobil yay sürükleme, çakışmasız `W`/`S` klavye, hareketli/sürüklemeli fare & Odak Lazeri | ✅ Tamamlandı |
| **F2.3** | **Light Weaver Karakter Görseli & Kamera** | Neon vizörlü parçalı koşucu, `RunnerProceduralAnimator`, `TrailRenderer` & `CameraFollow2D` | ✅ Tamamlandı |

### ⚡ Faz 3: Pil Ekonomisi, Enerji Hücreleri & Prizmatik Mekanikler
| Görev Kodu | Görev / Özellik | Detay | Durum |
| :---: | :--- | :--- | :---: |
| **F3.1** | **`BatterySystem` (Pil & Spektrum Fazları)** | Zaman/mesafe tüketimi, Kritik Kor (<%20), Overcharge (>%95) ve *Perfect Weave* | ✅ Tamamlandı |
| **F3.2** | **`EnergyOrb` (+ENERGY Batarya Hücreleri)** | Altıgen mavi çerçeveli altın yıldırım hücreleri, ışıkla şarj ve manyetik çekim | ✅ Tamamlandı |
| **F3.3** | **`PrismaticJumpPad` & `PrismRelay2D`** | Işıkla aktifleşen dairesel sıçrama halkaları ve ışığı bölen elmas prizmalar | ✅ Tamamlandı |

### 🌌 Faz 4: Bölüm Üretici (Level 1-5 & Sonsuz Boşluk) + Gölge Tehlikeleri
| Görev Kodu | Görev / Özellik | Detay | Durum |
| :---: | :--- | :--- | :---: |
| **F4.1** | **`EndlessChunkDirector` (3 Katmanlı Akış)** | Alt/Orta/Üst irtifa koridorları, 5 Sektör (*Level 4 - The Echo Void* dahil) | ⬜ Planlandı |
| **F4.2** | **`ShadowHazard` (Gölge Engelleri)** | Hareketli gölge sütunları, ışıktan eriyen *Umbra Stalker*, ışık emen küreler | ⬜ Planlandı |

### 🎨 Faz 5: HUD, Menüler & Pentatonik ASMR Ses Motoru
| Görev Kodu | Görev / Özellik | Detay | Durum |
| :---: | :--- | :--- | :---: |
| **F5.1** | **`LightWeaverHUD` (Referans Birebir HUD)** | Sol üst gradyan pil barı (`BATTERY: 74%`), Sağ üst `LEVEL 4 - THE ECHO VOID` & Skor | ⬜ Planlandı |
| **F5.2** | **Menü Akışı & Checkpoint/Revive** | Ana Menü, Seviye Seçimi, Duraklatma (ESC/Pause) ve Acil Solar Şarj (Revive) | ⬜ Planlandı |
| **F5.3** | **`HarmonicAudioManager` & Juice** | Do Minör Pentatonik kristal cam/çan ASMR ses sentezleyicisi, Hit-Stop & Shake | ⬜ Planlandı |

### 📱 Faz 6: Mobil Mağaza Hazırlığı, Monetizasyon & Cila
| Görev Kodu | Görev / Özellik | Detay | Durum |
| :---: | :--- | :--- | :---: |
| **F6.1** | **`AdManager` & `IAPManager`** | ScriptableObject tabanlı Ödüllü Reklam ve Reklamları Kaldır altyapısı | ⬜ Planlandı |
| **F6.2** | **`SafeAreaFit` & 60/120 FPS Denetimi** | Çentikli ekran uyumu ve mobil bellek/performans doğrulaması | ⬜ Planlandı |

---

## 📜 Geliştirme Günlüğü (Changelog)

### [2026-11-10] — v0.4.0: Faz 3 Pil Ekonomisi, Enerji Hücreleri, Jump Pad & Prizmalar + Rampa Cila
- **Faz 3 Sistemleri:**
  - `BatterySystem.cs`: Mesafe/Focus Beam tüketim eğrisi, 3 Spektrum Fazı (`Normal`, `Overcharge` 3x skor + geniş prizma huzmesi, `CriticalEmber` titreme + %15 Adrenalin hızı + 2x şarj ödülü) ve *Perfect Weave* (`+%3` pil) kuruldu.
  - `EnergyOrb.cs`: Altıgen neon siyan çerçeveli altın yıldırım `+ENERGY` hücreleri (`+12%` ve `+24%` Super), ışık odaklanmasında manyetik uçuşla koşucuya çekim ve respawn yenilemesi eklendi.
  - `PrismaticJumpPad.cs` & `PrismRelay2D.cs`: Işıkla şarj olan dairesel sıçrama halkaları ve ışığı iki yöne kıran elmas prizmalar + prefab'lar üretildi.
- **Faz 2 Akış Düzeltmeleri:**
  - `RunnerMotor2D.cs`: Eğimli rampa uçlarında takılmayı önleyen `normal.x < -0.18f` basamak atlama düzeltmesi, boşluk düşüşünde anlık kamera snap (`CameraFollow2D`) ve kesintisiz örtüşen rampa dizilimi uygulandı.
  - Görsel Cila: Geliştirilmiş neon koşucu silüeti, diegetik sırt pil barı ve küçültülmüş parlak emitter çekirdeği.

### [2026-11-10] — v0.3.0: Faz 1 Düzeltmeleri + Faz 2 Karakter Fiziği, Kontroller & Oynanabilir Parkur
- **Faz 1 Düzeltmeleri:**
  - `CrystalPlatform.cs`: `Phantom` fazında `SpriteRenderer.enabled = false` yapılarak ışıksız kristallerin zifiri karanlıkta %100 görünmez olması sağlandı; 4-noktalı analitik ışık örneklemesi ve 1:1 elmas parlama oranı eklendi.
  - `LightConeCaster2D.cs`: Altın kenar ışınlı ve pürüzsüz sönümlemeli `volumetric_beam_gradient.png` UV koni mesh'ine yükseltildi.
  - `LightTouchInput.cs`: Sabit fare imlecinin `W`/`S` ve Ok tuşlarını ezmesi hatası giderildi; mobil dokunmatik, klavye ve aktif fare uyumu sağlandı.
- **Faz 2 Sistemleri (`RunnerMotor2D`, `FlashlightController2D`, `RunnerProceduralAnimator`, `CameraFollow2D`):**
  - `RunnerMotor2D`: `Player` ve `HardlightWorld` katman izolasyonuyla Zero-GC otomatik koşu, `34°-38°` kristal rampa tırmanışı ve otomatik basamak atlama kuruldu.
  - `LightWeaverSetupUtility`: Neon vizörlü koşucu prefab'ı (`PF_LightWeaverRunner.prefab`), ayak altı kıvılcım partikülleri, hız izi ve ~80m çift rotalı kristal parkuru sahneye kalıcı olarak kuruldu.

### [2026-11-10] — v0.2.0: Faz 1 Çekirdek Işık & Kristalleşme Motoru (Hardlight Engine)
- **Çekirdek Sistemler:** `ILightReactive`, `LightReactiveRegistry` ve `ObjectPooler` (`IPoolable`) sıfır GC tahsisiyle (Zero-GC) yazıldı.
- **Volümetrik Fener Konisi (`LightConeCaster2D`):** Altın çekirdekten neon siyana (`#00F0FF`) ve prizmatik eflatuna (`#B537F2`) uzanan dinamik mesh ve açısal hedef tarayıcı kuruldu. Sağ tık / ikinci parmak ile daralan Odak Lazeri (`Focus Beam`) eklendi.
- **Kristal Platform (`CrystalPlatform`):** Hayalet fazdan (`Phantom`) katı kristale (`Solid` + `BoxCollider2D` aktif) geçiş, Squash & Stretch büyüme tepkisi, köşe elmas parlamaları ve ışık çekildiğinde `0.48s` süren Akkor Soğuma (`Afterglow`) mekaniği tamamlandı.
- **Sahne & Sanat (`LightWeaverPhase1Setup`):** Prosedürel fasetli kristal dokuları (`prism_crystal_block.png`, `diamond_starburst.png`), `PF_CrystalPlatform.prefab` ve referans görseldeki kademeli kristal rampa dizilimi sahneye kalıcı olarak inşa edildi.

### [2026-11-10] — v0.1.0: Proje Başlangıcı, Master GDD & GitHub Kurulumu
- **GDD (`Docs/GDD.md`):** Referans konsept görseli ve oyun özeti temel alınarak 6 DDA sütunlu (Hardlight Crystallization, Gaze Dilemma, Tri-Layer Altitude, Pentatonic ASMR, Spectrum Shift, Just-in-Time Weave) kapsamlı tasarım dokümanı oluşturuldu.
- **Mimari & Takip:** `PROJECT_MEMORY.md`, `Docs/PROJECT_TRACKER.md`, `docs/plans/task.md`, `.gitignore` ve `.gitattributes` yapılandırıldı.
- **GitHub & Taşıma:** Proje `C:\Users\Yusuf\Documents\GitHub\Light-Weaver` dizinine taşındı ve `https://github.com/RozZyen/Light-Weaver` uzak deposuna bağlandı.
