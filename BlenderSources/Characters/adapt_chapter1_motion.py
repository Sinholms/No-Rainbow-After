"""Bake CC0 Quaternius/KinemIQ hit deltas and an original shoulder-grab onto the C1 silhouette.
Run with Blender --background --python. Rebuildable; leaves original placeholder/hero untouched.
"""
import math
from pathlib import Path
import bpy
from mathutils import Vector, Matrix, Quaternion

ROOT = Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'BlenderSources/Characters/ACT_001_ShadowPlaceholder_v1.blend'))
scene = bpy.context.scene
scene.render.fps = 30
meshes = [o for o in scene.objects if o.type == 'MESH']
for obj in meshes:
    world = obj.matrix_world.copy()
    obj.parent = None
    obj.data.transform(world)
    obj.matrix_world = Matrix.Identity(4)

data = bpy.data.armatures.new('C1_ShadowSkeleton')
rig = bpy.data.objects.new('C1_ShadowMotion', data)
scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
spec = {
 'Root':((0,0,0),(0,0,.2),None),
 'Spine':((0,.02,1.06),(0,-.02,1.42),'Root'),
 'Chest':((0,-.02,1.42),(0,-.13,1.87),'Spine'),
 'Head':((0,-.13,1.87),(0,-.20,2.24),'Chest'),
}
for side,sgn,elbow,wrist in [('L',-1,1.05,.58),('R',1,1.15,.76)]:
    spec['Upper'+side]=((sgn*.20,-.09,1.70),(sgn*.23,-.08,elbow),'Chest')
    spec['Fore'+side]=((sgn*.23,-.08,elbow),(sgn*.20,-.15,wrist),'Upper'+side)
    spec['Hand'+side]=((sgn*.20,-.15,wrist),(sgn*.18,-.16,wrist-.13),'Fore'+side)
for name,(head,tail,parent) in spec.items():
    b=data.edit_bones.new(name); b.head=head; b.tail=tail
    if parent: b.parent=data.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')

def blend(a,b,t): return [(a,1-max(0,min(1,t))),(b,max(0,min(1,t)))]
for obj in meshes:
    for n in spec: obj.vertex_groups.new(name=n)
    for v in obj.data.vertices:
        z=v.co.z
        if 'Arm' in obj.name:
            side='L' if 'Left' in obj.name else 'R'
            elbow=1.05 if side=='L' else 1.15
            wrist=.58 if side=='L' else .76
            weights=blend('Fore'+side,'Upper'+side,(z-elbow+.13)/.26) if z>elbow-.13 else blend('Hand'+side,'Fore'+side,(z-wrist+.06)/.12)
        elif 'Head' in obj.name or 'FaceWrap' in obj.name: weights=[('Head',1)]
        elif 'Lower' in obj.name: weights=[('Root',1)]
        else: weights=blend('Spine','Chest',(z-1.22)/.35)
        for name,w in weights:
            if w>0: obj.vertex_groups[name].add([v.index],w,'REPLACE')
    obj.parent=rig
    mod=obj.modifiers.new('C1 deform','ARMATURE'); mod.object=rig

def action(name):
    rig.animation_data_create()
    a=bpy.data.actions.new(name);rig.animation_data.action=a
    for p in rig.pose.bones: p.rotation_mode='QUATERNION';p.matrix_basis=Matrix.Identity(4)
    return a

def key(frame):
    for p in rig.pose.bones:
        p.keyframe_insert('rotation_quaternion',frame=frame,group=p.name)
        p.keyframe_insert('location',frame=frame,group=p.name)

