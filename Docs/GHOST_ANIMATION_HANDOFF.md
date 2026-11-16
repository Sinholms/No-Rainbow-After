# Chapter 1 visible Raka animation repair — 2026-11-17

The encounter now animates the visible Raka skeleton. The old motion clone embedded in `PFB_ACT_001_ShadowPlaceholder.prefab` was removed. Runtime initialization no longer creates or serializes rigs while the prefab installer runs in Edit Mode.

`ShadowMotionPlayback` selects Raka's existing Idle_Breath, Walk_Lurch, Pursue, Search_Cover, Hide_Pass and Fight_Reaction clips from its controller. Sampling follows the encounter clock, with a 0.12 second blend between looping poses. The automatic Animator is disabled on this encounter instance to avoid competing pose updates. The standalone hero asset and its Animator Controller remain available for other uses. The old motion prefab remains a fallback for scenes that still use the original placeholder.

Raka has no authored grab clip. The encounter samples its idle pose and solves both arms toward shoulder-height contact points during the 1.15 second struggle, with a smooth reach/release envelope. The three-second first-escape window now plays a recovery reaction rather than freezing the mesh; movement resumes afterwards. Hit reactions affect the visible spine, with a stronger final reaction. Animation leaves the world position/collider under AI control. Posture adjustments are additive to the sampled spine pose.

Validation: Unity 6000.6.8f1 PlayMode, 12/12 ChapterOneExpansionTests passed, 0 skipped, 51.05 seconds; no Console errors after the run. Tests now check the visible hero rig, absence of the duplicate playback rig, forward hand reach below the head, pose reset and planted world root, hit reaction, paused hand pose, animated recovery, actual resumed pursuit, and a subsequent automatic catch/checkpoint restore. Clue storage is isolated for the fixture. The two pre-existing local ProjectSettings modifications were preserved byte-for-byte.

- Full test report: `QA/GhostAnimation_Tests.json`.
- Actual Unity camera captures: `QA/GhostAnimation_Grab.png`, `QA/GhostAnimation_Hit.png`.
- Raw captured sequences remain locally in the ignored `Captures/GhostAnimation/` folder.

This pass changes animation integration, not the authored Raka model or its visual design. The grab is procedural on the hero skeleton rather than a new Blender-authored animation asset.
