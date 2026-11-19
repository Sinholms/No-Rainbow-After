# Chapter 2 — Di Bawah Sorotan

**Rencana produksi aktif · 19 November 2026**

Raka bangun di kos, menyalin berita kecelakaan yang melibatkan seorang pemuda dan seorang pemudi, lalu masuk ke jalan yang terasa seperti TKP dalam berita. Pemain harus mencapai **halte di seberang garis polisi** dengan membaca sorotan Shadow dan lampu mobil. Di halte, Raka mengenali tasnya sendiri. Chapter 3 dimulai dengan janji untuk menemui Bayu dan membicarakan kaitan Raka dengan kecelakaan itu.

Dokumen ini menjadi acuan implementasi Chapter 2 dan transisinya ke Chapter 3. GDD v1.1 tetap menjadi acuan identitas karakter, misteri Ella, ID petunjuk, dan aturan ending.

## 1. Alur yang dimainkan

| Bagian | Tujuan pemain | Hasil |
| --- | --- | --- |
| Bangun di kos | **Kerjakan tugas di laptop.** Raka bangun di ranjang seusai Chapter 1. Tugas yang terbengkalai dan chat Bayu dari hari-hari sebelumnya bisa dilihat di kamar. | Pemain mendapat konteks sebelum masuk ke adegan horor. |
| Ketik berita | Salin satu laporan kecelakaan dua anak muda yang dibagi menjadi bagian pendek. Isi berita fokus pada lokasi dan kejadian. Nasib kedua orang masih belum jelas. | Saat bagian terakhir dikirim dengan benar, stres naik ke **100%**, bunyi ketikan terputus, lalu layar gelap dan Raka berpindah ke sisi jalan. Trigger berjalan sekalipun pemain mengetik tanpa salah. |
| Seberangi jalan | **Capai halte di seberang garis polisi.** Halte terlihat dari posisi awal. Gunakan box, jongkok, dan celah di antara sapuan lampu. | Pemain melewati tiga segmen dan tiba di zona aman di halte. |
| Periksa tas di halte | Kenali tas Raka lewat gantungan yang sempat terlihat di kos. | Kepemilikan tas memperdalam misteri soal keterlibatan Raka. Setelah inspeksi, kembali ke kos. |
| Balas Bayu | Raka bisa menulis *“Besok bisa ketemu? Ada yang mau gue ceritain soal kecelakaan itu.”* Bayu menyepakati satu tempat bertemu. | Chapter 2 selesai. Objektif pembuka Chapter 3 adalah **Temui Bayu di kampus**. Pertemuan itu menjadi jembatan menuju konfrontasi Chapter 3. |

## 2. Core loop stealth

**Baca arah sorotan → pilih cover atau jalur → jongkok dan pindah saat ada celah → cari posisi untuk membaca sorotan berikutnya.**

| Segmen | Susunan ancaman | Keputusan yang diuji |
| --- | --- | --- |
| A — Mobil polisi | Satu mobil diam dengan lampu depan menyala. Cover pertama terlihat jelas dari spawn. | Dari posisi berdiri, lihat sorotan dan cover. Jongkok untuk melewati bagian dengan box rendah. |
| B — Shadow berjaga | Shadow berdiri dan menyapu jalan dengan pola tetap. Kepala mulai menoleh dan statis radio terdengar sebelum sapuan beralih. | Baca arah berikutnya. Pilih kapan meninggalkan cover dan menyeberang. |
| C — Shadow berpatroli | Shadow berjalan sambil menyapu jalan. Sorotannya sesekali menyilang dengan lampu mobil. | Pilih jalur pendek yang terbuka atau jalur memutar dengan cover tambahan. Kedua jalur menuju halte. |

Susun map secara linear menuju halte, dengan satu percabangan pendek di segmen C. Pada tiap checkpoint, beri pemain satu tempat aman yang memungkinkan ia melihat ancaman dan cover berikutnya. Ubah jarak cover, arah sapuan, dan persilangan lampu untuk menaikkan tekanan.

