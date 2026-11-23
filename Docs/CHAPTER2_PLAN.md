# Chapter 2 — Bizarre Edition

**Rencana produksi aktif · 23 November 2026**

Setelah kembali ke kos di akhir Chapter 1, Raka masih harus mengerjakan esai kuliah dan berangkat lembur. Pintu ujung lorong membawanya ke jalan pasar yang berubah menjadi TKP kecelakaan. Ia harus melewati dua polisi yang berjaga untuk mencapai halte dan naik bus menuju tempat kerja.

## 1. Alur yang dimainkan

| Beat | Yang dialami pemain |
| --- | --- |
| Bangun di kos | Raka bangun di pinggir kasur. Langkahnya lambat dan berat. Objektif pertama adalah **Kerjakan esai di laptop**. HP fisik terlihat di samping laptop dan bisa ditemukan sendiri. |
| Ketik esai | Monkeytype berisi potongan esai kuliah tentang kecelakaan yang baru terjadi. Pemain mengetik ulang bagian esai yang disiapkan Raka. Hasil tugas menentukan apakah hari berlanjut. |
| Diminta lembur lagi | Jika ada bagian yang berhasil, permintaan lembur dari tempat kerja muncul di layar laptop setelah sesi selesai. Objektif berubah menjadi **Berangkat lembur**. Raka berjalan melewati lorong sampai pintu ujung. |
| Balik ke kasur | Jika seluruh bagian gagal, Raka kembali ke kasur. Gunakan fade dan perubahan cahaya dari siang ke malam untuk menunjukkan bahwa ia rebahan seharian. Percobaan bab berakhir dan pemain bisa mengulang dari bangun di kos. |
| Pintu lorong → TKP | Setelah membuka dan melewati ambang pintu ujung, Raka mendengar sirene. Pemain mendadak berada di sisi aman jalan pasar, melihat mobil dan pantulan rotator dari kilasan sebelum Raka pingsan di prolog. Objektif menjadi **Capai halte dan naik bus**. |
| TKP → bus | Dua polisi bertubuh besar menyapu jalur dengan senter dari pos masing-masing. Raka memakai cover dan jeda sapuan untuk tiba di halte. Pemain berinteraksi untuk naik bus menuju tempat kerja. Beat naik bus menutup Chapter 2 dan memicu handoff ke Chapter 3 satu kali. |

Gerakan berat berlaku saat jalan di kos, lorong, dan TKP. Mulai dengan **sekitar 75% kecepatan jalan normal** sebagai angka prototipe. Pakai langkah yang lebih pelan dan berat serta sedikit jeda saat mulai bergerak. Pertahankan respons kamera dan pastikan Raka berhenti dengan responsif. Batasi juga kecepatan sprint dalam profil bab ini. Uji jarak antar-cover saat jalan dan jongkok agar rute bisa diselesaikan tanpa sprint.

### Pintu keluar yang dipakai dua chapter

- Ganti dinding buntu di ujung lorong kos normal, sesuai titik pada foto referensi, dengan kusen dan daun pintu yang bisa dibuka. Beri bukaan yang cukup untuk kapsul pemain, collider yang mengikuti daun pintu, dan sedikit ruang pijak di seberang ambang.
- **Chapter 1:** setelah syarat cerita slip lembur terpenuhi, Raka menyusuri lorong kos, menekan **E** untuk membuka pintu, lalu melangkah keluar. Pemicu lama berdasarkan posisi di lorong diganti dengan penyeberangan ambang ini. Tujuan perpindahan adalah titik masuk koridor horor Chapter 1. Pintu yang mengakhiri encounter koridor horor tetap merupakan pintu terpisah.
- **Chapter 2:** pintu fisik yang sama terkunci sampai sesi esai menghasilkan permintaan lembur. Setelah pintu dibuka, Raka melangkah melewati ambang untuk berpindah ke posisi aman di TKP.
- Tekan **E** hanya membuka daun pintu. Transisi berjalan satu kali saat Raka menyeberang dari sisi lorong ketika pintu sudah terbuka dan syarat bab terpenuhi. Pemain bisa membuka pintu, berhenti, atau mundur tanpa berpindah. Jaga arah pandang saat muncul di tujuan agar terasa seperti meneruskan langkah yang sama.

### Esai dan kondisi gagal

