# Pembagian Tugas Kerja Tim (3 Orang) — No Rainbow After

**Proyek:** No Rainbow After (First-Person Psychological Horror)<br>
**Target Platform:** PC Windows x64 • Unity 6000.6.8f1 (URP 17.6.0) • Blender 5.2.3 LTS<br>
**Acuan Utama:**
- [Rencana aktif Chapter 2 — Di Bawah Sorotan](Docs/CHAPTER2_PLAN.md)
- [GDD v1.1 Reference](Docs/GDD_No_Rainbow_After_v1_1_REFERENCE.md)
- [Urutan Tugas T00–T16](Docs/CODEX_TASKS_No_Rainbow_After_v1_1.md)
- [Desain Dunia, Karakter & Aset](Docs/design.md)
- [Konsep Cutscene Pembuka (Prolog)](Docs/PROLOGUE_CUTSCENE.md)
- [Handoff Generasi Aset Raka (T13)](Docs/RAKA_GENERATION_HANDOFF.md)

---

## 1. Ringkasan 3 Peran Utama

| Anggota | Role Utama | Fokus Tanggung Jawab | Kepemilikan Task ID |
| :--- | :--- | :--- | :--- |
| **Orang 1** | **Lead Systems, UI & Technical Programmer** | Kontrol FPP, sistem interaksi/inspeksi, UI ponsel & menu, save/checkpoint, sistem audio mixer & aksesibilitas, kualitas grafis & performa build. | `T01`, `T02`, `T03`, `T07`, `T08`, `T15`, `T16` |
| **Orang 2** | **Gameplay, Narrative & AI Programmer** | Core loop tiap chapter, state machine cerita, mini-task akademik, sistem clue, AI monster, logika 3 ending & sekuens cutscene. | `T04`, `T05`, `T06`, `T09`, `T10`, `T11`, `T12` |
| **Orang 3** | **3D Blender Artist, Level Designer & Creative Director** | Modeling & rigging Blender (environment, prop, monster Raka), tekstur/material PBR, penataan lampu/atmosfer, naskah dialog/cerita & rekaman suara/SFX. | `Prolog`, `T04` (Art), `T07` (Art), `T12` (Art/Set), `T13`, `T14` |

---

## 2. Rincian Tugas per Anggota

### A. Orang 1 — Systems, UI & Technical Programmer
Bertanggung jawab atas seluruh sistem pondasi yang menyentuh input pemain, layar antarmuka (UI), penyimpanan data, audio teknis, dan performa aplikasi.

