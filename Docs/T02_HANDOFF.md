# T02 Kontrol first person dan environment uji Blender

Tanggal: 6 November 2026. Status: implementasi dan validasi T02 selesai. Acuan: GDD v1.1, CODEX_TASKS T02 dan `design.md`, khususnya skala, kamera, input, pipeline Blender dan keterbacaan.

## Cara mencoba

Di Unity, buka `Assets/_NoRainbowAfter/Scenes/FirstPersonTest.unity`, tekan Play lalu klik Game View. Alternatif dari menu: Mulai Baru -> Coba Kontrol. Scene Blender yang dipakai adalah `NRA_T02_ControllerLab` dalam `BlenderSources/T02/ControllerLab.blend`.

| Input | Perilaku |
| --- | --- |
| WASD | Jalan, diagonal dinormalisasi |
| Mouse | Melihat dari kamera first person, pitch dibatasi ±85° |
| Left Shift | Tahan untuk sprint, kapasitas awal 5 detik |
| Left Ctrl | Toggle crouch; tidak bisa berdiri saat ruang kepala terhalang |
| Esc | Pause/resume; cursor terbuka ketika pause |
| Pause -> slider | Ubah sensitivitas dan FOV, tersimpan lintas scene |
| Pause -> Kembali ke Menu | Keluar dari lab dengan waktu dipulihkan ke 1 |

Input baru mengikuti Left Ctrl dalam desain; binding C di template `Assets/InputSystem_Actions.inputactions` tidak diubah. Headbob tidak ditambahkan. Kamera ada pada player, tanpa third-person offset atau tubuh/animasi karakter baru.

Sesuai instruksi pengguna, scene dan viewport yang sedang dikerjakan dibiarkan terlihat; jangan mengembalikan Blender/Unity ke scene awal setelah bekerja. Semua mesh environment uji dibuat melalui Blender. Jangan mengganti pembuatan model 3D ke jalur lain tanpa memberi tahu pengguna jika Blender tidak dapat diakses.

## Implementasi dan tuning

`PFB_Raka_FirstPerson.prefab` memakai CharacterController. Script player berada dalam assembly `NoRainbowAfter.Player` agar dapat diuji tanpa ketergantungan pada menu. Input action asset disalin per player saat runtime agar enable/disable input tidak memodifikasi aset sumber. Map Global/Pause tetap aktif ketika map Player dinonaktifkan.

| Setting pada FirstPersonMotor | Default |
| --- | --- |
| Walk / sprint / crouch speed | 2.4 / 3.4 / 1.2 m/s |
| Acceleration / gravity | 16 m/s² / -20 m/s² |
| Standing / crouching capsule | 1.7 / 1.1 m |
| Camera standing / crouching | 1.58 / 0.95 m lokal dari kaki player |
| Capsule radius / skin / step | 0.28 / 0.025 / 0.25 m |
| FOV / sensitivitas | 75° / 0.08 derajat per pixel |
| Batas FOV / sensitivitas | 65–95° / 0.01–0.3 |
| Sprint / delay / recharge | 5 / 1 / 4 detik |

Sprint hanya mengonsumsi stamina ketika benar-benar berpindah secara horizontal. Saat habis, sprint menunggu recharge penuh atau tombol Shift dilepas setelah minimal 25% terisi, sehingga tidak tersendat dengan sprint berulang setiap frame. HUD hanya menunjukkan indikator stamina tipis ketika berkurang; ini bukan health bar atau skor kondisi mental.

Sensitivitas dan FOV yang tersimpan menimpa default prefab. Ubah melalui menu pause untuk menguji; key PlayerPrefs: `NRA.MouseSensitivity` dan `NRA.FieldOfView`. Kedua nilai dikembalikan ke default desain setelah pengujian.

## Aset Blender dan import

- Source: `BlenderSources/T02/ControllerLab.blend`.
- Script pembuatan yang dapat diaudit: `BlenderSources/T02/create_controller_lab.py`.
- Export: `Assets/_NoRainbowAfter/Art/Models/Environment/T02_ControllerLab.fbx`.
- Model dibuat di Blender 5.2.3 LTS melalui bridge aktif, dalam satu scene/collection baru. Scene asli Blender dipertahankan. Source disimpan sebagai copy, bukan menimpa file pengguna.
- 256 mesh, 3.392 triangle setelah modifier. Floor dasar 12 × 18 m, wall 2.7 m, atap merunduk 1.25 m clear, ramp naik 0.6 m selama 3 m (~11.31°), empat anak tangga 0.15 m, pintu 0.90 × 2.05 m clear.
- Ini environment khusus menguji kontrol, bukan kamar kos/Chapter 1. Dimensi kamar kos 4 × 5 m dari desain tetap untuk T04.
- FBX `-Z Forward / Y Up`, unit meter. Pengukuran Unity menemukan orientasi X/Z terbalik; assembly `T02_BlenderLab` diputar Y 180° agar posisi jalur cocok. Tidak menggunakan negative scale. Bounds wall/floor sekitar 12.2 × 2.86 × 18.2 m termasuk ketebalan dan fixture.
- Collider hanya pada struktur, tunnel, ramp, tangga dan pintu. Ramp memakai MeshCollider statis, struktur kotak memakai BoxCollider. Tile/strip/lampu dekoratif tidak mendapat collider.
- Lima material Unity URP Lit asli dibuat untuk greybox dengan warna concrete, floor, trim, teal dan amber. Belum ada tekstur PBR hero final; pekerjaan tersebut tetap mengikuti T07/T14 dan `design.md`.

