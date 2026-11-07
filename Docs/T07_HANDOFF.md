# T07 — Visual vertical slice (URP hero room & corridor polish, 3 quality presets, dan benchmark FPS standalone)

Tanggal: 7 November 2026. Baseline `0b389c7` (di atas `5ae514b`, T06 + penguncian referensi T13), Unity **6000.6.8f1 / URP 17.6.0 / Input System 1.20.1**, Blender **5.2.3 LTS**. Sesi ini dikerjakan langsung dengan model **Gemini 4 Argon** di Antigravity sesuai arahan sesi pengguna (tanpa mengklaim sub-agent Terra/Luna yang tidak tersedia, dan tanpa mengubah aturan permanen di `Docs/design.md` §17.1). Cakupan berhenti tepat pada **T07**; **T08** (*Phone UI / Chapter 2*) dan **T13** (*rigged hero monster Raka*) tidak dimulai.

## 1. Audit awal, keputusan renderer URP, dan perlindungan aset

- **Status Git awal & perubahan pengguna:** Pada awal sesi hanya terdapat perubahan pengguna pada `ProjectSettings/ProjectSettings.asset` (`0CF6715193CCA1DE9CF0C77AE8E198AF3A02C5C2F9F59341CCF91DE31A6CA535`) dan `ProjectSettings/UnityConnectSettings.asset` (`C3DBB998537BC5C0357729E0EA4554AE78213E38A6E3BCE52FA300E0090E81FF`). Keduanya dipertahankan identik byte-demi-byte di luar commit fitur. Profil clue pemain asli di `Application.persistentDataPath/clues-v1.json` (`6666DEAD0F1C36CB032CAF8947393AEDEF07E77EB43BEC1F5CF4A5F93CF2BE46`) juga dijaga utuh menggunakan isolasi direktori sementara selama pengujian dan benchmark.
- **Pembacaan 12 dokumen acuan:** Seluruh dokumen acuan (`Docs/design.md`, `Docs/CODEX_MASTER_PROMPT.md`, `Docs/CODEX_TASKS_No_Rainbow_After_v1_1.md`, `Docs/GDD_No_Rainbow_After_v1_1.docx`, `Docs/GDD_No_Rainbow_After_v1_1_REFERENCE.md`, `Docs/KOS_ATMOSPHERE_HANDOFF.md`, `Docs/T02_HANDOFF.md` s.d. `Docs/T06_HANDOFF.md`, serta `Docs/References/ACT_Shadow_Raka_Turnaround_Spec.md`) telah dibaca sebelum perubahan. Catatan kejujuran `.docx`: karena `Docs/GDD_No_Rainbow_After_v1_1.docx` adalah arsip biner OOXML yang tidak dapat dirender visual langsung oleh tool teks, teks lengkap diekstrak secara jujur dari `word/document.xml` (1.096 paragraf non-kosong) dan dicocokkan dengan `Docs/GDD_No_Rainbow_After_v1_1_REFERENCE.md`.
- **Verifikasi MCP & backup:** Unity MCP dan Blender MCP v1.8 diverifikasi secara *read-only* lebih dulu (lihat [T07_Audit.json](QA/T07_Audit.json)). Snapshot lengkap scene, material, pengaturan kualitas, dan pengaturan proyek disimpan di `Temp/T07Baseline/20261107-164230/` (diabaikan Git) sebelum perubahan dilakukan.
- **Gap T07 sebelum perubahan:**
  1. `QualitySettings.asset` masih berupa 6 level bawaan template Unity (`Very Low` s.d. `Ultra`) yang mayoritas menunjuk aset pipa generik tanpa pemetaan **Low / Medium / High** nyata maupun pemilih UI di menu **Pengaturan** (`MainMenu`) dan **Jeda / Pause** (`MasterGameplay`).
  2. Material `KosAtmosphere` belum memiliki peta Ambient Occlusion (`_OcclusionMap`), sebagian tekstur kain/lantai masih seragam prosedural awal, belum ada variasi decal rembesan lembap dinding, jejak genangan basah koridor, tetesan hujan kaca jendela, maupun partikel atmosferik debu tipis & tetesan air langit-langit.
  3. Pencahayaan koridor ujung jauh belum memiliki *backlight* pemisah siluet dingin sehingga siluet `ACT_001_ShadowPlaceholder` pada jarak `8–12 m` dan `2–3 m` melalui aperture Circular POV masih dapat ditingkatkan keterbacaannya.
  4. Belum ada komponen sampling *frame time* / runner benchmark diagnostik non-intrusif maupun data FPS nyata pada executable standalone Windows x64.
