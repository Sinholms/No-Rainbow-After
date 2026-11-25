# Di Bawah Sorotan — Arsip QA Chapter 2

Hasil pengujian prototipe Di Bawah Sorotan pada `1c02650`, dengan baseline `d703909` dan penggabungan rig first-person dari `origin/main` pada `f75bdb5`. Pengujian ini mencakup build lokal `Builds/Chapter2Playtest/NoRainbowAfter.exe`. Plan dan kriteria uji untuk revisi ada di [Chapter 2 Bizarre Edition](../../CHAPTER2_PLAN.md).

---

## 1. Environment & Keterbacaan Tujuan dari Spawn

Dari titik spawn di depan Warung Sri `(-3.20, 0.00, -39.50)` ke papan **HALTE PASAR 04** `(0.40, 2.52, -2.50)` berjarak sekitar **37,25 meter**. Dua lajur aspal basah diapit trotoar dan fasad ruko tertutup.
- **Segmen A (`z = -39.5 .. -24.0`)**: `Peti rendah A` (`(-1.80, 0.475, -33.20)`, tinggi `0,95 m`) di depan Warung Sri menghadap lampu depan mobil `Patroli depan warung` (`(1.80, 0.82, -31.20)`), diikuti `Peti tinggi A` (`(-2.30, 0.825, -27.20)`, tinggi `1,65 m`).
- **Segmen B (`z = -24.0 .. -13.0`)**: `Shadow berjaga` (`(3.30, 1.68, -18.50)`) menyapu jalan secara lateral dari sisi kanan menuju deretan cover tengah (`Peti B1`, `Gerobak B2`, `Peti B3`).
- **Segmen C (`z = -13.0 .. -2.5`)**: Dua lampu bersilangan dari `Shadow berpatroli` (`(-3.20, 1.68, -11.60)`) di kiri dan mobil `Patroli dekat halte` (`(2.15, 0.82, -9.80)`) di kanan. Pemain dapat memotong lewat **jalur tengah** (`Barikade C1` → `Barikade C2`) atau memutari **jalur trotoar kiri** (`Peti trotoar C0` → `Barikade C1` → `Peti trotoar C3` → `Barikade C2`).

[Sumber Blender](../../../BlenderSources/Environment/ChapterTwoStreet.blend) · [Modul FBX](../../../Assets/_NoRainbowAfter/Art/Models/Environment/ChapterTwo) · [Prefab jalan](../../../Assets/_NoRainbowAfter/Resources/ChapterTwoStreet.prefab)

* [Jalan dari spawn](Jalan.jpg) — Papan **HALTE PASAR 04** menyala terang tepat di tengah sumbu pandang (`dot = 0,994`, garis pandang bebas dari mata Raka ke papan nama halte).
* [Warung Sri](Warung.jpg) · [Halte Pasar](Halte.jpg) · [Lampu pada kain Shadow](SorotanShadow.jpg)
* [Peti rendah — Berdiri](PetiBerdiri.jpg) · [Peti rendah — Jongkok](PetiJongkok.jpg)
* [Ketidakcocokan sorotan sebelum perbaikan](KetidakcocokanSorotan.jpg) · [Sapuan samping selaras sesudah perbaikan](SapuanSelaras.jpg) · [Dua lampu bersilangan di Segmen C](PersilanganLampu.jpg)

---

## 2. Temuan Nyata yang Direproduksi & Diperbaiki (Tampilan Cahaya vs. Deteksi)

Selama pengujian peti rendah, sapuan samping, dan persilangan lampu dari sudut pandang first-person, ditemukan ketidakcocokan nyata antara mesh kerucut cahaya di lantai (`Spotlight floor footprint`) dan logika deteksi `Visible()` di `ChapterTwoSpotlightEncounter.cs`:

