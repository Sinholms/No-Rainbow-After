# NO RAINBOW AFTER — GDD v1.1 TEXT REFERENCE

> **Generated, read-only Markdown text mirror of `GDD_No_Rainbow_After_v1_1.docx`.**
> Canonical GDD is the DOCX. This mirror reproduces textual paragraphs and tables for tools that cannot read `.docx`. It does **not** reproduce embedded visual artwork or document layout; view original DOCX and 2D monster reference for those.
> Do not edit this mirror independently of the GDD, and do not treat layout omissions as missing game requirements.

GAME DESIGN DOCUMENT  /  VERSI IMPLEMENTASI

# NO RAINBOW AFTER

*A first-person psychological horror about survival, guilt, and the possibility of moving forward.*

DOKUMEN DESAIN + RENCANA PRODUKSI UNITY / BLENDER / CODEX

Status: GDD operasional v1.1  •  Platform: PC Windows  •  Engine: Unity 6  •  Sudut pandang: First Person

Berdasarkan: PPT game “No Rainbow After”, outline storyline terbaru dari tim, dan dokumen “Langkah Awal Perencanaan Game Psikologis”.

Disusun: 06 November 2026

Tujuan dokumen: menjadi sumber spesifikasi tunggal bagi anggota tim dan agen Codex yang bekerja melalui MCP Unity/Blender.

Catatan: detail teknis, parameter awal, dan aturan ending yang tidak ditentukan dalam bahan asli adalah REKOMENDASI DESAIN v1.1 dan perlu diuji saat prototyping.

PERUBAHAN v1.1: (1) desain monster humanoid rusak dengan wajah samar/luka dan darah ringan; (2) pipeline Blender rigging + 8 animasi; (3) katalog ID T00–T16 disamakan dengan Markdown; (4) QA monster tambahan QA15–QA16. Storyline, 3 chapter, 3 ending, dan FPP tidak diubah.

## 0  Ringkasan keputusan desain & cakupan revisi

Dokumen ini menggantikan rancangan konseptual lama pada bagian yang bertentangan dengan cerita final. Hal yang tetap berlaku: tema sosial, nuansa atmosferik, eksplorasi FPP, karakter Raka–Ella, dan sasaran tugas Grafika Komputer.

| Keputusan | Spesifikasi versi 1.0 |
| --- | --- |
| Judul resmi | No Rainbow After (final; bukan Purple Rain atau Selamat!). |
| Alur utama | Prolog → Chapter 1 Denial/Anger → Chapter 2 Bargaining/Depression → Chapter 3 Acceptance → ending. |
| Protagonis | Raka, mahasiswa semester 5 yang kuliah sambil bekerja karena tekanan ekonomi keluarga. |
| Ella | Teman kuliah yang disukai Raka; nasib setelah kecelakaan tidak dipastikan dalam prolog. |
| Twist | Dalam Hidden Ending terungkap bahwa Ella sebenarnya hidup dan baru sadar. |
| Good Ending | Raka pulih secara bertahap, mampu melanjutkan kuliah, wisuda; ada surat misterius dari Ella. |
| Bad Ending | Akhir tragis Raka disampaikan secara implisit melalui lingkungan dan pesan Bayu, tanpa adegan eksplisit. |
| Struktur level | Tiga chapter utama; lima tahapan duka merupakan tema naratif, bukan lima stage wajib. |
| Arah visual | Horror realistis, lembap, muram, sinematik, terinspirasi pendekatan atmosfer Silent Hill 2 Remake; dunia dan aset harus orisinal. |
| Target implementasi | Vertical slice yang dapat dimainkan dahulu, lalu game lengkap; otomasi via Codex–Unity MCP, Blender bila perlu. |

### Asumsi dan batasan yang diputuskan untuk memperjelas implementasi

- Durasi target versi tugas: 20–30 menit (penyesuaian desain dari estimasi PPT 15–25 menit; dipastikan lagi setelah playtest).
- Target audiens 16+; tema duka, kecelakaan, depresi, dan bahaya psikologis diberi peringatan konten yang proporsional.
- Bahasa dialog, UI, dan subtitle utama adalah Bahasa Indonesia; istilah teknis internal boleh berbahasa Inggris.
- Satu area MasterGameplay dengan sub-root chapter untuk iterasi; scene Boot, MainMenu, MasterGameplay, dan Credits dipisah untuk memudahkan pengembangan.
- Tidak ada multiplayer, sistem tembak-menembak, open-world, inventory kompleks, procedural generation, atau sistem combat berat.
- Plot twist, isi surat, pilihan respons akhir, dan satu kontak Bayu Kopling mengikuti cerita asli. Aturan kondisional ending dibuat eksplisit dalam Bab 5 sebagai usulan implementasi.
## 1  Game overview & visi produk

### 1.1 Elevator pitch

Setelah kecelakaan motor pada malam hujan yang melibatkan Ella, Raka menjalani kuliah dan kerja sambil dihantui rasa bersalah. Kamar kos, lorong, dan jalan yang familier berubah menjadi manifestasi pikirannya. Pemain harus menavigasi pekerjaan dan tugas akademik yang semakin berat, memilih untuk melawan, menghindar, atau mendekati bayangan dirinya sendiri, serta menemukan potongan kebenaran tentang malam kecelakaan. Akhir perjalanan dapat berupa kelulusan yang membawa secercah harapan, tragedi dalam kesunyian, atau pengungkapan mengejutkan bahwa Ella ternyata masih hidup.

### 1.2 Game pillars

| Pilar | Implementasi nyata | Tolak ukur |
| --- | --- | --- |
| Atmosfer yang menekan | Ruang sempit, kabut, bayangan, hujan, material basah, suara jauh dan perubahan ruang kecil. | Pemain merasa tidak nyaman meskipun tidak ada jumpscare. |
| Narasi melalui tindakan | Tugas akademik, telepon, chat, surat, dan pilihan menghadapi monster bukan sekadar cutscene. | Setiap aksi punya makna emosional yang jelas. |
| Pilihan yang bermakna | Fight / Hide / Approach ditanamkan sejak Chapter 1, kemudian diberi konsekuensi puncak di Chapter 3. | Pemain memahami hubungan antara keputusan dan akhir, tanpa formula moralistik. |
| Visual sinematik terukur | PBR material, pencahayaan terarah, komposisi, fog, audio 3D, bukan pemaksaan seluruh fitur high-end. | Vertical slice stabil dengan mood yang konsisten. |

### 1.3 Sasaran & definisi keberhasilan

- Pemain menyelesaikan prolog, 3 chapter, dan minimal 1 ending tanpa bantuan pengembang.
- Setiap chapter memiliki tujuan jelas, affordance interaksi terlihat, serta sistem checkpoint yang adil.
- Pemain memahami bahwa monster adalah metafora kondisi internal Raka dan tidak selalu merupakan makhluk nyata.
- Setiap ending memiliki pemicu teruji, cutscene/sajian penutup, subtitle, dan transisi kembali ke menu.
- Build Windows dapat berjalan tanpa error merah pada Unity Console dan tanpa asset missing.
- Visual tidak sekadar “gelap”: objek penting selalu dapat ditemukan lewat pencahayaan, suara, dan kontras.
### 1.4 Integrasi SDGs dalam lingkungan

| SDG | Realitas Raka | Elemen interaktif yang menampilkan isu |
| --- | --- | --- |
| SDG 1 — No Poverty | Keterbatasan finansial, kerja paruh waktu, menanggung kebutuhan sendiri, keluarga membantu adik di kampung. | Tagihan, amplop transfer, slip lembur, uang makan terbatas, chat keluarga. |
| SDG 4 — Quality Education | Akses dan partisipasi akademik terganggu kelelahan, biaya, waktu, fasilitas, dan deadline. | Buku pinjaman, laptop tua, tugas tertunda, formulir magang, notifikasi dosen. |
| SDG 3 — Good Health and Well-being | Duka dan trauma setelah kecelakaan, kelelahan, keterputusan sosial, kebutuhan akan dukungan. | Pacing tubuh, monolog, pesan teman, ruang tidur, pilihan menerima bantuan. |

Catatan representasi: SDGs menjadi konteks, bukan klaim bahwa kemiskinan otomatis menyebabkan satu kondisi klinis. Depresi/PTSD ditangani dengan bahasa berhati-hati dan tidak didiagnosis sembarangan oleh UI game.

## 2  Story bible, karakter & aturan misteri

### 2.1 Latar belakang naratif (canon)

Raka, mahasiswa semester lima, menanggung biaya hidup melalui pekerjaan paruh waktu karena penghasilan keluarga di kampung terbatas dan orang tua masih membiayai sekolah adik-adiknya. Beban kerja, tugas, dan persiapan magang menggerus waktu tidur. Pada suatu malam hujan, Raka mengantar Ella, teman kuliah yang ia sukai. Keduanya bercanda dan berjanji akan wisuda bersama. Raka mengalami microsleep saat berkendara, disusul suara klakson truk, selip ban, benturan, dan layar gelap. Tidak ada visual yang memastikan apa yang terjadi kepada Ella. Raka terbangun sendiri di kamar kos dengan ingatan yang terpecah; ia meyakini Ella telah meninggal. Rasa bersalah serta kesulitan akademik dan ekonomi berlanjut. Cerita berikutnya dibangun dari sudut pandang dan persepsi Raka yang tidak selalu lengkap.

### 2.2 Karakter

