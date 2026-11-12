# NO RAINBOW AFTER — WORLD, CHARACTER & ASSET DESIGN BIBLE

**Document:** `design.md`  
**Version:** 1.1-compatible / Production Design v1.1\
**Updated:** 2026-11-07 (original: 2026-11-06)\
**Project:** First-person psychological horror / PC Windows / Unity 6 / installed URP; original HDRP target unresolved / Blender\
**Companions:** `GDD_No_Rainbow_After_v1_1.docx` + `CODEX_TASKS_No_Rainbow_After_v1_1.md`  
**Audience:** Game designer, environment artist, technical artist, programmer, Codex agents (Unity MCP; Blender required for 3D asset authoring).\
**Status:** Operational design specification; **new concrete art/layout values in this file are IMPLEMENTATION PROPOSALS**, not amendments to established story canon.

> **Read this first.** `GDD v1.1` owns **story, narrative invariants, gameplay, ending rules, schedule and task IDs**. `CODEX_TASKS v1.1` owns task execution **T00–T16**. This file owns **how worlds, characters, props, materials, visuals and assets should be designed and handed to Unity**. Explicit user instructions take precedence; the presentation/workflow decisions recorded below do not amend story canon. Otherwise, report conflicts against the GDD before editing the affected feature. Concept art is a **2D reference**, not a finished 3D mesh. Do not claim a feature or asset exists until inspected in the actual Unity/Blender project.

---

## 0. Design lock — facts that MUST NOT drift

| Topic | Final contract |
|---|---|
| Title | **NO RAINBOW AFTER**; do not rename to *Purple Rain* or *Selamat!*. |
| Player view | **FPP only** during gameplay; eye-level Raka viewpoint. Cinematic ending shots may use staged cameras where explicitly specified. |
| Story | Raka is a financially struggling **5th-semester student**, studies and works, experiences a rainy-night motor crash with **Ella**, a college student he likes. The crash is **audio-only**. |
| Ella | The player is led to believe she died, but this is **not objectively confirmed**. The **Hidden Ending** reveals: **“Si Ella udah sadar”**. No earlier scene may confirm death as fact. |
| Chapters | **Prologue** → **C1 Denial & Anger** → **C2 Bargaining & Depression** → **C3 Acceptance** → ending. No revival of the old five-stage structure. |
| Good | C3 **Approach** → Raka slowly returns to academic life → **graduation** in light rain/sunset → mysterious early letter from Ella. |
| Bad | C3 **Fight or Hide**, without all clues → tragic **implied** absence; phone receives Bayu’s concerned messages. No depiction of a method or body. |
| Hidden | C3 **Fight or Hide**, with **CLUE_01–CLUE_04 all collected** → similarly somber room but Bayu message says **Ella regained consciousness**. Twist, not an explicit happy outcome. |
| Clues | `CLUE_01` photo/promised graduation; `CLUE_02` overtime slip; `CLUE_03` old semester-five chat; `CLUE_04` ambiguous post-crash voice recording. Unique IDs persist. |
| Monster | Shadow of Raka: **human, distorted, drenched, guilty**, not a demon or generic boss. Blurred face, optional **superficial scars / restrained dark blood marks**, never graphic gore. |
| Scope | No open world, combat-tree, inventory RPG, full procedural levels, ray tracing requirement, or mandatory third-person animation. |
| Theme | Poverty, barriers to education, grief, and mental health; **SDG 1, SDG 4, SDG 3** through environment and story, never through a numeric 'depression score'. |

### 0.1 What is new in this document?

Unless explicitly marked **implemented** or **user requirement**, specific `size`, `hex color`, `asset filename`, `poly budget`, `lighting configuration` and `blockout layout` entries are **proposed implementation defaults** to validate in-editor. Canonical values from GDD—dorm approximately **4 × 5 m**, corridor **12–18 m**, camera height approximately **1.65 m**, standard door height approximately **2.05 m**, FOV starting at **75°**, T00–T16, and clue and ending contracts—remain unchanged.

### 0.2 Asset sources and rights

Only use original models, original audio, legally licensed assets with recorded source/license, or placeholders made by the team. *Silent Hill 2 Remake* is a **quality/mood reference only**. The user's *Silent Hill f* menu reference guides spacious composition, atmosphere and lighting; *Pacify* guides the requested oppressive first-person mood. These references are not sources of copied creatures, environments, UI, logo, sound or textures, and the circular POV is a user requirement rather than a claim of reproducing Pacify's exact effect. Do not use the copyrighted Prince song *Purple Rain* without a suitable license. Avoid using real WhatsApp branding/icons; create a fictional messaging UI with familiar functionality.

### 0.3 Implemented baseline — verified handoff, 2026-11-07

This is a dated implementation snapshot, not a claim that the full design below exists. Recheck the live project before further work.

| Area | Implemented state / remaining work |
|---|---|
| Editor / renderer | Unity **6000.6.8f1**, **URP 17.6.0**, Input System **1.20.1**. The project originated in URP; no HDRP migration or final fallback decision has been made. |
| T01 / menu | Boot, MainMenu, MasterGameplay and Credits; original imagegen rainy-kos menu background with live UI. Menu art is 2D; gameplay uses realtime 3D. |
| T02 / player | True FPP movement, sprint/stamina, crouch/ceiling checks, pause, sensitivity and FOV preferences. `FirstPersonTest.unity` remains an isolated control/debug lab. |
| Current gameplay art | New Game enters the Blender-authored kos and corridor in `MasterGameplay.unity`, with `ENV_KosAtmosphere` under `Chapter1Root`. Room **4 × 5 m**, ceiling **2.7 m**, corridor **14 × 1.8 m**, two usable side niches with spatial markers. Monster hiding behavior remains T06. |
| Blender assets | `BlenderSources/Environment/KosAtmosphere_v1.blend`, generator and manifest; exported `Assets/_NoRainbowAfter/Art/Models/Environment/ENV_KosAtmosphere_v1.fbx`: **226 meshes / 63,128 triangles**. Bed/cloth, desk/chair, fan, books/notes, window, doors, shoes/bag/umbrella, cables and hallway utility props are present. This remains an initial art pass, not final photorealism approval. |
| Materials / atmosphere | Six BaseColor/Normal/Mask sets: Wall, Floor, Wood, Cloth, Metal, Paper. Wall/Wood base colors use imagegen; other maps are procedural approximations, not calibrated scans. Warm desk/cold window lighting, camera-follow spotlight, fog, room reflection probe, ACES, light grain and original synthesized rain are implemented. |
| POV | Aspect-correct circular black mask, soft edge, pause toggle/radius slider and saved preferences; see §11. |
| Verification | Last art-pass handoff: **14/14 PlayMode tests passed**; Windows x64 Mono preview built with **0 errors / 0 warnings**. Build success and Editor screenshots do not establish standalone FPS or a complete playthrough. |
| T03 / interaction | Camera raycast 2.3 m with solid occlusion and self-collider exclusion, E prompts, readable inspect overlay and Esc priority; book, work slip and locked hallway door have stable IDs and session objective flags. Verified 22/22 PlayMode tests and Windows build, 0 errors/warnings. See [T03 handoff](T03_HANDOFF.md); no saved clue registry or chapter gate yet. |
| T04 / route readiness | Chapter1Root, initial objective guidance, dorm spawn/session checkpoint with pause recovery button, and two walkable cover niches. Blender niches were deepened and cabinets relocated; mesh/triangle counts unchanged. Verified 25/25 tests including 313.5 s simulated traversal. See [T04 handoff](T04_HANDOFF.md); human pacing and monster hide resolution remain unverified/pending. |
| T05 / narrative foundation | GameState, ObjectiveTracker, ordered C1 beat/event director, maximum three corridor-return loops, mild prop/light/fog variations, T03 book/slip bridge, clue-only unique ID persistence and subtitle/audio/encounter contracts. Verified 33/33 PlayMode tests; C1-04–06 use encounter test doubles and a transition request, not playable monster/cinematic/phone. See [T05 handoff](T05_HANDOFF.md). |
| Still pending | Prologue, functional hide/monster, Chapter 1 transition cinematic, phone/tasks/full clue placement, full saves and endings. The art pass and T03–T05 do **not** complete Chapter 1 or the later tasks. |

Evidence and limitations: [kos atmosphere handoff](KOS_ATMOSPHERE_HANDOFF.md), [T02 handoff](T02_HANDOFF.md), [PlayMode results](QA/KosAtmosphere_PlayMode_results.json), [Windows build report](QA/KosAtmosphere_Windows_build.json). Build output: `Builds/KosVisualPreview/NoRainbowAfter.exe` (local, ignored by Git).

### 0.4 User requirements for subsequent work

