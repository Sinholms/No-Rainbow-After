# No Rainbow After

Proyek game horor psikologis first person untuk Windows x64.

Buka dengan Unity **6000.6.8f1**. Pipeline yang saat ini terpasang adalah **URP 17.6.0**; target HDRP dalam GDD masih memerlukan keputusan terpisah.

Untuk menjalankan proyek, buka `Assets/_NoRainbowAfter/Scenes/Boot.unity` lalu tekan Play. Mulai Baru kini langsung masuk ke kamar kos dan lorong 3D dalam MasterGameplay. Kontrol FPP: `WASD`/mouse untuk bergerak dan melihat (termasuk tubuh fisik FPP Raka berjari-5 ala *MiSide* yang tampak saat menunduk), `Shift` sprint, `Ctrl` crouch, `E` berinteraksi, `Tab` minigame atur napas untuk menurunkan stres (atau `1x klik Tab` untuk keluar dari laptop), `F` menutup layar inspeksi dokumen (slip lembur, buku, pintu), `Enter` mengirim ketikan tugas pada laptop (otomatis keluar setelah 5 kalimat selesai), dan `Esc` untuk Pause; environment dan prop dibuat di Blender mengikuti `Docs/design.md`.
Efek pandangan melingkar aktif secara default. Ubah Lebar pandang atau matikan Tepi pandangan melingkar melalui menu pause. Debug lab T02 tetap tersedia di `Assets/_NoRainbowAfter/Scenes/FirstPersonTest.unity`.

Chapter 1 sudah memiliki objektif **"Mengerjakan tugas"**, **Barometer Stres berwarna merah di pojok kiri atas** (minimal `75%` di Chapter 1 dan `45%` di Chapter 2, dengan stres awal `50%` di Chapter 2; layar memerah + peringatan berkedip pelan saat mencapai `>= 70%`), **minigame mengetik tugas kuliah ala Monkeytype** (`5` kalimat, huruf ketikan kuning, timer `60–30 detik`, kirim dengan `Enter`, keluar dengan `1x klik Tab`, otomatis keluar setelah 5 kalimat selesai) di laptop meja belajar (`PROP_RakaLaptop`), **kenaikan stres berkala di lorong horor** yang mendorong Raka bersembunyi di lemari (`E`) lalu menekan **`[Tab]` atur napas di bar hijau**, sistem **tertangkap Shadow** (pelarian pertama masih memberi kesempatan kabur; tertangkap berikutnya menaikkan stres ke 100%), serta pintu keluar ujung lorong yang memulangkan Raka ke kos. Stres 100% di lorong horor **mengulang Chapter 1 dari kamar kos**. Di kamar sendiri, stres 100% membuat Raka berjongkok dan terkunci sampai menggunakan `Tab` untuk mengatur napas.

**Chapter 2 Bizarre Edition** dimulai saat Raka kembali ke kos. Ketik esai di laptop, lalu buka dan lewati pintu di ujung lorong saat diminta lembur. Di TKP, baca sorotan dua polisi dan berlindung menuju Halte Pasar. Setiap kali tertangkap, stres bertambah 10% dan Raka kembali ke checkpoint; saat mencapai 100%, Chapter 2 diulang dari kamar kos. Tekan **E** di pintu bus untuk naik.

Untuk tes per bab, buka satu scene dari folder `Assets/_NoRainbowAfter/Scenes` dan tekan **Play**.

| Scene | Mulai dari |
| --- | --- |
| `MasterGameplay` | Alur cerita lengkap yang tersedia. Lewat **Mulai Baru** dari menu, Raka mulai dengan kilasan prolog. |
| `Chapter1` | Raka di kos pada awal bab pertama. |
| `Chapter2` | Raka bangun di kos dengan esai dan HP Ella siap dimainkan. |
| `Chapter3` | Scene kosong untuk pekerjaan Chapter 3 nanti. |

Scene tes `Chapter1` dan `Chapter2` memuat aset yang sama dari `MasterGameplay`. Clue yang dikumpulkan saat tes berlaku sampai sesi Play berakhir.

## Membuka proyek setelah clone

1. Pasang Git dan Unity Editor **6000.6.8f1** melalui Unity Hub (Windows Build Support untuk membuat build Windows).
2. Jalankan `git clone https://github.com/Sinholms/No-Rainbow-After.git`.
3. Tambahkan folder hasil clone sebagai proyek di Unity Hub. Tunggu pemulihan package dan impor aset pertama; proses ini membutuhkan internet. Git diperlukan untuk dependency Unity MCP yang tercatat di `Packages/manifest.json`.
4. Buka `Assets/_NoRainbowAfter/Scenes/Boot.unity`, tekan Play, lalu pilih Mulai Baru.

`Assets` beserta `.meta`, `Packages`, `ProjectSettings`, `BlenderSources`, dan `Docs` disimpan dalam Git. Folder `Library`, `Temp`, `Logs`, `UserSettings`, build, backup, dan scene pemulihan otomatis tidak disertakan. Unity membuat ulang cache saat impor pertama. Sumber Blender tersedia untuk pengeditan; aset FBX yang dipakai game sudah disertakan, sehingga Blender tidak diperlukan untuk sekadar menjalankan game.

## Kolaborasi tim

Branch utama adalah **main**. Ambil pembaruan sebelum mulai bekerja dan gunakan branch terpisah untuk setiap pekerjaan, misalnya `orang1/ui-ponsel`, `orang2/chapter2`, atau `orang3/environment`. Gabungkan perubahan melalui pull request setelah diperiksa. Sepakati satu editor untuk `MasterGameplay.unity` pada satu waktu; gunakan prefab atau scene kerja terpisah untuk pekerjaan anggota lain. Sertakan `.meta` ketika menambah, memindahkan, atau menghapus aset Unity.

## Dokumentasi dan rencana kerja

- [Rencana aktif Chapter 2 — Bizarre Edition](Docs/CHAPTER2_PLAN.md), termasuk pintu keluar lorong untuk Chapter 1 dan 2, esai, HP Ella, serta jalur ke halte.
- [Referensi cerita dan clue — GDD v1.1](Docs/GDD_No_Rainbow_After_v1_1_REFERENCE.md)
- [Urutan tugas T00–T16](Docs/CODEX_TASKS_No_Rainbow_After_v1_1.md)
- [Desain dunia dan aset](Docs/design.md)
- [Hasil T01 dan cara menguji](Docs/T01_HANDOFF.md)
- [Menu sinematik imagegen dan hasil pengujian](Docs/MENU_ART_HANDOFF.md)
- [T02 kontrol first person dan aset Blender](Docs/T02_HANDOFF.md)
- [Revisi environment horror dan POV melingkar](Docs/KOS_ATMOSPHERE_HANDOFF.md)
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
