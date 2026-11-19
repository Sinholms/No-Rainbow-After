"""High-detail organic FPP physical student body for Raka (ACT_Raka_StudentBody).
Matched closely against the user's character turnaround reference (FRONT, SIDE, BACK, HAND DETAIL):
- Tailored buttoned-up short-sleeve dark navy/slate/charcoal plaid collared shirt with
  folded pointed collar, front button placket + 5 buttons, left chest pocket + clipped pen,
  back yoke seam, and open curved hemmed tail (no balloon end-caps)
- Smooth curved blue neck lanyard, metallic clasp, and beveled white student ID card
- Continuous unified quad-manifold Arm -> Forearm -> Wrist -> Palm -> 5 Articulated Fingers
  (seamlessly extruded Thumb + Index/Middle/Ring/Pinky with interdigital skin webbing & Subsurf)
- Sleek flush matte-black circular wristwatch on left dorsal wrist
- Tailored dark charcoal-black slim trousers with subtle knee drape + open folded ankle hems
- Rounded minimalist matte-black slip-on sneakers with thick off-white platform rubber soles

Exports hierarchy with FPP animation pivots intact:
  ACT_Raka_StudentBody -> TorsoPivot, Left/RightArmPivot, Left/RightLegPivot, Left/RightKneePivot
Faces -Y in Blender (+Z forward in Unity after prefab wrapper orientation).
"""
import math
from pathlib import Path
import numpy as np
import bpy
import bmesh
from mathutils import Vector, Matrix

ROOT = Path(r"c:\Users\User\Documents\Nono\No-Rainbow-After")
EXPORT_FBX = ROOT / "Assets/_NoRainbowAfter/Art/Models/Characters/ACT_Raka_StudentBody.fbx"
BLEND_PATH = ROOT / "BlenderSources/Characters/ACT_Raka_StudentBody.blend"
TEX_DIR = ROOT / "Assets/_NoRainbowAfter/Art/Textures/RakaBody"


def save_texture(name, rgb):
    TEX_DIR.mkdir(parents=True, exist_ok=True)
    h, w = rgb.shape[:2]
    rgba = np.concatenate([np.clip(rgb, 0.0, 1.0), np.ones((h, w, 1), dtype=np.float32)], axis=2)
    img = bpy.data.images.get(name)
    if img is not None and (img.size[0] != w or img.size[1] != h):
        bpy.data.images.remove(img)
        img = None
    if img is None:
        img = bpy.data.images.new(name, width=w, height=h, alpha=True)
    img.colorspace_settings.name = "sRGB"
    img.pixels.foreach_set(rgba.astype(np.float32).ravel())
    path = TEX_DIR / f"{name}.png"
    img.filepath_raw = str(path)
    img.file_format = "PNG"
    img.save()
    return img


def make_flannel_texture():
    """Crisp dark navy / muted slate-blue / charcoal plaid matching the turnaround shirt."""
    s = 1024
    v, u = np.mgrid[0:1:complex(s), 0:1:complex(s)]
    freq = 7.0
    gu = (u * freq) % 1.0
    gv = (v * freq) % 1.0

    slate_u = (gu < 0.48).astype(np.float32)
    slate_v = (gv < 0.48).astype(np.float32)
    navy_u = ((gu >= 0.44) & (gu < 0.88)).astype(np.float32)
    navy_v = ((gv >= 0.44) & (gv < 0.88)).astype(np.float32)
    seam_u = (np.abs(gu - 0.93) < 0.032).astype(np.float32)
    seam_v = (np.abs(gv - 0.93) < 0.032).astype(np.float32)

    weave = (np.sin((u + v) * 460.0 * math.pi) * 0.012 + np.sin((u - v) * 460.0 * math.pi) * 0.006)[:, :, None]

    col = np.full((s, s, 3), np.array([0.070, 0.082, 0.098], dtype=np.float32))
    col += (slate_u[:, :, None] * 0.5 + slate_v[:, :, None] * 0.5) * np.array([0.088, 0.132, 0.178], dtype=np.float32)
    col += (navy_u[:, :, None] * 0.5 + navy_v[:, :, None] * 0.5) * np.array([0.025, 0.048, 0.078], dtype=np.float32)
    col += (slate_u * slate_v)[:, :, None] * np.array([0.048, 0.075, 0.105], dtype=np.float32)
    col *= (1.0 - 0.38 * np.maximum(seam_u, seam_v)[:, :, None])
    col += weave
    return save_texture("T_RakaBody_Flannel", col)


def make_jeans_texture():
    """Fine dark charcoal-black tailored trouser fabric."""
    s = 512
    v, u = np.mgrid[0:1:complex(s), 0:1:complex(s)]
    rng = np.random.default_rng(42)
    grain = rng.normal(0.0, 0.008, (s, s, 1)).astype(np.float32)
    twill = (np.sin((u * 180.0 + v * 180.0) * math.pi) * 0.008)[:, :, None].astype(np.float32)
    col = np.array([0.078, 0.082, 0.088], dtype=np.float32) + twill + grain
    return save_texture("T_RakaBody_Jeans", col)


def make_material(name, color, roughness=0.80, metallic=0.0, image=None):
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
    if "Specular IOR Level" in bsdf.inputs:
        bsdf.inputs["Specular IOR Level"].default_value = 0.25 if roughness > 0.7 else 0.42
    if image is not None:
        tex = nodes.new("ShaderNodeTexImage")
        tex.image = image
        links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    mat.diffuse_color = (*color, 1.0)
    mat.roughness = roughness
    mat.metallic = metallic
    return mat


