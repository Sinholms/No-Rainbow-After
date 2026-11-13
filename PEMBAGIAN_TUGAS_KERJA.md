# Pembagian Tugas Kerja Tim (3 Orang) — No Rainbow After

**Proyek:** No Rainbow After (First-Person Psychological Horror)<br>
**Target Platform:** PC Windows x64 • Unity 6000.6.8f1 (URP 17.6.0) • Blender 5.2.3 LTS<br>
**Acuan Utama:**
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
   - Memelihara kontrol FPP (`FirstPersonPlayer`, `FirstPersonMotor`, `FirstPersonInputSource`), raycast interaksi 2,3 m anti-tembus dinding (`PlayerInteractor`), overlay inspeksi dokumen (`InspectOverlay`), dan efek pandangan melingkar (`CircularViewMask`).
   - **Task `T08` (Phone & Chat UI):** Membangun `PhoneUIController` diegetik dengan merek/tampilan fiktif yang menampilkan kontak Ella & Bayu Kopling, chat lama Ella, pilihan **Kirim Pesan** (menampilkan tepat **satu centang** tanpa balasan) atau **Kunci Layar**, serta menyimpan flag `messageEllaSent`.
   - **Task `T15` (Audio Mixer, Subtitles & Accessibility):** Membangun `AudioDirector` dengan 5 bus `AudioMixer` (`Master`, `Voice`, `Ambience`, `SFX`, `Music`), fitur *audio ducking* otomatis saat dialog/monolog berjalan, sinkronisasi subtitle (`SubtitlePlaceholder`), serta pengaturan aksesibilitas (volume terpisah, FOV, sensitivitas, *reduce flashes*, skala teks).
   - **Save, Checkpoint & Menu Flow:** Memastikan persistensi data lintas sesi (`GameState`, penyimpanan clue JSON, `PlayerPrefs`), menu utama (`ShellScreen`), dan menu jeda (`FirstPersonSession`).
2. **Core Loop:**
   - Menangani fase **Explore & Interact**: kejelasan prompt `[E]`, pembacaan dokumen/surat di layar, navigasi tombol `Esc` yang konsisten (menutup overlay Inspect/Phone terlebih dahulu sebelum membuka Pause).
3. **Cerita & Integrasi Aset:**
   - Mengimplementasikan layar **Content Notice** (peringatan konten psikologis secara proporsional & informasi dukungan krisis) serta layar `Credits.unity`.
   - Mengintegrasikan file audio (`.wav`/`.ogg`), font, dan sprite 2D dari Orang 3 ke dalam prefab UI Unity.
4. **QA & Optimasi Build (`T16`):**
   - Memelihara `GraphicsQualityService` (`Low` / `Medium` / `High`) dan `StandaloneBenchmarkRunner`, melakukan profiling CPU/GPU 1080p, serta menyiapkan build final Windows x64 (`NoRainbowAfter.exe`).

---

### B. Orang 2 — Gameplay, Narrative & AI Programmer
Bertanggung jawab atas alur permainan dari Prolog hingga Ending, kecerdasan buatan monster, mini-task, pengumpulan clue, dan determinisme percabangan cerita.

1. **Koding (Gameplay, AI & Narrative):**
   - Mengelola `ChapterDirector`, `GameState`, `LoopAtmosphere`, dan `ObjectiveTracker` untuk mengatur perpindahan antar-bab (`PrologueRoot` → `Chapter1Root` → `Chapter2Root` → `Chapter3Root` → `EndingsRoot`) di `MasterGameplay.unity`.
   - **Task `T09` (Academic Tasks & Montage):** Mengimplementasikan `AcademicTaskController` untuk 3 tugas mini di Chapter 2:
     - `TASK_A_NOTES`: Mengurutkan 3 catatan kuliah secara kronologis.
     - `TASK_B_INTERNSHIP`: Memilih berkas magang yang benar dari 3 dokumen.
     - `TASK_C_SCHEDULE`: Memilih prioritas pada jadwal kerja & kuliah yang bentrok.
     - Mengatur efek jalan melambat sementara (*slow walk*) dan percabangan menuju *bed montage* jika ketiga tugas gagal, lalu tetap melanjutkan ke Chapter 3 tanpa softlock.
   - **Task `T10` (Clue Registry & Foreshadowing):** Mengelola pencatatan unik (`HashSet`) tanpa duplikasi untuk `CLUE_01` s/d `CLUE_04` agar tetap tersimpan saat pemain tertangkap monster, respawn checkpoint, atau memuat ulang sesi.
   - **Task `T11` (Chapter 3 Choice & `EndingResolver`):** Membangun sistem pilihan akhir diegetik yang mengunci input secara atomik (`Fight` dengan klik kiri, `Hide` dengan merunduk di cover, `Approach` dengan berjalan mendekat) serta class C# murni `EndingResolver` beserta unit test NUnit (`EditMode`):
     - `Approach` → **Good Ending**
     - `Fight` atau `Hide` + lengkap 4 Clue (`CLUE_01`..`CLUE_04`) → **Hidden Ending**
     - `Fight` atau `Hide` + kurang dari 4 Clue → **Bad Ending**
   - **Task `T06` & `T13` (Monster AI & Animator Integration):** Memelihara `ShadowEncounterController` dan `ShadowVisibility`, serta menyambungkan `Animator Controller` 8 klip animasi dari model monster final (`T13`) tanpa mengubah ukuran kapsul collider (`r ≈ 0,23 m, h ≈ 1,80 m`) atau merusak logika pintu/cover.
