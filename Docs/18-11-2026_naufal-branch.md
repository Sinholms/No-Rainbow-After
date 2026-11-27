# Laporan Harian Progres & Rangkuman Percakapan — 18 November 2026 (`naufal-branch`)

Dokumen ini merangkum seluruh rangkaian diskusi *brainstorming* desain, iterasi mekanik *gameplay*, perombakan aset 3D Blender & skrip C# Unity, perbaikan *bug*, pembaruan dokumen proyek, serta hasil verifikasi *PlayMode tests* pada tanggal **18 November 2026**.

---

## 1. Ringkasan Percakapan & Evolusi Desain (*Core Loop* → Implementasi)

### A. Tahap *Brainstorming* Core Loop (Diskusi Masukan Dosen)
- **Masukan Awal:** Dosen menilai *core loop* proyek *No Rainbow After* masih kurang memiliki keterhubungan sebab-akibat antara **tekanan depresi Raka** dengan **kemunculan *Shadow*** (*trigger* naratif dan *gameplay* masih perlu diperjelas agar perjalanan dari Chapter 1 sampai Ending terasa utuh dan berkesan).
- **Perumusan Alur *Core Loop* Chapter 1:**
  1. **Realitas Kos & Tekanan Akademik-Ekonomi:** Pemain bangun di kos, melihat kondisi ruang yang berangsur berubah, mengecek ponsel/slip lembur kerja (`CLUE_02`), serta duduk di meja belajar untuk mengerjakan tugas kuliah pada laptop Raka (`PROP_RakaLaptop`).
  2. **Hubungan Depresi → Manifestasi *Shadow*:** Kesalahan mengetik tugas, kelelahan/tenggat waktu, serta tekanan atmosfer koridor menaikkan **Barometer Stres**, menjembatani kondisi mental Raka dengan munculnya bayangan pengejar (*Shadow*) di koridor horor yang terdistorsi.
  3. **Regulasi Napas & Pelarian:** Pemain tidak sekadar berlari menghindari *Shadow* ke dalam lemari persembunyian (`HidingWardrobe`), tetapi juga harus menjaga kesadaran dan menenangkan dirinya menggunakan latihan pernapasan (`[Tab]`).

### B. Iterasi Implementasi 1 — Laptop Monkeytype, Stress Barometer `50%`, FPP Body, & Tutup `[F]`
- Memodelkan prop laptop mahasiswa [PROP_RakaLaptop.fbx](../Assets/_NoRainbowAfter/Art/Models/Props/PROP_RakaLaptop.fbx) melalui Blender (`INT_LAPTOP`, objektif awal **"Mengerjakan tugas"**).
- Mengimplementasikan [LaptopTypingMinigame.cs](../Assets/_NoRainbowAfter/Scripts/Interaction/LaptopTypingMinigame.cs) bergaya *Monkeytype* dengan bank kalimat bertema kuliah mahasiswa.
- Menambahkan **Barometer Stres** berwarna merah di pojok kiri atas HUD ([StressBarometer.cs](../Assets/_NoRainbowAfter/Scripts/Player/StressBarometer.cs)) yang dimulai pada angka **`50%`**, naik saat salah mengetik, pengiriman tidak lengkap, maupun waktu habis.
- Memisahkan tombol penutupan layar inspeksi dokumen (slip lembur, buku, pintu) ke **`[F]`** agar `Esc` tetap eksklusif untuk menu jeda.
- Membangun fisik tubuh FPP Raka (*headless*, terlihat saat menunduk ala *MiSide*) via [ACT_Raka_StudentBody.fbx](../Assets/_NoRainbowAfter/Art/Models/Characters/ACT_Raka_StudentBody.fbx) dan [FirstPersonBodyVisual.cs](../Assets/_NoRainbowAfter/Scripts/Player/FirstPersonBodyVisual.cs).

