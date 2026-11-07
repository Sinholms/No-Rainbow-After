"""Original T06 placeholder silhouette for ACT_001_ShadowPlaceholder (No Rainbow After).
Authors a grounded, rain-soaked, hunched humanoid shadow silhouette (~2.28 m tall, ~0.52 m wide)
with obscured wrapped head, wet hair strips, connected asymmetric shoulders, elongated limbs,
tattered student cloth, subtle chest indentation, and soaked academic paper scraps.
Note: T06 is a functional 3D placeholder; final deformable topology, Generic rig, Variant A/B 2K PBR,
and 8 named animation clips remain T13.
"""
import json
import math
from pathlib import Path
import bpy
import bmesh
from mathutils import Vector, Euler

ROOT = Path("D:/GRAVKOM/No rainbow after")
SCENE_NAME = "NRA_T06_ShadowPlaceholder_v1"
BLEND_PATH = ROOT / "BlenderSources/Characters/ACT_001_ShadowPlaceholder_v1.blend"
FBX_PATH = ROOT / "Assets/_NoRainbowAfter/Art/Models/Characters/ACT_001_ShadowPlaceholder_v1.fbx"
MANIFEST_PATH = ROOT / "BlenderSources/Characters/ACT_001_ShadowPlaceholder_v1_manifest.json"
PREVIEW_PATH = ROOT / "Docs/QA/T06_Blender_Placeholder.png"


def make_material(name, base_rgba, metallic, roughness):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = base_rgba
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    out = nodes.new("ShaderNodeOutputMaterial")
    bsdf.inputs["Base Color"].default_value = base_rgba
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return mat


def add_lofted_tube(bm, rings, sides=10, cap_start=True, cap_end=True):
    """Build a lofted tube through (center_vec, rx, ry, twist_rad, tilt_euler) rings."""
    ring_verts = []
    for center, rx, ry, twist, tilt in rings:
        rot = Euler(tilt, "XYZ").to_matrix()
        verts = []
        for i in range(sides):
            ang = (2.0 * math.pi * i / sides) + twist
            local = Vector((math.cos(ang) * rx, math.sin(ang) * ry, 0.0))
            v = bm.verts.new(center + rot @ local)
            verts.append(v)
        ring_verts.append(verts)

    for r in range(len(ring_verts) - 1):
        a = ring_verts[r]
        b = ring_verts[r + 1]
        for i in range(sides):
            j = (i + 1) % sides
            bm.faces.new((a[i], a[j], b[j], b[i]))

    if cap_start and ring_verts:
        c0 = sum((v.co for v in ring_verts[0]), Vector()) / sides
        vc = bm.verts.new(c0)
        r0 = ring_verts[0]
        for i in range(sides):
            j = (i + 1) % sides
            bm.faces.new((vc, r0[j], r0[i]))

    if cap_end and ring_verts:
        c1 = sum((v.co for v in ring_verts[-1]), Vector()) / sides
        vc = bm.verts.new(c1)
        rn = ring_verts[-1]
        for i in range(sides):
            j = (i + 1) % sides
            bm.faces.new((vc, rn[i], rn[j]))


def add_curved_strip(bm, points, widths, normal_hint=Vector((0.0, -1.0, 0.0)), thickness=0.005, skew=0.0):
    """Build a thin solidified ribbon/cloth/paper strip with optional torn-edge skew."""
    front_l, front_r, back_l, back_r = [], [], [], []
    n_pts = len(points)
    for idx, (pt, w) in enumerate(zip(points, widths)):
        if idx == 0:
            tangent = (points[1] - points[0]).normalized()
        elif idx == n_pts - 1:
            tangent = (points[-1] - points[-2]).normalized()
        else:
            tangent = (points[idx + 1] - points[idx - 1]).normalized()
        side = tangent.cross(normal_hint)
        if side.length < 1e-4:
            side = tangent.cross(Vector((1.0, 0.0, 0.0)))
        side.normalize()
        nrm = side.cross(tangent).normalized() * (thickness * 0.5)
        half = side * (w * 0.5)
        z_skew = Vector((0.0, 0.0, skew * (idx / max(1, n_pts - 1) - 0.5)))
        front_l.append(bm.verts.new(pt - half - z_skew + nrm))
        front_r.append(bm.verts.new(pt + half + z_skew + nrm))
        back_l.append(bm.verts.new(pt - half - z_skew - nrm))
        back_r.append(bm.verts.new(pt + half + z_skew - nrm))

    for i in range(n_pts - 1):
        bm.faces.new((front_l[i], front_r[i], front_r[i + 1], front_l[i + 1]))
        bm.faces.new((back_r[i], back_l[i], back_l[i + 1], back_r[i + 1]))
        bm.faces.new((back_l[i], front_l[i], front_l[i + 1], back_l[i + 1]))
        bm.faces.new((front_r[i], back_r[i], back_r[i + 1], front_r[i + 1]))
    bm.faces.new((back_l[0], back_r[0], front_r[0], front_l[0]))
    bm.faces.new((front_l[-1], front_r[-1], back_r[-1], back_l[-1]))