## File utama yang berubah

- `Scripts/Player/FirstPersonMotor.cs`, `FirstPersonInputSource.cs`, `FirstPersonPlayer.cs`, `NoRainbowAfter.Player.asmdef`.
- `Scripts/UI/FirstPersonSession.cs`: pause, pengaturan view, stamina HUD dan kembali ke menu.
- `Settings/NRA_FirstPersonActions.inputactions` dan `Art/Prefabs/PFB_Raka_FirstPerson.prefab`.
- `Scenes/FirstPersonTest.unity`: lab, satu kamera FPP, lighting, HUD dan pause. Scene ditambahkan ke Build Settings tanpa mengubah urutan scene sebelumnya.
- `Scenes/MasterGameplay.unity` dan `Scripts/UI/ShellScreen.cs`: tombol Coba Kontrol.
- `Scripts/Core/SceneFlow.cs`: ID FirstPersonTest ditambahkan di akhir enum; ID lama tetap.
- `Scripts/Editor/FirstPersonTestBuilder.cs`: pembuatan aset eksplisit dengan guard overwrite/dirty scene dan import action asset sinkron sebelum menyimpan referensi prefab.
- `Scripts/Editor/FoundationBuilder.cs`: generator T01 tetap terbatas pada empat scene fondasi.
- `.gitignore`: abaikan backup `.blend1`/`.blend2`.
- `ProjectSettings/URPProjectSettings.asset`: Unity menambahkan metadata default `m_ProjectSettingFolderPath: URPDefaultResources`. Tidak ada pergantian pipeline atau package upgrade.

Semua path script/aset di atas relatif terhadap `Assets/_NoRainbowAfter/`, kecuali yang secara eksplisit diawali BlenderSources, ProjectSettings atau Docs.

## Validasi

**12/12 PlayMode tests lulus**, 0 gagal, 0 skipped. Job terakhir: `113aef5d390044108136a894d14489b7`. [Hasil lengkap](QA/T02_PlayMode_results.json).

Tes memakai InputTestFixture untuk W/A/S/D, mouse delta, Shift, Left Ctrl dan Escape. Tes motor memeriksa collision dinding/lantai, normalisasi diagonal, sprint 5 detik, recharge, idle sprint, ceiling clearance, pitch/FOV dan kamera tanpa headbob. Tes integrasi memuat scene Blender yang benar-benar diimpor, lalu menelusuri tunnel, ramp, tangga dan pintu.

Smoke test uGUI/MCP juga menjalankan MainMenu -> MasterGameplay -> Coba Kontrol -> FirstPersonTest -> pause -> MainMenu -> masuk lagi. Saat pause: timeScale 0, cursor None dan input gerak dinonaktifkan. Resume mengembalikan timeScale 1, input gerak dan cursor Locked. FOV 86° / sensitivitas 0.11 bertahan saat reload scene; kemudian dikembalikan ke 75° / 0.08.

Unity Console setelah validasi: 0 warning dan 0 error. Missing script/reference diperiksa sebelum handoff. Masalah awal referensi input asset yang belum selesai diimpor sudah diperbaiki pada prefab dan builder; uji integrasi terakhir memverifikasi hasil perbaikannya.

Belum menjalankan build Windows atau profiling standalone; tidak membuat klaim FPS. Pengujian input menggunakan device sintetis InputTestFixture dan event UI, ditambah pemeriksaan state/runtime Unity. Pengguna tetap dapat melakukan playtest rasa gerak dengan keyboard/mouse fisik melalui Game View yang fokus.

## Bukti visual dan berikutnya

- [Blender viewport](QA/T02_Blender.png)
- [FPP Game View 1080p](QA/T02_FPP_1080p.png)
- [Pause Game View 1080p](QA/T02_Pause_1080p.png)

MainMenu imagegen tidak diubah. Chapter 1, item/interaksi, monster dan ending belum dibuat pada T02. Berikutnya **T03: raycast interaksi 2.3 m dengan occlusion dan overlay inspect**, lalu T04 greybox kos sesuai desain.