1. **Penyebab Teknis**:
   - `RebuildFootprint()` menggambar jaring kerucut di permukaan aspal (`y = 0,025 m`) berdasarkan proyeksi sudut horizontal (`XZ`), sedangkan `Visible()` menghitung sudut kerucut 3D penuh (`Vector3.Angle(beam.emitter.forward, delta)`) dari emitter yang berada di ketinggian `y = 0,82 m` (lampu mobil, kemiringan `2°`) dan `y = 1,68 m` (mata/lentera Shadow, kemiringan `7°`).
   - Akibatnya, saat `RebuildFootprint()` memanggil `Visible(floorPoint)` pada `y = 0,025 m`:
     - **Dead zone lantai di depan lampu mobil (`Patroli depan warung`)**: Kerucut 3D baru menyentuh lantai pada jarak `2,68 m`, sehingga di celah antara bemper mobil dan `Peti rendah A` (`2,54 m .. 2,80 m`) **tidak ada segitiga cahaya sama sekali di aspal** (`0` titik lantai terdeteksi), padahal kepala Raka yang berdiri di belakang `Peti rendah A` (`y = 1,57 m`) langsung terdeteksi (`IsIlluminated = true`).
     - **Dead zone & selisih lebar sapuan samping (`Shadow berjaga` & `Shadow berpatroli`)**: Dari ketinggian `1,68 m`, kerucut 3D sama sekali tidak menyentuh lantai pada jarak `< 3,33 m` di depan Shadow, dan pada jarak `4 m .. 8 m` penampang kerucut di lantai lebih sempit `0,6 m – 0,9 m` dibandingkan penampang pada dada/kepala Raka (`y = 0,98 m .. 1,57 m`). Pada posisi `(-0.90, 0.00, -14.80)` saat sapuan samping mendekat ([KetidakcocokanSorotan.jpg](KetidakcocokanSorotan.jpg)), meter merah dan bunyi peringatan sudah menyala (`IsIlluminated = true`, `Exposure = 0,35 s`), tetapi aspal di sekitar kaki Raka masih gelap tanpa jejak kerucut cahaya.
   - Selain itu, `ProjectFloor()` sebelumnya memakai `Physics.Raycast` tunggal yang dapat mengenai kapsul `CharacterController` Raka sendiri (membuat lubang pada kerucut merah tepat di bawah kaki Raka) serta memanjat naik ke plinth beton Halte Pasar dan trotoar, menghasilkan segitiga bergerigi di area aman halte.

2. **Perbaikan yang Diterapkan**:
   - Menyelaraskan pemeriksaan sudut dan jarak kerucut di `Visible()` (`ChapterTwoSpotlightEncounter.cs`) menggunakan proyeksi bidang horizontal (`Vector3.ProjectOnPlane`) yang dipadukan dengan raycast oklusi 3D (`Physics.RaycastNonAlloc`) dari emitter ke titik sampel tubuh (`kaki`, `dada`, `mata`). Dengan demikian, batas luar kerucut cahaya di aspal (`Spotlight floor footprint`) selalu berimpit `1:1` dengan volume deteksi tubuh Raka ([SapuanSelaras.jpg](SapuanSelaras.jpg)).
   - Memperbaiki `ProjectFloor()` agar mengabaikan collider di bawah `motor.transform`, membatasi proyeksi lantai pada permukaan aspal (`y <= +0,05 m`), serta melewati quad yang melintasi beda tinggi trotoar/plinth (`maxY - minY > 0,06 m`).
   - Memperbarui `ChapterTwoPhone.RefreshUi()` agar memfokuskan tombol aktif (`Kirim balasan` / `Tutup`) pada `EventSystem.current` ketika layar percakapan Bayu terbuka.

---

## 3. Hasil Verifikasi Otomatis & Pemeriksaan Teknis

### A. Sesi Utuh dengan Keyboard dan Mouse (Akhir Chapter 1 → Chapter 3)

