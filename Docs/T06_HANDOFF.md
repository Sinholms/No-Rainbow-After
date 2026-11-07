# T06 — Monster placeholder AI dan encounter

Tanggal: 7 November 2026. Baseline `d4ab8f0` (T05), Unity **6000.6.8f1 / URP 17.6.0 / Input System 1.20.1**, Blender **5.2.3 LTS**. Sesi ini dikerjakan langsung dengan model **Argon** sesuai override khusus sesi pengguna (tanpa mengubah aturan permanen `Docs/design.md` §17.1 dan tanpa mengklaim sub-agent Terra/Luna yang tidak tersedia). Cakupan berhenti tepat pada **T06**; T07 dan T13 tidak dimulai.

## Audit awal, gap T06 dan perlindungan aset

- **Status Git awal & perubahan pengguna:** Pada awal sesi hanya terdapat perubahan pengguna pada `ProjectSettings/ProjectSettings.asset` (`0CF6715193CCA1DE9CF0C77AE8E198AF3A02C5C2F9F59341CCF91DE31A6CA535`) dan `ProjectSettings/UnityConnectSettings.asset` (`C3DBB998537BC5C0357729E0EA4554AE78213E38A6E3BCE52FA300E0090E81FF`). Kedua file pengaturan pengguna tersebut dipertahankan utuh di luar commit fitur. Pengguna juga menyerahkan turnaround resmi karakter manifestasi Raka di `Docs/References/Raka’s Manifestation Character Turnaround.png` sebagai desain final (*design fix*) untuk acuan pengerjaan detail pada task berikutnya (**T13**).
- **Dokumen acuan:** Kesepuluh dokumen acuan dibaca sebelum implementasi. Catatan kejujuran pembacaan `.docx`: tool pembaca teks langsung menolak `Docs/GDD_No_Rainbow_After_v1_1.docx` karena berupa arsip biner ZIP; seluruh teks paragraf dan tabel (1.096 paragraf non-kosong) diekstrak dan dibaca langsung dari `word/document.xml` bersama mirror `Docs/GDD_No_Rainbow_After_v1_1_REFERENCE.md`, sedangkan tata letak halaman/gambar tersemat `.docx` tidak dirender langsung.
- **Verifikasi MCP:** Unity MCP terhubung langsung sejak awal (diuji read-only pada `MasterGameplay.unity`: `isDirty=false`, 1 kamera, 3 `SceneInteractable`, 0 missing script, 0 warning/error Console). Blender MCP awalnya terputus karena aplikasi GUI Blender belum terbuka; setelah menjalankan Blender 5.2.3 LTS yang terpasang, addon Blender MCP v1.8 (protocol 15) terhubung penuh. Detail di [T06_Audit.json](QA/T06_Audit.json).
- **Gap T06 sebelum perubahan:**
  1. Belum ada implementasi runtime `IShadowEncounter` di `MasterGameplay.unity` (C1-04 s.d. C1-06 pada T05 hanya diuji dengan test double).
  2. Belum ada model 3D placeholder siluet orisinal (`ACT_001_ShadowPlaceholder`), material URP karakter, maupun prefab karakter.
  3. Belum ada binding kontekstual klik kiri (`<Mouse>/leftButton`) pada `NRA_FirstPersonActions` / `FirstPersonInputSource` / `FirstPersonPlayer`.
  4. `CoverZone` hanya melaporkan `PlayerInside` (occupancy trigger) tanpa syarat crouch, pemeriksaan occlusion/visibility fisika, progres lewatnya monster, atau pembatalan saat berdiri/keluar cover.
  5. Belum ada state machine `Dormant → Spawn → Pursue → FightResolved / HideResolved / Caught`, fade non-grafis saat tertangkap, pemulihan checkpoint aman + fallback spawn, maupun mekanisme pembatalan attempt aktif di `ChapterDirector` tanpa mengulang `C1-04`.
