# No Rainbow After

Proyek game horor psikologis first person untuk Windows x64.

Buka dengan Unity **6000.6.8f1**. Pipeline yang saat ini terpasang adalah **URP 17.6.0**; target HDRP dalam GDD masih memerlukan keputusan terpisah.

Untuk menjalankan proyek, buka `Assets/_NoRainbowAfter/Scenes/Boot.unity` lalu tekan Play. Mulai Baru kini langsung masuk ke kamar kos dan lorong 3D dalam MasterGameplay. WASD/mouse, Shift sprint, Ctrl crouch, Esc pause; environment dibuat di Blender mengikuti `Docs/design.md`.
Efek pandangan melingkar aktif secara default. Ubah Lebar pandang atau matikan Tepi pandangan melingkar melalui menu pause. Debug lab T02 tetap tersedia di `Assets/_NoRainbowAfter/Scenes/FirstPersonTest.unity`.

Chapter 1 sudah memiliki objective, inspeksi dokumen, perubahan kamar, perpindahan ke koridor horor berkelok, monster pengejar, lima lemari interaktif, dan lampu plafon redup dengan percikan listrik pada setiap perubahan nyala/mati. Tekan E untuk berinteraksi dan bersembunyi. Tangkapan pertama memberi kesempatan kabur; tangkapan berikutnya mengembalikan pemain ke checkpoint. Kelanjutan Chapter 2, prolog sinematik, dan ending masih mengikuti rencana produksi.

## Membuka proyek setelah clone

1. Pasang Git dan Unity Editor **6000.6.8f1** melalui Unity Hub (Windows Build Support untuk membuat build Windows).
2. Jalankan `git clone https://github.com/Sinholms/No-Rainbow-After.git`.
3. Tambahkan folder hasil clone sebagai proyek di Unity Hub. Tunggu pemulihan package dan impor aset pertama; proses ini membutuhkan internet. Git diperlukan untuk dependency Unity MCP yang tercatat di `Packages/manifest.json`.
4. Buka `Assets/_NoRainbowAfter/Scenes/Boot.unity`, tekan Play, lalu pilih Mulai Baru.

`Assets` beserta `.meta`, `Packages`, `ProjectSettings`, `BlenderSources`, dan `Docs` disimpan dalam Git. Folder `Library`, `Temp`, `Logs`, `UserSettings`, build, backup, dan scene pemulihan otomatis tidak disertakan. Unity membuat ulang cache saat impor pertama. Sumber Blender tersedia untuk pengeditan; aset FBX yang dipakai game sudah disertakan, sehingga Blender tidak diperlukan untuk sekadar menjalankan game.

## Kolaborasi tim

Branch utama adalah **main**. Ambil pembaruan sebelum mulai bekerja dan gunakan branch terpisah untuk setiap pekerjaan, misalnya `orang1/ui-ponsel`, `orang2/chapter2`, atau `orang3/environment`. Gabungkan perubahan melalui pull request setelah diperiksa. Sepakati satu editor untuk `MasterGameplay.unity` pada satu waktu; gunakan prefab atau scene kerja terpisah untuk pekerjaan anggota lain. Sertakan `.meta` ketika menambah, memindahkan, atau menghapus aset Unity.

## Dokumentasi dan rencana kerja

- [Spesifikasi cerita](Docs/GDD_No_Rainbow_After_v1_1_REFERENCE.md)
- [Urutan tugas T00–T16](Docs/CODEX_TASKS_No_Rainbow_After_v1_1.md)
- [Desain dunia dan aset](Docs/design.md)
- [Hasil T01 dan cara menguji](Docs/T01_HANDOFF.md)
- [Menu sinematik imagegen dan hasil pengujian](Docs/MENU_ART_HANDOFF.md)
- [T02 kontrol first person dan aset Blender](Docs/T02_HANDOFF.md)
- [Revisi environment horror dan POV melingkar](Docs/KOS_ATMOSPHERE_HANDOFF.md)
- [Pembagian tugas tim dan roadmap](PEMBAGIAN_TUGAS_KERJA.md)
- [Rencana cutscene prolog](Docs/PROLOGUE_CUTSCENE.md)
- [Panduan tugas Codex](Docs/CODEX_MASTER_PROMPT.md)
- [Interaksi dan inspeksi T03](Docs/T03_HANDOFF.md)
- [Level dan lingkungan T04](Docs/T04_HANDOFF.md)
- [Narasi dan objective T05](Docs/T05_HANDOFF.md)
- [Monster T06](Docs/T06_HANDOFF.md)
- [Visual dan kualitas grafis T07](Docs/T07_HANDOFF.md)
- [Chapter 1 terbaru, perubahan koridor dan lampu](Docs/CHAPTER1_EXPANSION_HANDOFF.md)
- [Aset Raka dan rencana integrasinya](Docs/RAKA_GENERATION_HANDOFF.md)
- [Sumber, atribusi, dan catatan lisensi aset](Docs/ASSET_LICENSES.md)

Laporan pengujian dan screenshot tersedia di `Docs/QA/`. Pengujian integrasi terbaru ada di `Docs/QA/C1Lighting_Tests.json` (10 lulus) dan laporan build Windows di `Docs/QA/C1Lighting_Build.json`. Binary build tidak disimpan dalam repository. Aset Raka final tersedia sebagai prefab terpisah; visual encounter saat ini masih memakai placeholder sesuai catatan handoff.

Jangan memasukkan Library, Temp, Logs, atau UserSettings ke Git. Versioning aset Unity harus selalu menyertakan file `.meta`.