| Karakter | Peran dramatis | Representasi dalam game |
| --- | --- | --- |
| Raka | Protagonis; mahasiswa semester 5, pekerja paruh waktu, tertekan tetapi punya harapan untuk lulus. | FPP; suara napas/monolog; tangan sesekali; siluet dirinya pada monster dan cermin. |
| Ella | Teman kuliah yang disukai Raka; sumber kenangan dan janji wisuda. Tidak hadir sebagai musuh. | Suara prolog, chat satu centang, jejak surat, benda/memori; kebenaran nasibnya hanya di Hidden Ending. |
| Bayu Kopling | Teman mahasiswa sebagai jangkar realitas; mengirim pesan menanyakan ketidakhadiran. | Kontak WA, notifikasi, dialog singkat opsional, pesan terakhir pada Bad/Hidden. |
| Keluarga / teman | Mewakili harapan, jarak emosional, dan dukungan yang tidak selalu hadir tepat waktu. | Chat, suara samar, figur latar cutscene wisuda atau foto statis. |
| Bayangan Raka | Metafora rasa bersalah, amarah, penolakan, dan ketakutan akan masa depan. | Makhluk humanoid hitam lembap, bentuk tubuh mencerminkan Raka; muka samar (gurat luka/bercak darah kecil), paper fragments, dada retak; AI placeholder T06, hero T13. |

### 2.3 Kronologi yang diketahui pemain

1. Semester 5: Raka kuliah sambil bekerja; kedekatan dengan Ella berkembang melalui interaksi kecil.
1. Malam hujan: janji wisuda di motor, kelelahan, microsleep, suara tabrakan, cut to black.
1. Setelah insiden: Raka berada sendirian di kos, bersikap menarik diri dan berulang kali gagal fokus.
1. Chapter 1: persepsinya mulai mendistorsi tempat tinggal; bayangan Raka mengejar/menekan.
1. Chapter 2: Raka dapat mengirim pesan ke Ella (satu centang), menghadapi tugas kuliah/kerja, menemukan petunjuk yang tidak sepenuhnya cocok dengan asumsi awalnya.
1. Chapter 3: Raka menghadapi manifestasi diri. Hasil ditentukan melalui pilihan akhir dan informasi yang ditemukannya.
1. Good: lompatan waktu menuju wisuda dan surat; Bad/Hidden: penutupan tragis di kamar dengan pesan yang berbeda.
### 2.4 Kebenaran rahasia & aturan penyembunyian informasi

- Fakta di dunia cerita: Ella tidak dipastikan meninggal dalam prolog. Hidden Ending mengonfirmasi Ella masih hidup dan sudah sadar; kondisi tepat Ella sebelum itu tidak boleh dipastikan lewat dialog umum.
- Yang dipercaya Raka: Ella meninggal atau tidak dapat kembali. Monolog Raka boleh menyatakan keyakinannya (“Aku kehilangan Ella”), tetapi narator objektif tidak boleh mengonfirmasi kematian Ella.
- Pemain menerima petunjuk ambigu; tidak boleh muncul berita koran atau sertifikat kematian yang faktual, kecuali jelas disajikan sebagai distorsi pikiran Raka.
- Good Ending menyimpan misteri Ella: surat yang ditemukan saat wisuda memperkuat janjinya, bukan membuktikan bahwa ia telah meninggal. Ini menjaga kemungkinan twist pada permainan ulang.
- Jembatan logika yang direkomendasikan: Raka terisolasi dan informasi medis pascakecelakaan tidak utuh dalam ingatannya; percakapan keluarga/teman tentang kondisi Ella tersendat, tanpa memperlihatkan mereka sengaja berbohong.
- Pertanyaan yang dibiarkan terbuka secara artistik: mengapa Ella menulis surat saat semester lima? Petunjuk “time capsule untuk wisuda” dapat ditemukan di chat lama, tetapi isi lengkapnya baru dibuka dalam Good Ending.
### 2.5 Tema visual dan simbol

| Motif | Makna | Penerapan visual/audio |
| --- | --- | --- |
| Hujan | Kecelakaan, ingatan, sekaligus kemungkinan perubahan. | Rintik tipis saat wisuda; deras di prolog dan Bad/Hidden. |
| Lorong | Rutinitas tanpa jalan keluar dan perasaan terperangkap. | Geometri modular dengan perubahan subtil antarkunjungan. |
| Siluet | Wujud konflik batin Raka. | Animasi tersentak, distorsi volume, tekstur matte gelap. |
| Satu centang | Keinginan untuk mengulang keadaan yang tak dapat dijangkau. | UI ponsel dengan status belum terkirim ke penerima. |
| Surat | Janji dan masa depan; sesuatu yang terasa terlambat. | Kertas tua tertutup amplop, tulisan tangan orisinal. |
| Cahaya senja | Harapan yang tidak berarti kesedihan lenyap. | Warna amber lembut tanpa grading terlalu jenuh. |

## 3  Pengalaman pemain & core gameplay

### 3.1 Core loop

Explore (mengamati ruang) → Interact (membaca/mengambil benda) → Perceive (anomali/tekanan meningkat) → Decide (menghadapi/menghindar/meminta bantuan) → Consequence (dialog, perubahan dunia, flag cerita) → Transition (chapter/ending).

### 3.2 Skema kendali & UX

| Aksi | Input PC | Detail desain |
| --- | --- | --- |
| Move | W/A/S/D | CharacterController; akselerasi halus, collider stabil. |
| Look | Mouse | Sensitivitas dapat diubah, lock/unlock kursor otomatis. |
| Walk / Sprint | Left Shift | Tahan untuk sprint; stamina terbatas ketika dikejar. |
| Crouch | Left Ctrl | Toggle disarankan; ruang sembunyi memiliki volume trigger. |
| Interact / Inspect | E | Raycast 2,3 m; prompt kontekstual; hold pendek bila perlu. |
| Fight (kontekstual) | Klik kiri | Hanya aktif di encounter monster, animasi pukul pendek; bukan combat penuh. |
| Read/Phone | E / tombol UI | Layar baca dokumen/WhatsApp dengan fokus cursor. |
| Pause / Back | Esc | Menu Pause, lanjut, sensitivitas, subtitle, kembali menu. |
| Navigate choices | Mouse / Enter | Pilihan diegetik dengan highlight dan suara umpan balik. |

### 3.3 Interaksi & inspect mode

- Objek relevan memiliki collider, layer Interactable, data ID unik, deskripsi singkat, dan penanda visual sangat halus saat crosshair diarahkan.
- Interaksi hanya saat jarak 2,3 meter dan tidak terhalang occluder; tidak boleh menembus dinding atau objek besar.
- Untuk dokumen, kamera transisi ke inspect mode atau tampilkan overlay teks yang mudah dibaca; Esc menutup overlay tanpa bergerak.
- Item cerita disimpan sebagai flag atau log jurnal, bukan model 3D yang wajib menempel pada tangan pemain; mengurangi beban animasi dan bug.
- Door/ChapterGate memeriksa objective flags; jika tertutup, tampilkan alasan singkat dalam monolog alih-alih popup teknis.
### 3.4 Sistem stamina, stres, dan kegagalan

| Sistem | Perilaku | Batasan & feedback |
| --- | --- | --- |
| Sprint | Stamina berkurang selama sprint dan pulih saat berjalan. | MVP: 5 dtk sprint, 4 dtk recharge, delay 1 dtk; angka dapat dituning. |
| Panic audio | Detak jantung dan napas meningkat di dekat monster. | Audio mixer event-driven; hindari distorsi menyakitkan. |
| Chase reset | Monster mencapai radius tangkap atau pemain tertahan terlalu lama. | Fade out, pulse audio, respawn checkpoint chapter; bukan game over permanen. |
| Academic tasks | Tiga kegiatan sederhana dengan timer opsional. | Gagal tugas tidak menghentikan cerita; berakhir pada rangkaian “rebahan seharian”. |
| Slow walk | Pada akhir Chapter 2, jalan melambat sementara. | Tetap responsif; pemotongan kecepatan maksimal disarankan 40–50%, tanpa softlock. |

### 3.5 Kamera dan kenyamanan

- Kamera first person dengan FOV default 75 (rentang 65–95), bob halus dan opsional dimatikan.
- Vignette dan motion blur moderat, masing-masing dapat dikurangi/dimatikan; tidak menyembunyikan petunjuk inti.
- Segala kejutan visual diberi warning konten dan opsi reduce flashes; subtitle on/off dan kontrol volume terpisah.
- Perpindahan realitas/mindscape menggunakan fade dan re-layout ruangan, bukan teleportasi mendadak yang membingungkan.
## 4  Desain alur, objective & level per chapter

### 4.0 Prolog — Rain, Promise, Crash (±2 menit)

Tujuan narasi: Membangun kedekatan, janji wisuda, tanda kelelahan, dan kecelakaan tanpa menunjukkan tubuh atau kepastian kematian Ella.

1. Layar gelap: hujan terdengar lewat spatial ambience; Raka dan Ella bercanda soal wisuda.
1. Suara Raka makin berat; animasi vignette pelan; respons Ella terdengar khawatir.
1. Klakson, rem/ban selip, benturan singkat, diam total selama 1–2 detik.
1. Fade menuju kamar kos; tampil teks objektif “Bersiap berangkat”.
Prolog audio-only memerlukan rekaman suara original/berizin, tidak memakai audio permainan atau lagu komersial.

### 4.1 Chapter 1 — Denial & Anger: “Rutinitas yang Retak” (±6–8 menit)