- Use this file for environment, character, monster, item, texture, lighting and Blender asset decisions on every relevant task.
- Gameplay should feel grounded and creepy: believable silhouettes, cloth/curved forms, material wear, layered lighting and deliberate clutter. Plain box geometry is acceptable for temporary collision/blockout checks, not the presented finished environment. If realism is beyond current capability, deliver a coherent horror look and state the limitation.
- Author new 3D assets in **Blender**, then integrate in Unity. Imagegen supplies original bitmap assets/references, not finished 3D models. If Blender cannot be used, tell the user **before** substituting another 3D production method.
- Keep the current work visible in Blender/Unity; headless generation may assist, but open/update the resulting scene for inspection. Do not restore an old scene/view after work or discard unrelated user changes.
- Delegate code as specified in §17.1: **GPT-6.1 Terra / GPT-6.0 Luna**; the lead agent handles assets and verification. Make a scoped **local commit after game-screen changes**; do not push without instructions.
- The visual pass is complete; the user requested discussion afterward. This document revision does not authorize starting the next gameplay task.

---

## 1. Experience goals and visual thesis

**Elevator visual pitch:** A realistic, oppressive Indonesian student boarding house whose familiar objects subtly mutate into visual evidence of exhaustion, regret, and unprocessed memories. Rain binds prologue, monster, and conclusion. The most frightening thing is a wet humanoid silhouette whose posture sometimes resembles Raka.

**Art pillars**

1. **Familiar → wrong:** recognizable college/work objects, then micro-changes, missing items, displaced photos, stretched perspective, light failure.
2. **Body and space carry emotion:** trembling fluorescent light, shallow breathing, shadow in doorframe, water marks, ink bleeding, repetitive hallways.
3. **Horror with legibility:** true darkness around focal points, not complete blackness; important doors/items are readable through contrast, composition, and audio.
4. **Rain as memory:** heavy storm = crash, unresolved dread; fine rain + warm sky = graduation and continued life. Never assume that one happy shot means a clinical cure.

**Mood target:** atmospheric photorealistic/grounded horror inspired by modern survival-horror lighting and material detail, with **original** cultural/location identity. Realistic **hero room** first; repeat modular kits rather than building five unique AAA-sized maps.

**Visual hierarchy (FPP):** path/exit > interactive object > monster silhouette > secondary decorative set dressing. Horror objects should interrupt, never make the play space unreadable.

---

## 2. World rules & narrative geography

### 2.1 Real world vs. mindscape

| Rule | Everyday reality | Raka’s subjective / mindscape distortion |
|---|---|---|
| Geometry | Scale and doors roughly plausible | Same modular mesh reused; repeated corridor, room orientation/prop arrangement changes |
| Light | Fluorescent desk lights, weak window spill, phone screen | Pools of light relocate; distant illumination has ambiguous origin |
| Sound | Fan hum, rain against window, traffic, phone vibration | Rain heard indoors, breath grows spatially impossible, voice fragment repeats |
| Object placement | Study/work clutter conveys struggle | Photo turns around, notebook moves, overtime sheet reappears, wet footsteps stop unexpectedly |
| Monster | Not objectively observable in normal scenes | Manifestation of guilt; no in-world NPC calls it a literal demon |
| Ella | Voice, belongings, traces, past conversation | Memory can be fragmented; no objective death confirmation before Hidden |

### 2.2 Spaces and transitions (single MasterGameplay scene)

```text
Boot → MainMenu → MasterGameplay
                          |
                          +-- Root_Prologue    [AudioCrash]
                          +-- Root_Chapter1    [Dorm + LoopedCorridor + ShadowEncounter]
                          +-- Root_Chapter2    [DormStudy + CampusHint + WorkHint + Phone]
                          +-- Root_Chapter3    [ConfrontationHall + ShadowFinal]
                          +-- Root_EndingGood  [StudyMontage + GraduationShot + Letter]
                          +-- Root_EndingBad   [EmptyDorm + BayuChat]
                          +-- Root_EndingHidden[EmptyDorm + BayuTwist]
                          |
                          +----------------------→ Credits / MainMenu
```

This is the **planned logical** hierarchy, not a description of the current scene or permission to leave all geometry and lights active simultaneously. Current gameplay is the `ENV_KosAtmosphere` art/test environment (§0.3); chapter roots, prologue routing and endings remain to be implemented. Implement chapter roots with deterministic activation and spawn points, fade/blackout, and state restore. The ending zones may be staged within MasterGameplay or implemented with an equivalent safe loading method: preserve the GDD's externally observable scene flow.

### 2.3 Map boundaries

- Create a **small, replayable route** with believable walls, locked doors and controlled blackout transitions rather than a sprawling dorm/campus.
- Sound from unseen places expands the world while keeping modeled geometry small.
- **No explicit gameplay at the moment of impact:** prologue is audio-first, camera dark; any exterior vehicle shot must be non-graphic.
- The graduation sequence can be a compact cinematic set; never require a fully explorable campus or fully rigged crowd for MVP.

---

## 3. World building — spaces with production measurements

> Scale: **1 Unity unit = 1 meter**. All numbers other than GDD canon are **suggested starting values**. Block out and run a CharacterController/camera check before high-detail modeling.

### WB-01 — Kamar Kos Raka (hero room, Chapter 1 + Chapter 2 + ending)

**Purpose:** primary emotional anchor, interaction tutorial, repeated return point, source of all social and academic context. **Base shell:** approximately **4 m × 5 m** interior, **2.7 m** ceiling (GDD), with one door, one window, desk/study wall, bed, small storage/utility corner.

**Suggested readable layout:**

```text
+----------------------------- 4 m -----------------------------+
| window / rain glass      fan / cable        photos / calendar |
|                                                               |
| [bed 0.9x2m]                               [study desk 1.2m] |
| [folded blanket]            WALK ZONE      [chair / books]    | 5m
|                                                               |
| water jug / shoes             [DOOR 0.9m] [bag / work slip]    |
+---------------------------------------------------------------+
```

**Set dressing:** peeling plaster, inexpensive worn laminate desk, basic mattress, electrical extension, family envelope or **fictional** budget list, class notes, internship forms, overtime slip, cracked watch, shoes, damp umbrella. Avoid misery clichés or excessive random trash: poverty should be seen in **lack of space, repairs, schedule overload, and reuse**, not caricature.

**Environment states**

| State ID | When | Set change | Lighting and sound |
|---|---|---|---|
| `Dorm_Normal` | C1-01 | Clutter plausible; study materials in reach | Dim fluorescent warm-neutral with weak blue window spill; rain outdoors |
| `Dorm_Loop1` | first return | One note moves; photo angle changes | Subtle flicker; extra distant footstep |
| `Dorm_Loop2` | second return | Overtime slip now conspicuous; wet footprint | Cooler exposure, hum lower-frequency |
| `Dorm_Loop3` | final loop | Ella-related anomaly and shadow cue in door | Increased contrast, single moment of silence |
| `Dorm_Depression` | C2 | Heavier desaturation; study objects dominate | Phone provides focus light; no full black-out |
| `Dorm_Ending` | Bad/Hidden | Empty, still, silent room; phone visibly active | Night blue, strong rain; never show means of self-harm |

**Interaction focal points:** book `INT_BOOK`, overtime slip `INT_OVERTIME`, picture `INT_CLUE_01`, notebook `INT_STUDY`, window `INT_WINDOW_RAIN`, door `INT_C1_DOOR`, phone `INT_PHONE`.

**Rule:** heavy environment swaps must NOT destroy saved objects or reset `CLUE_*` flags.

### WB-02 — Corridor / Looped Kos (C1, also reused C3)

**Purpose:** spatial confusion, silhouette distance, chase/hide. **Blockout:** 12–18 m route, modular pieces **2 m or 4 m** long, 1.6–2.0 m walkable width (proposal), side niches for **2+ cover zones**. Player-height hallway door consistent with GDD (~2.05 m). Monster visual height up to 2.45 m requires **separate collision behavior** (see §6.6) and must not clip through low doorways.

**Kit:** wall-left/right, end cap, corner, ceiling, floor, exposed pipe, doorframe, utility panel, old light fixture, leak decal, water-puddle decal, hide niche/closet geometry. Every repeated segment needs variable prop/light layers so the loop changes visually without fully rebuilding the hallway.

**Loop communication:** use **a single clear anomaly** per loop (moved photo → changed paper → shadow) and keep the exit path readable. Door interaction includes an understated prompt. Limit to 2–3 loops per GDD. Do not spawn the monster while the player has no room to move or hide.

### WB-03 — Study / Internship Desk (C2)

