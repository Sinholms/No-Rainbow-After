# Di Bawah Sorotan — verifikasi Chapter 2

Branch `chapter2` berangkat dari plan `fbda807` dan memuat pembaruan rig first-person dari `origin/main` pada `f75bdb5`. Pengujian memakai Unity 6000.6.8f1.

## Environment

Dari spawn ke papan Halte Pasar berjarak sekitar 37 meter. Dua lajur aspal diapit trotoar. Peti rendah di depan Warung Sri menghadap lampu depan sedan. Di tengah, Shadow menyapu jalan dari sisi kanan. Menjelang garis polisi, pemain dapat memotong lewat tengah atau memutari peti tambahan di trotoar.

Delapan modul di file Blender berisi 666 objek yang masih dapat diedit. Delapan FBX menggabungkan geometri tiap modul untuk Unity. Material aspal menggunakan peta warna dan kehalusan genangan serta normal permukaan. Posisi collider cover dipertahankan dari blockout. [Sumber Blender](../../../BlenderSources/Environment/ChapterTwoStreet.blend) · [Modul FBX](../../../Assets/_NoRainbowAfter/Art/Models/Environment/ChapterTwo) · [Prefab jalan](../../../Assets/_NoRainbowAfter/Resources/ChapterTwoStreet.prefab)

[Jalan dari spawn](Jalan.jpg) · [Warung Sri](Warung.jpg) · [Halte Pasar](Halte.jpg) · [Lampu pada kain Shadow](SorotanShadow.jpg)

Papan toko dan pita polisi memakai tulisan dalam ruang 3D agar geometri di depan kamera menutupinya. Dua tangkapan dari tempat yang sama memperlihatkan batas atas peti relatif terhadap mata pemain. [Berdiri](PetiBerdiri.jpg) · [Jongkok](PetiJongkok.jpg)

## Alur dan stealth

| Pemeriksaan | Hasil |
| --- | --- |
| Akhir Chapter 1 sampai Chapter 3 | Tes memakai pintu keluar koridor, bangun di kos, laptop, tas di halte dan telepon Bayu. Setiap beat selesai satu kali. Daftar petunjuk bertahan sampai objektif **Temui Bayu di kampus**. |
| Laporan kecelakaan | Menutup dan membuka laptop mempertahankan bagian yang sudah selesai. Empat bagian yang diketik benar memicu stres 100% dan satu perpindahan ke jalan. Kontrol terkunci selama fade. |
| Halte dan cover dari sudut pandang Raka | Papan halte berada dalam bidang pandang dan mempunyai garis pandang bebas dari spawn. Di posisi yang sama di belakang peti rendah, kepala yang berdiri terkena lampu dan tubuh yang jongkok terlindung. |
| Paparan dan pause | Dua sorotan tumpang tindih mengisi satu meter. Cover solid memutus deteksi dan meter turun. Pause menghentikan perubahan meter dan rotasi kepala. |
| Tiga checkpoint | Pada A, B dan C, tangkapan memulangkan pemain ke marker segmen yang sudah dicapai. Pose aman dan meter kosong diperiksa sesudah respawn. |
| Dua rute akhir | Motor first-person menyelesaikan jalur tengah dan jalur trotoar dengan tangkapan aktif pada **2,0** serta **1,5 detik**. Percobaan memakai fase awal patroli dan stamina awal yang sama. |
| Rig first-person dari main | Tes berdiri dan jongkok memeriksa telapak kaki, dada, serta jarak kartu identitas dari kamera saat Raka menunduk. |

Tes cerita memakai interaksi raycast, input ketik terprogram dan callback chat. Tes itu memindahkan pemain di antara beberapa posisi untuk memeriksa pemicu. Tes rute terpisah benar-benar menggerakkan karakter melalui collider. Durasi permainan tetap **2,0 detik**. Keterbacaan peringatan, kemudahan pemain baru memilih cover, dan kenyamanan timing masih perlu playtest manusia.

## Regresi

Setelah prefab final dan rig first-person digabung, sembilan tes integrasi yang sama lulus di Editor PlayMode dan Windows Player x64. Ketujuh tes Chapter 2 mencakup environment, paparan dan traversal. Dua tes lainnya menjalankan alur dari lemari Chapter 1 hingga objektif Bayu serta memeriksa rig jongkok dari main. XML dari kedua runner disimpan bersama laporan ini. [Hasil Editor](PlayMode.xml) · [Hasil Windows Player](WindowsPlayer.xml)

Suite PlayMode lengkap setelah penggabungan main menjalankan 75 tes. Sebanyak 67 lulus. Tujuh kegagalan Shadow koridor lama juga tercatat di baseline Chapter 1. Tes lab first-person mengharapkan kapsul berdiri lebih tinggi dari 1,79 m dan mengukur 1,70 m. Delapan kegagalan ini juga muncul pada pengujian sebelum merge. [Baseline Shadow lama](../ChapterOneSequence/BaselineLegacyTests.xml)
