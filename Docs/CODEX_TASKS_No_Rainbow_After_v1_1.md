# No Rainbow After — Implementation Tasks for Codex + Unity/Blender MCP

**Companion to:** `GDD_No_Rainbow_After_v1_1.docx`  
**Priority:** [Bizarre Edition](CHAPTER2_PLAN.md) governs the Chapter 2 sequence, shared corridor exit, handoff to Chapter 3, and proposed Ella message condition. Use GDD v1.1 for the remaining story canon and clue IDs.

**Target:** Windows x64, Unity 6.x, HDRP if GPU permits, first-person psychological horror, 3 chapters, three endings.

## ID contract — identical to section 9.3 of GDD v1.1

This is the **only canonical task ID list**. Execute one ID at a time. Never renumber, merge or skip a dependency without documenting the decision. The GDD table and every heading below use these exact identifiers.

| ID | Name | Acceptance summary |
|---|---|---|
| T00 | Audit project & MCP (read-only) | Laporan kondisi dan batas akses aktual. |
| T01 | Bootstrap project, scene, Git | Boot -> MainMenu -> MasterGameplay -> Credits berjalan. |
| T02 | FPP Controller + input | Kontrol FPP stabil, collision benar, Console bersih. |
| T03 | Raycast interaction & inspection | Tidak menembus dinding; objek dapat diperiksa. |
| T04 | Chapter 1 greybox | Rute playable; tidak ada softlock. |
| T05 | Narrative director + corridor loop | C1-01 sampai C1-06 berlangsung runtut. |
| T06 | Monster placeholder AI + encounter | Fight, Hide, Caught dapat diuji tanpa gore. |
| T07 | HDRP visual vertical slice | Keterbacaan item dan FPS pada build Windows teruji. |
| T08 | Ponsel opsional dan chat Ella | Pesan terkirim sekali dengan satu centang. Keputusan dicatat untuk ending. |
| T09 | Esai, lembur dan TKP | Cabang kasur/lembur, pintu lorong bersama, dua polisi statis, dan naik bus di halte. |
| T10 | Clue registry + foreshadowing | 4 clue unik tersimpan tanpa membocorkan twist. |
| T11 | Chapter 3 + EndingResolver | Uji Approach, serta kombinasi Fight/Hide, clue, dan pesan Ella sesuai plan aktif. |
| T12 | Tiga ending + credits | QA09–QA11 lulus; tanpa representasi metode eksplisit. |
| T13 | Monster hero asset Blender -> Unity | Rig/animasi valid, tampak jelas pada FPP, performa terukur. |
| T14 | Blender prop pipeline & modular art | Aset Blender masuk Unity dengan orientasi/material benar. |
| T15 | Audio, subtitle, accessibility | Audio dan pilihan aksesibilitas berfungsi/persist. |
| T16 | Full QA, optimization, Windows submission | Windows x64 dapat tamat, ketiga ending dan dokumentasi siap. |

## Global agent instructions (paste first)

```text
You are a senior Unity 6 gameplay engineer and technical artist working on No Rainbow After.
Read and follow the attached GDD v1.1. This is a student project, so favor small,
tested, modular features over massive frameworks. Use current Unity 6 APIs and the
packages actually installed. Use Unity MCP to inspect the real Editor and scene.
Use Docs/CHAPTER2_PLAN.md for the shared corridor exit, Chapter 2 gameplay, and the handoff to Chapter 3.
If you cannot access the MCP bridge, report that honestly; do not pretend to edit Unity.

Before ANY task: inspect Unity version, render pipeline, package manifest, current
scenes/prefabs, Unity Console, and git status. Plan files/components to change.
Commit/backup before scene or prefab modifications. Never delete unrelated files,
upgrade packages, pull unreviewed assets, or execute unknown downloaded scripts.

After implementation: compile; check console; run targeted EditMode or PlayMode
tests when feasible; demonstrate the exact working path, report changed files,
checks performed, remaining issues, and next task. Do one task at a time.

Narrative invariants: Raka is a 5th-semester student struggling economically and
academically; Ella is someone he likes; the crash is audio-only and her fate is
uncertain until Hidden Ending. Good Ending has Raka gradually moving forward and
graduating, finding an earlier letter from Ella. Bad Ending is tragic and implied
non-graphically via absence and Bayu's chat. Hidden Ending retains a tragic tone
but reveals Bayu's message "Si Ella udah sadar". No copied Silent Hill 2 assets. Keep first-person perspective (no chase camera).
Monster visual canon: humanoid distorted shadow of Raka; tall hunched body,
elongated arms, wet obsidian-black skin/clothing, subtle broken chest motif,
student paper fragments, partially obscured face with optional scars and small
smears of dark/diluted blood. No excessive gore, dismemberment, explicit injuries,
or imitation of any known franchise monster. Face becomes partly recognizable
as Raka only in purposeful closer reveals; distant silhouette remains ambiguous.
```