**Purpose:** three short academic tasks in increasingly burdensome atmosphere. Reuse WB-01 desk and perhaps a different camera framing rather than building a new full interior. Include three clearly segmented task objects: **notes**, **internship paperwork**, **schedule**. Movement is heavier, but controls remain responsive and every task can be failed without a progression softlock. No hard timer in Story mode.

**Visual feedback:** documents slide from tidy to disordered; pen, notification light and rustling create focus. UI inspect overlay must make text legible regardless of screen resolution.

### WB-04 — Campus / Work hints (C2, optional modular micro-set)

**Purpose:** communicate pressure from outside Raka’s room without full world construction. Tiny corridor vignette with noticeboard (“Magang”, “Tugas”, “Konsultasi”) and a **fictional** work clock or shift board. Do not name a real university/business without permission. Can be conveyed through photo, sound and one doorway vista if time is limited.

### WB-05 — Crash Memory / Wet Road (mostly audio, optional 3D vignette)

**Purpose:** symbol of fragmented accident, not a playable realistic traffic sequence. A short 4–8 m wet road section, safety barrier, blurred twin headlights, torn umbrella or helmet, rain and distant emergency color cues. **No impact animation or Ella’s injury/body**. During gameplay, use short spatial glimpses in Chapter 3 memory collage rather than a fully simulated road/truck chase.

### WB-06 — Chapter 3 Confrontation Hall

**Purpose:** single readable arena for **Fight / Hide / Approach**. Reuse corridor walls, adding a wide end chamber (proposal ~6 × 8 m), two side cover volumes, one clear central approach lane, half-hidden exit behind shadow. Stage monster at 8–12 m and briefly reveal facial hints at 2–3 m. Keep backlighting and fog controlled, avoid overexposing the dark creature into invisibility.

**Choice staging:**
- **Fight:** contextual left-click only at allowed range; one scripted physical response, not a health-based boss.
- **Hide:** crouch inside legitimate cover zone, wait for encounter resolution; no moral judgment UI.
- **Approach:** player walks forward to trigger zone; camera must remain first person until scripted transition.
- Resolve choices atomically once, disable input only during transition; save and route using canonical `EndingResolver`.

### WB-07 — Graduation / Good Ending micro-set

**Purpose:** visible proof that Raka resumes studies and graduates after time passes. **Not instant recovery.** Production-efficient cinematic: (1) 2–3 snapshots of studying/work rebalancing and time passing, (2) stage/campus courtyard with sunset + fine rain, (3) congratulations from family/friends, (4) close-up of envelope and original handwritten Ella letter. Natural warm amber, blue clouds and wet pavement; neither copy nor remake a real university graduation set.

**Framing:** one 6–10 m hero shot area, background student silhouettes, 2–4 static/low animation figures if resources permit. Letter visual is central; UI inspect text must be subtitled/readable.

### WB-08 — Bad / Hidden End Room

Reuse WB-01 geometry and phone focus. Establish absence with **camera, sound, room state, unanswered messages**; avoid displaying tools, wounds or physical method of suicide. Compose phone at 1–2 m viewing distance, with large legible onscreen text. Same lighting/room base for Bad and Hidden, **different message payload** after deterministic resolver; prevent accidental branch leakage.

---

## 4. Environmental storytelling — props tell the story, not exposition

| Narrative pressure | Environmental evidence | Suggested interaction/response | GDD relation |
|---|---|---|---|
| Economic hardship | repaired shoes, cheap groceries, overtime slip, split rent note, modest room | inspect overtime, glance at work hours | SDG 1, `CLUE_02` |
| Education barriers | worn textbooks, shared notes, internship forms, packed deadlines | book pickup, 3 academic tasks | SDG 4, C1/C2 |
| Fatigue before crash | watch, shift schedule, untidy notes, tired voice | short fragment in prologue, overtime slip | Accident causal chain |
| Raka–Ella relationship | graduation photo, subtle shared memory, old chat | inspect photo, old chat, envelope | `CLUE_01`, `CLUE_03` |
| Fragmented memory | partly audible voice record, blurred wet road lights, repetitive corridor | inspect recording; environmental echo | `CLUE_04`; preserve twist |
| Social withdrawal | unanswered but not malicious chats, missing attendance messages | phone notifications from Bayu | Bayu / ending |
| Hope without easy cure | clearer routine, realistic time skip, graduation in drizzle | observe graduation and read letter | Good Ending |

**Fair mystery rule:** Each clue means something true but incomplete. The final message about Ella must feel **possible in retrospect**, not be contradicted by objective evidence. A one-checkmark fictional chat status **does not prove death**.

---

## 5. Character bible

### CHAR-01 — Raka (protagonist, first person)

- **Role:** 5th-semester student working part-time, physically exhausted and grieving after the crash.
- **Personality:** responsible and self-demanding, tries to keep routine, struggles to ask for support; allow nuanced expression beyond constant hopelessness.
- **Visual language:** ordinary budget student clothing, worn backpack, simple analog/digital watch; wet sleeves/hands on rainy sequences. Not a tactical or heroic avatar.
- **FPP pipeline:** visible body **not required for MVP**; optional `Arms_Raka` and occasional hands for book/phone/letter interactions. Main storytelling uses voice/breath, screen height, footsteps, environmental framing. Do not build a full third-person controller.
- **Voice:** Indonesian natural student register; small pauses and controlled breath, not constant screaming.
- **Story consistency:** Raka **believes** he lost Ella; do not state she objectively died in any neutral narrator text.
- **Graduation:** use a separate clean torso/portrait or carefully staged hand/robe sequence; no need for full NPC facial rig to prove completion.
- **Deliverables:** voice scratch track (original), optional hand mesh & skeleton, chapter look references, graduation costume variants only if on camera.

### CHAR-02 — Ella (supporting, voice and memory)

- **Role:** college student Raka likes; emotional anchor and promise to graduate together.
- **Presentation:** original voice during rain prologue; traces via personal objects, photograph, text, and letter; **full realtime 3D model not required** for playable MVP.
- **Humanization:** concrete details (shared class jokes, ink handwriting, helmet or umbrella) more important than polished character render.
- **Ambiguity:** do not include tombstone, death certificate, unambiguous funeral, medical note certifying death or objective obituary in normal story.
- **Hidden truth:** alive and has regained consciousness; reveal through Bayu message, not a surprise combat encounter.
- **Letter:** written earlier as a playful **future-graduation time capsule**. This is a proposed in-world rationale consistent with GDD; don't reveal everything too early.

### CHAR-03 — Bayu Kopling (supporting, UI/audio only)

- **Role:** fellow student whose messages make absence socially visible and deliver plot twist.
- **Visual production:** fictional profile icon / initials, phone name, a handful of Indonesian chat lines; no full character rig required.
- **Required Bad text (original outline):** “Ka, lu di mana?”; “Lu kenapa ga masuk grafkom tadi?”; “Pak dosen nanyain lo.”
- **Required Hidden text:** “Ka, lu di mana?”; **“Si Ella udah sadar.”** Preserve reveal wording.

### CHAR-04 — Family / classmates (Good Ending supporting silhouettes)

- **Role:** congratulations, ordinary human connection, not proof that everyone supported Raka perfectly during the crisis.
- **Production:** silhouettes, cropped framing, voice-over or caption; P2 full rigs only if schedule allows.

---

## 6. MONSTER BIBLE — `ACT_Shadow_Raka`

### 6.1 Concept and emotional function

The monster is **Raka turned into the shape of his guilt**, not a separate species. Distant: frightening anonymous mass; close: a wet student-jacket remnant, familiar shoulders, paper fragments and barely suggested human face. Avoid familiar franchise creature silhouettes and explicit weapons. The creature should be memorable from **shadow and sound** before its detailed mesh is visible.

### 6.2 Silhouette + proportions (starting spec)

| Feature | Proposed model | Narrative rationale |
|---|---|---|
| Height | **2.25–2.45 m visible height**, hunched; scale checked in Unity | feels overburdening, uncanny |
| Head | slightly downturned, asymmetric, hair/tendrils masking face | denial and withheld identity |
| Torso | uneven shoulders, rib/chest crack-like indentation | trauma, not a literal glowing weak point |
| Arms | elongated to near mid-thigh/knee or lower, asymmetric | predatory silhouette, human distortion |
| Hands | very thin/elongated fingers, **not** oversized knife claws | uneasy without becoming a monster boss |
| Legs/feet | recognizable human leg chain and broad planted stance | implies it could be Raka |
| Surface | wet charcoal/obsidian, ripped old jacket / cloth remnants | rain and everyday identity |
| Embedded objects | 3–6 scraps of academic paper, rain-soaked | material weight of studies/work |

