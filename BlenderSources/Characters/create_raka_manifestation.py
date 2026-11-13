"""Reference-directed Raka asset. Run in Blender; never alters the T06 source."""
import bpy, math, random, json
from pathlib import Path
from mathutils import Vector
R=Path('D:/GRAVKOM/No rainbow after')
random.seed(13)
scene=bpy.data.scenes.new('NRA_Raka_ReferencePass')
bpy.context.window.scene=scene
scene.unit_settings.system='METRIC'
parts=[]
def mat(name,color,rough=.6,metal=0):
 m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
 n=m.node_tree.nodes; l=m.node_tree.links; p=n.get('Principled BSDF')
 p.inputs['Base Color'].default_value=(*color,1); p.inputs['Roughness'].default_value=rough; p.inputs['Metallic'].default_value=metal
 noise=n.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value=85; noise.inputs['Detail'].default_value=4
 bump=n.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=.23; bump.inputs['Distance'].default_value=.009
 l.new(noise.outputs['Fac'],bump.inputs['Height']); l.new(bump.outputs['Normal'],p.inputs['Normal'])
 ramp=n.new('ShaderNodeValToRGB'); ramp.color_ramp.elements[0].color=(*(v*.42 for v in color),1); ramp.color_ramp.elements[1].color=(*(min(v*1.5,1) for v in color),1)
 l.new(noise.outputs['Fac'],ramp.inputs[0]); l.new(ramp.outputs[0],p.inputs['Base Color'])
 return m
skin=mat('Raka_Skin_Weathered',(.19,.115,.087),.49)
cloth=mat('Raka_Shirt_AgedLinen',(.38,.31,.23),.89)
hair=mat('Raka_Hair_Wet',(.016,.012,.009),.3)
leather=mat('Raka_Leather',(.065,.036,.021),.62)
metal=mat('Raka_Buckles',(.25,.22,.16),.38,.7)
paper=mat('Raka_Paper',(.56,.47,.33),.92)
red=mat('Raka_FadedRed',(.28,.035,.022),.88)
ink=mat('Raka_Ink',(.10,.077,.055),.9)
skirt=mat('Raka_PleatedCloth',(.075,.060,.045),.95)
def mesh(name,vs,fs,ma,bone='Spine'):
 me=bpy.data.meshes.new(name); me.from_pydata(vs,[],fs); me.update(); o=bpy.data.objects.new(name,me); scene.collection.objects.link(o); o.data.materials.append(ma)
 for p in me.polygons:p.use_smooth=True
 o['bind_bone']=bone; parts.append(o); return o
def tube(name,points,radii,ma,bone='Spine',sides=12):
 vs=[];fs=[]
 for i,pt in enumerate(points):
  tangent=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(i-1,0)])
  tangent.normalize(); a=tangent.cross(Vector((0,1,0)))
  if a.length<.01:a=tangent.cross(Vector((1,0,0)))
  a.normalize();b=tangent.cross(a).normalized()
  rx,ry=(radii[i],radii[i]) if isinstance(radii[i],(float,int)) else radii[i]
  for j in range(sides):
   t=j*2*math.pi/sides;vs.append(Vector(pt)+a*rx*math.cos(t)+b*ry*math.sin(t))
 for i in range(len(points)-1):
  for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;fs.append((a,b,b+sides,a+sides))
 fs.extend([tuple(reversed(range(sides))),tuple((len(points)-1)*sides+j for j in range(sides))])
 return mesh(name,vs,fs,ma,bone)
def ribbon(name,points,widths,ma,bone='Spine',axis=(1,0,0)):
 vs=[];fs=[]
 for p,w in zip(points,widths):vs.extend([Vector(p)-Vector(axis)*w/2,Vector(p)+Vector(axis)*w/2])
 for i in range(len(points)-1):fs.append((i*2,i*2+1,i*2+3,i*2+2))
 o=mesh(name,vs,fs,ma,bone);mod=o.modifiers.new('Woven thickness','SOLIDIFY');mod.thickness=.002;return o
def ell(name,loc,scale,ma,bone='Spine'):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,location=loc);o=bpy.context.object;o.name=name;o.scale=scale
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(ma);o['bind_bone']=bone
 for p in o.data.polygons:p.use_smooth=True
 parts.append(o);return o
