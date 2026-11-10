# 🔦 LIGHT WEAVER: Prism Runner — Master Game Design Document (GDD v2.1)

> **Proje Kodu:** `LIGHT_WEAVER`  
> **Sürüm:** `v2.1.0-360-mobile-ready` (360° Tam Fener + Mobil Yatay Çift-Başparmak Ergonomisi + VFX Cila)  
> **Hedef Motor:** Unity 6 (`6000.3.23f1`, Universal Render Pipeline - URP 2D)  
> **Hedef Platformlar:** Mobil (Android & iOS Landscape — 60/120 FPS Zero-GC) + PC  
> **Geliştirme Metodolojisi:** 6 Evrensel DDA Sütunu, Çoklu-Dokunma Ayrımı (`EnhancedTouch`), Sıfır Tahsis (Zero-GC)  

---

## 📌 1. VİZYON DEĞİŞİMİ & OYUN KİMLİĞİ (Neden v2.0 / v2.1?)

İlk prototip yalnızca "feneri aşağıda tutup dümdüz koşma" ve kısıtlı açıya kilitlenme riskini taşıyordu. **GDD v2.0 & v2.1**, oyuncunun fenerini **tam 360 derece (-180° .. +180°) özgürce** yukarı, aşağı, ileri ve arkaya çevirebildiği; pasif izlemeyi **aktif refleks, bulmaca ve aksiyona** dönüştüren derin sistemik mimariyi ve telefon ergonomisini tanımlar.

| Alan | Detay |
| :--- | :--- |
| **Oyun Adı** | **LIGHT WEAVER: Prism Runner** |
| **Tür (Genre)** | 2D Sistemik Işık Platformeri & Taktiksel Sonsuz Koşucu (*Active Hardlight Action-Runner*) |
| **Temel Kanca (The Rule-Breaker Hook)** | **Dünya Zifiri Karanlıktır — Ama Her Işık İyi Değildir!**<br>• Siyan kristaller ışığınla katılaşır; ancak **Ters Faz (*Umbra*) Köprüleri** ışığın değdiğinde **erir** (üzerinden geçerken ışığı 360° döndürüp yukarıya/arkaya kaçırman gerekir!).<br>• Önünü kesen **Karanlık Kilit Kapılarını** açmak için ışığını kapının tepesindeki elmasa odaklamalısın (`LineRenderer` enerji halatıyla bağlı).<br>• Zemini olmayan devasa uçurumlardan geçmek için havadaki **Foton Çapalarına (*Photon Anchor*)** ışık tutup sallanmalısın! |
| **Kontrol Şeması (360° PC & Mobil)** | **PC:** `Fare` (Tam 360° imleç takibi) · `W/A/S/D` veya `Ok Tuşları` (8-Yön 360° nişan) · `Space` (Zıpla / Çift Zıpla) · `Sağ Tık` / `Shift` (Odak Lazeri) · `F`/`E` (Flash-Burst)<br>**Mobil Yatay (Landscape Ergonomisi):** `EnhancedTouch` Çoklu-Dokunma ayrımıyla:<br>• **Sol Başparmak Kontrol Grubu:** Sol alt **`JUMP`** (Zıplama / Çift Zıplama) ve hemen yanında fener+uzun ışın simgeli **`LONG: OFF/ON` (Uzun Far / High-Beam Toggle)** butonu (`12.39m <-> 18.88m` menzil değişimi) + üstünde `BURST [F]`.<br>• **Sağ Başparmak (2 Tercihli 360° Nişan Modu):**<br>  — **Mod 0 (`AIM: 360 STICK` — Varsayılan):** Sağ alanda dokunulan noktada açılan şeffaf **360° Sanal Joystick** (`FloatingAimStickBase/Knob`). Parmağın hedefi ve kristalleri kapatmaz; merkeze göre kaydırma yönü anlık 360° fener açısını belirler!<br>  — **Mod 1 (`AIM: TOUCH POINT`):** Dokunulan dünya noktasına doğrudan 360° ışık yöneltme.<br>• **Hızlı Aksiyon:** Çift dokunuş veya sol üstteki `BURST [F]` ile Flash-Burst patlaması (Menüdeki `AimModeToggleBtn` ile mod tercihi kalıcı kaydedilir). |
| **Baskı Mekaniği** | **The Eclipse (Hiçlik Dalgası):** Ekranın solundan kovalayan canlı mor-kızıl karanlık dalgası. Yavaşlarsan seni yutar; Hız Kristalleri ve üst rotalarla onu geri püskürtürsün. |
| **Roguelite Derinliği** | Her sektör geçişinde zaman yavaşlar ve oyuncu 3 rastgele **Prizma Yükseltmesi (*Prism Blessing*)** kartından birini seçer! |

---

## 🏛️ 2. DERİN OYUN TASARIMI MİMARİSİ (6 EVRENSEL DDA SÜTUNU — v2.0)

