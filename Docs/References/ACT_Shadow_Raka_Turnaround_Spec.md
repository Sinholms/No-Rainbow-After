# Desain Fix Karakter Manifestasi Raka (`ACT_Shadow_Raka` — Acuan Wajib T13)

- **File Turnaround Utama (Resolusi Penuh `1672×941`):** `Docs/References/Raka’s Manifestation Character Turnaround.png`
- **Status Desain:** **LOCKED / DESIGN FIX** (ditetapkan pengguna pada penutupan T06, 7 November 2026).
- **Target Implementasi Penuh:** **T13** (*Monster hero asset — Blender MCP → Unity*: modeling detail, UV/2K PBR Variant A & B, Generic rig, dan 8 klip animasi). Pada T06 masih digunakan mesh siluet placeholder (`ACT_001_ShadowPlaceholder_v1`).

> **Instruksi untuk Agent T13 (dan task terkait):**
> Ketika membangun ulang `ACT_Shadow_Raka.blend` / `ACT_Shadow_Raka.fbx` di Blender pada **T13**, **wajib menjadikan `Docs/References/Raka’s Manifestation Character Turnaround.png` sebagai acuan bentuk utama dan membuatnya semirip mungkin** pada kelima sudut pandang (`FRONT`, `LEFT SIDE`, `BACK`, `RIGHT SIDE`, `FRONT THREE-QUARTER`), sambil tetap mematuhi aturan non-gore GDD serta kontrak gameplay/kapsul T06 & T11.

---

## 1. Panduan Proporsi Orthographic (`HEAD` – `SHOULDER` – `WAIST` – `KNEE` – `FEET`)

Pasang `Docs/References/Raka’s Manifestation Character Turnaround.png` sebagai *Reference Image Empty* di Blender pada tampak depan (`FRONT`), samping kiri (`LEFT SIDE`), belakang (`BACK`), dan samping kanan (`RIGHT SIDE`) dengan menyamakan kelima garis bantu horizontal pada gambar:

1. **`FEET` (`z = 0,00 m`):** Telapak kaki manusia telanjang menapak rata di lantai (jari-jari kaki sedikit menyebar dan menegang, tumit dan tendon Achilles tegas).
2. **`KNEE` (~`27–29%` tinggi total):** Lutut kurus menonjol; ujung pergelangan tangan menggantung mencapai garis `KNEE`, sedangkan jari-jari panjang menjuntai melewati bawah garis `KNEE`.
3. **`WAIST` (~`55–57%` tinggi total):** Pinggang sangat ramping dan cekung, diikat sabuk kulit ganda dengan lembaran kertas tugas/lembur menggantung di sisi pinggul di atas kain lipit koyak.
4. **`SHOULDER` (~`81–83%` tinggi total):** Bahu kurus sedikit asimetris; leher menjorok maju (*forward neck crane* terlihat jelas pada `LEFT SIDE` dan `RIGHT SIDE`).
5. **`HEAD` (`100%` tinggi total — target `2,25–2,45 m` di dunia Unity):** Kepala menunduk ke depan-bawah dengan mata tertutup lilitan kain kusam dan rambut hitam basah menjuntai.

---

## 2. Checklist Detail Visual & Kostum yang Harus Direkonstruksi di Blender (T13)

### A. Kepala, Penutup Mata & Rambut Basah
- **Lilitan penutup mata (*blindfold*):** Kain perban kasar krem-abu kusam melilit rapat menutupi kedua mata dan pangkal hidung secara diagonal/berlapis; bagian bawah hidung, tulang pipi cekung, dan bibir/rahang Raka yang tegang tetap samar terlihat di bawah lilitan (menjaga penahanan identitas hingga Chapter 3 tanpa gore).
- **Slip kertas di kepala:** Dua potongan kertas catatan/akademik kusam terselip mencuat di lipatan atas kain kepala pada sisi pelipis kiri dan kanan (terlihat jelas dari kelima sudut pandang).
- **Rambut basah panjang:** Helai-helai rambut hitam kecokelatan lembap (*mesh cards/strips* terkontrol agar hemat performa) terurai melewati bahu hingga punggung tengah, membingkai sisi wajah pada tampak samping dan menutup tengkuk pada tampak `BACK`.