def belt(name,z,rx,ry,ma=leather,cx=0,cy=0,bone='Pelvis',width=.035):
 vs=[];fs=[]
 for j in range(65):
  t=j*math.tau/64
  for dz in [-width/2,width/2]:vs.append((cx+rx*math.cos(t),cy+ry*math.sin(t),z+dz))
 for j in range(64):fs.append((j*2,j*2+1,j*2+3,j*2+2))
 return mesh(name,vs,fs,ma,bone)
def buckle(name,x,y,z,bone='Pelvis',w=.055,h=.045):
 pts=[(x-w/2,y,z-h/2),(x+w/2,y,z-h/2),(x+w/2,y,z+h/2),(x-w/2,y,z+h/2),(x-w/2,y,z-h/2)]
 tube(name,pts,[.004]*5,metal,bone,6);tube(name+' pin',[(x,y-.002,z-h/2),(x,y-.002,z+h/2)],[.002]*2,metal,bone,6)
# Gaunt torso, sloped shoulders, bowed neck and face.
tube('Torso',[(0,0,1.16),(0,0,1.31),(0,0,1.47),(0,-.005,1.64),(0,0,1.79),(0,.01,1.91)],[(.13,.085),(.105,.075),(.12,.075),(.17,.105),(.205,.095),(.11,.07)],skin,sides=32)
ell('Pelvis',(0,0,1.18),(.15,.09,.17),skin,'Pelvis')
tube('Neck',[(0,.015,1.86),(0,-.025,2.02),(0,-.10,2.13)],[.065,.058,.052],skin,'Neck',20)
ell('Head',(0,-.12,2.19),(.095,.105,.145),skin,'Head')
ell('Jaw',(0,-.171,2.105),(.064,.062,.065),skin,'Head')
ell('Nose',(0,-.226,2.16),(.018,.032,.034),skin,'Head')
tube('Mouth',[(-.033,-.223,2.103),(0,-.235,2.095),(.031,-.223,2.101)],[.003,.003,.002],ink,'Head',6)
# Five paired rib contours and tendons; restrained intact anatomy.
for s in [-1,1]:
 for j in range(5):
  z=1.72-j*.048
  tube('Rib',[(s*.018,-.105,z),(s*.07,-.112,z-.018),(s*(.15-j*.006),-.063,z-.038)],[.008,.009,.004],skin,sides=8)
 tube('Clavicle',[(s*.02,-.09,1.87),(s*.12,-.09,1.87),(s*.22,-.01,1.88)],[.012,.014,.008],skin,sides=10)
 # Legs and articulated bare feet.
 side='L' if s>0 else 'R';x=s*.105
 tube('Thigh.'+side,[(x,0,1.23),(x*1.1,0,1.07),(x*1.05,-.02,.90),(x,-.045,.76)],[.078,.068,.052,.043],skin,'Thigh.'+side,20)
 ell('Knee.'+side,(x,-.065,.76),(.043,.033,.048),skin,'Shin.'+side)
 tube('Shin.'+side,[(x,-.025,.76),(x,.005,.63),(x,.017,.47),(x,-.005,.20),(x,-.015,.10)],[.044,.049,.038,.026,.032],skin,'Shin.'+side,20)
 ell('Foot.'+side,(x,-.065,.065),(.052,.105,.053),skin,'Foot.'+side)
 for j in range(5):
  xx=x+(j-2)*.019
  ell('Toe.'+side,(xx,-.155+(j*.006),.035),(.0105,.039-j*.003,.023),skin,'Foot.'+side)
 # Wide dangling arms, elbows, palms and five individual fingers.
 sh=(s*.215,0,1.88);el=(s*.30,-.008,1.39);wr=(s*.395,-.065,.91)
 ell('Shoulder.'+side,sh,(.067,.065,.083),skin,'UpperArm.'+side)
 tube('UpperArm.'+side,[sh,(s*.265,.006,1.66),el],[.053,.044,.033],skin,'UpperArm.'+side,20)
 tube('Forearm.'+side,[el,(s*.337,-.025,1.21),wr],[.036,.041,.023],skin,'Forearm.'+side,20)
 ell('Palm.'+side,(s*.405,-.066,.856),(.034,.023,.073),skin,'Hand.'+side)
 for j in range(4):
  xx=s*(.38+j*.018);z=.805;length=[.125,.16,.155,.13][j]
  pts=[(xx,-.072,z),(xx+s*.008,-.083,z-length*.45),(xx-s*.005,-.105,z-length*.85),(xx-s*.027,-.104,z-length)]
  tube('Finger%d.'%j+side,pts,[.009,.008,.006,.003],skin,'Hand.'+side,10)
 tube('Thumb.'+side,[(s*.379,-.068,.873),(s*.352,-.084,.826),(s*.347,-.103,.78)],[.012,.009,.004],skin,'Hand.'+side,10)
 for j in range(3):
  tube('Arm tendon',[(s*(.302+j*.009),-.043,1.38),(s*(.344+j*.009),-.065,1.16),(s*(.384+j*.007),-.09,.93)],[.003,.004,.002],skin,'Forearm.'+side,6)