- **Backup sebelum perubahan:** `Temp/T06Baseline/20261107-151500/` menyimpan snapshot `MasterGameplay.unity`, `NRA_FirstPersonActions.inputactions`, `PFB_Raka_FirstPerson.prefab`, `ProjectSettings.asset`, dan `UnityConnectSettings.asset`; installer juga menyimpan `Temp/T06Baseline/MasterGameplay.before-t06.unity` (diabaikan oleh Git).

## Perilaku dan cara mencoba

Jalankan dari `MainMenu.unity` (**Mulai Baru**) atau langsung Play `Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity`:

1. **Prasyarat ChapterDirector (tetap utuh dari T05):**
   - Ambil buku di meja (`E` pada `INT_BOOK`), tutup inspect (`Esc`).
   - Keluar melalui pintu kamar kos yang terbuka ke lorong (`z ≥ 6,0 m`), lalu kembali ke kamar (`z ≤ 4,5 m`) untuk menyelesaikan loop pertama (`C1-02`).
   - Periksa slip lembur (`E` pada `INT_OVERTIME` → mencatat `CLUE_02`, menyelesaikan `C1-03`), tutup inspect (`Esc`).
   - Masuk kembali ke lorong (`z ≥ 6,0 m`): `ChapterDirector` menyelesaikan `C1-04` (*"Siapa di sana?"*, `AUD_C1_SHADOW`) dan memulai encounter.
2. **Spawn (`ShadowEncounterState.Spawn`):**
   - Siluet bayangan Raka muncul di koridor jauh sekitar **10,6 m** di depan pemain (dekat lampu `Corridor Far`, tidak pernah menempel di kamera), menghadap pemain dan bertahan singkat (`1,15 dtk`) agar terbaca jelas dari FPP dengan ruang reaksi ke pintu/cover.
3. **Pursue (`ShadowEncounterState.Pursue`):**
   - Bayangan mengejar memakai `CharacterController` (`radius = 0,23 m`, `height = 1,80 m`) tanpa menambah paket atau NavMesh baru.
   - Tidak menembus dinding lorong, `INT_C1_DOOR`, pintu terbuka kamar, perabot meja/ranjang, atau kabinet cover.
   - Saat melewati ambang pintu kamar kos yang memiliki celah bersih lebar `0,66 m` (`x ≈ +0,16`) dan ambang atas `SM_PROP_DoorLintel` setinggi `2,05 m`, `ShadowPlaceholderVisual` otomatis membungkukkan pivot tubuh atas (`PIV_ShadowUpper`) sehingga tinggi render maksimum turun ke `< 2,02 m` tanpa menembus kusen atas.
4. **Fight (`ShadowEncounterState.FightResolved`):**
   - Ketika bayangan berada dalam jarak sah (`≤ 2,75 m`), masuk kerucut arah pandang kamera (`≤ 34°`), dan **tidak terhalang objek padat**, HUD menampilkan prompt kontekstual **`[Klik Kiri] Lawan`**.
   - Satu klik kiri (`<Mouse>/leftButton`) mengunci state secara atomik ke `FightResolved`, menampilkan feedback mundur/recoil non-grafis singkat (`0,42 dtk`, `AUD_C1_FIGHT`), lalu memanggil callback resolusi `EncounterResponse.Fight` **tepat satu kali** (tahan spam klik).
   - Input Fight dinonaktifkan selama Pause, Inspect, transisi Caught, serta pada frame tepat saat keluar dari Pause.
5. **Hide (`ShadowEncounterState.HideResolved`):**
   - Masuk ke salah satu relung cover sah (`C1_COVER_LEFT` di `z = 9,0 m` atau `C1_COVER_RIGHT` di `z = 13,0 m`) dan **merunduk (`Left Ctrl`)**.
   - Menyentuh volume `CoverZone` sambil berdiri **tidak pernah auto-Hide** (monster akan tetap mendeteksi, masuk ke mulut relung, dan menangkap pemain). Merunduk tepat di depan tatapan dekat monster yang tidak terhalang juga tidak menipu monster.
   - `ShadowVisibility.EvaluateCover` memeriksa occupancy `CoverZone`, postur crouch aktual (`IsCrouched` dan tinggi kapsul `≤ 1,28 m`), serta raycast fisika garis pandang mata monster ke kamera/dada pemain dan sudut tatapan membungkuk monster. Saat pemain tersembunyi sah, monster beralih ke lintasan patroli koridor; setelah pemain bertahan minimal `2,0 dtk` hingga monster melewati relung dan terhalang dinding koridor (`|Δz| ≥ 2,15 m`), state masuk `HideResolved` (`AUD_C1_HIDE`) dan menyelesaikan `EncounterResponse.Hide` **tepat satu kali**.
   - Berdiri kembali atau keluar dari volume cover sebelum selesai langsung membatalkan dan me-reset progres Hide ke `0`.
