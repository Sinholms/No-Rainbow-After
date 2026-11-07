# T05 — Narrative director dan corridor loop

Tanggal: 7 November 2026. Baseline `197433a` (T04), Unity **6000.6.8f1 / URP 17.6.0 / Input System 1.20.1**. T05 mengimplementasikan fondasi narasi dan loop; **Chapter 1 lengkap belum selesai**. Beat C1-04–C1-06 diverifikasi melalui kontrak encounter dengan test double, bukan monster/Fight/Hide yang playable. T06 tidak dikerjakan.

## Audit, acuan dan perlindungan

Status Git awal hanya memuat perubahan pengguna pada `ProjectSettings/ProjectSettings.asset` dan `ProjectSettings/UnityConnectSettings.asset`. Tidak ada perubahan pengguna lain saat audit. Scene aktif MasterGameplay tidak dirty, tanpa missing script dan tanpa warning/error Console. Unity dan Blender bridge terhubung; Blender membuka `NRA_KosAtmosphere_v1`, 226 objek. [Audit aktual](QA/T05_Audit.json).

`design.md`, master prompt, TASKS v1.1, GDD DOCX asli, mirror GDD, serta handoff kos/T02/T03/T04 dibaca. Teks paragraf/tabel DOCX dibaca langsung dari OOXML; artwork/layout DOCX tidak dirender dan dokumen tidak diubah. GDD tetap otoritas cerita/gameplay. Prolog → C1 Denial/Anger → C2 Bargaining/Depression → C3 Acceptance → ending tetap menjadi struktur cerita; implementasi saat ini masih masuk langsung ke C1 seperti baseline. Tidak ada konfirmasi objektif nasib Ella, perubahan aturan ending, atau skor kesehatan mental.

Backup sebelum scene diubah: `Temp/T05Baseline/20261107-104525/` menyimpan scene, source Blender, FBX dan kedua file pengaturan pengguna. Installer juga membuat `Temp/T05Baseline/MasterGameplay.before-t05.unity`. Folder Temp ignored oleh Git.

## Perilaku dan cara mencoba

Play `Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity`. Ambil buku dengan E, tutup inspect dengan Esc, berjalan melalui pintu kos yang terbuka sampai ke lorong, lalu kembali ke kamar. Catatan bergeser/berputar sedikit; HUD mengarahkan membaca slip lembur. Slip bisa dibaca lebih awal: clue langsung diregistrasikan, tetapi beat C1-03 menunggu beat C1-02. Keluar–kembali dapat mengubah suasana sampai **loopIndex 3**, tanpa menambah indeks lagi.

Ambang narasi memakai posisi pintu kos z=5 m: perjalanan harus mencapai z≥6 m dan kembali ke z≤4,5 m. Runtime memeriksa lajur x sekitar pintu (±1,3 m); jitter di ambang, pause, inspect, serta `DormCheckpoint.TryRestore()` tidak menambah loop. Buku adalah syarat progres narasi, bukan collider baru yang menutup rute T04. Trigger ini adalah pemeriksaan spatial dalam ChapterDirector, bukan pintu beranimasi.

**Pemetaan pintu:** `INT_C1_DOOR` tetap mesh `SM_PROP_HallClosedDoor` di kanan lorong sekitar z=7,1 m, dengan flag `objective.doorChecked`. Itu pintu kamar terkunci untuk uji T03, bukan gate narasi. `SM_PROP_OpenDormDoor` dan lintasan kos–lorong tetap terbuka.

Setelah loop dan pembacaan slip, director menunggu provider encounter. Tidak ada monster atau tombol debug Fight/Hide yang ditambahkan ke game. Rute, cover, inspect, input dan tombol pause **Kembali ke titik awal** tetap berfungsi. Batas konten playable sesi ini berada sebelum encounter T06.

## Sistem dan kontrak integrasi

Assembly baru `Scripts/Narrative/NoRainbowAfter.Narrative.asmdef` memiliki:

- `GameState`: state sesi untuk chapter C1, beat, indeks loop, respons C1 dan set objective; profil disk khusus empat ID clue.
- `ObjectiveTracker`: menyinkronkan tiga flag T03 secara unik ke GameState, menyediakan `IsCompleted` / `ObjectiveCompleted`, dan memperbarui HUD. Komponen HUD T04 `ChapterOneObjective` tetap ada tetapi dinonaktifkan pada scene ini.
- `ChapterDirector`: `BeatCompleted(string)`, `LoopChanged(int)`, `CueRequested(NarrativeCue)`, `ChapterTransitionRequested()`, serta state yang dapat diperiksa. Indeks hanya maju berurutan.
- `IShadowEncounter`: `IsReady`, event `Ready`, dan `Begin(Action<EncounterResponse> resolve)`. T06 memasang provider melalui `ChapterDirector.SetEncounter(provider)`. Provider dimulai setelah buku/loop/slip valid, pemain berada di lorong, serta pause/inspect ditutup. Tanpa provider siap tidak ada auto-resolution atau cue yang mengklaim siluet sudah muncul.
- Callback hanya menerima `Fight` atau `Hide`; `None`, callback usang setelah provider diganti/disabled, dan callback ganda ditolak. Provider pengganti tidak mengulang beat C1-04. Ini kontrak narasi, **bukan implementasi AI atau mekanik respons**.
- `IAudioCueSource.CueRequested`: interface event untuk AudioDirector T15. `NarrativeCue` memuat speaker, teks, durasi dan ID audio; `AUD_C1_SHADOW` dipakai saat kontrak encounter dimulai. Cue lain adalah ID placeholder C1. Tidak ada voice recording, mixer atau layer monster baru yang diklaim.
- `SubtitlePlaceholder`: uGUI runtime, antrean cue, speaker/durasi, tanpa raycast; disembunyikan dan timernya berhenti selama pause/inspect. [Capture subtitle](QA/T05_Loop1_Subtitle.png).