1. **Koding (Core Systems & UI):**
   - Memelihara kontrol FPP (`FirstPersonPlayer`, `FirstPersonMotor`, `FirstPersonInputSource`), raycast interaksi 2,3 m anti-tembus dinding (`PlayerInteractor`), overlay inspeksi dokumen (`InspectOverlay`), efek pandangan melingkar (`CircularViewMask`), dan fisik tubuh FPP Raka ala *MiSide* (`FirstPersonBodyVisual` dengan model `ACT_Raka_StudentBody`).
   - **Sistem Input & Penutupan Overlay (`[F]` & `1x [Tab]`):** Memisahkan fungsi penutupan inspeksi dokumen (Slip Lembur, Buku, Pintu) ke tombol **`[F]`** (`CloseInspect`), mempertahankan `Esc` khusus untuk Pause Menu, serta tombol **`[Tab]` sekali klik** untuk keluar dari minigame laptop.
   - **Barometer Stres Merah (`StressBarometer`):** Membangun HUD Barometer Stres berwarna **merah** satu-layer (`StressBarometerCanvas`) di pojok kiri atas yang dimulai dari **`50%`**, disertai efek layar memerah dan peringatan berkedip pelan saat mencapai **`>= 70%`**. Stres naik saat salah ketik/timeout di laptop, naik berkala di koridor horor, memuncak ke **`100%`** saat tertangkap *Shadow*, dan diturunkan lewat minigame atur napas `[Tab]`.
   - **UI Minigame Laptop Monkeytype (`LaptopTypingMinigame`):** Membangun layar overlay mengetik ala Monkeytype (`5` kalimat) pada laptop meja belajar Raka (huruf benar berwarna **kuning**, merah jika salah, timer progresif **`60` → `30` detik**, kirim `Enter`, keluar `1x [Tab]`, otomatis keluar setelah 5 kalimat selesai).
   - **Minigame Atur Napas (`BreathingMinigame` via `[Tab]`):** Mekanik pengendalian stres (termasuk saat bersembunyi di dalam lemari maupun pemulihan pasca-tertangkap 100%) dengan menahan parameter di dalam **bar hijau** (`[Spasi]`/klik kiri tarik napas, lepas hembus), dilengkapi audio napas sinkron dan anggukan kamera FPP (naik ke atas saat tarik napas, turun ke bawah saat keluar napas).
   - **Task `T08` (Ponsel dan chat):** Membuat UI chat fiktif untuk pesan lama Bayu dan percakapan setelah Raka menginspeksi tas. Pengiriman pesan akhir harus membuka objektif Chapter 3 satu kali. Koordinasikan chat lama Ella dengan `T10`.
   - **Task `T15` (Audio Mixer, Subtitles & Accessibility):** Membangun `AudioDirector` dengan 5 bus `AudioMixer` (`Master`, `Voice`, `Ambience`, `SFX`, `Music`), fitur *audio ducking* otomatis saat dialog/monolog berjalan, sinkronisasi subtitle (`SubtitlePlaceholder`), serta pengaturan aksesibilitas (volume terpisah, FOV, sensitivitas, *reduce flashes*, skala teks).
   - **Save, Checkpoint & Menu Flow:** Memastikan persistensi data lintas sesi (`GameState`, penyimpanan clue JSON, `PlayerPrefs`), menu utama (`ShellScreen`), dan menu jeda (`FirstPersonSession`).
2. **Core Loop:**
   - Menangani fase **Explore & Interact**: kejelasan prompt `[E]`, pembacaan dokumen/surat di layar yang ditutup dengan **`[F]`**, navigasi Pause Menu dengan `Esc`, serta keluar dari minigame laptop dengan **tahan `[Enter]` selama 5 detik**.
3. **Cerita & Integrasi Aset:**
   - Mengimplementasikan layar **Content Notice** (peringatan konten psikologis secara proporsional & informasi dukungan krisis) serta layar `Credits.unity`.
   - Mengintegrasikan file audio (`.wav`/`.ogg`), font, dan sprite 2D dari Orang 3 ke dalam prefab UI Unity.
4. **QA & Optimasi Build (`T16`):**
   - Memelihara `GraphicsQualityService` (`Low` / `Medium` / `High`) dan `StandaloneBenchmarkRunner`, melakukan profiling CPU/GPU 1080p, serta menyiapkan build final Windows x64 (`NoRainbowAfter.exe`).

---

### B. Orang 2 — Gameplay, Narrative & AI Programmer
Bertanggung jawab atas alur permainan dari Prolog hingga Ending, kecerdasan buatan monster, mini-task, pengumpulan clue, dan determinisme percabangan cerita.

