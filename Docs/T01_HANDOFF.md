# T01 Bootstrap project scene dan Git

Tanggal: 6 November 2026. Status: acceptance alur scene lulus di Editor Play Mode.

## Hasil

Unity 6000.6.8f1; target StandaloneWindows64; URP 17.6.0 yang sudah terpasang. UI memakai uGUI 2.6.0 dengan InputSystemUIInputModule dan Input System 1.20.1. Tidak ada paket baru atau perubahan pipeline.

Scene Boot otomatis menuju MainMenu. Menu Bahasa Indonesia menyediakan Mulai Baru, Lanjutkan, Pengaturan, Kredit, dan Keluar. Lanjutkan nonaktif karena sistem checkpoint/save belum dibuat. Pengaturan volume utama mengubah AudioListener.volume dan disimpan di PlayerPrefs dengan key NRA.MasterVolume. Pengaturan bus audio dan aksesibilitas lanjutan tetap menjadi pekerjaan T15.

MasterGameplay adalah placeholder navigasi. Root prolog, tiga chapter, dan ending disediakan dalam keadaan nonaktif. Tombol Lihat Kredit mengarah ke Credits; kedua scene menyediakan jalan kembali ke menu. Belum ada gameplay/narasi, controller, atau ending yang diklaim selesai.

SceneFlow bertahan lintas scene, menolak permintaan bersamaan, memeriksa scene tersedia di Build Settings, dan memulihkan interaksi setelah load. FoundationBuilder hanya dijalankan secara eksplisit dan menolak overwrite scene yang sudah ada.

## File utama

- `.gitignore`, `.gitattributes`: abaikan cache Unity, IDE, build, dan Captures; pertahankan aset beserta metadata Unity.
- `Assets/_NoRainbowAfter/Scripts/Core/SceneFlow.cs`: layanan scene dengan enum dan path tetap.
- `Assets/_NoRainbowAfter/Scripts/Core/BootEntry.cs`: Boot ke MainMenu.
- `Assets/_NoRainbowAfter/Scripts/UI/ShellScreen.cs`: event tombol, pengaturan, fokus, dan status load.
- `Assets/_NoRainbowAfter/Scripts/Editor/FoundationBuilder.cs`: pembuatan awal folder dan scene, tanpa menjalankan otomatis saat import.
- `Assets/_NoRainbowAfter/Scenes/Boot.unity`, `MainMenu.unity`, `MasterGameplay.unity`, `Credits.unity` beserta `.meta`.
- `Assets/_NoRainbowAfter/`: folder Art, Audio, Data, Scripts, UI, VFX, Settings, ThirdParty, Documentation; folder kosong memakai `.gitkeep`.
- `ProjectSettings/EditorBuildSettings.asset`: empat scene baru di depan; SampleScene tetap tersedia sesudahnya.

Folder mengikuti GDD §8.2; dokumen kanonis tetap berada di `Docs/`. Scene berada langsung di `Scenes/` agar path sederhana. Folder `Documentation/` disiapkan untuk handoff lanjutan.

## Cara mencoba

1. Buka `Assets/_NoRainbowAfter/Scenes/Boot.unity` lalu tekan Play. Boot masuk ke MainMenu.
2. Pilih Pengaturan, ubah volume, lalu Kembali. Buka lagi untuk memeriksa nilainya. Tombol menu di belakang panel tidak menerima navigasi ketika panel terbuka.
3. Mulai Baru membuka MasterGameplay. Pilih Lihat Kredit, lalu Kembali ke Menu.
4. Kredit pada menu langsung membuka Credits. Lanjutkan tetap nonaktif. Keluar pada Editor menampilkan keterangan; pada player memanggil Application.Quit.
5. Esc disediakan untuk menutup Pengaturan, atau kembali ke menu dari placeholder gameplay/Credits. Pengujian keyboard fisik masih perlu dilakukan di Game View yang fokus.

Jangan menjalankan menu `No Rainbow After/Create T01 Foundation` pada scene yang sudah dibuat; generator sengaja membatalkan operasi agar tidak menimpa aset.

## Bukti validasi

Verifikasi dilakukan melalui Unity MCP terhadap Editor proyek ini:

| Pemeriksaan | Hasil |
| --- | --- |
| Import dan kompilasi empat script | Lulus; tipe runtime/editor berhasil dimuat |
| Boot -> MainMenu | Lulus melalui Start pada BootEntry |
| MainMenu -> MasterGameplay -> Credits -> MainMenu | Lulus melalui event Submit tombol uGUI, bukan pemanggilan loader langsung |
| MainMenu -> Credits | Lulus melalui event Submit |
| Dua permintaan load dalam satu frame | Permintaan kedua ditolak |
| SceneFlow dan EventSystem setelah perpindahan berulang | Masing-masing satu instance |
| Continue tanpa save | Nonaktif |
| Pengaturan dan tombol Kembali | Panel terbuka/tertutup, fokus kembali, menu belakang nonaktif ketika panel terbuka |
| Volume | Perubahan terbaca pada AudioListener dan PlayerPrefs; bertahan setelah restart Play Mode; akhirnya dikembalikan ke 80% |
| Empat scene dibuka dan diperiksa | Tidak ada missing script atau serialized reference; semuanya tersimpan |
| Generator dijalankan ulang | Dibatalkan sebelum mengubah scene |
| Unity Console akhir | 0 warning dan 0 error |
| SampleScene, Packages, GraphicsSettings, QualitySettings | Tidak berubah dari commit awal |

Screenshot Game View yang sudah diperiksa secara visual berada di `Captures/T01/MainMenu-1.png`, `Settings.png`, dan `Credits.png`. Resolusi capture Editor sekitar 945–961 × 613; bukan bukti 1080p standalone atau hasil profiling. `MainMenu.png` adalah capture awal sebelum transisi selesai; jangan gunakan sebagai bukti menu.

Tidak menjalankan NUnit atau build Windows pada T01. Pengujian Escape dengan injeksi input dari MCP pada Editor yang tidak fokus belum berhasil menghasilkan key press; fungsi Escape belum dinyatakan lulus uji keyboard fisik. Beberapa probe API/input gagal di tool dan sudah dihentikan; tidak menghasilkan file produksi atau error pada Console proyek. Perubahan sementara untuk pengujian background input dikembalikan ke ResetAndDisableNonBackgroundDevices sebelum keluar Play Mode.

## Source control dan batas lanjutan

Repository Git lokal dibuat. Commit `11ac941` menyimpan kondisi awal sebelum perubahan scene. Commit lokal menggunakan identitas agen `Codex <codex@local.invalid>` karena identitas Git pengguna belum dikonfigurasi. Tidak ada remote atau push.

Perbedaan dari target GDD masih terbuka: proyek berasal dari template URP, sedangkan target visual dokumen adalah HDRP. T01 menggunakan pipeline yang sudah terpasang; keputusan migrasi/fallback dan benchmark belum dibuat. Koneksi Blender bisa membaca scene, tetapi bridge melaporkan add-on belum sinkron; tidak ada aset Blender dihasilkan pada T01.

Berikutnya T02: CharacterController first person, Input System, sprint/stamina, crouch, pause, FOV/sensitivitas, headbob nonaktif, dan lingkungan uji terpisah. Jangan mulai pembangunan Chapter 1 sebelum kontrol dasar lolos.