| Elemen | Spesifikasi |
| --- | --- |
| Ruang | Kamar kos 4 × 5 m dan lorong modular 12–18 m; 2–3 titik persembunyian. |
| Mood | Kelabu lembap, fluorescent berkedip pelan; distorsi kecil setiap kali kembali ke kamar. |
| Objective utama | Ambil buku kuliah, identifikasi slip lembur, buka lorong; ketika monster hadir, pilih menghadapi atau bersembunyi. |
| Tutorial | WASD/mouse, E, sprint, crouch, checkpoint. |
| Batas desain | Monster tidak menyerang dengan kekerasan eksplisit; encounter singkat 60–90 detik. |
| Checkpoint | Awal kamar kos; setelah interaksi slip lembur; sebelum encounter monster. |

#### Urutan scripted beats

1. C1-01 — Bangun di kos. Pemain memeriksa jam, buku, dan meja; foto/objek yang berkaitan dengan Ella tampak sedikit bergeser.
1. C1-02 — Pintu kos menuju lorong; ketika kembali, posisi salah satu benda berubah (mini loop, maksimal 2–3 pengulangan tanpa puzzle memusingkan).
1. C1-03 — Slip lembur/komentar kesalahan di tempat kerja ditemukan; lampu berkedip dan monolog Raka menguat.
1. C1-04 — Manifestasi siluet muncul di ujung lorong. Pemain belajar lari, mengambil cover, atau memukulnya saat mendekat.
1. C1-05 — Jika Fight: satu pukulan scripted, monster tampak identik dengan postur Raka; jika Hide: monster lewat dan menghilang. Keduanya melanjutkan ke transisi.
1. C1-06 — Kamera menghitam; Raka bangun di kos dengan ponsel yang menyala (arah ke Chapter 2).
#### Tabel event & implementasi

| Event | Trigger | Output / flag |
| --- | --- | --- |
| C1_BookPickup | E di buku | objective.book=true; tutorial interaksi selesai. |
| C1_LoopAdvance | Player melewati ambang pintu | loopIndex naik sampai max 3; prop state berubah. |
| C1_MonsterEncounter | Baca slip lembur + buka lorong | aktifkan monster AI + audio layer. |
| C1_ResolveFight | Klik kiri saat radius encounter | ch1Response=Fight; cinematic clip; lanjut. |
| C1_ResolveHide | Player di hide volume hingga monster pergi | ch1Response=Hide; lanjut. |
| C1_Respawn | Tertangkap monster | kembalikan checkpoint dan state encounter sementara. |

#### Petunjuk opsional

- CLUE_01: Foto Raka–Ella dengan tulisan kecil “wisuda bareng ya” di belakangnya.
- CLUE_02: Slip lembur/tanda jam pulang larut, menjelaskan kelelahan malam kejadian.
### 4.2 Chapter 2 — Bargaining & Depression: “Hari yang Terasa Berat” (±8–11 menit)

| Elemen | Spesifikasi |
| --- | --- |
| Ruang | Kamar kos transisi ke desk-study, lorong kampus pendek, dan zona kerja simbolik (boleh modul reuse). |
| Mood | Warna desaturasi dan biru dingin; ambient lebih senyap, napas lebih jelas. |
| Objective utama | Pilih mengirim pesan kepada Ella, kerjakan tiga tugas mini, temukan petunjuk yang memperdalam misteri. |
| Kegagalan | Jika semua tugas gagal/ditinggalkan, cutscene Raka kembali berbaring seharian; lanjut Chapter 3, tidak softlock. |
| Checkpoint | Setelah pilihan WhatsApp; setelah penyelesaian rangkaian tugas. |
| Interaksi pilihan | Kirim pesan / simpan sebagai draft (tercatat, bukan moral score). |

#### Urutan scripted beats

1. C2-01 — Raka melihat kontak Ella. Pilih “Kirim pesan” atau “Kunci layar”. Jika kirim, centang satu ditampilkan dan tidak pernah berubah.
1. C2-02 — Tugas A: rapikan catatan kuliah dalam urutan 3 item (drag atau klik urut). Petunjuk menyebut hari menjelang kecelakaan.
1. C2-03 — Tugas B: pilih file yang benar untuk berkas magang dari tiga dokumen berbeda; memberi konteks tekanan akademik.
1. C2-04 — Tugas C: cek jadwal kerja dan deadline, lalu tandai satu prioritas; desain tidak menjadikan Raka “malas” jika gagal.
1. C2-05 — Seiring tugas, kontrol dan suara menjadi lebih berat. Jika gagal ketiganya, fade ke kasur dan perlihatkan waktu berlalu; jika sebagian berhasil, lanjut melalui koridor tanpa celebratory success.
1. C2-06 — Ponsel bergetar dengan pesan Bayu (bukan jawaban Ella). Raka melihat kembali koridor, mendengar suara monster dari Chapter 1.
1. C2-07 — Transisi halus menuju ruang konfrontasi Chapter 3.
#### Tabel tugas & feedback

| Task ID | Aturan validasi | Jika gagal | Jika berhasil |
| --- | --- | --- | --- |
| TASK_A_NOTES | Pilih 3 item dalam urutan yang benar. | Catatan dibiarkan; monolog tertekan. | Teks “Sudah tersusun.” |
| TASK_B_INTERNSHIP | Pilih dokumen magang dengan nama+tanggal tepat. | File disimpan salah; muncul koreksi ringan. | File siap dikirim. |
| TASK_C_SCHEDULE | Pilih prioritas pada jadwal bentrok. | Deadline dilewati; jam bergerak. | Jadwal tersusun sementara. |

#### Petunjuk narasi dan emosi

- CLUE_03: Chat lama “jangan lupa, kita wisuda bareng” dengan tanggal semester lima; menguatkan motif surat.
- CLUE_04: Rekaman suara singkat pascakecelakaan yang ambigu (“Ella ... masih ...” tertutup interferensi); tidak membocorkan twist.
- Chat “centang satu” bukan bukti seseorang meninggal, hanya status pesan yang tidak terkirim/diterima.
- Sistem tugas mendukung pemain yang kesulitan: no hard timer pada mode Story; tidak menghukum pemain dengan restart puluhan kali.
### 4.3 Chapter 3 — Acceptance: “Hadapi atau Menjauh” (±5–7 menit)

| Elemen | Spesifikasi |
| --- | --- |
| Ruang | Lorong panjang yang berubah menuju ruang kosong dengan monster menunggu di titik akhir. |
| Mood | Kontras hangat–dingin, lampu jauh, bentuk monster semakin menyerupai Raka. |
| Objective | Dekati siluet dan lakukan tindakan akhir: Fight, Hide, atau Approach. |
| Pertarungan | Fight adalah respons satu beat scripted; jangan bangun health bar atau boss combat. |
| Checkpoint | Tepat sebelum ruang pilihan; pemain dapat mengulang pilihan selama belum final. |
| Revelation visual | Saat Approach: bayangan menyatu/hilang, terdengar napas Raka yang kembali stabil, fade ke lompatan waktu wisuda. |

#### Urutan scripted beats

1. C3-01 — Bayangan muncul kembali; lingkungan menggabungkan motif kos, jalan kecelakaan, dan lorong kampus.
1. C3-02 — Player berhenti di zona final. Tiga respons dikenali melalui aksi diegetik: klik kiri memukul, crouch di cover untuk Hide, berjalan mendekat untuk Approach.
1. C3-03 — Encounter membekukan input sesaat setelah aksi valid; simpan ch3Response dan tentukan ending secara deterministik.
1. C3-04 — Approach mengarah ke Good Ending (kelulusan); Fight/Hide mengarah ke Bad atau Hidden sesuai pengumpulan petunjuk.
1. C3-05 — Semua ending punya fade, audio transisi, caption, dan pilihan kembali ke menu / credits.
#### Umpan balik yang tidak menghakimi

Hindari pesan “jawaban benar” dan “jawaban salah”. Presentasikan tindakan sebagai cara Raka merespons perasaan; Good Ending boleh menunjukkan bahwa ia mulai menerima dukungan, bukan sembuh instan. Pada ending tragis, jangan membuat kesan bahwa keputusan bersembunyi pada satu momen menyebabkan bunuh diri secara langsung.

### 4.4 Cutscene penutup

| Ending | Rangkaian visual & dialog | Durasi |
| --- | --- | --- |
| GOOD — “Selamat” | Time skip: Raka menata belajar dan perlahan memperbaiki akademiknya hingga wisuda di senja dengan hujan ringan; teman-teman yang dulu kurang peduli serta keluarga mengucapkan selamat; Raka menerima surat tulisan Ella; close-up tulisan “Selamat wisuda, Ka...” lalu fade. | ±90–150 dtk |
| BAD — “Gk Selamat Wle” | Kamar kos sunyi, hujan deras, HP di atas meja. Raka tidak ditampilkan melakukan tindakan apa pun; pesan Bayu: “Ka, lu di mana?”, “Lu kenapa ga masuk grafkom tadi?”, “Pak dosen nanyain lo”. Pemain mengerti akhir tragisnya melalui absensi dan framing lingkungan. | ±50–80 dtk |
| HIDDEN — “The Truth” | Sajian kamar senyap yang mirip Bad Ending, lalu pesan Bayu: “Ka, lu di mana?” dan “Si Ella udah sadar”. Musik terhenti; kamera menetap pada ponsel. Konfirmasi hidupnya Ella mengubah makna seluruh ingatan. | ±55–85 dtk |

Penting: hindari menampilkan metode bunuh diri, alat, atau tubuh secara eksplisit. Penutup tragis boleh disampaikan melalui absensi Raka, lingkungan, dan respons teman. Awali game dengan content notice yang akurat serta sertakan informasi dukungan krisis yang relevan pada credits/menu bantuan.

## 5  Branching system, flags & state machine

