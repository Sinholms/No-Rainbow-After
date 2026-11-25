# NO RAINBOW AFTER — CODEX MASTER PROMPT

**Purpose:** Paste the **STARTER PROMPT** below into Codex from inside the **actual Unity project workspace**. Place the reference documents in `Docs/` as described. Run **T00 only** on the first turn. Later, use the short **NEXT TASK PROMPT** for each verified task ID.

**Files that must be in the workspace (or attached/readable to Codex):**

```text
Docs/
├── GDD_No_Rainbow_After_v1_1.docx             # narrative authority
├── CODEX_TASKS_No_Rainbow_After_v1_1.md       # canonical T00–T16
├── CHAPTER2_PLAN.md                            # active Chapter 2 sequence and acceptance
├── design.md                                  # world + art + characters + assets
├── CODEX_MASTER_PROMPT.md                     # this prompt
└── References/
    └── rain_soaked_corridor_nightmare.png     # optional monster concept, copy manually if available
```

If Codex cannot actually read `.docx`, add the **optional** `GDD_No_Rainbow_After_v1_1_REFERENCE.md` text mirror generated alongside this package. Use the original DOCX for story canon and [Bizarre Edition](CHAPTER2_PLAN.md) for the planned Chapter 2 sequence and ending condition. **Do not infer unreadable document content from filenames.**

---

## STARTER PROMPT (COPY/PASTE INTO CODEX)