def apply_box_uv(mesh, uv_scale=2.2):
    uv = mesh.uv_layers.get("UVMap") or mesh.uv_layers.new(name="UVMap")
    for poly in mesh.polygons:
        poly.use_smooth = True
        n = poly.normal
        ax, ay, az = abs(n.x), abs(n.y), abs(n.z)
        for li in poly.loop_indices:
            co = mesh.vertices[mesh.loops[li].vertex_index].co
            if az >= ax and az >= ay:
                u_val, v_val = co.x, co.y
            elif ax >= ay:
                u_val, v_val = co.y, co.z
            else:
                u_val, v_val = co.x, co.z
            uv.data[li].uv = (u_val * uv_scale + 0.5, v_val * uv_scale + 0.5)


def finalize_mesh(col, name, verts, faces, mat, parent, offset=(0.0, 0.0, 0.0),
                  subdiv=2, uv_scale=2.2, solidify=0.0):
    shifted = [(v[0] - offset[0], v[1] - offset[1], v[2] - offset[2]) for v in verts]
    mesh = bpy.data.meshes.new(name + "_mesh")
    mesh.from_pydata(shifted, [], faces)
    mesh.update()
    apply_box_uv(mesh, uv_scale=uv_scale)

    obj = bpy.data.objects.new(name, mesh)
    col.objects.link(obj)
    obj.data.materials.append(mat)
    obj.parent = parent

    if solidify > 0.0:
        sol = obj.modifiers.new("Solidify", "SOLIDIFY")
        sol.thickness = solidify
        sol.offset = 0.0
    if subdiv > 0:
        sub = obj.modifiers.new("Subsurf", "SUBSURF")
        sub.levels = subdiv
        sub.render_levels = subdiv
    return obj


def loft_rings(col, name, ring_lists, mat, parent, offset=(0.0, 0.0, 0.0),
               cap_start=False, cap_end=False, subdiv=2, uv_scale=2.2, solidify=0.0):
    # Ensure consistent bottom-to-top vertical ring order so quad winding produces outward normals (critical for Unity backface culling).
    z0 = sum(p[2] for p in ring_lists[0]) / len(ring_lists[0])
    z1 = sum(p[2] for p in ring_lists[-1]) / len(ring_lists[-1])
    if len(ring_lists) >= 2 and abs(z1 - z0) > 1e-4 and z1 < z0:
        ring_lists = list(reversed(ring_lists))
        cap_start, cap_end = cap_end, cap_start

    sides = len(ring_lists[0])
    verts = [pt for ring in ring_lists for pt in ring]
    faces = []
    for r in range(len(ring_lists) - 1):
        b = r * sides
        n = (r + 1) * sides
        for j in range(sides):
            jn = (j + 1) % sides
            faces.append((b + j, b + jn, n + jn, n + j))

    # For outward CCW orientation relative to each ring centroid, verify bottom quad normal points outward;
    # if inward (e.g. folded cuff ring turning back up), build cleanly without flipping wall winding.
    if len(ring_lists) >= 2:
        c = Vector((0.0, 0.0, 0.0))
        for p in ring_lists[0]:
            c += Vector(p)
        c /= sides
        v0 = Vector(ring_lists[0][0])
        v1 = Vector(ring_lists[0][1])
        v2 = Vector(ring_lists[1][0])
        n_test = (v1 - v0).cross(v2 - v0)
        radial = Vector((v0.x - c.x, v0.y - c.y, 0.0))
        if n_test.dot(radial) < 0.0:
            faces = [tuple(reversed(f)) for f in faces]

    if cap_start:
        c0 = Vector((0.0, 0.0, 0.0))
        for p in ring_lists[0]:
            c0 += Vector(p)
        c0 /= sides
        c0_idx = len(verts)
        verts.append((c0.x, c0.y, c0.z))
        for j in range(sides):
            jn = (j + 1) % sides
            faces.append((c0_idx, jn, j))

    if cap_end:
        last_base = (len(ring_lists) - 1) * sides
        c1 = Vector((0.0, 0.0, 0.0))
        for p in ring_lists[-1]:
            c1 += Vector(p)
        c1 /= sides
        c1_idx = len(verts)
        verts.append((c1.x, c1.y, c1.z))
        for j in range(sides):
            jn = (j + 1) % sides
            faces.append((c1_idx, last_base + j, last_base + jn))

    return finalize_mesh(col, name, verts, faces, mat, parent, offset=offset,
                         subdiv=subdiv, uv_scale=uv_scale, solidify=solidify)


def sample_ellipse_ring(cx, cy, cz, rx, ry, sides=28, rot_z=0.0, crease_amp=0.0,
                        chest_shape=0.0, side_hem_lift=0.0, shoulder_drop=0.0):
    pts = []
    cz_rot = math.cos(rot_z)
    sz_rot = math.sin(rot_z)
    for j in range(sides):
        ang = j * math.tau / sides
        ca, sa = math.cos(ang), math.sin(ang)
        r_mod = 1.0 + crease_amp * math.sin(ang * 4.0 + cz * 14.0)
        if chest_shape != 0.0:
            if sa < 0.0:
                r_mod += chest_shape * 0.040 * (math.cos(ang * 2.0) + 0.5)
            else:
                r_mod -= chest_shape * 0.032 * max(0.0, 1.0 - abs(ca) * 3.0)
        lx = rx * ca * r_mod
        ly = ry * sa * r_mod
        rx_pt = lx * cz_rot - ly * sz_rot
        ry_pt = lx * sz_rot + ly * cz_rot
        dz = side_hem_lift * (ca ** 2) - shoulder_drop * (ca ** 2)
        pts.append((cx + rx_pt, cy + ry_pt, cz + dz))
    return pts


