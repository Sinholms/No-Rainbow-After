"""Replace the branching imported hub with textured straight sections and seal the loop."""
import bpy, bmesh, json
from pathlib import Path
ROOT=Path('D:/GRAVKOM/No rainbow after')
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'BlenderSources/Environment/HC3_Repaired.blend'))
main=bpy.data.objects['SM_HC3_Corridor_NoStorage']
wall_mat=main.data.materials[4]; ceiling_mat=main.data.materials[21]
kit=bpy.data.objects['HC3_East_00'].data.copy()
bpy.data.objects.remove(main,do_unlink=True)
bpy.data.objects.remove(bpy.data.objects['HC3_ContinuousFloorBacking'],do_unlink=True)
def box(name,loc,size,mat=wall_mat):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=bpy.context.object;o.name=name;o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(mat)
    uv=o.data.uv_layers.active or o.data.uv_layers.new()
    for p in o.data.polygons:
        axes=(0,1) if abs(p.normal.z)>.5 else (0,2) if abs(p.normal.y)>.5 else (1,2)
        for li in p.loop_indices:
            v=o.data.vertices[o.data.loops[li].vertex_index].co
            uv.data[li].uv=(v[axes[0]]*.5,v[axes[1]]*.5)
    return o
for i,x in enumerate([1.6,5.6,9.6,13.6,17.6]):
    o=bpy.data.objects.new('HC3_LinearApproach_%02d'%i,kit.copy())
    bpy.context.scene.collection.objects.link(o);o.location=(x,-1.65,0)
o=bpy.data.objects.new('HC3_Entry_Lower',kit.copy())
bpy.context.scene.collection.objects.link(o);o.location=(-1.85,1.1,0)
o.rotation_euler.z=1.5707963267948966
# A single entrance elbow, without the old imported side rooms.
box('HC3_EntranceElbow_Floor',(-1.715,-1.65,-.08),(2.63,2.36,.16))
box('HC3_EntranceElbow_Ceiling',(-1.715,-1.65,2.78),(2.63,2.36,.16),ceiling_mat)
box('HC3_EntranceElbow_West',(-3.03,-1.65,1.35),(.12,2.48,2.7))
box('HC3_EntranceElbow_South',(-1.715,-2.83,1.35),(2.75,.12,2.7))
box('HC3_EntranceElbow_North',(-.565,-.47,1.35),(.33,.12,2.7))
# The returning south leg ends here instead of creating a second route to the east.
south=bpy.data.objects['HC3_South_02']
bm=bmesh.new();bm.from_mesh(south.data)
# local X maps to world Y in Blender for this rotated straight segment.
bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,
                      plane_co=(-.65,0,0),plane_no=(1,0,0))
bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.calc_center_median().x<-.65],context='FACES')
bm.to_mesh(south.data);bm.free()
box('HC3_ReturnEnd_FullWall',(18.6,.7,1.35),(2.36,.18,2.7))
# Replace the large corner rooms with ordinary corridor-width elbows.
for o in list(bpy.context.scene.objects):
    if o.name.startswith(('HC3_BayA_','HC3_BayB_','HC3_BayC_')):
        bpy.data.objects.remove(o,do_unlink=True)
for tag,x,y,sides in [('A',32.6,-1.65,'SE'),('B',32.6,12.35,'NE'),('C',18.6,12.35,'NW')]:
    box('HC3_Elbow'+tag+'_Ceiling',(x,y,2.78),(2.36,2.36,.16),ceiling_mat)
    for side in sides:
        dx,dy={'S':(0,-1.12),'N':(0,1.12),'E':(1.12,0),'W':(-1.12,0)}[side]
        box('HC3_Elbow'+tag+'_'+side,(x+dx,y+dy,1.35),(.12,2.36,2.7) if dx else (2.36,.12,2.7))

# Three narrow side fittings at the user's marked positions, matching Wardrobe_East.
pockets=[('A',10.18,11.82,-.65,.82,'S'),
         ('B',33.35,35.07,4.38,6.02,'W'),
         ('C',24.68,26.32,9.88,11.35,'N')]