## Phase 0 — Project audit & foundation

**T00 / Audit read-only**

```text
Inspect the open Unity project and provide: Unity editor version, HDRP/URP and
render settings, scene list, package dependencies, folder structure, active scene,
missing references, first 30 Console warnings/errors, MCP capabilities, and git
status. Identify changes required for the GDD. DO NOT modify files or scenes.
```

**T01 / Bootstrap**

```text
Create Assets/_NoRainbowAfter folder layout from the GDD and Boot, MainMenu,
MasterGameplay, Credits scenes. Add a safe scene switching bootstrap and a
minimal MainMenu with New Game, Continue (disabled with no save), Settings,
Credits, Quit. Use the UI system already in the project; otherwise choose one.
Do not import packages without review. Check Console and open each scene.
Acceptance: scene flow Boot -> MainMenu -> MasterGameplay -> Credits works.
```

**T02 / FPS controller**

```text
Implement FirstPersonMotor based on CharacterController + Unity Input System:
WASD, mouse look with cursor lock, LeftShift sprint/stamina (5 seconds initially),
LeftCtrl crouch, Esc pause, F close-inspect, Tab breathing exercise minigame, configurable FOV and sensitivity, disable headbob.
Include visible organic 5-fingered MiSide-style FPP student body (ACT_Raka_StudentBody) synced to player movement
and single-layer top-left red Stress Barometer starting at 50% (reducible via [Tab] green-bar breathing with synced breath audio & camera pitch nodding).
Add a simple test environment and explain how to tune serialized settings.
Acceptance: all controls work in FIRST PERSON (FPP body visible on downward pitch), player collides, no Console exceptions.
```

**T03 / Interaction & inspect**

```text
Implement IInteractable, PlayerInteractor (camera raycast 2.3m with occlusion),
interaction prompt, document inspect overlay closed with [F] (not Esc), and Monkeytype-style
laptop typing minigame (INT_LAPTOP on study desk PROP_RakaLaptop). Laptop generates 5 college-themed
sentences (yellow typed highlight, no broken markup tags), scales timers from 60s down to 30s, submits on [Enter] press, auto-submits on timer expiration (raising red Stress
Barometer on typos/incomplete/timeout; screen turns red & warning blinks at >=70%), auto-exits after 5 completed sentences, and exits on single [Tab] click.
Acceptance: no interaction through walls; E opens laptop/documents; F closes inspect; 5 sentences auto-exit laptop; 1x [Tab] exits early.
```

## Phase 1 — Chapter 1 vertical slice

**T04 / Dorm + corridor greybox**

```text
Construct a 4x5m dorm and modular 12-18m corridor using primitives/Blender props.
Include bed, desk, PROP_RakaLaptop, books, doorway, 2 hide spots, CharacterController spawn,
colliders, initial objective "Mengerjakan tugas" and checkpoint. Set geometry under Chapter1Root.
Acceptance: playable 5-minute route, no missing collider and no softlock.
```

**T05 / Narrative + looping corridor**

```text
Implement GameState, ObjectiveTracker ("Mengerjakan tugas"), ChapterDirector and C1 LoopAdvance
(max 3) with small changes to props and light/fog per loop. Add laptop/book and work-slip
objectives, a trigger for ShadowEncounter, an AudioDirector event interface and
subtitles placeholder. Persist four clue IDs using unique set semantics.
For the shared door revision, use the normal corridor exit threshold for the
Chapter 1 horror transition after the work-slip gate.
Acceptance: C1 scripted beats C1-01 to C1-06 execute in order in PlayMode.
```

**T06 / Shadow encounter**

```text
Create a shadow silhouette enemy PLACEHOLDER (final Blender asset is T13) with Dormant/Spawn/Pursue/
FightResolved/HideResolved/Caught states. Use NavMesh or waypoint logic only
if actually needed. Fight is one contextual click interaction, not real combat;
Hide occurs via crouching in a marked cover zone until it leaves. Caught returns
to checkpoint via fade and does not erase clues. Avoid explicit injury/gore.
Acceptance: Fight, Hide and Caught all work from a fresh session.
```

**T07 / Visual vertical slice**

```text
Do NOT alter gameplay logic or prematurely replace the T06 monster placeholder. Build one hero room art pass: realistic PBR worn
wall, bed fabric, desk wood, fluorescent fixture, wet corridor decals, dust/fog
with careful lighting and composition. HDRP, no ray tracing required. Offer 3
quality presets, turn off expensive effects when needed. Provide screenshot
and FPS measurements in a standalone build, not just the Editor Game View.
Acceptance: all interactables remain visible and performance meets GDD target.
```

## Phase 2 — Chapter 2