- **Keputusan Teknis Renderer (Tetap URP 17.6.0 Forward+):** Sesuai `Docs/design.md` §0.3, §9, dan §12.3 serta otorisasi tugas T07, proyek mempertahankan **URP 17.6.0** tanpa migrasi HDRP dan tanpa *ray tracing* agar shader custom `Hidden/NoRainbowAfter/CircularViewMask`, material kos, dan 41 tes regresi T02–T06 tetap utuh 100%. Kualitas atmosferik dicapai melalui tekstur PBR + AO yang selaras secara fisik, geometri detail & mesh decal transparan Blender, pencahayaan terukur (2 lampu bayangan terarah bebas *shadow-atlas overflow*), `ReflectionProbe` *box-projected*, SSAO bertingkat, fog eksponensial, serta post-processing URP (`ACES Tonemapping`, `ColorAdjustments`, `Bloom`, `FilmGrain`).

## 2. Hero room & corridor visual polish (Blender 5.2.3 + PBR URP)

- **Geometri Detail & Mesh Decal di Blender (`BlenderSources/Environment/KosAtmosphere_v1.blend`):**
  - Skrip idempoten `BlenderSources/Environment/revise_t07_visual_polish.py` memperkaya scene `NRA_KosAtmosphere_v1` dan mengekspor ulang `Assets/_NoRainbowAfter/Art/Models/Environment/ENV_KosAtmosphere_v1.fbx` (**278 objek mesh**, **65.988 segitiga**; bertambah **52 mesh detail & decal baru** bertanda `SM_T07_*` dan `SM_DEC_*` **tanpa collider**).
  - Penambahan geometri mencakup: bantal tidur empuk berlipat (`SM_T07_Pillow`), selimut kusut menjuntai alami di tepi ranjang (`SM_T07_BlanketOverhang`), ambalan rak dinding meja beserta mug/buku catatan tambahan, rumah kap lampu neon koridor dengan tabung ganda & kotak sambungan kabel (`SM_T07_FluorescentHousing*`, `SM_T07_JunctionBox*`), klem pipa konduit, lis plint lembap, **9 mesh decal noda rembesan dinding** (`SM_DEC_DampStain_*`), **7 mesh decal genangan air organik tipis** di koridor & dekat ambang jendela (`SM_DEC_WetPuddle_*`), serta **lapisan jejak air hujan kaca jendela** (`SM_DEC_WindowRain`).
  - Seluruh invarian fisika dan gameplay dijaga utuh: celah bersih pintu terbuka kos tetap **`0,66 m` lebar × `2,05 m` ambang atas**, koridor `1,8 × 14 m`, kedua relung cover (`C1_COVER_LEFT` & `C1_COVER_RIGHT`), ketiga target `SceneInteractable` (`INT_BOOK`, `INT_OVERTIME`, `INT_C1_DOOR`), tepat `1` `Camera` aktif, **9 mesh kertas `LoopAtmosphere` tetap non-static**, dan `ACT_001_ShadowPlaceholder` **tidak** diganti prematur ke model T13.
- **Tekstur PBR + AO & Material URP (`Assets/_NoRainbowAfter/Art/Textures/KosAtmosphere/` & `Materials/KosAtmosphere/`):**
  - Albedo baru untuk keramik lantai (`Floor_BaseColor.png`, prompt tercatat di `Docs/Art/Kos_Floor_Albedo_Prompt.txt`) dan kain kasur/bantal kusam (`Cloth_BaseColor.png`, prompt di `Docs/Art/Kos_Cloth_Albedo_Prompt.txt`) diturunkan secara selaras menjadi peta `_Normal`, `_Mask` (metallic/smoothness), dan `_AO` menggunakan operator Sobel & tinggi luminansi terukur di NumPy/Blender.
  - Ditambahkan peta Ambient Occlusion (`Wall_AO`, `Wood_AO`, `Floor_AO`, `Cloth_AO`, `Metal_AO`, `Paper_AO`) serta set tekstur transparan *alpha-blended* (`WetPuddle_*`, `DampStain_*`, `WindowRain_*` dengan tepi jatuh organik sehingga bebas batas kotak spekular).