# Pleated ragged wrap: overlapping narrow woven panels with irregular hems.
for j in range(36):
 a=j*math.tau/36;pts=[]
 for k in range(7):
  z=1.29-k*(.30+random.random()*.09)/6;rad=1+k*.018
  pts.append((math.sin(a)*.155*rad,math.cos(a)*.102*rad,z))
 ribbon('Pleated rag %02d'%j,pts,[.031,.034,.032,.034,.028,.026,.009],skirt,'Pelvis',(math.cos(a),-math.sin(a),0))
# Torn shirt shoulder layers, open chest and layered left epaulette.
for s in [-1,1]:
 for j in range(7 if s>0 else 4):
  x=s*(.095+j*.022);z=1.91-abs(x)*.18
  ribbon('Torn shoulder',[(x,.07,z),(x,-.02,z+.016),(x,-.10,z-.03),(x+s*.018,-.115,z-.14-random.random()*.10)],[.060,.066,.056,.012],cloth,'Spine')
# Chest academic mark on cloth patch.
ribbon('F shirt patch',[(.105,-.12,1.85),(.11,-.125,1.79),(.10,-.122,1.70)],[.080,.088,.036],cloth)
tube('Circled F',[(.105+.027*math.cos(j*math.tau/40),-.132,1.79+.033*math.sin(j*math.tau/40)) for j in range(41)],[.002]*41,red,sides=6)
for pts in [[(.095,-.135,1.766),(.095,-.135,1.812)],[(.095,-.135,1.812),(.119,-.135,1.812)],[(.095,-.135,1.792),(.113,-.135,1.792)]]:tube('F ink',pts,[.002]*2,red,sides=6)
# Harness: diagonal front, back X, horizontal rib belt, twin waist bands.
ribbon('Front diagonal',[(-.18,-.09,1.9),(-.10,-.125,1.75),(0,-.13,1.58),(.11,-.10,1.44)],[.042]*4,leather)
for s in [-1,1]:ribbon('Back X',[(s*.18,.075,1.91),(s*.09,.11,1.73),(-s*.03,.11,1.57),(-s*.12,.075,1.43)],[.040]*4,leather)
for z,rx,ry,b in [(1.59,.167,.121,'Spine'),(1.32,.126,.095,'Pelvis'),(1.23,.16,.108,'Pelvis')]:
 belt('Harness belt',z,rx,ry,bone=b);buckle('Waist buckle',-.025,-ry-.008,z,b)
for z in [1.61,1.71,1.81]:ell('Harness rivet',(-.08,-.145,z),(.003,.002,.003),metal)
# Asymmetric character-left shin guard and character-right thigh strap.
tube('Left shin leather',[(.105,-.005,.23),(.105,.01,.43),(.105,.002,.68)],[(.031,.031),(.047,.048),(.051,.043)],leather,'Shin.L',20)
for z in [.29,.64]:belt('Shin strap',z,.053,.053,cx=.105,bone='Shin.L',width=.026);buckle('Shin buckle',.105,-.058,z,'Shin.L',.036,.032)
belt('Right thigh strap',.94,.067,.069,cx=-.112,bone='Thigh.R',width=.035);buckle('Thigh buckle',-.112,-.076,.94,'Thigh.R')
# Bandage ribbons around the entire head, diagonal edges and cloth overlap.
for j in range(6):
 pts=[]
 for k in range(65):
  a=k*math.tau/64;pts.append((.100*math.cos(a),-.12+.112*math.sin(a),2.16+j*.016+.020*math.cos(a+j*.6)))
 ribbon('Blindfold layer',pts,[.024]*65,cloth,'Head',(0,0,1))