## 3. Aturan sorotan dan tangkapan

- **Sorotan putih** di aspal menunjukkan arah, lebar, dan batas jarak cahaya. Bentuknya mengikuti kepala Shadow atau arah lampu depan mobil. Cahaya dari kedua sumber memakai aturan paparan yang sama.
- **Cover** memutus pandangan langsung ke tubuh Raka. Lampu mengenai Raka ketika ia berada dalam sudut dan jarak cahaya serta tubuhnya terlihat. Berdiri bisa membuat kepala terlihat di atas box rendah. Jongkok menurunkan tubuh sampai terlindung. Petunjuk visual dan deteksi harus sepakat pada posisi tersebut.
- **Peringatan merah** muncul pada area cahaya di dekat Raka saat ia tersorot. Bunyi singkat dan indikator paparan yang mulai terisi memberi peringatan saat pandangan pemain mengarah ke depan. Hindari kedipan cepat.
- **Timer tangkapan** habis saat paparan terkumpul sampai **2,0 detik**. Saat keluar dari semua sorotan, indikator turun cepat. Dua lampu yang mengenai Raka bersamaan tetap mengisi satu indikator pada laju yang sama. Saat pause, sapuan dan indikator berhenti.
- **Respawn** membawa Raka ke awal segmen aktif saat indikator penuh. Checkpoint berada di balik cover, indikator kembali kosong, dan petunjuk yang sudah diperiksa tetap tercatat. Urutan setelah laporan tersimpan untuk retry encounter, sehingga retry dimulai langsung di jalan.
- **Uji 2,0 detik sebagai baseline** untuk seluruh bab. Coba **1,5 detik** jika pemain baru bisa membaca peringatan dan peluang kabur pada 2,0 detik sudah terasa longgar. Uji dua durasi di layout yang sama. Jika pemain berkali-kali tertangkap karena cover di luar jangkauan atau sorotan pertama datang dari luar layar tanpa peringatan, benahi layout dan sinyal sebelum memperpendek timer.

## 4. Shadow Raka dan SDG 3

Gunakan rig dan siluet `ACT_Shadow_Raka` yang sudah ada. Shadow mendongak, statis radio masuk, lalu sorotan muncul dari sela kain penutup matanya. Rig, sumber sorotan, dan arah area putih harus bergerak bersama. Sorotan mewakili perasaan Raka sedang diperiksa dan takut disalahkan. Tanda kesehariannya muncul lebih dulu, lewat tugas yang terbengkalai, pesan yang ia biarkan selama berhari-hari, dan hal yang dulu ia nikmati tetapi kini sulit ia mulai.