def build_spline_tube(col, name, control_points_radii, mat, parent, offset=(0.0, 0.0, 0.0), sides=12, subdiv=2):
    rings = []
    n_pts = len(control_points_radii)
    prev_side = Vector((1.0, 0.0, 0.0))
    for i, (pos, rx, ry) in enumerate(control_points_radii):
        p = Vector(pos)
        if i == 0:
            tangent = (Vector(control_points_radii[1][0]) - p).normalized()
        elif i == n_pts - 1:
            tangent = (p - Vector(control_points_radii[-2][0])).normalized()
        else:
            tangent = (Vector(control_points_radii[i + 1][0]) - Vector(control_points_radii[i - 1][0])).normalized()

        side = prev_side - tangent * prev_side.dot(tangent)
        if side.length < 1e-5:
            fallback = Vector((0.0, 1.0, 0.0)) if abs(tangent.x) > 0.8 else Vector((1.0, 0.0, 0.0))
            side = tangent.cross(fallback).normalized()
        else:
            side.normalize()
        prev_side = side
        normal = tangent.cross(side).normalized()

        ring = []
        for j in range(sides):
            ang = j * math.tau / sides
            pt = p + side * (rx * math.cos(ang)) + normal * (ry * math.sin(ang))
            ring.append((pt.x, pt.y, pt.z))
        rings.append(ring)

    return loft_rings(col, name, rings, mat, parent, offset=offset, cap_start=True, cap_end=True, subdiv=subdiv, uv_scale=3.0)


def build_shirt_collar(col, mat_flannel, parent, torso_origin):
    verts, faces = [], []
    sides = 36
    front_angle = -math.pi * 0.5
    gap_half = 0.22
    r_profiles = [
        (0.072, 0.066, 1.344, 0.0),
        (0.078, 0.072, 1.383, 0.0),
        (0.097, 0.089, 1.336, 0.023),
    ]
    start_ang = front_angle + gap_half
    end_ang = front_angle + math.tau - gap_half
    for rx, ry, z_base, front_drop in r_profiles:
        for j in range(sides):
            t = j / (sides - 1)
            ang = start_ang + t * (end_ang - start_ang)
            ca, sa = math.cos(ang), math.sin(ang)
            front_factor = max(0.0, -sa) ** 2.2
            z = z_base - front_drop * front_factor
            flare = 1.0 + 0.10 * front_factor
            verts.append((rx * ca * flare, -0.010 + ry * sa * flare, z))

    for r in range(len(r_profiles) - 1):
        b, n = r * sides, (r + 1) * sides
        for j in range(sides - 1):
            faces.append((b + j, b + j + 1, n + j + 1, n + j))

    return finalize_mesh(col, "SM_Raka_Collar", verts, faces, mat_flannel, parent,
                         offset=torso_origin, subdiv=2, uv_scale=2.5, solidify=0.005)


def build_front_placket_and_buttons(col, mat_flannel, mat_button, parent, torso_origin):
    chest_profile = [
        (0.802, -0.112),
        (0.880, -0.109),
        (0.980, -0.105),
        (1.120, -0.117),
        (1.240, -0.123),
        (1.335, -0.100),
    ]
    rings = []
    for z, cy_front in chest_profile:
        rings.append([
            (-0.014, cy_front - 0.002, z),
            (0.0, cy_front - 0.0035, z),
            (0.014, cy_front - 0.002, z),
            (0.014, cy_front + 0.002, z),
            (0.0, cy_front + 0.001, z),
            (-0.014, cy_front + 0.002, z),
        ])
    loft_rings(col, "SM_Raka_ButtonPlacket", rings, mat_flannel, parent,
               offset=torso_origin, cap_start=True, cap_end=True, subdiv=1, uv_scale=2.5)

    button_z = [1.26, 1.18, 1.09, 1.00, 0.91]
    z_arr = [p[0] for p in chest_profile]
    y_arr = [p[1] for p in chest_profile]
    for idx, bz in enumerate(button_z):
        by = float(np.interp(bz, z_arr, y_arr)) - 0.0045
        disc_rings = []
        for dy, r in [(0.0015, 0.0040), (0.0, 0.0050), (-0.0020, 0.0044)]:
            disc_rings.append([(r * math.cos(k * math.tau / 12), by + dy, bz + r * math.sin(k * math.tau / 12)) for k in range(12)])
        loft_rings(col, f"SM_Raka_Button_{idx}", disc_rings, mat_button, parent,
                   offset=torso_origin, cap_start=True, cap_end=True, subdiv=1)