### 5.1 Aturan ending — spesifikasi implementasi v1.1

Aturan yang disarankan berikut mengutamakan pilihan klimaks yang sudah ditentukan tim: Approach menghasilkan Good. Hidden tetap memiliki rangka kejadian tragis seperti Bad, tetapi membuka plot twist setelah pemain mengumpulkan bukti yang cukup. Pilihan WhatsApp dan tugas Chapter 2 mewarnai perjalanan tanpa menjadi “skor kesehatan mental”.

| Prioritas | Kondisi | Hasil |
| --- | --- | --- |
| 1 | ch3Response == Approach | GoodEnding → transisi wisuda + surat Ella. |
| 2 | ch3Response == Fight atau Hide DAN semua 4 clue ditemukan | HiddenEnding → penutup tragis + pesan “Si Ella udah sadar”. |
| 3 | ch3Response == Fight atau Hide DAN clue kurang dari 4 | BadEnding → penutup tragis + pesan ketidakhadiran Bayu. |

Ini aturan desain yang ditetapkan agar sistem dapat dikodekan; dapat diubah ketika playtest. Hindari menghitung “nilai depresi”, moralitas, atau kesehatan mental sebagai statistik permainan.

### 5.2 GameState yang disimpan

| Field / tipe | Nilai | Pemakaian |
| --- | --- | --- |
| currentChapter : enum | Prologue, Chapter1, Chapter2, Chapter3, Ending | Pemulihan progress dan pengaktif root chapter. |
| chapter1Response : enum | None, Fight, Hide | Variasi monolog dan sound cue. |
| messageEllaSent : bool | true/false | Variasi UI WhatsApp / dialog di Chapter 2. |
| taskStatus : enum[3] | NotStarted, Succeeded, Failed | Variasi urutan montage, bukan penentu kematian. |
| collectedClueIds : HashSet<string> | CLUE_01 s/d CLUE_04 | Syarat Hidden Ending; persist antar chapter. |
| chapter3Response : enum | None, Fight, Hide, Approach | Pemicu ending utama. |
| checkpointId : string | mis. C2_AfterPhone | Respawn tanpa mengulang sequence terselesaikan. |
| endingId : enum | None, Good, Bad, Hidden | Menyetel sequence penutup yang sesuai. |

### 5.3 Pseudocode ending resolver

EndingId ResolveEnding(GameState state) {

if (state.Chapter3Response == Response.Approach)

return EndingId.Good;

bool rejected = state.Chapter3Response == Response.Fight

|| state.Chapter3Response == Response.Hide;

if (rejected && state.HasAllRequiredClues())

return EndingId.Hidden;

if (rejected)

return EndingId.Bad;

return EndingId.None; // Choice not made yet

}

### 5.4 Syarat konsistensi naratif

- Konten chapter tidak pernah mengatakan objektif “Ella meninggal” sebagai fakta. Gunakan “Raka percaya Ella sudah tiada”.
- CLUE_04 harus cukup ambigu agar Hidden Ending tetap mengejutkan dan tidak terasa menipu pemain.
- CLUE_01 dan CLUE_03 menjelaskan surat sebagai janji jauh sebelum wisuda, bukan surat yang tiba dari dunia gaib.
- Pengumpulan clue tidak mengubah Raka menjadi “lebih sembuh” secara mekanis; hanya memicu pengungkapan informasi pada cabang non-Approach.
- Jika pemain melewatkan semua petunjuk, Bad Ending tetap dapat dicapai tanpa risiko softlock.
- Jika player menyentuh trigger Fight/Hide/Approach bersamaan, satu respons pertama yang valid dikunci secara atomik.
## 6  Art direction: atmosfer setara referensi, scope tetap realistis

### 6.1 Target visual

Target artistik adalah kesan sinematik, realistis, lembap, kotor, dan menekan yang diasosiasikan dengan Silent Hill 2 Remake: komposisi lorong sempit, kedalaman kabut, tekstur lapuk, pencahayaan berlapis, refleksi lantai basah, serta audio yang menimbulkan antisipasi. Ini adalah REFERENSI KUALITAS ATMOSFER, bukan target menyamai seluruh kualitas AAA, jumlah aset, animasi, atau fotorealisme karakter. Dibutuhkan karya visual dan desain lingkungan yang orisinal; jangan menyalin karakter, peta, logo, suara, musik, tekstur, atau mesh game tersebut.

### 6.2 Keputusan render pipeline

| Komponen | Keputusan awal | Catatan implementasi |
| --- | --- | --- |
| Unity Editor | Unity 6.x versi LTS/versi stabil yang tersedia dan disepakati tim. | Kunci versi editor di dokumentasi dan source control. |
| Render Pipeline | HDRP untuk target PC menengah–atas dengan GPU dedicated. | Lakukan benchmark vertical slice; URP adalah fallback jika hardware tidak mendukung HDRP. |
| Lighting | Mixed/baked untuk geometri statis + lampu dinamis penting. | Prioritaskan arah cahaya dan kualitas shadow. |
| Atmosfer | Fog/volumetric bila performa cukup; kartu kabut/particle sebagai fallback. | Jangan mengandalkan kabut untuk menutupi seluruh kekurangan aset. |
| Refleksi | Probe + screen-space reflection selektif untuk lantai basah. | Ray tracing bukan syarat MVP. |
| Pasca-proses | Tonemapping, exposure, color grading, film grain halus, vignette. | Motion blur dan intensitas grain dapat diatur pemain. |
| PBR | Albedo, normal, roughness/smoothness, AO, metallic sesuai kebutuhan. | UV dan skala material konsisten. |

### 6.3 Peta mood dan warna

| Area | Palette (arah, bukan absolut) | Key light / material | Audio |
| --- | --- | --- | --- |
| Prolog | Biru malam, abu hujan, highlight amber lampu kendaraan. | Pantulan aspal basah, headlight bloom terkendali. | Hujan stereo, helm, percakapan intim. |
| Chapter 1 | Abu-abu kusam / hijau sakit, aksen merah sangat terbatas. | Fluorescent, cat dinding lembap, besi karat, debu. | Buzz lampu, langkah, kipas, napas. |
| Chapter 2 | Biru dingin, desaturasi, gelap perlahan. | Ruang kerja dan meja belajar, ponsel menjadi fokus. | Ketikan, notifikasi, jam, ambience jauh. |
| Chapter 3 | Hitam + kontras hangat samar. | Siluet backlit, fog layer bertingkat. | Detak jantung, motif audio prolog. |
| Good Ending | Amber senja, hujan lembut, kulit dan kain natural. | Rim light, langit redup, depth lembut. | Rintik, suara orang, ambient hopeful. |
| Bad/Hidden | Biru-hitam malam, kilatan dari luar. | Satu area terang untuk ponsel. | Hujan deras, getar ponsel, jeda sunyi. |

### 6.4 Quality bar yang dapat dicapai tim kecil

- Bangun satu kamar kos yang sangat detail (hero room), satu koridor modular, satu set dekora­si jalan hujan yang pendek; duplikasi dengan perubahan lighting/props untuk dunia psikologis.
- Fokus PBR pada 12–20 prop hero: meja, kasur, buku, ponsel, jam, slip lembur, helm, surat, frame foto, payung, pintu, lampu.
- Gunakan decals noda air, jamur, cat terkelupas, kertas berserakan untuk kaya detail tanpa menambah banyak mesh unik.
- Fotorealisme lebih sering ditentukan kombinasi exposure, skala, komposisi, light placement, roughness, dan material yang rapi dibanding jumlah poligon tinggi.
- Jangan mengejar “sama persis seperti Silent Hill 2 Remake”. Target realistis adalah atmosfer terinspirasi, dengan 1 ruangan sekelas showcase vertikal, bukan game AAA penuh.
### 6.5 Kamera, motion, dan komposisi

- Ketinggian kamera dasar ±1,65 m; pintu ±2,05 m; tinggi plafon kamar ±2,7 m agar terasa masuk akal.
- Key light membentuk rute; main object berada di zona fokus, bukan sekadar ikut highlight kuning game UI.
- Siluet di chapter 1 jelas terbaca saat terpisah dari background; stage 3 memprioritaskan shape language monster yang menyerupai postur Raka.
- Lantai basah dibuat dengan roughness bervariasi dan decal puddle; hindari menjadikan semua permukaan seperti cermin.
- Jarak kabut dipilih supaya fokus maju 5–15 m tetap terbaca; ukur dalam Game View dengan post-processing aktif.
## 7  Asset bible & workflow Blender → Unity

### 7.1 Asset list prioritas dan spesifikasi

| ID | Aset | Prioritas | LOD / detail awal |
| --- | --- | --- | --- |
| ENV_001 | Kos modular: lantai, plafon, dinding, kusen, pintu | P0 | Modular grid 1 m; lightmap UV. |
| ENV_002 | Lorong modular + pipa/kabel/tangga | P0 | Segmen 2–4 m yang dapat dipasang berulang. |
| PROP_001 | Kasur, bantal, selimut, meja, kursi, kipas | P0 | Medium detail dengan tekstur PBR. |
| PROP_002 | Buku, pulpen, kertas, slip lembur, laptop | P0 | Hero props close-up readable. |
| PROP_003 | Ponsel Raka + UI WhatsApp fiktif | P0 | Mesh sederhana + render-texture/UI. |
| ACT_001 | ACT_Shadow_Raka (monster hero) + rig/8 animasi | P0 (placeholder T06; hero T13) | Humanoid terdistorsi realistis; muka samar, gurat luka/bercak darah ringan, chest crack, kertas basah. |
| PROP_004 | Foto Ella, helm, payung, jam, amplop surat | P1 | Beri storytelling detail, text legible. |
| ENV_003 | Jalan pendek saat prolog / ambience hujan | P1 | Dapat berupa scene audio-only/shot sinematik. |
| ENV_004 | Area wisuda: podium / backdrop / kursi | P1 | 1 komposisi kamera sinematik; crowd sederhana. |
| VFX_001 | Kabut, droplet, asap, decals, wetness | P1 | Gunakan native HDRP + VFX ringan. |
| ACT_002 | Figur latar keluarga/teman saat wisuda | P2 | Stylized background / silhouette; tidak wajib full rig. |

