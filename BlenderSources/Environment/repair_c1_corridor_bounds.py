"""Close corridor floor gaps and author three corner wardrobe bays without changing the route."""
import bpy,json
from pathlib import Path
ROOT=Path('D:/GRAVKOM/No rainbow after')
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'BlenderSources/Environment/HC3_Extended.blend'))
main=bpy.data.objects['SM_HC3_Corridor_NoStorage']
floor_mat=main.data.materials[4];wall_mat=main.data.materials[4];ceiling_mat=main.data.materials[21]
def box(name,loc,size,mat):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mat)
    uv=o.data.uv_layers.active or o.data.uv_layers.new()
    for p in o.data.polygons:
        axes=(0,1) if abs(p.normal.z)>.5 else (0,2) if abs(p.normal.y)>.5 else (1,2)
        for li in p.loop_indices:
            v=o.data.vertices[o.data.loops[li].vertex_index].co
            uv.data[li].uv=(v[axes[0]]*.5,v[axes[1]]*.5)
    return o
def wall(name,axis,value,start,end):
    if end-start<.02:return
    box(name,(value,(start+end)/2,1.35) if axis=='X' else ((start+end)/2,value,1.35),(.12,end-start,2.7) if axis=='X' else (end-start,.12,2.7),wall_mat)
for prefix in ['TurnA','TurnB','TurnC']:
    for o in list(bpy.context.scene.objects):
        if o.name.startswith(prefix+'_'):bpy.data.objects.remove(o,do_unlink=True)
# Expanded corner bays; apertures align with the existing two corridor approaches.
bays=[('A',31.1,35.1,-4.15,-.15,{'W':(-2.77,-.53),'N':(31.48,33.72)}),
      ('B',31.1,35.1,10.85,14.85,{'W':(11.23,13.47),'S':(31.48,33.72)}),
      ('C',16.1,20.1,10.85,14.85,{'E':(11.23,13.47),'S':(17.48,19.72)})]
for tag,x0,x1,y0,y1,openings in bays:
    box('HC3_Bay'+tag+'_Ceiling',((x0+x1)/2,(y0+y1)/2,2.78),(x1-x0,y1-y0,.16),ceiling_mat)
    for side,axis,value,a,b in [('W','X',x0,y0,y1),('E','X',x1,y0,y1),('S','Y',y0,x0,x1),('N','Y',y1,x0,x1)]:
        if side in openings:
            lo,hi=openings[side];wall('HC3_Bay'+tag+'_'+side+'0',axis,value,a,lo);wall('HC3_Bay'+tag+'_'+side+'1',axis,value,hi,b)
        else:wall('HC3_Bay'+tag+'_'+side,axis,value,a,b)
rects=[[-2.97,-.73,-2.8,11.18],[-2.97,33.72,-2.8,-.5],[31.48,33.72,-2.8,13.47],
       [17.48,33.72,11.23,13.47],[17.48,19.72,-2.8,13.47],[5.1,9.5,-4.9,3.1],
       [24.78,26.42,-.65,.82]]+[[b[1],b[2],b[3],b[4]] for b in bays]
xs=sorted({r[i] for r in rects for i in (0,1)});ys=sorted({r[i] for r in rects for i in (2,3)})
inside=lambda x,y:any(a<=x<=b and c<=y<=d for a,b,c,d in rects)
vertices=[];faces=[];edges=[]
for i in range(len(xs)-1):
 for j in range(len(ys)-1):
    x0,x1=xs[i:i+2];y0,y1=ys[j:j+2]
    if not inside((x0+x1)/2,(y0+y1)/2):continue
    k=len(vertices);vertices.extend([(x0,y0,-.035),(x1,y0,-.035),(x1,y1,-.035),(x0,y1,-.035)]);faces.append((k,k+1,k+2,k+3))
    for a,b,n in [((x0,y0),(x1,y0),(0,-1)),((x1,y0),(x1,y1),(1,0)),((x1,y1),(x0,y1),(0,1)),((x0,y1),(x0,y0),(-1,0))]:
        mx,my=(a[0]+b[0])/2,(a[1]+b[1])/2
        if not inside(mx+n[0]*.001,my+n[1]*.001):edges.append({'a':list(a),'b':list(b),'normal':list(n)})
mesh=bpy.data.meshes.new('C1_FloorBacking');mesh.from_pydata(vertices,[],faces);mesh.update()
obj=bpy.data.objects.new('HC3_ContinuousFloorBacking',mesh);bpy.context.scene.collection.objects.link(obj);mesh.materials.append(floor_mat)
uv=mesh.uv_layers.new()
for p in mesh.polygons:
 for li in p.loop_indices:
    v=mesh.vertices[mesh.loops[li].vertex_index].co;uv.data[li].uv=(v.x*.5,v.y*.5)
manifest={'rectangles':rects,'boundaryEdges':edges,'wardrobes':[{'id':'CornerA','position':[34,0,-3],'yaw':135},{'id':'CornerB','position':[34,0,13.7],'yaw':45},{'id':'CornerC','position':[17.2,0,13.7],'yaw':-45}]}
(ROOT/'BlenderSources/Environment/C1_CorridorRepair_manifest.json').write_text(json.dumps(manifest,indent=2))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/_NoRainbowAfter/Art/Models/Environment/ENV_HC3_NoStorage.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSources/Environment/HC3_Repaired.blend'))
print('CORRIDOR_REPAIRED',len(faces),'floor cells;',len(edges),'boundary edges; 3 wardrobe bays')