# Wet hair ribbons fall from crown to shoulder blades, varied ends and strand ridges.
for j in range(100):
 a=random.uniform(0,math.tau);back=math.sin(a)>-.25
 length=random.uniform(.36,.77) if back else random.uniform(.12,.35)
 pts=[]
 for k in range(12):
  t=k/11;rad=.025+.095*math.sin(min(t*2.0,1)*math.pi/2)
  pts.append((math.cos(a)*rad+.010*math.sin(t*9+j),-.115+math.sin(a)*rad+t*.075,2.335-t*length))
 ribbon('Wet hair lock',pts,[.013*(1-k/13) for k in range(12)],hair,'Head',(math.sin(a),-math.cos(a),0))
# Paper slips with original abstract handwriting, not a pasted concept texture.
for j,(x,y,z,b) in enumerate([(-.085,-.14,2.30,'Head'),(.085,-.13,2.30,'Head'),(-.11,-.113,1.31,'Pelvis'),(.11,-.115,1.28,'Pelvis'),(.155,.025,1.26,'Pelvis'),(-.11,.106,1.28,'Pelvis'),(.06,.12,1.30,'Pelvis')]):
 length=.085 if j<2 else .19;w=.040 if j<2 else .075
 ribbon('Academic slip',[(x,y,z),(x+.006,y-.010,z-length*.5),(x-.004,y-.004,z-length)],[w,w,w*.88],paper,b)
 for k in range(8):
  zz=z-.012-k*length/10
  tube('Slip handwriting',[(x-w*.35,y-.013,zz),(x+random.uniform(.05,.34)*w,y-.013,zz+.001)],[.0006]*2,ink,b,4)
# Reference image is a non-rendering orthographic guide in the source.
ref=bpy.data.objects.new('REFERENCE_LOCKED_TURNAROUND',None);scene.collection.objects.link(ref);ref.empty_display_type='IMAGE'
ref.data=bpy.data.images.load(str(R/'Docs/References/Raka’s Manifestation Character Turnaround.png'),check_existing=True);ref.empty_display_size=4.6;ref.location=(0,.7,1.17);ref.rotation_euler=(math.pi/2,0,0);ref.hide_render=True;ref.hide_set(True)
# Generic skeleton. Weighted segment blends support deformable limbs.
arm=bpy.data.armatures.new('Raka_GenericSkeleton');rig=bpy.data.objects.new('ACT_Shadow_Raka',arm);scene.collection.objects.link(rig);bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
bones={}
def bone(n,h,t,parent=None):
 b=arm.edit_bones.new(n);b.head=h;b.tail=t
 if parent:b.parent=arm.edit_bones[parent]
 bones[n]=(Vector(h),Vector(t));return b
bone('Root',(0,0,0),(0,0,.2));bone('Pelvis',(0,0,1.14),(0,0,1.32),'Root');bone('Spine',(0,0,1.32),(0,0,1.89),'Pelvis');bone('Neck',(0,0,1.89),(0,-.09,2.10),'Spine');bone('Head',(0,-.09,2.10),(0,-.12,2.33),'Neck')
for s in [-1,1]:
 side='L' if s>0 else 'R';x=s*.105
 bone('Thigh.'+side,(x,0,1.20),(x,-.03,.76),'Pelvis');bone('Shin.'+side,(x,-.03,.76),(x,-.015,.10),'Thigh.'+side);bone('Foot.'+side,(x,-.015,.10),(x,-.17,.035),'Shin.'+side)
 bone('UpperArm.'+side,(s*.215,0,1.88),(s*.30,-.008,1.39),'Spine');bone('Forearm.'+side,(s*.30,-.008,1.39),(s*.395,-.065,.91),'UpperArm.'+side);bone('Hand.'+side,(s*.395,-.065,.91),(s*.405,-.08,.70),'Forearm.'+side)
