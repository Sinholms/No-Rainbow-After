"""Custom student laptop asset (PROP_RakaLaptop) for No Rainbow After.
Measured against Blender MCP reference LapTop_Cube.002 (0.34m x 0.24m x 0.23m),
authored clean for URP with worn graphite body, raised keycaps, student sticker,
and an emissive Monkeytype/assignment screen texture facing -Y in Blender (+Z in Unity).
"""
import math
from pathlib import Path
import numpy as np
import bpy
from mathutils import Euler

ROOT = Path(r"c:\Users\User\Documents\Nono\No-Rainbow-After")
EXPORT_FBX = ROOT / "Assets/_NoRainbowAfter/Art/Models/Props/PROP_RakaLaptop.fbx"
BLEND_PATH = ROOT / "BlenderSources/Environment/PROP_RakaLaptop.blend"
SCREEN_TEX = ROOT / "Assets/_NoRainbowAfter/Art/Textures/Props/T_RakaLaptop_Screen.png"


def make_material(name, color, roughness=0.45, metallic=0.1, emission=None):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    out = nodes.new("ShaderNodeOutputMaterial")
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    if emission is not None:
        bsdf.inputs["Emission Color"].default_value = (*emission, 1.0)
        bsdf.inputs["Emission Strength"].default_value = 1.6
    mat.diffuse_color = (*color, 1.0)
    return mat


def make_screen_texture():
    SCREEN_TEX.parent.mkdir(parents=True, exist_ok=True)
    w, h = 512, 320
    img = np.zeros((h, w, 4), dtype=np.float32)
    img[:, :, :3] = np.array([0.075, 0.095, 0.115], dtype=np.float32)
    img[:, :, 3] = 1.0
    # Subtle window border and header bar
    img[h - 32 : h - 4, 16 : w - 16, :3] = np.array([0.13, 0.16, 0.19])
    # Red stress header pill + amber progress bar
    img[h - 24 : h - 12, 28:116, :3] = np.array([0.78, 0.18, 0.18])
    img[h - 24 : h - 12, w - 160 : w - 28, :3] = np.array([0.92, 0.68, 0.24])
    # Simulated Monkeytype text rows in the center of the laptop screen
    rows = [195, 168, 141, 114]
    rng = np.random.default_rng(7)
    for row_idx, y in enumerate(rows):
        x = 54
        while x < w - 70:
            word_w = int(rng.integers(18, 56))
            if x + word_w >= w - 54:
                break
            if row_idx == 0:
                col = np.array([0.85, 0.90, 0.92])
            elif row_idx == 1 and x < 210:
                col = np.array([0.90, 0.28, 0.26]) if x > 140 else np.array([0.85, 0.90, 0.92])
            else:
                col = np.array([0.36, 0.43, 0.47])
            img[y : y + 10, x : x + word_w, :3] = col
            x += word_w + 10
    # Bottom hold-enter hint bar
    img[26:40, 140:372, :3] = np.array([0.22, 0.27, 0.31])
    b_img = bpy.data.images.get("T_RakaLaptop_Screen") or bpy.data.images.new(
        "T_RakaLaptop_Screen", width=w, height=h, alpha=True
    )
    b_img.colorspace_settings.name = "sRGB"
    b_img.pixels.foreach_set(np.clip(img, 0.0, 1.0).ravel())
    b_img.filepath_raw = str(SCREEN_TEX)
    b_img.file_format = "PNG"
    b_img.save()
    return b_img