Stres 100% menandai perpindahan dari laptop ke jalan. Selama stealth, pakai indikator paparan sebagai peringatan utama dan hentikan efek layar stres dari adegan laporan. Di penutup, pemain menggerakkan Raka untuk menghubungi orang yang ia percaya dan menyampaikan satu kebutuhan yang konkret. Ini mengaitkan desain dengan promosi kesehatan mental dan kesejahteraan pada Target 3.4. [PBB — SDG 3](https://sdgs.un.org/goals/goal3) · [WHO — Depresi](https://www.who.int/news-room/fact-sheets/detail/depression)

Referensi desain adalah pembacaan patroli dari *Assassin’s Creed* dan ketegangan saat berlindung dari *Little Nightmares*. Bentuk Shadow, map jalan, suara, dan UI dibuat untuk *No Rainbow After*. [Ubisoft — Stealth](https://news.ubisoft.com/en-gb/article/1hges9IWLOmNAFpiavYmKl) · [Bandai Namco — Little Nightmares](https://en.bandainamcoent.eu/little-nightmares/news/escape-your-childhood-fears-little-nightmares-available-today-on-ios-and)

## 5. Urutan pengerjaan

1. **Blockout stealth (`T09`, Orang 2 + Orang 3).** Buat jalur A–C, cover, spawn aman, dan halte dalam scene kerja. Pastikan bisa mencapai halte dengan kontrol FPP saat ini. Mulai dari lampu mobil statis, kemudian Shadow berjaga, lalu patroli dan persilangan cahaya.
2. **Deteksi, feedback, dan retry (`T09`, Orang 2 + Orang 1).** Gunakan arah, sudut, jangkauan, dan occlusion yang sama untuk deteksi dan penanda lantai. Deteksi tetap konsisten pada kualitas grafis berbeda. Terapkan indikator paparan, pause, dan respawn per segmen. Lakukan playtest sebelum memoles aset.
3. **Sambungkan pembuka (`T09`, Orang 2 + Orang 1).** Jadikan kembalinya Raka ke kos sesudah `INT_C1_EXIT` sebagai titik sambung. Mulai beat bangun setelah transisi kos selesai. Pakai ulang UI `LaptopTypingMinigame` dengan konfigurasi berita Chapter 2. Menutup laptop menyimpan bagian yang sudah selesai dalam sesi ini. Saat laporan tuntas, kunci trigger satu kali, pindahkan pemain, dan aktifkan kontrol setelah fade. Jaga lima kalimat, timer, dan penalti laptop Chapter 1.
4. **Sambungkan ending (`T08`/`T10`, Orang 1 + Orang 2).** Rakit chat fiktif Bayu, inspeksi tas, dan permintaan transisi Chapter 3. Gunakan `ChapterDirector` dan `GameState` sebagai titik integrasi. `GameState.ActiveChapter` saat ini masih tetap `Chapter1`, jadi tambah routing bab aktif dan beri pemilik yang jelas untuk HUD objektif setelah transisi. Pertahankan `CLUE_01`–`CLUE_04` dan penyimpanan yang sudah ada. Tas menjadi pemicu cerita. Penempatan chat lama Ella dan rekaman ambigu tetap bagian dari `T10`.
5. **Poles dan jalankan regresi (seluruh tim).** Poles Shadow, mobil, box, aspal dan halte setelah alur bisa dimainkan. Gunakan prefab visual Shadow yang ada dengan perilaku khusus Chapter 2, sehingga `ShadowMotionPlayback` tetap menjadi pemutar pose rig. Integrasikan scene kerja ke alur utama dan uji Chapter 1, Chapter 2, serta handoff Chapter 3.

## 6. Kriteria selesai

- Dari alur normal Chapter 1, pemain kembali ke kos, bangun, mengirim laporan, melihat stres mencapai 100%, lalu tiba di jalan. Keluar dan membuka kembali laptop memulihkan bagian laporan yang sudah selesai. Menekan Enter berulang hanya memicu satu transisi.
- Sorotan terbaca dari posisi berdiri dan jongkok pada build Windows. Uji batas luar cone, belakang box tinggi, berdiri dan jongkok di box rendah, sorotan yang datang dari samping, dua lampu bersamaan, dan pause saat indikator terisi.
- Pemain bisa menyelesaikan ketiga segmen, termasuk kedua jalur segmen C. Tertangkap mengulang segmen aktif dalam kondisi aman. Halte, inspeksi tas, dan kirim chat hanya memicu progres satu kali.
- Pemain baru bisa menunjuk tujuan dari spawn, menjelaskan penyebab tertangkap, dan menyebut tindakan Raka setelah mencapai halte. Catat segmen dan alasan yang paling sering membuat pemain gagal sebelum memilih timer final.
- Tes regresi Chapter 1 mencakup laptop, slip lembur, Shadow, lemari, atur napas, respawn, dan kembali ke kos. Handoff Chapter 3 mempertahankan petunjuk yang sudah ditemukan dan membuka objektif pembukanya.

**Setelah playtest**, tetapkan durasi paparan, kecepatan putar Shadow, jarak box, dan panjang jalur memutar. Selaraskan naskah laporan dan balasan Bayu dengan kronologi cerita sebelum rekaman atau finalisasi UI.