Tes integrasi `WardrobeHideDespawnsMonster_AndRecordsHideOnce` (`ChapterOneExpansionTests.cs`) telah diperbarui agar menjalankan satu sesi penuh dari dalam lemari `Wardrobe_East` di akhir Chapter 1 hingga masuk ke Chapter 3 **hanya menggunakan event `Keyboard` dan `Mouse` (`InputSystem`)**, tanpa teleport (`TryRestorePose`), tanpa debug skip, dan tanpa pemanggilan callback langsung:

| Tahap Alur | Input yang Digunakan | Hasil Verifikasi Otomatis |
| --- | --- | --- |
| Keluar lemari & pintu koridor Chapter 1 | `[E]` keluar lemari, `[W]` jalan ke ujung koridor (`z = -17,35`), `[E]` buka `InteractionDoor_Exit` | Transisi normal dari Chapter 1 ke kamar kos malam kedua tanpa teleport. |
| Bangun di kos & progres laporan setelah laptop ditutup/dibuka | `[W]` + `[E]` buka laptop, ketik Bagian 1 + `[Enter]`, tutup dengan `[Tab]`, buka kembali dengan `[E]` | Saat laptop dibuka kembali, `CompletedSections == 1` (`Progres laporan: 1/4 bagian`) bertahan utuh; Bagian 1 tidak perlu diketik ulang. |
| Penyelesaian laporan & stres 100% | Ketik Bagian 2, 3, dan 4 dengan `QueueTextEvent` + `[Enter]` | `IsComplete == true`, `StressNormalized == 1,00` (`100%`), kontrol terkunci selama fade, dan transisi ke jalan malam terjadi tepat satu kali (`transitionedToStreet == 1`). |
| Jalan malam ke Halte Pasar | `[W]` + `[LeftShift]` + rotasi yaw `Mouse.delta` menyusuri cover hingga `Halte Pasar` | Karakter berjalan fisik melewati collider jalan sampai masuk ke zona aman `Halte Pasar` (`IsCompleted == true`). |
| Inspeksi tas di halte | Arahkan kamera ke `Tas di halte`, tekan `[E]` (mendekat), `[F]` (ambil foto), `[Tab]` (tutup) | Petunjuk `Foto gantungan bintang`, `Kartu mahasiswa Dira`, dan `Slip loker kampus` tercatat satu kali; ponsel otomatis berdering. |
| Balasan Bayu & objektif akhir | Klik kiri mouse (`MouseButton.Left`) pada tombol UI **"Kirim balasan"**, lalu `[Tab]` untuk menutup | `ReplySent == true`; event `OnObjectiveChanged("Temui Bayu di kampus")` terpanggil **tepat satu kali** (`kampusObjectiveCount == 1`), diikuti transisi ke Chapter 3. |

### B. Pemeriksaan Mekanik Stealth, Cover, Sapuan, Persilangan, Pause, dan Respawn