- **Partikel Atmosferik Ringan (`VFX_KosAtmosphere`):**
  - Dua sistem partikel debu halus melayang di berkas lampu meja & jendela (`FX_DormDustMotes`, `FX_WindowColdMist` memakai `MAT_VFX_DustMote`) serta dua emiter tetesan air langit-langit lambat terkendali di koridor & dekat jendela (`FX_CorridorWaterDrips`, `FX_WindowWaterDrips` memakai `MAT_VFX_WaterDrip`).

## 3. Pencahayaan, komposisi, & keterbacaan FPP

- **Kontras Hangat vs Dingin Tanpa Gelap Buta:**
  - Area meja belajar disinari `Desk Lamp` + `Desk Task Spot` hangat (`~2950 K`, bayangan lembut terarah ke meja/kursi/lantai keramik) yang kontras terhadap bocoran hujan dingin dari `Window Moonlight` (`~7800 K`), menjaga permukaan `INT_BOOK` dan `INT_OVERTIME` sangat mudah dibaca.
  - Lorong memadukan lampu plafon hangat dekat pintu (`Corridor Near`) dengan lampu plafon dingin (`Corridor Far`) serta tambahan `Corridor End Backlight` dingin di ujung lorong (`z ≈ 18,4 m`) sehingga siluet `ACT_001_ShadowPlaceholder` terpisah tegas dari dinding belakang baik saat *spawn* jauh (`10,6 m`, rentang `8–12 m`) maupun jarak dekat (`2,4 m`, rentang `2–3 m`) di dalam bukaan **Circular POV** tanpa efek kedip ekstrem.
- **Refleksi Koridor Akurat:**
  - `Room Reflection Probe` (`4,02 × 2,72 × 5,02 m`) dan `Corridor Reflection Probe` (`1,82 × 2,72 × 14,05 m`) dikonfigurasi *box-projected* sesuai dimensi ruang aktual dengan `probeBlending = true` pada URP Forward+ (`Medium` & `High`), menghasilkan kilau tipis lembap pada nat keramik dan genangan koridor tanpa artefak cakram pucat.

## 4. Tiga quality presets (`Low` / `Medium` / `High`) & UI pengaturan

Tiga pasangan aset URP dibuat di `Assets/_NoRainbowAfter/Settings/Quality/` dan didaftarkan ke `ProjectSettings/QualitySettings.asset` (indeks `0 = Low`, `1 = Medium`, `2 = High` default Standalone), dikendalikan oleh [`GraphicsQualityService`](../Assets/_NoRainbowAfter/Scripts/Quality/GraphicsQualityService.cs) dan [`QualityAtmosphereController`](../Assets/_NoRainbowAfter/Scripts/Quality/QualityAtmosphereController.cs):