**Important geometry constraint:** A 2.45 m creature cannot pass a 2.05 m doorway while standing upright. Use lowered/leaning posture animation, a separate reduced-height navigation capsule (~1.7–1.9 m proposed, validated against geometry), or reserve pursuit for a taller corridor. Do **not** permit mesh clipping as 'horror distortion' during gameplay without intentional staging.

### 6.3 Face and damage — two art variants, one shared rig

- **Variant A / Early encounter:** face almost entirely obscured by hair, shadow and wet surface; faint cheek scratches; a little reddish-brown staining if lit. Use in Chapter 1.
- **Variant B / Close reveal:** profile of nose/eye socket and human cheek becomes subtly readable; **additional superficial scar marks and small dark/diluted blood smears** on face/jacket. Use only briefly in Chapter 3.
- **Hard limits:** no open deep wounds, hanging organs, exposed bones, dismemberment, heavy splatter, or graphic injury. The game is about psychological distress, not gore spectacle.
- **Technical implementation:** same topology/rig/Animator; variant through material instance + optional decal layers and calibrated lighting, **not** a duplicate enemy logic prefab.

### 6.4 Color / material brief

- Albedo charcoal near-black **but not RGB(0,0,0)**; preserve roughness and shape under low light.
- Local highlights cool blue; occasional dark rust-red blood tint only in small marks.
- Surface wetness: mixed smoothness; skin/canvas cloth should not all reflect like oil.
- Chest fissure: no emissive 'heart core' by default. Optional very subtle warm passing-headlight glint; could be achieved by lighting instead of emission.
- Paper scraps: soaked low-saturation beige / ash white; torn edges, illegible fictional assignments.
- Hair/tendrils: mesh strips/cards kept cheap and stable; avoid full expensive hair simulation.

### 6.5 Animation set — **exactly mapped to T13 (8 named clips)**

| Clip | Description | Engine use |
|---|---|---|
| `Idle_Breath` | still, shoulders move too slowly, head bowed | first appearance / idle |
| `Walk_Lurch` | slow uneven weight shift | ambient stalking |
| `Pursue` | aggressive-but-humanoid chase, no random teleporting | C1 pursuit |
| `Search_Cover` | slows and looks at cover without instant omniscience | C1 hide branch |
| `Fight_Reaction` | flinch / recoil on scripted player response | C1/C3 fight branch |
| `Hide_Pass` | passes hide zone, loses interest and exits | C1 hide resolution |
| `Approach_Reveal` | head raises, shape becomes a little more human, then quiet | C3 approach |
| `Despawn` | fades / backs into darkness without gore | transition |

**Rig:** Blender Generic rig, bone names stable, weight painting verified, no mandatory Unity Humanoid retarget. Keep **root motion OFF** for v1.1; AI/encounter motor moves the GameObject. IK is optional. Transition behavior depends on T06/T11 state logic; T13 replaces **visual only**.

### 6.6 FPP encounter staging (art + behavior responsibilities)

- **8–12 m:** silhouette readable as a person-like threat; facial resemblance **not** legible.
- **2–3 m:** brief reveal of subtle human face details; make scars visible without close-up gore.
- **C1:** a few silhouettes/door glimpses, then chase/hide/fight encounter; do not camp at the player's camera.
- **C2:** no recurring boss fight; use offscreen scrape, wet prints, shadow and ambient motif.
- **C3:** silhouette waits; central path indicates Approach, cover indicates Hide, range prompt indicates Fight. `EndingResolver` is canonical.
- **Caught:** blackout + heartbeat + checkpoint. No explicit bodily attack required.

### 6.7 Blender production and review

**Files:** `ACT_Shadow_Raka.blend`, `ACT_Shadow_Raka.fbx`, `T_...` texture set, `MAT_Shadow_Raka_A`, `MAT_Shadow_Raka_B`, `PFB_ACT_Shadow_Raka.prefab`, 8 exported animation clips.

**Checks:** clean deformation topology at joints; transforms applied; UV clean; 2K base maps initially (only if justified), LOD0 ~20–40k triangles **as starting target not guarantee**; LOD1 optional after profile; silhouette approved in fog and without fog; eight animations have no T-pose or clipping; normals and backfaces correct; reflections visible but not too glossy; prefab runs T06/T11 events unchanged.

**Reference image (LOCKED CANONICAL TURNAROUND FOR T13):**
- **`Docs/References/Raka’s Manifestation Character Turnaround.png`** (`1672×941`, 5 views: `FRONT`, `LEFT SIDE`, `BACK`, `RIGHT SIDE`, `FRONT THREE-QUARTER`) is the **user-approved locked character design ("design fix")** for Raka's manifestation (`ACT_Shadow_Raka` / `ACT_001_ShadowRakaFinal`). When executing **T13**, the agent **must** load this turnaround in Blender and rebuild the final rigged 3D character to match its proportions, blindfold + temple paper slips, wet long hair, torn student shirt with circled red `"F"` mark, X-crossed leather harness & waist belts, hip academic/overtime paper sheets, tattered pleated lower wrap, asymmetric left shin guard + right thigh strap, and elongated hands/fingers as closely as possible (full breakdown in `Docs/References/ACT_Shadow_Raka_Turnaround_Spec.md`), while preserving GDD non-gore rules and T06/T11 gameplay capsule/doorway clearance.
- `Docs/References/Untitled.png` remains available as an atmospheric corridor mood reference (`Docs/References/rain_soaked_corridor_nightmare.png` is not present; do not assume identical filenames). Neither reference image constitutes a completed monster mesh/rig until modeled and rigged in Blender during T13.

---

## 7. Asset registry — P0 / P1 / P2

**Naming:** The group IDs below **retain exactly the group IDs in GDD §7**. Suffixes identify individual deliverables; they are not replacements for `CLUE_*` runtime IDs.  
**P0** = required to finish first playable build. **P1** = visual polish/strong narrative. **P2** = optional, cut early if schedule/performance suffers.

### 7.1 Environment modules

| Asset file ID | Group | Priority | Description / base design | Scene(s) | Suggested treatment |
|---|---|---|---|---|---|
| `ENV_001_DormShell` | ENV_001 | P0 | 4×5 m kos shell; modular wall/ceiling/floor | C1/C2/Bad/Hidden | clean lightmap UV, PBR plaster |
| `ENV_001_DormDoor` | ENV_001 | P0 | hinged old wood door, ~0.9×2.05 m | C1/C2 | door collider, pivot hinge |
| `ENV_001_DormWindow` | ENV_001 | P0 | window frame and wet pane | C1/C2/ending | condensation shader/decal |
| `ENV_001_DormCornerKit` | ENV_001 | P1 | trim, skirting, utility recess | C1/C2 | modular |
| `ENV_002_CorridorStraight2m` | ENV_002 | P0 | repeatable straight segment | C1/C3 | snap grid 1 m |
| `ENV_002_CorridorStraight4m` | ENV_002 | P0 | longer corridor segment | C1/C3 | snap grid 1 m |
| `ENV_002_CorridorCorner` | ENV_002 | P0 | 90° turn/end wall variant | C1/C3 | soft corner shading |
| `ENV_002_DoorFrame` | ENV_002 | P0 | hallway door threshold | C1 | loop trigger volume |
| `ENV_002_HideNiche` | ENV_002 | P0 | occluding niche / wardrobe area | C1/C3 | collider + hide volume |
| `ENV_002_PipesCables` | ENV_002 | P1 | overhead PVC pipes, wires, leak | C1/C3 | repetition with variants |
| `ENV_003_RoadVignette` | ENV_003 | P1 | small wet roadside / barrier | memory glimpse | 4–8 m non-playable |
| `ENV_004_GraduationSet` | ENV_004 | P1 | simple stage/courtyard podium and chairs | Good | one camera composition |
| `ENV_004_Backdrop` | ENV_004 | P1 | fictional ceremony signage/background | Good | original typography |
| `ENV_004_BackgroundCrowd` | ENV_004 | P2 | 2–4 silhouette classmates/family | Good | impostor/simple rig |

### 7.2 Room furniture, academic/work and symbolic items