| Segmen & Skenario | Rute / Posisi Uji | Jumlah Percobaan Otomatis | Perilaku Deteksi & Penyebab Gagal/Lolos | Reaksi terhadap Peringatan Merah & Bunyi |
| --- | --- | --- | --- | --- |
| **Segmen A — Peti rendah berdiri vs jongkok** | Jalur awal `(-1.80, 0.00, -34.25)` di belakang `Peti rendah A` (tinggi peti `0,95 m`) | 2姿态 (Berdiri & Jongkok) | **Berdiri**: mata (`1,57 m`) dan dada (`0,98 m`) melampaui tinggi peti → terkena sorotan `Patroli depan warung` (`IsIlluminated = true`, [PetiBerdiri.jpg](PetiBerdiri.jpg)). **Jongkok**: kapsul turun ke `1,05 m` (mata `0,84 m`) → seluruh raycast terhalang peti (`IsIlluminated = false`, [PetiJongkok.jpg](PetiJongkok.jpg)). | Setelah perbaikan kerucut lantai, sorotan di depan `Peti rendah A` memiliki `> 8` titik lantai aktif sehingga pemain melihat cahaya di sekitar peti sebelum berdiri. |
| **Segmen B — Sapuan dari samping (`Shadow berjaga`)** | Tengah jalan menuju `Gerobak B2` `(-0.90, 0.00, -14.80)`, lalu mundur ke `(-2.50, 0.00, -14.50)` | 1 siklus sapuan + reaksi mundur | Saat `Shadow berjaga` menyapu dari kanan ke kiri, tepi kerucut lantai menyentuh kaki Raka tepat saat `IsIlluminated` berubah menjadi `true` ([SapuanSelaras.jpg](SapuanSelaras.jpg)). | Setelah terpapar `0,35 s` (`Exposure = 0,35 s`, vignette merah + dengung peringatan aktif), Raka mundur ke balik `Gerobak B2`; dalam `0,50 s` meter turun kembali ke `< 0,05 s` tanpa tertangkap. |
| **Segmen C — Dua lampu yang bersilangan** | Titik persilangan tengah `(-1.10, 0.00, -7.20)` di antara `Shadow berpatroli` & `Patroli dekat halte` | 1 uji tumpang tindih (`0,50 s`) | Kedua kerucut lantai (`Shadow berpatroli` dari kiri dan `Patroli dekat halte` dari kanan) menyala merah bersamaan di atas aspal ([PersilanganLampu.jpg](PersilanganLampu.jpg)). Meter paparan bersama bertambah dengan laju `1x` (`0,50 s` dalam `0,50 s`, tidak berlipat ganda). | Kedua kerucut lantai berubah merah serentak dengan bar paparan; pemain punya sisa waktu `1,50 s` (pada ambang `2,0 s`) untuk bergeser ke balik `Barikade C2`. |
| **Pause saat meter terisi** | Segmen A di depan lampu `Patroli depan warung` (`Exposure = 0,60 s`) | 1 siklus pause (`1,50 s` selama pause) | Saat `SetPaused(true)` dipanggil pada `Exposure = 0,60 s`, `ManualTick(1.5f)` menjaga `Exposure` tetap `0,60 s` dan menghentikan rotasi patroli (`yaw` tidak berubah). | Bar merah tetap membeku selama menu pause terbuka dan baru melanjutkan pengisian/penurunan setelah `SetPaused(false)`. |
| **Respawn Checkpoint A, B, dan C** | A: `(-3.2, 0.0, -39.5)`<br>B: `(-3.2, 0.0, -24.0)`<br>C: `(-2.8, 0.0, -13.0)` | 3 tangkapan berurutan (1 per segmen) | Saat `Exposure >= 2,0 s` di Segmen A (`z = -30,2`), B (`z = -18,2`), dan C (`z = -8,4`), `CatchCount` bertambah `1 -> 2 -> 3`, mengembalikan Raka ke marker checkpoint segmen terakhir dengan `Exposure == 0` dan `IsIlluminated == false`. | Setiap titik respawn berada di luar kerucut cahaya aktif sehingga pemain tidak langsung terpapar ulang sesaat setelah respawn. |

### C. Perbandingan Rute (Jalur Tengah vs. Jalur Trotoar) pada Ambang 2,0 Detik dan 1,5 Detik

Pengujian urutan percobaan (`ChapterTwoTimedRouteTests.cs` dan `ChapterTwoRouteTests.cs`) dijalankan menggunakan motor first-person (`FirstPersonMotor.SimulateInput`) dari spawn hingga zona aman `Halte Pasar` dengan fase awal patroli dan stamina awal yang identik:

