"""Add looping bone-driven hair sway to the existing Raka Blender model.

Run after build_raka_turnaround.py with Blender --background --python.
The character's rest geometry, UVs, material nodes and object transforms are
preserved and verified. This is authored secondary motion for Blender playback.
"""
from pathlib import Path
import hashlib
import json
import math

import bpy
import numpy as np
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
MODEL = ROOT / "BlenderSources/Characters/RakaTurnaround/Raka_Manifestation.blend"
QA = ROOT / "Docs/QA/RakaTurnaround"
UNIT = 2.34 / 845
PREFIX = "HairSway"
PERIOD = 96


def geometry_digest():
    """Include mesh, UV, colour and transform data, including non-hair objects."""
    digest = hashlib.sha256()
    for obj in sorted(bpy.context.scene.objects, key=lambda item: item.name):
        if obj.type != "MESH":
            continue
        digest.update(obj.name.encode())
        digest.update(np.asarray(obj.matrix_world, dtype=np.float64).tobytes())
        mesh = obj.data
        for items, field, width, dtype in [
            (mesh.vertices, "co", 3, np.float32),
            (mesh.loops, "vertex_index", 1, np.int32),
            (mesh.polygons, "loop_start", 1, np.int32),
            (mesh.polygons, "loop_total", 1, np.int32),
            (mesh.polygons, "material_index", 1, np.int32),
        ]:
            values = np.empty(len(items) * width, dtype=dtype)
            items.foreach_get(field, values)
            digest.update(values.tobytes())
        for uv in mesh.uv_layers:
            values = np.empty(len(mesh.loops) * 2, dtype=np.float32)
            uv.data.foreach_get("uv", values)
            digest.update(uv.name.encode())
            digest.update(values.tobytes())
        for attr in mesh.color_attributes:
            values = np.empty(len(attr.data) * 4, dtype=np.float32)
            attr.data.foreach_get("color", values)
            digest.update(values.tobytes())
        digest.update(str([material.name for material in mesh.materials]).encode())
    return digest.hexdigest()


def evaluated_positions(objects):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    positions = {}
    for obj in objects:
        mesh = obj.evaluated_get(depsgraph).data
        values = np.empty(len(mesh.vertices) * 3, dtype=np.float32)
        mesh.vertices.foreach_get("co", values)
        positions[obj.name] = values.reshape(-1, 3)
    return positions


def point(x, depth, row):
    return ((x - 230) * UNIT, (depth - 547) * UNIT, (865 - row) * UNIT)