def build_chest_pocket_and_pen(col, mat_flannel, mat_watch, mat_metal, parent, torso_origin):
    pocket_rings = []
    slices = [
        (1.124, 0.019, -0.005),
        (1.137, 0.031, -0.004),
        (1.168, 0.032, -0.004),
        (1.196, 0.032, -0.004),
    ]
    for z, hw, dy_out in slices:
        cx, cy = 0.095, -0.103
        ring = []
        for u in np.linspace(-1.0, 1.0, 6):
            px = cx + float(u) * hw
            py = cy - math.cos(float(u) * 0.45) * 0.011 + dy_out
            ring.append((px, py, z))
        for u in np.linspace(1.0, -1.0, 6):
            px = cx + float(u) * hw
            py = cy - math.cos(float(u) * 0.45) * 0.011 + 0.002
            ring.append((px, py, z))
        pocket_rings.append(ring)

    loft_rings(col, "SM_Raka_ShirtPocket", pocket_rings, mat_flannel, parent,
               offset=torso_origin, cap_start=True, cap_end=True, subdiv=1, uv_scale=2.5)

    pen_pts = [
        ((0.082, -0.118, 1.158), 0.0034, 0.0034),
        ((0.082, -0.117, 1.196), 0.0038, 0.0038),
        ((0.082, -0.115, 1.224), 0.0038, 0.0038),
        ((0.082, -0.114, 1.236), 0.0022, 0.0022),
    ]
    build_spline_tube(col, "SM_Raka_PocketPen", pen_pts, mat_watch, parent, offset=torso_origin, sides=10, subdiv=1)

    clip_pts = [
        ((0.082, -0.121, 1.228), 0.0013, 0.0010),
        ((0.082, -0.123, 1.216), 0.0015, 0.0011),
        ((0.082, -0.123, 1.182), 0.0013, 0.0010),
    ]
    build_spline_tube(col, "SM_Raka_PenClip", clip_pts, mat_metal, parent, offset=torso_origin, sides=8, subdiv=1)


def build_lanyard_and_id_card(col, mat_lanyard, mat_metal, mat_card, parent, torso_origin):
    strap_L = [
        ((0.044, -0.025, 1.376), 0.0070, 0.0022),
        ((0.068, -0.068, 1.356), 0.0075, 0.0022),
        ((0.056, -0.106, 1.310), 0.0075, 0.0022),
        ((0.031, -0.124, 1.245), 0.0072, 0.0022),
        ((0.004, -0.126, 1.188), 0.0068, 0.0022),
    ]
    strap_R = [
        ((-0.044, -0.025, 1.376), 0.0070, 0.0022),
        ((-0.068, -0.068, 1.356), 0.0075, 0.0022),
        ((-0.056, -0.106, 1.310), 0.0075, 0.0022),
        ((-0.031, -0.124, 1.245), 0.0072, 0.0022),
        ((-0.004, -0.126, 1.188), 0.0068, 0.0022),
    ]
    build_spline_tube(col, "SM_Raka_Lanyard_L", strap_L, mat_lanyard, parent, offset=torso_origin, sides=12, subdiv=2)
    build_spline_tube(col, "SM_Raka_Lanyard_R", strap_R, mat_lanyard, parent, offset=torso_origin, sides=12, subdiv=2)

    clip_pts = [
        ((0.0, -0.127, 1.192), 0.0062, 0.0040),
        ((0.0, -0.129, 1.172), 0.0050, 0.0036),
        ((0.0, -0.129, 1.158), 0.0058, 0.0036),
    ]
    build_spline_tube(col, "SM_Raka_LanyardClip", clip_pts, mat_metal, parent, offset=torso_origin, sides=10, subdiv=1)

    hw, hd = 0.026, 0.0022
    card_rings = []
    for cz in [1.078, 1.092, 1.132, 1.156]:
        cy = -0.128 - (cz - 1.078) * 0.025
        w_scale = 0.90 if (cz == 1.078 or cz == 1.156) else 1.0
        card_rings.append([
            (-hw * w_scale, cy - hd, cz),
            (0.0, cy - hd * 1.10, cz),
            (hw * w_scale, cy - hd, cz),
            (hw * w_scale, cy + hd, cz),
            (0.0, cy + hd * 1.10, cz),
            (-hw * w_scale, cy + hd, cz),
        ])
    loft_rings(col, "SM_Raka_StudentCard", card_rings, mat_card, parent,
               offset=torso_origin, cap_start=True, cap_end=True, subdiv=2, uv_scale=4.0)


def make_hand_palm_ring(cx, cy, cz, rx_thickness, ry_width, side_sign, knuckle_arch=0.0):
    """Build an ordered 16-vertex ring: dorsal[0..7] from -Y to +Y, then palmar[7..0] back from +Y to -Y.
    Columns across Y allow direct quad extrusion for Index [0..1], Middle [2..3], Ring [4..5], Pinky [6..7]
    while [1..2], [3..4], [5..6] form anatomical interdigital skin webbing.
    """
    # Normalize 8 Y positions across [-0.5 .. +0.5] so finger quads and slim web gaps align anatomically
    y_norm = [-0.50, -0.31, -0.25, -0.05, 0.02, 0.22, 0.28, 0.50]
    inward = -side_sign  # palmar side points toward body (-side_sign)

    dorsal = []
    palmar = []
    for i, t in enumerate(y_norm):
        py = cy + t * ry_width * 2.0
        # Elliptical thickness profile: thinner toward outer radial/ulnar edges
        profile = math.sqrt(max(0.18, 1.0 - (t * 1.90) ** 2))
        half_x = rx_thickness * profile
        # Subtle middle-knuckle crest & palmar arch
        z_arch = knuckle_arch * math.sin((t + 0.5) * math.pi) * 0.008
        # Dorsal lies on outer (+side_sign), palmar lies on inner (-side_sign)
        dorsal.append((cx + side_sign * half_x, py, cz + z_arch))
        palmar.append((cx + inward * half_x, py, cz + z_arch))

    # Concatenate dorsal[0..7] followed by reversed palmar[7..0]
    return dorsal + list(reversed(palmar))