- **Usulan prototipe:** tiga potongan pendek yang membentuk esai. Gunakan paragraf pembuka, refleksi tentang kecelakaan, dan penutup. Teks boleh menggemakan motif hujan, cahaya biru, atau suara yang terasa familier. Nasib Ella tetap tersirat lewat informasi yang belum lengkap.
- Setiap bagian punya satu hasil akhir. Pemain boleh memperbaiki salah ketik dan mencoba Enter lagi selama waktu tersisa. Bagian yang belum lolos ketika waktunya habis dicatat gagal, lalu sesi berlanjut ke bagian berikutnya. Menutup laptop atau pause menahan progres dan waktu tersisa. Membuka lagi melanjutkan bagian yang sama.
- **0/3 berhasil** memicu adegan rebahan. **1/3 sampai 3/3 berhasil** memicu permintaan lembur setelah semua bagian selesai dinilai. Durasi bagian ditentukan setelah uji mengetik. Hasil ini mengatur alur hari Raka. Pilihan ending global dibaca terpisah.

### Foto, HP Ella, dan Hidden Ending

Foto Raka dan Ella ada dalam bingkai di meja kos, di sebelah kanan HP dekat lampu. Saat diperiksa, pemain melihat fotonya dan tulisan Ella “Wisuda bareng, ya.” yang memberi `CLUE_01`.

- Ponsel diletakkan di samping laptop dan tersedia selama Raka masih di kos, sebelum maupun sesudah esai. Interaksi normal dengan benda bisa membuka chat jika pemain mengarah langsung ke HP. Pemain menemukannya tanpa objektif, notifikasi yang memancing, penanda, atau teks **Buka HP**.
- Ella masih di rumah sakit. Raka belum tahu apakah Ella masih hidup, dan HP tidak mengungkap kondisi Ella. Saat membuka kontak Ella, pemain hanya melihat draf permintaan maaf dan rasa rindu dari Raka.
- **Kirim** menampilkan satu pesan dari Raka dengan **satu centang**. Pesan sudah terkirim, tetapi belum sampai ke HP Ella. Tidak ada riwayat pesan dari Ella atau balasan. **Jangan kirim** menutup ponsel. Pemain bisa membukanya lagi selama masih di kos.
- Draf Raka mengingat janji untuk wisuda bersama dan memberi `CLUE_03` ketika dibaca. Status kirim adalah flag cerita tersendiri yang dicatat satu kali per percobaan. Flag bertahan saat retry di TKP dan ketika bab berikutnya dimuat. Memulai ulang percobaan Chapter 2 dari kasur mengulang pilihan kirim untuk percobaan baru.
- **Usulan aturan ending:** pertahankan `Approach` → **Good**. Untuk `Fight` atau `Hide`, **Hidden** membutuhkan semua empat clue yang sudah ditetapkan serta pesan yang Raka kirim ke Ella. Jika syarat itu belum lengkap, hasilnya **Bad**. Pembedaan ini membuat keputusan mengirim berpengaruh tanpa menampilkan syarat Hidden kepada pemain. Beat rebahan adalah akhir hari di Chapter 2, terpisah dari tiga ending global.

## 2. Encounter TKP

Pakai jalan pasar, halte, mobil patroli, dan cover yang sudah tersedia sebagai dasar. Posisi dua polisi ditentukan saat blockout berdasarkan jalur berjalan dan collider map.

| Titik | Penempatan dan fungsi |
| --- | --- |
| Spawn | Di sisi jalan yang terlindung dengan pandangan langsung ke papan halte. Pemain sempat melihat polisi pertama, sorot senter, dan cover berikutnya sebelum terpapar. |
| Polisi 1 | Di tepi jalan dekat fasad ruko atau pagar solid, menghadap lintasan menuju cover pertama. Belakang pos rapat ke batas map dan sisi luarnya tersambung ke penghalang. |
| Polisi 2 | Di tepi berlawanan, mendekati akses halte, dengan belakang membelakangi pagar atau barikade yang menutup celah trotoar. Sorotannya menjaga lintasan terakhir menuju halte. |
| Halte | Titik aman setelah penyeberangan kedua. Pemain melihat bus atau tanda keberangkatannya, lalu **Naik bus** lewat interaksi. |

- **Tepat dua polisi diam di pos.** Kaki tetap di tempat sepanjang encounter. Bahu, kepala, dan tangan boleh bergerak terbatas untuk memperlihatkan arah sapuan senter. Bentuk tubuh lebih tinggi dan besar dari manusia biasa, dengan seragam terbaca dalam siluet. Gunakan prototipe siluet sederhana untuk uji jalur sebelum membuat aset final.
- **Senter polisi adalah sumber deteksi.** Pakai ulang cakupan sorotan, oklusi cover, indikator paparan, dan retry yang sudah tersedia. Mulai dari ambang tangkap **2 detik** sebagai baseline uji, lalu sesuaikan bersama lebar sapuan dan jarak cover untuk gerakan lambat. Saat tertangkap, pemain kembali ke cover aman sebelum lintasan aktif. Checkpoint mempertahankan hasil esai dan pilihan HP.
- **Mobil polisi menjadi dekorasi.** Rotator dan refleksi boleh menyala untuk suasana. Tempatkan mobil di luar lintasan cover utama. Putus hubungan lampu mobil dengan deteksi, meter paparan, dan checkpoint.
- **Tutup jalan pintas dengan geometri nyata.** Sambungkan belakang pos ke dinding atau pagar, dan tutup celah di ujung trotoar dengan barikade. Uji sambil merapat ke kedua sisi jalan, menyusup di belakang badan polisi, dan jongkok di sela collider. Rute utama tetap memberi cover sebelum setiap lintasan senter dan jeda yang cukup untuk berpindah.
- **Sirene terus terdengar dari pintu sampai naik bus.** Kenalkan motif yang sama lewat kilasan polisi di prolog. Di TKP, sirene terdengar terus dan rotator memantul di aspal basah. Bedakan bunyi peringatan pendek ketika tersorot agar tetap terbaca di atas sirene.