Alur state/event: C1-01 saat start → C1-02 pada perjalanan keluar–kembali pertama yang sah → C1-03 setelah slip diketahui → C1-04 ketika provider encounter siap dimulai → C1-05 setelah callback Fight/Hide valid → C1-06 dan `ChapterTransitionRequested`. **C1-06 hanya kontrak permintaan transisi**: blackout, bangun dengan ponsel menyala, cinematic dan Chapter 2 belum dibuat. `ActiveChapter` tetap Chapter1 sampai integrasi chapter berikutnya tersedia.

T03 mendapat event tambahan `PlayerInteractor.InteractionCompleted` yang dipanggil setelah overlay inspect dibuka. Flag dan raycast 2,3 m tidak diganti. Motor mendapat event `PoseRestored` setelah pemulihan pose berhasil; director memakai ini untuk membatalkan perjalanan loop yang sedang armed. Checkpoint tidak menghapus state/clue.

## Variasi visual

`LoopAtmosphere` menggerakkan sembilan transform mesh Blender yang sudah ada: OpenNotes + heading/lima baris, WorkSlip + heading. Posisi/rotasi awal dicache dan perubahan dihitung dari baseline, tidak diakumulasi. Teks mengikuti kertas. Yaw dihitung terhadap world-up supaya kertas tidak terangkat dari meja akibat orientasi impor FBX.

| Loop | Catatan | Slip | Intensitas Corridor Near | Fog density |
| --- | --- | --- | --- | --- |
| 0 | posisi awal | posisi awal | 0,800 | 0,01200 |
| 1 | +3 cm X, yaw +4° | posisi awal | 0,736 | 0,01344 |
| 2 | +6 cm X, yaw +8° | +3 cm X | 0,664 | 0,01500 |
| 3 | +9 cm X, yaw +12° | +6 cm X | 0,592 | 0,01656 |

Nilai bertransisi perlahan; tint koridor sedikit lebih dingin. Tidak ada flashing. Variasi berhenti selama pause/inspect dan baseline dipulihkan saat komponen dinonaktifkan/keluar Play Mode. Desk/window/player light, material, hujan dan POV melingkar dipertahankan. [Loop 0](QA/T05_Loop0_Desk.png), [loop 3](QA/T05_Loop3_Desk.png).

Upaya grouping awal ditolak Unity karena child Model Prefab tidak boleh dipindah parent. Perbaikan final menggerakkan array transform langsung, memakai dua marker pivot kosong; tidak melakukan unpack prefab. Sembilan objek yang bergerak ditandai nonstatic agar static batching/baking tidak menahan visual lama. Mesh tetap terhubung ke FBX. Source `.blend` dan FBX tidak diubah, tidak ada model 3D baru atau metode modeling pengganti; Blender tetap membuka kos asli.

Installer **No Rainbow After → Install T05 Narrative** memerlukan MasterGameplay aktif dan semua scene tersimpan. Pemasangan dua kali menghasilkan satu director/tracker/subtitle/atmosphere, dua pivot, sembilan target nonstatic dan hubungan Model Prefab tetap utuh. [Bukti](QA/T05_Installer.json).

## Persistensi clue: cakupan dan batas

Hanya `CLUE_01`, `CLUE_02`, `CLUE_03`, `CLUE_04` yang diterima secara exact/ordinal; null/ID lain ditolak, duplikat tidak menambah jumlah. `CLUE_02` dikumpulkan saat slip diinspeksi. Foto/chat/rekaman dan penempatan tiga clue lain tetap T10/T08; API yang menerima empat ID tidak berarti empat objek clue telah ditempatkan.

Profil default: `Application.persistentDataPath/clues-v1.json`, JSON `schemaVersion: 1` dan daftar ID terurut. Set unik dimuat ulang; penulisan memakai file sementara lalu replace/move. File dengan schema yang belum didukung atau format bermasalah tidak otomatis ditimpa; `StorageStatus` melaporkan masalah. Clue bersifat monotonic pada scene reload/checkpoint. Loop, objective dan respons **belum disimpan ke disk**.

