# No Rainbow After

Proyek game horor psikologis first person untuk Windows x64.

Buka dengan Unity **6000.6.8f1**. Pipeline yang saat ini terpasang adalah **URP 17.6.0**; target HDRP dalam GDD masih memerlukan keputusan terpisah.

Untuk menjalankan proyek, buka `Assets/_NoRainbowAfter/Scenes/Boot.unity` lalu tekan Play. Mulai Baru kini langsung masuk ke kamar kos dan lorong 3D dalam MasterGameplay. WASD/mouse, Shift sprint, Ctrl crouch, Esc pause; environment dibuat di Blender mengikuti `Docs/design.md`.
Efek pandangan melingkar aktif secara default. Ubah Lebar pandang atau matikan Tepi pandangan melingkar melalui menu pause. Debug lab T02 tetap tersedia di `Assets/_NoRainbowAfter/Scenes/FirstPersonTest.unity`. Objective, interaksi item, monster, dan alur cerita Chapter 1 belum diimplementasikan.

- [Spesifikasi cerita](Docs/GDD_No_Rainbow_After_v1_1_REFERENCE.md)
- [Urutan tugas T00–T16](Docs/CODEX_TASKS_No_Rainbow_After_v1_1.md)
- [Desain dunia dan aset](Docs/design.md)
- [Hasil T01 dan cara menguji](Docs/T01_HANDOFF.md)
- [Menu sinematik imagegen dan hasil pengujian](Docs/MENU_ART_HANDOFF.md)
- [T02 kontrol first person dan aset Blender](Docs/T02_HANDOFF.md)
- [Revisi environment horror dan POV melingkar](Docs/KOS_ATMOSPHERE_HANDOFF.md)

Jangan memasukkan Library, Temp, Logs, atau UserSettings ke Git. Versioning aset Unity harus selalu menyertakan file `.meta`.