def finish_mesh_object(collection, name, bm, mat, parent=None, origin_world=Vector((0.0, 0.0, 0.0)), smooth=True):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    if origin_world.length > 0.0:
        bmesh.ops.translate(bm, vec=-origin_world, verts=bm.verts)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    for poly in mesh.polygons:
        poly.use_smooth = smooth
    mesh.materials.append(mat)

    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    obj.location = origin_world
    if parent is not None:
        obj.parent = parent
        obj.matrix_parent_inverse = parent.matrix_world.inverted()

    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.02)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.select_set(False)
    return obj


def build_shadow_placeholder():
    BLEND_PATH.parent.mkdir(parents=True, exist_ok=True)
    FBX_PATH.parent.mkdir(parents=True, exist_ok=True)
    MANIFEST_PATH.parent.mkdir(parents=True, exist_ok=True)
    PREVIEW_PATH.parent.mkdir(parents=True, exist_ok=True)

    if SCENE_NAME in bpy.data.scenes:
        old_scene = bpy.data.scenes[SCENE_NAME]
        if bpy.context.window.scene == old_scene and len(bpy.data.scenes) > 1:
            for s in bpy.data.scenes:
                if s != old_scene:
                    bpy.context.window.scene = s
                    break
        if len(bpy.data.scenes) > 1:
            bpy.data.scenes.remove(old_scene)

    scene = bpy.data.scenes.new(SCENE_NAME)
    bpy.context.window.scene = scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 1440

    col = bpy.data.collections.get("COL_ShadowPlaceholder")
    if col is not None:
        for o in list(col.objects):
            bpy.data.objects.remove(o, do_unlink=True)
    else:
        col = bpy.data.collections.new("COL_ShadowPlaceholder")
    if col.name not in scene.collection.children:
        scene.collection.children.link(col)

    # Restrained damp matte-wet charcoal materials (not glossy oil, not pure RGB 0,0,0)
    mat_skin = make_material("MAT_Shadow_Skin", (0.052, 0.055, 0.060, 1.0), 0.03, 0.56)
    mat_cloth = make_material("MAT_Shadow_Cloth", (0.078, 0.080, 0.085, 1.0), 0.01, 0.78)
    mat_paper = make_material("MAT_Shadow_Paper", (0.36, 0.33, 0.28, 1.0), 0.0, 0.90)

    root = bpy.data.objects.new("ACT_001_ShadowPlaceholder", None)
    root.empty_display_type = "PLAIN_AXES"
    root.empty_display_size = 0.25
    root.location = (0.0, 0.0, 0.0)
    col.objects.link(root)

    upper_origin = Vector((0.0, 0.02, 1.06))
    upper_pivot = bpy.data.objects.new("PIV_ShadowUpper", None)
    upper_pivot.empty_display_type = "ARROWS"
    upper_pivot.empty_display_size = 0.18
    upper_pivot.location = upper_origin
    col.objects.link(upper_pivot)
    upper_pivot.parent = root
    upper_pivot.matrix_parent_inverse = root.matrix_world.inverted()

    bpy.context.view_layer.update()
    created_meshes = []

    # 1. Lower body: uneven bowed legs with bent knees & gaunt silhouette (z = 0.00 .. 1.10 m)
    bm_lower = bmesh.new()
    left_leg_rings = [
        (Vector((-0.115, -0.10, 0.00)), 0.052, 0.118, 0.0, (0.0, 0.0, 0.08)),
        (Vector((-0.112, -0.05, 0.06)), 0.040, 0.062, 0.0, (0.0, 0.0, 0.05)),
        (Vector((-0.125, -0.05, 0.30)), 0.038, 0.046, 0.06, (0.08, -0.04, 0.0)),
        (Vector((-0.132, -0.10, 0.54)), 0.050, 0.056, 0.10, (0.12, -0.05, 0.0)),
        (Vector((-0.108, -0.03, 0.82)), 0.062, 0.068, 0.04, (-0.08, 0.02, 0.0)),
        (Vector((-0.082, 0.02, 1.05)), 0.074, 0.078, 0.0, (0.0, 0.0, 0.0)),
    ]
    add_lofted_tube(bm_lower, left_leg_rings, sides=10)

    right_leg_rings = [
        (Vector((0.110, 0.04, 0.00)), 0.050, 0.112, 0.0, (0.0, 0.0, -0.10)),
        (Vector((0.108, 0.06, 0.06)), 0.039, 0.060, 0.0, (0.0, 0.0, -0.08)),
        (Vector((0.106, 0.03, 0.31)), 0.036, 0.044, -0.06, (-0.06, 0.03, 0.0)),
        (Vector((0.088, -0.04, 0.56)), 0.048, 0.054, -0.08, (0.10, 0.06, 0.0)),
        (Vector((0.094, 0.01, 0.83)), 0.060, 0.066, -0.03, (-0.06, 0.0, 0.0)),
        (Vector((0.078, 0.03, 1.05)), 0.072, 0.076, 0.0, (0.0, 0.0, 0.0)),
    ]
    add_lofted_tube(bm_lower, right_leg_rings, sides=10)

    pelvis_rings = [
        (Vector((0.0, 0.02, 0.95)), 0.135, 0.084, 0.0, (0.05, 0.03, 0.0)),
        (Vector((0.0, 0.025, 1.04)), 0.142, 0.088, 0.02, (0.06, 0.03, 0.0)),
        (Vector((0.0, 0.02, 1.11)), 0.118, 0.078, 0.03, (0.08, 0.03, 0.0)),
    ]
    add_lofted_tube(bm_lower, pelvis_rings, sides=12)
    created_meshes.append(finish_mesh_object(col, "SM_ACT_ShadowLower", bm_lower, mat_skin, parent=root))

    # 2. Tattered soaked cloth strips around waist/hips
    bm_skirt = bmesh.new()
    hem_angles = [-2.65, -2.10, -1.52, -0.92, -0.32, 0.32, 0.92, 1.52, 2.10, 2.68]
    for idx, ang in enumerate(hem_angles):
        length = 0.30 + (idx % 4) * 0.08
        rx, ry = 0.148, 0.098
        flare = 0.020 if idx % 2 == 0 else 0.008
        p0 = Vector((math.sin(ang) * rx, math.cos(ang) * ry + 0.02, 1.06))
        p1 = Vector((math.sin(ang) * (rx + flare), math.cos(ang) * (ry + flare) + 0.01, 1.06 - length * 0.54))
        p2 = Vector((math.sin(ang) * (rx + flare * 0.6), math.cos(ang) * (ry + flare * 0.6), 1.06 - length))
        n_hint = Vector((math.sin(ang), math.cos(ang), 0.0)).normalized()
        add_curved_strip(bm_skirt, [p0, p1, p2], [0.076, 0.062, 0.028], normal_hint=n_hint, thickness=0.007, skew=0.02)
    created_meshes.append(finish_mesh_object(col, "SM_ACT_ShadowLowerCloth", bm_skirt, mat_cloth, parent=root))

    # 3. Gaunt hunched torso + clavicle/deltoid shoulder bridges (parented to PIV_ShadowUpper)
    bm_torso = bmesh.new()
    torso_rings = [
        (Vector((0.0, 0.02, 1.06)), 0.114, 0.076, 0.0, (0.08, 0.04, 0.0)),
        (Vector((-0.008, 0.01, 1.22)), 0.106, 0.074, 0.04, (0.14, 0.06, 0.0)),
        (Vector((-0.016, -0.02, 1.42)), 0.148, 0.096, 0.06, (0.22, 0.08, 0.0)),
        (Vector((-0.012, -0.065, 1.62)), 0.188, 0.108, 0.05, (0.28, 0.09, 0.0)),
        (Vector((0.004, -0.105, 1.77)), 0.205, 0.102, 0.03, (0.34, 0.08, 0.0)),
        (Vector((0.000, -0.135, 1.88)), 0.074, 0.068, 0.0, (0.38, 0.06, 0.0)),
    ]
    add_lofted_tube(bm_torso, torso_rings, sides=14)
    # Left & right clavicle-to-deltoid bridges so arms flow organically from hunched shoulders
    clav_l = [
        (Vector((-0.09, -0.09, 1.74)), 0.068, 0.065, 0.0, (0.2, -0.5, 0.0)),
        (Vector((-0.16, -0.095, 1.72)), 0.062, 0.060, 0.0, (0.2, -0.8, 0.0)),
        (Vector((-0.205, -0.10, 1.68)), 0.052, 0.054, 0.0, (0.2, -1.1, 0.0)),
    ]
    clav_r = [
        (Vector((0.09, -0.085, 1.77)), 0.068, 0.065, 0.0, (0.2, 0.5, 0.0)),
        (Vector((0.16, -0.088, 1.76)), 0.060, 0.058, 0.0, (0.2, 0.8, 0.0)),
        (Vector((0.202, -0.092, 1.72)), 0.050, 0.052, 0.0, (0.2, 1.1, 0.0)),
    ]
    add_lofted_tube(bm_torso, clav_l, sides=10)
    add_lofted_tube(bm_torso, clav_r, sides=10)
    # Subtle chest fissure indent (trauma motif, non-glowing, non-gore)
    for v in bm_torso.verts:
        if -0.055 < v.co.x < 0.045 and v.co.y < -0.08 and 1.44 < v.co.z < 1.70:
            factor = max(0.0, 1.0 - abs(v.co.x + 0.008) / 0.05) * max(0.0, 1.0 - abs(v.co.z - 1.57) / 0.13)
            v.co.y += 0.036 * factor
    created_meshes.append(
        finish_mesh_object(col, "SM_ACT_ShadowTorso", bm_torso, mat_skin, parent=upper_pivot, origin_world=upper_origin)
    )

    # 4. Tattered student shirt/jacket drapes & cross-bag strap
    bm_jacket = bmesh.new()
    shoulder_strips = [
        (
            [Vector((-0.185, -0.09, 1.75)), Vector((-0.222, -0.10, 1.58)), Vector((-0.195, -0.105, 1.38))],
            [0.125, 0.102, 0.045],
            Vector((-0.8, -0.6, 0.0)),
        ),
        (
            [Vector((0.185, -0.08, 1.79)), Vector((0.218, -0.085, 1.63)), Vector((0.195, -0.09, 1.45))],
            [0.130, 0.105, 0.048],
            Vector((0.8, -0.6, 0.0)),
        ),
        (
            [Vector((-0.145, -0.155, 1.73)), Vector((0.0, -0.175, 1.48)), Vector((0.125, -0.105, 1.20))],
            [0.038, 0.036, 0.034],
            Vector((0.0, -1.0, 0.0)),
        ),
        (
            [Vector((-0.085, 0.05, 1.71)), Vector((-0.095, 0.085, 1.44)), Vector((-0.075, 0.08, 1.18))],
            [0.125, 0.110, 0.062],
            Vector((0.0, 1.0, 0.0)),
        ),
        (
            [Vector((0.085, 0.05, 1.73)), Vector((0.095, 0.085, 1.46)), Vector((0.075, 0.08, 1.20))],
            [0.125, 0.110, 0.062],
            Vector((0.0, 1.0, 0.0)),
        ),
    ]
    for pts, wds, nh in shoulder_strips:
        add_curved_strip(bm_jacket, pts, wds, normal_hint=nh.normalized(), thickness=0.008, skew=0.015)
    created_meshes.append(
        finish_mesh_object(col, "SM_ACT_ShadowJacket", bm_jacket, mat_cloth, parent=upper_pivot, origin_world=upper_origin)
    )

    # 5. Bowed tilted head + damp face wrap + uneven wet hair tendrils (top z = 2.27 m)
    bm_head = bmesh.new()
    head_rings = [
        (Vector((-0.008, -0.138, 1.87)), 0.062, 0.064, 0.0, (0.42, 0.14, 0.0)),
        (Vector((-0.026, -0.178, 1.98)), 0.084, 0.098, 0.06, (0.48, 0.18, 0.0)),
        (Vector((-0.038, -0.198, 2.11)), 0.094, 0.110, 0.08, (0.50, 0.20, 0.0)),
        (Vector((-0.044, -0.186, 2.21)), 0.076, 0.090, 0.06, (0.46, 0.18, 0.0)),
        (Vector((-0.046, -0.168, 2.27)), 0.036, 0.042, 0.0, (0.42, 0.16, 0.0)),
    ]
    add_lofted_tube(bm_head, head_rings, sides=12)
    created_meshes.append(
        finish_mesh_object(col, "SM_ACT_ShadowHead", bm_head, mat_skin, parent=upper_pivot, origin_world=upper_origin)
    )

    bm_wrap = bmesh.new()
    wrap_rings = [
        (Vector((-0.034, -0.192, 2.03)), 0.099, 0.114, 0.06, (0.50, 0.20, 0.0)),
        (Vector((-0.041, -0.202, 2.13)), 0.101, 0.116, 0.10, (0.50, 0.20, 0.0)),
    ]
    add_lofted_tube(bm_wrap, wrap_rings, sides=12, cap_start=False, cap_end=False)
    hair_angles = [-2.3, -1.75, -1.25, -0.78, -0.35, 0.38, 0.85, 1.38, 1.92, 2.42]
    for idx, ang in enumerate(hair_angles):
        drop = 0.22 + (idx % 4) * 0.065
        hx = -0.038 + math.sin(ang) * 0.094
        hy = -0.186 - math.cos(ang) * 0.104
        p0 = Vector((hx, hy, 2.16))
        p1 = Vector((hx + math.sin(ang) * 0.012, hy - 0.018, 2.16 - drop * 0.52))
        p2 = Vector((hx + math.sin(ang) * 0.006, hy - 0.030, 2.16 - drop))
        add_curved_strip(bm_wrap, [p0, p1, p2], [0.024, 0.016, 0.005], normal_hint=Vector((0.0, -1.0, 0.0)), thickness=0.0035)
    created_meshes.append(
        finish_mesh_object(col, "SM_ACT_ShadowFaceWrap", bm_wrap, mat_cloth, parent=upper_pivot, origin_world=upper_origin)
    )

    # 6. Elongated asymmetric Left Arm (elbow bent slightly outward/back, long forearm reaching shin z=0.45m)
    bm_arm_l = bmesh.new()
    arm_l_rings = [
        (Vector((-0.200, -0.095, 1.69)), 0.052, 0.055, 0.0, (0.14, -0.10, 0.0)),
        (Vector((-0.228, -0.070, 1.38)), 0.041, 0.044, 0.05, (0.08, -0.06, 0.0)),
        (Vector((-0.236, -0.085, 1.05)), 0.039, 0.043, 0.10, (0.16, -0.02, 0.0)),
        (Vector((-0.220, -0.135, 0.72)), 0.031, 0.034, 0.12, (0.14, 0.04, 0.0)),
        (Vector((-0.204, -0.155, 0.58)), 0.027, 0.021, 0.12, (0.08, 0.05, 0.0)),
    ]
    add_lofted_tube(bm_arm_l, arm_l_rings, sides=9)
    for f_idx, fx_off in enumerate([-0.018, -0.006, 0.006, 0.018]):
        flen = 0.120 + (1 if f_idx in (1, 2) else 0) * 0.024
        f_rings = [
            (Vector((-0.204 + fx_off * 0.5, -0.156 + fx_off, 0.58)), 0.0062, 0.0062, 0.0, (0.0, 0.0, 0.0)),
            (Vector((-0.194 + fx_off * 0.5, -0.170 + fx_off, 0.58 - flen * 0.55)), 0.0048, 0.0048, 0.0, (0.0, 0.0, 0.0)),
            (Vector((-0.180 + fx_off * 0.5, -0.160 + fx_off, 0.58 - flen)), 0.0025, 0.0025, 0.0, (0.0, 0.0, 0.0)),
        ]
        add_lofted_tube(bm_arm_l, f_rings, sides=6)
    created_meshes.append(
        finish_mesh_object(col, "SM_ACT_ShadowArmLeft", bm_arm_l, mat_skin, parent=upper_pivot, origin_world=upper_origin)
    )

    # 7. Asymmetric Right Arm (bent elbow, reaches mid-thigh z=0.68m)
    bm_arm_r = bmesh.new()
    arm_r_rings = [
        (Vector((0.198, -0.088, 1.73)), 0.050, 0.053, 0.0, (0.12, 0.10, 0.0)),
        (Vector((0.222, -0.065, 1.44)), 0.039, 0.042, -0.05, (0.06, 0.06, 0.0)),
        (Vector((0.226, -0.080, 1.15)), 0.037, 0.040, -0.08, (0.14, 0.02, 0.0)),
        (Vector((0.206, -0.120, 0.86)), 0.029, 0.032, -0.10, (0.10, -0.04, 0.0)),
        (Vector((0.192, -0.135, 0.76)), 0.025, 0.019, -0.10, (0.06, -0.05, 0.0)),
    ]
    add_lofted_tube(bm_arm_r, arm_r_rings, sides=9)
    for f_idx, fx_off in enumerate([-0.016, -0.005, 0.006, 0.017]):
        flen = 0.095 + (1 if f_idx in (1, 2) else 0) * 0.018
        f_rings = [
            (Vector((0.192 - fx_off * 0.5, -0.136 + fx_off, 0.76)), 0.0058, 0.0058, 0.0, (0.0, 0.0, 0.0)),
            (Vector((0.183 - fx_off * 0.5, -0.148 + fx_off, 0.76 - flen * 0.55)), 0.0044, 0.0044, 0.0, (0.0, 0.0, 0.0)),
            (Vector((0.172 - fx_off * 0.5, -0.140 + fx_off, 0.76 - flen)), 0.0024, 0.0024, 0.0, (0.0, 0.0, 0.0)),
        ]
        add_lofted_tube(bm_arm_r, f_rings, sides=6)
    created_meshes.append(
        finish_mesh_object(col, "SM_ACT_ShadowArmRight", bm_arm_r, mat_skin, parent=upper_pivot, origin_world=upper_origin)
    )

    # 8. Rain-soaked crooked/torn academic paper scraps (5 scraps, MAT_Shadow_Paper)
    bm_papers = bmesh.new()
    paper_specs = [
        ([Vector((-0.072, -0.172, 1.65)), Vector((-0.084, -0.180, 1.58)), Vector((-0.076, -0.170, 1.51))], [0.082, 0.078, 0.068], 0.018),
        ([Vector((-0.105, -0.096, 1.22)), Vector((-0.120, -0.108, 1.12)), Vector((-0.108, -0.098, 1.02))], [0.108, 0.102, 0.088], -0.022),
        ([Vector((0.080, -0.094, 1.19)), Vector((0.092, -0.105, 1.09)), Vector((0.084, -0.096, 1.00))], [0.098, 0.094, 0.080], 0.020),
        ([Vector((0.140, -0.025, 1.15)), Vector((0.152, -0.032, 1.05)), Vector((0.144, -0.026, 0.96))], [0.088, 0.084, 0.072], -0.016),
        ([Vector((0.052, 0.068, 1.61)), Vector((0.058, 0.082, 1.52)), Vector((0.050, 0.076, 1.44))], [0.086, 0.082, 0.070], 0.015),
    ]
    for pts, wds, sk in paper_specs:
        add_curved_strip(bm_papers, pts, wds, normal_hint=Vector((0.0, -1.0, 0.0)), thickness=0.003, skew=sk)
    created_meshes.append(
        finish_mesh_object(col, "SM_ACT_ShadowPapers", bm_papers, mat_paper, parent=upper_pivot, origin_world=upper_origin)
    )

    # Preview lighting + camera
    key = bpy.data.lights.new("LGT_PreviewRim", type="POINT")
    key.energy = 160.0
    key.color = (0.48, 0.68, 0.92)
    key_obj = bpy.data.objects.new("LGT_PreviewRim", key)
    key_obj.location = (0.7, 1.7, 2.45)
    col.objects.link(key_obj)

    fill = bpy.data.lights.new("LGT_PreviewFront", type="POINT")
    fill.energy = 55.0
    fill.color = (0.92, 0.76, 0.58)
    fill_obj = bpy.data.objects.new("LGT_PreviewFront", fill)
    fill_obj.location = (-1.0, -2.1, 1.55)
    col.objects.link(fill_obj)

    cam_data = bpy.data.cameras.new("CAM_Preview")
    cam_data.lens = 42.0
    cam_obj = bpy.data.objects.new("CAM_Preview", cam_data)
    cam_obj.location = (-0.95, -3.25, 1.30)
    direction = Vector((0.0, -0.05, 1.15)) - cam_obj.location
    cam_obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    col.objects.link(cam_obj)
    scene.camera = cam_obj

    if scene.world is None:
        scene.world = bpy.data.worlds.new("WLD_ShadowPreview")
    scene.world.use_nodes = True
    bg = scene.world.node_tree.nodes.get("Background")
    if bg is not None:
        bg.inputs[0].default_value = (0.018, 0.022, 0.028, 1.0)
        bg.inputs[1].default_value = 0.6

    bpy.context.view_layer.update()

    min_v = Vector((1e9, 1e9, 1e9))
    max_v = Vector((-1e9, -1e9, -1e9))
    total_tris = 0
    total_verts = 0
    for obj in created_meshes:
        mw = obj.matrix_world
        for v in obj.data.vertices:
            wv = mw @ v.co
            min_v.x = min(min_v.x, wv.x)
            min_v.y = min(min_v.y, wv.y)
            min_v.z = min(min_v.z, wv.z)
            max_v.x = max(max_v.x, wv.x)
            max_v.y = max(max_v.y, wv.y)
            max_v.z = max(max_v.z, wv.z)
        obj.data.calc_loop_triangles()
        total_tris += len(obj.data.loop_triangles)
        total_verts += len(obj.data.vertices)

    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    upper_pivot.select_set(True)
    for obj in created_meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root

    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=True,
        object_types={"EMPTY", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
        use_custom_props=True,
    )

    scene.render.filepath = str(PREVIEW_PATH)
    scene.render.image_settings.file_format = "PNG"
    bpy.ops.render.render(write_still=True)

    for window in bpy.context.window_manager.windows:
        for area in window.screen.areas:
            if area.type == "VIEW_3D":
                for space in area.spaces:
                    if space.type == "VIEW_3D":
                        space.shading.type = "MATERIAL"
                        r3d = space.region_3d
                        r3d.view_location = Vector((0.0, -0.05, 1.15))
                        r3d.view_distance = 3.3
                        r3d.view_rotation = Euler((math.radians(82.0), 0.0, math.radians(-24.0)), "XYZ").to_quaternion()

    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH), compress=True)

    manifest = {
        "assetId": "ACT_001_ShadowPlaceholder",
        "task": "T06",
        "note": "Original T06 3D placeholder silhouette authored in Blender 5.2.3 LTS. Final deformable topology, Generic rig, Variant A/B 2K PBR, and 8 animations remain T13.",
        "blendSource": str(BLEND_PATH),
        "fbxExport": str(FBX_PATH),
        "previewRender": str(PREVIEW_PATH),
        "exportAxes": {"forward": "-Z", "up": "Y", "unityAssemblyYawDegrees": 0.0},
        "boundsMinMetres": [round(min_v.x, 4), round(min_v.y, 4), round(min_v.z, 4)],
        "boundsMaxMetres": [round(max_v.x, 4), round(max_v.y, 4), round(max_v.z, 4)],
        "dimensionsMetres": {
            "widthX": round(max_v.x - min_v.x, 4),
            "depthY": round(max_v.y - min_v.y, 4),
            "heightZ": round(max_v.z - min_v.z, 4),
        },
        "upperPivotHeightMetres": 1.06,
        "meshCount": len(created_meshes),
        "vertices": total_verts,
        "triangles": total_tris,
        "materials": ["MAT_Shadow_Skin", "MAT_Shadow_Cloth", "MAT_Shadow_Paper"],
        "meshes": [o.name for o in created_meshes],
    }
    MANIFEST_PATH.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    return manifest


if __name__ == "__main__":
    result = build_shadow_placeholder()
    print(json.dumps(result, indent=2))