| Parameter | `Low` (*Rendah*) | `Medium` (*Sedang*) | `High` (*Tinggi*) |
| :--- | :--- | :--- | :--- |
| **URP Pipeline / Renderer** | `NRA_URP_Low` / `NRA_Renderer_Low` | `NRA_URP_Medium` / `NRA_Renderer_Medium` | `NRA_URP_High` / `NRA_Renderer_High` |
| **Render Scale** | `0,85` | `1,00` | `1,00` |
| **MSAA + Camera AA** | `1x` + `None` | `2x` + `FXAA` | `4x` + `SMAA` |
| **Resolusi Tekstur (`Mipmap Limit`)** | Setengah (`1`) | Penuh (`0`) | Penuh (`0`) |
| **Shadow Distance & Cascades** | `12 m` / `1` cascade (`512`) | `22 m` / `2` cascades (`1024`) | `35 m` / `4` cascades (`2048`) |
| **Additional & Soft Shadows** | Mati (`Player View Light` tanpa shadow) | Aktif (`1024`, *Soft Medium*) | Aktif (`2048`, *Soft High*) |
| **SSAO** | Mati | Aktif (*Downsampled*, sampel sedang) | Aktif (*Full-res*, sampel tinggi) |
| **Realtime Reflection Probes** | Mati | Aktif (`128 px`, *Box Projection* + *Probe Blending*) | Aktif (`256 px`, *Box Projection* + *Probe Blending*) |
| **Partikel Debu & Tetesan Air** | `0` debu, `0` tetesan (dimatikan penuh) | `1` emiter debu (`55%`), `2` tetesan air | `2` emiter debu (`100%`), `2` tetesan air |
| **Post-Processing Runtime** | `ACES` + `ColorAdjustments` (`Bloom` & `FilmGrain` mati) | + `Bloom` (`0,14`) + `FilmGrain` (`0,07`) | + HQ `Bloom` (`0,22`) + `FilmGrain` (`0,11`) |

- **Integrasi UI & Persistensi:**
  - Menu **Jeda / Pause** di `MasterGameplay.unity` ([`FirstPersonSession`](../Assets/_NoRainbowAfter/Scripts/UI/FirstPersonSession.cs)) dan panel **Pengaturan** di `MainMenu.unity` ([`ShellScreen`](../Assets/_NoRainbowAfter/Scripts/UI/ShellScreen.cs)) kini menampilkan pemilih **`Kualitas grafis`** (`Rendah` / `Sedang` / `Tinggi`) dengan penanda tombol aktif tebal (*bold*) dan warna aksen.
  - Pilihan disimpan ke `PlayerPrefs` (`NRA.QualityPreset`) dan langsung berlaku lintas scene tanpa mengganggu slider `Sensitivitas mouse`, `Bidang pandang (FOV)`, `Lebar pandang`, maupun toggle `Tepi pandangan melingkar`. Volume post-processing di-clone khusus saat runtime (`Application.isPlaying`) sehingga aset `KosAtmosphereVolume.asset` di disk tidak pernah menjadi *dirty*.

## 5. Pengukuran performa nyata (Windows x64 Standalone vs Editor)

- **Cara menjalankan ulang benchmark standalone secara mandiri:**
  ```powershell
  & "D:\GRAVKOM\No rainbow after\Builds\T07VisualSlice\NoRainbowAfter.exe" --nra-benchmark --nra-width 1920 --nra-height 1080 --nra-warmup 1.2 --nra-seconds 3.0 --nra-output "D:\GRAVKOM\No rainbow after\Docs\QA\T07_Standalone_FPS.json"
  ```
  Tanpa flag `--nra-benchmark`, [`StandaloneBenchmarkRunner`](../Assets/_NoRainbowAfter/Scripts/Quality/StandaloneBenchmarkRunner.cs) tidak aktif sama sekali sehingga permainan normal tetap bersih 100%.
- **Spesifikasi perangkat uji aktual:** `NVIDIA GeForce RTX 3050 6GB Laptop GPU` (`Direct3D12`, `6001 MB` VRAM), `13th Gen Intel Core i5-13450HX`, `28 GB` RAM, resolusi terukur **`1920×1080`** (`vSyncCount = 0`, `targetFrameRate = -1`).
- **Perbandingan Agregat 3 Skenario (`DormHeroRoom`, `CorridorSilhouette10m`, `DynamicCorridorTraversal`):**

| Lingkungan Uji | Preset | Sampel Frame | Avg FPS | Avg Frame Time | P95 Frame Time | 1% Low FPS |
| :--- | :--- | ---: | ---: | ---: | ---: | ---: |
| **Windows x64 Standalone (`FullScreenWindow`, 1080p)** | **Low** | 5.915 | **657,04 FPS** | **1,52 ms** | 2,35 ms | **327,94 FPS** |
| **Windows x64 Standalone (`FullScreenWindow`, 1080p)** | **Medium** | 2.671 | **296,57 FPS** | **3,37 ms** | 4,53 ms | **157,54 FPS** |
| **Windows x64 Standalone (`FullScreenWindow`, 1080p)** | **High** | 2.077 | **230,67 FPS** | **4,34 ms** | 5,82 ms | **127,57 FPS** |
| Unity Editor PlayMode (`Windowed` Game View, 1080p) | Low | 1.220 | 270,89 FPS | 3,69 ms | 5,50 ms | 108,10 FPS |
| Unity Editor PlayMode (`Windowed` Game View, 1080p) | Medium | 989 | 219,35 FPS | 4,56 ms | 6,34 ms | 88,12 FPS |
| Unity Editor PlayMode (`Windowed` Game View, 1080p) | High | 870 | 193,03 FPS | 5,18 ms | 6,88 ms | 100,36 FPS |

