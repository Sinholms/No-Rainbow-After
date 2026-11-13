"""Second reference pass: continuous anatomy, bowed posture and worn surfaces."""
import bpy, math, random, json
from pathlib import Path
from mathutils import Vector
R=Path('D:/GRAVKOM/No rainbow after');random.seed(42)
bpy.ops.wm.open_mainfile(filepath=str(R/'BlenderSources/Characters/ACT_Shadow_Raka.blend'))
sc=bpy.context.scene;rig=bpy.data.objects['ACT_Shadow_Raka'];skin=bpy.data.materials['Raka_Skin_Weathered']
parts=[o for o in sc.objects if o.type=='MESH']
# Weld intersecting anatomical sections into a single continuous surface.
skins=[o for o in parts if o.data.materials and o.data.materials[0]==skin]
bpy.ops.object.select_all(action='DESELECT')
for o in skins:
 o.select_set(True)
 for m in list(o.modifiers):o.modifiers.remove(m)
bpy.context.view_layer.objects.active=skins[0];bpy.ops.object.join();body=bpy.context.object;body.name='Raka_ContinuousAnatomy'
rem=body.modifiers.new('Anatomical union','REMESH');rem.mode='VOXEL';rem.voxel_size=.0035;bpy.ops.object.modifier_apply(modifier=rem.name)
sm=body.modifiers.new('Organic transition','SMOOTH');sm.factor=1.2;sm.iterations=5;bpy.ops.object.modifier_apply(modifier=sm.name)
dec=body.modifiers.new('Surface budget','DECIMATE');dec.ratio=.42;bpy.ops.object.modifier_apply(modifier=dec.name)
# Preserve the five fingers while reducing round toy-like joints.
for v in body.data.vertices:
 p=v.co
 if p.z>2.04:p.y-=.05*(p.z-2.04)/.30
 if p.z>1.95:p.z-=.035*(p.z-1.95)/.4
for o in [o for o in sc.objects if o.type=='MESH' and o!=body]:
 for v in o.data.vertices:
  p=o.matrix_world@v.co
  if p.z>2.04:
   p.y-=.05*(p.z-2.04)/.30;p.z-=.035*(p.z-1.95)/.4;v.co=o.matrix_world.inverted()@p
# Material weathering at multiple scales, tendon-like longitudinal surface relief.
for name in ['Raka_Skin_Weathered','Raka_Leather','Raka_Shirt_AgedLinen','Raka_Paper','Raka_PleatedCloth']:
 m=bpy.data.materials[name];n=m.node_tree.nodes;l=m.node_tree.links;p=n.get('Principled BSDF');r=next(x for x in n if x.type=='VALTORGB')
 if name=='Raka_Skin_Weathered':colors=[(.027,.012,.007,1),(.115,.062,.040,1)]
 elif name=='Raka_Shirt_AgedLinen':colors=[(.035,.025,.014,1),(.23,.17,.105,1)]
 elif name=='Raka_Paper':colors=[(.12,.075,.035,1),(.40,.29,.16,1)]
 elif name=='Raka_Leather':colors=[(.008,.004,.002,1),(.055,.025,.009,1)]
 else:colors=[(.009,.007,.004,1),(.065,.038,.020,1)]
 r.color_ramp.elements[0].color=colors[0];r.color_ramp.elements[1].color=colors[1]
 coord=n.new('ShaderNodeTexCoord');mapping=n.new('ShaderNodeVectorMath');mapping.operation='MULTIPLY';mapping.inputs[1].default_value=(12,12,1.2);l.new(coord.outputs['Generated'],mapping.inputs[0])
 grain=n.new('ShaderNodeTexNoise');grain.inputs['Scale'].default_value=12;grain.inputs['Detail'].default_value=5;l.new(mapping.outputs[0],grain.inputs['Vector']);l.new(grain.outputs['Fac'],r.inputs[0])
 bump=next(x for x in n if x.type=='BUMP');bump.inputs['Strength'].default_value=.42;bump.inputs['Distance'].default_value=.008;l.new(grain.outputs['Fac'],bump.inputs['Height'])
