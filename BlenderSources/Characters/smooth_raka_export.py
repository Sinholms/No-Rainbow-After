"""Restore smooth shading after decimation without reducing topology again."""
import bpy
from pathlib import Path
from mathutils import Vector
R=Path('D:/GRAVKOM/No rainbow after');bpy.ops.wm.open_mainfile(filepath=str(R/'BlenderSources/Characters/ACT_Shadow_Raka.blend'))
s=bpy.context.scene;bpy.ops.object.select_all(action='DESELECT')
for o in s.objects:
 if o.type=='MESH':
  o.select_set(True)
  for p in o.data.polygons:p.use_smooth=True
rig=bpy.data.objects['ACT_Shadow_Raka'];rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(R/'Assets/_NoRainbowAfter/Art/Models/Characters/ACT_Shadow_Raka.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True)
s.camera.location=(3,-5,1.5);s.camera.rotation_euler=(Vector((0,0,1.18))-s.camera.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.wm.save_as_mainfile(filepath=str(R/'BlenderSources/Characters/ACT_Shadow_Raka.blend'),compress=True)
s.render.filepath=str(R/'Docs/QA/Raka/Optimized_ThreeQuarter.png');bpy.ops.render.render(write_still=True)
