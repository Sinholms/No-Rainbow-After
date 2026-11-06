# Revisi environment kos dan pandangan melingkar

Revisi ini menanggapi permintaan pengguna agar tampilan in-game lebih meyakinkan dan menyeramkan, dengan pinggir pandangan gelap. Ini merupakan art pass yang dimajukan atas permintaan pengguna; tidak mengklaim seluruh T03–T07, narasi Chapter 1, atau game lengkap selesai.

## Perilaku yang sekarang terlihat

Mulai Baru pada menu langsung memuat MasterGameplay: kamar kos 4 × 5 m, plafon sekitar 2.7 m, pintu sekitar 0.9 × 2.05 m, dan lorong 14 m dengan lebar sekitar 1.8 m serta dua ceruk samping. Komposisi, objek mahasiswa, warna, material basah dan keterbacaan mengikuti `Docs/design.md`. Lab kotak T02 tetap menjadi scene debug terpisah dan tidak dipakai sebagai environment utama.

Aset mencakup kasur dengan selimut berlipat dan bantal, frame tempat tidur tubular, meja/drawer, kursi melengkung, buku/catatan kerja, lampu meja, kipas dengan kisi, jendela berjeruji, sepatu, tas, payung, kabel, pintu berpola panel, lemari utilitas dan pipa lorong. Seluruh mesh dibuat di Blender. Tidak menggunakan model franchise atau aset unduhan yang tidak ditinjau.

Cahaya hangat meja, cahaya dingin jendela, titik lampu di lorong, bayangan sorot kamera, reflection probe gelap, tonemapping ACES dan grain ringan memberi suasana horror. Audio hujan 12 detik merupakan sintesis asli dari generator aset. Tidak menambahkan flicker/flashes, motion blur atau monster.

POV memakai masker hitam melingkar dengan tepi lembut dan koreksi aspect ratio. Default radius 0.50 kali tinggi layar, softness 0.11; UI menyebutnya Lebar pandang 50%. Toggle dan slider tersedia saat pause, disimpan pada `NRA.CircularViewEnabled` dan `NRA.CircularViewRadius`. Masker dimatikan saat pause dan berada di bawah HUD agar teks/opsi tetap terbaca. Sorot kamera juga mengikuti arah pandang.