def add_box(col, name, location, size, mat, bevel=0.0, rot_euler=(0.0, 0.0, 0.0)):
    hx, hy, hz = size[0] * 0.5, size[1] * 0.5, size[2] * 0.5
    verts = [
        (-hx, -hy, -hz), (hx, -hy, -hz), (hx, hy, -hz), (-hx, hy, -hz),
        (-hx, -hy, hz), (hx, -hy, hz), (hx, hy, hz), (-hx, hy, hz),
    ]
    faces = [
        (0, 3, 2, 1), (4, 5, 6, 7),
        (0, 1, 5, 4), (1, 2, 6, 5),
        (2, 3, 7, 6), (3, 0, 4, 7),
    ]
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    uv = mesh.uv_layers.new(name="UVMap")
    for poly in mesh.polygons:
        n = poly.normal
        axes = (0, 1) if abs(n.z) > 0.5 else (1, 2) if abs(n.x) > 0.5 else (0, 2)
        for li in poly.loop_indices:
            co = mesh.vertices[mesh.loops[li].vertex_index].co
            u = (co[axes[0]] / max(1e-4, size[axes[0]])) + 0.5
            v = (co[axes[1]] / max(1e-4, size[axes[1]])) + 0.5
            uv.data[li].uv = (u, v)
    obj = bpy.data.objects.new(name, mesh)
    col.objects.link(obj)
    obj.data.materials.append(mat)
    obj.location = location
    obj.rotation_euler = Euler(rot_euler, "XYZ")
    if bevel > 0.0:
        mod = obj.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 2
    return obj