6. **Caught (`ShadowEncounterState.Caught`) & Pause Checkpoint:**
   - Jika monster mencapai jarak tangkap (`≤ 1,05 m`) dengan garis pandang bersih terhadap pemain yang tidak tersembunyi, encounter masuk state `Caught`: layar meredup non-grafis (tanpa gore/cedera eksplisit), collider monster dinonaktifkan dan diparkir ke posisi dormant terlebih dahulu, lalu pemain dikembalikan ke `DormSpawn_T04` (atau ke 4 titik fallback aman di dalam kamar bila `DormSpawn_T04` terhalang) disertai cue *"Napas dulu... aku masih di kamar."* (`AUD_C1_CAUGHT`).
   - `Caught` **bukan** resolusi Fight/Hide: tidak memajukan `C1-05`/`C1-06`, tidak mengirim `ChapterTransitionRequested`, tidak menghapus buku/slip/`LoopIndex` atau clue di disk, dan tidak mengulang `C1-04` saat pemain masuk lorong lagi untuk mencoba ulang.
   - Selama Pause atau Inspect, pergerakan AI, timer Spawn/Hide, dan klik Fight berhenti penuh. Menekan tombol Pause **"Kembali ke titik awal"** membatalkan pengejaran aktif ke `Dormant` dan menggugurkan callback lama sehingga monster tidak tertinggal mengejar posisi lama.

## Kontrak integrasi dan arsitektur

- `ChapterDirector` (`NoRainbowAfter.Narrative`):
  - Tetap menjadi satu-satunya pengatur progres C1 (`SetEncounter(IShadowEncounter)`).
  - Ditambahkan `IsCurrentEncounter(IShadowEncounter)` dan `ResetActiveEncounter()`: menggugurkan token generasi attempt yang belum selesai (saat `Caught` atau pemulihan checkpoint) dan membuka kembali pintu `TryBeginEncounter()` ketika pemain kembali memasuki lorong pada `BeatIndex == 4` **tanpa mengulang beat `C1-04`**.
  - `AwaitingEncounter` diperbarui mencakup `(BeatIndex == 3 || BeatIndex == 4) && !encounterBegun && !resolved`.
- `FirstPersonInputSource` & `FirstPersonPlayer` (`NoRainbowAfter.Player`):
  - Action `"Fight"` (`<Mouse>/leftButton`) ditambahkan ke map `"Player"` pada `NRA_FirstPersonActions.inputactions` dan fallback `CreateDefaultAsset()`.
  - `FirstPersonPlayer` meneruskan `FightRequested`, menambahkan `IsTransitionLocked` / `SetTransitionLocked(bool)` untuk transisi pemulihan `Caught`, serta guard frame `unpausedFrame` agar klik tombol UI *Lanjutkan* tidak bocor menjadi input Fight.
- Assembly baru `NoRainbowAfter.AI` (`Assets/_NoRainbowAfter/Scripts/AI/`):
  - `ShadowVisibility`: helper statis raycast occlusion (`IsLineOfSightClear`, `HasAnyClearSightlineToPlayer`), evaluasi cover (`EvaluateCover`), dan validasi jarak/sudut/penghalang klik Fight (`CanPlayerFightMonster`).
  - `ShadowPlaceholderVisual`: postur prosedural napas/ayunan langkah, recoil Fight, transisi Hide, dan bungkuk otomatis berbasis posisi ambang + `Physics.SphereCastNonAlloc` langit-langit rendah (`GetHighestRenderedY()`). Dipisahkan dari logika AI agar aset hero ber-rig dan 8 animasi pada **T13** dapat menggantikan visual tanpa membongkar state machine T06.
  - `ShadowEncounterController`: implementasi penuh `IShadowEncounter` dan `IAudioCueSource`, navigasi `CharacterController` koridor–relung–pintu–kamar, UI uGUI runtime ringan non-raycast (`[Klik Kiri] Lawan` + layar fade non-grafis), serta manajemen subscription aman pada `OnEnable`/`OnDisable`/`OnDestroy`.