def add_hair_sway():
    scene = bpy.context.scene
    rig = bpy.data.objects["ACT_Shadow_Raka"]
    if scene.get("raka_hair_sway_version"):
        raise RuntimeError("Hair sway already exists. Rebuild the base model before reapplying.")
    hair_objects = [obj for obj in rig.children if obj.name.startswith("Hair |")]
    assert len(hair_objects) == 3
    scene.frame_set(1)
    original_digest = geometry_digest()
    rest_before = evaluated_positions(hair_objects)
    old_bones = {
        bone.name: np.asarray(bone.matrix_local).copy()
        for bone in rig.data.bones
    }
    old_actions = {action.name: len(action.slots) for action in bpy.data.actions}
    rig.hide_set(False)
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    chains = {
        "Left": [(178, 520, 94), (173, 540, 155), (170, 560, 230), (167, 580, 350)],
        "Right": [(259, 520, 94), (272, 540, 155), (284, 560, 230), (292, 580, 350)],
        "Back": [(220, 543, 94), (226, 590, 155), (229, 605, 230), (230, 603, 350)],
    }
    for label, points in chains.items():
        parent = rig.data.edit_bones["Head"]
        for index, (start, end) in enumerate(zip(points, points[1:]), 1):
            bone = rig.data.edit_bones.new(f"{PREFIX}.{label}.{index}")
            bone.head = point(*start)
            bone.tail = point(*end)
            bone.parent = parent
            bone.use_connect = index > 1
            parent = bone
    bpy.ops.object.mode_set(mode="OBJECT")
    hair_bones = [bone for bone in rig.pose.bones if bone.name.startswith(PREFIX)]
    for bone in hair_bones:
        bone.rotation_mode = "XYZ"
    # Blend continuously from fixed crown to root, middle and tip of the locks.
    for obj in hair_objects:
        obj.vertex_groups.clear()
        xyz = np.array([vertex.co[:] for vertex in obj.data.vertices])
        rows = 865 - xyz[:, 2] / UNIT
        x = xyz[:, 0] / UNIT + 230
        depth = xyz[:, 1] / UNIT + 547
        transitions = [np.clip((rows - lo) / (hi - lo), 0, 1)
                       for lo, hi in [(90, 126), (137, 180), (211, 259)]]
        blends = [1 - transitions[0], transitions[0] - transitions[1],
                  transitions[1] - transitions[2], transitions[2]]
        back = np.clip((depth - 542) / 50, 0, 1)
        right = np.clip((x - 208) / 30, 0, 1)
        lateral = {"Left": (1 - back) * (1 - right),
                   "Right": (1 - back) * right, "Back": back}
        groups = {"Head": blends[0]}
        for label, influence in lateral.items():
            for index in range(1, 4):
                groups[f"{PREFIX}.{label}.{index}"] = influence * blends[index]
        for name, values in groups.items():
            group = obj.vertex_groups.new(name=name)
            for idx in np.flatnonzero(values > 0):
                group.add([int(idx)], float(values[idx]), "REPLACE")
        assert all(abs(sum(group.weight for group in vertex.groups) - 1) < 1e-5
                   for vertex in obj.data.vertices)
    animation = rig.animation_data_create()
    action = bpy.data.actions.new("Hair_Sway")
    animation.action = action
    # Four-second damped-looking loop. Each chain and segment has a different
    # phase, with a larger excursion at the ends. Frame 1 is the original pose.
    for frame in range(1, PERIOD + 2, 4):
        t = math.tau * (frame - 1) / PERIOD
        for label_index, label in enumerate(chains):
            for segment in range(1, 4):
                bone = rig.pose.bones[f"{PREFIX}.{label}.{segment}"]
                phase = label_index * .43 + (segment - 1) * .38
                amplitude = .0374 + segment * .0204
                sway = amplitude * (math.sin(t - phase) - math.sin(-phase))
                drift = amplitude * .36 * (math.sin(2 * t - phase) - math.sin(-phase))
                bone.rotation_euler = (drift, .16 * sway, sway)
                bone.keyframe_insert(data_path="rotation_euler", frame=frame, group=bone.name)
    slot = animation.action_slot
    animation.action = None
    track = animation.nla_tracks.new()
    track.name = "Hair sway | loop"
    strip = track.strips.new("Hair_Sway", 1, action)
    strip.action_slot = slot
    strip.repeat = 5
    strip.extrapolation = "NOTHING"
    scene.frame_start = 1
    scene.frame_end = PERIOD
    scene.render.fps = 24
    scene.frame_set(1)
    bpy.context.view_layer.update()
    rest_after = evaluated_positions(hair_objects)
    rest_delta = max(float(np.max(np.abs(rest_after[name] - values)))
                     for name, values in rest_before.items())
    assert rest_delta < 1e-6, rest_delta
    assert geometry_digest() == original_digest
    for name, matrix in old_bones.items():
        assert np.array_equal(matrix, np.asarray(rig.data.bones[name].matrix_local))
    assert all(len(bpy.data.actions[name].slots) == slots for name, slots in old_actions.items())
    scene.frame_set(25)
    moving = evaluated_positions(hair_objects)
    movement = {name: float(np.linalg.norm(moving[name] - positions, axis=1).max())
                for name, positions in rest_before.items()}
    assert all(value > .003 for value in movement.values()), movement
    scene.frame_set(PERIOD + 1)
    end = evaluated_positions(hair_objects)
    loop_delta = max(float(np.abs(end[name] - positions).max())
                     for name, positions in rest_before.items())
    assert loop_delta < 1e-6, loop_delta
    scene.frame_set(1)
    rig.select_set(False)
    # Keep the driving armature enabled for interactive viewport playback.
    # Viewport overlays already hide bone outlines from the character preview.
    rig.hide_set(False)
    scene["raka_hair_sway_version"] = 1
    scene["Hair playback"] = "Space to play frames 1-96. Hair sway | loop is enabled in NLA."
    scene["Hair motion"] = "Authored bone animation with 3 chains. Runtime physics requires game integration."
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(MODEL), compress=True)
    result = {
        "rest_geometry_sha256": original_digest,
        "rest_pose_max_delta_m": rest_delta,
        "loop_seam_max_delta_m": loop_delta,
        "hair_displacement_frame25_m": movement,
        "hair_bones_added": len(hair_bones),
        "existing_actions_preserved": list(old_actions),
        "period_frames": PERIOD,
        "fps": 24,
        "method": "Authored looping bone animation",
    }
    (QA / "HairSwayValidation.json").write_text(json.dumps(result, indent=2))
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    bpy.ops.wm.open_mainfile(filepath=str(MODEL))
    add_hair_sway()