- **Rincian Per Skenario pada Build Windows x64 Standalone (1080p):**
  - `DormHeroRoom` (meja belajar, lampu hangat, tepi ranjang, vista pintu): `Low` **590,25 FPS** (`1,69 ms`) | `Medium` **274,39 FPS** (`3,64 ms`) | `High` **210,30 FPS** (`4,76 ms`).
  - `CorridorSilhouette10m` (koridor basah + siluet monster `10,6 m`): `Low` **675,00 FPS** (`1,48 ms`) | `Medium` **309,94 FPS** (`3,23 ms`) | `High` **238,43 FPS** (`4,19 ms`).
  - `DynamicCorridorTraversal` (pergerakan FPP kamar → pintu → koridor): `Low` **705,86 FPS** (`1,42 ms`) | `Medium` **305,39 FPS** (`3,27 ms`) | `High` **243,28 FPS** (`4,11 ms`).
  - Bukti mentah lengkap: [T07_Standalone_FPS.json](QA/T07_Standalone_FPS.json) dan [T07_Editor_FPS.json](QA/T07_Editor_FPS.json).

## 6. Verifikasi otomatis, build standalone, dan bukti visual 1080p

1. **Tes Otomatis PlayMode (`48/48` lulus, `0` gagal, `0` dilewati — `32,39 dtk`, job `36129512a9e949bea14c4cf8a21590d0`):**
   - Seluruh **41 tes regresi T02–T06** lulus tanpa perubahan perilaku gameplay.
   - **7 tes baru T07** di [`VisualQualitySliceTests`](../Assets/_NoRainbowAfter/Scripts/Tests/PlayMode/VisualQualitySliceTests.cs) memverifikasi keunikan ketiga preset URP, penskalaan partikel/bayangan/probe/AA tanpa mengotori `VolumeProfile` disk, persistensi tombol kualitas di Pause & MainMenu tanpa merusak pengaturan FPP/Circular POV, keutuhan `1` kamera aktif + `9` mesh `LoopAtmosphere` non-static + `0` collider pada mesh dekorasi T07 + traversal pintu fisik, keterbacaan tak terhalang `INT_BOOK` / `INT_OVERTIME` / `INT_C1_DOOR` serta siluet monster pada `10,6 m` dan `2,4 m` di ketiga preset, serta akurasi [`FrameTimeSampler`](../Assets/_NoRainbowAfter/Scripts/Quality/FrameTimeSampler.cs).
   - Laporan lengkap: [T07_PlayMode_results.json](QA/T07_PlayMode_results.json).
2. **Build Windows x64 Standalone (`Succeeded`, `0` error / `0` warning — `148,17 MB`, job `build-b8ef18643a`):**
   - Output di folder lokal yang diabaikan Git: `Builds/T07VisualSlice/NoRainbowAfter.exe` (4 scene produksi: `Boot`, `MainMenu`, `MasterGameplay`, `Credits`). Laporan: [T07_Windows_build.json](QA/T07_Windows_build.json).