1. **Koding (Gameplay, AI & Narrative):**
   - Mengelola `ChapterDirector`, `GameState`, `LoopAtmosphere`, `ObjectiveTracker`, dan `ChapterOneObjective` untuk objektif awal Chapter 1 **`"Mengerjakan tugas"`** (melalui minigame mengetik di laptop `INT_LAPTOP`) serta perpindahan antar-bab (`PrologueRoot` → `Chapter1Root` → `Chapter2Root` → `Chapter3Root` → `EndingsRoot`) di `MasterGameplay.unity`.
   - **Minigame Mengetik Tugas Kuliah ala Monkeytype (`LaptopTypingMinigame`):** Mengimplementasikan generator 5 kalimat bertema kuliah mahasiswa, timer progresif per kalimat (**`60` → `30` detik**) dengan *auto-submit* saat waktu habis, `[Enter]` untuk mengirim kalimat, `1x [Tab]` untuk keluar manual, auto-exit otomatis tepat setelah 5 kalimat selesai, serta penalti kenaikan **Barometer Stres** (`+2%` typo, `+10%` kirim salah/belum selesai, `+15%` waktu habis).
   - **Tekanan Stres Koridor, Respawn Awal Koridor & Transisi Pasca Chapter 1 (`ChapterOneCorridorShift`, `ShadowEncounterController`, `ObjectiveTracker`):** Kenaikan stres berkala di koridor yang mendorong pemain masuk ke `HidingWardrobe` lalu menekan `[Tab]` atur napas; jika tertangkap *Shadow*, pemain respawn di **spawn awal koridor dengan stres 100%** dan wajib mengatur napas terlebih dahulu sebelum dapat melangkah lagi; setelah melewati `INT_C1_EXIT`, Raka kembali ke kamar kos dengan objektif placeholder **`"Chapter 2 sedang dikembangkan"`**.
   - **Task `T09` (Laporan berita dan stealth sorotan):** Mengerjakan beat bangun, profil laptop Chapter 2, transisi saat stres 100%, patroli Shadow, deteksi sorotan dengan cover, checkpoint per segmen, dan inspeksi tas di halte. Kerjakan blockout bersama Orang 3 dan feedback paparan bersama Orang 1.
   - **Task `T10` (Clue Registry & Foreshadowing):** Mengelola pencatatan unik (`HashSet`) tanpa duplikasi untuk `CLUE_01` s/d `CLUE_04` agar tetap tersimpan saat pemain tertangkap monster, respawn checkpoint, atau memuat ulang sesi.
   - **Task `T11` (Chapter 3 Choice & `EndingResolver`):** Membangun sistem pilihan akhir diegetik yang mengunci input secara atomik (`Fight` dengan klik kiri, `Hide` dengan merunduk di cover, `Approach` dengan berjalan mendekat) serta class C# murni `EndingResolver` beserta unit test NUnit (`EditMode`):
     - `Approach` → **Good Ending**
     - `Fight` atau `Hide` + lengkap 4 Clue (`CLUE_01`..`CLUE_04`) → **Hidden Ending**
     - `Fight` atau `Hide` + kurang dari 4 Clue → **Bad Ending**
   - **Task `T06` & `T13` (Monster AI & Animator Integration):** Memelihara `ShadowEncounterController` dan `ShadowVisibility`, serta menyambungkan `Animator Controller` 8 klip animasi dari model monster final (`T13`) tanpa mengubah ukuran kapsul collider (`r ≈ 0,23 m, h ≈ 1,80 m`) atau merusak logika pintu/cover.
2. **Core Loop:**
   - Menangani fase **Perceive, Decide & Consequence**: pengerjaan tugas kuliah di laptop yang memengaruhi **Barometer Stres Merah** (mulai `50%`), perubahan ruang/cahaya saat loop koridor, ketegangan pengejaran, penguncian pilihan pemain, dan eksekusi konsekuensi cerita.
3. **Cutscene & Sequencer (`Prolog` & `T12`):**
   - Mengkodekan alur cutscene pembuka ([PROLOGUE_CUTSCENE.md](Docs/PROLOGUE_CUTSCENE.md): berkendara saat hujan, dialog Raka & Ella, Raka menoleh ke belakang untuk berterima kasih, mobil dekat di perempatan, *hard cut* ke layar hitam sebelum kontak fisik, lalu suara benturan & hujan) dan ketiga sekuens penutup (`T12`: Good, Bad, Hidden).

---

### C. Orang 3 — 3D Blender Artist, Level Designer & Creative Director
Bertanggung jawab atas produksi aset 3D di Blender, tekstur/material PBR, penataan komposisi ruang & cahaya di Unity, penulisan naskah dialog, serta produksi suara/voice acting.