| Asset file ID | Group | Priority | Narrative + use | Approx. dimensions (if useful) | Interact / note |
|---|---|---|---|---|---|
| `PROP_001_Bed` | PROP_001 | P0 | makes room believable, depression montage | 0.9×2 m | focus/sleep fade |
| `PROP_001_PillowBlanket` | PROP_001 | P0 | lived-in softness, changing pose | fits bed | static/deformed mesh |
| `PROP_001_StudyDesk` | PROP_001 | P0 | study, tasks, phone, letter | ~1.2×0.6×0.75 m | collider |
| `PROP_001_Chair` | PROP_001 | P0 | signs of night work | ~0.5×0.5×0.9 m | static |
| `PROP_001_Fan` | PROP_001 | P1 | audio/poverty details | table or standing | hum/optional blade |
| `PROP_001_WaterJug` | PROP_001 | P2 | everyday prop | ~0.5 m | static |
| `PROP_002_Textbook` | PROP_002 | P0 | Chapter 1 take book | ~A5–A4 footprint | `INT_BOOK`, `objective.book` |
| `PROP_002_Pen` | PROP_002 | P0 | desk and notes story | standard pen | decorative/optional inspect |
| `PROP_002_OvertimeSlip` | PROP_002 | P0 | exhaustion context | A5 paper | `CLUE_02`, readable |
| `PROP_002_NotesStack` | PROP_002 | P0 | academic task A | 3 cards/sheets | `TASK_A_NOTES` |
| `PROP_002_InternshipForms` | PROP_002 | P0 | academic task B | 3 documents | `TASK_B_INTERNSHIP` |
| `PROP_002_ScheduleBoard` | PROP_002 | P0 | academic task C | A4 notice | `TASK_C_SCHEDULE` |
| `PROP_002_Laptop` | PROP_002 | P1 | work and deadline context | ~0.32 m wide | can be noninteractive |
| `PROP_002_Backpack` | PROP_002 | P1 | student everyday life | human-sized bag | decorative |
| `PROP_003_RakaPhone` | PROP_003 | P0 | Ella/Bayu chat & endings | ~0.15 m long | diegetic overlay |
| `PROP_004_PhotoFrame` | PROP_004 | P0 | graduation promise | small frame | `CLUE_01`, inspect reverse |
| `PROP_004_EllaHelmet` | PROP_004 | P1 | accident memory | plausible helmet | no graphic damage |
| `PROP_004_Umbrella` | PROP_004 | P1 | rain/Ella association | folded ~0.6 m | symbolic |
| `PROP_004_CrackedWatch` | PROP_004 | P1 | exhaustion and crash time | wrist-size | inspect optional |
| `PROP_004_LetterEnvelope` | PROP_004 | P1 | graduation reveal | small envelope | read only in Good |
| `PROP_004_EllaLetter` | PROP_004 | P1 | handwriting/future promise | A5 letter | readable text |
| `PROP_004_Shoes` | PROP_004 | P1 | inexpensive repaired shoes | pair | set dressing; no method hint |
| `PROP_004_RentNote` | PROP_004 | P1 | quiet SDG 1 context | A5 | fictional text |
| `PROP_004_WorkShiftCard` | PROP_004 | P1 | clock-out timeline | receipt-sized | complement overtime slip |

### 7.3 Characters, VFX, audio and UI assets

| ID | Group | Priority | Deliverable | Notes |
|---|---|---|---|---|
| `ACT_001_ShadowPlaceholder` | ACT_001 | P0 / T06 | primitive humanoid + state machine | use for prototype; later replace only the mesh/Animator |
| `ACT_001_ShadowRakaFinal` | ACT_001 | P0 / T13 | Blender rigged monster + 8 anims | §6 full spec |
| `ACT_002_GraduationFigures` | ACT_002 | P2 | silhouettes/low-cost NPC figures | optional |
| `VFX_001_Fog` | VFX_001 | P1 | controlled fog layering / Volume | performance presets |
| `VFX_001_WetDecals` | VFX_001 | P1 | puddle edges, moisture, stains | decals/vertex variation |
| `VFX_001_DrippingWater` | VFX_001 | P1 | leaks and drips from ceiling | particle/VFX |
| `VFX_001_LightFlicker` | VFX_001 | P0 | mild fluorescent instability | reduce-flashes toggle |
| `VFX_001_BlackoutFade` | VFX_001 | P0 | scene/caught transitions | never a long unskippable blind control state |
| `VFX_001_MonsterReveal` | VFX_001 | P1 | controlled silhouette/reveal | no gore |
| `AUD_PrologueRain` | AUDIO | P0 | original licensed rain ambience | loop/crossfade |
| `AUD_CrashLayer` | AUDIO | P0 | horn + brakes + collision cues | audio-only |
| `AUD_MonsterBreath` | AUDIO | P0 | movement/low rumble | spatial, not continuous shriek |
| `AUD_Phone` | AUDIO | P0 | vibration/tap/chat alert | reveal cue |
| `AUD_Graduation` | AUDIO | P1 | gentle rain/crowd ambiences | non-copyrighted music |
| `UI_CrosshairPrompt` | UI | P0 | small reticle + contextual E | minimal |
| `UI_Inspection` | UI | P0 | readable documents and subtitles | accessibility |
| `UI_PhoneChat` | UI | P0 | fictional contact list and chat | no official logos |
| `UI_Settings` | UI | P0 | volume, subtitles, sensitivity, reduce motion | persist |
| `UI_ContentNotice` | UI | P0 | proportionate content notice + help text | gentle wording |

**Texture deliverables** are named as maps of the asset or trim sheet, not independent interactables. P0 color and normal mapping should come **after** the greybox and collision pass.

---

## 8. Item bible — inspect details, clue placement and readable text

### ITEM-01 / Textbook (`PROP_002_Textbook`)

- **Location:** desk, slightly disorganized. Appears under soft practical light.
- **Gameplay:** camera raycast → `[E] Ambil Buku` → `objective.book=true` → short monologue; object can be hidden/marked used, not physically carried all the way through C1.
- **Art:** well-used fictional academic book, no real textbook cover imitation; textured pages, damaged corner, a few notes.

### ITEM-02 / Overtime slip (`PROP_002_OvertimeSlip`, `CLUE_02`)

- **Location:** desk/corner, becomes conspicuous in second loop.
- **Gameplay:** inspect both sides, collect clue only on inspection; never require grabbing a small 3D paper in dark corners without a cue.
- **Art/text:** fake shift, late clock-out, no real company names. Avoid a contradictory exact timestamp to the accident prologue.

### ITEM-03 / Graduation photo (`PROP_004_PhotoFrame`, `CLUE_01`)

- **Location:** near desk/window; backside has a small handwriting: **“Wisuda bareng, ya.”**
- **Gameplay:** examine back for clue. Show that relationship is affectionate without declaring Ella a confirmed girlfriend.
- **Visual variants:** photo angled slightly differently across C1 loops, but clue remains possible to collect.

### ITEM-04 / Phone (`PROP_003_RakaPhone`)

- **C2:** player can choose **Send** or **Lock/Draft** for Ella. If sent, show **one checkmark**; **no reply**. Save `messageEllaSent` but don't treat it as mental-health score.
- **Old conversation (`CLUE_03`):** semester-five graduation joke/time capsule reference; ambiguity preserved.
- **Bayu:** C2 reminder/check-in and ending texts, separate from Ella thread.
- **UX:** entirely fictional messenger UI. Use size-legible typography; avoid brand copyright. Check subtitles and screenreader-friendly text architecture if possible.

### ITEM-05 / Ambiguous recording (`CLUE_04`)

- **Location:** after or between C2 tasks, with clear opt-in inspect (not an autoplays-every-time jumpscare).
- **Gameplay:** audible and subtitled fragment like **“Ella... masih...”** disrupted by noise; enough to be fair but not an explicit confirmation of survival.
- **Rule:** never label the file “Ella ICU Alive” or reveal a medical diagnosis in metadata.

### ITEM-06 / Three academic tasks

- `TASK_A_NOTES`: choose correct chronological order of three notes; success/fail both progress.
- `TASK_B_INTERNSHIP`: identify the correct internship document from three; success/fail both progress.
- `TASK_C_SCHEDULE`: select a reasonable priority among conflicting deadlines/shifts; success/fail both progress.
- If **all three fail**, use a **bed / passing-time montage**, **then progress to C3**. This is a narrative state, not a humiliation punishment.

### ITEM-07 / Ella letter (`PROP_004_EllaLetter`)

- **Location:** delivered in a gift at graduation during Good Ending; originally written around semester five as a playful future message/time capsule.
- **Sample prop wording (proposed, not final script):** “Selamat wisuda, Ka. Aku tahu perjalananmu nggak gampang. Aku berharap kamu tetap jalan, walau pelan.”
- **Mystery management:** handwriting is hers, but the letter is **not** evidence that she died or sent it supernaturally. The early letter origin can remain subtly uncertain if desired.

---

## 9. Lighting, palette and atmosphere by chapter

> The colors below are **art-direction swatches**, not fixed numerical light values. The installed renderer is URP (§0.3); original HDRP goals remain a separate unresolved production decision. Validate exposure and white balance on the target build, then tune atmosphere for each space using supported features.