3. **Bukti Tangkapan Layar Perbandingan (`1920×1080`, diambil langsung dari Windows Standalone & Blender):**
   - Kamar kos — meja belajar, buku, slip lembur & vista koridor (**High** vs **Medium** vs **Low**):
     - [Standalone_High_DormDesk_1080p.png](QA/StandaloneShots/Standalone_High_DormDesk_1080p.png)
     - [Standalone_Medium_DormDesk_1080p.png](QA/StandaloneShots/Standalone_Medium_DormDesk_1080p.png)
     - [Standalone_Low_DormDesk_1080p.png](QA/StandaloneShots/Standalone_Low_DormDesk_1080p.png)
   - Kamar kos — ranjang, bantal/selimut, kipas & bocoran hujan dingin jendela (**High**):
     - [Standalone_High_DormBedWindow_1080p.png](QA/StandaloneShots/Standalone_High_DormBedWindow_1080p.png)
   - Koridor — pantulan keramik basah & siluet `ACT_001_ShadowPlaceholder` jarak `10,6 m` (**High** vs **Medium** vs **Low**):
     - [Standalone_High_CorridorSilhouette10m_1080p.png](QA/StandaloneShots/Standalone_High_CorridorSilhouette10m_1080p.png)
     - [Standalone_Medium_CorridorSilhouette10m_1080p.png](QA/StandaloneShots/Standalone_Medium_CorridorSilhouette10m_1080p.png)
     - [Standalone_Low_CorridorSilhouette10m_1080p.png](QA/StandaloneShots/Standalone_Low_CorridorSilhouette10m_1080p.png)
   - Siluet jarak dekat `2,4 m` di koridor (**High**):
     - [Standalone_High_MonsterReveal2_4m_1080p.png](QA/StandaloneShots/Standalone_High_MonsterReveal2_4m_1080p.png)
   - UI Jeda / Pause dengan pemilih **`Kualitas grafis`** (`Rendah` / `Sedang` / `Tinggi`):
     - [Standalone_High_PauseQualityUI_1080p.png](QA/StandaloneShots/Standalone_High_PauseQualityUI_1080p.png)
   - Viewport Blender 5.2.3 (`KosAtmosphere_v1.blend`):
     - [T07_Blender_KosPolish.png](QA/T07_Blender_KosPolish.png)

## 7. Daftar file ditambahkan & diubah

### Ditambahkan
- `Assets/_NoRainbowAfter/Scripts/Quality/NoRainbowAfter.Quality.asmdef`
- `Assets/_NoRainbowAfter/Scripts/Quality/GraphicsQualityService.cs`
- `Assets/_NoRainbowAfter/Scripts/Quality/QualityAtmosphereController.cs`
- `Assets/_NoRainbowAfter/Scripts/Quality/FrameTimeSampler.cs`
- `Assets/_NoRainbowAfter/Scripts/Quality/StandaloneBenchmarkRunner.cs`
- `Assets/_NoRainbowAfter/Scripts/Editor/T07VisualSliceInstaller.cs`
- `Assets/_NoRainbowAfter/Scripts/Tests/PlayMode/VisualQualitySliceTests.cs`
- `Assets/_NoRainbowAfter/Settings/Quality/NRA_Renderer_Low.asset`, `NRA_URP_Low.asset`
- `Assets/_NoRainbowAfter/Settings/Quality/NRA_Renderer_Medium.asset`, `NRA_URP_Medium.asset`
- `Assets/_NoRainbowAfter/Settings/Quality/NRA_Renderer_High.asset`, `NRA_URP_High.asset`
- `Assets/_NoRainbowAfter/Art/Textures/KosAtmosphere/` (`Cloth_AO.png`, `Floor_AO.png`, `Metal_AO.png`, `Paper_AO.png`, `Wall_AO.png`, `Wood_AO.png`, `DampStain_*`, `WetPuddle_*`, `WindowRain_*`)
- `Assets/_NoRainbowAfter/Art/Materials/KosAtmosphere/` (`MAT_H_DampStain.mat`, `MAT_H_WetPuddle.mat`, `MAT_H_WindowRain.mat`)
- `Assets/_NoRainbowAfter/Art/Textures/VFX/` (`T_VFX_DustMote.png`, `T_VFX_WaterDrip.png`) & `Assets/_NoRainbowAfter/Art/Materials/VFX/` (`MAT_VFX_DustMote.mat`, `MAT_VFX_WaterDrip.mat`)
- `BlenderSources/Environment/revise_t07_visual_polish.py`, `BlenderSources/Environment/T07_visual_polish_manifest.json`
- `Docs/Art/Kos_Cloth_Albedo_Prompt.txt`, `Docs/Art/Kos_Floor_Albedo_Prompt.txt`
- `Docs/T07_HANDOFF.md`
- `Docs/QA/T07_Audit.json`, `Docs/QA/T07_PlayMode_results.json`, `Docs/QA/T07_Windows_build.json`, `Docs/QA/T07_Standalone_FPS.json`, `Docs/QA/T07_Editor_FPS.json`, `Docs/QA/T07_FinalValidation.json`, `Docs/QA/T07_Blender_KosPolish.png`, serta `Docs/QA/StandaloneShots/*.png`