1. **Blender & 3D Asset Pipeline (`T13` & `T14`):**
   - **Monster Hero (`T13` — `ACT_Shadow_Raka`):**
     - Menyempurnakan aset [`BlenderSources/Characters/ACT_Shadow_Raka.blend`](Docs/RAKA_GENERATION_HANDOFF.md) mengikuti referensi terkunci [ACT_Shadow_Raka_Turnaround_Spec.md](Docs/References/ACT_Shadow_Raka_Turnaround_Spec.md).
     - Memastikan siluet bungkuk (`2,25–2,45 m`), lilitan perban mata + 2 slip kertas di pelipis, rambut basah, kemeja koyak dengan huruf `"F"` merah dilingkari di dada kiri, sabuk silang `X`, lembaran kertas di pinggang, kain lipit bawah, pelindung betis kiri, dan jari tangan memanjang.
     - Menyiapkan 2 varian tampilan pada rig yang sama: **Varian A** (wajah samar tertutup rambut/bayangan untuk Chapter 1) dan **Varian B** (struktur wajah Raka lebih terbaca dengan gurat luka superfisial & noda darah gelap non-gore untuk Chapter 3).
     - Memoles weight painting dan 8 klip animasi Generic: `Idle_Breath`, `Walk_Lurch`, `Pursue`, `Search_Cover`, `Fight_Reaction`, `Hide_Pass`, `Approach_Reveal`, `Despawn`.
   - **Prop Laptop Meja Belajar (`PROP_RakaLaptop` via Blender MCP):**
     - Mengambil referensi model laptop melalui Blender MCP lalu memodelkan ulang prop **`PROP_RakaLaptop`** ([`create_raka_laptop.py`](BlenderSources/Environment/create_raka_laptop.py)) dengan chassis gelap, deretan keyboard, dan layar emissive menyala di atas meja belajar Raka (`SM_PROP_StudyDesktop`).
   - **Fisik Tubuh FPP Raka ala *MiSide* (`ACT_Raka_StudentBody` via Blender MCP):**
     - Mengambil referensi dan turnaround mahasiswa kasual lalu memodifikasi aset orisinal organik **`ACT_Raka_StudentBody`** ([`create_raka_student_body.py`](BlenderSources/Characters/create_raka_student_body.py)) dengan *Subdivision Surface*: kemeja lengan pendek kotak-kotak biru-charcoal berkancing + kerah lipat, saku dada kiri dengan pulpen, lanyard biru + ID card putih, lengan anatomis + jam tangan hitam + **tangan 5 jari terartikulasi organik**, celana kain gelap, dan sepatu *slip-on* sol putih tebal, tanpa kepala agar bebas clipping kamera FPP.
   - **3D Props Chapter 2 & Clues (`T14`):**
     - Siapkan jalan basah, mobil polisi, box, garis polisi, halte, tas Raka dan gantungannya. Gunakan kembali laptop dan rig Shadow. Koordinasikan ponsel dan prop petunjuk dengan `T08` dan `T10`.
   - **3D Micro-Sets (Prolog, Chapter 3 & Wisuda):**
     - **Set Prolog (`ENV_003`):** Stang motor, kaca spion, lengan Raka, siluet Ella berhelm di boncengan, mobil dari arah berlawanan, dan potongan jalan basah perempatan empat arah.
     - **Set Chapter 3 (`WB-06`):** Ruang konfrontasi ujung lorong (`~6 × 8 m`) dengan 2 titik cover samping dan jalur tengah Approach.
     - **Set Good Ending (`ENV_004`):** Panggung/pelataran wisuda senja berhujan rintik beserta siluet latar keluarga/teman (`ACT_002`).
2. **Asset 2D, Voice Acting & Sound Design:**
   - Membuat gambar/tekstur bacaan close-up (tulisan tangan belakang foto *"Wisuda bareng, ya"*, slip lembur, dokumen magang, dan surat tulisan tangan Ella).
   - Mengoordinasikan rekaman suara asli (*voice acting*) Bahasa Indonesia untuk Raka & Ella di Prolog, monolog Raka, serta rekaman suara ber-interferensi `CLUE_04` (*"Ella... masih..."*).
   - Mengumpulkan/membuat SFX berlisensi bersih: hujan deras vs rintik, mesin motor, cipratan ban, lampu sein, klakson, rem, benturan logam (diputar saat layar sudah hitam), detak jantung, langkah kaki basah, dan getar ponsel.
