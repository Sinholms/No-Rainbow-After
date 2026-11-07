"""Reviewed T04 retrofit of the existing Blender kos; no scene regeneration.

Run inside Blender with NRA_KosAtmosphere_v1 loaded. Export names/axes stay stable.
Back up source and FBX before running; only named geometry is resized/moved.
"""
import bpy
import json
from pathlib import Path
from mathutils import Vector

ROOT = Path('D:/GRAVKOM/No rainbow after')
scene = bpy.data.scenes['NRA_KosAtmosphere_v1']
collection = bpy.data.collections['NRA_KosAtmosphere_Geometry']
bpy.context.window.scene = scene
required = ['SM_ENV_CorridorCeiling', 'SM_ENV_NicheBack', 'SM_ENV_NicheBack.001',
            'SM_ENV_NicheFloor', 'SM_ENV_NicheFloor.001', 'SM_ENV_NicheEnd',
            'SM_ENV_NicheEnd.001', 'SM_ENV_NicheEnd.002', 'SM_ENV_NicheEnd.003',
            'SM_PROP_UtilityCabinet', 'SM_PROP_UtilityCabinet.001']
for name in required:
    obj = collection.objects.get(name)
    if obj is None or obj.type != 'MESH':
        raise RuntimeError('Required original mesh unavailable: ' + name)
    if any(abs(v - 1.0) > 0.001 for v in obj.scale):
        raise RuntimeError('Unexpected unapplied scale: ' + name)

def resize_x(obj, width, floor=False):
    old_width = max(v.co.x for v in obj.data.vertices) - min(v.co.x for v in obj.data.vertices)
    factor = width / old_width
    for vertex in obj.data.vertices:
        vertex.co.x *= factor
    obj.data.update()
    # Preserve the original metre-based planar UV density after changing width.
    uv = obj.data.uv_layers.active
    scale = 2.5 if floor else 0.7
    if uv:
        for poly in obj.data.polygons:
            axes = (0, 1) if abs(poly.normal.z) > 0.5 else (1, 2) if abs(poly.normal.x) > 0.5 else (0, 2)
            for li in poly.loop_indices:
                co = obj.data.vertices[obj.data.loops[li].vertex_index].co
                uv.data[li].uv = (co[axes[0]] * scale, co[axes[1]] * scale)

resize_x(collection.objects['SM_ENV_CorridorCeiling'], 4.80)
for obj in collection.objects:
    side = -1 if obj.location.x < 0 else 1
    if obj.name.startswith('SM_ENV_NicheBack'):
        obj.location.x = side * 2.25
    elif obj.name.startswith('SM_ENV_NicheFloor'):
        obj.location.x = side * 1.55
        resize_x(obj, 1.50, floor=True)
    elif obj.name.startswith('SM_ENV_NicheEnd'):
        obj.location.x = side * 1.62
        resize_x(obj, 1.34)
    elif obj.name.startswith('SM_PROP_UtilityCabinet'):
        obj.location.x = side * 2.02
    elif obj.name.startswith('SM_PROP_CabinetDoor'):
        obj.location.x = side * 1.83
    elif obj.name.startswith('SM_PROP_CabinetVent'):
        obj.location.x = side * 1.816
bpy.context.view_layer.update()

for obj in scene.objects:
    obj.select_set(False)
for obj in collection.objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active = collection.objects['SM_ENV_RoomFloor']
export = ROOT / 'Assets/_NoRainbowAfter/Art/Models/Environment/ENV_KosAtmosphere_v1.fbx'
bpy.ops.export_scene.fbx(filepath=str(export), use_selection=True, object_types={'MESH'},
    apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y',
    use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False, path_mode='AUTO', use_custom_props=True)

graph = bpy.context.evaluated_depsgraph_get()
triangles = 0
for obj in collection.objects:
    if obj.type == 'MESH':
        evaluated = obj.evaluated_get(graph)
        mesh = evaluated.to_mesh()
        mesh.calc_loop_triangles()
        triangles += len(mesh.loop_triangles)
        evaluated.to_mesh_clear()
manifest_path = ROOT / 'BlenderSources/Environment/KosAtmosphere_v1_manifest.json'
manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
manifest['revision'] = 'T04: deeper cover niches, relocated cabinets, extended ceiling'
manifest['triangles'] = triangles
manifest_path.write_text(json.dumps(manifest, indent=2), encoding='utf-8')
report = {'revision': 'T04', 'meshCount': len(collection.objects), 'triangles': triangles,
    'roomMetres': [4, 5, 2.7], 'corridorMetres': [1.8, 14, 2.7],
    'nicheInnerDepthMetres': 1.26, 'nicheClearLaneBeforeCabinetMetres': 0.916,
    'coverPointsUnity': [[-1.45, 0.04, 9.0], [1.45, 0.04, 13.0]],
    'changed': [{'name': n, 'locationBlender': list(collection.objects[n].location),
                 'sizeBlender': list(collection.objects[n].dimensions)} for n in required]}
(ROOT / 'BlenderSources/Environment/T04_cover_manifest.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == 'VIEW_3D':
            space = area.spaces.active
            eye = Vector((0.15, 7.6, 1.65))
            target = Vector((-1.45, 9.0, 0.95))
            space.region_3d.view_rotation = (target - eye).to_track_quat('-Z', 'Y')
            space.region_3d.view_distance = (target - eye).length
            space.region_3d.view_location = target
            space.region_3d.view_perspective = 'PERSP'
            space.clip_start = 0.02
            space.overlay.show_overlays = False
            area.tag_redraw()
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'BlenderSources/Environment/KosAtmosphere_v1.blend'), copy=True, compress=True)
print(json.dumps(report))
