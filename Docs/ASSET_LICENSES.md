# Asset sources — No Rainbow After

## Chapter 1 hit motion — Quaternius / KinemIQ (CC0)

- Source publisher: https://huggingface.co/datasets/Kinemiq/free-game-animations
- Original animation: Quaternius Universal Animation Library, CC0 1.0.
- Conversion: KinemIQ. Original archive and publisher license are in `ThirdPartySources/QuaterniusMotion/`.
- Used clips: `Hit_to_the_chest` and `Hit_knockback`, retargeted/retimed onto the C1 silhouette. Final collapse and shoulder grab are original additions.
- Rebuild and provenance: `ThirdPartySources/QuaterniusMotion/README.md`, `Docs/CHAPTER1_MOTION_HANDOFF.md`.

## Horror Corridor 3 — private coursework adaptation

- Uploader: Katydid (@Katydid.).
- Listing: https://sketchfab.com/3d-models/horror-corridor-3-e03a192de67d442ebdf42f43a8a2d37a
- Listing license: Creative Commons Attribution-NonCommercial 4.0, https://creativecommons.org/licenses/by-nc/4.0/
- User-supplied download: `horror-corridor-3.zip`, copied from Downloads into `ThirdPartySources/HorrorCorridor3/` on 12 November 2026. The download remains intact.
- **Unresolved provenance:** the archive's source is named `Silent+Hill+Downpour+-+polizeiwache.usdz`. This suggests a possible connection to third-party game content; the uploader's authority to license that underlying content has not been established. The listing's license is recorded as a claim, not a verified license grant for underlying content. The user explicitly chose to retain this archive for a private assignment after this uncertainty was explained. Reassess before any distribution.
- Changes: removed storage/library room; normalized floor height and ceiling clearance; reused clipped corridor sections to extend the route into a return loop; added corner connections; converted materials for Unity URP; baked navigation and added original wardrobes/lighting.
- Source scripts: `BlenderSources/Environment/prepare_horror_corridor3.py` and `extend_horror_corridor3.py`.

Suggested attribution in coursework/credits:

> Horror Corridor 3 — Katydid, via Sketchfab. Listing: CC BY-NC 4.0. Modified for private coursework; underlying source provenance remains unverified. Source archive filename references Silent Hill Downpour.

## Original additions

The Chapter 2 sentry, shuttle, siren and heavy footstep are original project assets. The Blender source is [ChapterTwoBizarre.blend](../BlenderSources/Environment/ChapterTwoBizarre.blend). The [Blender build script](../BlenderSources/Environment/create_chapter_two_bizarre.py) exports the FBX files and synthesizes both WAVs.

Room disarray reuses original No Rainbow After furniture and papers, arranged in Blender. Hiding wardrobes and corridor connection geometry are original project additions. These additions do not resolve the provenance uncertainty of the downloaded corridor.
