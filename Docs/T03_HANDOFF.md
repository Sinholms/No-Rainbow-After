# T03 — Interaction dan inspect

Tanggal: 7 November 2026. T03 diimplementasikan dan diverifikasi pada Unity **6000.6.8f1 / URP 17.6.0 / Input System 1.20.1**. Acuan dibaca: `design.md`, master prompt, tasks v1.1, GDD Markdown reference, kos atmosphere handoff dan T02 handoff. GDD digunakan melalui mirror teks; artwork/layout DOCX tidak ditinjau ulang.

## Audit dan perlindungan perubahan

Baseline Git `0d482f3`, visual `bc950e2`. Scene aktif `MasterGameplay`, tidak dirty, tanpa missing script. Unity dan Blender bridge terhubung; Blender tetap membuka `NRA_KosAtmosphere_v1` (226 objek). Console awal hanya memiliki satu warning transport duplikat MCP, tanpa error game. [Audit](QA/T03_Audit.json).

Perubahan pengguna pada `ProjectSettings/ProjectSettings.asset` dan `UnityConnectSettings.asset` dipertahankan dan dikecualikan dari commit. Snapshot sebelum pemasangan ada di `Temp/T03Baseline/` (ignored). Build menambahkan kembali InputSettings ke `preloadedAssets`; daftar tersebut dikembalikan ke daftar kosong milik pengguna saat awal sesi. Kedua file dibandingkan dengan snapshot melalui SHA-256 dan sama persis.

## Perilaku dan cara mencoba

Buka `Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity`, Play, lalu fokus Game View. Dekati meja, arahkan reticle ke buku atau slip, tekan **E**. Jarak maksimum **2,3 m** diukur dari kamera sampai permukaan collider. Baca teks, lalu **Esc** untuk kembali; **Esc berikutnya** membuka pause. WASD, mouse look, sprint dan toggle crouch terkunci selama inspect. Kursor dibuka saat membaca dan dikunci kembali saat melanjutkan. Kehilangan fokus menutup inspect dan membuka pause.

| ID stabil | Mesh Blender yang digunakan | Flag sesi | Perilaku |
| --- | --- | --- | --- |
| `INT_BOOK` | `SM_PROP_StackedTextbook` | `objective.book` | Ambil/monolog melalui overlay; ditandai terpakai, prompt hilang setelah interaksi pertama. Model tetap terlihat. |
| `INT_OVERTIME` | `SM_PROP_WorkSlip` | `objective.overtime` | Slip lembur dapat dibaca berulang; flag tetap unik. |
| `INT_C1_DOOR` | `SM_PROP_HallClosedDoor` | `objective.doorChecked` | Pintu kamar pertama di kanan lorong; menampilkan alasan terkunci dan tetap tertutup. |

Pintu kos yang sudah terbuka dan lintasan kos–lorong tetap dipertahankan. `INT_C1_DOOR` adalah target uji T03 pada pintu lorong; belum menjadi chapter gate/loop T05. Teks slip merupakan placeholder fiktif tentang lembur dan biaya kuliah, tanpa timestamp kecelakaan atau spoiler Ella.

Raycast mempertimbangkan solid collider pada **semua layer**, mengabaikan trigger dan hanya hierarchy pemain sendiri. Hit solid terdekat menentukan target; dinding Default maupun Ignore Raycast tetap memblokir. Target diperiksa ulang saat E, sehingga focus frame sebelumnya tidak bisa dipakai melewati penghalang baru. Uji langsung menemukan capsule pemain menghalangi ray saat menunduk; perbaikan dan regresi berdiri/merunduk sudah disertakan.

Tiga mesh mendapat BoxCollider sesuai bounds dan layer `Interactable` (slot user 8). UI runtime memakai uGUI, reticle kecil, prompt E dengan outline hitam, dan overlay teks yang tidak bergantung pada detail tekstur kertas. Kos, material, lighting dan preferensi POV melingkar digunakan kembali. Tidak ada mesh 3D baru, penggantian metode modeling, paket baru atau migrasi pipeline.

## Implementasi

Semua path berikut relatif terhadap `Assets/_NoRainbowAfter/`:

