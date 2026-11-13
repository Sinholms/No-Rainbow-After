# Chapter 1 expansion — 12 November 2026

## Current layout revision — 13 November 2026

### Follow-up: plain elbows, side wardrobes and faulty ceiling lamps

The three enlarged corner bays have been replaced by corridor-width elbows. The same three wardrobes now fit against the side walls at the marked locations in the user's annotated overhead image: local (11, 0, 0.12), (34.37, 0, 5.2), and (25.5, 0, 10.58), facing into the walkway. Narrow wall apertures and backing walls fit the cabinets, like the existing east wardrobe. There are still five wardrobes total. The perimeter floor, collision and navigation are rebuilt from the revised manifest.

Seventeen visible twin-tube ceiling fixtures add dim warm-neutral illumination over a low steady fill. Each independently starts a short blink sequence after a random **1–2 second** pause. Every on-to-off and off-to-on edge emits seven small sparks, a 45ms local flash, and a quiet spatial electrical crackle. Individual dark pulses last 90–220ms; neighboring lamps are not synchronized. The transition discharge follows the user's clarification: it occurs on both light-state edges, rather than being a separate random fault event. The fixed-seed timing does not modify Unity's global random stream.

`CorridorFaultyLight` controls light, tube emission, particles and audio together. Disabling the corridor clears sparks/audio and resets the fixture; player pause freezes the scheduling. No realtime shadows are added. The short electrical tick WAV is synthesized original noise. Install with `CorridorLightingInstaller.Install()` after regenerating geometry and calling the corridor repair installer. Capture and test reports for this pass are under `Docs/QA/C1Lighting*`.

The horror corridor now has one winding route. The branching imported hub and its side rooms were removed and replaced with matching textured corridor sections. An entrance elbow connects to the east stretch, then the route turns through the three existing wardrobe corners and ends on the south return leg. A full-height textured mesh wall with collision seals the old return-to-main connection at local x=18.6, z=0.7. The side walls along the replaced hub are visible mesh geometry, not invisible blockers. The former loop descriptions below document the earlier layout.

All five wardrobes remain. The backing floor and perimeter manifest now follow only the retained route and wardrobe bays. Navigation has been rebuilt. Encounter, capture, hiding response and chapter completion behavior are preserved.

To reproduce from the repaired source, run `BlenderSources/Environment/simplify_c1_corridor.py` and then `ChapterOneExpansionInstaller.RepairCorridor()` on saved MasterGameplay. The current Blender source is `HC3_Linear.blend`. The generator uses `HC3_Repaired.blend` as its baseline and does not overwrite it or the user's edited `HC3_Extended.blend`.

All nine ChapterOneExpansionTests passed, including physical traversal, monster navigation, hiding/completion, corner wardrobe access, floor/perimeter, arrival narration, capture/checkpoint recovery, and a new rendered-wall/closed-shortcut check. Reports and screenshots: `Docs/QA/C1Linear_Tests.json`, `Docs/QA/C1Linear_Build.json`, `Docs/QA/C1Linear/`.

User-authorized revision to T04/T05/T06/T07: longer, varied horror corridors, no warehouse/library room; strongly disordered dorm room; E to hide in wardrobes; first capture permits escape. Ending script remains in progress.

## Playable sequence

1. Start MasterGameplay (or New Game). Read the book with E. Leave the room, then return.
2. A short blackout reveals a disordered room: 105 scattered sheets on the floor, 16 fallen books, overturned chair and displaced bag. Floating pages were removed following user feedback. The desk and overtime slip remain reachable. Solid fallen furniture has mesh collision; paper decoration does not block movement.
3. Read the overtime slip and close inspection with Esc. Cross the room doorway: a short blackout transfers Raka into the extended horror corridor.
4. Explore the entrance arm, main crossing and east stretch, following the HUD hint **Berjalan ke ujung lorong**. Encounter is gated until the east stretch (world x >= 79.8), giving roughly 30m of exploration before the monster appears ahead and the objective changes to **Lolos dari bayangan** with wardrobe/Fight instructions.
5. E on a wardrobe enters it; the player can look through the door opening. E exits from inside regardless of gaze direction. Pause/inspection block exit input. An obstructed exit keeps the player concealed rather than placing them inside another solid.
6. Entering unseen and remaining inside for 6 seconds triggers monster departure and one Hide resolution. Entering within a clear, close monster sightline is detected and does not grant a free Hide. Existing Fight remains available outside wardrobes.
7. Capture plays a 1.15-second procedural animation: the monster reaches with its arms and leans forward, while the FPP camera turns toward it and shakes during the struggle. First capture then releases controls and grants 3 seconds to escape while the monster recovers. Red screen edges intensify during the grab and remain softly pulsing after release. Second capture plays the grab before the checkpoint fade; clue IDs remain intact and the red border clears on recovery. Returning to the corridor permits another attempt.
8. Successful Hide or Fight now shows **Chapter 1 selesai**. The Chapter 2 transition cinematic is still pending; this UI makes the currently implemented endpoint explicit.

## Route and assets

Horror Corridor 3 is adapted in Blender: storage/library removed, floor height and ceiling clearance corrected, sections reused for an east extension with turns and a connected return loop. Route colors differ between entrance, east stretch and return. The physical traversal test covered **78.5m**, compared with the old 14m corridor; this measures traversal distance, not human playtime. A fixed return route is implemented, not procedural generation or randomized room rearrangement on each lap.

