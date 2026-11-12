"""Private assignment adaptation. Provenance uncertainty is recorded in Docs/ASSET_LICENSES.md."""
import bpy,bmesh,math,json,shutil
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path('D:/GRAVKOM/No rainbow after')
bpy.ops.wm.read_factory_settings(use_empty=True)
source=next((ROOT/'ThirdPartySources/HorrorCorridor3/Original/source').glob('*.usdz'))
bpy.ops.wm.usd_import(filepath=str(source))
bpy.data.objects['model'].scale=(.00032,)*3
bpy.context.view_layer.update()
front=bpy.data.objects['FrontSide']
matrix=front.matrix_world.copy()
front.parent=None
front.data.transform(matrix)
front.matrix_world=Matrix.Identity(4)
bm=bmesh.new();bm.from_mesh(front.data)
# Split every boundary before removing the storage room, including its floor and furniture.
for axis,value in [(0,-.55),(0,2.3),(1,-.55)]:
    normal=Vector((1,0,0) if axis==0 else (0,1,0))
    co=Vector((value,0,0) if axis==0 else (0,value,0))
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.0001,plane_co=co,plane_no=normal)
faces=[f for f in bm.faces if -.55<f.calc_center_median().x<2.3 and f.calc_center_median().y>-.55]
bmesh.ops.delete(bm,geom=faces,context='FACES')
bm.to_mesh(front.data);bm.free()
front.name='SM_HC3_Corridor_NoStorage'
# Widen horizontal passages while keeping plausible ceiling height.
for v in front.data.vertices: v.co.x*=1.5;v.co.y*=1.5;v.co.z+=.0835
for o in list(bpy.context.scene.objects):
    if o != front: bpy.data.objects.remove(o,do_unlink=True)
texdir=ROOT/'Assets/_NoRainbowAfter/Art/Textures/HorrorCorridor3'
texdir.mkdir(parents=True,exist_ok=True)
materials=[]
for mat in front.data.materials:
    if mat is None: continue
    entry={'name':mat.name,'texture':''}
    if mat.use_nodes:
        for node in mat.node_tree.nodes:
            if node.type=='TEX_IMAGE' and node.image:
                img=node.image
                target=texdir/(img.name.replace('/','_').replace('\\','_')+'.png')
                img.filepath_raw=str(target);img.file_format='PNG';img.save()
                entry['texture']=str(target.relative_to(ROOT)).replace('\\','/')
                break
    materials.append(entry)
front.select_set(True);bpy.context.view_layer.objects.active=front
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/_NoRainbowAfter/Art/Models/Environment/ENV_HC3_NoStorage.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
(ROOT/'BlenderSources/Environment/HC3_manifest.json').write_text(json.dumps({'materials':materials,'removed':'Storage/library room: x[-.55,2.3], y>-.55 before horizontal widening','horizontalScale':1.5},indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSources/Environment/HC3_NoStorage.blend'))
print('HC3_READY',len(front.data.polygons),'polygons',len(materials),'materials')