- `Scripts/Interaction/SceneInteractable.cs`: `IInteractable`, metadata ter-serialize, `IObjectiveFlags` dan `SessionObjectiveFlags` dengan HashSet.
- `Scripts/Interaction/PlayerInteractor.cs` dan assembly `NoRainbowAfter.Interaction`: pemilihan target, focus/prompt, aktivasi dan overlay.
- `Scripts/Player/FirstPersonInputSource.cs`, `FirstPersonPlayer.cs`, `Settings/NRA_FirstPersonActions.inputactions`: aksi E, input lock inspect terpisah dari pause, Global/Esc tetap aktif. Asset input tetap disalin per pemain; fallback E mendukung asset lama.
- `Scripts/UI/FirstPersonSession.cs`: urutan Esc, kehilangan fokus dan pemulihan kontrol.
- `Scripts/Editor/InteractionInstaller.cs`: menu `No Rainbow After/Install T03 Interaction`, hanya untuk MasterGameplay yang terbuka dan scene tidak dirty; backup, layer, collider dan komponen dipasang idempotently. Tidak membangun ulang environment.
- `Scenes/MasterGameplay.unity`: pemasangan komponen pada player dan tiga mesh. Unity turut men-serialize default URP AdditionalLightData; nilai cahaya tidak dituning. `ProjectSettings/TagManager.asset` mendapat layer serta normalisasi serialisasi Unity.
- `Scripts/Tests/PlayMode/InteractionTests.cs`, `InteractionSceneTests.cs`, ekstensi tes input dan referensi assembly. Satu pemanggilan API tes kos lama diperbarui ke overload Unity yang tidak deprecated.

Terra 6.1 mengerjakan integrasi dan tes; Luna 6.0 menangani perbaikan kecil API tes dan outline UI. Agen utama meninjau diff, memasang scene, memeriksa Blender/Unity, menjalankan tes/build dan membuat commit lokal.

## Verifikasi dan bukti

- **22/22 PlayMode tests lulus**, 0 gagal/skipped: 14 tes baseline dan 8 tes tambahan. Meliputi input E, flag unik, batas jangkauan, trigger, solid occluder lintas layer, collider pemain, raycast ulang, tiga ID scene, inspect lock, Esc dan regresi kontrol/lintasan kos. [Hasil](QA/T03_PlayMode_results.json).
- Smoke runtime melalui MCP: ketiga target berhasil menandai flag masing-masing. Event keyboard sintetis E membuka slip; event Esc pertama menutup inspect tanpa pause dan mengembalikan cursor Locked; event Esc kedua membuka pause/cursor None. Device sintetis dibersihkan. Input fisik manusia belum diuji.
- Compile dan Console akhir **0 warning/error**. Scene validation **0 issue / missing script / broken prefab**.
- Windows x64 Mono build **Succeeded, 0 error / 0 warning**, 131,97 MB, 62,10 detik. Output lokal ignored: `Builds/T03InteractionPreview/NoRainbowAfter.exe`. Boot, MainMenu, MasterGameplay dan Credits disertakan. [Laporan](QA/T03_Windows_build.json). Build belum diluncurkan atau diukur FPS-nya; tidak mengklaim profiling, Low/High sweep atau full playthrough.
- Capture Editor **1920 × 1080**: [prompt](QA/T03_Prompt.png), [inspect slip](QA/T03_InspectSlip_Final.png), [buku](QA/T03_BookInspect.png), [pintu terkunci](QA/T03_LockedDoor.png). Keterbacaan prompt diperbaiki setelah review permukaan kertas terang. Capture lain memakai UI yang sama; art tidak diganti.

Flag T03 hanya berlaku untuk sesi scene berjalan. Belum ada save, registry `CLUE_02`, ChapterDirector, loop, monster, phone atau ending. T03 tidak menyelesaikan Chapter 1. Pekerjaan berikutnya adalah audit sisa gate **T04** terhadap environment Blender yang sudah ada, lalu T05 setelah diotorisasi; jangan mengganti kos kembali dengan lab kotak.

Hasil tetap terlihat di Blender dan Unity. Commit lokal dibuat setelah verifikasi; tidak ada push.