### 2.1. Kural Bozan Sistemik Anomali: "4 Fazlı Kristal & Ters Işık Yasası"
Sadece tek tip katılaşan zemin yerine, her biri farklı taktik isteyen **4 Kristal Türü** bulunur:
1. **Standart Siyan Kristal (`Standard`):** Fener ışığında anında (`0.008s`) katılaşır; ışık çekildiğinde `0.40s` *Afterglow* ardından tamamen yok olur.
2. **Hız Akımı Kristali (`KineticBoost` — Turkuaz/Altın Şeritli):** Işıkla katılaşıp üzerine basıldığında koşucuya **%45 hız fırlatması** ve kısa süreli ileri momentum sağlar; arkadan gelen *The Eclipse* dalgasından uzaklaşmanın anahtarıdır.
3. **Kırılgan Buz Kristali (`FragileShard` — Buz Beyazı):** Fenerle katılaşır ancak koşucunun ayağı değdikten **`0.24` saniye** sonra çatlayıp parçalanır — oyuncuyu anında zıplamaya veya sonraki basamağa akmaya zorlar!
4. **Ters Faz Gölge Köprüsü (`UmbraBridge` — Koyu Mor/Kızıl Şeritli):** **KURAL BOZUCU!** Varsayılan olarak karanlıkta **KATIDIR** (hafif parlayan tehlike sınırıyla sezilir). Fener ışığın üzerine değerse **0.08 saniyede eriyip yok olur!** Oyuncu bu köprüden geçerken fenerini **bilinçli olarak yukarıya veya arkaya çevirip** karanlıkta koşmak zorundadır!

### 2.2. Çapraz Kilit & Sistemik Sinerji: "Tek Açı Ölümcüldür"
Feneri sürekli `-12°` aşağı tutan oyuncu **maksimum 8 saniye** hayatta kalabilir çünkü:
* **Prism Lock Gates (Elmas Kilitli Bariyerler):** Yolu kesen geçilmez lazer kapıları. Kapının üstündeki *Elmas Kilit* düğmesine fenerini `0.20s` (Odak Lazeri ile `0.08s`) doğrultmadan kapı açılmaz; çarparsan ağır pil hasarı alıp geriye savrulursun!
* **Photon Anchors (Foton Çapa Kancaları):** Zemin bulunmayan 9–12 metrelik dev uçurumların yukarısında asılı parlak halkalar. Işığı yukarı kaldırıp bu düğüme kilitlediğinde fenerin bir **ışık halatına (Tether)** dönüşür ve seni yukarı-ileri dev bir parabolle fırlatır!
* **Void Leech (Işık Emen Kara Küre):** Üzerine ışık tutulursa pilini (`-11%/sn`) emer. Altındaki yolu yakarken tepedeki leech'i karanlıkta bırakmalı veya *Flash-Burst* ile patlatmalısın.
* **Umbra Stalker (Gölge Avcısı):** Yalnızca fener ışığı üzerine tutulduğunda `0.22s`'de eriyerek `+3.5%` enerjiye dönüşür.

### 2.3. Aktif Oyuncu Hareketi: *Photon Jump* & Çift Zıplama
* Koşucu artık sadece pasif bir hedef değildir:
  * **Zıplama (`Space` / `JUMP`):** `0.12s` Coyote Time + `0.12s` Jump Buffer ile ultra-keskin platformer zıplaması.
  * **Işık Çift Zıplaması (`Double-Jump`):** Havadayken ikinci kez basıldığında ayaklar altından siyan bir foton halkası patlar (`%3.5` pil maliyeti), oyuncuyu yükseklere çıkarır veya ölümcül boşluklardan kurtarır!

### 2.4. Baskı Mekaniği: *The Eclipse* (Hiçlik Dalgası)
* Ekranın sol tarafından yaklaşan nabızlı karanlık fırtına duvarı.
* Oyuncu normal hızda koşarken dalga sabit mesafede takiptedir; *KineticBoost* kristallerine basmak, *Perfect Weave* yapmak ve zıplama halkalarını yakalamak dalgayı geriye iter (`Pushback`).
* Kapılara çarpmak, gölge engellerine takılmak veya yavaşlamak dalganın ekranın soluna yaklaşmasına yol açar; dalga seni yutarsa koşu biter!

### 2.5. Ontolojik Faz Evrimi & Sektör Prizma Güçlendirmeleri (*Prism Blessings*)
* **Pil Spektrum Fazları:**
  * `Normal (%20-%95)`: Dengeli siyan huzme.
  * `Overcharge (>%95 veya 6x Perfect Weave)`: Gökkuşağı kırılması, +%32 geniş huzme, **3x Skor**.
  * `CriticalEmber (<%20)`: Kızıl/amber titreme, **+%15 Adrenalin hızı**, **2x enerji toplama**.