| Urutan Percobaan | Rute Segmen C | Ambang Tangkap (`CatchSeconds`) | Hasil | Total Waktu Tempuh | Paparan Kontinu Maks Tanpa Berhenti (`CatchSeconds = 999s`) | Puncak Segmen A (`peakA`) | Puncak Segmen B (`peakB`) | Puncak Segmen C (`peakC`) | Puncak Total (`maxPeak`) | Margin Aman (`CatchSeconds - maxPeak`) |
| :---: | --- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **#1** | **Jalur Tengah** (`Barikade C1` → `Barikade C2`) | **2,0 s** (Baseline) | Lolos (0 tangkapan) | `14,52 s` | `1,92 s` | `1,32 s` | `1,00 s` | `1,00 s` | `1,32 s` | **`0,68 s`** (`Segmen C: 1,00 s`) |
| **#2** | **Jalur Trotoar** (`Peti trotoar C0` → `C1` → `Peti trotoar C3` → `C2`) | **2,0 s** (Baseline) | Lolos (0 tangkapan) | `16,08 s` | `2,56 s` | `1,32 s` | `1,00 s` | `1,20 s` | `1,32 s` | **`0,68 s`** (`Segmen C: 0,80 s`) |
| **#3** | **Jalur Tengah** (`Barikade C1` → `Barikade C2`) | **1,5 s** (Uji Ketat) | Lolos (0 tangkapan) | `14,52 s` | `1,92 s` | `1,32 s` | `1,00 s` | `1,00 s` | `1,32 s` | **`0,18 s`** (`Segmen C: 0,50 s`) |
| **#4** | **Jalur Trotoar** (`Peti trotoar C0` → `C1` → `Peti trotoar C3` → `C2`) | **1,5 s** (Uji Ketat) | Lolos (0 tangkapan) | `16,08 s` | `2,56 s` | `1,32 s` | `1,00 s` | `1,20 s` | `1,32 s` | **`0,18 s`** (`Segmen C: 0,30 s`) |