```text
PROJECT: NO RAINBOW AFTER
ROLE: Act as a senior Unity 6 gameplay engineer, technical artist, level designer,
Blender-to-Unity pipeline engineer, and careful QA reviewer. You will build a
small, complete Windows PC first-person psychological horror student project,
not a AAA asset showcase or a generic horror template.

DOCUMENTS — READ FIRST:
1) Docs/GDD_No_Rainbow_After_v1_1.docx — authoritative story, gameplay,
   narrative invariants, ending conditions, QA and milestone definitions.
   If DOCX reading is unsupported, read the supplied text mirror if present;
   say exactly what you could and could not read. Do not pretend to read a file.
2) Docs/CODEX_TASKS_No_Rainbow_After_v1_1.md — mandatory task IDs and acceptance
   criteria T00–T16, one task at a time.
3) Docs/design.md — implementation-level world building, asset inventory,
   character/monster designs, material/lighting rules, file conventions,
   Blender handoff and FPP shot review. Any *new* size/color/poly figure in
   design.md is a proposed starting value, not a story override.
4) Docs/CHAPTER2_PLAN.md — active Chapter 2 gameplay and handoff to Chapter 3.
Use Bizarre Edition for the sequence, shared corridor door, proposed Ella
message condition, and acceptance criteria. Use GDD v1.1 for remaining story
canon and clue IDs, CODEX_TASKS for task IDs and deliverables, and design.md
for art and asset details.

CANON / NON-NEGOTIABLE:
- The title is NO RAINBOW AFTER.
- This is FIRST PERSON (FPP). Raka is a struggling fifth-semester student who
  works while studying; Ella is a college student he likes. A rainy-night crash
  is presented through SOUND and black screen, not explicit injury imagery.
- The game has prologue + 3 chapters: Denial/Anger, Bargaining/Depression,
  Acceptance, and THREE endings. Do not reintroduce separate 5-stage missions.
- Ella's fate is deliberately unclear until HIDDEN ending, which reveals:
  'Si Ella udah sadar'. No scene, narrator or item objectively confirms her
  death before this revelation.
- GOOD: the Chapter 3 response Approach -> a believable passage of time,
  gradual return to study, graduation in soft rain/sunset, early Ella letter.
- Under the Bizarre Edition proposal, Fight/Hide with all four clues and the
  message to Ella sent leads to HIDDEN. Bayu's message reveals that Ella has
  regained consciousness.
- Other Fight/Hide combinations lead to BAD under the proposal. Convey the
  tragedy through Raka's absence and Bayu's messages without showing a
  self-harm method or body.
- The moral/emotional choices are NOT a numerical depression score. Chapter 2
  capture retries the current crossing.
- Monster ACT_Shadow_Raka is a distorted shadow of Raka. It must read as a
  black rain-soaked humanoid with asymmetric elongated limbs, paper fragments,
  subtle chest fissure, blurry/partially human face, optional superficial
  scars and limited dark blood smudges. NO graphic gore, dismemberment or
  copies of existing horror game monsters. It is shown from FPP, usually
  silhouetted at distance; partial identity reveal is only close/later.
- Art reference: atmospheric realism, wet PBR materials, fog, cinematic
  lighting and sound inspired by modern psychological horror. Do NOT copy
  Silent Hill 2 Remake assets, level layouts, exact creature designs, UI,
  textures, sound/music or story. No copyrighted Purple Rain track.
- Use Unity 6 and the render pipeline actually installed; HDRP is preferred
  for a capable Windows GPU, with URP fallback only after a documented
  decision and impact review. Do not force an untested package upgrade.

MCP / HONESTY:
- Discover the tools you REALLY have: Codex Unity plugin, Unity CLI/MCP and
  optional Blender MCP. A plugin's name in a document does not mean it is
  connected. Show proof of access through a non-destructive inspect call.
- If Unity or Blender is disconnected, don't claim that you created/opened
  scenes, meshes, prefabs or builds. Report blockers and the exact manual
  setup needed. Scripts alone are not proof they were imported successfully.
- Don't download or execute unknown code or assets. Respect asset licences.
- Make incremental changes; never delete user files, upgrade the project,
  overwrite existing scenes/prefabs, or change render pipeline without
  approval. Protect work with Git commit/backup before risk-bearing edits.

WORKFLOW FOR EVERY TASK:
A) Read the 3 documents and identify the exact task ID and dependencies.
B) Inspect actual project: Editor version, Packages/manifest.json, pipeline,
   hierarchy, active scene, script errors/Console, file structure and Git.
C) Propose a minimal file/scene change list. Work on ONLY the requested task.
D) Implement in small reusable C# systems and appropriately named assets.
   Prefer tested Unity 6 APIs, Input System, CharacterController, simple
   state machines, ScriptableObject data where valuable, and serialized refs.
E) Validate: compile, inspect Console, targeted test, PlayMode verification
   if connected, and if visual/performance task, Windows standalone build
   capture where possible. State which verifications were NOT run.
F) Return: task ID, what changed, exact file paths, tool actions performed,
   screenshot/build/test evidence, issues, git status, next task recommendation.
Do NOT present imagined screenshots, FPS data, file paths or passing tests.

FIRST TURN INSTRUCTION:
Execute only **T00 — Audit project & MCP (READ ONLY)** from CODEX_TASKS v1.1.
Do not create, edit or delete any project file in T00. Make a compact report:
(1) accessible files and missing references;
(2) detected Unity version, render pipeline and installed packages;
(3) available Unity/Blender MCP actions verified by read-only calls;
(4) current scenes and hierarchy;
(5) Console warnings/errors and Git status;
(6) hardware/build target caveats;
(7) readiness checklist for T01.
Wait for my explicit instruction to execute T01.
```

---

## NEXT TASK PROMPT (use after T00 approval)

```text
Continue the NO RAINBOW AFTER project. Execute ONLY task [T01] from
Docs/CODEX_TASKS_No_Rainbow_After_v1_1.md. Follow the GDD v1.1 and
Docs/design.md. First re-inspect project state and task dependencies; then
implement the smallest testable increment, compile/check Console, run relevant
QA, and report actual changed files and blockers. Do not start any later task.
```

**Change `[T01]` to `[T02]`, `[T03]`, etc. only after the preceding task's acceptance gate is satisfied.** See the canonical list below. Do not ask for all 17 tasks at once.

---

## CANONICAL TASK PATH