### 7.1A  Monster hero — ACT_Shadow_Raka (final concept v1.1)

Tujuan naratif: manifestasi rasa bersalah, kelelahan, ketakutan, dan kemarahan Raka; bukan iblis generik. Sosok tampak mengancam dari jarak jauh, tetapi saat dekat menyisakan ciri manusia agar klimaks Chapter 3 terasa seperti menghadapi diri sendiri. Ini konsep artistik v1.1 dari diskusi tim; angka modeling adalah anggaran awal untuk diuji.

Gambar konsep: referensi mood, proporsi, bahan, dan siluet; bukan aset 3D siap pakai. Desain final tetap harus diinterpretasikan serta diperbaiki di Blender.

| Komponen | Spesifikasi visual (design lock) | Batasan produksi |
| --- | --- | --- |
| Siluet & proporsi | Humanoid terdistorsi, tinggi tampak ±2,25–2,45 m, membungkuk; lengan memanjang dan asimetris; tangan kurus. | Proporsi harus lolos pintu kos/koridor, collision tidak mencubit pemain. |
| Wajah & luka | Wajah samar tertutup rambut basah/gelap. Varian A: gurat luka ringan; varian B: lebih banyak lecet superfisial dan noda darah gelap di pipi/pakaian, tetap tidak grafis. | TANPA luka terbuka grafis, organ, mutilasi, cipratan darah berlebihan atau jumpscare close-up berkepanjangan. |
| Bahan & motif | Material PBR hitam arang/obsidian basah, kain jaket mahasiswa rusak, tempelan catatan/tugas kuliah, retakan dada dengan kilasan cahaya redup seperti lampu truk. | Jangan menyalin desain karakter franchise lain; jangan memakai transparansi kompleks di seluruh tubuh. |
| Identitas emosional | Jauh = siluet asing; mendekat = gestur, tinggi, atau bagian wajah menyerupai Raka. Pada Approach ia melemah/menghilang, bukan mengeluarkan darah. | Pengungkapan kemiripan visual dilakukan bertahap, bukan diperlihatkan sepenuhnya sejak Chapter 1. |
| FPP staging | Penampakan 8–12 m melalui kabut, balik pintu, atau ujung lorong; penyikapan dekat 2–3 m saat beat cerita yang terkontrol. | Siluet harus jelas terbaca; pemain tidak dipaksa menatap detail wajah terlalu lama. |

#### 7.1A.1  Blender → Unity: deliverables monster

- T06 menggunakan monster placeholder yang sudah dapat dikejar, dihindari, dan diselesaikan dengan Fight. T13 baru mengganti visual dengan hero asset; jangan menghapus logika dan checkpoint T06.
- Di Blender: simpan ACT_Shadow_Raka.blend dengan topology deformable, UV dan material terpisah secukupnya; tinggi berbasis meter, origin di telapak kaki, transforms applied, armature bernama jelas.
- Gunakan rig Generic (paling aman untuk proporsi nonmanusia); jangan memaksa Humanoid retarget. Animasi minimum: Idle_Breath, Walk_Lurch, Pursue, Search_Cover, Fight_Reaction, Hide_Pass, Approach_Reveal, Despawn. Root motion nonaktif mula-mula.
- Export FBX + texture set, impor ke Unity HDRP, remap material dan Animator, buat prefab ACT_Shadow_Raka; tautkan state AI dari T06 dan Approach Trigger T11. Jangan membuat ulang ending logic.
- Budget uji: LOD0 ±20–40 ribu triangles, tekstur utama hingga 2K, LOD1 bila profiling memerlukan. Pilihan akhir harus mengikuti hasil profiler build Windows, bukan angka sebagai syarat kaku.
- Tolak ukur: 8–12 m tetap terbaca sebagai siluet, pada 2–3 m detail luka ringan terlihat tanpa grafis eksplisit; tidak T-pose, clipping, gliding, material unassigned atau perubahan hitbox saat animasi.
#### 7.1A.2  Behaviour dan tingkat ancaman menurut chapter

| Momen | Kemunculan | Respon pemain / AI |
| --- | --- | --- |
| Chapter 1 | Sekilas di ujung koridor, silhouette first; audio langkah berjarak, lampu berkedip. | Chase singkat: Fight kontekstual atau Hide di cover. Jika tertangkap reset checkpoint. |
| Chapter 2 | Muncul tidak langsung melalui bayangan, bunyi atau motif visual; tidak menghalangi tiga tugas. | Tidak ada boss combat; tekanan naratif melalui ambience dan pergerakan. |
| Chapter 3 | Sosok menunggu di ruang pilihan, bagian wajah/gestur sedikit lebih manusiawi. | Fight/Hide/Approach. Pada Approach sosok menghilang/menyatu, lalu Good Ending. |

Rujukan untuk Codex: Monster placeholder = T06; artistik monster Blender + FBX + rig + Animator + prefab = T13; konsistensi perilaku akhir = T11. Semua ID tersebut identik di Markdown.

### 7.2 Konvensi produksi aset

- Unit Blender 1 unit = 1 meter; apply transforms, origin logis (pintu pada engsel, meja pada pusat lantai).
- Nama objek: ENV_Corridor_Wall_A, PROP_Desk_01, ACT_Shadow_Raka; nama file tanpa spasi dan tanpa karakter spesial.
- Prioritaskan FBX bila rig/animasi Unity memerlukannya; glTF 2.0 untuk pertukaran PBR statis bila kompatibilitas proyek diuji. Simpan file master .blend.
- Setiap aset punya UV0 tidak overlap untuk material, dan UV2 non-overlap bila menggunakan baked lighting/lightmaps.
- Buat varian LOD hanya jika aset benar-benar mahal dan terlihat jauh; koridor tertutup sering lebih diuntungkan oleh occlusion daripada LOD kompleks.
- Skala awal tekstur hero 2K bila perlu close-up, prop biasa 1K, clutter 512–1K; prioritaskan penggunaan material atlas untuk objek repetitif.
- Tekstur harus memiliki lisensi jelas; gunakan karya tim, situs asset berlisensi, atau procedural material buatan sendiri. Jangan menyalin aset Silent Hill.
### 7.3 Quality control aset sebelum import

1. Pastikan ukuran relatif terhadap dummy manusia 1,7 m benar dan pivot sesuai kebutuhan gameplay.
1. Hapus geometri tak terlihat, perbaiki normals dan non-manifold yang memunculkan artefak.
1. Bake normal/AO dari high-poly ke low-poly jika ada; cek peta roughness/smoothness saat diaplikasikan di shader Unity.
1. Ekspor dengan nama baku; lakukan import pada project staging, periksa material, orientasi sumbu, transform, dan collider.
1. Pasang prefab dan uji interaksi E; jangan serahkan asset tanpa uji di Game View.
1. Catat file asli .blend, file ekspor, folder material, lisensi sumber, serta screenshot preview asset.
### 7.4 Tujuan penggunaan Blender MCP

Blender MCP tepat untuk pekerjaan repetitif dan terukur: memodelkan meja/kursi/pintu, membuat material prosedural, melakukan rename, menerapkan bevel/modifier, menata UV, membuat beberapa varian prop, serta ekspor batch. Untuk aset hero yang sangat diperhatikan kamera (monster, tangan, surat, wajah karakter), tetap lakukan inspeksi artistik dan perbaikan manual karena hasil otomatis dapat tidak konsisten.

## 8  Arsitektur teknis Unity & struktur project

### 8.1 Scene architecture

Scenes/

Boot.unity               # initialize services/save/settings

MainMenu.unity           # start / continue / settings / credits

MasterGameplay.unity     # single gameplay scene with chapter roots

Credits.unity            # credits and support resources

MasterGameplay Hierarchy:

_Systems (GameManager, Narrative, Audio, Save, UI)

Player (CharacterController, Camera, Interactor)

PrologueRoot

Chapter1Root (Kos, Corridor, Monster, Triggers)

Chapter2Root (Kos variant, Phone, Tasks, Clues)

Chapter3Root (Confrontation, ChoiceTriggers)

EndingsRoot (GoodSet, BadSet, HiddenSet)

GlobalVolume / LightingRigs / AudioZones

MasterGameplay mempertahankan satu alur gameplay terintegrasi sesuai konsep awal PPT. ChapterRoot dapat diaktif/nonaktifkan dan ruang diposisikan terpisah untuk menghindari light bleeding; opsi pemecahan menjadi additive scenes disediakan jika kebutuhan memori atau tim membesar.

### 8.2 Folder convention

Assets/_NoRainbowAfter/

Art/{Models,Materials,Textures,Decals,Animations,Prefabs}

Audio/{Ambience,SFX,Voice,Music,Mixers}

Data/{ScriptableObjects,Localization,Dialogue,Clues}

Scenes/  Scripts/{Core,Player,Interaction,Narrative,AI,UI,Editor,Tests}

UI/{Prefabs,Fonts,Sprites}  VFX/  Settings/  ThirdParty/