3. **Cerita, Naskah & Level Lighting:**
   - Menulis naskah final Bahasa Indonesia untuk dialog Prolog, monolog tiap chapter, chat lama Ella (`CLUE_03`), chat Bayu Kopling di Chapter 2, chat Bayu di Bad Ending (*"Ka, lu di mana?"*, *"Lu kenapa ga masuk grafkom tadi?"*, *"Pak dosen nanyain lo"*), chat Bayu di Hidden Ending (*"Ka, lu di mana?"*, *"Si Ella udah sadar"*), dan surat wisuda Ella.
   - Menjaga **aturan konsistensi misteri**: tidak pernah menyatakan Ella meninggal sebagai fakta objektif sebelum twist terungkap, dan tidak menampilkan kekerasan eksplisit/metode bunuh diri.
   - Menata pencahayaan dan kabut di Unity agar objek interaktif selalu mudah ditemukan dan siluet monster terbaca tegas pada jarak `8–12 m` maupun `2–3 m`.

---

## 3. Matriks Pembagian Berdasarkan Elemen

| Elemen Proyek | Orang 1 (Systems, UI & Tech) | Orang 2 (Gameplay, AI & Narrative) | Orang 3 (Blender, Art, Level & Story) |
| :--- | :--- | :--- | :--- |
| **Koding C#** | FPP Controller, Body Visual, Barometer Stres Merah (`50%`) + `[Tab]` Breathing Minigame, Raycast Interaksi (`E`/`F`), UI Monkeytype/Phone/Inspect/Menu, Save/Prefs, AudioMixer, Quality Presets. | `ChapterDirector`, `LaptopTypingMinigame`, encounter sorotan Chapter 2, `ClueRegistry`, `ShadowEncounterController`, `EndingResolver`, Cutscene Sequencer. | Skrip bantu automasi Blender Python (`BlenderSources/*.py`, `create_raka_laptop.py`, `create_raka_student_body.py`). |
| **Blender & 3D** | Validasi import `.fbx`, collider, `PROP_RakaLaptop`, `ACT_Raka_StudentBody`, dan shader/material URP. | Menyambungkan `Animator`, bone pivot, dan trigger zona ke prefab 3D. | Modeling, sculpting, modifikasi referensi Blender MCP, UV unwrap, baking tekstur PBR, rigging Generic, dan pembuatan 8 animasi. |
| **Asset 2D & Audio** | Integrasi sprite UI, font, `AudioClip`, dan pengaturan `AudioMixer` bus. | Memicu event audio & subtitle (`IAudioCueSource`) dari state cerita/AI. | Pembuatan desain dokumen 2D, rekaman voice Raka/Ella, serta kurasi/pembuatan SFX & ambience. |
| **Cerita & Naskah** | Menyajikan teks pada UI Phone, Inspect Overlay, Subtitle, dan Content Notice. | Mengatur urutan kemunculan beat cerita (`C1`–`C3`), syarat 4 clue, dan logika penentu 3 ending. | Menulis naskah dialog, monolog, isi chat Ella/Bayu, surat wisuda, dan storyboard cutscene. |
| **Core Loop** | **Explore & Interact** (kontrol kamera/gerak, tubuh FPP, Barometer Stres, prompt `[E]`, tutup `[F]`, tahan `Enter` 5 detik keluar laptop). | **Perceive, Decide & Consequence** (tugas ketik Monkeytype C1, loop ruangan, tugas C2, pilihan `Fight/Hide/Approach`, transisi). | **Visual Guiding & Pacing** (tata letak prop laptop/meja, kontras lampu hangat/dingin, jarak pandang kabut). |

---

## 4. Urutan Pengerjaan (Roadmap Sisa Proyek)

### Tahap 1 — Chapter 2: Di Bawah Sorotan (`T08`, `T09`, `T10`, `T13`, `T14`)
- **Orang 1:** Menyiapkan feedback paparan, profil UI laporan, dan chat Bayu (`T08`).
- **Orang 2:** Menghubungkan pembuka, stealth, retry, inspeksi tas, dan handoff Chapter 3 (`T09`). Integrasikan petunjuk sesuai `T10`.
- **Orang 3:** Membuat blockout jalan dan halte, menata box dan lampu mobil, serta memasang sorotan pada rig Shadow yang ada (`T13`, `T14`).