- `SubtitlePlaceholder`: menerima sumber cue opsional `IAudioCueSource` ketiga pada `Configure(...)` sehingga cue bernarasi dari encounter (seperti pemulihan `Caught`) ikut tampil dan tunduk pada suspensi Pause/Inspect, sementara cue audio murni bertulisan kosong (`AUD_C1_SHADOW_PURSUE`, `AUD_C1_FIGHT`, `AUD_C1_HIDE`) diabaikan oleh subtitle namun siap didengar `AudioDirector` T15.

## Aset placeholder Blender dan pemetaan pintu

- **Blender 3D Placeholder (`ACT_001_ShadowPlaceholder_v1`):**
  - Dibuat secara orisinal di Blender 5.2.3 LTS melalui `BlenderSources/Characters/create_shadow_placeholder.py`, tersimpan di `BlenderSources/Characters/ACT_001_ShadowPlaceholder_v1.blend` dan diekspor ke `Assets/_NoRainbowAfter/Art/Models/Characters/ACT_001_ShadowPlaceholder_v1.fbx` (9 objek mesh, 1.012 vertex, 1.812 segitiga, ukuran `0,54 × 0,46 × 2,29 m`, kaki tepat di `z = 0`, pivot `PIV_ShadowUpper` pada tinggi `1,06 m`, tanpa gore).
  - Mewakili bayangan rasa bersalah dan kelelahan Raka (siluet membungkuk dengan balutan kepala/rambut basah, jaket/tali tas terkulai, dan lembaran kertas lembur/catatan menempel di tubuh), menggunakan 3 material URP Lit matte-basah di `Assets/_NoRainbowAfter/Art/Materials/Characters/` (`MAT_Shadow_Skin`, `MAT_Shadow_Cloth`, `MAT_Shadow_Paper`) yang dirakit ke `Assets/_NoRainbowAfter/Art/Prefabs/PFB_ACT_001_ShadowPlaceholder.prefab`.
  - Topologi deformasi final, rig Generic, tekstur PBR 2K Variant A/B, dan 8 klip animasi tetap menjadi cakupan **T13**.
- **Pemetaan pintu & keutuhan kos:**
  - `INT_C1_DOOR` tetap berupa pintu kamar terkunci `SM_PROP_HallClosedDoor` di sisi kanan lorong (`z ≈ 7,1 m`, flag `objective.doorChecked`) untuk uji T03 dan **tidak** diubah maknanya menjadi gate narasi.
  - Hubungan Model Prefab `ENV_KosAtmosphere` ke `ENV_KosAtmosphere_v1.fbx`, pencahayaan URP, kabut, suara hujan, variasi kertas `LoopAtmosphere`, serta tepi pandang melingkar dipertahankan utuh.
  - Installer idempoten **No Rainbow After → Install T06 Shadow Encounter** dijalankan dua kali berturut-turut dan menghasilkan tepat 1 instance `ACT_001_ShadowPlaceholder` di bawah `Chapter1Root`.

## Daftar file berubah dan ditambahkan

