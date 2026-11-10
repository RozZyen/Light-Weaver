# 📋 LIGHT WEAVER: Prism Runner — Geliştirme Takip ve İlerleme Dökümanı (Project Tracker)

> 🔒 **KATI GÜVENLİK VE API KURALI (STRICT SECURITY RULE):**
> Projede kullanılacak hiçbir API anahtarı, secret, token veya keystore bilgisi Git commitlerinde veya versiyon kontrolüne açık dosyalarda **ASLA YER ALMAYACAKTIR**.

---

## 📊 Genel İlerleme Durumu

- **Aktif Faz:** Faz 0 — Mimari Tasarım, GDD & GitHub Kurulumu Tamamlandı (Unity 6 Projesi & MCP Bağlantısı Bekleniyor) 🔦💎
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
| **F1.1** | **`ILightReactive` & `ObjectPooler`** | Zero-GC nesne havuzu ve ışığa duyarlı varlık arayüzü | ⬜ Planlandı |
| **F1.2** | **`LightConeCaster2D` (Fener Konisi)** | Prosedürel ışık huzmesi mesh'i + `OverlapCircleNonAlloc` açısal tarama | ⬜ Planlandı |
| **F1.3** | **`CrystalPlatform` Durum Makinesi** | Phantom (%8 iz) $\rightarrow$ Solid (Neon Kristal) $\rightarrow$ 0.45s Afterglow $\rightarrow$ Çözülme | ⬜ Planlandı |
| **F1.4** | **Prosedürel Neon-Kristal Sanat Üreticisi** | Fasetli kristal platformlar, elmas parlamaları ve fener huzmesi dokuları | ⬜ Planlandı |

### 🏃‍♂️ Faz 2: Karakter Fiziği, Dikey Tırmanış & Kontroller
| Görev Kodu | Görev / Özellik | Detay | Durum |
| :---: | :--- | :--- | :---: |
| **F2.1** | **`RunnerMotor2D` (Otomatik Koşu & Rampa)** | Akıcı yatay koşu, 30°-45° kristal rampa tırmanışı, Coyote Time | ⬜ Planlandı |
| **F2.2** | **`LightTouchInput` & `FlashlightController2D`** | Mobil tek parmak yay sürükleme (-60°/+65°), PC fare uyumu, Odak Lazeri | ⬜ Planlandı |
| **F2.3** | **Light Weaver Karakter Görseli & Animasyonu** | Neon biyo-zırhlı koşucu, fener kolu pivotu, `RunnerProceduralAnimator` & Trail | ⬜ Planlandı |

### ⚡ Faz 3: Pil Ekonomisi, Enerji Hücreleri & Prizmatik Mekanikler
| Görev Kodu | Görev / Özellik | Detay | Durum |
| :---: | :--- | :--- | :---: |
| **F3.1** | **`BatterySystem` (Pil & Spektrum Fazları)** | Zamanla azalan pil, Kritik Kor (<%20), Overcharge (>%95) ve *Perfect Weave* | ⬜ Planlandı |
| **F3.2** | **`EnergyOrb` (+ENERGY Batarya Hücreleri)** | Altıgen mavi çerçeveli altın yıldırım hücreleri, ışıkla şarj ve manyetik çekim | ⬜ Planlandı |
| **F3.3** | **`PrismaticJumpPad` & `PrismRelay2D`** | Işıkla aktifleşen dairesel sıçrama halkaları ve ışığı bölen elmas prizmalar | ⬜ Planlandı |

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

### [2026-11-10] — v0.1.0: Proje Başlangıcı, Master GDD & GitHub Kurulumu
- **GDD (`Docs/GDD.md`):** Referans konsept görseli ve oyun özeti temel alınarak 6 DDA sütunlu (Hardlight Crystallization, Gaze Dilemma, Tri-Layer Altitude, Pentatonic ASMR, Spectrum Shift, Just-in-Time Weave) kapsamlı tasarım dokümanı oluşturuldu.
- **Mimari & Takip:** `PROJECT_MEMORY.md`, `Docs/PROJECT_TRACKER.md`, `docs/plans/task.md`, `.gitignore` ve `.gitattributes` yapılandırıldı.
- **GitHub & Taşıma:** Proje `C:\Users\Yusuf\Documents\GitHub\Light-Weaver` dizinine taşındı ve `https://github.com/RozZyen/Light-Weaver` uzak deposuna bağlandı.