def build_laptop():
    EXPORT_FBX.parent.mkdir(parents=True, exist_ok=True)
    BLEND_PATH.parent.mkdir(parents=True, exist_ok=True)

    # Clear temporary objects so we export only PROP_RakaLaptop
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for col in list(bpy.data.collections):
        bpy.data.collections.remove(col)

    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0

    col = bpy.data.collections.new("PROP_RakaLaptop_Col")
    scene.collection.children.link(col)

    mat_chassis = make_material("MAT_Laptop_Chassis", (0.14, 0.15, 0.17), roughness=0.42, metallic=0.35)
    mat_dark = make_material("MAT_Laptop_DarkTrim", (0.04, 0.045, 0.05), roughness=0.65, metallic=0.1)
    mat_keys = make_material("MAT_Laptop_Keys", (0.055, 0.06, 0.07), roughness=0.52, metallic=0.05)
    mat_sticker = make_material("MAT_Laptop_Sticker", (0.34, 0.52, 0.49), roughness=0.72, metallic=0.0)
    mat_accent_key = make_material("MAT_Laptop_AccentKey", (0.18, 0.24, 0.27), roughness=0.48, metallic=0.05)

    screen_img = make_screen_texture()
    mat_screen = make_material("MAT_Laptop_Screen", (0.10, 0.13, 0.16), roughness=0.18, metallic=0.0, emission=(0.18, 0.26, 0.30))
    nt = mat_screen.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    tex_node = nt.nodes.new("ShaderNodeTexImage")
    tex_node.image = screen_img
    nt.links.new(tex_node.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(tex_node.outputs["Color"], bsdf.inputs["Emission Color"])

    root = bpy.data.objects.new("PROP_RakaLaptop", None)
    col.objects.link(root)

    # 1. Main chassis base sitting flush at z = 0..0.015 (33 cm wide x 22.5 cm deep)
    feet_h = 0.002
    base_h = 0.013
    for sx in (-0.135, 0.135):
        for sy in (-0.088, 0.088):
            f = add_box(col, f"SM_PROP_LaptopFoot", (sx, sy, feet_h * 0.5), (0.022, 0.014, feet_h), mat_dark)
            f.parent = root

    base = add_box(col, "SM_PROP_LaptopBase", (0.0, 0.0, feet_h + base_h * 0.5), (0.330, 0.224, base_h), mat_chassis, bevel=0.0025)
    base.parent = root

    # Subtle trackpad plate on palmrest (-Y is front facing user)
    trackpad = add_box(col, "SM_PROP_LaptopTrackpad", (0.0, -0.062, feet_h + base_h + 0.0004), (0.098, 0.064, 0.001), mat_dark, bevel=0.0005)
    trackpad.parent = root

    # Worn student sticker on right palmrest
    sticker = add_box(col, "SM_PROP_LaptopSticker", (0.112, -0.072, feet_h + base_h + 0.0004), (0.042, 0.026, 0.0008), mat_sticker, bevel=0.0002, rot_euler=(0.0, 0.0, -0.14))
    sticker.parent = root

    # Recessed dark tray under keyboard
    kb_z = feet_h + base_h + 0.0002
    well = add_box(col, "SM_PROP_KeyboardWell", (0.0, 0.028, kb_z), (0.282, 0.104, 0.001), mat_dark)
    well.parent = root

    # Individual sculpted keycaps (5 rows)
    row_ys = [0.070, 0.052, 0.034, 0.016, -0.002]
    for r_idx, y_pos in enumerate(row_ys):
        if r_idx < 4:
            cols = 14 if r_idx == 0 else 13
            key_w = (0.270 / cols) - 0.0025
            start_x = -0.135 + key_w * 0.5 + 0.001
            for c_idx in range(cols):
                is_enter = (r_idx == 2 and c_idx == cols - 1)
                kw = key_w * 1.45 if is_enter else key_w
                kx = start_x + c_idx * ((0.270 - key_w * 0.2) / cols)
                k = add_box(col, "SM_PROP_LaptopKey", (kx, y_pos, kb_z + 0.0016), (kw, 0.014, 0.0024), mat_accent_key if is_enter else mat_keys, bevel=0.0004)
                k.parent = root
        else:
            # Bottom row with wide spacebar
            layout = [(-0.118, 0.026), (-0.088, 0.024), (-0.005, 0.108), (0.076, 0.024), (0.112, 0.032)]
            for kx, kw in layout:
                k = add_box(col, "SM_PROP_LaptopKey", (kx, y_pos, kb_z + 0.0016), (kw, 0.014, 0.0024), mat_keys, bevel=0.0004)
                k.parent = root

    # Hinge cylinder/block at Y = +0.106
    hinge = add_box(col, "SM_PROP_LaptopHinge", (0.0, 0.106, feet_h + base_h - 0.001), (0.230, 0.012, 0.010), mat_dark, bevel=0.002)
    hinge.parent = root

    # 2. Lid assembly pivoted at hinge (0, 0.106, feet_h + base_h), tilted open 108 deg (-18 deg from vertical towards +Y)
    lid_pivot = bpy.data.objects.new("LaptopLidPivot", None)
    col.objects.link(lid_pivot)
    lid_pivot.parent = root
    lid_pivot.location = (0.0, 0.106, feet_h + base_h)
    lid_pivot.rotation_euler = Euler((math.radians(-18.0), 0.0, 0.0), "XYZ")

    lid_h = 0.214
    lid_w = 0.330
    lid = add_box(col, "SM_PROP_LaptopLid", (0.0, 0.0, lid_h * 0.5), (lid_w, 0.007, lid_h), mat_chassis, bevel=0.002)
    lid.parent = lid_pivot

    bezel = add_box(col, "SM_PROP_LaptopBezel", (0.0, -0.0032, lid_h * 0.5), (lid_w - 0.006, 0.002, lid_h - 0.006), mat_dark)
    bezel.parent = lid_pivot

    screen = add_box(col, "SM_PROP_LaptopScreen", (0.0, -0.0042, lid_h * 0.5 + 0.002), (0.304, 0.0012, 0.184), mat_screen)
    screen.parent = lid_pivot

    # Apply bevel modifiers and join static base parts vs lid/screen cleanly
    bpy.ops.object.select_all(action="DESELECT")
    for obj in list(col.objects):
        if obj.type == "MESH" and len(obj.modifiers) > 0:
            bpy.context.view_layer.objects.active = obj
            obj.select_set(True)
            bpy.ops.object.convert(target="MESH")
            obj.select_set(False)

    # Save blend and export FBX (forward -Y in Blender -> +Z in Unity)
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH), compress=True)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in col.objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root

    bpy.ops.export_scene.fbx(
        filepath=str(EXPORT_FBX),
        use_selection=True,
        object_types={"EMPTY", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=False,
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
    )
    print(f"EXPORTED_LAPTOP: {EXPORT_FBX}")


if __name__ == "__main__":
    build_laptop()