bpy.ops.object.mode_set(mode='OBJECT')
for o in parts:
 # Convert world coordinates to weights before parent assignment (identity rig).
 group=o.vertex_groups.new(name=o['bind_bone']);group.add(list(range(len(o.data.vertices))),1,'REPLACE')
 o.parent=rig;mod=o.modifiers.new('Generic skin','ARMATURE');mod.object=rig
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.025);bpy.ops.object.mode_set(mode='OBJECT')
# Eight authored in-place motion studies; zero root translation.
rig.animation_data_create()
names=['Idle_Breath','Walk_Lurch','Pursue','Search_Cover','Fight_Reaction','Hide_Pass','Approach_Reveal','Despawn']
for name in names:
 action=bpy.data.actions.new(name);rig.animation_data.action=action
 for f in range(1,50,4):
  t=(f-1)/48;wave=math.sin(t*math.tau);pulse=math.sin(t*math.pi)
  for p in rig.pose.bones:p.rotation_mode='XYZ';p.rotation_euler=(0,0,0)
  rig.pose.bones['Spine'].rotation_euler.x=.018*wave
  if name in ['Walk_Lurch','Pursue','Hide_Pass']:
   strength=.28 if name=='Pursue' else .16
   for s,side in [(1,'L'),(-1,'R')]:
    rig.pose.bones['Thigh.'+side].rotation_euler.x=s*strength*wave;rig.pose.bones['Shin.'+side].rotation_euler.x=max(0,-s*wave)*.20
    rig.pose.bones['UpperArm.'+side].rotation_euler.x=-s*.10*wave
  if name=='Search_Cover':rig.pose.bones['Neck'].rotation_euler.z=wave*.48
  if name=='Fight_Reaction':rig.pose.bones['Spine'].rotation_euler.x=-pulse*.40
  if name=='Approach_Reveal':rig.pose.bones['Head'].rotation_euler.x=-pulse*.20
  if name=='Despawn':rig.pose.bones['Spine'].rotation_euler.x=pulse*.48
  for p in rig.pose.bones:p.keyframe_insert(data_path='rotation_euler',frame=f)
 track=rig.animation_data.nla_tracks.new();track.name=name;track.strips.new(name,1,action);track.mute=True
rig.animation_data.action=None
for p in rig.pose.bones:p.rotation_euler=(0,0,0)
scene.frame_set(1)
# Neutral studio render reveals geometry instead of hiding it in game fog.
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=1000;scene.render.resolution_y=1300;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Raka Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.22,.22,.22,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.5
def light(name,loc,power,size):
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,1.3))-o.location).to_track_quat('-Z','Y').to_euler()
light('Key',(-2,-3,4),350,3);light('Fill',(2,-1,2.5),180,2);light('Rim',(0,2,3),400,2)
cam=bpy.data.objects.new('Raka_Preview',bpy.data.cameras.new('Raka_Preview'));scene.collection.objects.link(cam);scene.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=2.65
out=R/'Docs/QA/Raka';out.mkdir(parents=True,exist_ok=True)
for name,loc in [('Front',(0,-5,1.2)),('ThreeQuarter',(3,-5,1.5)),('Back',(0,5,1.2)),('Side',(5,0,1.2))]:
 cam.location=loc;cam.rotation_euler=(Vector((0,0,1.18))-cam.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=rig
fbx=R/'Assets/_NoRainbowAfter/Art/Models/Characters/ACT_Shadow_Raka.fbx'
bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True,bake_anim_force_startend_keying=True,use_mesh_modifiers=True)
scene['production_status']='Reference-directed first mesh pass; material bake and game integration pending visual review'
bpy.ops.wm.save_as_mainfile(filepath=str(R/'BlenderSources/Characters/ACT_Shadow_Raka.blend'),compress=True)
tri=sum(len(o.data.polygons) for o in parts)
(out/'manifest.json').write_text(json.dumps({'meshes':len(parts),'polygons':tri,'bones':len(bones),'clips':names,'status':scene['production_status']},indent=2))
print('RAKA_GENERATED',len(parts),tri)
