"""Final generation pass: bowed silhouette and portable 2K baked surfaces."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix
R=Path('D:/GRAVKOM/No rainbow after');bpy.ops.wm.open_mainfile(filepath=str(R/'BlenderSources/Characters/ACT_Shadow_Raka.blend'))
sc=bpy.context.scene;rig=bpy.data.objects['ACT_Shadow_Raka'];obs=[o for o in sc.objects if o.type=='MESH'];out=R/'Assets/_NoRainbowAfter/Art/Textures/Raka';out.mkdir(parents=True,exist_ok=True)
# Bend the neck and head forward with a continuous transition rather than a straight cylinder.
for o in obs:
 for v in o.data.vertices:
  p=o.matrix_world@v.co
  if p.z>1.91:
   fac=min(1,(p.z-1.91)/.18);pivot=Vector((0,0,1.98));rot=Matrix.Rotation(math.radians(24)*fac,3,'X');p=pivot+rot@(p-pivot);p.y-=.035*fac;v.co=o.matrix_world.inverted()@p
# Organic, mottled skin; damp grain is fine-scale instead of wood-like longitudinal bands.
m=bpy.data.materials['Raka_Skin_Weathered'];n=m.node_tree.nodes;l=m.node_tree.links
mapping=next(x for x in n if x.type=='VECT_MATH');mapping.inputs[1].default_value=(2.5,2.5,2.5)
bump=next(x for x in n if x.type=='BUMP');bump.inputs['Distance'].default_value=.0015;bump.inputs['Strength'].default_value=.32
# Explicitly re-unwrap each merged material mesh before baking; earlier islands overlap after joins.
sc.render.engine='CYCLES';sc.cycles.samples=8;sc.render.bake.margin=12;sc.render.bake.use_clear=True
for o in obs:
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.008);bpy.ops.object.mode_set(mode='OBJECT')
 ma=o.data.materials[0];nodes=ma.node_tree.nodes;links=ma.node_tree.links;p=nodes.get('Principled BSDF');targets={}
 for kind in ['BaseColor','Normal','Roughness']:
  img=bpy.data.images.new(ma.name+'_'+kind,width=2048,height=2048,alpha=False);img.colorspace_settings.name='sRGB' if kind=='BaseColor' else 'Non-Color'
  node=nodes.new('ShaderNodeTexImage');node.image=img;nodes.active=node
  if kind=='BaseColor':
   sc.render.bake.use_pass_direct=False;sc.render.bake.use_pass_indirect=False;sc.render.bake.use_pass_color=True;bpy.ops.object.bake(type='DIFFUSE')
  elif kind=='Normal':bpy.ops.object.bake(type='NORMAL')
  else:bpy.ops.object.bake(type='ROUGHNESS')
  img.filepath_raw=str(out/(ma.name+'_'+kind+'.png'));img.file_format='PNG';img.save();targets[kind]=node
 links.new(targets['BaseColor'].outputs['Color'],p.inputs['Base Color'])
 nm=nodes.new('ShaderNodeNormalMap');links.new(targets['Normal'].outputs['Color'],nm.inputs['Color']);links.new(nm.outputs[0],p.inputs['Normal']);links.new(targets['Roughness'].outputs['Color'],p.inputs['Roughness'])
 print('BAKED',o.name,flush=True)
sc['production_status']='Generated reference-directed model, 2K baked BaseColor/Normal/Roughness, Generic rig, 8 basic in-place motion studies; gameplay swap pending'
sc.cycles.samples=32;qa=R/'Docs/QA/Raka'
for name,loc in [('Front',(0,-5,1.2)),('ThreeQuarter',(3,-5,1.5)),('Back',(0,5,1.2)),('Side',(5,0,1.2))]:
 sc.camera.location=loc;sc.camera.rotation_euler=(Vector((0,0,1.18))-sc.camera.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(qa/(name+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in obs:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(R/'Assets/_NoRainbowAfter/Art/Models/Characters/ACT_Shadow_Raka.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True)
bpy.ops.wm.save_as_mainfile(filepath=str(R/'BlenderSources/Characters/ACT_Shadow_Raka.blend'),compress=True)
for o in obs:o.data.calc_loop_triangles()
(qa/'manifest.json').write_text(json.dumps({'meshes':len(obs),'triangles':sum(len(o.data.loop_triangles) for o in obs),'bones':len(rig.data.bones),'clips':[a.name for a in bpy.data.actions],'textureResolution':2048,'maps':['BaseColor','Normal','Roughness'],'status':sc['production_status']},indent=2))