* **Catatan Efek Menghafal Rute & Keputusan Default `2,0 Detik`**:
  - Bila pemain berjalan terus tanpa berhenti membaca sapuan (`CatchSeconds = 999s`), jalur tengah mengakumulasi paparan kontinu **`1,92 s`** (masih di bawah `2,0 s`, tetapi gagal pada `1,5 s`), sedangkan jalur trotoar mengakumulasi **`2,56 s`** (pasti tertangkap baik pada `2,0 s` maupun `1,5 s` bila tidak berhenti berlindung di balik `Peti trotoar C0` / `Peti trotoar C3`).
  - Pada ambang **`1,5 detik`** (Percobaan #3 dan #4), rute hanya dapat diselesaikan bila pemain sudah menghafal ritme berhenti di setiap cover, karena margin aman di Segmen A menyempit menjadi **`0,18 s`** (`1,32 s / 1,50 s`) dan di Segmen C jalur trotoar menyempit menjadi **`0,30 s`** (`1,20 s / 1,50 s`). Oleh karena itu, **`2,0 detik` dipertahankan sebagai nilai default** (`CatchSeconds = 2f`).

---

## 4. Jawaban atas Lima Pertanyaan Evaluasi Chapter 2

1. **Apakah pemain mengenali halte dari spawn?**
   - **Secara teknis/visual**: **Ya.** Dari titik spawn `(-3.20, 0.00, -39.50)`, papan **HALTE PASAR 04** yang menyala terang berada tepat di tengah koridor pandang (`dot = 0,994` terhadap arah hadap awal kamera) dengan garis pandang (`line-of-sight`) bebas hambatan di atas peti-peti kayu ([Jalan.jpg](Jalan.jpg)).
   - **Dari sudut pandang pemain manusia baru**: percobaan buta belum dilakukan pada prototipe ini.
2. **Bagian mana yang paling sering membuat tertangkap?**
   - **Segmen A (`Peti rendah A` → `Peti tinggi A`, puncak paparan `1,32 s`)**: Memiliki durasi paparan puncak tertinggi (`1,32 s` pada traversal normal) dan menjebak pemain yang berhenti sambil **berdiri** di belakang `Peti rendah A` (tinggi peti `0,95 m`, sehingga kepala pada `1,57 m` tetap terkena lampu `Patroli depan warung` kecuali pemain jongkok).
   - **Segmen C pada Jalur Trotoar (`Peti trotoar C0` → `Peti trotoar C3`, puncak paparan `1,20 s`, paparan kontinu tanpa berhenti `2,56 s`)**: Zona persilangan dua lampu (`Shadow berpatroli` + `Patroli dekat halte`) pasti menangkap pemain (`2,56 s > 2,00 s`) bila pemain mencoba berlari langsung di sepanjang trotoar tanpa berhenti menunggu sapuan di balik `Peti trotoar C3` / `Barikade C2`.
3. **Bagaimana pengalaman kedua rute berbeda?**
   - **Jalur Tengah (`Barikade C1` → `Barikade C2`)**: Lebih singkat (`14,52 s`) dan langsung menuju halte, tetapi menuntut penentuan waktu (`timing`) yang presisi untuk memotong titik silang dua sorotan lampu di tengah aspal (`peakC = 1,00 s`; paparan kontinu tanpa jeda `1,92 s`).
   - **Jalur Trotoar (`Peti trotoar C0` → `Barikade C1` → `Peti trotoar C3` → `Barikade C2`)**: Lebih panjang (`16,08 s`) dan bersifat *stop-and-go* (berpindah dari satu peti trotoar ke peti berikutnya). Rute ini memberi lebih banyak titik singgah fisik di pinggir jalan, tetapi melintasi busur sapuan `Shadow berpatroli` lebih lama (`peakC = 1,20 s`; paparan kontinu tanpa jeda `2,56 s` sehingga wajib berhenti di cover antara).
4. **Durasi paparan mana yang didukung hasil uji?**
   - **Ambang `2,0 detik` didukung penuh sebagai default.** Pada `2,0 detik`, pemain memiliki margin reaksi `0,68 s – 1,00 s` setelah terkena tepi sorotan untuk mundur atau jongkok ke balik cover. Pada `1,5 detik`, margin aman di Segmen A tinggal `0,18 s` (`1,32 s / 1,50 s`) dan di Segmen C trotoar tinggal `0,30 s` (`1,20 s / 1,50 s`), yang terlalu menghukum bagi pemain baru yang belum menghafal pola sapuan.
5. **Apa yang masih belum terverifikasi?**
   - Hasil teknis mencakup traversal dan deteksi. Respons pemain manusia baru belum tercatat pada prototipe ini.

---

## 5. Regresi & Perbandingan terhadap Baseline

* **9 Tes Integrasi Chapter 2 (`Editor PlayMode` & `Windows Player x64`)**:
  - **Unity Editor PlayMode**: **9/9 Lulus** ([PlayMode.xml](PlayMode.xml)) — sesuai baseline `9/9`, kini mencakup sesi penuh keyboard+mouse dari akhir Chapter 1 hingga Chapter 3, uji keselarasan lantai sapuan samping, uji persilangan dua lampu, dan telemetri urutan percobaan `2,0 s` vs `1,5 s`.
  - **Windows Player x64 (`StandaloneWindows64`)**: **9/9 Lulus** ([WindowsPlayer.xml](WindowsPlayer.xml)).
* **Suite PlayMode Lengkap (`75` tes)**:
  - **67/75 Lulus** — sama persis dengan baseline pra-sesi (`67/75`).
  - Delapan kegagalan yang tersisa adalah kegagalan lama dari baseline sebelum merge (`7` tes Shadow koridor Chapter 1 lama + `1` tes tinggi kapsul berdiri `FirstPersonVisualLabTests` yang mengharapkan `> 1,79 m` sementara rig menggunakan `1,70 m`). **Tidak ada regresi baru.** [Baseline Shadow lama](../ChapterOneSequence/BaselineLegacyTests.xml)