### Ditambahkan
- `Assets/_NoRainbowAfter/Scripts/AI/NoRainbowAfter.AI.asmdef`
- `Assets/_NoRainbowAfter/Scripts/AI/ShadowVisibility.cs`
- `Assets/_NoRainbowAfter/Scripts/AI/ShadowPlaceholderVisual.cs`
- `Assets/_NoRainbowAfter/Scripts/AI/ShadowEncounterController.cs`
- `Assets/_NoRainbowAfter/Scripts/Editor/T06ShadowEncounterInstaller.cs`
- `Assets/_NoRainbowAfter/Scripts/Tests/PlayMode/ShadowEncounterTests.cs`
- `Assets/_NoRainbowAfter/Art/Models/Characters/ACT_001_ShadowPlaceholder_v1.fbx`
- `Assets/_NoRainbowAfter/Art/Materials/Characters/MAT_Shadow_Skin.mat`
- `Assets/_NoRainbowAfter/Art/Materials/Characters/MAT_Shadow_Cloth.mat`
- `Assets/_NoRainbowAfter/Art/Materials/Characters/MAT_Shadow_Paper.mat`
- `Assets/_NoRainbowAfter/Art/Prefabs/PFB_ACT_001_ShadowPlaceholder.prefab`
- `BlenderSources/Characters/create_shadow_placeholder.py`
- `BlenderSources/Characters/ACT_001_ShadowPlaceholder_v1.blend`
- `BlenderSources/Characters/ACT_001_ShadowPlaceholder_v1_manifest.json`
- `Docs/T06_HANDOFF.md`
- `Docs/QA/T06_Audit.json`, `Docs/QA/T06_PlayMode_results.json`, `Docs/QA/T06_Windows_build.json`, `Docs/QA/T06_FinalValidation.json`
- `Docs/QA/T06_Blender_Placeholder.png`, `Docs/QA/T06_SpawnSilhouette_1080p.png`, `Docs/QA/T06_FightPrompt_1080p.png`, `Docs/QA/T06_HideCover_1080p.png`, `Docs/QA/T06_CaughtFade_1080p.png`

### Diubah
- `Assets/_NoRainbowAfter/Settings/NRA_FirstPersonActions.inputactions`
- `Assets/_NoRainbowAfter/Scripts/Player/FirstPersonInputSource.cs`
- `Assets/_NoRainbowAfter/Scripts/Player/FirstPersonPlayer.cs`
- `Assets/_NoRainbowAfter/Scripts/Narrative/ChapterDirector.cs`
- `Assets/_NoRainbowAfter/Scripts/Narrative/SubtitlePlaceholder.cs`
- `Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity`
- `Assets/_NoRainbowAfter/Scripts/Tests/PlayMode/NoRainbowAfter.PlayModeTests.asmdef`
- `Assets/_NoRainbowAfter/Scripts/Tests/PlayMode/FirstPersonInputTests.cs`
- `Assets/_NoRainbowAfter/Scripts/Tests/PlayMode/NarrativeTests.cs`

## Hasil verifikasi aktual

1. **Tes Otomatis PlayMode (`41/41` lulus, `0` gagal, `0` dilewati — `28,27 dtk`, job `534cd919b3ac4ac6bb50e80123fca7e2`):**
   - Seluruh **33 regresi T02–T05** tetap hijau (termasuk traversal rute T04 `313,5 dtk` simulasi, interaksi raycast/penghalang T03, loop/clue/subtitle/atmosfer T05). Pada `NarrativeTests.ProviderReadinessAndSynchronousResolutionAreExplicit`, provider T06 yang kini terpasang di scene dibersihkan lebih dulu dengan `director.SetEncounter(null)` khusus untuk menguji kontrak *tanpa-provider → provider-belum-siap*.
   - **8 tes baru T06** (`FirstPersonInputTests.FightUsesMouseLeftButtonAndCannotFireWhileGameplayLocked` + 7 tes integrasi di `ShadowEncounterTests`) memverifikasi dari sesi segar dengan storage clue sementara terisolasi:
     - Jarak staging Spawn FPP, penghalang tembok pada klik Fight, feedback Fight, dan ketahanan spam klik kiri (resolusi `Fight` tepat 1x, urutan `C1-01..C1-06`, transisi 1x).
     - Penyelesaian `Hide` di **`C1_COVER_LEFT`** dan **`C1_COVER_RIGHT`** setelah merunduk dan dilewati monster hingga terhalang dinding.
     - Berdiri di dalam `CoverZone` tidak pernah auto-Hide (berakhir `Caught`), serta berdiri/keluar dari cover langsung membatalkan `HideElapsedSeconds` ke `0`.
     - Pembekuan AI/timer/input saat Inspect & Pause, urutan `Esc` menutup Inspect sebelum Pause, serta pembatalan pengejaran oleh tombol Pause *"Kembali ke titik awal"* diikuti retry bersih tanpa duplikasi `C1-04`.
     - `Caught` berulang dengan pelestarian clue (`CLUE_01` + `CLUE_02`), fallback spawn aman saat `DormSpawn_T04` ditutup penghalang padat tanpa instant re-catch atau kunci kontrol tertinggal, lalu berhasil menyelesaikan encounter pada percobaan berikutnya.
     - Kepatuhan collision terhadap dinding koridor / `INT_C1_DOOR` dan bungkuk otomatis `< 2,02 m` di bawah kusen pintu `2,05 m`.
   - Bukti lengkap: [T06_PlayMode_results.json](QA/T06_PlayMode_results.json).
