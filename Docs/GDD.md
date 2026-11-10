# 🔦 LIGHT WEAVER: Sands of Mystery — Master Game Design Document (GDD v3.0)

> **Proje Kodu:** `LIGHT_WEAVER`  
> **Sürüm:** `v3.0.0-egypt-detective` (Mısır Piramit Gecesi, Modüler Trençkotlu Dedektif & 210° Fener Yayı)  
> **Hedef Motor:** Unity 6 (`6000.3.23f1`, Universal Render Pipeline - URP 2D)  
> **Hedef Platformlar:** Mobil (Android & iOS Landscape — 60/120 FPS Zero-GC) + PC  
> **Geliştirme Metodolojisi:** 6 Evrensel DDA Sütunu, Çoklu-Dokunma Ayrımı (`EnhancedTouch`), Modüler 2D Kemik/Eklem Rig'i, Sıfır Tahsis (Zero-GC)  

---

## 📌 1. VİZYON DEĞİŞİMİ & OYUN KİMLİĞİ (Mısır Gizemi + Dedektif)

**GDD v3.0**, oyunu soğuk soyut uzaydan çıkarıp **Antik Mısır Gecesi, Gize Piramitleri Paralaksı ve Elinde Pirinç Feneriyle Yolunu Arayan Trençkotlu Bir Dedektif** kimliğine oturtur. Oyuncu `210°` (`-105°` .. `+105°`) taktiksel ileri+dikey fener yayıyla gereksiz arka görüş karmaşası olmadan hem ayağının tam altını hem de Photon Anchor salınımlarındaki tepe ivmesini ustaca yakalar.

| Alan | Detay |
| :--- | :--- |
| **Oyun Adı** | **LIGHT WEAVER: Sands of Mystery** |
| **Tür (Genre)** | 2D Sistemik Işık Platformeri & Taktiksel Sonsuz Koşucu (*Atmospheric Egypt Mystery Runner*) |
| **Ana Kahraman (Modüler Rig)** | **Trençkotlu Dedektif** (`#BC945F` Deve Tüyü, `#DFBE93` Açık Yaka, `#5B4039` Fedora Şapka, `#836C54` Pantolon, `#1F171A` Kontur & Bot): Ayrı Kafa/Şapka, Gövde+Kemer, Rüzgârda Dalgalanan Trençkot Etekleri, 2-Eklemli Diz/Bacak ve **2-Eklemli Modüler Fener Kolu (`UpperArm -> Forearm -> VintageFlashlight`)**. |
| **Temel Kanca (The Rule-Breaker Hook)** | **Çöl Gecesi Zifiri Karanlıktır — Ama Her Işık İyi Değildir!**<br>• Altın hiyeroglifli kumtaşları ışığınla katılaşır; ancak **Lanetli Anubis (*Umbra*) Köprüleri** fener değdiğinde **çözünüp erir** (feneri yukarı/+105°'ye kaçırmalısın!).<br>• Önünü kesen **Tapınak Lazer Kapılarını** açmak için ışığını kapının tepesindeki mühür elmasına odaklamalısın.<br>• Uçurumları aşmak için gökyüzündeki **Foton Çapalarına (*Photon Anchor*)** `210°` yayı kullanarak ışık halatıyla tutunup sallanmalısın! |
| **Kontrol Şeması (210° PC & Mobil)** | **PC:** `Fare` (`-105°..+105°` 210° imleç takibi) · `W/A/S/D` veya `Oklar` · `Space` (Zıpla / Çift Zıpla) · `Sağ Tık` / `Shift` (Uzun Fener) · `F`/`E` (Flash-Burst)<br>**Mobil Yatay (Landscape Ergonomisi):** `EnhancedTouch` Çoklu-Dokunma ayrımıyla:<br>• **Sol Başparmak Kontrol Grubu:** Sol alt **`JUMP`** ve yanında fener+uzun ışın ikonlu **`LONG: OFF/ON` (Uzun Fener Toggle)** butonu (`12.39m <-> 18.88m` menzil) + üstünde `BURST [F]`.<br>• **Sağ Başparmak (`AIM: 210 STICK` / `TOUCH POINT`):** Sağ alanda açılan şeffaf sanal joystick veya dokunma noktasıyla `-105°..+105°` kesintisiz fener kontrolü. |
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

### 5 Mısır Keşif Sektörü Profili
1. **Level 1 - OUTSKIRTS OF GIZA:** Gece çölünde kumtaşı harabeler, ilk Horus Hız Taşları, basit zıplamalar, *UmbraStalker* girişi.
2. **Level 2 - THE SANDSTONE TOMBS:** *Prism Relay* kırıcıları, **Prism Lock Gates (Tapınak Lazer Kapıları)** ve ilk **Umbra Bridge (Lanetli Köprü)**!
3. **Level 3 - CHAMBER OF ANUBIS:** **Photon Anchor** uçurum sallanmaları, *Void Leech* kümeleri, *Fragile Shard* çatlak kireçtaşları.
4. **Level 4 - HALL OF SCARABS:** Hızlı tempo, çift katmanlı *JumpPad + Tether* akrobatik kombinasyonları, kombine kapı+lanetli köprü sınavları.
5. **Level 5 - THE PHARAOH'S VAULT:** Yoğun kum fırtınası baskısı (*The Eclipse*), hızlı peş peşe kilit kapıları ve kırılgan tablet koşuları!

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

* **Renk Paleti (60-30-10 — Mısır Gecesi & Dedektif Referansı):**
  * **Gece Çölü & Piramitler (%60 Zemin):** `#080A16` (Çöl Gecesi), `#2B2540` / `#141122` (Piramit & Obelisk Paralaksı), `#20181D` (Yakın Kumullar)
  * **Dedektif Trençkot & Kumtaşı (%30 Karakter/Platform):** `#BC945F` / `#DFBE93` / `#A88453` (Trençkot beji), `#5B4039` / `#836C54` (Fedora şapka, pantolon, kemer), `#1F171A` (Dış kontur & çizme)
  * **Fener Işını, Altın & Turkuaz (%10 Odak):** `#FFD24C` (Pirinç fener kehribar-altını), `#00D8C8` (Antik turkuaz scarab keşfi), `#F53855` (Lanet & tehlike)
* **HUD:** Sol üstte gradyan pil barı (`BATTERY: XX%`) + Flash-Burst barı; sağ üstte `LEVEL X - OUTSKIRTS OF GIZA` + skor sayacı + *Eclipse* tehlike barı; sol altta **`JUMP`** + **`LONG: OFF/ON`** ikon butonları + **`BURST [F]`**, sağ alanda şeffaf **210° Floating Stick**.
* **ASMR Ses:** Do Minör Pentatonik (`C4–G5`) prosedürel cam arp/çan notaları, kilit kapısı kırılma rezonansı, Photon Tether ışık çekiş sesi, hız kristali armonisi ve dinamik space drone.
