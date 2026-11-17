"""Bake the approved Raka turnaround materials and export its weighted Unity FBX.

The Blender review source stays editable. Export meshes are combined per material,
with their existing rest coordinates and skin weights preserved.
"""
from pathlib import Path
import json
import sys

import bpy
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).parent))
from animate_raka_hair import geometry_digest

SOURCE = ROOT / "BlenderSources/Characters/RakaTurnaround/Raka_Manifestation.blend"
TEXTURES = ROOT / "Assets/_NoRainbowAfter/Art/Textures/Raka"
FBX = ROOT / "Assets/_NoRainbowAfter/Art/Models/Characters/ACT_Shadow_Raka.fbx"
QA = ROOT / "Docs/QA/RakaTurnaround"
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene = bpy.context.scene
rig = bpy.data.objects["ACT_Shadow_Raka"]
assert scene.get("raka_hair_sway_version") == 1
expected = json.loads((QA / "HairSwayValidation.json").read_text())["rest_geometry_sha256"]
assert geometry_digest() == expected
scene.frame_set(1)
rig.hide_set(False)
for track in rig.animation_data.nla_tracks:
    track.mute = True
for bone in rig.pose.bones:
    bone.matrix_basis.identity()
meshes = [obj for obj in rig.children if obj.type == "MESH"]
materials = sorted({obj.data.materials[0] for obj in meshes}, key=lambda mat: mat.name)
names = ["Skin", "Linen", "Leather", "Paper", "Hair", "Strands", "Lining", "Fingers"]
assert len(materials) == len(names), [mat.name for mat in materials]
scene.render.engine = "CYCLES"
scene.cycles.samples = 2
scene.render.bake.margin = 8
scene.render.bake.use_clear = True
outputs = []
for material, label in zip(materials, names):
    # The sorted numbered source materials have a stable export mapping.
    selected = [obj for obj in rig.children if obj.type == "MESH" and obj.data.materials[0] == material]
    bpy.ops.object.select_all(action="DESELECT")
    for obj in selected:
        obj.hide_set(False)
        obj.select_set(True)
    bpy.context.view_layer.objects.active = selected[0]
    bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = "Raka_" + label
    obj.data.name = obj.name
    nodes, links = material.node_tree.nodes, material.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    output = nodes.get("Material Output")
    original_surface = output.inputs["Surface"].links[0].from_socket
    # Baking emission transfers the reference colours without baking studio light.
    emission = nodes.new("ShaderNodeEmission")
    color = bsdf.inputs["Base Color"]
    if color.is_linked:
        links.new(color.links[0].from_socket, emission.inputs["Color"])
    else:
        emission.inputs["Color"].default_value = color.default_value
    bake_uv = obj.data.uv_layers.new(name="UnityBake")
    obj.data.uv_layers.active = bake_uv
    bake_uv.active_render = True
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=1.15, island_margin=.012)
    bpy.ops.object.mode_set(mode="OBJECT")
    export_name = "Raka_Reference_" + label
    resolution = 2048 if label in {"Skin", "Linen", "Hair", "Lining"} else 1024
    maps = {}
    for kind in ("BaseColor", "Normal", "Roughness"):
        image = bpy.data.images.new(export_name + "_" + kind, width=resolution, height=resolution, alpha=False)
        image.colorspace_settings.name = "sRGB" if kind == "BaseColor" else "Non-Color"
        target = nodes.new("ShaderNodeTexImage")
        target.image = image
        nodes.active = target
        if kind == "BaseColor":
            links.new(emission.outputs[0], output.inputs["Surface"])
            bpy.ops.object.bake(type="EMIT")
        else:
            links.new(original_surface, output.inputs["Surface"])
            bpy.ops.object.bake(type="NORMAL" if kind == "Normal" else "ROUGHNESS")
        image.filepath_raw = str(TEXTURES / (export_name + "_" + kind + ".png"))
        image.file_format = "PNG"
        image.save()
        maps[kind] = image
    # FBX exports one standard texture UV. Preserve the baked geometry/normals.
    for layer in list(obj.data.uv_layers):
        if layer.name != "UnityBake":
            obj.data.uv_layers.remove(layer)
    obj.data.uv_layers.active_index = 0
    material.name = export_name
    nodes.clear()
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    out = nodes.new("ShaderNodeOutputMaterial")
    links.new(bsdf.outputs[0], out.inputs[0])
    for kind, image in maps.items():
        tex = nodes.new("ShaderNodeTexImage")
        tex.image = image
        if kind == "BaseColor":
            links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
        elif kind == "Roughness":
            links.new(tex.outputs["Color"], bsdf.inputs["Roughness"])
        else:
            normal = nodes.new("ShaderNodeNormalMap")
            links.new(tex.outputs["Color"], normal.inputs["Color"])
            links.new(normal.outputs[0], bsdf.inputs["Normal"])
    outputs.append(obj)
    print("BAKED", export_name, len(obj.data.vertices), flush=True)

bpy.ops.object.select_all(action="DESELECT")
rig.select_set(True)
for obj in outputs:
    obj.select_set(True)
bpy.context.view_layer.objects.active = rig
# All actions are exported independently. NLA hair motion must not be added to
# body takes, because Unity plays the hair-only clip after sampling the body.
bpy.ops.export_scene.fbx(
    filepath=str(FBX), use_selection=True, object_types={"ARMATURE", "MESH"},
    axis_forward="-Z", axis_up="Y", add_leaf_bones=False,
    bake_anim=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=True,
    bake_anim_force_startend_keying=True, bake_anim_step=1, bake_anim_simplify_factor=0,
    use_mesh_modifiers=True, mesh_smooth_type="FACE")
(QA / "UnityExport.json").write_text(json.dumps({
    "source": str(SOURCE.relative_to(ROOT)),
    "source_rest_geometry_sha256": expected,
    "meshes": len(outputs), "bones": len(rig.data.bones),
    "triangles": sum(sum(len(face.vertices)-2 for face in obj.data.polygons) for obj in outputs),
    "materials": [obj.data.materials[0].name for obj in outputs],
    "actions": [action.name for action in bpy.data.actions],
    "unweighted_vertices": sum(not vertex.groups for obj in outputs for vertex in obj.data.vertices),
}, indent=2))
print("RAKA_UNITY_EXPORT_COMPLETE", flush=True)