2. **Smoke Sintetis MCP & Bukti Screenshot FPP (1920×1080):**
   - Dijalankan pada Play Mode Editor dimulai dari `MainMenu.unity` → tombol **Mulai Baru** → `MasterGameplay.unity` memakai `ChapterDirector.StoragePathOverride` sementara di `%TEMP%`; profil pemain asli `Application.persistentDataPath/clues-v1.json` diverifikasi tetap utuh (`{"schemaVersion":1,"clueIds":["CLUE_02"]}`).
   - Bukti visual yang telah ditinjau:
     - Render preview Blender: [T06_Blender_Placeholder.png](QA/T06_Blender_Placeholder.png)
     - Siluet Spawn di koridor jauh (FPP): [T06_SpawnSilhouette_1080p.png](QA/T06_SpawnSilhouette_1080p.png)
     - Jarak dekat & prompt kontekstual `[Klik Kiri] Lawan`: [T06_FightPrompt_1080p.png](QA/T06_FightPrompt_1080p.png)
     - Merunduk di dalam `C1_COVER_LEFT` saat bayangan melintas di depan relung: [T06_HideCover_1080p.png](QA/T06_HideCover_1080p.png)
     - Pemulihan non-grafis `Caught` di kamar kos (`DormSpawn_T04`): [T06_CaughtFade_1080p.png](QA/T06_CaughtFade_1080p.png)
3. **Build Windows x64 (`Succeeded`, `0` error / `0` warning — `132,12 MB`, job `build-9e0435db62`):**
   - Output lokal (diabaikan Git): `Builds/T06EncounterPreview/NoRainbowAfter.exe` dengan 4 scene produksi (`Boot`, `MainMenu`, `MasterGameplay`, `Credits`). Catatan: percobaan build awal yang mengikutsertakan scene lab `FirstPersonTest.unity` memunculkan 1 warning lama pada mesh `SM_ENV_T02_Ramp`; build 4 scene produksi bersih `0` error / `0` warning. Bukti: [T06_Windows_build.json](QA/T06_Windows_build.json).
   - Perubahan otomatis `preloadedAssets` oleh proses build telah dibersihkan kembali, dan SHA-256 `ProjectSettings/ProjectSettings.asset` serta `ProjectSettings/UnityConnectSettings.asset` diverifikasi identik dengan snapshot awal pengguna.
4. **Playtest Fisik & Profiling Standalone:**
   - Belum dijalankan oleh manusia secara fisik dan executable standalone `NoRainbowAfter.exe` belum diluncurkan/diprofilkan FPS-nya dalam sesi ini (tidak diklaim).

## Catatan wajib untuk agent selanjutnya (T13 — Desain Fix Karakter Manifestasi Raka)

Pengguna telah menetapkan **[Raka’s Manifestation Character Turnaround.png](References/Raka’s%20Manifestation%20Character%20Turnaround.png)** (`1672×941`, 5 sudut pandang: `FRONT`, `LEFT SIDE`, `BACK`, `RIGHT SIDE`, `FRONT THREE-QUARTER`) sebagai **desain final (*design fix*)** karakter monster representasi rasa bersalah Raka (`ACT_Shadow_Raka` / `ACT_001_ShadowRakaFinal`).