| Space / phase | Primary palette | Practical light | Atmosphere + material | Player focus |
|---|---|---|---|---|
| Prologue | night slate `#24303C`, wet amber `#B88B5C` | hidden vehicle glow | rain/black screen, emotional audio | voice and promise |
| C1 Denial | dirty gray `#696C66`, sickly teal `#405954` | fluorescent tube | plaster, damp stain, modest clutter | book, door, photo |
| C1 Anger | charcoal `#19191C`, muted rust `#703C3A` | flicker, distant warm leak | disturbed shadows, moving paper | silhouette/cover |
| C2 Bargaining | blue-gray `#3D4C5A`, desaturated sand `#817C70` | desk lamp, phone | paper, reflection from glass | task objects and chat |
| C2 Depression | storm blue `#253546`, near-black `#181C22` | small task lamp | loss of contrast, longer sound tails | next achievable task |
| C3 Acceptance | charcoal `#181D22`, pale warm `#BAA083` | rear spill / rim light | layered fog, wet humanoid | central approach lane |
| Good graduation | faded sunset `#BE936A`, cloud blue `#738395` | soft sky and rims | fine rain, damp ground | letter and congratulations |
| Bad/Hidden | midnight blue `#17212A`, phone blue `#9CB4BE` | phone screen + cold window | empty room and heavy rain | message content |

**Current URP setup and further lighting work**

1. Current kos art uses ACES tonemapping, mild film grain/color adjustments, scene fog and a dark room reflection probe. The circular POV is a separate UI shader (§11), not volume vignette alone.
2. Keep the warm desk/cold window contrast and limited corridor light pools. The camera-follow spotlight currently has range **10 m**, outer angle **65°**, inner angle **25°**, intensity **3** and soft shadows; these are recorded URP implementation values, not portable HDRP physical-light settings.
3. Use baked/mixed light where practical after profiling and after testing chapter-state changes. The current pass does not establish completed light baking, per-chapter volume transitions or monster lighting.
4. Wet surfaces use smoothness variation and selective reflection probes. SSR and HDRP volumetric fog are **future pipeline-dependent proposals**, not features assumed to be available or implemented in the current renderer.
5. Keep routes and future item prompts legible through the circular aperture at Low/High quality. Tune shadow distance, light counts and supported effects against actual measurements.
6. Require no ray tracing. Profile a **Windows standalone build**, not Editor only. Any HDRP migration requires a separate decision and material/lighting/performance validation; do not upgrade the pipeline as part of routine asset work.

**Lighting review shots:** camera at dorm doorway, desk at item pickup distance, 8–12 m monster silhouette, C2 phone close-up, 2–3 m monster reveal, graduation close-up letter, phone text in Bad/Hidden. Test color accessibility with grayscale preview and subtitles.

---

## 10. Audio worldbuilding and interaction feedback

| Sound layer | GDD cue reference | 2D/3D | Artistic purpose |
|---|---|---|---|
| Rainstorm / drip | `AUD_PROLOGUE_RAIN` | blend 2D wash + window 3D | memory motif |
| Crash horn + brake + impact | `AUD_CRASH` | mostly cinematic / controlled | convey event without injury image |
| Fan / fluorescent hum | C1 ambience | spatial / local | lived-in poverty, pressure |
| Wet footsteps | C1 / C3 | 3D position | anticipation and hide feedback |
| Monster breath / low rumble | `AUD_C1_SHADOW` | positional and occluded | threat cue without music overuse |
| Phone haptic/tap | `AUD_C2_PHONE`, `AUD_HIDDEN_PING` | UI nearfield | important interaction/reveal |
| Raka exertion / pause | `AUD_C2_WEIGHT`, `AUD_C3_APPROACH` | centered 2D/voice | subjective emotional pacing |
| Graduation rain and crowd | `AUD_GOOD_GRAD` | blended ambience | grounded good ending |

**Mixer groups:** Master / Voice / Ambience / SFX / Music. Voice and subtitles must remain understandable. Implement adjustable dynamic range if team capacity allows; music must be original/licensed. Keep audio references as filenames/IDs, not claims that real recordings already exist.

---

## 11. First-person UI, text and interaction standards

- **Controls:** implemented WASD move, mouse look, Shift sprint, Ctrl **toggle** crouch, E interact/inspect and Esc close-inspect-before-pause. Contextual left-click Fight remains for T06. Always check existing Input System mapping before adding bindings.
- **Camera:** eye ≈1.65 m, FOV default 75° and adjustable 65–95°; current head-bob is off. Any later motion effect should be optional.
- **Circular POV — user requirement, implemented:** the world is visible through a centered circle with dark/opaque surroundings and a soft edge. Correct for aspect ratio so the aperture stays circular. Default enabled; radius **0.50 × viewport height**, adjustable **0.45–0.80**, softness **0.11**. Radius is not a percentage of the screen width or a camera FOV change; the present UI label is “Lebar pandang 50%” at default.
- **POV comfort/UI:** pause provides the effect toggle and radius slider. Preferences `NRA.CircularViewEnabled` / `NRA.CircularViewRadius` override defaults; preserve the user's saved values. Hide the mask while paused, keep HUD/settings readable above it and do not intercept UI raycasts. Camera-follow light supports the atmosphere without replacing ambient/material detail.
- **Interaction:** raycast max 2.3 m, proper occlusion/layer mask, no interaction through walls.
- **HUD:** tiny reticle, one `[E]` label, subtitles at bottom, no health bar or numerical mental-health meter.
- **Inspect:** centered reading surface with zoom or overlay; temporarily lock locomotion; Esc returns focus. Small paper must not become unreadable because of artistic texture grunge.
- **Phone:** legible header, thread labels, message list, one-checkmark sent state, back path; no real service logo.
- **Color/comfort:** current art adds no flicker/flashes or motion blur. Implement future reduce-flashes, subtitle size and volume controls with the relevant tasks; do not claim them complete. FOV/sensitivity and circular POV preferences already persist. Avoid stacking a strong volume vignette over the circular mask.
- **Content note:** trauma, grief, serious mental-health themes and implied death. Credit/menu help text may point to local support sources, without inventing a crisis number.

---

## 12. Blender → Unity production contract

### 12.1 Coordinate, scale and mesh rules

- Author Blender in **meters**; use consistent origins and units. Confirm measured size of known test cube in Unity before batch export.
- Apply location/rotation/scale **where safe**, preserve rig skin weights on character; use a tested FBX export preset, document axes and orientation. Do **not** assume exporter default axes are universally correct.
- Modular architecture: **1 m grid**; 2 m / 4 m tile lengths. Ground origins at base/corner with documented snap points. Keep pivot on doors at hinge.
- Use separate colliders in Unity: primitive Box/Capsule when possible; avoid MeshCollider for detailed clutter.
- Wall/ceiling/floor get lightmap UVs if baked lighting chosen; trim sheet and tiling materials minimize unique textures.
- Character armature: one clear root; Generic rig; animation clips baked/exported by name; check for extra leaf bones and root drift.
- LOD strategy is profiler-driven; use lower-poly variants for repeated distance meshes only where useful. Do not force physically unrealistic polycaps on one hero prop when small scene budget permits.

### 12.2 Naming / folder layout (proposal, adapt to existing project)

```text
NoRainbowAfter/                         # Unity project root, example only
├── Assets/
│   └── _NoRainbowAfter/
│       ├── Art/
│       │   ├── SourceBlender/          # optionally keep .blend outside Unity and version separately
│       │   ├── Models/Environment/
│       │   ├── Models/Props/
│       │   ├── Models/Characters/
│       │   ├── Materials/
│       │   ├── Textures/
│       │   ├── Animations/
│       │   └── VFX/
│       ├── Audio/{Voice,Ambience,SFX,Music}/
│       ├── Prefabs/{Environment,Props,Characters,UI}/
│       ├── Scenes/{Boot,MainMenu,MasterGameplay,Credits}/
│       ├── Scripts/{Core,Player,Interaction,Narrative,AI,UI,Save,Audio}/
│       ├── ScriptableObjects/{Items,Chapters,Dialogue,Clues}/
│       ├── Tests/{EditMode,PlayMode}/
│       └── Settings/
├── Docs/
│   ├── GDD_No_Rainbow_After_v1_1.docx
│   ├── CODEX_TASKS_No_Rainbow_After_v1_1.md
│   ├── design.md
│   ├── CODEX_MASTER_PROMPT.md
│   └── References/Untitled.png          # existing bitmap; inspect before use
└── BlenderSources/                    # current tracked .blend sources, generators and manifests
```

**Do not silently relocate existing project files.** If the Unity project uses another structure, adapt with a clear mapping in the task report.

**Naming:** meshes `SM_ENV_...` / `SM_PROP_...`; characters `SK_ACT_...`; textures `T_<asset>_BaseColor|Normal|Mask|AO`; materials `MAT_...`; prefabs `PFB_...`; clips `AN_...`; audio `AUD_...`; stable interactable IDs `INT_...` and four canonical clues only `CLUE_01`–`CLUE_04`.

