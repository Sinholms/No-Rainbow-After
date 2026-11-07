# No Rainbow After — Implementation Tasks for Codex + Unity/Blender MCP

**Companion to:** `GDD_No_Rainbow_After_v1_1.docx`  
**Priority:** The GDD v1.1 is the source of truth. Original PPT/outline is reference; where documents conflict, the latest storyline and GDD win.  
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
| T08 | Phone/chat Ella & Bayu | Kedua pilihan tersimpan, Ella tidak membalas. |
| T09 | Tugas akademik + montage | Setiap jalur dapat meneruskan ke Chapter 3. |
| T10 | Clue registry + foreshadowing | 4 clue unik tersimpan tanpa membocorkan twist. |
| T11 | Chapter 3 + EndingResolver | Approach Good; Fight/Hide + 4 clue Hidden; lainnya Bad. |
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
LeftCtrl crouch, Esc pause, configurable FOV and sensitivity, disable headbob.
Add a simple test environment and explain how to tune serialized settings.
Acceptance: all controls work in FIRST PERSON (no third-person camera), player collides, no Console exceptions.
```

**T03 / Interaction & inspect**

```text
Implement IInteractable, PlayerInteractor (camera raycast 2.3m with occlusion),
interaction prompt and document inspect overlay. Make three test interactables:
book, work slip, locked door. Each object needs a stable ID and objective flag.
Acceptance: no interaction through walls; E prompts and overlay work; Esc closes.
```

## Phase 1 — Chapter 1 vertical slice

**T04 / Dorm + corridor greybox**

```text
Construct a 4x5m dorm and modular 12-18m corridor using primitives only.
Include bed, desk, books, doorway, 2 hide spots, CharacterController spawn,
colliders, initial objective and checkpoint. Set geometry under Chapter1Root.
Acceptance: playable 5-minute route, no missing collider and no softlock.
```

**T05 / Narrative + looping corridor**

```text
Implement GameState, ObjectiveTracker, ChapterDirector and C1 LoopAdvance
(max 3) with small changes to props and light/fog per loop. Add book and work-slip
objectives, a trigger for ShadowEncounter, an AudioDirector event interface and
subtitles placeholder. Persist four clue IDs using unique set semantics.
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

**T08 / Phone and chat**

```text
Build diegetic phone overlay with Ella and Bayu contact list, Ella's old chat,
choice Send message / Lock phone, and exactly one checkmark if sent. Do not
show Ella replying. Save messageEllaSent bool. Provide back navigation and
controller focus handling. Use original fictional phone UI (not an official WhatsApp trademark/interface copy).
Acceptance: both choices persist across chapter and reload.
```

**T09 / Academic tasks + montage**

```text
Implement three simple interactive mini tasks: order three notes, choose the
right internship document, and prioritize a schedule conflict. Track enum
NotStarted/Succeeded/Failed. No hard timer in Story setting. After all tasks,
slow movement temporarily and branch to normal transition or bed montage if
all three fail. In both cases player proceeds to Chapter 3.
Acceptance: no progression softlock, all-failed path tested.
```

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
Use a pure C# EndingResolver:
- Approach -> Good
- Fight OR Hide with all CLUE_01...CLUE_04 -> Hidden
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
Acceptance: QA09-QA11 from GDD pass in Windows build.
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

**T14 / Blender MCP for props**

```text
If Blender MCP is connected, make one original PBR desk as a TEST asset:
~1.2m wide, bevels, sensible UV, material slots, apply transforms, export
FBX or glTF as chosen by the project, verify scale/orientation in Unity,
then create a prefab with proper collider. Do not create all 3D props in one
batch. Report .blend and export file paths and any manual corrections needed.
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

- **GDD 9.3 and the table above must stay identical.** Do not use the old GDD v1.0 numbering (where T07 meant phone UI).
- T06 = functional greybox monster; T13 = Blender hero monster asset + Unity prefab. They are intentionally separate.
- T07 = HDRP vertical slice, T08 = Ella/Bayu phone, T11 = ending logic, T12 = ending sequences, T16 = full QA. These IDs are FIXED.
- Preview / concept image is a visual reference, **not** a ready-to-use 3D model. Need manual artistic assessment for rigging, face and topology.
- No tool operation may be claimed complete until Unity/Blender reports actual saved files and validation passes.

## Tool setup references (check current versions)

- Unity official Codex plugin: https://unity.com/blog/unity-plugin-codex
- Unity MCP: https://unity.com/blog/unity-ai-mcp-how-to-get-started
- Unity HDRP system requirements: https://github.com/Unity-Technologies/Graphics/blob/master/Packages/com.unity.render-pipelines.high-definition/Documentation~/System-Requirements.md
- Community Blender MCP (not Blender Foundation): https://github.com/ahujasid/mcp-for-blender

Never give a remote AI agent unrestricted file/network access for the sake of convenience; use local connections and review changes in Git.