Referensi Pacify ditinjau pada [halaman resmi Hitchcock Games](https://hitchcockgames.com/pacify/). Efek lingkaran dibuat berdasarkan permintaan pengguna; tidak mengklaim bahwa ini replika persis efek Pacify. Kamera tetap first person dan headbob tetap nonaktif.

## Pembagian kerja dan file utama

Sesuai instruksi pengguna, GPT-6.1 Terra menulis kode; agen utama menangani aset Blender, pemilihan tekstur, pemeriksaan kode, instalasi scene, pengujian dan capture. Untuk pekerjaan berikutnya pertahankan delegasi kode ke Terra/Luna, gunakan `design.md`, dan biarkan scene/viewport kerja tetap terlihat di Blender/Unity.

- Source Blender: `BlenderSources/Environment/KosAtmosphere_v1.blend`, generator `create_kos_atmosphere.py`, manifest `KosAtmosphere_v1_manifest.json`.
- Export: `Assets/_NoRainbowAfter/Art/Models/Environment/ENV_KosAtmosphere_v1.fbx`.
- Model final: 226 mesh dan 63.128 triangle. Ekspor meter, -Z Forward/Y Up, assembly Unity diputar Y 180° sesuai pengukuran pipeline sebelumnya. Physics struktur memakai collider box/mesh yang tercantum per bagian dalam manifest.
- Tekstur: `Art/Textures/KosAtmosphere/`, enam set base color/normal/mask. Wall dan Wood base color dibuat dengan built-in imagegen; prompt lengkap di `Docs/Art/Kos_Wall_Albedo_Prompt.txt` dan `Kos_Wood_Albedo_Prompt.txt`. Peta lainnya dibuat secara prosedural di Blender; normal/roughness adalah pendekatan material, bukan scan PBR yang terkalibrasi. Mask URP memakai R metallic dan A smoothness, dengan smoothness multiplier 1 pada material bertekstur.
- Material dan volume: `Art/Materials/KosAtmosphere/`.
- `Scripts/UI/CircularViewMask.cs` dan `Art/Shaders/CircularViewMask.shader`: mask UI, material runtime yang dilepas ketika scene ditutup, tanpa raycast.
- `Scripts/UI/FirstPersonSession.cs`: kontrol pause dan preferensi pandangan opsional; kompatibel dengan lab lama yang tidak memiliki field mask.
- `Scripts/Editor/KosAtmosphereInstaller.cs`: instalasi eksplisit berbasis manifest, pengaman scene dirty, satu player/camera/UI dan material yang dapat dipasang ulang. Scene lama dilindungi oleh commit baseline sebelum revisi.
- `Scenes/MasterGameplay.unity`: scene yang diperbarui, GUID tetap. MainMenu dan source kontrol pemain tidak diubah.
- `Audio/Ambience/AUD_KosRain.wav`: loop asli, tanpa lagu/rekaman dari game lain.

Path relatif script/aset di atas memakai prefix `Assets/_NoRainbowAfter/` kecuali dinyatakan lain. Source .blend mengemas tekstur agar dapat dibuka kembali. Imagegen originals tetap disimpan dan file yang dipakai sudah berada di workspace.

## Verifikasi

**14/14 PlayMode tests lulus**, termasuk seluruh tes T02, scene kos memiliki satu kamera dan player aktif, tidak membawa geometry lab, lintasan dari spawn melewati pintu hingga lorong, serta pause/resume yang menutup/memulihkan mask. [Hasil lengkap](QA/KosAtmosphere_PlayMode_results.json).

Shader mask didukung dan daftar pesan kompilasi shader kosong. Toggle serta slider diuji lewat uGUI dan nilai preferensi terbaca kembali. Default kode adalah 0.50; nilai aktif mengikuti pengaturan pengguna yang tersimpan. Capture Game View dibuat pada 1920 × 1080, termasuk view kamar dan kasur dari POV pemain. Ini capture Editor, bukan bukti performa standalone.

Build Windows x64 Mono **Succeeded**, 0 error / 0 warning, 111.61 MB, job `build-64dc4280a3`. Output: `Builds/KosVisualPreview/NoRainbowAfter.exe`; folder build diabaikan Git. Build mencakup Boot, MainMenu, MasterGameplay dan Credits. Build belum dijalankan untuk full playthrough atau profiling standalone; tidak membuat klaim FPS atau optimisasi penuh. Scene debug T02 tetap utuh. Package dan pipeline tidak di-upgrade. Unity menulis metadata serialisasi/build URP serta preload Input System saat build; nilai render quality pokok tetap memakai preset PC yang terpasang.

Pemeriksaan akhir: Unity Console 0 warning/error, satu scene gameplay terbuka, satu kamera dan tidak ada missing serialized reference. Scene kerja tetap terlihat di Blender dan Unity. Perubahan `UnityConnectSettings.asset` yang muncul dari sesi Editor tidak dimasukkan ke commit visual ini agar pengaturan layanan pengguna tidak tercampur.

Bukti: [kamar dengan mask lingkaran](QA/Kos_Circular_1080p.png), [kasur dari POV pemain](QA/Kos_Bed_1080p.png), [viewport Blender](QA/Kos_Blender.png). Prompt imagegen disertakan untuk kedua material albedo.

## Cara mencoba dan batas pekerjaan

Buka Boot atau MasterGameplay lalu Play. Gunakan mouse/WASD; Esc membuka pause untuk pengaturan pandangan. Scene kos tetap terbuka di Blender dan Unity agar pengguna dapat mengikuti pekerjaan. Tidak ada cerita, clue/quest, monster, combat atau inspect yang dinyatakan selesai pada art pass ini.

Setelah validasi dan commit lokal, hentikan pengeditan dan masuk ke diskusi dengan pengguna. Jangan otomatis memulai tugas berikutnya.