### 12.3 PBR & material requirements

- **Albedo/base color:** free of baked direct shadows. Realistic muted wear variation; correct color space.
- **Normal:** tangent-space and correct channel orientation after import; validate on angled light.
- **Mask/smoothness:** match the shader actually in use. Current kos **URP Lit metallic/smoothness** maps use **R = metallic, A = smoothness**; their material smoothness multiplier is **1**. This is not an HDRP mask map. A future HDRP Lit conversion requires its R metallic / G AO / B detail mask / A smoothness layout and separate import/material verification. Do not pass Blender roughness directly as smoothness; repack/invert as needed for the selected shader.
- **AO:** subtle enough not to darken readable object edges; filmic color management configured intentionally.
- **Decals:** leak/stain/wet edge additions on static modules; avoid every worn mark being unique high-poly geometry.
- **Typography:** paper, labels, and envelope should use fictional original readable text textures, not arbitrary AI-generated illegible letters.
- **Current texture fidelity:** Wall/Wood albedos are generated originals; normals and other procedural maps approximate surface detail. Inspect seams, texture scale and light response in-game; do not describe these as scanned or fully calibrated PBR materials.

### 12.4 Standard handoff per art asset

Every delivered asset should include:

1. `.blend` source (or source scene reference) and exported FBX/glTF where applicable.
2. Required textures, materials and legal attribution/licence where sourced externally.
3. Measured bounding box in meters; pivot/orientation convention.
4. Tris/vertices and expected use count; material count; map resolutions.
5. Unity prefab or import settings + collision/LOD policy.
6. Screenshot in Blender **and** Unity FPP Game View with scale reference.
7. Known limitations and whether accepted for P0/P1/P2.

---

## 13. Unity scene / prefab component contract

The tree below is the **target architecture for later tasks**. It does not assert that story systems, interactables, hiding zones or chapter roots currently exist; use §0.3 and the latest handoff for implemented state.

```text
MasterGameplay
├── Systems
│   ├── GameManager / GameState / SaveService
│   ├── ChapterDirector / NarrativeDirector / ObjectiveTracker
│   ├── ClueRegistry / EndingResolver / AudioDirector
│   └── SettingsService / SubtitlePresenter
├── Player_Raka_FPP
│   ├── CharacterController / FirstPersonMotor
│   ├── CameraRoot / FPP Camera
│   ├── PlayerInteractor / InteractionPrompt
│   └── AudioListener / optional FPP Arms
├── Root_Prologue         [inactive except current chapter]
├── Root_Chapter1
│   ├── ENV_001_DormShell
│   ├── ENV_002_CorridorRoot + LoopVariants
│   ├── InteractiveObjects
│   ├── HidingZones / ChapterTriggers / Checkpoints
│   └── ShadowEncounter (T06; final visual from T13)
├── Root_Chapter2
│   ├── StudyTask_A_B_C / PhonePoint / ClueLocations
│   └── MontageBed / ChapterTriggers / Checkpoints
├── Root_Chapter3
│   ├── ChoiceApproach / ChoiceFight / ChoiceHide
│   └── ShadowFinal / ResolveTrigger
├── Root_EndingGood / Root_EndingBad / Root_EndingHidden
└── UI_Root
    ├── Interaction / Inspect / Phone / Subtitle / Pause / Settings
    └── Fade / ContentNotice
```

**Runtime ID collision policy:** every `IInteractable` has a unique stable ID. Avoid scene name-based fragile lookups for story-critical triggers; expose serialized references and run validation in EditMode. ScriptableObjects can hold item metadata, while save flags refer to canonical IDs. Never represent the monster as a NavMeshAgent without checking animation/collision integration.

---

## 14. Design-to-task handoff — **canonical T00–T16**

| ID | Art/design output used by Codex | Review / acceptance gate |
|---|---|---|
| **T00** | Read `design.md`, inspect project, MCP capabilities, hardware/quality targets | **Read-only** audit; no invented scene or Blender connection |
| **T01** | Folder/scene scaffolding, naming conventions | Boot→MainMenu→MasterGameplay→Credits works |
| **T02** | eye height/FOV and movement feel | true FPP, CharacterController stable |
| **T03** | `INT_*` metadata and inspect legibility | no wall penetration, input prompt works |
| **T04** | WB-01/02 primitive greybox in correct scale | dorm+12–18 m hall, cover, checkpoint |
| **T05** | dorm variants, loop anomaly timeline | C1-01..C1-06 in sequence |
| **T06** | placeholder creature and occluding hide zones | Fight/Hide/Caught all pass |
| **T07** | one-room HDRP hero art + fog/material studies | clear FPP screenshot + Windows build profile |
| **T08** | phone mesh and fictional UI/chat style | Ella one-tick/draft and Bayu present; save persists |
| **T09** | desk tasks, legible papers and montage staging | TASK_A/B/C, all-fail still advances |
| **T10** | exact `CLUE_01`–`CLUE_04` prop placements | 4 unique clues, no premature spoiler |
| **T11** | Chapter 3 confrontation composition and affordances | Approach Good; Fight/Hide +4 Hidden, else Bad |
| **T12** | graduation micro-set, dark dorm, ending text visual | non-graphic 3 endings, letter and messages |
| **T13** | §6 monster `.blend`, FBX, 8 clips, two material variants | rig, scale, animation, fog FPP readability; preserve T06/T11 |
| **T14** | modular props and Blender transfer proof asset | source/export/prefab dimensions pass |
| **T15** | audio sources, subtitle styles, optional visual settings | mixer/subtitles/accessibility work |
| **T16** | cross-scene art pass and QA shots | Windows x64, QA01–QA16, performant |

**Task sequencing note:** `T06` monster is **functional placeholder**; `T13` final monster is a later visual swap. The canonical name of `T07` remains **HDRP vertical slice**; `T08` is phone. The current URP art pass does not resolve the original HDRP target or satisfy T07's standalone profiling gate. Preserve IDs and report this pipeline difference when planning T07.

**User-directed sequence exception:** after T02, the user requested an early Blender kos/corridor art pass and circular POV because the in-game greybox was too plain. That pass is implemented and locally committed, but advances only those visual requirements. Retain/reuse the new art while implementing and checking the remaining T03–T07 mechanics and gates; do not replace the presented environment with the debug lab. Temporary blockout/collider tests remain acceptable internally.

---

## 15. Scene shot list & art review (FPP)

| Shot ID | Context | What must read clearly | Failure to avoid |
|---|---|---|---|
| `SHOT_01` | doorway → dorm | bed, desk and one interactable book | flat total darkness |
| `SHOT_02` | desk closeup | overtime slip and clues | unreadable prop text |
| `SHOT_03` | corridor 12 m | depth, exit, subtle anomaly | too dense fog |
| `SHOT_04` | monster C1 8–12 m | distinct distorted humanoid silhouette | facial identity spoiled early |
| `SHOT_05` | hide niche | visual cover from creature | cover that AI 'sees through' |
| `SHOT_06` | phone | readable one-tick Ella chat | actual branded UI assets |
| `SHOT_07` | task papers | readable 3-choice puzzle | tiny font / unnecessary drag complexity |
| `SHOT_08` | C3 confrontation | obvious approach lane + hide zone | accidental trigger overlap |
| `SHOT_09` | monster reveal 2–3 m | faint human likeness + optional scar marks | cartoon boss + graphic gore |
| `SHOT_10` | graduation | Raka’s completion + warm drizzle | sudden unexplained instant recovery |
| `SHOT_11` | Ella letter | readable handwritten message | objectively confirming death |
| `SHOT_12` | dark room with phone | Bayu text readable, respectful framing | showing means/body |

For each shot, keep an **in-game screenshot** at Low and High settings, plus notes: player position, light recipe, performance, important interaction distance. A reference image alone cannot satisfy shot approval.

---

## 16. Quality budgets, priority and safe scope cuts

**Target:** Windows 1080p, ideally **60 FPS on agreed test GPU**, minimum stable ~30 FPS when fallback needed (see GDD). No performance promise without standalone profile. Avoid screen-wide high-cost volumetrics and ray tracing dependency.

| Level | Must deliver | Cut/replace if delayed |
|---|---|---|
| **P0 / MVP** | FPP, 4×5 dorm, modular corridor, interactables, looping, T06 silhouette, C2 phone/tasks, 4 clues, C3 response, 3 ending placeholders, functional UI/audio | no cinematic NPC crowd, no complex exterior |
| **P1 / vertical slice art** | high-quality hero room, realistic wet PBR materials, ambient lighting/fog, final monster model, letter, graduation micro-set, spatial sound | fewer props rather than unoptimized props |
| **P2 / stretch** | animated fan, city exterior background, full body Raka/Ella 3D renders, multiple crowd figures, secondary monster variants, extra camera effects | remove first at scope freeze |

