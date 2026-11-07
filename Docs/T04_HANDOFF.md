# T04 — Rute kos, cover geometry dan checkpoint

Tanggal: 7 November 2026. Baseline `ce291e3` (T03), Unity **6000.6.8f1 / URP 17.6.0 / Input System 1.20.1**. T04 memakai pengecualian workflow yang disetujui pengguna: environment Blender tetap dipakai, tidak diganti kembali dengan greybox primitif. Cerita dan gameplay tetap mengikuti GDD v1.1; tidak ada migrasi HDRP.

## Audit dan hasil

[Audit awal](QA/T04_Audit.json) menemukan kamar 4 × 5 m, lorong 14 × 1,8 m, furniture dan collision utama sudah ada. Yang belum ada: `Chapter1Root`, objective awal, checkpoint, marker cover dan cukup ruang pemain di dua ceruk berlemari. Scene awal tidak dirty dan Console tanpa warning/error. Snapshot source, FBX, scene dan pengaturan pengguna disimpan di `Temp/T04Baseline/` (ignored).

`MasterGameplay` sekarang menempatkan `ENV_KosAtmosphere` di bawah **Chapter1Root**. Player, UI dan lighting global tetap terpisah. Spawn `DormSpawn_T04` ada di kamar pada `(0.15, 0.04, 1.1)`, yaw 10°. HUD menampilkan **“Bersiap berangkat”** dan petunjuk mengambil buku. Setelah flag T03 `objective.book` aktif, petunjuk mengarahkan eksplorasi kamar dan lorong. Ini bridge kecil untuk T04, belum ObjectiveTracker/ChapterDirector T05.

Menu pause memiliki tombol **“Kembali ke titik awal”**. Tombol memulihkan posisi, yaw, pitch, stance berdiri dan kecepatan pemain, lalu tetap berada di pause sampai pemain memilih Lanjutkan. Flag interaksi dan status buku terpakai tidak dihapus. Jika spawn terhalang, pemindahan ditolak; label tombol menjelaskan titik awal terhalang. Checkpoint hanya berlaku pada sesi berjalan, tanpa save file atau checkpoint encounter.

## Blender dan cover

Source dan export tetap `BlenderSources/Environment/KosAtmosphere_v1.blend` dan `Assets/_NoRainbowAfter/Art/Models/Environment/ENV_KosAtmosphere_v1.fbx`, GUID dipertahankan. Revisi dibuat melalui Blender bridge dengan script yang dapat ditinjau: `BlenderSources/Environment/revise_t04_cover.py`. Generator `create_kos_atmosphere.py` juga diperbarui agar hasil generasi berikutnya memakai ukuran yang sama.

- Dua dinding belakang ceruk dipindah dari x ±1,65 menjadi ±2,25 m; lantai dan end-wall diperlebar. Kedalaman dalam ceruk sekitar 1,26 m.
- Lemari dan detail pintu/vent dipindah dari x ±1,44 menjadi ±2,02 m, menyisakan lajur pemain sekitar 0,91 m. Lemari tetap solid; pemain tidak harus menembusnya.
- Plafon lorong diperlebar menjadi 4,8 m agar menutup ceruk yang lebih dalam. UV planar dihitung ulang pada mesh yang diperlebar untuk mempertahankan densitas tekstur.
- Jumlah geometry tetap **226 mesh / 63.128 triangle**. Kamar, furniture lain, material, cahaya, hujan dan preferensi POV tidak diganti. [Ukuran hasil Blender](../BlenderSources/Environment/T04_cover_manifest.json), [viewport](QA/T04_Blender.png).

Marker spatial **C1_COVER_LEFT** di `(-1.45, 0.9, 9)` dan **C1_COVER_RIGHT** di `(1.45, 0.9, 13)` memakai trigger 0,8 × 1,8 × 1,1 m. Keduanya bisa dimasuki dan ditinggalkan menggunakan CharacterController. Geometri menghalangi sightline dari dua posisi lorong yang diuji pada masing-masing ceruk. `PlayerInside` memeriksa overlap collider terkini agar tidak tertinggal aktif setelah pemulihan checkpoint. Ini **bukan** keputusan Hide, AI visibility atau resolusi encounter; perilaku tersebut tetap T06.