### B. Torso, Kemeja Koyak & Stempel Nilai `"F"`
- **Anatomi tubuh atas:** Kulit cokelat-abu kusam lembap bertekstur urat/serat otot kering dan tulang rusuk/selangka menonjol (tanpa luka menganga, organ keluar, atau gore eksplisit). Pada siluet jarak jauh di koridor gelap/berkabut URP/HDRP tetap harus terbaca gelap mengancam, sedangkan dari jarak dekat (`2–3 m`) detail permukaan terlihat.
- **Sisa kemeja kuliah:** Kemeja lengan pendek putih-abu kusam yang robek tidak beraturan di dada atas, bahu, dan punggung kanan; di atas bahu kiri karakter (kanan gambar `FRONT`) terdapat tumpukan kain/bantalan bahu berlapis yang koyak.
- **Motif simbolik akademik:** Tanda huruf **`"F"` merah di dalam lingkaran merah** tercetak/tertulis pudar pada sisa kain kemeja di dada kiri karakter (kanan pengamat pada `FRONT` dan `FRONT THREE-QUARTER`) — simbol kegagalan akademik dan rasa bersalah Raka (pastikan bukan logo merek dunia nyata).

### C. Sabuk Silang (*Harness*) & Lembaran Kertas Pinggang
- **Tali sabuk kulit cokelat tua dengan gesper logam kusam:**
  - Tampak `FRONT`: satu sabuk melintang diagonal dari bahu kanan ke rusuk kiri, satu sabuk horizontal melingkari bawah dada, serta dua sabuk melingkari pinggang/pinggul.
  - Tampak `BACK`: sabuk membentuk silang **`X`** tegas di punggung atas dengan gesper di tengah/bawah dan terhubung ke sabuk pinggang.
- **Lembaran kertas tugas/slip lembur di pinggang:** Empat sampai lima lembar kertas krem bernoda lembap dengan guratan tulisan tangan/garis margin kemerahan pudar terjepit di bawah sabuk pinggang (di sisi kiri-depan, kanan-depan, serta kiri-belakang pinggul) menyerupai panel *tasset* di atas kain bawah.

### D. Lengan & Tangan Memanjang
- **Lengan sangat panjang dan kurus kering:** Otot/tendon lengan bawah memanjang tidak wajar namun tetap anatomis manusiawi.
- **Tangan & jari:** Telapak tangan besar kurus dengan **5 jari panjang beruas tegas** dan ujung kuku gelap melengkung ke dalam di bawah lutut (bukan cakar pisau raksasa atau senjata boss).

### E. Kain Bawah Koyak, Pelindung Betis Asimetris & Kaki
- **Kain lipit bawah (*tattered pleated wrap/skirt*):** Kain gelap cokelat-arang bermotif kotak samar dengan lipatan vertikal dan ujung bawah compang-camping tidak rata menggantung dari pinggang hingga pertengahan paha.
- **Detail kaki asimetris (penting untuk kesetiaan desain):**
  - **Paha kanan karakter** (kiri gambar `FRONT`): terdapat satu tali pengikat kulit horizontal di paha atas tepat di bawah ujung kain; betis kanan dibiarkan telanjang.
  - **Betis kiri karakter** (kanan gambar `FRONT`, kiri gambar `BACK`): mengenakan **pelindung/balutan kulit gelap tinggi (*shin guard/wrap*) dengan dua sabuk gesper** dari bawah lutut hingga atas pergelangan kaki.

---

## 3. Batas Teknis Integrasi dengan T06 & T11 (Jangan Dirusak di T13)

1. **Kapsul navigasi & pintu kos `2,05 m`:**
   - Pertahankan ukuran `CharacterController` T06 (`radius ≈ 0,23 m`, `height ≈ 1,80 m`) agar tetap muat melewati celah pintu kamar kos terbuka selebar `0,66 m`.
   - Karena tinggi berdiri mesh `2,25–2,45 m` melebihi ambang pintu `2,05 m`, animasi `Pursue` / `Walk_Lurch` atau bone spine/neck harus tetap menunduk saat melintasi pintu sehingga tidak menembus `SM_PROP_DoorLintel`.
2. **Swap visual murni:**
   - Ganti `VisualWrapper` / tambahkan `Animator` (Generic rig, `applyRootMotion = false`) untuk ke-8 klip T13 (`Idle_Breath`, `Walk_Lurch`, `Pursue`, `Search_Cover`, `Fight_Reaction`, `Hide_Pass`, `Approach_Reveal`, `Despawn`) **tanpa mengubah aturan state/kontrak `ShadowEncounterController`, `ChapterDirector`, maupun `EndingResolver`**.
3. **Variant A & Variant B pada rig yang sama:**
   - Gunakan topologi dan rig yang sama untuk **Variant A** (Chapter 1: lebih gelap/tertutup bayangan & rambut basah di koridor) dan **Variant B** (Chapter 3 jarak dekat: detail lilitan wajah, guratan luka superfisial tipis, dan detail kertas/kemeja `"F"` lebih terbaca dalam pencahayaan dekat, tetap tanpa gore eksplisit).
