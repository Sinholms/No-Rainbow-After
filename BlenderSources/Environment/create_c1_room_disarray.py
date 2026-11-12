"""Additive horror-room dressing; original room and its interactive papers stay intact."""
import bpy, random, math, json
from pathlib import Path
from mathutils import Matrix, Vector

ROOT = Path('D:/GRAVKOM/No rainbow after')
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'BlenderSources/Environment/KosAtmosphere_v1.blend'))
rng = random.Random(1126)
sources = list(bpy.context.scene.objects)
collection = bpy.data.collections.new('C1_Room_Disarray')
bpy.context.scene.collection.children.link(collection)
created=[]

def duplicate(source, name, matrix):
    obj=source.copy()
    obj.data=source.data.copy()
    obj.name=name
    collection.objects.link(obj)
    obj.matrix_world=matrix
    created.append(obj)
    return obj

def scatter(source, name, pos, angle=0, scale=1):
    obj=duplicate(source,name,source.matrix_world.copy())
    obj.location=pos
    obj.rotation_euler=(0,0,angle)
    obj.scale *= scale
    return obj

paper=next(o for o in sources if o.name=='SM_PROP_OpenNotes')
book=next(o for o in sources if o.name.startswith('SM_PROP_StackedTextbook'))
# Papers have no collision: fill the visible floor while keeping solid furniture off the route.
for i in range(105):
    x=rng.uniform(-1.55,1.55)
    y=rng.uniform(.40,4.48)
    if x>0 and y>3.15: y=rng.uniform(.5,2.7)
    scatter(paper,'SM_C1_ScatteredPaper_%03d'%i,(x,y,.025+rng.uniform(0,.018)),rng.uniform(-math.pi,math.pi),rng.uniform(.45,1.05))
for i in range(16):
    scatter(book,'SM_C1_FallenBook_%02d'%i,(rng.choice([-1,1])*rng.uniform(.65,1.6),rng.uniform(.65,2.65),.05),rng.uniform(-math.pi,math.pi),rng.uniform(.75,1.1))
# Duplicate the complete curved chair, rotate as a group about its seat pivot.
pivot=Vector((1.25,2.98,.45))
turn=Matrix.Rotation(math.radians(91),4,'Y')
for i,source in enumerate(o for o in sources if 'Chair' in o.name):
    matrix=Matrix.Translation(Vector((1.12,2.40,.28))) @ turn @ Matrix.Translation(-pivot) @ source.matrix_world
    duplicate(source,'SM_C1_OverturnedChair_%02d'%i,matrix)
for i,source in enumerate(o for o in sources if 'StudentBag' in o.name or 'BagStrap' in o.name):
    duplicate(source,'SM_C1_DumpedBag_%02d'%i,Matrix.Translation(Vector((-.55,1.25,0))) @ source.matrix_world)

for obj in bpy.context.scene.objects: obj.select_set(False)
for obj in created: obj.select_set(True)
bpy.context.view_layer.objects.active=created[0]
bpy.ops.object.convert(target='MESH')
out=ROOT/'Assets/_NoRainbowAfter/Art/Models/Environment/ENV_C1_RoomDisarray.fbx'
bpy.ops.export_scene.fbx(filepath=str(out),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
(ROOT/'BlenderSources/Environment/C1_RoomDisarray_manifest.json').write_text(json.dumps({'objects':[o.name for o in created],'paperCount':105,'bookCount':16,'source':'Original project meshes rearranged in Blender; no third-party assets.'},indent=2))
# Save an inspectable source with both the normal shell and horror dressing.
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSources/Environment/C1_RoomDisarray.blend'))
print('C1_ROOM_DISARRAY_EXPORTED',len(created))
