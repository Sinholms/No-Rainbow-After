import math
import os
import bpy
import bmesh
from mathutils import Vector

REPO = r"c:\Users\User\Documents\Nono\No-Rainbow-After"
BLEND_PATH = os.path.join(REPO, "BlenderSources", "Environment", "ChapterThreeParking", "source", "untitled.blend")
OUT_DIR = os.path.join(REPO, "Assets", "_NoRainbowAfter", "Art", "Models", "Environment", "ChapterThree")
OUT_FBX = os.path.join(OUT_DIR, "ENV_C3_BasementParking.fbx")

ARENA_HALF = 26.0
CEILING_HEIGHT = 4.4


def ensure_material(name):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name=name)
    return mat


def make_box(name, center, size, material):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co.x *= size[0]
        v.co.y *= size[1]
        v.co.z *= size[2]
    bm.to_mesh(mesh)
    bm.free()

    # Generate simple box UVs so URP normal/roughness maps tile cleanly
    uv_layer = mesh.uv_layers.new(name="UVMap")
    for poly in mesh.polygons:
        n = poly.normal
        for loop_idx in poly.loop_indices:
            co = mesh.vertices[mesh.loops[loop_idx].vertex_index].co
            if abs(n.z) >= abs(n.x) and abs(n.z) >= abs(n.y):
                uv = (co.x * 0.25, co.y * 0.25)
            elif abs(n.x) >= abs(n.y):
                uv = (co.y * 0.25, co.z * 0.25)
            else:
                uv = (co.x * 0.25, co.z * 0.25)
            uv_layer.data[loop_idx].uv = uv

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = center
    obj.data.materials.append(material)
    return obj


def make_cylinder(name, p0, p1, radius, material, segments=12):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    vec = Vector(p1) - Vector(p0)
    length = vec.length
    bmesh.ops.create_cone(
        bm,
        cap_ends=True,
        cap_tris=False,
        segments=segments,
        radius1=radius,
        radius2=radius,
        depth=length,
    )
    rot = vec.normalized().to_track_quat('Z', 'Y').to_matrix().to_4x4()
    bmesh.ops.transform(bm, matrix=rot, verts=bm.verts)
    bm.to_mesh(mesh)
    bm.free()

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = (Vector(p0) + Vector(p1)) * 0.5
    obj.data.materials.append(material)
    return obj


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=BLEND_PATH)

    # Clear all source objects after loading material/geometry context from the zip blend
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)

    mat_floor = ensure_material("C3_Floor")
    mat_ceiling = ensure_material("C3_Ceiling")
    mat_wall = ensure_material("C3_Wall")
    mat_pipe = ensure_material("C3_PipeRed")
    mat_light = ensure_material("C3_LightEmissive")
    mat_paint = ensure_material("C3_LanePaint")
    mat_trim = ensure_material("C3_ConcreteTrim")

    # Wide open underground basement parking arena (52m x 52m outer shell, 4.4m ceiling)
    make_box("C3_BasementFloor", (0.0, 0.0, -0.20), (ARENA_HALF * 2.0 + 2.0, ARENA_HALF * 2.0 + 2.0, 0.40), mat_floor)
    make_box("C3_BasementCeiling", (0.0, 0.0, CEILING_HEIGHT + 0.20), (ARENA_HALF * 2.0 + 2.0, ARENA_HALF * 2.0 + 2.0, 0.40), mat_ceiling)

    # Four perimeter concrete walls framing the underground garage (no interior corridors or pillars)
    wall_len = ARENA_HALF * 2.0 + 1.0
    wall_h = CEILING_HEIGHT + 0.4
    wall_z = CEILING_HEIGHT * 0.5
    make_box("C3_Wall_North", (0.0, ARENA_HALF, wall_z), (wall_len, 0.5, wall_h), mat_wall)
    make_box("C3_Wall_South", (0.0, -ARENA_HALF, wall_z), (wall_len, 0.5, wall_h), mat_wall)
    make_box("C3_Wall_East", (ARENA_HALF, 0.0, wall_z), (0.5, wall_len, wall_h), mat_wall)
    make_box("C3_Wall_West", (-ARENA_HALF, 0.0, wall_z), (0.5, wall_len, wall_h), mat_wall)

    # Perimeter concrete wall plinths / hazard curbing right along the outer walls
    for idx, (cx, cy, sx, sy) in enumerate([
        (0.0, ARENA_HALF - 0.35, wall_len - 1.0, 0.25),
        (0.0, -ARENA_HALF + 0.35, wall_len - 1.0, 0.25),
        (ARENA_HALF - 0.35, 0.0, 0.25, wall_len - 1.0),
        (-ARENA_HALF + 0.35, 0.0, 0.25, wall_len - 1.0),
    ]):
        make_box(f"C3_PerimeterCurb_{idx}", (cx, cy, 0.12), (sx, sy, 0.24), mat_trim)

    # Overhead structural ceiling beams & red sprinkler pipes (derived from the zip's ceiling layout, scaled to 4.4m height)
    beam_y_positions = [-18.0, -6.0, 6.0, 18.0]
    for i, by in enumerate(beam_y_positions):
        make_box(f"C3_CeilingBeam_{i}", (0.0, by, CEILING_HEIGHT - 0.16), (ARENA_HALF * 2.0, 0.55, 0.32), mat_trim)

    pipe_x_positions = [-16.0, -5.5, 5.5, 16.0]
    for i, px in enumerate(pipe_x_positions):
        make_cylinder(
            f"C3_CeilingPipe_{i}",
            (px, -ARENA_HALF + 0.5, CEILING_HEIGHT - 0.36),
            (px, ARENA_HALF - 0.5, CEILING_HEIGHT - 0.36),
            0.065,
            mat_pipe,
        )

    # Overhead fluorescent fixture housings across the ceiling grid
    fixture_xs = [-15.0, 0.0, 15.0]
    fixture_ys = [-15.0, 0.0, 15.0]
    for xi, fx in enumerate(fixture_xs):
        for yi, fy in enumerate(fixture_ys):
            make_box(f"C3_CeilingLightBox_{xi}_{yi}", (fx, fy, CEILING_HEIGHT - 0.05), (1.8, 0.38, 0.08), mat_light)

    # Faint worn parking bay paint lines flush on floor near perimeter (no collision)
    for side in (-1, 1):
        for bay in range(-3, 4):
            bx = bay * 5.2
            make_box(f"C3_BayLine_{side}_{bay}", (bx, side * 20.5, 0.004), (0.12, 4.2, 0.008), mat_paint)
        make_box(f"C3_DriveLane_{side}", (0.0, side * 12.0, 0.004), (36.0, 0.12, 0.008), mat_paint)

    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    bpy.ops.export_scene.fbx(
        filepath=OUT_FBX,
        use_selection=False,
        axis_forward='-Z',
        axis_up='Y',
        apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,
        object_types={'MESH'},
        use_mesh_modifiers=True,
        mesh_smooth_type='FACE',
    )
    print(f"[C3] Exported expanded basement arena FBX to {OUT_FBX}")


if __name__ == "__main__":
    main()
