# Chapter 1 motion sources

Downloaded 14 November 2026 from the publisher's dataset:
https://huggingface.co/datasets/Kinemiq/free-game-animations

- Archive: `KinemIQ_CC0_GLB.zip` (originally `KinemIQ_Free_Animations_CC0_GLB.zip`).
- Original motion: Quaternius, Universal Animation Library.
- Conversion/cleanup: KinemIQ.
- License: CC0 1.0, https://creativecommons.org/publicdomain/zero/1.0/ . The publisher's accompanying license is preserved in `Library/README.txt`.
- Used motions: `GLB/Hit_to_the_chest.glb` and `GLB/Hit_knockback.glb`.
- Adaptation: relative joint rotation from the first frame, retimed and reduced for the hunched C1 silhouette; feet remain fixed. Final collapse is an original addition.
- `Grab_Player` is an original two-arm shoulder reach/hold/release, authored for the silhouette's long asymmetric arms. It does not contain the proposed Zombie Bite clip, which is absent from this downloaded pack.

Rebuild: run `BlenderSources/Characters/adapt_chapter1_motion.py` with Blender, then run the Unity editor method `NoRainbowAfter.Editor.ChapterOneMotionInstaller.Install`.

Suggested optional credit: “Hit motion by Quaternius (CC0), converted by KinemIQ; adapted for No Rainbow After.”