### Tahap 2 — Chapter 3, Cutscene Prolog & Sistem Audio (`Prolog`, `T11`, `T15`)
- **Orang 1:** Membangun `AudioDirector`, 5 bus `AudioMixer`, integrasi voice/SFX, subtitle sync, dan menu aksesibilitas (`T15`).
- **Orang 2:** Membangun arena konfrontasi Chapter 3, pilihan atomik `Fight / Hide / Approach`, `EndingResolver` beserta unit test (`T11`), dan mengganti visual placeholder `T06` dengan prefab hero `T13`.
- **Orang 3:** Membuat micro-set Prolog (motor, Ella membonceng, perempatan hujan) dan set Wisuda di Blender, serta menyelesaikan rekaman suara Raka & Ella.

### Tahap 3 — Tiga Ending, Full QA & Pengumpulan (`T12`, `T16`)
- **Orang 2 & Orang 3:** Merakit sekuens Cutscene Prolog serta ketiga Ending (`T12` — Good wisuda + surat Ella, Bad kamar sunyi + chat Bayu, Hidden kamar sunyi + chat *"Si Ella udah sadar"*).
- **Seluruh Tim (Orang 1, 2, 3):** Menjalankan seluruh 16 skenario pengujian (`QA01–QA16`), menguji performa standalone Windows x64 (`T16`), dan merapikan dokumentasi akhir.

---

## 5. Pembagian Tanggung Jawab QA (`QA01`–`QA16`)

| Anggota | Kasus Uji QA yang Dipertanggungjawabkan |
| :--- | :--- |
| **Orang 1** | `QA06` (chat Bayu terbuka setelah inspeksi tas dan membuka C3 satu kali), `QA12` (Tombol `F` menutup overlay inspect, tahan `Enter` 5 detik keluar laptop, `Esc` untuk Pause), `QA14` (Build Windows x64 stabil dari Boot hingga Credits + benchmark FPS Low/Medium/High). |
| **Orang 2** | `QA02` (Minigame mengetik laptop `"Mengerjakan tugas"`, Barometer Stres merah 50% naik saat salah/timeout, & loop koridor C1), `QA03` (C1 Fight), `QA04` (C1 Hide), `QA05` (C1 Caught kembali ke checkpoint tanpa hapus clue), `QA07` (selesaikan laporan, lewati sorotan dengan retry per segmen, capai halte), `QA08` & `QA13` (Persistensi 4 clue unik saat save/load/respawn), `QA09` (Approach → Good), `QA10` (Fight/Hide < 4 clue → Bad), `QA11` (Fight/Hide = 4 clue → Hidden). |
| **Orang 3** | `QA01` (Alur MainMenu → Prolog hujan di perempatan → *hard cut* hitam sebelum tabrakan → masuk kos), `QA15` (Validasi FBX monster `T13`, rig & 8 animasi bebas T-pose/clipping pintu 2,05 m), `QA16` (Keterbacaan siluet monster pada 8–12 m dan wajah samar/luka ringan pada 2–3 m di FPP). |

---

## 6. Aturan Kolaborasi Git & Unity (Mencegah Konflik Scene)

1. **Hindari Mengedit `MasterGameplay.unity` Secara Bersamaan:**
   - **Orang 1** bekerja pada prefab UI (`Assets/_NoRainbowAfter/UI/Prefabs/`), pengaturan kualitas, dan script di folder `Player/`, `Interaction/`, `UI/`, `Quality/`.
   - **Orang 2** bekerja pada prefab logika chapter/encounter, unit test, dan script di folder `Narrative/`, `AI/`, `Tests/`.
   - **Orang 3** bekerja di folder `BlenderSources/` dan merakit prefab visual di `Assets/_NoRainbowAfter/Art/` (`Models/`, `Materials/`, `Textures/`, `Prefabs/`).
   - Gunakan mekanisme prefab atau editor installer script saat menyatukan hasil kerja ke dalam `MasterGameplay.unity`.
2. **Wajib Menyertakan File `.meta`:**
   - Setiap penambahan aset baru (`.fbx`, `.png`, `.wav`, `.mat`, `.prefab`, `.cs`) **wajib** di-commit bersama file `.meta`-nya agar referensi GUID di Unity tidak rusak di perangkat anggota tim lain.
3. **Jangan Commit Folder Cache Lokal:**
   - Jangan pernah memasukkan folder `Library/`, `Temp/`, `Logs/`, `UserSettings/`, atau `Builds/` ke dalam commit Git.