### Diubah
- `Assets/_NoRainbowAfter/Art/Models/Environment/ENV_KosAtmosphere_v1.fbx`
- `Assets/_NoRainbowAfter/Art/Textures/KosAtmosphere/` (`Cloth_*`, `Floor_*`, `Wall_Mask/Normal`, `Wood_Mask/Normal`, dan pengaturan `.meta` mipmap/aniso)
- `Assets/_NoRainbowAfter/Art/Materials/KosAtmosphere/` (`MAT_H_Cloth.mat`, `MAT_H_Floor.mat`, `MAT_H_Glass.mat`, `MAT_H_Ink.mat`, `MAT_H_LampCold.mat`, `MAT_H_LampWarm.mat`, `MAT_H_Metal.mat`, `MAT_H_Paper.mat`, `MAT_H_Wall.mat`, `MAT_H_Wood.mat`, `KosAtmosphereVolume.asset`)
- `Assets/_NoRainbowAfter/Scripts/UI/FirstPersonSession.cs`
- `Assets/_NoRainbowAfter/Scripts/UI/ShellScreen.cs`
- `Assets/_NoRainbowAfter/Scripts/Tests/PlayMode/NoRainbowAfter.PlayModeTests.asmdef`
- `Assets/_NoRainbowAfter/Scenes/MainMenu.unity`
- `Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity`
- `BlenderSources/Environment/KosAtmosphere_v1.blend`, `BlenderSources/Environment/KosAtmosphere_v1_manifest.json`
- `ProjectSettings/GraphicsSettings.asset`, `ProjectSettings/QualitySettings.asset`

## 8. Keterbatasan saat ini, pengingat T13, dan status Git

- **Keterbatasan cakupan saat ini:** Sesuai batas T07, transisi akhir Chapter 1 masih berupa event `ChapterTransitionRequested` (belum masuk **T08** Phone UI / bangun di ranjang / Chapter 2), monster masih menggunakan placeholder orisinal `ACT_001_ShadowPlaceholder` (belum diganti ke karakter ber-rig & 8 klip animasi **T13** sesuai referensi terkunci `Docs/References/Raka’s Manifestation Character Turnaround.png` dan `Docs/References/ACT_Shadow_Raka_Turnaround_Spec.md`), serta audio penuh masih menunggu **T15**. Benchmark dijalankan secara deterministik terukur pada executable standalone asli; playtest subjektif manusia secara fisik belum diklaim.
- **Status Editor, Blender & Git:**
  - Unity Editor terbuka bersih pada `MasterGameplay.unity` (`isDirty = false`, preset `High` aktif, `0` error/warning Console) dan Blender 5.2.3 terbuka bersih pada `KosAtmosphere_v1.blend` (`is_dirty = false`). Ringkasan: [T07_FinalValidation.json](QA/T07_FinalValidation.json).
  - SHA-256 `ProjectSettings/ProjectSettings.asset` (`0CF6715193CCA1DE9CF0C77AE8E198AF3A02C5C2F9F59341CCF91DE31A6CA535`), `ProjectSettings/UnityConnectSettings.asset` (`C3DBB998537BC5C0357729E0EA4554AE78213E38A6E3BCE52FA300E0090E81FF`), dan `clues-v1.json` diverifikasi identik 100% dengan baseline pengguna.
  - Satu commit lokal terfokus dibuat tanpa `git push`; satu-satunya file kerja yang tidak di-stage / tidak di-commit adalah kedua file pengaturan pengguna tersebut (`ProjectSettings/ProjectSettings.asset` dan `ProjectSettings/UnityConnectSettings.asset`).