Use [Bizarre Edition](CHAPTER2_PLAN.md) for the sequence, prototype order, and acceptance tests. Keep the task IDs.

**T08 / Phone and chat**

Place the optional phone beside the laptop. Allow the player to send Ella one message with a single checkmark or leave without sending. Pass that choice to `T11`. Coordinate Ella's old chat with `T10`.

**T09 / Coursework, overtime and police spotlights**

Wire the essay to the stay-in-bed and overtime branches. Crossing the open door at the end of the normal corridor leads to the horror corridor in Chapter 1 and the crash site in Chapter 2. Use two stationary police officers with readable flashlight sweeps, cover, and a safe retry. Boarding the bus hands off to Chapter 3.

**T10 / Clues & narrative foreshadowing**

```text
Place CLUE_01 photo with graduation promise, CLUE_02 work slip, CLUE_03
semester-five chat, CLUE_04 ambiguous partial post-crash voice recording.
Provide inspected UI and tracking without duplicate registrations. Never
state Ella is dead as objective fact. Keep CLUE_04 fair but non-spoiling.
Acceptance: clue count 0-4 is accurate after save/load and respawn.
```

## Phase 3 — Chapter 3 and endings

**T11 / Choice & ending system**

```text
Implement Chapter3 encounter with three mutually exclusive and diegetic
responses: Fight (click), Hide (crouch/cover), Approach (walk into trigger).
Atomic choice lock prevents accidental double triggers.
Prototype the Bizarre Edition conditions in a pure C# EndingResolver:
- Approach -> Good
- Fight OR Hide with all CLUE_01...CLUE_04 and Ella message sent -> Hidden
- Fight OR Hide otherwise -> Bad
- No response -> None
Do not use any numeric depression or moral health score.
Write EditMode NUnit tests for all branch cases and null/default state.
```

**T12 / Ending scenes**

```text
Implement placeholder endings first: GOOD shows montage of Raka resuming
studies, graduation in warm sunset/light rain, congratulatory messages, and
mysterious handwritten Ella letter. BAD shows quiet dark dorm and Bayu texts
asking where he is and why he missed class; imply tragedy without showing
method, body, or instruments. HIDDEN uses similar non-graphic atmosphere
but the key new Bayu message reads 'Si Ella udah sadar'. Provide subtitles,
credits transition and restart. No copyrighted songs or copied assets.
Acceptance: Run the GDD ending cases in the Windows build with the
message and clue combinations in Bizarre Edition.
```

## Phase 4 — Monster production, Blender props and polish

**T13 / Monster hero asset — Blender MCP → Unity**

```text
Task T13 only. Build ORIGINAL ACT_Shadow_Raka asset from GDD section 7.1A.
LOCKED TURNAROUND REFERENCE (WAJIB DIIKUTI):
- Use `Docs/References/Raka’s Manifestation Character Turnaround.png` (5-view turnaround:
  FRONT, LEFT SIDE, BACK, RIGHT SIDE, FRONT THREE-QUARTER) and the detailed breakdown in
  `Docs/References/ACT_Shadow_Raka_Turnaround_Spec.md` as the user-approved locked design fix.
- Rebuild the 3D mesh, proportions (HEAD / SHOULDER / WAIST / KNEE / FEET), blindfold + temple
  paper slips, long wet hair, torn shirt with circled red 'F' academic mark, X-back / diagonal
  chest leather straps, waist belt academic paper sheets, tattered pleated lower wrap,
  asymmetric left shin guard + right thigh strap, and elongated hands/fingers in Blender as
  faithfully as possible to this turnaround while keeping GDD non-gore limits and T06 doorway/capsule compatibility.
No copied franchise monsters or downloaded unreviewed meshes.

VISUAL LOCK:
- Humanoid distorted black silhouette, 2.25–2.45 m overall including slouch;
  elongated asymmetric arms/hands, slightly hunched, humanoid feet and posture.
- Wet charcoal/obsidian PBR skin + tattered college-jacket traces; few stuck
  paper pages / damaged course notes, not a readable brand/logo.
- Face partly hidden by wet hair/tendrils/fog. Closer: vague human likeness,
  superficial scar-like streaks and small, dark blood stains are OK.
  Offer two optional material looks using the SAME rig: A (mostly obscured
  face, faint scratches) and B (more visible superficial scars, dark blood
  smudges on face/jacket). Keep identity hidden until Chapter 3; avoid open wounds, organs,
  dismemberment, heavy splatter or graphic gore.
- Subtle cracked chest silhouette / accident light leak as symbolic motif;
  no glowing game-boss weak spot or oversized weapon/claws.

ASSET PIPELINE:
1) Inspect connected Blender MCP and Unity MCP tools; if disconnected,
   report blocker and give manual Blender/Unity steps, do not fake file edits.
2) Make .blend source with clean named meshes, applied transforms, correct
   metre scale and pivot, UV unwrap, 2K PBR materials, controlled alpha usage.
3) Use compatible Generic rig (not forced Humanoid retarget), document bone
   hierarchy, skin weights and animation export. Implement Idle_Breath,
   Walk_Lurch, Pursue, Search_Cover, Fight_Reaction, Hide_Pass,
   Approach_Reveal, Despawn. Root motion OFF initially; drive from AI motor.
4) Prepare LOD0 and LOD1 if justified; aim for ~20–40k triangles LOD0 as
   a starting budget, not rigid requirement; optimize based on build profiling.
5) Export ACT_Shadow_Raka.fbx; import with correct material remapping to
   Unity HDRP, Animator Controller, capsule/nav agent, occlusion rules,
   shadow settings and audio hooks. Save prefab ACT_Shadow_Raka.prefab.
6) Keep T06 placeholder functional: swap model/animator but preserve existing
   fight/hide/chase/checkpoint states and Chapter 3 approach trigger.

FPP QA:
- At 8–12 m in fog: monster reads as a threatening SILHOUETTE without
  revealing facial identity. At 2–3 m: partial face/scars visible without gore.
- No persistent monster in player's face; use occasional corridor silhouettes,
  occlusion and distance-based stalking; tune chase readability and audio cues.
- Test Idle/Pursue/Hide/Fight/Approach animation transitions and hit colliders;
  verify no clipping doorframes, missing materials, root motion drift or T-pose.
- Report Blender source/export, Unity prefab and materials, screenshots in
  FPP, draw/triangle costs, Windows profiling result, and blockers.
Acceptance: QA15 + QA16 pass and prior T06/T11 branch logic still works.
```