for tag,x0,x1,y0,y1,opening in pockets:
    for o in list(bpy.context.scene.objects):
        if o.type!='MESH':continue
        world=o.matrix_world.copy()
        points=[world@v.co for v in o.data.vertices]
        if not points or max(v.x for v in points)<x0 or min(v.x for v in points)>x1 or max(v.y for v in points)<y0 or min(v.y for v in points)>y1:continue
        bm=bmesh.new();bm.from_mesh(o.data)
        for v in bm.verts:v.co=world@v.co
        for axis,value in [(0,x0),(0,x1),(1,y0),(1,y1),(2,2.4)]:
            normal=[0,0,0];normal[axis]=1;co=[0,0,0];co[axis]=value
            bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=co,plane_no=normal)
        doomed=[]
        for f in bm.faces:
            c=f.calc_center_median()
            if x0+.0001<c.x<x1-.0001 and y0+.0001<c.y<y1-.0001 and c.z<2.4:doomed.append(f)
        bmesh.ops.delete(bm,geom=doomed,context='FACES')
        inv=world.inverted()
        for v in bm.verts:v.co=inv@v.co
        bm.to_mesh(o.data);bm.free()
    box('HC3_SideWardrobe'+tag+'_Ceiling',((x0+x1)/2,(y0+y1)/2,2.78),(x1-x0,y1-y0,.16),ceiling_mat)
    for side in 'NSEW':
        if side==opening:continue
        if side in 'NS':box('HC3_SideWardrobe'+tag+'_'+side,((x0+x1)/2,y1 if side=='N' else y0,1.35),(x1-x0,.1,2.7))
        else:box('HC3_SideWardrobe'+tag+'_'+side,(x1 if side=='E' else x0,(y0+y1)/2,1.35),(.1,y1-y0,2.7))
# Solid backing under only the retained walkway; no navigable side-room floor.
rects=[[-3.03,-.73,-2.83,11.18],[-3.03,33.72,-2.83,-.5],
       [31.48,33.72,-2.8,13.47],[17.48,33.72,11.23,13.47],
       [17.48,19.72,.7,13.47],[24.78,26.42,-.65,.82],
       ]+[[p[1],p[2],p[3],p[4]] for p in pockets]
xs=sorted({r[i] for r in rects for i in (0,1)});ys=sorted({r[i] for r in rects for i in (2,3)})
inside=lambda x,y:any(a<=x<=b and c<=y<=d for a,b,c,d in rects)
verts=[];faces=[];edges=[]
for x0,x1 in zip(xs,xs[1:]):
 for y0,y1 in zip(ys,ys[1:]):
    if not inside((x0+x1)/2,(y0+y1)/2):continue
    k=len(verts);verts.extend([(x0,y0,-.035),(x1,y0,-.035),(x1,y1,-.035),(x0,y1,-.035)]);faces.append((k,k+1,k+2,k+3))
    for a,b,n in [((x0,y0),(x1,y0),(0,-1)),((x1,y0),(x1,y1),(1,0)),((x1,y1),(x0,y1),(0,1)),((x0,y1),(x0,y0),(-1,0))]:
        if not inside((a[0]+b[0])/2+n[0]*.001,(a[1]+b[1])/2+n[1]*.001):edges.append({'a':list(a),'b':list(b),'normal':list(n)})
mesh=bpy.data.meshes.new('C1_LinearFloorBacking');mesh.from_pydata(verts,[],faces);mesh.update();mesh.materials.append(wall_mat)
o=bpy.data.objects.new('HC3_ContinuousFloorBacking',mesh);bpy.context.scene.collection.objects.link(o)
uv=mesh.uv_layers.new()
for p in mesh.polygons:
 for li in p.loop_indices:
    v=mesh.vertices[mesh.loops[li].vertex_index].co;uv.data[li].uv=(v.x*.5,v.y*.5)
manifest=json.loads((ROOT/'BlenderSources/Environment/C1_CorridorRepair_manifest.json').read_text())
manifest.update(rectangles=rects,boundaryEdges=edges,layout='Single winding route; side rooms removed; return connection sealed by full-height textured wall')
manifest['wardrobes']=[{'id':'CornerA','position':[11,0,.12],'yaw':0},
                       {'id':'CornerB','position':[34.37,0,5.2],'yaw':90},
                       {'id':'CornerC','position':[25.5,0,10.58],'yaw':180}]
(ROOT/'BlenderSources/Environment/C1_CorridorRepair_manifest.json').write_text(json.dumps(manifest,indent=2))
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/_NoRainbowAfter/Art/Models/Environment/ENV_HC3_NoStorage.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSources/Environment/HC3_Linear.blend'))
print('LINEAR_CORRIDOR_READY')