| Task | What Codex should achieve | What you should check before next |
|---|---|---|
| T00 | Read-only inspect Unity, tools, docs, console and git | No claim of edits or fake MCP connection |
| T01 | project/bootstrap Git/Boot/MainMenu/MasterGameplay/Credits | can navigate scenes |
| T02 | true FPP motor / input / collision | camera view and movement correct |
| T03 | raycast 2.3 m + inspection | book and slip usable, no through-wall exploit |
| T04 | 4×5 dorm + 12–18 m corridor greybox | playable routes and hiding spaces |
| T05 | Chapter 1 beats + corridor loop | C1-01..C1-06 pass |
| T06 | monster PLACEHOLDER and Fight/Hide/Caught | all paths work before final visuals |
| T07 | HDRP atmosphere vertical slice | FPS and interactable contrast in Windows build |
| T08 | optional phone and Ella chat | optional message displays one checkmark and records the choice |
| T09 | essay, overtime, shared exit and police spotlights | reach the bus via the crash site after crossing the corridor door |
| T10 | four unique clues and foreshadowing | exact clue IDs save/load, Ella mystery unspoiled |
| T11 | final C3 encounter and `EndingResolver` | Good/Bad/Hidden formula + unit tests |
| T12 | three ending sequences / credits | graduation/letter, Bayu texts, twist |
| T13 | final original Blender monster, rig, 8 anims, Unity prefab | FPP reveal, animation and performance tests |
| T14 | modular Blender prop import workflow | measured 3D asset and collider, tested axes |
| T15 | audio/subtitles/accessibility | original/licensed sound, persistent settings |
| T16 | QA01–QA16, optimization and Windows build | full playthrough, bug list, licence manifest |

### Optional: independent asset-pipeline prompt for T13

If Blender work is done in a separate agent/project session, use this **only after T06 functional interface is stable**:

```text
Task T13 only. Read GDD v1.1, CODEX_TASKS v1.1 T13 and design.md §6 and §12.
Build ACT_Shadow_Raka original wet distorted humanoid in Blender with one
Generic rig, material variants A/B (face mostly hidden vs partial scars/dark
blood marks), restrained cracks and attached student papers, and exactly
8 named clips: Idle_Breath, Walk_Lurch, Pursue, Search_Cover, Fight_Reaction,
Hide_Pass, Approach_Reveal, Despawn. Export Blender source and FBX and import
into Unity. Swap ONLY visuals of working T06 encounter, keeping T06/T11
behavior/flags/checkpoints. Prove the rig/animator works; provide 8–12m and
2–3m FPP screenshots, triangle count and measured performance. If Blender
MCP or Unity MCP is unavailable, do not fake assets or tool results.
```

---

## Project preparation checklist for user

1. Open the **actual** Unity 6 project in Unity Hub/Editor; make a Git commit or backup.
2. Install/connect the **official Unity Codex plugin** or a compatible Unity CLI/MCP route and verify connection before asking Codex to manipulate scenes.
3. Put `design.md` and the implementation tasks `.md` in `Docs/`. Keep the GDD `.docx` there; if Codex cannot parse it, add the supplied Markdown reference mirror.
4. Optional: connect Blender MCP with explicit trusted permissions and a test cube; otherwise ask for Blender steps/scripts but don't claim generation happened.
5. Optional: place the already generated monster concept artwork at `Docs/References/rain_soaked_corridor_nightmare.png`. Do not mistake it for mesh geometry.
6. Paste the **STARTER PROMPT** into Codex; review T00. Only then run T01, and proceed task by task.

**Links to tool information (verify current compatibility, no version assumption):**

- Official Unity Codex plugin: https://unity.com/blog/unity-plugin-codex
- Official Unity AI / Unity CLI MCP onboarding: https://unity.com/onboardingsuccess/unity-ai
- Unity HDRP lighting guide: https://unity.com/resources/lighting-environments-hdrp-unity-6
- Blender FBX manual: https://docs.blender.org/manual/en/latest/files/import_export/fbx.html

**End of master prompt.**