## Implementasi dan cara mencoba

File runtime baru ada di `Assets/_NoRainbowAfter/Scripts/Level/`: `DormCheckpoint.cs`, `CoverZone.cs`, `ChapterOneObjective.cs`, dan assembly `NoRainbowAfter.Level`. `FirstPersonMotor.TryRestorePose` menolak volume spawn yang terhalang dan memulihkan pose FPP. `FirstPersonSession.cs` memiliki referensi checkpoint/button opsional agar lab T02 tetap kompatibel.

Installer eksplisit `Scripts/Editor/ChapterOneInstaller.cs`, menu **No Rainbow After → Install T04 Chapter One Readiness**, hanya berjalan pada MasterGameplay aktif dengan semua scene tersimpan. Installer membuat backup, mempertahankan model yang ada, menyesuaikan bounds collider mesh yang direvisi, serta memasang root/spawn/marker/UI secara idempotent. Uji pemasangan ulang menghasilkan 1 chapter root, 1 checkpoint, 2 cover dan 1 tombol. T03 `InteractionInstaller` diperbaiki agar bisa menemukan environment di bawah chapter root.

Untuk mencoba: Play MasterGameplay, ambil buku dengan E, tutup inspect dengan Esc, lewati pintu kos, telusuri lorong sampai ujung, masuk–keluar ceruk kiri sekitar z 9 dan kanan sekitar z 13, lalu kembali ke kamar. Coba Ctrl di ceruk. Tekan Esc, pilih Kembali ke titik awal, lalu Lanjutkan. Pintu uji T03 tetap terkunci; tidak perlu dibuka untuk menelusuri lorong.

## Verifikasi

**25/25 PlayMode tests lulus**, 0 gagal/skipped: 22 regresi T02/T03 dan 3 tes T04. [Hasil lengkap](QA/T04_PlayMode_results.json). Rute berjalan memakai motor/collision sungguhan dari spawn melewati ambang pintu hingga ujung lorong, lalu masuk–keluar kedua ceruk dan kembali. Setelah itu traversal berulang berlangsung **313,5 detik simulasi**, dengan pemeriksaan lantai dan ketercapaian waypoint. Ini soak test geometri selama lebih dari lima menit simulasi, bukan bukti durasi narasi atau pacing lima menit dari penguji manusia.

Tes juga memeriksa sightline cover, spawn yang terhalang, pemulihan pitch/stance, flag dan status buku yang tetap ada, urutan inspect/pause, serta marker cover langsung nonaktif setelah checkpoint restore. Test harness traversal mematikan handler kehilangan fokus selama motor dikendalikan tes agar Editor yang tidak fokus tidak menghentikan fixed update; perilaku pause produk tetap aktif dan diuji terpisah.

Smoke UI memanggil event tombol checkpoint pada pause: pemain kembali ke spawn, pause tetap aktif, label sukses dan flag buku tetap ada. Bukti Editor 1920 × 1080: [objective](QA/T04_Objective.png), [pintu masuk ceruk](QA/T04_CoverEntry.png), [pause/checkpoint](QA/T04_CheckpointPause.png). Pemeriksaan Console, scene dan build akhir dicatat di [runtime QA](QA/T04_RuntimeSmoke.json) dan [laporan Windows](QA/T04_Windows_build.json). Output lokal ignored: `Builds/T04RoutePreview/NoRainbowAfter.exe`.

Build belum diluncurkan untuk playthrough atau profiling FPS. Belum dilakukan playtest pacing oleh manusia atau sweep kualitas Low/High. T04 ini menyelesaikan kesiapan rute, collision, cover geometry dan checkpoint; loop C1, prolog, narasi runtut, monster, hide resolution, save, phone, clue dan ending belum selesai. Tugas berikutnya **T05**, setelah diotorisasi.

Terra 6.1 menangani integrasi dan tes; Luna 6.0 menangani wiring tombol checkpoint. Agen utama mengerjakan aset Blender, review, pemasangan Unity, tes, capture, build dan commit lokal. Perubahan pengguna pada `ProjectSettings.asset` dan `UnityConnectSettings.asset` tetap dipertahankan serta dikecualikan dari commit. Tidak ada push. Hasil kerja dibiarkan terlihat di Blender dan Unity.