**Working budget proposals:** 1K texture for most repeated props, 2K for near-camera hero items; reuse trim sheets; monster ~20–40k triangles LOD0 initially; keep material slot count low. Use numbers as **tests** not guarantees or rigid acceptance thresholds. Avoid static baking choices that prevent chapter-specific lighting changes.

---

## 17. Security, production and definition of done

- **Before MCP work:** Codex audits project state and tool list, reads three documents, confirms current scene, Unity version, render pipeline, console, source-control status; does **not** fabricate access.
- **Task scope:** T00–T05 have recorded handoffs; recheck the relevant baseline instead of restarting them. T05 verifies encounter contracts with test doubles and does not complete Chapter 1. Continue one bounded task after its dependencies, honoring the user's early visual-pass exception (§14). Only continue to a subsequent task when authorized.
- **Scene/asset safety:** snapshot/commit before substantial scene/prefab edits. Authorized reversible changes can proceed; preserve unrelated user assets/settings and ask before unapproved destructive removal, pipeline migration or package upgrades.
- **Build validation:** compile clean, run targeted tests, try PlayMode if connected, record actual screenshots/paths and Windows build results.
- **Art approval:** original silhouette and material, correct scale, functional collision, accessible interaction, appropriate light, performance checked; no copyright violations.
- **Sensitive content:** narrate grief respectfully and non-graphically. Avoid the misleading message that a single emotion/choice deterministically causes a real-world outcome.
- **Reporting:** exact changed files, which MCP tools were used, evidence of compilation/PlayMode tests, unfinished items, next task. Unsupported actions must be identified as blockers.
- **Local handoff:** after game-screen changes, make a scoped local commit with the related assets/source/evidence. Exclude unrelated Editor/user setting changes, leave the current Blender/Unity work visible, then respect the user's requested discussion boundary.

### 17.1 Code delegation and token budget — explicit user workflow

| Role | Work allocation | Verification / escalation |
|---|---|---|
| **GPT-6.0 Luna** (`gpt-6-luna`) | Default for small, well-specified code tasks: local UI wiring, simple utilities, limited repetitive edits and straightforward fixes using established patterns. | Give exact files, interfaces and acceptance checks. Escalate to Terra if the work requires broader architecture reasoning or unresolved cross-system debugging. |
| **GPT-6.1 Terra** (`gpt-6.1-terra`) | Complex code: gameplay state, save/clue/ending contracts, controller or shader integration, cross-scene logic, substantial refactors and difficult bugs. | Handle integration-sensitive decisions; document assumptions and meaningful validation for lead review. |
| **Lead agent / Sol** | Art direction, asset creation/selection, Blender/Unity asset integration, task scoping, code review and final visual/runtime verification. | Inspect the actual diff and resulting scene, check canon/material/scale/collision/UI behavior, run appropriate checks, and make the scoped local commit. Delegation does not replace verification. |

Choose the smallest capable code agent to reduce token usage; do not send every task to both models. Use focused context with `Docs/design.md` sections, relevant source paths, constraints and acceptance criteria rather than duplicating full chat history or the entire project. Have the agent read necessary dependencies and return a concise changed-file/validation/limitations report. Parallel code work is useful only for genuinely independent files/interfaces; serialize dependent edits. Reuse an existing suitable agent when practical, and avoid repeated tests after a passing result unless new edits or evidence justify them.

Model availability must be checked at dispatch; do not claim a model ran when unavailable. If the requested model cannot be used, report that limitation before substituting another model. The completed kos pass used Terra for code; this table governs subsequent authorized coding work and does not initiate it.

---

## 18. Quick asset prompts — short briefs for individual Blender MCP tasks

> Use these **only after T00** and after dependencies are met. Name output based on the registry above.

**Dorm/room kit (T14 or P1 art continuation):**

```text
Create only the original ENV_001_DormShell / ENV_001_DormDoor modular meshes defined
in Docs/design.md §§3 and 7. Use meters, 4×5 m shell, 2.7 m ceiling, 0.9×2.05 m
door with hinge pivot, aged plaster and inexpensive wood. Prepare correct UVs,
export with tested axis scale, then import as Unity prefabs with colliders and
materials for the installed URP pipeline. Do not migrate render pipelines.
Do not build unrelated rooms. Validate from Raka's 1.65 m FPP camera.
```

**Monster (T13):**

```text
Implement ACT_Shadow_Raka only according to Docs/design.md §6 and CODEX_TASKS T13.
Use the concept reference only if it exists in the actual workspace. Make one
Generic rig with 8 named animations, two restrained face/damage materials, wet
black PBR, paper fragments and subtle chest fissure. Keep T06/T11 AI untouched.
Report Blender source, FBX, Unity prefab, fog-distance FPP screenshots, rig
validation, and build performance. No graphic gore and no franchise copies.
```

**Phone (T08):**

```text
Implement only Raka's fictional phone presentation based on Docs/design.md §§7–8.
Ella thread: send/lock choice, one checkmark if sent, never a reply. Bayu's
thread is separate. Preserve save flags, exact clues and ending reveal wording.
No official messaging app logos. Verify font legibility at 1080p and Esc back.
```

**Graduation (T12 art pass):**

```text
Create a production-efficient sunset/drizzle graduation micro-set using the
Docs/design.md WB-07 and GDD Good Ending. Show passage of study time, graduation,
congratulations, and the handwritten early letter. Do not imply instantaneous
mental recovery; use few original props/figures and camera-focused composition.
```

---

## 19. Open decisions — defaults allow work without unnecessary questions

| Decision | Default for v1.1-compatible production | When to revisit |
|---|---|---|
| Final look of monster face | A early / B late, same rig | after FPP 2–3 m art test |
| Exact dorm dressing items | listed P0/P1 set | after hero room screenshot |
| C2 campus/work assets | micro-sets or sound-only vistas | if narrative clarity suffers |
| C3 arena dimensions | ~6×8 m room as proposal | if hide/approach paths conflict |
| Pipeline | Work in installed URP 17.6.0; original HDRP target remains unresolved, with no migration or final fallback approval inferred | separate pipeline decision informed by Windows profiling before T07 acceptance |
| Graduation population | silhouette/cropped figures | if Good Ending feels empty |
| Monster detailed mesh time | T13 after functional T06 | if team wants independent artist to work in parallel, keep interface lock |

## 20. Source and cross-reference notes

- **Primary story/GDD:** `GDD_No_Rainbow_After_v1_1.docx` (authoritative).
- **Canonical implementation IDs:** `CODEX_TASKS_No_Rainbow_After_v1_1.md` (**T00–T16**, do not renumber).
- **This file:** implementation-level **world/asset/art** choices only. Its new geometry, palette and size proposals are **not retroactive GDD requirements** until approved and measured in Unity.
- **Implementation evidence:** `T01_HANDOFF.md`, `MENU_ART_HANDOFF.md`, `T02_HANDOFF.md`, `KOS_ATMOSPHERE_HANDOFF.md` and the QA reports referenced in §0.3. Handoffs record checks at delivery time, not perpetual live Editor status.
- **Tooling references:** installed Unity MCP, URP shader/lighting settings and Blender FBX exporter; HDRP guidance applies only to a separately approved migration. Recheck versions and connection capabilities in the actual environment.

**Revision v1.1:** distinguished installed URP from the original HDRP goal; recorded the kos art/QA baseline, circular POV and Blender/visible-work requirements; clarified planned systems versus completed work; corrected reference/file casing; added Terra/Luna code allocation with focused context for token economy. Story, four clues, ending rules and T00–T16 numbering are unchanged.

**End of `design.md`**

## Authorized Chapter 1 revision — 12 November 2026

The user requested a substantially disordered room on the first return, an overtime-slip-triggered transfer to longer horror corridors without the source asset's storage/library area, E-interactive wardrobe hiding that makes the monster depart, and an escape opportunity on first capture. These explicit requests supersede the earlier fixed 14m encounter layout and crouch-only hiding assumptions for the expanded Chapter 1. The route is a connected authored return loop rather than procedural generation. First capture permits escape; a subsequent capture restores the checkpoint while preserving clues.

The user explicitly selected the supplied Horror Corridor 3 archive for private coursework after its unresolved source provenance was explained. See [asset attribution and provenance](ASSET_LICENSES.md) and [implementation/handoff](CHAPTER1_EXPANSION_HANDOFF.md). No final Bad/Secret Ending revision is locked: preserve the recorded Hide response as an integration hook while the script is in progress.
