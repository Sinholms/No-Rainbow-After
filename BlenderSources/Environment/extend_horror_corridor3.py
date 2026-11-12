"""Extend the adapted corridor with a continuous return route using its own textured sections."""
import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Matrix,Vector
ROOT=Path('D:/GRAVKOM/No rainbow after')
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'BlenderSources/Environment/HC3_NoStorage.blend'))
main=next(o for o in bpy.context.scene.objects if o.type=='MESH')
# Imported interior floor is at 1.69m; normalize to ground and raise its low ceiling.
for v in main.data.vertices:v.co.z=(v.co.z-1.69)*1.87
def cut(mesh,axis,value,remove):
    bm=bmesh.new();bm.from_mesh(mesh)
    normal=Vector((1,0,0) if axis==0 else (0,1,0))
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=normal*value,plane_no=normal)
    bmesh.ops.delete(bm,geom=[f for f in bm.faces if remove(f.calc_center_median()[axis])],context='FACES')
    bm.to_mesh(mesh);bm.free()
kit=main.data.copy()
cut(kit,0,12,lambda x:x<12);cut(kit,0,16,lambda x:x>16)
kit.transform(Matrix.Translation((-14,1.65,0)))
cut(main.data,0,19.6,lambda x:x>19.6)
cut(main.data,1,3.1,lambda y:y>3.1)
# Open a connection at the return side of the new loop.
bm=bmesh.new();bm.from_mesh(main.data)
for axis,value in [(0,17.55),(0,19.6),(1,-.72)]:
    n=Vector((1,0,0) if axis==0 else (0,1,0))
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=n*value,plane_no=n)
bmesh.ops.delete(bm,geom=[f for f in bm.faces if 17.55<f.calc_center_median().x<19.6 and f.calc_center_median().y>-.72],context='FACES')
bm.to_mesh(main.data);bm.free()
pieces=[]
def section(name,x,y,angle):
    o=bpy.data.objects.new(name,kit.copy());bpy.context.scene.collection.objects.link(o)
    o.location=(x,y,0);o.rotation_euler.z=math.radians(angle);pieces.append(o)
for i,x in enumerate([21.6,25.6,29.6]):section('HC3_East_%02d'%i,x,-1.65,0)
for i,y in enumerate([5.1,9.1]):section('HC3_Entry_%02d'%i,-1.85,y,90)
for i,y in enumerate([1.35,5.35,9.35]):section('HC3_North_%02d'%i,32.6,y,90)
for i,x in enumerate([29.6,25.6,21.6]):section('HC3_Return_%02d'%i,x,12.35,180)
for i,y in enumerate([9.35,5.35,1.35]):section('HC3_South_%02d'%i,18.6,y,90)
def box(name,loc,size,mat):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(main.data.materials[mat])
    # Explicit box UVs for consistent repeat scale, rather than stretched solid-color blocks.
    uv=o.data.uv_layers.active or o.data.uv_layers.new()
    for p in o.data.polygons:
        n=p.normal;axes=(0,1) if abs(n.z)>.5 else (0,2) if abs(n.y)>.5 else (1,2)
        for li in p.loop_indices:
            c=o.data.vertices[o.data.loops[li].vertex_index].co
            uv.data[li].uv=(c[axes[0]]*.5,c[axes[1]]*.5)
    pieces.append(o)
for name,x,y,walls in [('TurnA',32.6,-1.65,['S','E']),('TurnB',32.6,12.35,['N','E']),('TurnC',18.6,12.35,['N','W'])]:
    box(name+'_Floor',(x,y,-.08),(2.12,2.12,.16),4)
    box(name+'_Ceiling',(x,y,2.78),(2.12,2.12,.16),21)
    for wall in walls:
        dx,dy=(1.08,0) if wall=='E' else (-1.08,0) if wall=='W' else (0,1.08) if wall=='N' else (0,-1.08)
        box(name+'_'+wall,(x+dx,y+dy,1.35),(.12,2.2,2.7) if dx else (2.2,.12,2.7),4)
# Seal the removed storage room beside the original left arm.
box('HC3_StorageBoundary',(-.86,1.1,1.35),(.10,4,2.7),4)
box('HC3_EntryEnd',(-1.85,11.18,1.35),(2.25,.12,2.7),4)
# Recess a wardrobe into the east stretch, keeping the main walkway open.
east=next(o for o in pieces if o.name=='HC3_East_01')
bm=bmesh.new();bm.from_mesh(east.data)
for axis,value in [(0,-.82),(0,.82),(1,.80)]:
    n=Vector((1,0,0) if axis==0 else (0,1,0))
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=n*value,plane_no=n)
bmesh.ops.delete(bm,geom=[f for f in bm.faces if -.82<f.calc_center_median().x<.82 and f.calc_center_median().y>.8 and f.calc_center_median().z<2.4],context='FACES')
bm.to_mesh(east.data);bm.free()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/_NoRainbowAfter/Art/Models/Environment/ENV_HC3_NoStorage.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSources/Environment/HC3_Extended.blend'))
print('EXTENDED_CORRIDOR_READY',len(pieces),'new sections, approximately 85m total route')