Ini **bukan save game lengkap**. `GameState.ResetClues()` adalah hook reset eksplisit untuk integrasi lifecycle New Game mendatang; menu New Game saat ini belum memanggilnya, sehingga profil clue yang sudah ada tetap dimuat. Continue tetap seperti baseline. Jangan menganggap reload akan melanjutkan beat atau checkpoint chapter. Tes memakai path sementara yang terisolasi; smoke memakai `Temp/T05SmokeFinal/clues-v1.json`, bukan profil pemain.

## Verifikasi aktual

**33/33 PlayMode tests lulus**, 0 gagal/skipped, job `e26cc08db3de4e86b0fdca1f7f98d9ae`. [Hasil lengkap](QA/T05_PlayMode_results.json). Delapan tes T05 mencakup urutan C1-01…06 untuk callback Fight dan Hide dari **test double**, slip lebih awal, book guard, batas tiga loop, jitter/pause/inspect, checkpoint restore, pergantian provider/callback usang/ganda, provider belum siap dan callback sinkron, persist/reload empat ID unik serta perlindungan schema masa depan, transform kertas/heading, restore lighting/fog dan subtitle pause/inspect. Tes lain mempertahankan regresi T02–T04 termasuk occlusion, E/Esc, input dan cover. Traversal T04 kembali mencapai **313,5 detik simulasi**; bukan pengukuran pacing narasi manusia.

Smoke MCP pada Play Mode baru menggerakkan motor/collision sungguhan dan sampling spatial director; interaksi dokumen dipanggil melalui komponen T03, bukan input fisik manusia. Empat perjalanan keluar–kembali berhenti di indeks 3. Setelah inspeksi slip, file dimuat ulang dan berisi satu `CLUE_02`. Tombol pause checkpoint benar-benar dipanggil melalui `Button.onClick`: kembali ke spawn, tetap pause, loop=3, beat=3, buku/slip/clue tetap ada, provider masih ditunggu. [State smoke](QA/T05_RuntimeSmoke.json), [pause checkpoint](QA/T05_CheckpointPause.png), [inspect](QA/T05_InspectSlip.png). Capture Game View komposit 1920×1080 telah ditinjau. Smoke mengaktifkan background ticking sementara; tidak mengubah PlayerSettings.

Build Windows x64 **Succeeded, 0 error / 0 warning**, 132,00 MB, job `build-231fcc15e7`. Output lokal ignored: `Builds/T05NarrativePreview/NoRainbowAfter.exe`, berisi Boot, MainMenu, MasterGameplay dan Credits. [Laporan build terakhir](QA/T05_Windows_build.json). Build belum diluncurkan atau diprofilkan.

Pemeriksaan akhir: kompilasi bersih; Console 0 warning/error; scene tersimpan dengan satu player/kamera, 0 missing script, 0 issue/broken prefab. Fog dan transform kembali ke baseline setelah stop. [Validasi akhir](QA/T05_FinalValidation.json). Pada smoke awal, cleanup test runner/background ticking memerlukan sesi Play Mode baru; pemanggilan pause di luar runtime sempat menulis TimeManager. Waktu Editor dipulihkan ke 1 dan TimeManager dikembalikan ke baseline. Satu build perantara memperingatkan source cleanup belum dikompilasi; setelah refresh/compile, build terakhir di atas bersih. Tidak ada perubahan perilaku kode setelah 33 tes lulus; cleanup terakhir hanya komentar/EOF.

Build juga menambahkan preload InputSettings secara otomatis. Blok preload dikembalikan ke daftar kosong pengguna. SHA-256 akhir kedua file pengaturan pengguna sama persis dengan snapshot awal: `ProjectSettings.asset` = `0CF6715193CCA1DE9CF0C77AE8E198AF3A02C5C2F9F59341CCF91DE31A6CA535`, `UnityConnectSettings.asset` = `C3DBB998537BC5C0357729E0EA4554AE78213E38A6E3BCE52FA300E0090E81FF`. Source Blender dan FBX juga cocok dengan hash awal.

## Batas berikutnya dan status Git

T05 berhenti pada fondasi yang teruji ini. AI, siluet, Fight/Hide/Caught playable, respawn encounter/fade dan cinematic tetap T06/dependensi lanjutan; full Chapter 1, prolog, phone, tugas, full save dan ending belum selesai. Tidak ada profiling standalone, sweep Low/High atau playtest manusia yang diklaim.

Terra 6.1 menangani kode kompleks/integrasi/tes, Luna 6.0 menangani subtitle dan cleanup kecil, agen utama melakukan audit, arah variasi visual, review, pemasangan scene, pengujian/capture/build serta commit lokal terfokus `feat: implement T05 narrative director and corridor loops`. Tidak ada push. Hasil dibiarkan terlihat pada scene kos di Unity dan Blender. Kedua pengaturan pengguna dikecualikan dari commit; perubahan kerja yang ditinggalkan hanya dua file tersebut. `git diff --check` bersih. Berhenti di T05; jangan otomatis memulai T06.