# Rebind unified anatomy with smoothly blended nearest-segment weights.
body.vertex_groups.clear();bones=[b for b in rig.data.bones if b.name!='Root'];groups={b.name:body.vertex_groups.new(name=b.name) for b in bones}
def distance(p,a,b):
 d=b-a;t=max(0,min(1,(p-a).dot(d)/d.length_squared));return (p-a-d*t).length
for v in body.data.vertices:
 p=body.matrix_world@v.co;ds=sorted([(distance(p,b.head_local,b.tail_local),b.name) for b in bones])[:2]
 weights=[1/(d+.015)**4 for d,n in ds];total=sum(weights)
 for (_,name),w in zip(ds,weights):groups[name].add([v.index],w/total,'REPLACE')
mod=body.modifiers.new('Generic skin','ARMATURE');mod.object=rig
bpy.context.view_layer.objects.active=body;bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.015);bpy.ops.object.mode_set(mode='OBJECT')
# Fill crown with overlapping hair surfaces following the skull.
hair=bpy.data.materials['Raka_Hair_Wet']
for j in range(48):
 a=j*math.tau/48;vs=[]
 for k in range(9):
  t=k/8;rad=.022+.077*math.sin(t*1.7);z=2.32-t*.20
  for da in [-.045,.045]:vs.append((math.cos(a+da)*rad,-.14+math.sin(a+da)*(.04+.075*t),z))
 me=bpy.data.meshes.new('Crown hair');me.from_pydata(vs,[],[(k*2,k*2+1,k*2+3,k*2+2) for k in range(8)]);me.materials.append(hair)
 o=bpy.data.objects.new('Crown hair',me);sc.collection.objects.link(o);o.parent=rig;g=o.vertex_groups.new(name='Head');g.add(list(range(len(vs))),1,'REPLACE');m=o.modifiers.new('Skin','ARMATURE');m.object=rig
# Join compatible pieces: retain weights, reduce the hundreds of draw calls.
for ma in [m for m in bpy.data.materials if m.name.startswith('Raka_')]:
 obs=[o for o in sc.objects if o.type=='MESH' and o!=body and len(o.data.materials)==1 and o.data.materials[0]==ma]
 if not obs:continue
 bpy.ops.object.select_all(action='DESELECT')
 for o in obs:
  bpy.context.view_layer.objects.active=o;o.select_set(True)
  for m in list(o.modifiers):
   if m.type=='SOLIDIFY':bpy.ops.object.modifier_apply(modifier=m.name)
 bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.join();obs[0].name=ma.name.replace('Raka_','Raka_Mesh_')
sc['production_status']='Second mesh pass, 8 in-place animation studies; procedural surfaces require texture bake; not integrated in gameplay'
sc.cycles.samples=32;out=R/'Docs/QA/Raka'
for name,loc in [('Front',(0,-5,1.2)),('ThreeQuarter',(3,-5,1.5)),('Back',(0,5,1.2)),('Side',(5,0,1.2))]:
 sc.camera.location=loc;sc.camera.rotation_euler=(Vector((0,0,1.18))-sc.camera.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in sc.objects:
 if o.type=='MESH':o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(R/'Assets/_NoRainbowAfter/Art/Models/Characters/ACT_Shadow_Raka.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True)
bpy.ops.wm.save_as_mainfile(filepath=str(R/'BlenderSources/Characters/ACT_Shadow_Raka.blend'),compress=True)
for o in sc.objects:
 if o.type=='MESH':o.data.calc_loop_triangles()
(out/'manifest.json').write_text(json.dumps({'meshes':len([o for o in sc.objects if o.type=='MESH']),'triangles':sum(len(o.data.loop_triangles) for o in sc.objects if o.type=='MESH'),'bones':len(rig.data.bones),'clips':[a.name for a in bpy.data.actions],'status':sc['production_status']},indent=2))
