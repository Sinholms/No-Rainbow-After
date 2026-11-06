# Main menu sinematik dengan latar imagegen

Tanggal: 6 November 2026. Perubahan visual lanjutan T01 atas permintaan pengguna; tidak memulai T02 atau mengklaim vertical slice T07 selesai.

## Hasil dan batas aset

MainMenu menggunakan latar penuh berupa ilustrasi imagegen orisinal: kos lembap, pintu kosong, kursi belajar, kertas, genangan dan kabut biru. Komposisi menu sederhana di kiri serta objek dominan di kanan mengikuti arah referensi screenshot pengguna. Tidak menggunakan gambar/aset Silent Hill sebagai tekstur produksi.

Latar ini adalah gambar 2D, bukan mesh atau lingkungan 3D yang dapat dijelajahi. Unity menambahkan hujan dan kabut prosedural bergerak serta drift gambar yang sangat kecil; tidak ada monster atau narasi baru. Tidak ada model 3D baru dalam perubahan ini.

Instruksi pengguna untuk produksi berikutnya: semua model 3D harus dibuat lewat Blender; jika Blender tidak dapat digunakan, beri tahu pengguna sebelum mengganti jalur pengerjaan. Blender MCP telah diverifikasi melalui status, scene_info dan eksekusi Python read-only: Blender 5.2.3 LTS dapat diakses dan operator ekspor FBX tersedia. Bridge masih melaporkan metadata add-on tidak sinkron. Tidak ada file .blend atau FBX yang diklaim dibuat untuk menu ini.

## File dan sumber

- `Assets/_NoRainbowAfter/Art/Textures/Menu/T_Menu_KosRain_v1.png`: hasil built-in imagegen, 1672 × 941. Disalin ke proyek dari keluaran generasi; sumber asli tetap disimpan. SHA256: `CFE9C355FA993F0D668824A877E74D086BEF6A883E71FEEC725B77C0CD745C8B`.
- `Docs/Art/MenuBackground_Prompt.txt`: prompt lengkap yang digunakan. Mode built-in, bukan CLI/API fallback.
- `Assets/_NoRainbowAfter/UI/Fonts/CrimsonText/CrimsonText-Regular.ttf`: Crimson Text Regular, dari [repositori Google Fonts](https://github.com/google/fonts/blob/main/ofl/crimsontext/CrimsonText-Regular.ttf). Copyright 2010 The Crimson Text Project Authors. Lisensi SIL OFL 1.1 disertakan pada `OFL.txt`. SHA256 font: `48E6C5D5AD1D01599D374ECB817E15890D1FEB3B8A3A88E527D44C90389E1F06`.
- `Assets/_NoRainbowAfter/Art/MenuAtmosphere.shader` dan `Art/Materials/MAT_MenuAtmosphere.mat`: shader/material uGUI untuk hujan dan kabut ringan, tidak menerima raycast.
- `Assets/_NoRainbowAfter/Scripts/UI/MenuAtmosphere.cs`: aspect-fill, crop proporsional, overscan dan animasi unscaled time. Material runtime dibuat per instance lalu dilepas ketika scene ditutup.
- `Assets/_NoRainbowAfter/Scripts/UI/MenuItemFocus.cs`: penanda fokus/hover untuk menu teks.
- `Assets/_NoRainbowAfter/Scripts/Editor/MenuArtPass.cs`: perubahan visual eksplisit pada MainMenu dengan pengaman scene dirty, import texture tanpa rescale power-of-two, dan referensi aksi yang tetap dipakai.
- `Assets/_NoRainbowAfter/Scripts/UI/ShellScreen.cs`: ketika Settings terbuka, sembunyikan dan blokir menu dasar; kembalikan tampilan dan fokus saat panel ditutup.
- `Assets/_NoRainbowAfter/Scripts/Editor/FoundationBuilder.cs`: Settings dibuat sebagai sibling ScreenContent agar panel tidak ikut tersembunyi.
- `Assets/_NoRainbowAfter/Scenes/MainMenu.unity`: scene yang diperbarui, dengan GUID tetap.

Font dan ilustrasi merupakan aset yang masuk proyek; tidak ada paket Unity baru, perubahan pipeline, model impor, atau audio baru.

## Validasi aktual

Unity 6000.6.8f1, URP 17.6.0. Kompilasi script berhasil; shader supported dan daftar pesan kompilasi shader kosong. Unity Console akhir: 0 warning dan 0 error.

Pemeriksaan Play Mode melalui Unity MCP:

- Screenshot Game View 1920 × 1080 diperiksa visual untuk MainMenu dan Settings.
- Pada 1366 × 768 dan viewport lebar 945 × 420, Title, lima tombol, hint dan footer berada dalam viewport. Aspect gambar tetap dipertahankan; tidak dipaksa menjadi tekstur 2048 × 1024.
- Raycast uGUI pada pusat setiap tombol mengenai tombol yang benar. Continue tetap nonaktif.
- Event navigasi Down dari Mulai Baru melewati Continue dan memilih Pengaturan.
- Event pointer membuka Settings. Menu dasar memiliki alpha 0 dan tidak memblokir input; fokus berpindah ke slider. Event Submit pada Kembali memulihkan menu dan fokus.
- Nilai waktu shader bergerak dan gambar mengalami drift kecil; tidak ada flash/flicker yang ditambahkan.
- Event tombol menjalankan MainMenu -> MasterGameplay -> Credits -> MainMenu. Permintaan scene kedua saat loading ditolak. Setelah kembali terdapat tepat satu SceneFlow, EventSystem dan MenuAtmosphere, tanpa missing script.

Tidak menjalankan NUnit atau build Windows/standalone pada perubahan ini. Tidak membuat klaim FPS atau profiling GPU. Tes keyboard fisik/Esc belum diulang; verifikasi navigasi di atas menggunakan event uGUI. Preset Game View sementara untuk pengujian dikembalikan ke preset awal setelah capture.

## Bukti dan cara membuka

- [MainMenu pada Game View](Art/MainMenu_GameView_1920x1080.png)
- [Settings pada Game View](Art/Settings_GameView_1920x1080.png)

Buka `Assets/_NoRainbowAfter/Scenes/MainMenu.unity` lalu Play, atau buka Boot untuk alur awal. Hujan dan drift terlihat saat Play Mode. `No Rainbow After/Apply Cinematic Menu Art` dapat diterapkan ulang secara eksplisit setelah menyimpan scene terbuka; aksi ini sengaja memperbarui tampilan MainMenu saja.

Scene Boot, MasterGameplay, Credits, SampleScene, logika SceneFlow, paket dan render settings tetap pada kondisi sebelumnya. Implementasi FPP masih merupakan tugas berikutnya, T02.