def build_unified_arm_and_hand(col, sname, side_sign, arm_pivot, sh_world, mat_skin):
    """Build ONE watertight quad-manifold mesh for Forearm -> Wrist -> Palm -> 5 Sculpted Fingers.
    No separate capsules, zero sausage wrist seams; Subsurf L2 yields organic muscles, knuckles & fingers.
    """
    ox, oy, oz = sh_world
    inward = -side_sign

    # Continuous 16-vert loops from upper arm inside sleeve -> elbow -> muscular forearm -> wrist -> palm -> knuckles
    # Upper arm & bicep stay comfortably inside SM_Raka_Sleeve (so no skin pokes through fabric)
    arm_sections = [
        (side_sign * 0.205, -0.009, 1.210, 0.036, 0.038, 0.0),  # deep inside short sleeve
        (side_sign * 0.216, -0.012, 1.135, 0.037, 0.038, 0.0),  # upper arm inside sleeve
        (side_sign * 0.226, -0.016, 1.080, 0.039, 0.040, 0.0),  # emerging cleanly from cuff
        (side_sign * 0.233, -0.022, 1.015, 0.037, 0.038, 0.0),  # elbow crease
        (side_sign * 0.237, -0.032, 0.965, 0.039, 0.037, 0.0),  # forearm flexor/brachioradialis swell
        (side_sign * 0.238, -0.044, 0.900, 0.030, 0.032, 0.0),  # tapered mid forearm
        (side_sign * 0.236, -0.052, 0.858, 0.022, 0.028, 0.0),  # anatomical slender wrist
        (side_sign * 0.236, -0.055, 0.838, 0.020, 0.034, 0.2),  # heel of palm & thumb saddle top
        (side_sign * 0.234, -0.058, 0.806, 0.0165, 0.039, 0.5), # mid palm (thumb saddle bottom)
        (side_sign * 0.231, -0.058, 0.778, 0.0125, 0.039, 1.0), # MCP knuckle arch
    ]

    bm = bmesh.new()
    rings_bm = []
    for cx, cy, cz, rx, ry, arch in arm_sections:
        ring_coords = make_hand_palm_ring(cx, cy, cz, rx, ry, side_sign, knuckle_arch=arch)
        ring_verts = [bm.verts.new((p[0] - ox, p[1] - oy, p[2] - oz)) for p in ring_coords]
        rings_bm.append(ring_verts)

    # Connect arm/palm rings cleanly around perimeter (16 quads per section)
    for r in range(len(rings_bm) - 1):
        r0, r1 = rings_bm[r], rings_bm[r + 1]
        for j in range(16):
            jn = (j + 1) % 16
            bm.faces.new((r0[j], r0[jn], r1[jn], r1[j]))

    # Cap top inside sleeve cleanly
    top_center = bm.verts.new(sum((v.co for v in rings_bm[0]), Vector()) / 16.0)
    for j in range(16):
        jn = (j + 1) % 16
        bm.faces.new((top_center, rings_bm[0][jn], rings_bm[0][j]))

    # Cap knuckle arch across the 7 dorsal-to-palmar columns (indices i = 0..6):
    # Dorsal[i]=i, Dorsal[i+1]=i+1, Palmar[i+1]=15-(i+1), Palmar[i]=15-i
    knuckle_ring = rings_bm[-1]
    knuckle_quads = []
    for i in range(7):
        d_a = knuckle_ring[i]
        d_b = knuckle_ring[i + 1]
        p_b = knuckle_ring[15 - (i + 1)]
        p_a = knuckle_ring[15 - i]
        f = bm.faces.new((d_a, d_b, p_b, p_a))
        knuckle_quads.append(f)

    # Slightly pull inward the 3 webbing faces (indices 1, 3, 5) to carve interdigital valleys
    for w_idx in (1, 3, 5):
        f = knuckle_quads[w_idx]
        # Shift mid vertices subtly upward (+Z) so web sits higher than knuckle exit
        for v in f.verts:
            v.co.z += 0.0025

    bm.verts.ensure_lookup_table()
    bm.faces.ensure_lookup_table()

    # Helper: extrude a 4-vert face along chained phalanx joints cleanly in quad topology
    def extrude_finger_from_quad(face, phalanx_steps):
        """Extrude quad iteratively along [(delta_vec, scale_x, scale_y), ...] ending with soft rounded dome."""
        curr_verts = list(face.verts)
        # First inset/pinch base slightly (0.90) to define finger crease cleanly away from web
        center0 = sum((v.co for v in curr_verts), Vector()) / 4.0
        base_verts = []
        # Create inner base loop connected to `face` via a short collar step
        n_dir = face.normal.normalized()
        bm.faces.remove(face)
        prev_loop = curr_verts
        for step_idx, (delta, sx, sy, rot_curl) in enumerate(phalanx_steps):
            c_prev = sum((v.co for v in prev_loop), Vector()) / 4.0
            c_next = c_prev + Vector(delta)
            new_loop = []
            for v in prev_loop:
                rel = v.co - c_prev
                # Local frame scaling
                scaled = Vector((rel.x * sx, rel.y * sy, rel.z))
                new_loop.append(bm.verts.new(c_next + scaled))
            for k in range(4):
                kn = (k + 1) % 4
                bm.faces.new((prev_loop[k], prev_loop[kn], new_loop[kn], new_loop[k]))
            prev_loop = new_loop
        # Close rounded fingertip dome
        bm.faces.new(prev_loop)

    # 1. Extrude 4 Fingers from alternating knuckle quads: Index(0), Middle(2), Ring(4), Pinky(6)
    # Rounded anatomical fingertips (avoid pinching) and natural gentle finger curl
    finger_specs = [
        (0, [  # Index (front -Y, rounded anatomical tip)
            ((inward * 0.001, -0.001, -0.004), 0.94, 0.94, 0.0),
            ((inward * 0.002, -0.001, -0.022), 0.96, 0.96, 0.0),
            ((inward * 0.004, -0.001, -0.018), 0.95, 0.95, 0.0),
            ((inward * 0.005, -0.001, -0.014), 0.92, 0.92, 0.0),
            ((inward * 0.002,  0.000, -0.005), 0.74, 0.74, 0.0),
        ]),
        (2, [  # Middle (longest, ~7.1cm)
            ((inward * 0.001,  0.000, -0.004), 0.94, 0.92, 0.0),
            ((inward * 0.003,  0.000, -0.025), 0.95, 0.95, 0.0),
            ((inward * 0.006,  0.000, -0.020), 0.93, 0.93, 0.0),
            ((inward * 0.006,  0.000, -0.015), 0.88, 0.88, 0.0),
            ((inward * 0.002,  0.000, -0.005), 0.65, 0.65, 0.0),
        ]),
        (4, [  # Ring (~6.6cm)
            ((inward * 0.001,  0.001, -0.004), 0.94, 0.92, 0.0),
            ((inward * 0.004,  0.002, -0.023), 0.95, 0.95, 0.0),
            ((inward * 0.006,  0.001, -0.018), 0.93, 0.93, 0.0),
            ((inward * 0.006,  0.001, -0.014), 0.88, 0.88, 0.0),
            ((inward * 0.002,  0.000, -0.005), 0.65, 0.65, 0.0),
        ]),
        (6, [  # Pinky (~5.0cm)
            ((inward * 0.002,  0.002, -0.004), 0.88, 0.84, 0.0),
            ((inward * 0.004,  0.003, -0.017), 0.92, 0.92, 0.0),
            ((inward * 0.006,  0.002, -0.014), 0.90, 0.90, 0.0),
            ((inward * 0.006,  0.001, -0.010), 0.84, 0.84, 0.0),
            ((inward * 0.002,  0.000, -0.004), 0.62, 0.62, 0.0),
        ]),
    ]

    # Locate thumb saddle quad on the -Y front side between palm ring [7] and [8]
    r_hi = rings_bm[7]
    r_lo = rings_bm[8]
    thumb_quad = bm.faces.get((r_hi[15], r_hi[0], r_lo[0], r_lo[15]))

    for q_idx, steps in finger_specs:
        extrude_finger_from_quad(knuckle_quads[q_idx], steps)

    # 2. Extrude proportional anatomical Thumb forward-downward with rounded tip
    if thumb_quad is not None:
        extrude_finger_from_quad(thumb_quad, [
            ((inward * 0.004, -0.010, -0.010), 0.78, 0.86, 0.0),
            ((inward * 0.006, -0.009, -0.016), 0.95, 0.95, 0.0),
            ((inward * 0.005, -0.006, -0.015), 0.92, 0.92, 0.0),
            ((inward * 0.002, -0.003, -0.005), 0.72, 0.72, 0.0),
        ])

    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(f"SM_Raka_ArmHand_{sname}_mesh")
    bm.to_mesh(mesh)
    bm.free()

    apply_box_uv(mesh, uv_scale=3.0)
    obj = bpy.data.objects.new(f"SM_Raka_Forearm_{sname}", mesh)
    col.objects.link(obj)
    obj.data.materials.append(mat_skin)
    obj.parent = arm_pivot
    sub = obj.modifiers.new("Subsurf", "SUBSURF")
    sub.levels = 2
    sub.render_levels = 2
    return obj