Documentation/{GDD,Tasks,Licenses,PlaytestLogs}

### 8.3 Komponen C# utama

| Class / interface | Tugas | Dependency |
| --- | --- | --- |
| GameManager | Bootstrapping, chapter transition, pause, global state. | GameState, ChapterDirector. |
| GameState | Data runtime flags, clue set, task status, active chapter. | Plain C# serializable. |
| SaveService | Persist/read state versioned JSON atau persistent data. | GameState, checkpoint IDs. |
| FirstPersonMotor | Move, sprint, crouch, stamina, headbob adapter. | CharacterController, Input System. |
| PlayerInteractor | Raycast from camera, prompt, dispatch E action. | IInteractable, UI. |
| IInteractable | Interface Focus/Interact, data display name/id. | InteractionData. |
| NarrativeDirector | Menjalankan beat, monolog, subtitle, cinematic events. | DialogueData, AudioDirector. |
| ChapterDirector | Set root active, lock input, stage objective guards. | GameManager, ObjectiveTracker. |
| ObjectiveTracker | Mencatat objective dan completion flags. | GameState. |
| ShadowEncounterController | Patrol/chase/hide/fight scripted; checkpoint. | AI Navigation atau trigger sederhana. |
| PhoneUIController | Menyimulasikan kontak Ella & Bayu dan centang satu. | GameState, UI. |
| AcademicTaskController | Task A/B/C, status fail/success, montage. | GameState. |
| ClueRegistry | Collect, deduplicate, persist 4 clue IDs. | GameState. |
| EndingResolver | Pilih Good/Bad/Hidden berdasarkan aturan deterministik. | GameState. |
| AudioDirector | Mixer snapshots, 3D cue, layered rain & tension. | AudioMixer. |
| SettingsService | Volume, subtitles, FOV, camera effects, graphics preset. | UI, Player, Pipeline. |

### 8.4 State machines utama

ChapterDirector:

Boot -> Prologue -> Chapter1 -> Chapter2 -> Chapter3 -> Ending

Chapter1 Shadow:

Dormant -> Spawn -> Pursue -> [FightResolved | HideResolved | Caught]

Caught -> CheckpointRestore -> Pursue

Chapter2 Tasks:

PhoneDecision -> TaskA -> TaskB -> TaskC -> [DayComplete | BedMontage]

Chapter3:

Confrontation -> ChoiceLocked -> EndingResolve -> EndingSequence -> Credits

### 8.5 Sistem scriptable data (agar Codex tidak hardcode)

- ScriptableObject ChapterDefinition: chapterId, root, startingCheckpoint, objective list, audio snapshot, light rig, entry beat.
- ScriptableObject InteractableDefinition: ID, displayName, prompt verb, clue ID (opsional), completion action.
- ScriptableObject NarrativeBeat: trigger ID, subtitle line IDs, audio clips, blocking control, fade, camera target.
- ScriptableObject AudioCue: ID, clip, mixer bus, spatial blend, priority, cooldown.
- ScriptableObject EndingDefinition: enum id, final sequence, message content, ending text, credit transition.
- Unit testing logika ending tidak harus mengaktifkan scene; berikan GameState fixture dan assert hasil resolver.
### 8.6 Dependensi teknis & keputusan keamanan

- Gunakan Unity Input System untuk aksi baru; Unity TextMeshPro atau UI Toolkit/uGUI yang konsisten untuk menu dan ponsel; jangan mencampur tiga sistem UI tanpa alasan.
- HDRP + Volume + Shader Graph untuk efek visual. Cinemachine/Timeline hanya bila diperlukan cutscene; jangan membuat sistem kamera custom yang rumit sebelum prototipe stabil.
- AI Navigation/NavMesh hanya untuk chase bila navigasi dinamis diperlukan; bila ruang sempit scripted, waypoint/StateMachine lebih stabil dan mudah diuji.
- MCP/agent tidak boleh menjalankan delete massal, package upgrade, asset-import dari internet, atau file system write di luar project tanpa review. Simpan commit Git sebelum perubahan besar.
- Nomor versi project dan dependensi dikunci setelah vertical slice; perubahan Unity/HDRP memerlukan branch tersendiri dan smoke test.
## 9  Pipeline Codex ↔ Unity MCP ↔ Blender MCP

### 9.1 Tooling dan prasyarat

Pada 2026 tersedia plugin resmi Unity untuk Codex berbasis skills/CLI, serta Unity MCP Server yang dapat menghubungkan agen kompatibel MCP ke Editor. Pilih salah satu jalur koneksi yang didukung versi terpasang dan verifikasi tool yang benar-benar tersedia; plugin skills sendiri tidak otomatis berarti koneksi MCP Editor sudah aktif. Blender MCP adalah integrasi pihak ketiga (bukan resmi Blender Foundation); perlakukan izin menjalankan Python di Blender sebagai akses penuh ke sesi itu.

| Komponen | Yang harus disiapkan | Verifikasi minimum |
| --- | --- | --- |
| Unity 6 + HDRP | Install Editor & module Windows Build Support; buat project HDRP. | Editor buka project, scene demo render. |
| Codex + Unity plugin | Pasang plugin Unity resmi untuk Codex menurut dokumentasi terkini. | Skill Unity terbaca; project dapat dianalisis. |
| Unity MCP Server | Aktifkan bridge/relay melalui Unity AI atau jalur MCP/CLI yang tersedia. | Codex bisa membaca hierarchy + Unity Console. |
| Blender + Blender MCP | Pasang Blender, UV, add-on MCP pihak ketiga hanya dari repo tepercaya. | Codex bisa membuat kubus uji dan mendapatkan scene info. |
| Git + .gitignore | Versioning Assets/Packages/ProjectSettings & docs; abaikan Library/Temp/Obj. | Commit bersih sebelum setiap batch tugas. |
| Test build | Windows x64 build profile, quality presets. | Build launch dan input berfungsi. |

### 9.2 Alur kerja setiap permintaan ke Codex

1. Inspect: minta Codex membaca struktur project, Unity Console, scene hierarchy, dan dependensi yang tersedia terlebih dahulu.
1. Plan: pecah satu pekerjaan menjadi hasil kecil, sebutkan file/prefab/scene yang boleh disentuh dan acceptance criteria.
1. Implement: buat script + prefab/scene minimal; jangan menyelesaikan seluruh chapter dalam satu prompt besar.
1. Validate: compile, jalankan EditMode/PlayMode test jika ada, lalu Play Mode manual dan ambil catatan screenshot.
1. Review: cek perubahan git diff, console error, missing scripts dan serialized references; koreksi sebelum commit.
1. Commit: beri nama commit sesuai fitur, perbarui dokumentasi progress dan task acceptance status.
### 9.3 Katalog ID tugas tunggal (identik dengan Markdown v1.1)

| ID | Tugas (ID sama seperti MD v1.1) | Definition of Done / Gate |
| --- | --- | --- |
| T00 | Audit project & MCP (read-only) | Laporan kondisi dan batas akses aktual. |
| T01 | Bootstrap project, scene, Git | Boot -> MainMenu -> MasterGameplay -> Credits berjalan. |
| T02 | FPP Controller + input | Kontrol FPP stabil, collision benar, Console bersih. |
| T03 | Raycast interaction & inspection | Tidak menembus dinding; objek dapat diperiksa. |
| T04 | Chapter 1 greybox | Rute playable; tidak ada softlock. |
| T05 | Narrative director + corridor loop | C1-01 sampai C1-06 berlangsung runtut. |
| T06 | Monster placeholder AI + encounter | Fight, Hide, Caught dapat diuji tanpa gore. |
| T07 | HDRP visual vertical slice | Keterbacaan item dan FPS pada build Windows teruji. |
| T08 | Phone/chat Ella & Bayu | Kedua pilihan tersimpan, Ella tidak membalas. |
| T09 | Tugas akademik + montage | Setiap jalur dapat meneruskan ke Chapter 3. |
| T10 | Clue registry + foreshadowing | 4 clue unik tersimpan tanpa membocorkan twist. |
| T11 | Chapter 3 + EndingResolver | Approach Good; Fight/Hide + 4 clue Hidden; lainnya Bad. |
| T12 | Tiga ending + credits | QA09–QA11 lulus; tanpa representasi metode eksplisit. |
| T13 | Monster hero asset Blender -> Unity | Rig/animasi valid, tampak jelas pada FPP, performa terukur. |
| T14 | Blender prop pipeline & modular art | Aset Blender masuk Unity dengan orientasi/material benar. |
| T15 | Audio, subtitle, accessibility | Audio dan pilihan aksesibilitas berfungsi/persist. |
| T16 | Full QA, optimization, Windows submission | Windows x64 dapat tamat, ketiga ending dan dokumentasi siap. |

Kontrak tugas v1.1: T00–T16 (17 ID). T07 = HDRP visual, T08 = Phone UI, T11 = EndingResolver, T13 = MONSTER HERO Blender, T14 = properti Blender, T15 = audio, T16 = QA. T07 pada GDD v1.0 dahulu merujuk phone — ID lama dinyatakan tidak berlaku.

### 9.4 Protokol instruksi Codex untuk tiap task

Tiap task harus menyebut: konteks game, scene target, input-output, file yang boleh dibuat/diubah, pengecekan Unity Console, PlayMode atau test, dan daftar hasil. Gunakan checklist “stop if uncertainty” untuk langkah berisiko seperti menghapus folder, upgrade package, atau mengedit scene utama yang sedang dipakai anggota tim.

### 9.5 Setup referensi — jangan menyalin perintah tanpa cek versi

# Rujukan resmi: Unity Plugin for Codex (pilih jalur instal terbaru)