### C. Iterasi Implementasi 2 — Tubuh Organik 5-Jari, Perbaikan Bug UI/Teks, & Minigame Napas `[Tab]`
Berdasarkan tinjauan langsung dan gambar referensi karakter (*turnaround* depan, samping, belakang, dan detail tangan):
- **Perombakan Organik `ACT_Raka_StudentBody`:** Menghilangkan siluet kotak-kotak (*box primitives*) dan sambungan terpisah melalui [create_raka_student_body.py](../BlenderSources/Characters/create_raka_student_body.py) memakai *Subdivision Surface Level 2* dan *outward face normals*: kemeja lengan pendek kotak-kotak biru-navy/charcoal berkancing di atas celana, kerah lipat, saku dada kiri + pulpen, lanyard biru berkontur + ID card putih, jam tangan hitam bundar di kiri, lengan bawah yang menyatu mulus ke **tangan 5 jari terartikulasi** (*unified quad-manifold*), celana kain ramping gelap, dan sepatu *slip-on* bersol putih tebal.
- **Perbaikan Bug Layer Ganda `"50%"` di Stress Barometer:** Menghapus duplikasi panel `StressMirror` pada laptop, membersihkan *added prefab overrides* ganda pada `Player_Raka_FPP` lewat [RakaGameplayRevampInstaller.cs](https://github.com/Sinholms/No-Rainbow-After/blob/8029415ddc516ed8ff9b8cccd460dc38b94bc23a/Assets/_NoRainbowAfter/Scripts/Editor/RakaGameplayRevampInstaller.cs), dan memusatkan tampilan di satu *overlay Canvas* khusus (`StressBarometerCanvas`, `sortingOrder = 25`).
- **Perbaikan Bug Markup `<br>[huruf]<br/>` & Warna Kuning Mengetik:** Menghapus tag `<u>...</u>` yang tidak didukung `UnityEngine.UI.Text` dari `BuildMonkeytypeRichText()` dan mengubah huruf yang sudah diketik menjadi berwarna **kuning (`#FFD54F` / `#FFF176`)**.
- **Durasi & Auto-Exit Awal:** Mengubah timer laptop dari `18` detik menjadi rentang `60`–`30` detik dan keluar otomatis setelah kalimat selesai.
- **Minigame Atur Napas (`[Tab]`):** Membuat [BreathingMinigame.cs](../Assets/_NoRainbowAfter/Scripts/Player/BreathingMinigame.cs) dengan indikator parameter di dalam **bar hijau** (`[Spasi]` / klik kiri untuk tarik napas, lepas untuk keluar napas), audio napas ([AUD_RakaBreath_Inhale.wav](../Assets/_NoRainbowAfter/Audio/SFX/AUD_RakaBreath_Inhale.wav) & [AUD_RakaBreath_Exhale.wav](../Assets/_NoRainbowAfter/Audio/SFX/AUD_RakaBreath_Exhale.wav)), serta anggukan kamera FPP (**naik ke atas** saat tarik napas, **turun ke bawah** saat hembus napas) melalui `SetBreathingPitchOffset()` di [FirstPersonMotor.cs](../Assets/_NoRainbowAfter/Scripts/Player/FirstPersonMotor.cs).

### D. Iterasi Implementasi 3 — Penyempurnaan Kontrol `[Tab]`, 5 Kalimat, Tekanan Koridor `>= 70%`, Respawn `100%`, & Penutup C1
- **Keluar Laptop `1x Klik [Tab]`:** Mengganti mekanik tahan `Enter` 5 detik menjadi `1x tekan [Tab]` (`FirstPersonPlayer.LaptopClosedFrame` mencegah minigame napas ikut terbuka di frame yang sama). Tombol `[Enter]` langsung mengirim kalimat.
- **Target Mengetik Menjadi `5 Kalimat`:** `RequiredSentencesForObjective = 5` dengan skala waktu progresif **`60.0` → `52.5` → `45.0` → `37.5` → `30.0` detik**, otomatis keluar setelah kalimat ke-5 selesai.
- **Kenaikan Stres Koridor & Atur Napas saat Bersembunyi:** Di [ChapterOneCorridorShift.cs](../Assets/_NoRainbowAfter/Scripts/AI/ChapterOneCorridorShift.cs), stres Raka di koridor naik **`+4%` setiap `3,0 detik`** selama di luar persembunyian (`!player.IsHiding && !player.IsBreathing`). Masuk ke `HidingWardrobe` menghentikan kenaikan stres koridor dan mengizinkan `[Tab]` untuk mengatur napas tanpa keluar dari lemari.
- **Efek Layar Memerah & Peringatan Berkedip Pelan (`>= 70%`):** Pada [StressBarometer.cs](../Assets/_NoRainbowAfter/Scripts/Player/StressBarometer.cs), begitu stres mencapai `70%`, *vignette* merah mulai menyelimuti layar disertai teks peringatan berkedip pelan untuk segera bersembunyi dan menekan `[Tab]`.
- **Tertangkap *Shadow* Respawn Awal Koridor & Stres `100%`:** Pada [ShadowEncounterController.cs](../Assets/_NoRainbowAfter/Scripts/AI/ShadowEncounterController.cs) dan `ChapterOneCorridorShift.TryRespawnAtCorridorStart()`, tangkapan yang berujung respawn tidak lagi memulangkan Raka ke kos, melainkan ke **titik awal koridor (`Entry`)** dengan **`StressBarometer = 100%`** dan status wajib pemulihan (`RequiresBreathRecovery` / `FirstPersonPlayer.IsBreathRecoveryRequired`) yang menahan langkah kaki sampai Raka menekan `[Tab]` dan menurunkan stres ke bawah `70%`.
- **Selesai Chapter 1 Kembali ke Kos:** Membuka pintu keluar koridor (`INT_C1_EXIT`) menutup koridor horor dan memulangkan Raka kembali ke kamar kosnya (`DormCheckpoint`) dengan objektif HUD [ObjectiveTracker.cs](../Assets/_NoRainbowAfter/Scripts/Narrative/ObjectiveTracker.cs) bertuliskan **`"Chapter 2 sedang dikembangkan"`** tanpa pindah ke `MainMenu`.

---

## 2. Daftar Lengkap Perubahan File

### A. Aset Blender, Tekstur PBR, Material URP & Audio SFX
| Kategori | Path File | Keterangan |
| :--- | :--- | :--- |
| Script Blender Props | [create_raka_laptop.py](../BlenderSources/Environment/create_raka_laptop.py) | Generator 3D laptop mahasiswa (`PROP_RakaLaptop.blend` → `PROP_RakaLaptop.fbx`). |
| Model Props FBX | [PROP_RakaLaptop.fbx](../Assets/_NoRainbowAfter/Art/Models/Props/PROP_RakaLaptop.fbx) | Model laptop di atas meja belajar Raka (`SM_PROP_StudyDesktop`). |
| Tekstur & Material Laptop | [T_RakaLaptop_Screen.png](../Assets/_NoRainbowAfter/Art/Textures/Props/T_RakaLaptop_Screen.png) & `Assets/_NoRainbowAfter/Art/Materials/Props/MAT_Laptop_*.mat` | Layar emissive + 6 material URP (`Chassis`, `DarkTrim`, `Keys`, `AccentKey`, `Sticker`, `Screen`). |
| Script Blender Karakter | [create_raka_student_body.py](../BlenderSources/Characters/create_raka_student_body.py) | Generator 3D tubuh FPP Raka organik 5-jari (`ACT_Raka_StudentBody.blend` → `ACT_Raka_StudentBody.fbx`). |
| Model Karakter FBX | [ACT_Raka_StudentBody.fbx](../Assets/_NoRainbowAfter/Art/Models/Characters/ACT_Raka_StudentBody.fbx) | Fisik tubuh FPP Raka *headless* dengan hierarki pivot gerak lengkap. |
| Tekstur & Material Karakter | [T_RakaBody_Flannel.png](../Assets/_NoRainbowAfter/Art/Textures/RakaBody/T_RakaBody_Flannel.png), [T_RakaBody_Jeans.png](../Assets/_NoRainbowAfter/Art/Textures/RakaBody/T_RakaBody_Jeans.png), & `MAT_RakaBody_*.mat` | Pola kotak-kotak biru-navy/charcoal kemeja, kain celana gelap, kulit, lanyard, ID card, jam tangan, dan sepatu *slip-on*. |
| Audio Pernapasan SFX | [AUD_RakaBreath_Inhale.wav](../Assets/_NoRainbowAfter/Audio/SFX/AUD_RakaBreath_Inhale.wav) & [AUD_RakaBreath_Exhale.wav](../Assets/_NoRainbowAfter/Audio/SFX/AUD_RakaBreath_Exhale.wav) | Klip audio tarik & hembus napas tersinkronisasi untuk minigame `[Tab]`. |

### B. Skrip C# Runtime, Editor, Input, Prefab & Scene
- **Skrip Baru:**
  - [StressBarometer.cs](../Assets/_NoRainbowAfter/Scripts/Player/StressBarometer.cs) — Pelacak stres `0–100%` (mulai `50%`), HUD tunggal pojok kiri atas (`sortingOrder = 25`), serta efek layar merah & warning berkedip saat `>= 70%`.
  - [BreathingMinigame.cs](../Assets/_NoRainbowAfter/Scripts/Player/BreathingMinigame.cs) — Minigame `[Tab]` bar hijau (`-10%/dtk`), pemutaran suara napas, dan modulasi anggukan kamera (`SetBreathingPitchOffset`).
  - [FirstPersonBodyVisual.cs](../Assets/_NoRainbowAfter/Scripts/Player/FirstPersonBodyVisual.cs) — Penggerak prosedural bagian tubuh FPP `ACT_Raka_StudentBody`.
  - [LaptopTypingMinigame.cs](../Assets/_NoRainbowAfter/Scripts/Interaction/LaptopTypingMinigame.cs) — Minigame *Monkeytype* 5 kalimat, teks kuning tanpa markup rusak, timer `60 → 30` detik, kirim `[Enter]`, keluar `1x [Tab]` atau auto-exit setelah kalimat ke-5.
  - [RakaGameplayRevampInstaller.cs](https://github.com/Sinholms/No-Rainbow-After/blob/8029415ddc516ed8ff9b8cccd460dc38b94bc23a/Assets/_NoRainbowAfter/Scripts/Editor/RakaGameplayRevampInstaller.cs) — Menu installer Unity Editor untuk merakit prop laptop, input asset, audio napas, body prefab, dan deduplikasi komponen di scene & prefab.
- **Skrip, Pengaturan, Scene & Prefab yang Diperbarui:**
  - Input & Kontrol Pemain: [FirstPersonInputSource.cs](../Assets/_NoRainbowAfter/Scripts/Player/FirstPersonInputSource.cs), [NRA_FirstPersonActions.inputactions](../Assets/_NoRainbowAfter/Settings/NRA_FirstPersonActions.inputactions), [FirstPersonPlayer.cs](../Assets/_NoRainbowAfter/Scripts/Player/FirstPersonPlayer.cs), [FirstPersonMotor.cs](../Assets/_NoRainbowAfter/Scripts/Player/FirstPersonMotor.cs), [NoRainbowAfter.Player.asmdef](../Assets/_NoRainbowAfter/Scripts/Player/NoRainbowAfter.Player.asmdef).
  - Interaksi, UI & Alur Bab: [PlayerInteractor.cs](../Assets/_NoRainbowAfter/Scripts/Interaction/PlayerInteractor.cs), [NoRainbowAfter.Interaction.asmdef](../Assets/_NoRainbowAfter/Scripts/Interaction/NoRainbowAfter.Interaction.asmdef), [FirstPersonSession.cs](../Assets/_NoRainbowAfter/Scripts/UI/FirstPersonSession.cs), [ChapterOneObjective.cs](../Assets/_NoRainbowAfter/Scripts/Level/ChapterOneObjective.cs), [ObjectiveTracker.cs](../Assets/_NoRainbowAfter/Scripts/Narrative/ObjectiveTracker.cs), [ChapterDirector.cs](../Assets/_NoRainbowAfter/Scripts/Narrative/ChapterDirector.cs), [SubtitlePlaceholder.cs](../Assets/_NoRainbowAfter/Scripts/Narrative/SubtitlePlaceholder.cs).
  - AI, Koridor & Sembunyi: [ChapterOneCorridorShift.cs](../Assets/_NoRainbowAfter/Scripts/AI/ChapterOneCorridorShift.cs), [HidingWardrobe.cs](../Assets/_NoRainbowAfter/Scripts/AI/HidingWardrobe.cs), [ShadowEncounterController.cs](../Assets/_NoRainbowAfter/Scripts/AI/ShadowEncounterController.cs).
  - Scene & Prefab Utama: [MasterGameplay.unity](../Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity) & [PFB_Raka_FirstPerson.prefab](../Assets/_NoRainbowAfter/Art/Prefabs/PFB_Raka_FirstPerson.prefab).

### C. Dokumentasi & Pengujian Otomatis (`PlayMode`)
- **Dokumentasi Proyek:** [README.md](../README.md), [PEMBAGIAN_TUGAS_KERJA.md](../PEMBAGIAN_TUGAS_KERJA.md), [GDD_No_Rainbow_After_v1_1_REFERENCE.md](GDD_No_Rainbow_After_v1_1_REFERENCE.md), [CODEX_TASKS_No_Rainbow_After_v1_1.md](CODEX_TASKS_No_Rainbow_After_v1_1.md), [design.md](design.md).
- **Automated PlayMode Tests:** [FirstPersonInputTests.cs](https://github.com/Sinholms/No-Rainbow-After/blob/8029415ddc516ed8ff9b8cccd460dc38b94bc23a/Assets/_NoRainbowAfter/Scripts/Tests/PlayMode/FirstPersonInputTests.cs), [InteractionSceneTests.cs](https://github.com/Sinholms/No-Rainbow-After/blob/8029415ddc516ed8ff9b8cccd460dc38b94bc23a/Assets/_NoRainbowAfter/Scripts/Tests/PlayMode/InteractionSceneTests.cs), [ChapterOneExpansionTests.cs](https://github.com/Sinholms/No-Rainbow-After/blob/8029415ddc516ed8ff9b8cccd460dc38b94bc23a/Assets/_NoRainbowAfter/Scripts/Tests/PlayMode/ChapterOneExpansionTests.cs).

---

## 3. Ringkasan Hasil Verifikasi Terakhir

- **Unity Editor Console:** `0` error kompilasi maupun runtime (`MasterGameplay.unity` dalam keadaan aktif dan bersih).
- **PlayMode Automated Test Suite (Run pasca-edit final pada 9 suite aktif terkait):** **56 / 56 LULUS** (`0 failed`, `0 skipped`, durasi `71.72 detik`):
  - `InteractionSceneTests`: **5/5** (uji laptop 5 kalimat 60–30 detik, teks kuning tanpa tag rusak, `1x [Tab]` keluar laptop, auto-exit kalimat ke-5, single-layer `StressBarometer`, efek warning `>= 70%`, dan `[Tab]` breathing bar hijau + anggukan kamera).
  - `NoRainbowAfter.Tests.ChapterOneExpansionTests`: **12/12** (uji stres naik berkala di koridor, jeda kenaikan & boleh `[Tab]` saat bersembunyi di lemari, tertangkap *Shadow* me-respawn ke `world.Entry` dengan stres `100%` & kunci pemulihan napas, serta keluar `INT_C1_EXIT` kembali ke kamar kos dengan objektif `"Chapter 2 sedang dikembangkan"`).
  - `NoRainbowAfter.Tests.FirstPersonInputTests`: **7/7**
  - `NoRainbowAfter.Tests.FirstPersonMotorTests`: **9/9**
  - `InteractionTests`: **4/4**
  - `NoRainbowAfter.Tests.ChapterOneSceneTests`: **2/2**
  - `NoRainbowAfter.Tests.KosAtmosphereIntegrationTests`: **2/2**
  - `NarrativeTests`: **8/8**
  - `NoRainbowAfter.Tests.VisualQualitySliceTests`: **7/7**