* **Sektör Arası 3'lü Kart Seçimi (*Roguelite Blessing*):**
  Her yeni sektöre geçerken zaman duraklar ve oyuncu 3 rastgele karttan 1'ini seçer:
  1. **Geniş Mercek (*Prism Lens*):** Fener koni açısı kalıcı `+%25` genişler.
  2. **Solar Kondansatör (*Capacitor*):** Alınan tüm `+ENERGY` küreleri `+%45` daha fazla pil verir.
  3. **Gölge Avcısı (*Shadow Bane*):** `UmbraStalker` erime süresi yarıya iner ve öldürünce `+6%` pil verir.
  4. **Tüy Hafifliği (*Photon Feather*):** Zıplama gücü `+%15` artar, Çift Zıplama pil harcamaz.
  5. **Huzme Verimliliği (*Eco Core*):** Pil tüketim hızı `%22` azalır.
  6. **Şok Dalga Ustası (*Burst Dynamo*):** *Flash-Burst* dolum süresi yarıya iner (`2.0s`) ve menzili `+%35` büyür.

### 2.6. Yüksek Risk / Yüksek Ödül & Solar Revive
* **Perfect Weave:** Koşucu platforma değmeden son anda aydınlatılan her kristal için **`+%3` pil** + skor + `0.03s` Hit-Stop + oktav çan akoru.
* **Solar Revive:** Her koşuda 1 kez düşüş sonrası anında güvenli bağlantılı kurtarma köprüsü kurularak `%100` pille devam etme.

---

## 🌌 3. SEKTÖRLER & 8 RİTMİK BÖLÜM ÖRÜNTÜSÜ

### 5 Sektör Profili
1. **Level 1 - THE SILENT ABYSS:** Siyan kristaller, ilk Hız Kristalleri, basit zıplamalar, *UmbraStalker* girişi.
2. **Level 2 - THE PRISM CAVERNS:** *Prism Relay* kırıcıları, **Prism Lock Gates (Kilitli Kapılar)** ve ilk **Umbra Bridge (Ters Köprü)**!
3. **Level 3 - THE UMBRA SWARM:** **Photon Anchor** uçurum sallanmaları, *Void Leech* kümeleri, *Fragile Shard* kırılan buz basamakları.
4. **Level 4 - THE ECHO VOID:** Hızlı tempo, çift katmanlı *JumpPad + Tether* akrobatik kombinasyonları, kombine kapı+ters köprü sınavları.
5. **Level 5 - THE SINGULARITY CORE:** Yoğun karanlık baskısı (*The Eclipse*), hızlı peş peşe kilit kapıları ve kırılgan kristal koşuları!

### 8 Balistik Doğrulanmış Chunk Örüntüsü (`EndlessChunkDirector`)
1. `FlatPulse`: Standart + `+ENERGY` yayları ve zıplama boşlukları.
2. `ClimbRamp`: `30°` eğimli rampa + Hız Kristali zirve çıkışı.
3. `JumpCanopyFork`: Sıçrama Halkalı üst *Starlight Canopy* + alt *Abyss Edge*.
4. `PrismGateBreach`: Yolu kesen **Prism Lock Gate**! Tepe elmasını fenerle aydınlatıp kapıyı açarak geç.
5. `UmbraInversion`: **Ters Faz Gölge Köprüsü!** Geçerken ışığı yukarı/başka yöne çekip karanlık bırakmalısın, ardından normal siyan kristale geri dön!
6. `PhotonAnchorAbyss`: Zemini olmayan 11m'lik dev uçurum! Yukarıdaki **Photon Anchor**'a ışığı tutup kancayla fırlayarak geç.
7. `FragileSprint`: Peş peşe dizili **Kırılgan Buz Kristalleri** üzerinde hızlı zıplamalar.
8. `PrismRelayHazard`: *Prism Relay* + *Moving Shadow Pillar* / *Void Leech* taktik geçişi.

---

## 🎨 4. SANAT, UI/UX VE SES BİBLOSU

* **Renk Paleti (60-30-10):** Boşluk `#05070D` (%60) · Siyan/Eflatun Kristaller `#00F0FF` / `#B537F2` (%30) · Altın Enerji / Turuncu Sıçrama / Kırmızı Tehlike `#FFD000` / `#FF6B00` / `#FF1E56` (%10).
* **HUD:** Sol üstte gradyan pil barı (`BATTERY: XX%`) + Flash-Burst dolum çubuğu; sağ üstte `LEVEL X - SECTOR` + virgüllü skor sayacı (`31,500`) + *Eclipse* tehlike mesafe çubuğu; alt köşelerde ergonomik mobil dokunmatik `JUMP` ve `FOCUS / BURST` kontrolleri.
* **ASMR Ses:** Do Minör Pentatonik (`C4–G5`) prosedürel cam arp/çan notaları, kilit kapısı kırılma rezonansı, Photon Tether ışık çekiş sesi, hız kristali armonisi ve dinamik space drone.