Saat mengerjakan **T13** di Blender, agent selanjutnya **wajib** memuat gambar turnaround tersebut sebagai *Reference Image* di Blender dan membangun ulang model ber-rig semirip mungkin sesuai rincian di **[ACT_Shadow_Raka_Turnaround_Spec.md](References/ACT_Shadow_Raka_Turnaround_Spec.md)**:

1. **Proporsi 5 garis bantu (`HEAD` / `SHOULDER` / `WAIST` / `KNEE` / `FEET`):** Tubuh kurus kering menjulang (`2,25–2,45 m`), leher menjorok maju dengan kepala menunduk, pergelangan tangan menggantung mencapai garis `KNEE` dengan 5 jari panjang melengkung melewati bawah lutut, serta telapak kaki telanjang menapak rata di `FEET` (`z = 0`).
2. **Kepala & wajah:** Lilitan kain perban krem-abu kusam menutup kedua mata dan pangkal hidung (hidung bawah, tulang pipi cekung, dan rahang/mulut tetap samar terlihat tanpa gore), **dua potongan kertas terselip di lipatan atas kepala** (pelipis kiri-kanan), serta rambut hitam basah panjang terurai melewati bahu hingga punggung tengah.
3. **Torso, kemeja koyak & sabuk silang:** Kulit cokelat-abu lembap berserat urat/tulang rusuk menonjol; sisa kemeja kuliah putih-abu koyak dengan **tanda nilai `"F"` merah dilingkari di dada kiri** dan tumpukan kain koyak di bahu kiri; sabuk kulit cokelat tua bergesper menyilang **`X`** di punggung atas, diagonal/horizontal di dada, serta sabuk ganda di pinggang.
4. **Kertas pinggang, kain lipit bawah & kaki asimetris:** Lembaran kertas tugas/lembur bernoda terjepit di bawah sabuk pinggang (depan kiri-kanan dan pinggul belakang); kain lipit gelap bermotif kotak samar compang-camping sebatas paha tengah; **satu tali pengikat di paha atas kanan** (betis kanan telanjang) dan **pelindung/balutan kulit gelap tinggi dengan dua gesper di betis kiri**.
5. **Kompatibilitas T06/T11:** Pertahankan ukuran kapsul `CharacterController` (`r ≈ 0,23 m, h ≈ 1,80 m`) dan postur menunduk saat melintasi ambang pintu kos `2,05 m`, `applyRootMotion = false`, serta lakukan penggantian visual/Animator murni tanpa merusak kontrak `ShadowEncounterController`, `ChapterDirector`, atau `EndingResolver`.

## Keterbatasan saat ini dan status Git

- **Batas naratif & visual:** `C1-06` masih berupa kontrak event `ChapterTransitionRequested` seperti batas T05; sekuens blackout/cinematic akhir Chapter 1, bangun dengan ponsel menyala (T07/T08 sesuai peta task), Chapter 2, penyambungan `GameState.ResetClues()` ke tombol *Mulai Baru*, audio layer monster penuh (T15), serta model karakter final ber-rig dan 8 animasi sesuai turnaround di atas (T13) belum dikerjakan.
- **Status Editor & Blender:** `MasterGameplay.unity` tersimpan bersih (`isDirty=false`, Console `0` warning/error, `0` missing script/broken prefab) dengan `ACT_001_ShadowPlaceholder` terlihat di Scene View Unity dan `ACT_001_ShadowPlaceholder_v1.blend` terbuka di viewport Blender. Ringkasan validasi akhir: [T06_FinalValidation.json](QA/T06_FinalValidation.json).
- **Status Git:** Commit lokal terfokus dibuat tanpa `git push` (mencakup implementasi T06 serta pencatatan resmi file turnaround `Docs/References/Raka’s Manifestation Character Turnaround.png` dan spesifikasi `Docs/References/ACT_Shadow_Raka_Turnaround_Spec.md` untuk agent selanjutnya). Perubahan kerja yang sengaja dibiarkan tidak ter-stage / tidak di-commit hanya kedua file pengaturan milik pengguna: `ProjectSettings/ProjectSettings.asset` dan `ProjectSettings/UnityConnectSettings.asset`.
