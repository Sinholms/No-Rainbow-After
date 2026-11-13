# Raka manifestation — generated asset pass

Date: 7 November 2026. Scope: user's request to generate the monster now, following the locked five-view turnaround. This is an asset generation pass, **not acceptance of the full T13 milestone**.

## Outputs

- Blender source: `BlenderSources/Characters/ACT_Shadow_Raka.blend`.
- FBX: `Assets/_NoRainbowAfter/Art/Models/Characters/ACT_Shadow_Raka.fbx`.
- 27 baked 2048×2048 PNG maps (BaseColor, tangent Normal, Roughness for nine materials): `Assets/_NoRainbowAfter/Art/Textures/Raka/`.
- Four studio views and optimized preview: `Docs/QA/Raka/`.
- Reproduction sequence in Blender: `create_raka_manifestation.py`, `refine_raka_manifestation.py`, `finish_raka_manifestation.py`, `optimize_raka_manifestation.py`, `smooth_raka_export.py` (all under `BlenderSources/Characters/`). Run in that order; each stage overwrites the new Raka asset, never the original T06 model. Do not run the optimizer twice on the same optimized file.
- Unity preparation utility: `No Rainbow After → Art → Prepare Raka Generated Asset`.

## Actual generated structure

9 meshes; 45,842 triangles; 24,267 weighted vertices, no unweighted vertices; 17-bone Generic skeleton. Eight authored basic in-place motion studies: Idle_Breath, Walk_Lurch, Pursue, Search_Cover, Fight_Reaction, Hide_Pass, Approach_Reveal, Despawn. They are simple keyed motions, not motion capture or finished character animation. Root bone has no translational animation.

Model includes layered eye wraps, paper slips, wet hair strips, five fingers per hand, circled red F, diagonal front harness and back X, belt buckles, waist papers with abstract writing, torn lower wrap, character-left shin guard and right thigh strap. The original turnaround is loaded as a hidden image guide in the Blender source. Geometry and UVs are original, with procedural surfaces baked to textures; the concept image was not pasted onto the mesh.

## Visual review and limits

Preview reveals a substantially simpler character than the reference. Shoulder and knee anatomy remain rounded, torso and limbs have limited sculpted definition, hair is sparse and ribbon-like, and cloth tears are regular. Head posture was bent forward but needs further silhouette work to match the reference's hunched character. Do not describe this as a photorealistic match or final hero model.

Only one material appearance is supplied. Separate A/B looks, finer face and skin sculpting, distressed plaid, polished cloth/hair deformation, finger bones, LOD1, doorway animation clearance, in-game FPP shots and standalone performance validation remain outside the completed generation pass. Roughness PNGs are supplied; the initial Unity preview utility uses constant smoothness rather than packing their inversion into URP smoothness alpha.

The standalone visual prefab is separate from the active T06 encounter. No gameplay AI, narrative, cover, checkpoint, capsule or scene changes were made. Unity import validation succeeded: 9 skinned renderers, Generic non-Humanoid avatar, 8 clips/controller states, Idle_Breath default, root motion off, five correctly looping clips, nine assigned materials, height 2.34 m. Final console read contained zero errors and warnings; an initial clip-name-prefix import failure was resolved. No game build or encounter regression result is claimed. See `QA/Raka/manifest.json` for Blender measurements and `QA/Raka/UnityValidation.json` for import checks.

## Tooling and preservation

Blender MCP verified the clean T06 source and opened the new asset. Generation, baking and FBX export ran with installed Blender 5.2.3 in background processes. A Blender MCP render request timed out at the Unity bridge; file existence and timestamp must be checked before claiming that render completed. The new source remains open for review. T06 source copied to `Temp/T13Baseline/` before generation. Existing VFX materials, project settings and recovery assets were excluded from this work.

The optimized preview was subsequently verified by file timestamp and visual inspection. Local commit was attempted but Git rejected it because user.name/user.email are not configured. Generated changes remain staged; no identity was invented and no Git configuration was changed.