# Source animation files are skeleton-node GLBs. Blender imports their animated joints as empties.
source_map={'Spine':'spine_01','Chest':'spine_03','Head':'Head','UpperL':'upperarm_l','ForeL':'lowerarm_l','HandL':'hand_l','UpperR':'upperarm_r','ForeR':'lowerarm_r','HandR':'hand_r'}
clips=[]
for filename,name,seconds in [('Hit_to_the_chest','Hit_Chest',.52),('Hit_knockback','Hit_Final',.85)]:
    before=set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(ROOT/f'ThirdPartySources/QuaterniusMotion/Library/GLB/{filename}.glb'))
    sources=[o for o in bpy.data.objects if o not in before]
    nodes={o.name.split('.')[0]:o for o in sources}
    source_action=next((o.animation_data.action for o in sources if o.animation_data and o.animation_data.action),None)
    end=source_action.frame_range[1]
    scene.frame_set(0)
    base={n:nodes[src].matrix_world.to_quaternion().copy() for n,src in source_map.items() if src in nodes}
    samples=[]
    frames=round(seconds*30)
    for f in range(frames+1):
        scene.frame_set(round(end*f/frames))
        samples.append({n:(nodes[source_map[n]].matrix_world.to_quaternion() @ q.inverted()) for n,q in base.items()})
    for o in sources: bpy.data.objects.remove(o,do_unlink=True)
    a=action(name); clips.append(a)
    for f,deltas in enumerate(samples):
        # Fade the first/last poses into the existing hunched silhouette, feet remain planted.
        envelope=min(1,f/2,(frames-f)/4) if name=='Hit_Chest' else min(1,f/2)
        for n in spec:
            pb=rig.pose.bones[n];rest=data.bones[n].matrix_local
            delta=Quaternion().slerp(deltas.get(n,Quaternion()),max(0,envelope)*(.8 if name=='Hit_Chest' else .7))
            desired=delta.to_matrix().to_4x4() @ rest
            if n=='Root': desired=rest.copy()
            if pb.parent:
                desired.translation = pb.parent.matrix @ pb.parent.bone.matrix_local.inverted() @ rest.translation
            pb.matrix=desired
            bpy.context.view_layer.update()
        # Add staggered impact/settle to suit the much taller, thinner silhouette.
        # Source motion stays on separate joints; these offsets add readable weight in FPP.
        t=f/frames
        impact=math.sin(math.pi*min(1,t/.48)) if t<.48 else 0
        settle=math.sin(math.pi*max(0,min(1,(t-.12)/.88)))
        rig.pose.bones['Spine'].rotation_quaternion @= Quaternion((1,0,0), math.radians(-9)*impact)
        rig.pose.bones['Chest'].rotation_quaternion @= Quaternion((1,0,0), math.radians(-14)*impact+math.radians(4)*settle)
        rig.pose.bones['Head'].rotation_quaternion @= Quaternion((1,0,0), math.radians(9)*settle)
        rig.pose.bones['UpperL'].rotation_quaternion @= Quaternion((1,0,0),math.radians(10)*settle)
        rig.pose.bones['UpperR'].rotation_quaternion @= Quaternion((1,0,0),math.radians(7)*settle)
        key(f)
    if name=='Hit_Final':
        # Original final collapse after the source knockback; root collider stays grounded.
        for f in range(frames+1,37):
            t=(f-frames)/(36-frames)
            root=rig.pose.bones['Root'];root.rotation_quaternion=Quaternion((1,0,0),-math.radians(72)*t);root.location.z=-.35*t
            key(f)

def smooth(t): t=max(0,min(1,t));return t*t*(3-2*t)
a=action('Grab_Player');clips.append(a)
for f in range(36):
    t=f/35
    reach=smooth(t/.27)*(1-smooth((t-.78)/.22))
    for p in rig.pose.bones: p.matrix_basis=Matrix.Identity(4)
    bpy.context.view_layer.update()
    # Small body lean; solve shoulder/elbow/wrist explicitly, avoiding an overhead arm swing.
    for side,sgn in [('L',-1),('R',1)]:
        upper=rig.pose.bones['Upper'+side];fore=rig.pose.bones['Fore'+side];hand=rig.pose.bones['Hand'+side]
        shoulder=data.bones[upper.name].head_local.copy()
        rest=data.bones[hand.name].head_local.copy()
        wrist=rest.lerp(Vector((sgn*.29,-.95,1.54)),reach)
        wrist.y += math.sin(t*math.pi*10)*.018*reach if t>.3 else 0
        a_len=data.bones[upper.name].length;b_len=data.bones[fore.name].length
        d=wrist-shoulder;dist=min(d.length,a_len+b_len-.005);axis=d.normalized()
        pole=Vector((sgn,0,-.6));pole=(pole-axis*pole.dot(axis)).normalized()
        along=(a_len*a_len-b_len*b_len+dist*dist)/(2*dist)
        elbow=shoulder+axis*along+pole*math.sqrt(max(0,a_len*a_len-along*along))
        for pb,start,end in [(upper,shoulder,elbow),(fore,elbow,wrist)]:
            restbone=data.bones[pb.name];rot=(restbone.tail_local-restbone.head_local).rotation_difference(end-start)
            matrix=rot.to_matrix().to_4x4()@restbone.matrix_local;matrix.translation=start;pb.matrix=matrix
            bpy.context.view_layer.update()
        # Fingers point towards the shoulder contact rather than hanging from raised arms.
        direction=Vector((sgn*-.10,-.10,-.10)).normalized()
        restbone=data.bones[hand.name];rot=(restbone.tail_local-restbone.head_local).rotation_difference(direction)
        matrix=rot.to_matrix().to_4x4()@restbone.matrix_local;matrix.translation=wrist;hand.matrix=matrix
        bpy.context.view_layer.update()
    key(f)

# Neutral action provides a reliable reset when chase/hide/capture ends.
a=action('Rest');clips.append(a);key(0);key(1)
rig.animation_data.action=None
for clip in clips:
    track=rig.animation_data.nla_tracks.new();track.name=clip.name
    strip=track.strips.new(clip.name,0,clip);strip.action_frame_start=clip.frame_range[0];strip.action_frame_end=clip.frame_range[1]
    track.mute=True
scene.frame_set(0)
bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True)
for o in meshes:o.select_set(True)
bpy.context.view_layer.objects.active=rig
out=ROOT/'Assets/_NoRainbowAfter/Art/Models/Characters/ACT_C1_ShadowMotion.fbx'
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSources/Characters/ACT_C1_ShadowMotion.blend'))
bpy.ops.export_scene.fbx(filepath=str(out),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,axis_forward='-Z',axis_up='Y',bake_anim=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True,bake_anim_simplify_factor=0,apply_scale_options='FBX_SCALE_UNITS')
print('EXPORTED',out)