2. **Core Loop:**
   - Menangani fase **Perceive, Decide & Consequence**: perubahan halus ruang/cahaya saat loop koridor, ketegangan pengejaran, penguncian pilihan pemain, dan eksekusi konsekuensi cerita tanpa *depression score* numerik.
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
   - **3D Props Chapter 2 & Clues (`T14`):**
     - `PROP_003_RakaPhone` (ponsel Raka), `PROP_004_PhotoFrame` (bingkai foto `CLUE_01`), `PROP_002_NotesStack` (3 kartu catatan), `PROP_002_InternshipForms` (3 dokumen magang), `PROP_002_ScheduleBoard` (jadwal kerja/kuliah), prop perekam suara (`CLUE_04`), serta `PROP_004_LetterEnvelope` & `PROP_004_EllaLetter` (amplop & surat Ella).
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
| **Koding C#** | FPP Controller, Raycast Interaksi, UI Phone/Inspect/Menu, Save/Prefs, AudioMixer, Quality Presets. | `ChapterDirector`, `AcademicTaskController`, `ClueRegistry`, `ShadowEncounterController`, `EndingResolver`, Cutscene Sequencer. | Skrip bantu automasi Blender Python (`BlenderSources/*.py`) bila diperlukan. |
| **Blender & 3D** | Validasi import `.fbx`, collider, dan penyesuaian shader/material URP. | Menyambungkan `Animator`, bone pivot, dan trigger zona ke prefab 3D. | Modeling, sculpting, UV unwrap, baking tekstur PBR 2K, rigging Generic, dan pembuatan 8 animasi. |
| **Asset 2D & Audio** | Integrasi sprite UI, font, `AudioClip`, dan pengaturan `AudioMixer` bus. | Memicu event audio & subtitle (`IAudioCueSource`) dari state cerita/AI. | Pembuatan desain dokumen 2D, rekaman voice Raka/Ella, serta kurasi/pembuatan SFX & ambience. |
| **Cerita & Naskah** | Menyajikan teks pada UI Phone, Inspect Overlay, Subtitle, dan Content Notice. | Mengatur urutan kemunculan beat cerita (`C1`–`C3`), syarat 4 clue, dan logika penentu 3 ending. | Menulis naskah dialog, monolog, isi chat Ella/Bayu, surat wisuda, dan storyboard cutscene. |
| **Core Loop** | **Explore & Interact** (kontrol kamera/gerak, prompt `[E]`, buka HP/dokumen). | **Perceive, Decide & Consequence** (loop ruangan, tugas C2, pilihan `Fight/Hide/Approach`, transisi). | **Visual Guiding & Pacing** (tata letak prop, kontras lampu hangat/dingin, jarak pandang kabut). |

---

## 4. Urutan Pengerjaan (Roadmap Sisa Proyek)

### Tahap 1 — Penyelesaian Chapter 2 & Pemolesan Monster (`T08`, `T09`, `T10`, `T13`, `T14`)
- **Orang 1:** Membangun UI Ponsel diegetik (`T08` — kontak Ella & Bayu, fitur centang satu).
- **Orang 2:** Membangun 3 mini-task akademik + *bed montage* (`T09`) serta integrasi penuh 4 clue (`T10`).
- **Orang 3:** Memoles model, material A/B, dan 8 animasi `ACT_Shadow_Raka` (`T13`) di Blender + membuat prop meja belajar & clue Chapter 2 (`T14`).

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
| **Orang 1** | `QA06` (Kirim pesan Ella centang satu & tersimpan), `QA12` (Urutan tombol `Esc` menutup Inspect/Phone sebelum Pause), `QA14` (Build Windows x64 stabil dari Boot hingga Credits + benchmark FPS Low/Medium/High). |
| **Orang 2** | `QA02` (Buku & loop koridor C1), `QA03` (C1 Fight), `QA04` (C1 Hide), `QA05` (C1 Caught kembali ke checkpoint tanpa hapus clue), `QA07` (Gagal semua task C2 → bed montage → lanjut C3), `QA08` & `QA13` (Persistensi 4 clue unik saat save/load/respawn), `QA09` (Approach → Good), `QA10` (Fight/Hide < 4 clue → Bad), `QA11` (Fight/Hide = 4 clue → Hidden). |
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