# Unity MCP: di Project Settings/AI jika versi Editor menyediakan

# Uji: "read scene hierarchy and latest console errors"

# Blender MCP komunitas (contoh dari dokumentasi repositori):

winget install --id=astral-sh.uv -e

uvx mcp-for-blender setup

# Atau pendaftaran manual melalui Codex CLI:

codex mcp add blender -- uvx mcp-for-blender

# Dalam Blender, aktifkan add-on MCP for Blender,

# lalu verifikasi dengan satu cube test sebelum membuat aset produksi.

Jangan menjalankan perintah instal pihak ketiga dari sumber yang tidak diperiksa. Blender MCP dapat mengeksekusi Python; pertahankan listener di localhost, backup .blend, dan review script sebelum dieksekusi. Verifikasi path dan UI sesuai versi perangkat masing-masing.

## 10  Audio direction, subtitle & rancangan dialog

### 10.1 Strategi suara

| Bus AudioMixer | Konten | Aturan |
| --- | --- | --- |
| Master | Seluruh suara. | Limiter ringan; pengaturan overall volume. |
| Voice | Raka, Ella, Bayu, monolog. | Prioritas paling jelas; duck ambience saat dialog. |
| Ambience | Hujan, kipas, fluorescent, lorong, jalan. | Layer loop 3D/2D; transisi halus. |
| SFX | Langkah, interaksi, pintu, notifikasi, pukul. | Variasi acak; tidak menyakitkan. |
| Music | Drone original, tekstur tonal, cue ending. | BGM minimal; diam adalah bagian desain. |

### 10.2 Cue sheet minimal

| Cue ID | Waktu / trigger | Arah suara |
| --- | --- | --- |
| AUD_PROLOGUE_RAIN | Scene prolog dimulai. | Hujan deras + mesin motor, voice di tengah. |
| AUD_CRASH | Raka kehilangan fokus. | Klakson + rem + benturan singkat + hening. |
| AUD_C1_SHADOW | Siluet spawn. | Low rumble, bisikan samar, detak jantung. |
| AUD_C2_PHONE | Ponsel dibuka. | Tap layar, sent message, satu notifikasi. |
| AUD_C2_WEIGHT | Task ke-2/3 selesai atau gagal. | Ambience sempit, napas menonjol. |
| AUD_C3_APPROACH | Raka berjalan mendekati monster. | Drone melembut, napas sinkron. |
| AUD_GOOD_GRAD | Cutscene wisuda. | Ambience outdoor hangat + rintik hujan. |
| AUD_HIDDEN_PING | Pesan Ella sudah sadar. | Getar ponsel, lapisan musik berhenti. |

### 10.3 Contoh dialog kerja (placeholder, bukan naskah final)

Prolog — Ella: “Nanti kita wisuda bareng, ya. Jangan tidur dulu, Ka.”

Prolog — Raka: “Iya ... bentar lagi sampai.”

Chapter 1 — monolog Raka: “Aku cuma harus berangkat. Kalau terus bergerak, semuanya akan baik-baik saja.”

Chapter 2 — pesan Raka: “El, maaf. Aku masih pengen ngobrol.” [satu centang]

Chapter 3 — Approach: “Aku enggak bisa mengulang malam itu. Tapi aku masih di sini.”

Good Ending — surat Ella: “Selamat wisuda, Ka. Aku tahu kamu bakal sampai di sini.”

Bad Ending — chat Bayu: “Ka, lu di mana? Lu kenapa ga masuk grafkom tadi? Pak dosen nanyain lo.”

Hidden Ending — chat Bayu: “Ka, lu di mana? Si Ella udah sadar.”

Subtitle perlu timing dan speaker name. Naskah di atas contoh agar implementasi bisa dimulai; dialog final tetap milik tim penulis. Gunakan voice yang dibuat/diizinkan, tidak meniru suara aktor/game lain.

## 11  UI/UX, accessibility & save flow

### 11.1 Daftar antarmuka

- Main Menu: New Game, Continue (aktif jika save ada), Settings, Credits, Quit.
- HUD minimal: crosshair (opsional), prompt “[E] Baca / Ambil / Buka”, satu objective line jika pemain lama diam.
- Phone UI: layar kontak, chat Ella, chat Bayu, status pesan satu centang, tombol back; gunakan merek fiktif bila perlu.
- Document Inspect: perbesaran item dan overlay teks readable; tidak memaksa pemain membaca tekstur 3D yang terlalu kecil.
- Pause/Settings: volume master/voice/ambience/music, sensitivity, FOV, subtitles on/off, reduced camera shake/flashes, quality preset.
- Content Notice: ringkas dan netral, bisa dilewati setelah dibaca; gunakan bantuan atau hotline yang diverifikasi untuk wilayah rilis.
- End Card: nama ending, Continue to Credits, Main Menu; opsi replay chapter baru setelah fitur checkpoint siap.
### 11.2 Save/checkpoint & anti-softlock

- Simpan setelah menyelesaikan masing-masing chapter dan checkpoint besar; save file diberi schemaVersion untuk migrasi.
- Tidak menyimpan posisi pemain saat sedang dikejar; simpan checkpoint ID dan spawn position yang aman.
- CLUE yang dikumpulkan bersifat monotonic dan tidak hilang saat respawn checkpoint.
- UI Esc berfungsi di semua menu dan menutup overlay dahulu sebelum pause global.
- Semua objective punya fallback: jika event tidak terpicu setelah durasi tertentu, tampilkan hint diegetik; jangan menipu progres quest.
## 12  Produksi, milestones & pembagian peran

### 12.1 MVP dan vertical slice

Vertical slice yang disarankan adalah Chapter 1 lengkap selama ±5–7 menit: kos detail, lorong, sistem interaksi, monster siluet, sprint/hide/fight, lighting HDRP, audio dan checkpoint. Setelah kualitas dan framerate terbukti, gandakan fondasi sistem untuk Chapter 2–3. Tidak direkomendasikan membuat seluruh model realistis sebelum motor/interaksi selesai.

| Fase | Output wajib | Gate menuju fase berikutnya |
| --- | --- | --- |
| M0 — Pre-production | GDD disetujui, design locks, repo Git, hardware test. | Versi Unity/HDRP dipilih dan anggota paham scope. |
| M1 — Prototype | FPP, E, kos greybox, corridor, encounter monster, checkpoint. | Bisa berjalan 5 menit tanpa error blokir. |
| M2 — Vertical Slice | Satu ruang hero realistis, chase/hide, audio, UI, HDRP lighting. | Kualitas visual mood disetujui & FPS stabil. |
| M3 — Alpha | Chapter 1–3 + semua flag + 3 ending placeholder. | 100% jalur narasi dapat ditamatkan. |
| M4 — Beta | Aset final prioritas, voice, VFX, subtitles, credits, QA. | Tidak ada blocker/high severity bugs. |
| M5 — Submission | Build Windows, README kontrol, demo video, GDD final. | Game berjalan di PC target + dokumentasi lengkap. |

### 12.2 Contoh sprint kerja 8 minggu (sesuaikan deadline kelas)

| Minggu | Fokus | Demo hasil |
| --- | --- | --- |
| 1 | Project setup, Git, HDRP baseline, FPP + interaction. | FPP bergerak di kamar greybox. |
| 2 | Looping kos, monster encounter, C1 objective. | Chapter 1 greybox selesai. |
| 3 | Blender hero props, HDRP lighting, audio slice. | Chapter 1 vertical slice polished. |
| 4 | Phone UI, task A/B/C, clue registry. | Chapter 2 dapat dimainkan. |
| 5 | Chapter 3 decision system, all endings logic. | 3 ending dapat dipicu. |
| 6 | Ending cutscene, surat, voice/subtitles, scene transitions. | Full playthrough alpha. |
| 7 | Asset polish, lighting passes, performance profiles. | Beta feature-complete. |
| 8 | Playtest, bugfix, Windows build, laporan & presentasi. | Submission candidate. |

### 12.3 Pembagian kerja berdasarkan rancangan tim sebelumnya

| Peran | Tanggung jawab inti | Bentuk handoff |
| --- | --- | --- |
| Programmer 1 — Core | FPP, stamina, crouch, interaksi, UI dasar, save/checkpoint, camera effects. | Prefab + script + scene test + README. |
| Programmer 2 — Logic | Monster AI, chapter events, phone/task systems, clue registry, ending resolver, audio event hooks. | Script + ScriptableObjects + automated tests. |
| Game Designer / 3D Artist | Lore, pacing, level layout, Blender props, lighting HDRP, audio/narasi, cutscenes, playtest. | Asset package + layout + narrative cue sheets. |

Peran berasal dari pembagian PPT awal dan perlu disesuaikan dengan kapasitas nyata tim. Codex membantu implementasi, bukan pengganti pemilik desain, art direction, playtest, dan review teknis.

## 13  QA plan & acceptance testing

### 13.1 Checklist minimum setiap fitur

- Fitur dapat dijalankan di Unity Play Mode tanpa exception merah, missing reference, atau input terkunci.
- Display dan audio tidak hilang setelah berpindah chapter; menu Pause/Resume tetap bekerja.
- Rute Fight, Hide, Approach dapat ditempuh dengan keyboard/mouse tanpa memaksa manipulasi inspector.
- Tidak ada clue ganda, jalur hidden dapat dicapai dan tidak aktif jika clue kurang.
- Fitur visual tetap dapat dipahami pada preset Low dan ketika motion blur / camera shake dimatikan.
- Minimal tiga penguji luar tim mampu memahami objective tanpa verbal walkthrough dari developer.
- Build Windows yang dibagikan direkam bersama versi Unity, resolusi, GPU, dan catatan bug.
### 13.2 Test case utama