Five original wooden wardrobes are installed: entrance terminus, a recessed opening in the east stretch, and three additional corner bays. Blender sources are `C1_RoomDisarray.blend`, `C1_HidingWardrobe.blend`, and the current `HC3_Repaired.blend` (derived from `HC3_Extended.blend`); corresponding preparation scripts are stored alongside them.

The original user download is copied into `ThirdPartySources/HorrorCorridor3/` and kept locally. **Source provenance is uncertain** and was disclosed before integration; the user selected private coursework use. Attribution and the exact concern are recorded in [ASSET_LICENSES.md](ASSET_LICENSES.md).

## Narrative contract and limits

Hide triggers the established `EncounterResponse.Hide` callback once and records `GameState.Chapter1Response`. This is a hook for the evolving Bad/Secret Ending script; this change does not implement or lock a new final ending rule. Chapter 1 still emits `ChapterTransitionRequested`; the later wake-up/phone/Chapter 2 sequence remains future work. The monster visual remains the existing encounter placeholder, not the separately staged Raka hero asset.

## Verification

- 30 selected PlayMode tests passed: 26 input/movement/interaction/narrative regression tests and 4 expansion integration tests. Following the deeper encounter trigger change, all 4 expansion tests passed again. An additional monster navigation test passed, bringing the selected distinct coverage to **31 tests**.
- Expansion checks cover first-return room change, slip-gated transfer, physical floor/collision traversal of the return route, Hide departure and single callback, first-escape/second-capture recovery with clue preservation, and monster navigation around corners.
- Reports: `Docs/QA/C1Expansion_PlayMode.json`, `C1Expansion_FinalIntegration.json`, `C1Expansion_MonsterNavigation.json`. Old T06 tests asserting the original straight-corridor spawn/one-capture behavior were not treated as acceptance tests for the new design; the entire historical suite was not rerun.
- Actual Game View captures are under `Docs/QA/C1Expansion/`. Human pacing and fear response still require playtesting.
- Windows build output: `Builds/Chapter1Expanded/NoRainbowAfter.exe` (ignored by Git). See `C1Expansion_WindowsBuild.json` for the final build result.
- Final Windows build succeeded with 0 errors and 1 pre-baked collision warning concerning the existing `SM_ENV_T02_Ramp`. Collision pre-baking is enabled for the new environment models. A standalone smoke run completed using the existing benchmark harness (`C1Expansion_StandaloneSmoke.json`); it exercises the baseline scenarios, so its FPS values are not a benchmark of the expanded corridor.

No push. Unrelated staged Raka assets, recovery files and user project-setting changes are excluded from the Chapter 1 commit.

## Follow-up feedback pass

Removed all 8 floating room pages from the `.blend`, exported FBX, and generator after user feedback. The room uses grounded clutter. Five updated integration tests passed (`Docs/QA/C1Feedback_Tests.json`), including the new grab state, retained red border, pause freeze, restored controls, second-capture checkpoint, preserved clue, and the corridor objective hint. Current captures are in `Docs/QA/C1Feedback/`; earlier screenshots with floating pages document the superseded visual pass. Grab animation is procedural on the existing placeholder model; it is not a new rigged animation clip or an integration of the separately staged Raka hero asset.

The red border was subsequently reduced after visual feedback: resting opacity is 0.08–0.10 (previously roughly 0.27), and grab opacity is 0.28 (previously 0.68). It now occupies only the outer 0.10 viewport-height band of the configured circular POV and renders behind its black mask and HUD. The center and black corners remain clear of the red tint. The capture/checkpoint integration test passed again after the adjustment. `AfterEscape_SubtleRed.png` is a staged in-engine preview of this reduced effect; its objective text is unrelated to the damage preview.

## Floor, corner wardrobe, and arrival narration repair

A physics audit reproduced six missing-floor samples along the return junction near world x=77.55–79.35, z=-2.70. `repair_c1_corridor_bounds.py` authors a continuous backing floor across the route union and expands the three outer corners into wardrobe bays. `Repair Corridor Bounds And Add Wardrobes` installs 100 perimeter collision segments, three additional wardrobes with warm local lights, and rebuilt navigation. Door interaction colliders make E reachable even when aiming through the narrow visual viewing slit. The corner rooms preserve the through-route and provide space outside the main running line.

As a backup, an unexpected fall below y=-1 restores the player to the most recent grounded navigation position, preserving clue state. This does not replace the floor and perimeter repairs: a physical traversal test verifies the repaired edge stops the player without triggering fallback recovery.

On the first successful corridor arrival, obsolete queued subtitles clear and Raka's monologues appear in sequence: **Aku di mana? Kenapa aku bisa di sini?**, then **Aku harus menemukan jalan keluar dan pergi kerja.** These are subtitles and named audio cues; no voice recording has been fabricated. The sequence plays once, and does not advance or duplicate chapter beats on checkpoint retry.

Eight integration tests passed, covering earlier encounter/route behavior plus repaired floor samples, physical perimeter collision, entry/exit and E-raycast access for all three new wardrobes, arrival subtitle ordering, and fall recovery with clue preservation. See `Docs/QA/C1CorridorRepair_Tests.json`. Current captures are under `Docs/QA/C1CorridorRepair/`.

To reproduce: generate/extend the source corridor as before, run `BlenderSources/Environment/repair_c1_corridor_bounds.py`, then invoke the repair installer on saved MasterGameplay. Both authoring and installation preserve the original source files and unrelated staged Raka work.