def build_organic_sneaker(col, sname, hip_x, mat_sneaker, mat_sole, knee_pivot, knee_world):
    """Build minimalist black slip-on sneaker with thick rounded off-white platform sole."""
    def foot_ring(y_heel, y_toe, x_half_mid, z_val):
        pts = []
        steps = 28
        for j in range(steps):
            ang = j * math.tau / steps
            ca, sa = math.cos(ang), math.sin(ang)
            t_toe = max(0.0, -sa)
            t_heel = max(0.0, sa)
            width_mod = 1.0 - 0.22 * (t_heel ** 1.6) - 0.28 * (max(0.0, t_toe - 0.44) / 0.56) ** 2.0
            rx = x_half_mid * width_mod
            cy = (y_heel + y_toe) * 0.5
            ry = (y_heel - y_toe) * 0.5
            pts.append((hip_x + rx * ca, cy + ry * sa, z_val))
        return pts

    # Thick off-white platform rubber sole (z = 0.000 .. 0.032)
    sole_rings = [
        foot_ring(0.068, -0.175, 0.050, 0.001),
        foot_ring(0.070, -0.178, 0.053, 0.015),
        foot_ring(0.067, -0.173, 0.051, 0.031),
    ]
    loft_rings(col, f"SM_Raka_SneakerSole_{sname}", sole_rings, mat_sole, knee_pivot,
               offset=knee_world, cap_start=True, cap_end=True, subdiv=2, uv_scale=3.0)

    # Minimalist matte black slip-on upper tucked smoothly under trouser ankle cuff
    upper_rings = [
        foot_ring(0.064, -0.171, 0.049, 0.028),
        foot_ring(0.060, -0.156, 0.047, 0.054),
        foot_ring(0.051, -0.105, 0.044, 0.080),
        foot_ring(0.044, -0.045, 0.040, 0.106),
    ]
    loft_rings(col, f"SM_Raka_SneakerUpper_{sname}", upper_rings, mat_sneaker, knee_pivot,
               offset=knee_world, cap_start=True, cap_end=True, subdiv=2, uv_scale=3.0)


