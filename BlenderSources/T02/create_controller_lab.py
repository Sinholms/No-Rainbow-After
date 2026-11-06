import bpy, math, json
from pathlib import Path
ROOT = Path("D:/GRAVKOM/No rainbow after")
SOURCE = ROOT / "BlenderSources/T02/ControllerLab.blend"
EXPORT = ROOT / "Assets/_NoRainbowAfter/Art/Models/Environment/T02_ControllerLab.fbx"
SCENE_NAME = "NRA_T02_ControllerLab"
if SCENE_NAME in bpy.data.scenes or SOURCE.exists() or EXPORT.exists():
    raise RuntimeError("T02 source/export already exists; refusing to overwrite.")
SOURCE.parent.mkdir(parents=True, exist_ok=True)
EXPORT.parent.mkdir(parents=True, exist_ok=True)
lab = bpy.data.scenes.new(SCENE_NAME)
lab.unit_settings.system = 'METRIC'
lab.unit_settings.scale_length = 1.0
collection = bpy.data.collections.new("NRA_T02_LabGeometry")
lab.collection.children.link(collection)
bpy.context.window.scene = lab
materials = {}
def material(name, color, roughness):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    node = mat.node_tree.nodes.get('Principled BSDF')
    node.inputs['Base Color'].default_value = (*color, 1)
    node.inputs['Roughness'].default_value = roughness
    materials[name] = mat
    return mat
concrete = material("MAT_LabConcrete", (0.25, 0.30, 0.31), 0.83)
floor = material("MAT_LabFloor", (0.10, 0.16, 0.18), 0.46)
trim = material("MAT_LabTrim", (0.065, 0.10, 0.12), 0.65)
accent = material("MAT_LabTeal", (0.16, 0.39, 0.38), 0.57)
amber = material("MAT_LabAmber", (0.49, 0.39, 0.22), 0.62)
faces = [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
def mesh_object(name, vertices, polygons, mat):
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], polygons)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    obj.data.materials.append(mat)
    uv = mesh.uv_layers.new(name="UVMap")
    for poly in mesh.polygons:
        for j, index in enumerate(poly.loop_indices):
            uv.data[index].uv = ((j == 1 or j == 2), (j >= 2))
    return obj
def box(name, center, dimensions, mat, bevel=0):
    x,y,z = [v/2 for v in dimensions]
    vertices = [(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),(-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
    obj = mesh_object(name, vertices, faces, mat)
    obj.location = center
    if bevel:
        modifier = obj.modifiers.new("SmallEdgeBevel", 'BEVEL')
        modifier.width = bevel
        modifier.segments = 1
        modifier.affect = 'EDGES'
    return obj
box("SM_ENV_T02_Foundation", (0,4,-0.16), (12,18,0.32), floor)
for x in range(-6,6):
    for y in range(-5,13):
        box("SM_ENV_T02_Tile_%02d_%02d" % (x+6,y+5), (x+0.5,y+0.5,0.008), (0.985,0.985,0.016), floor)
box("SM_ENV_T02_WallLeft", (-6,4,1.35), (0.2,18.2,2.7), concrete)
box("SM_ENV_T02_WallRight", (6,4,1.35), (0.2,18.2,2.7), concrete)
box("SM_ENV_T02_WallFront", (0,-5,1.35), (12,0.2,2.7), concrete)
box("SM_ENV_T02_WallBack", (0,13,1.35), (12,0.2,2.7), concrete)
for side in [-5.88,5.88]:
    box("SM_ENV_T02_Skirting", (side,4,0.10), (0.06,18,0.20), trim)
for x in [-4.6,-1.6,1.6,4.6]:
    box("SM_ENV_T02_LaneStripe", (x,4,0.024), (0.035,16.5,0.008), accent)
# Crouch passage: 1.25m clear, 2.2m wide.
box("SM_ENV_T02_CrouchRoof", (3,3,1.40), (2.2,4,0.30), amber, 0.01)
for side in [1.84,4.16]:
    box("SM_ENV_T02_CrouchSide", (side,3,0.70), (0.12,4,1.4), trim, 0.01)
# Ramp: 0.60m rise over 3m run, approximately 11.3 degrees.
ramp = mesh_object("SM_ENV_T02_Ramp", [(-1,-1.5,0),(1,-1.5,0),(1,1.5,0),(-1,1.5,0),(-1,-1.5,0.025),(1,-1.5,0.025),(1,1.5,0.625),(-1,1.5,0.625)], faces, accent)
ramp.location = (-3,6,0)
box("SM_ENV_T02_RampLanding", (-3,8.1,0.3125), (2,1.2,0.625), accent)
# Four shallow steps, each 0.15m high.
for i in range(4):
    height = (i+1)*0.15
    box("SM_ENV_T02_Step_%d" % (i+1), (0,6+i*0.65,height/2), (1.8,0.65,height), concrete, 0.008)
box("SM_ENV_T02_StepLanding", (0,8.4,0.30), (1.8,0.9,0.6), concrete)
# Door reference: 0.90m clear width and 2.05m clear height, matching design.md.
for x in [-0.53,0.53]:
    box("SM_ENV_T02_DoorJamb", (x,10.4,1.10), (0.16,0.24,2.20), trim, 0.008)
box("SM_ENV_T02_DoorLintel", (0,10.4,2.125), (1.22,0.24,0.15), trim, 0.008)
for y in [-2,3,8,11]:
    for x in [-5.5,5.5]:
        box("SM_ENV_T02_LightFixture", (x,y,2.45), (0.12,1.0,0.12), trim)
        box("SM_ENV_T02_LightDiffuser", (x,y,2.39), (0.08,0.8,0.035), accent)
# Save a copy of all scene data; the user's original Blender scene remains intact.
for obj in lab.objects:
    obj.select_set(True)
bpy.context.view_layer.objects.active = next(iter(collection.objects))
bpy.ops.export_scene.fbx(filepath=str(EXPORT), use_selection=True, object_types={'MESH'}, apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y', bake_space_transform=False, use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False, path_mode='AUTO')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE), copy=True)
depsgraph = bpy.context.evaluated_depsgraph_get()
tris = 0
for obj in collection.objects:
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    mesh.calc_loop_triangles()
    tris += len(mesh.loop_triangles)
    evaluated.to_mesh_clear()
print(json.dumps({'source':str(SOURCE),'export':str(EXPORT),'scene':lab.name,'objects':len(collection.objects),'triangles':tris,'floor_m':[12,18],'crouch_clearance_m':1.25,'door_clearance_m':[0.9,2.05],'ramp_degrees':round(math.degrees(math.atan(0.6/3)),2)}))
# Keep the working scene visible, as requested by the user.
bpy.context.window.scene = lab
