"""Reduce continuous body topology while retaining the baked UVs and rig."""
import bpy,json
from pathlib import Path
R=Path('D:/GRAVKOM/No rainbow after')
bpy.ops.wm.open_mainfile(filepath=str(R/'BlenderSources/Characters/ACT_Shadow_Raka.blend'))
sc=bpy.context.scene;body=bpy.data.objects['Raka_ContinuousAnatomy'];rig=bpy.data.objects['ACT_Shadow_Raka']
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);bpy.context.view_layer.objects.active=body
mod=body.modifiers.new('LOD0 budget','DECIMATE');mod.ratio=.12;bpy.context.view_layer.objects.active=body;bpy.ops.object.modifier_apply(modifier=mod.name)
obs=[o for o in sc.objects if o.type=='MESH']
for o in obs:
 o.data.calc_loop_triangles()
 for p in o.data.polygons:p.use_smooth=True
qa=R/'Docs/QA/Raka';data=json.loads((qa/'manifest.json').read_text());data['triangles']=sum(len(o.data.loop_triangles) for o in obs);data['weightedVertices']=sum(len(o.data.vertices) for o in obs);data['unweightedVertices']=sum(1 for o in obs for v in o.data.vertices if not v.groups);data['optimization']='Continuous body decimated to 12 percent after bake; UV and skin groups retained';(qa/'manifest.json').write_text(json.dumps(data,indent=2))
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in obs:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(R/'Assets/_NoRainbowAfter/Art/Models/Characters/ACT_Shadow_Raka.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True)
# Leave a useful material viewport and camera-framed view for the user.
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':
  area.spaces.active.region_3d.view_location=(0,0,1.18);area.spaces.active.region_3d.view_distance=3.1;area.spaces.active.shading.type='MATERIAL'
bpy.ops.wm.save_as_mainfile(filepath=str(R/'BlenderSources/Characters/ACT_Shadow_Raka.blend'),compress=True)
from mathutils import Vector
sc.camera.location=(3,-5,1.5);sc.camera.rotation_euler=(Vector((0,0,1.18))-sc.camera.location).to_track_quat('-Z','Y').to_euler()
sc.render.filepath=str(qa/'Optimized_ThreeQuarter.png');bpy.ops.render.render(write_still=True)
print(json.dumps(data))