**T14 / Blender MCP for props & Raka FPP student body**

```text
If Blender MCP is connected, search Blender MCP asset libraries for laptop and casual student
references, then author and customize:
1) PROP_RakaLaptop (laptop mahasiswa di meja belajar Raka) -> PROP_RakaLaptop.fbx;
2) ACT_Raka_StudentBody (fisik tubuh FPP Raka ala MiSide organik dengan Subdivision Surface: kemeja
   kotak-kotak biru-charcoal lengan pendek berkancing, saku+pulpen, lanyard ID card, jam tangan hitam,
   tangan 5 jari terartikulasi, celana gelap, sepatu slip-on sol putih tanpa kepala agar bebas clipping
   kamera) -> ACT_Raka_StudentBody.fbx.
Use bevels, sensible UVs, URP material slots, applied transforms, verify scale/orientation in Unity,
and wire into MasterGameplay.unity. Report .blend and export file paths.
```

**T15 / Audio + accessibility**

```text
Create audio mixer buses Master/Voice/Ambience/SFX/Music, layer rain loops,
footsteps, heartbeats, phone vibration and subtitle cues. Add separate volume
settings, FOV, reduced headbob/flashes and readable UI scaling. All audio
must be originally created or properly licensed.
Acceptance: subtitles sync, audio ducks under dialogue, toggles persist.
```

**T16 / QA, performance, submission**

```text
Run full game smoke test and all 16 QA cases (QA01–QA16) in the GDD. Use Unity Profiler to
report CPU/GPU bottlenecks at 1080p in a Windows standalone build, profile
fog/shadow/SSR cost, test quality presets. Fix errors without redesigning the
ending rules. Generate build folder, controls README, bug list, asset licence
manifest and annotated demo notes. Verify T13 rig, partial face scars/blood
visibility, silhouette legibility and all chapter-ending regressions.
```

## Cross-document handoff rules

- **Keep the task IDs from GDD 9.3.** Use the active Chapter 2 plan for the updated T08–T11 scope and acceptance criteria.
- T06 = functional greybox monster; T13 = Blender hero monster asset + Unity prefab. They are intentionally separate.
- T07 = HDRP vertical slice, T08 = phone, T11 = ending logic, T12 = ending sequences, T16 = full QA. These IDs are FIXED.
- Preview / concept image is a visual reference, **not** a ready-to-use 3D model. Need manual artistic assessment for rigging, face and topology.
- No tool operation may be claimed complete until Unity/Blender reports actual saved files and validation passes.

## Tool setup references (check current versions)

- Unity official Codex plugin: https://unity.com/blog/unity-plugin-codex
- Unity MCP: https://unity.com/blog/unity-ai-mcp-how-to-get-started
- Unity HDRP system requirements: https://github.com/Unity-Technologies/Graphics/blob/master/Packages/com.unity.render-pipelines.high-definition/Documentation~/System-Requirements.md
- Community Blender MCP (not Blender Foundation): https://github.com/ahujasid/mcp-for-blender

Never give a remote AI agent unrestricted file/network access for the sake of convenience; use local connections and review changes in Git.