def build_student_body():
    EXPORT_FBX.parent.mkdir(parents=True, exist_ok=True)
    BLEND_PATH.parent.mkdir(parents=True, exist_ok=True)

    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for col in list(bpy.data.collections):
        bpy.data.collections.remove(col)

    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0

    col = bpy.data.collections.new("ACT_Raka_StudentBody_Col")
    scene.collection.children.link(col)

    flannel_img = make_flannel_texture()
    jeans_img = make_jeans_texture()

    mat_tee = make_material("MAT_RakaBody_Tee", (0.04, 0.045, 0.052), roughness=0.92)
    mat_flannel = make_material("MAT_RakaBody_Flannel", (1.0, 1.0, 1.0), roughness=0.88, image=flannel_img)
    mat_jeans = make_material("MAT_RakaBody_Jeans", (1.0, 1.0, 1.0), roughness=0.86, image=jeans_img)
    mat_skin = make_material("MAT_RakaBody_Skin", (0.55, 0.38, 0.28), roughness=0.55)
    mat_lanyard = make_material("MAT_RakaBody_Lanyard", (0.14, 0.34, 0.48), roughness=0.76)
    mat_card = make_material("MAT_RakaBody_Card", (0.90, 0.91, 0.90), roughness=0.40)
    mat_watch = make_material("MAT_RakaBody_Watch", (0.035, 0.04, 0.045), roughness=0.42, metallic=0.25)
    mat_sneaker = make_material("MAT_RakaBody_Sneakers", (0.055, 0.06, 0.068), roughness=0.78)
    mat_sole = make_material("MAT_RakaBody_Sole", (0.85, 0.84, 0.79), roughness=0.68)

    root = bpy.data.objects.new("ACT_Raka_StudentBody", None)
    col.objects.link(root)

    torso_origin = (0.0, 0.0, 0.88)
    torso_pivot = bpy.data.objects.new("TorsoPivot", None)
    col.objects.link(torso_pivot)
    torso_pivot.parent = root
    torso_pivot.location = torso_origin

    # Dark neck cap below camera near-clip (z max = 1.375m)
    inner_neck = [
        sample_ellipse_ring(0.0, -0.008, 1.30, 0.095, 0.080, sides=24),
        sample_ellipse_ring(0.0, -0.010, 1.35, 0.072, 0.066, sides=24),
        sample_ellipse_ring(0.0, -0.010, 1.375, 0.060, 0.055, sides=24),
    ]
    loft_rings(col, "SM_Raka_InnerTee", inner_neck, mat_tee, torso_pivot, offset=torso_origin, cap_end=True, subdiv=1)

    # Tailored short-sleeve collared plaid shirt draping cleanly OVER trouser waist (solidified subtle hem edge)
    shirt_rings = [
        sample_ellipse_ring(0.0, 0.002, 0.790, 0.172, 0.115, sides=32, crease_amp=0.010, side_hem_lift=0.008),
        sample_ellipse_ring(0.0, 0.000, 0.895, 0.155, 0.105, sides=32, crease_amp=0.008),
        sample_ellipse_ring(0.0, -0.004, 1.020, 0.160, 0.107, sides=32, crease_amp=0.006, chest_shape=0.4),
        sample_ellipse_ring(0.0, -0.008, 1.150, 0.177, 0.114, sides=32, crease_amp=0.005, chest_shape=0.9),
        sample_ellipse_ring(0.0, -0.010, 1.268, 0.188, 0.116, sides=32, chest_shape=1.0),
        sample_ellipse_ring(0.0, -0.010, 1.335, 0.152, 0.095, sides=32),
        sample_ellipse_ring(0.0, -0.010, 1.364, 0.082, 0.072, sides=32),
    ]
    loft_rings(col, "SM_Raka_FlannelBody", shirt_rings, mat_flannel, torso_pivot,
               offset=torso_origin, cap_start=True, cap_end=False, subdiv=2, uv_scale=2.2)

    build_shirt_collar(col, mat_flannel, torso_pivot, torso_origin)
    build_front_placket_and_buttons(col, mat_flannel, mat_watch, torso_pivot, torso_origin)
    build_chest_pocket_and_pen(col, mat_flannel, mat_watch, mat_card, torso_pivot, torso_origin)
    build_lanyard_and_id_card(col, mat_lanyard, mat_card, mat_card, torso_pivot, torso_origin)

    # Sloped natural sleeves + unified quad-manifold forearms/5-finger hands + flush dorsal wristwatch
    for side_sign, sname in [(1, "Left"), (-1, "Right")]:
        sh_world = (side_sign * 0.194, -0.008, 1.290)
        arm_pivot = bpy.data.objects.new(f"{sname}ArmPivot", None)
        col.objects.link(arm_pivot)
        arm_pivot.parent = torso_pivot
        arm_pivot.location = (
            sh_world[0] - torso_origin[0],
            sh_world[1] - torso_origin[1],
            sh_world[2] - torso_origin[2],
        )

        # Tailored natural shoulder drape dropping cleanly from shoulder seam (solid outer plaid sleeve wrapping bicep)
        sleeve_rings = [
            sample_ellipse_ring(side_sign * 0.224, -0.015, 1.102, 0.055, 0.054, sides=24),  # crisp short sleeve cuff edge
            sample_ellipse_ring(side_sign * 0.212, -0.012, 1.175, 0.058, 0.058, sides=24, crease_amp=0.008),
            sample_ellipse_ring(side_sign * 0.178, -0.010, 1.245, 0.058, 0.062, sides=24),  # smooth deltoid shoulder emergence
            sample_ellipse_ring(side_sign * 0.130, -0.008, 1.275, 0.052, 0.066, sides=24),  # nested deeply inside upper torso armhole
        ]
        loft_rings(col, f"SM_Raka_Sleeve_{sname}", sleeve_rings, mat_flannel, arm_pivot,
                   offset=sh_world, cap_start=True, cap_end=False, subdiv=2, uv_scale=2.2)

        build_unified_arm_and_hand(col, sname, side_sign, arm_pivot, sh_world, mat_skin)

        # Sleek flush matte-black circular wristwatch on dorsal/outer Left wrist (+X in Blender = real Left in Unity)
        if side_sign > 0:
            wx, wy, wz = side_sign * 0.236, -0.052, 0.868
            strap_rings = [
                sample_ellipse_ring(wx, wy, wz - 0.009, 0.0235, 0.0305, sides=24),
                sample_ellipse_ring(wx, wy, wz, 0.0255, 0.0320, sides=24),
                sample_ellipse_ring(wx, wy, wz + 0.009, 0.0235, 0.0305, sides=24),
            ]
            loft_rings(col, "SM_Raka_WatchStrap", strap_rings, mat_watch, arm_pivot,
                       offset=sh_world, cap_start=False, cap_end=False, subdiv=2)

            dial_cx = wx + 0.023
            dial_cy = wy + 0.004
            dial_rings = []
            for d_out, r in [(-0.002, 0.013), (0.002, 0.0165), (0.0045, 0.0155)]:
                dial_rings.append([
                    (dial_cx + d_out, dial_cy + r * math.cos(a), wz + r * math.sin(a))
                    for a in [k * math.tau / 20 for k in range(20)]
                ])
            loft_rings(col, "SM_Raka_WatchDial", dial_rings, mat_watch, arm_pivot,
                       offset=sh_world, cap_start=True, cap_end=True, subdiv=1)

    # Trouser pelvis seated cleanly UNDER the shirt hem (no hard cylinder sticking outside shirt!)
    pelvis_rings = [
        sample_ellipse_ring(0.0, 0.002, 0.725, 0.144, 0.095, sides=32),
        sample_ellipse_ring(0.0, 0.001, 0.785, 0.146, 0.096, sides=32),
        sample_ellipse_ring(0.0, 0.000, 0.845, 0.138, 0.092, sides=32),
    ]
    loft_rings(col, "SM_Raka_JeansWaist", pelvis_rings, mat_jeans, root,
               offset=(0.0, 0.0, 0.0), cap_start=True, cap_end=False, subdiv=2)

    # Slim-straight organic trousers (shirt drapes completely over upper thigh tops) + minimalist slip-on sneakers
    for side_sign, sname in [(1, "Left"), (-1, "Right")]:
        hip_x = side_sign * 0.086
        hip_world = (hip_x, 0.0, 0.88)
        leg_pivot = bpy.data.objects.new(f"{sname}LegPivot", None)
        col.objects.link(leg_pivot)
        leg_pivot.parent = root
        leg_pivot.location = hip_world

        thigh_rings = [
            sample_ellipse_ring(hip_x, -0.010, 0.440, 0.063, 0.066, sides=24, crease_amp=0.014),
            sample_ellipse_ring(hip_x, -0.008, 0.560, 0.068, 0.073, sides=24, crease_amp=0.012),
            sample_ellipse_ring(hip_x, -0.002, 0.720, 0.074, 0.081, sides=24, crease_amp=0.010),
            sample_ellipse_ring(hip_x, 0.000, 0.835, 0.068, 0.076, sides=24),  # nested completely inside shirt tail!
        ]
        loft_rings(col, f"SM_Raka_Thigh_{sname}", thigh_rings, mat_jeans, leg_pivot,
                   offset=hip_world, cap_start=False, cap_end=False, subdiv=2, uv_scale=2.2)

        knee_world = (hip_x, -0.010, 0.48)
        knee_pivot = bpy.data.objects.new(f"{sname}KneePivot", None)
        col.objects.link(knee_pivot)
        knee_pivot.parent = leg_pivot
        knee_pivot.location = (0.0, knee_world[1] - hip_world[1], knee_world[2] - hip_world[2])

        shin_rings = [
            sample_ellipse_ring(hip_x, -0.008, 0.092, 0.053, 0.056, sides=24, crease_amp=0.016),  # crisp ankle break cuff
            sample_ellipse_ring(hip_x, -0.007, 0.200, 0.056, 0.058, sides=24, crease_amp=0.014),
            sample_ellipse_ring(hip_x, -0.008, 0.360, 0.061, 0.064, sides=24, crease_amp=0.014),
            sample_ellipse_ring(hip_x, -0.010, 0.515, 0.062, 0.065, sides=24, crease_amp=0.014),  # nested cleanly inside thigh
        ]
        loft_rings(col, f"SM_Raka_Shin_{sname}", shin_rings, mat_jeans, knee_pivot,
                   offset=knee_world, cap_start=True, cap_end=False, subdiv=2, uv_scale=2.2)

        build_organic_sneaker(col, sname, hip_x, mat_sneaker, mat_sole, knee_pivot, knee_world)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in list(col.objects):
        if obj.type == "MESH" and len(obj.modifiers) > 0:
            bpy.context.view_layer.objects.active = obj
            obj.select_set(True)
            bpy.ops.object.convert(target="MESH")
            obj.select_set(False)

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
    print(f"EXPORTED_RAKA_BODY_ORGANIC_V2: {EXPORT_FBX}")


if __name__ == "__main__":
    build_student_body()