## 3. Urutan implementasi

1. **Pintu dan alur kos.** Buat pintu bersama di ujung lorong normal. Di `ChapterOneCorridorShift`, pindahkan pemicu masuk koridor horor ke ambangnya. Beri `LaptopTypingMinigame` profil esai Chapter 2 dan hubungkan hasil ke adegan kasur atau permintaan lembur lewat `ChapterTwoFlow` dan `ChapterDirector`. Arahkan penyeberangan pintu saat Chapter 2 ke TKP. Setel profil langkah lewat kontrol FPP yang ada.
2. **Blockout TKP sampai bus.** Ubah `ChapterTwoStreet` dan builder prefab jalan untuk dua pos statis di tepi, batas fisik, cover, dan interaksi naik bus. Di `ChapterTwoSpotlightEncounter`, gunakan kembali deteksi senter dan retry. Uji rute sebelum memasang aset final.
3. **HP dan handoff ending.** Pakai ulang UI `ChapterTwoPhone` untuk chat Ella. Simpan keputusan dalam state cerita. Teruskan flag dan clue ke kontrak ending. Tas di halte boleh menjadi properti atau petunjuk opsional. Pastikan keempat ID clue masih bisa ditemukan di jalur cerita, termasuk jika petunjuk dari inspeksi tas lama perlu dipindahkan. Naik bus menjadi satu-satunya pemicu handoff bab dari halte.
4. **Audio, visual, dan playtest.** Cocokkan kilasan polisi prolog dengan mobil, rotator, dan sirene di TKP. Poles siluet penjaga, langkah berat, dan bus setelah layout bisa dimainkan. Untuk prototipe, bus cukup terlihat di halte dan transisi boleh berakhir dengan fade setelah pemain naik. Lokasi pembuka Chapter 3 setelah perjalanan bus ditentukan saat merancang bab itu.

## 4. Kriteria siap dimainkan

- Dari akhir Chapter 1, pemain dapat langsung mengerjakan esai. Tutup lalu buka laptop untuk memeriksa progres. Lulus sempurna, lulus sebagian, dan gagal semua menghasilkan cabang yang tepat. Nol keberhasilan menunjukkan Raka rebahan sampai malam dan menyediakan percobaan ulang.
- Pemain bisa melewati HP tanpa mendapat arahan untuk membukanya. Jika membuka HP dan menutupnya, flag kirim tetap kosong. Sebelum mengirim, hanya draf Raka yang terlihat. Jika mengirim, hanya ada satu pesan dari Raka dengan satu centang dan tanpa balasan. Retry jalan mempertahankan flag tersebut.
- Jalur esai yang lanjut ke lembur mengembalikan kontrol untuk berjalan ke pintu. Uji pintu sebelum syarat bab terpenuhi, membuka lalu mundur, dan mencoba melewati pintu tertutup. Melewati ambang saat pintu terbuka memicu koridor horor di Chapter 1 atau TKP di Chapter 2, masing-masing satu kali.
- Dari spawn TKP, pemain bisa melihat halte. Dalam playtest, cek apakah sirene dan mobil polisi dikenali sebagai kilasan dari prolog.
- Tepat dua polisi mempertahankan posisi. Kedua senter bereaksi sesuai arah dan cover. Lewat belakang polisi, menempel batas map, dan menyusuri trotoar tidak memberi rute pintas. Rute dengan cover dapat diselesaikan sambil memakai kecepatan Chapter 2. Lampu mobil tidak mengisi indikator paparan.
- Pemain bisa mencapai halte dan naik bus sekali tanpa membuka HP atau memeriksa tas. Uji handoff dengan kedua pilihan pesan, kombinasi clue lengkap dan tidak lengkap, serta pemulihan kontrol di akhir transisi. Lakukan regresi laptop, lorong, dan Shadow Chapter 1 di jalur normal.