| ID | Langkah / input | Expected |
| --- | --- | --- |
| QA01 | MainMenu→New Game→Prolog | Suara intro, subtitle, cut to kos. |
| QA02 | C1 ambil buku, interaksi koridor | Loop berubah dan objective tersimpan. |
| QA03 | C1 pilih Fight | Monster resolve, tidak jatuh ke geometry. |
| QA04 | C1 pilih Hide | Monster pergi dan chapter tetap lanjut. |
| QA05 | C1 tertangkap monster | Kembali checkpoint, flag clue aman. |
| QA06 | C2 kirim pesan Ella | Satu centang, state tersimpan. |
| QA07 | C2 gagal semua task | Bed montage, dapat lanjut Chapter 3. |
| QA08 | C2 kumpulkan semua clue; restart checkpoint | 4 clue unik masih tercatat. |
| QA09 | C3 pilih Approach | Good + wisuda + surat. |
| QA10 | C3 Fight, clue < 4 | Bad + pesan Bayu ketidakhadiran. |
| QA11 | C3 Hide, clue = 4 | Hidden + pesan Ella sudah sadar. |
| QA12 | Pause saat inspect/phone | Esc menutup overlay sesuai urutan. |
| QA13 | Save/load dari Chapter 2 | Task dan clue benar; tidak duplicate trigger. |
| QA14 | Windows build pada PC target | Tidak crash; full run sampai credits. |
| QA15 | Blender monster FBX masuk ke Unity, rig dan 8 animasi diuji lewat Animator. | Prefab valid; tanpa T-pose/clip; T06/T11 tetap berfungsi. |
| QA16 | Penampakan monster diuji dari FPP pada 8–12 m dan 2–3 m, fog dan preset Low/High. | Siluet terbaca; wajah samar + noda darah ringan tidak terlihat gore eksplisit; FPS diukur. |

### 13.3 Performance budget (target awal, bukan jaminan)

| Metrik | Target awal | Cara mengukur |
| --- | --- | --- |
| Resolusi | 1080p, High pada GPU dedicated kelas menengah. | Standalone Windows build. |
| Frame rate | Target 60 FPS pada PC uji yang disepakati; minimum stabil 30 FPS. | Unity Profiler / Frame Debugger + build capture. |
| Frame budget | 16,7 ms @ 60 FPS, 33,3 ms @ 30 FPS. | CPU/GPU frame time terpisah. |
| VFX/fog | Jangan memenuhi semua koridor dengan volumetric berkualitas ultra. | Uji fog on/off, render scale, shadow cost. |
| Drawcalls | Gunakan static batching/instancing bila cocok. | Profiler + Rendering Statistics. |
| Tekstur | Maks 2K hero prop saat perlu; kategori P1/2 lebih kecil. | Memory Profiler, mip maps, compression. |

### 13.4 Risiko & mitigasi

| Risiko | Dampak | Mitigasi |
| --- | --- | --- |
| Target grafis setara AAA | Scope dan GPU melebihi kapasitas kelas. | Prioritas satu hero room, lighting, reuse modular; turun ke URP jika hardware perlu. |
| MCP mengedit scene tanpa kontrol | Scene rusak, referensi hilang. | Git commit kecil, batas perubahan, test sesudah setiap prompt. |
| Asset Blender tidak seragam | Skala, material, UV dan orientasi kacau. | Naming, export preset, import checklist, prefab validation. |
| Ending terasa sewenang-wenang | Pemain merasa dihakimi. | Tanam foreshadowing, tampilkan pilihan dengan simbol yang konsisten. |
| Hidden Ending terlalu mudah ditebak | Twist hilang. | Petunjuk ambigu tetapi fair; lakukan blind playtest. |
| Cerita mental health sensitif | Pesan tidak tepat/menyakiti pemain. | Konten implisit, dukungan, konsultasi pembaca sensitif. |
| Fitur unfinished saat deadline | Build belum bisa tamat. | Freeze scope setelah alpha; polish hanya yang kritis. |

## 14  Prompt pack awal untuk Codex

Salin satu prompt pada satu waktu ke Codex ketika koneksi Unity MCP benar-benar aktif. Minta agent selalu melakukan inspeksi sebelum perubahan. Prompt lengkap dalam file pendamping CODEX_TASKS_No_Rainbow_After_v1_1.md.

### T00 — Audit proyek (read-only)

You are implementing the Unity 6 game "No Rainbow After". Before editing,

inspect Unity version, render pipeline, project folders, active scenes,

installed packages, MCP tools, Unity Console errors and git status.

Report findings, risks, and a small-step execution plan. Do not change

assets, packages, scenes, or source files yet. The GDD v1.1 is canonical.

### T02 — First Person Controller

Create a modular first-person CharacterController for Windows PC:

WASD mouse-look, Shift sprint with stamina, Ctrl crouch, Esc pause,

configurable sensitivity/FOV/headbob; use Unity Input System.

Place it in an isolated test scene/prefab, add tests if possible,

run PlayMode/Console validation. Do not modify Chapter 1 scene yet.

### T04 — Greybox Chapter 1 (monster menyusul pada T06)

For T04, use approved GDD and existing prefabs: build only

a GREYBOX dorm 4x5m, modular corridor and cover spots,

basic collisions and checkpoint; NO monster logic yet (T06).

Keep task T04 limited to level greybox. Test no softlock.

Do not edit AI/monster scripts; those belong to T06.

### T11 — EndingResolver + unit tests

Implement pure C# EndingResolver with deterministic priority:

Approach -> Good; Fight/Hide + all 4 clue IDs -> Hidden;

Fight/Hide + fewer clues -> Bad; no response -> None.

Add EditMode NUnit tests for every branch and missing/null data.

Keep mental health out of numerical scores. Do not change narrative text.

## 15  Definition of done keseluruhan & handoff package

### 15.1 Final delivery checklist

- [ ] Source Unity project dapat dibuka pada versi Editor yang dicatat dan assets resolve.
- [ ] Prolog dan tiga chapter dapat dimainkan awal hingga akhir.
- [ ] Setiap ending Good/Bad/Hidden dipicu sesuai test matrix, tidak ada softlock.
- [ ] Good Ending menampilkan proses Raka kembali kuliah, wisuda, dan surat Ella.
- [ ] Hidden Ending secara eksplisit mengungkap Ella hidup / sudah sadar tanpa membocorkannya dalam prolog.
- [ ] Bad/Hidden disajikan non-grafis dan content notice tersedia.
- [ ] Seluruh UI, subtitle, pengaturan audio, pause, checkpoint, save/load diuji.
- [ ] Fitur inti dan visual punya performance profile pada PC target.
- [ ] Semua asset/suara memiliki sumber dan lisensi yang dapat dipertanggungjawabkan.
- [ ] Build Windows x64, README, petunjuk kontrol, catatan bug, GDD, video demo siap diserahkan.
### 15.2 Output per sprint untuk memudahkan kerja agen

Setiap task wajib menghasilkan: (1) files changed, (2) ringkasan perilaku yang dibuat, (3) cara menguji di Editor, (4) console/test result, (5) risiko yang tersisa, dan (6) screenshot atau rekaman singkat jika perubahan bersifat visual. Jika MCP gagal, agent wajib melaporkan blocker, bukan mengklaim telah memodifikasi scene. Untuk tugas art T13, wajib lampirkan screenshot FPP dari jarak jauh/dekat dan status rig/export.

## 16  Referensi teknis & hak cipta

Unity. “The Official Unity Plugin for Codex.” 16 September 2026. https://unity.com/blog/unity-plugin-codex

Unity. “Unity’s AI tools in beta: How to get started with MCP.” 11 Mei 2026. https://unity.com/blog/unity-ai-mcp-how-to-get-started

Unity. “Unity’s AI tools for game development.” https://unity.com/features/ai

Unity Technologies. “HDRP System Requirements” (Unity Graphics repository). https://github.com/Unity-Technologies/Graphics/blob/master/Packages/com.unity.render-pipelines.high-definition/Documentation~/System-Requirements.md

Blender MCP komunitas (ahujasid), dokumentasi dan peringatan keamanan server. https://github.com/ahujasid/mcp-for-blender

Konami. “SILENT HILL 2 remake ... releases today.” 8 Oktober 2024. https://www.konami.com/games/us/en/topics/2681/

Sumber internal tim: No Rainbow After.pptx; storyline terbaru; Langkah_Awal_Perencanaan_Game_Psikologis.docx. Dokumen pengguna pada percakapan ini.

Hak cipta: Silent Hill 2 Remake hanya menjadi referensi atmosfer/teknik presentasi; seluruh lokasi, karakter, model, rekaman, tulisan, UI, dan musik No Rainbow After harus original atau berlisensi. Lagu “Purple Rain” tidak dipakai tanpa lisensi.

## Lampiran A  Daftar keputusan yang harus diuji pada prototipe

| Isu | Default v1.1 | Kriteria revisi |
| --- | --- | --- |
| Durasi | 20–30 menit. | Sesuaikan setelah blind playtest. |
| Pipeline | HDRP pada PC target. | Ganti URP jika PC ujian tak memadai. |
| Clue requirement | Empat dari empat clue untuk Hidden. | Turunkan ke 3/4 jika terlalu mudah terlewat. |
| Ending | Approach → Good; Fight/Hide + 4 clue → Hidden; sisanya Bad. | Jaga plot twist dan uji pengertian pemain. |

— Akhir Game Design Document —
