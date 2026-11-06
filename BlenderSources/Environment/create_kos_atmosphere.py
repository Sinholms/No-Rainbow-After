"""Original No Rainbow After environment. Run PHASE=1 (structure), 2 (props), 3 (export) in Blender."""
import bpy, math, json, random, wave
import numpy as np
from pathlib import Path
from mathutils import Vector

ROOT = Path('D:/GRAVKOM/No rainbow after')
SCENE_NAME = 'NRA_KosAtmosphere_v1'
EXPORT = ROOT / 'Assets/_NoRainbowAfter/Art/Models/Environment/ENV_KosAtmosphere_v1.fbx'
SOURCE = ROOT / 'BlenderSources/Environment/KosAtmosphere_v1.blend'
TEXDIR = ROOT / 'Assets/_NoRainbowAfter/Art/Textures/KosAtmosphere'
MANIFEST = ROOT / 'BlenderSources/Environment/KosAtmosphere_v1_manifest.json'
RNG = random.Random(41)
SPECS = {
    'Wall': ((0.40,0.42,0.36),0.0,0.17),
    'Floor': ((0.26,0.29,0.27),0.0,0.48),
    'Wood': ((0.30,0.18,0.095),0.0,0.30),
    'Cloth': ((0.24,0.30,0.32),0.0,0.08),
    'Metal': ((0.12,0.16,0.16),0.65,0.45),
    'Paper': ((0.64,0.59,0.45),0.0,0.04),
    'Ink': ((0.025,0.032,0.027),0.0,0.04),
    'Glass': ((0.06,0.13,0.17),0.15,0.80),
    'LampWarm': ((0.75,0.55,0.30),0.0,0.15),
    'LampCold': ((0.35,0.62,0.68),0.0,0.15)
}

def fbm(u,v):
    n = np.zeros_like(u)
    for i,amp in enumerate([0.45,0.25,0.15,0.09,0.06]):
        f=3*2**i
        x=u*f;y=v*f;xi=np.floor(x);yi=np.floor(y);tx=x-xi;ty=y-yi
        tx=tx*tx*(3-2*tx);ty=ty*ty*(3-2*ty)
        def h(a,b):return np.mod(np.sin(np.mod(a,f)*127.1+np.mod(b,f)*311.7+i*43.17)*43758.5453,1)
        a=h(xi,yi)*(1-tx)+h(xi+1,yi)*tx
        b=h(xi,yi+1)*(1-tx)+h(xi+1,yi+1)*tx
        n+=amp*(a*(1-ty)+b*ty)
    return n

def save_image(name,rgb,data=False):
    h,w = rgb.shape[:2]
    if rgb.ndim == 2:
        rgb = np.repeat(rgb[:,:,None],3,axis=2)
    if rgb.shape[2] == 3:
        rgb = np.concatenate([rgb,np.ones((h,w,1),dtype=np.float32)],axis=2)
    image = bpy.data.images.get(name) or bpy.data.images.new(name,width=w,height=h,alpha=True)
    image.colorspace_settings.name = 'Non-Color' if data else 'sRGB'
    image.pixels.foreach_set(np.clip(rgb,0,1).astype(np.float32).ravel())
    image.filepath_raw = str(TEXDIR/(name+'.png'))
    image.file_format = 'PNG'
    image.save()
    return image

def material(key, textured=True):
    color,metal,smooth = SPECS[key]
    mat = bpy.data.materials.get('MAT_H_'+key) or bpy.data.materials.new('MAT_H_'+key)
    mat.diffuse_color = (*color,1)
    mat.use_nodes=True
    mat.node_tree.nodes.clear()
    p=mat.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
    out=mat.node_tree.nodes.new('ShaderNodeOutputMaterial')
    mat.node_tree.links.new(p.outputs['BSDF'],out.inputs['Surface'])
    p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Metallic'].default_value=metal
    p.inputs['Roughness'].default_value=1-smooth
    if textured:
        s=1024
        v,u=np.mgrid[0:1:complex(s),0:1:complex(s)]
        n=fbm(u,v); fine=fbm(u*8,v*8)
        base=np.ones((s,s,3),dtype=np.float32)*np.array(color)
        height=n*0.25+fine*0.025
        rough=np.clip(1-smooth+(fine-0.5)*0.12,0.05,0.98)
        metallic=np.full((s,s),metal)
        if key=='Wall':
            peel=np.clip((fbm(u*2+0.17,v*2)-0.57)*8,0,1)
            damp=(1-v)**2*np.clip((n-0.22)*2,0,1)
            base*= (0.68+fine[:,:,None]*0.36)
            base=base*(1-peel[:,:,None]*0.5)+np.array([0.21,0.22,0.18])*peel[:,:,None]*0.5
            base*=1-damp[:,:,None]*0.63
            # Thin branching, uneven hairline cracks in the plaster.
            crack=np.clip((0.0025-np.abs(fbm(u+0.23,v)-0.52))*400,0,1)*(n>0.55)
            base*=1-crack[:,:,None]*0.75
            height=height-peel*0.22-crack*0.18
        elif key=='Floor':
            edge=np.minimum.reduce([u,1-u,v,1-v])
            grout=np.clip((0.018-edge)*90,0,1)
            stain=np.clip((n-0.48)*3,0,0.6)
            base*=0.77+fine[:,:,None]*0.30
            base*=1-stain[:,:,None]*0.38
            base=base*(1-grout[:,:,None])+np.array([0.07,0.085,0.07])*grout[:,:,None]
            height=0.3-grout*0.22+n*0.015
            rough=np.clip(0.45+fine*0.30-stain*0.30,0.18,0.9)
        elif key=='Wood':
            grain=np.sin((u*46+np.sin(v*7)*0.25+n*0.8)*2*math.pi)*0.5+0.5
            scratches=np.exp(-np.abs(np.sin(u*92*math.pi+np.sin(v*4)))*65)*(fine>0.48)
            base*=0.62+grain[:,:,None]*0.34+n[:,:,None]*0.24
            base+=scratches[:,:,None]*np.array([0.12,0.11,0.07])
            height=grain*0.08+n*0.025
        elif key=='Cloth':
            weave=(np.sin(u*256*math.pi)*np.sin(v*256*math.pi))*0.5+0.5
            base*=0.72+n[:,:,None]*0.35+weave[:,:,None]*0.08
            height=weave*0.05+n*0.03
            rough=np.full((s,s),0.91)
        elif key=='Metal':
            rust=np.clip((n-0.48)*5,0,1)
            base*=0.65+fine[:,:,None]*0.45
            base=base*(1-rust[:,:,None])+np.array([0.26,0.13,0.055])*rust[:,:,None]
            metallic*=1-rust
            rough=np.clip(0.45+rust*0.4+fine*0.1,0,1)
            height=rust*0.08+fine*0.04
        elif key=='Paper':
            base*=0.83+n[:,:,None]*0.12+fine[:,:,None]*0.05
            height=fine*0.03
            rough=np.full((s,s),0.94)
        base_image=save_image(key+'_BaseColor',base)
        dy,dx=np.gradient(height)
        normal=np.stack([-dx*11,-dy*11,np.ones_like(height)],axis=2)
        normal/=np.linalg.norm(normal,axis=2)[:,:,None]
        normal_image=save_image(key+'_Normal',normal*0.5+0.5,True)
        mask=np.stack([metallic,np.zeros_like(n),np.zeros_like(n),1-rough],axis=2)
        save_image(key+'_Mask',mask,True)
        tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=base_image
        mat.node_tree.links.new(tex.outputs['Color'],p.inputs['Base Color'])
        texn=mat.node_tree.nodes.new('ShaderNodeTexImage');texn.image=normal_image
        normal_node=mat.node_tree.nodes.new('ShaderNodeNormalMap')
        mat.node_tree.links.new(texn.outputs['Color'],normal_node.inputs['Color'])
        mat.node_tree.links.new(normal_node.outputs['Normal'],p.inputs['Normal'])
    if key.startswith('Lamp'):
        p.inputs['Emission Color'].default_value=(*color,1)
        p.inputs['Emission Strength'].default_value=2
    return mat

def register(obj,key,collision='none'):
    COLLECTION.objects.link(obj)
    obj.data.materials.append(MATS[key])
    obj['nra_collision']=collision
    return obj

def box(name,pos,size,key='Wood',bevel=0.01,collision='none'):
    x,y,z=[s/2 for s in size]
    verts=[(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),(-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
    faces=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    mesh=bpy.data.meshes.new(name+'_mesh');mesh.from_pydata(verts,[],faces);mesh.update()
    obj=register(bpy.data.objects.new(name,mesh),key,collision);obj.location=pos
    uv=mesh.uv_layers.new(name='UVMap')
    for poly in mesh.polygons:
        normal=poly.normal
        axes=(0,1) if abs(normal.z)>0.5 else (1,2) if abs(normal.x)>0.5 else (0,2)
        scale=2.5 if key=='Floor' else 0.7 if key=='Wall' else 1.8
        for li in poly.loop_indices:
            co=mesh.vertices[mesh.loops[li].vertex_index].co
            uv.data[li].uv=(co[axes[0]]*scale,co[axes[1]]*scale)
    if bevel:
        mod=obj.modifiers.new('Worn rounded edges','BEVEL');mod.width=bevel;mod.segments=3
    return obj

def tube(name,points,radius,key='Metal',closed=False):
    data=bpy.data.curves.new(name+'_curve','CURVE');data.dimensions='3D'
    data.bevel_depth=radius;data.bevel_resolution=2;data.resolution_u=8
    spline=data.splines.new('POLY');spline.points.add(len(points)-1)
    for p,co in zip(spline.points,points):p.co=(*co,1)
    spline.use_cyclic_u=closed
    return register(bpy.data.objects.new(name,data),key)

def ring(name,center,radius,axis='Z',wire=0.003,key='Metal',segments=48):
    x,y,z=center
    pts=[]
    for a in np.linspace(0,2*math.pi,segments,endpoint=False):
        c,s=math.cos(a)*radius,math.sin(a)*radius
        pts.append((x+c,y+s,z) if axis=='Z' else (x+c,y,z+s) if axis=='Y' else (x,y+c,z+s))
    return tube(name,pts,wire,key,True)

def text(name,value,pos,size=0.025,rotation=(0,0,0),key='Ink'):
    data=bpy.data.curves.new(name+'_font','FONT');data.body=value;data.size=size;data.extrude=0.0001
    obj=register(bpy.data.objects.new(name,data),key);obj.location=pos;obj.rotation_euler=rotation
    return obj

def setup():
    global SCENE,COLLECTION,MATS
    if SCENE_NAME in bpy.data.scenes or EXPORT.exists() or SOURCE.exists():
        raise RuntimeError('Atmosphere v1 already exists; refusing to overwrite unrelated work.')
    TEXDIR.mkdir(parents=True,exist_ok=True);SOURCE.parent.mkdir(parents=True,exist_ok=True)
    SCENE=bpy.data.scenes.new(SCENE_NAME);SCENE.unit_settings.system='METRIC';SCENE.unit_settings.scale_length=1
    COLLECTION=bpy.data.collections.new('NRA_KosAtmosphere_Geometry');SCENE.collection.children.link(COLLECTION)
    if bpy.context.window.scene != SCENE:bpy.context.window.scene=SCENE
    MATS={key:material(key,key in ['Wall','Floor','Wood','Cloth','Metal','Paper']) for key in SPECS}

def attach():
    global SCENE,COLLECTION,MATS
    SCENE=bpy.data.scenes[SCENE_NAME];COLLECTION=bpy.data.collections['NRA_KosAtmosphere_Geometry']
    if bpy.context.window.scene != SCENE:bpy.context.window.scene=SCENE
    MATS={key:bpy.data.materials['MAT_H_'+key] for key in SPECS}

def structure():
    box('SM_ENV_RoomFloor',(0,2.5,-0.10),(4.2,5.2,0.2),'Floor',0,'box')
    box('SM_ENV_RoomCeiling',(0,2.5,2.76),(4.2,5.2,0.14),'Wall',0,'box')
    box('SM_ENV_RoomFront',(0,-0.09,1.35),(4.2,0.18,2.7),'Wall',0,'box')
    box('SM_ENV_RoomRight',(2.09,2.5,1.35),(0.18,5.2,2.7),'Wall',0,'box')
    for mid,length in [(1.3,2.6),(4.65,0.9)]:
        box('SM_ENV_WindowWall',(-2.09,mid,1.35),(0.18,length,2.7),'Wall',0,'box')
    box('SM_ENV_WindowBelow',(-2.09,3.4,0.52),(0.18,1.6,1.04),'Wall',0,'box')
    box('SM_ENV_WindowAbove',(-2.09,3.4,2.4),(0.18,1.6,0.6),'Wall',0,'box')
    for x in [-1.27,1.27]:box('SM_ENV_DoorWall',(x,5.08,1.35),(1.46,0.18,2.7),'Wall',0,'box')
    box('SM_ENV_DoorHeader',(0,5.08,2.42),(1.08,0.18,0.56),'Wall',0,'box')
    box('SM_ENV_CorridorFloor',(0,12,-0.10),(2.0,14.2,0.2),'Floor',0,'box')
    box('SM_ENV_CorridorCeiling',(0,12,2.76),(2.15,14.2,0.14),'Wall',0,'box')
    # Small occluded side pockets, following design.md's readable corridor/cover layout.
    for side,gap in [(-1,(8.2,9.8)),(1,(12.2,13.8))]:
        for start,end in [(5,gap[0]),(gap[1],19)]:
            box('SM_ENV_CorridorWall',(side*0.99,(start+end)/2,1.35),(0.18,end-start,2.7),'Wall',0,'box')
        box('SM_ENV_NicheBack',(side*1.65,sum(gap)/2,1.35),(0.18,gap[1]-gap[0]+0.18,2.7),'Wall',0,'box')
        box('SM_ENV_NicheFloor',(side*1.25,sum(gap)/2,-0.1),(0.9,1.8,0.2),'Floor',0,'box')
        for y in gap:box('SM_ENV_NicheEnd',(side*1.32,y,1.35),(0.74,0.18,2.7),'Wall',0,'box')
    box('SM_ENV_CorridorEnd',(0,19.08,1.35),(2.15,0.18,2.7),'Wall',0,'box')
    for x in [-1.97,1.97]:box('SM_ENV_RoomSkirting',(x,2.5,0.055),(0.045,5,0.11),'Wood',0.006)
    for x in [-0.87,0.87]:box('SM_ENV_HallSkirting',(x,12,0.055),(0.045,14,0.11),'Wood',0.006)
    # A barred, rain-clouded window with a real opening in the wall.
    box('SM_PROP_WindowGlass',(-2.105,3.4,1.575),(0.025,1.42,1.10),'Glass',0,'box')
    for y in [2.66,4.14]:box('SM_PROP_WindowFrame',(-1.99,y,1.575),(0.12,0.07,1.24),'Wood',0.009)
    for z in [1.02,2.13]:box('SM_PROP_WindowSill',(-1.96,3.4,z),(0.18,1.55,0.075),'Wood',0.009)
    for y in np.linspace(2.8,4.0,6):tube('SM_PROP_WindowBars',[(-1.95,y,1.08),(-1.95,y,2.08)],0.012)
    # Rounded door framing; the actual open leaf is dressed during the prop pass.
    for x in [-0.5,0.5]:box('SM_PROP_DoorJamb',(x,5,1.06),(0.075,0.18,2.12),'Wood',0.008,'box')
    box('SM_PROP_DoorLintel',(0,5,2.09),(1.08,0.18,0.08),'Wood',0.008,'box')
    # Exposed pipes and overhead cable runs give the hallway a lived-in silhouette.
    for x,z in [(-0.72,2.46),(-0.57,2.54)]:
        tube('SM_PROP_HallPipe',[(x,5,z),(x,18.7,z),(x,18.9,1.4)],0.023)
        for y in [6,9,12,15,18]:ring('SM_PROP_PipeClamp',(x,y,z),0.032,'Y',0.007)
    tube('SM_PROP_RoomCable',[(1.8,3.8,0.1),(1.95,3.8,0.1),(1.95,3.8,1.2),(1.95,4.2,1.2)],0.005,'Ink')
    for y in [6.3,10.8,17.1]:
        box('SM_PROP_HallFixture',(0,y,2.62),(0.62,0.16,0.07),'Metal',0.01)
        box('SM_PROP_HallDiffuser',(0,y,2.57),(0.46,0.07,0.025),'LampCold' if y>15 else 'LampWarm',0.008)
    box('SM_PROP_RoomFixture',(0,2.6,2.60),(0.75,0.18,0.08),'Metal',0.015)
    box('SM_PROP_RoomDiffuser',(0,2.6,2.55),(0.60,0.07,0.025),'LampWarm',0.008)

def ellipsoid(name,pos,scale,key='Cloth'):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=16,location=pos)
    obj=bpy.context.object;obj.name=name
    for col in list(obj.users_collection):col.objects.unlink(obj)
    COLLECTION.objects.link(obj)
    obj.scale=scale
    obj.data.materials.append(MATS[key]);obj['nra_collision']='none'
    for poly in obj.data.polygons:poly.use_smooth=True
    return obj

def cloth_surface(name,center,width,length,key='Cloth',nx=28,ny=48):
    vertices=[];faces=[]
    for j in range(ny):
        v=j/(ny-1);y=(v-0.5)*length
        for i in range(nx):
            u=i/(nx-1);x=(u-0.5)*width
            fold=0.026*math.sin(y*13+x*3)+0.012*math.sin(x*25-y*6)+0.009*math.sin(y*32+x*8)
            drape=0.21*max(0,(abs(u-0.5)-0.38)/0.12)**1.3
            z=fold-drape+0.035*math.exp(-((y+0.52)/0.15)**2)
            vertices.append((center[0]+x,center[1]+y,center[2]+z))
    for j in range(ny-1):
        for i in range(nx-1):
            k=j*nx+i;faces.append((k,k+1,k+1+nx,k+nx))
    mesh=bpy.data.meshes.new(name+'_mesh');mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=register(bpy.data.objects.new(name,mesh),key)
    uv=mesh.uv_layers.new(name='UVMap')
    for poly in mesh.polygons:
        poly.use_smooth=True
        for li in poly.loop_indices:
            vi=mesh.loops[li].vertex_index;uv.data[li].uv=((vi%nx)/(nx-1),int(vi/nx)/(ny-1))
    solid=obj.modifiers.new('Cloth thickness','SOLIDIFY');solid.thickness=0.006
    return obj

def furniture():
    # Soft bedding on a tubular metal bed: no cube stand-ins for hero fabrics.
    for x in [-1.77,-0.77]:
        tube('SM_PROP_BedRail',[(x,0.65,0.15),(x,0.65,0.34),(x,2.8,0.34),(x,2.8,0.72)],0.021)
        for y in [0.8,2.6]:tube('SM_PROP_BedLeg',[(x,y,0.03),(x,y,0.34)],0.025)
    for y in [0.65,2.80]:
        tube('SM_PROP_BedCrossbar',[(-1.77,y,0.36),(-0.77,y,0.36)],0.021)
    box('SM_PROP_Mattress',(-1.27,1.73,0.44),(0.97,2.07,0.22),'Cloth',0.085,'box')
    cloth_surface('SM_PROP_WrinkledBlanket',(-1.27,1.45,0.575),1.08,1.70)
    ellipsoid('SM_PROP_SoftPillow',(-1.27,2.47,0.61),(0.40,0.245,0.11))
    tube('SM_PROP_MattressSeam',[(-1.75,0.72,0.48),(-1.75,2.73,0.48),(-0.80,2.73,0.48),(-0.80,0.72,0.48)],0.003,'Paper',True)
    # Worn study desk with bevels, drawer, cross braces and turned legs.
    box('SM_PROP_StudyDesktop',(1.28,3.74,0.77),(1.22,0.64,0.065),'Wood',0.018,'box')
    for x in [0.78,1.78]:
        for y in [3.51,3.97]:tube('SM_PROP_DeskLeg',[(x,y,0.04),(x+0.015,y,0.74)],0.026,'Wood')
    box('SM_PROP_DeskApron',(1.28,3.74,0.64),(1.1,0.50,0.16),'Wood',0.012)
    box('SM_PROP_DeskDrawer',(1.28,3.46,0.64),(0.54,0.025,0.13),'Wood',0.009)
    tube('SM_PROP_DeskHandle',[(1.20,3.43,0.64),(1.20,3.40,0.64),(1.36,3.40,0.64),(1.36,3.43,0.64)],0.007)
    tube('SM_PROP_DeskBrace',[(0.78,3.97,0.18),(1.78,3.97,0.18)],0.016)
    # Curved chair back and metal frame, rather than a rectangular chair proxy.
    box('SM_PROP_ChairSeat',(1.25,2.98,0.45),(0.44,0.44,0.045),'Wood',0.035,'box')
    for x in [1.06,1.44]:
        tube('SM_PROP_ChairFrame',[(x,2.76,0.03),(x,2.78,0.45),(x,3.16,0.45),(x,3.19,0.03)],0.015)
        tube('SM_PROP_ChairUpright',[(x,2.78,0.44),(x,2.75,0.90)],0.014)
    vertices=[];faces=[]
    for j in range(6):
        for i in range(17):
            x=(i/16-0.5)*0.43
            vertices.append((1.25+x,2.73+0.07*(x/0.22)**2,0.69+j/5*0.22))
    for j in range(5):
        for i in range(16):
            k=j*17+i;faces.append((k,k+1,k+18,k+17))
    mesh=bpy.data.meshes.new('ChairBack_mesh');mesh.from_pydata(vertices,[],faces);mesh.update()
    back=register(bpy.data.objects.new('SM_PROP_CurvedChairBack',mesh),'Wood')
    solid=back.modifiers.new('Back thickness','SOLIDIFY');solid.thickness=0.016
    # Notebook, textbooks and original readable student-life paper.
    for i in range(3):
        ob=box('SM_PROP_StackedTextbook',(0.91,3.82,0.82+i*0.042),(0.25,0.30,0.038),'Wood' if i%2 else 'Cloth',0.006)
        ob.rotation_euler.z=0.08*(i-1)
        box('SM_PROP_BookPages',(0.91,3.82,0.824+i*0.042),(0.235,0.29,0.025),'Paper',0.002)
    notebook=box('SM_PROP_OpenNotes',(1.40,3.60,0.812),(0.38,0.26,0.012),'Paper',0.002)
    text('SM_PROP_NotesHeading','CATATAN GRAFIKA',(1.23,3.67,0.820),0.018)
    for i in range(5):
        tube('SM_PROP_NotesLine',[(1.24,3.64-i*0.026,0.821),(1.52-(i%2)*0.06,3.64-i*0.026,0.821)],0.0005,'Ink')
    tube('SM_PROP_DeskPen',[(1.61,3.50,0.823),(1.66,3.65,0.823)],0.003,'Ink')
    box('SM_PROP_WorkSlip',(1.45,3.92,0.812),(0.22,0.15,0.006),'Paper',0.001)
    text('SM_PROP_WorkSlipHeading','JADWAL KERJA',(1.35,3.96,0.818),0.014)
    # Desk lamp with a curved stem and shaded bulb.
    ellipsoid('SM_PROP_LampBase',(1.76,3.82,0.82),(0.105,0.085,0.018),'Metal')
    tube('SM_PROP_LampStem',[(1.76,3.82,0.83),(1.77,3.82,1.08),(1.65,3.77,1.22)],0.011)
    ellipsoid('SM_PROP_LampShade',(1.62,3.76,1.20),(0.12,0.10,0.065),'Metal')
    ellipsoid('SM_PROP_LampBulb',(1.61,3.75,1.166),(0.045,0.038,0.018),'LampWarm')
    # Table fan: concentric cage, radial wires, hub, three curved blades.
    box('SM_PROP_FanStand',(-1.68,3.93,0.50),(0.42,0.38,0.95),'Wood',0.015,'box')
    ellipsoid('SM_PROP_FanBase',(-1.68,3.93,1.00),(0.14,0.12,0.025),'Metal')
    tube('SM_PROP_FanNeck',[(-1.68,3.93,1.00),(-1.68,3.97,1.26)],0.022)
    center=(-1.68,3.94,1.46)
    for r in [0.07,0.13,0.19,0.225]:ring('SM_PROP_FanCage',(-1.68,3.87,1.46),r,'Y',0.0025)
    ring('SM_PROP_FanCageBack',(-1.68,4.00,1.46),0.225,'Y',0.004)
    for a in np.linspace(0,2*math.pi,14,endpoint=False):
        tube('SM_PROP_FanSpoke',[(-1.68,3.855,1.46),(-1.68+math.cos(a)*0.225,3.87,1.46+math.sin(a)*0.225)],0.0022)
    ellipsoid('SM_PROP_FanMotor',center,(0.058,0.072,0.058),'Metal')
    for a in [0,2*math.pi/3,4*math.pi/3]:
        blade=ellipsoid('SM_PROP_FanBlade',(-1.68+math.cos(a)*0.11,3.94,1.46+math.sin(a)*0.11),(0.12,0.012,0.043),'Cloth')
        blade.rotation_euler.y=-a
    # Shoes, repaired bag, folded umbrella and extension cable ground the student room.
    for x in [1.37,1.63]:
        ellipsoid('SM_PROP_Shoe',(x,0.67,0.09),(0.10,0.20,0.09),'Ink')
        ellipsoid('SM_PROP_ShoeSole',(x,0.67,0.035),(0.105,0.205,0.027),'Metal')
        for j in range(3):tube('SM_PROP_Shoelace',[(x-0.055,0.65+j*0.035,0.155),(x+0.055,0.68+j*0.035,0.155)],0.002,'Paper')
    bag=ellipsoid('SM_PROP_StudentBag',(1.72,1.0,0.35),(0.23,0.14,0.34),'Cloth')
    tube('SM_PROP_BagStrap',[(1.54,1.0,0.4),(1.52,1.07,0.75),(1.9,1.07,0.75),(1.90,1.0,0.4)],0.014,'Ink')
    tube('SM_PROP_UmbrellaShaft',[(1.9,0.5,0.05),(1.80,0.58,0.93),(1.72,0.58,1.0),(1.66,0.58,0.95)],0.009,'Metal')
    umbrella=ellipsoid('SM_PROP_FoldedUmbrella',(1.85,0.55,0.48),(0.065,0.065,0.38),'Ink')
    umbrella.rotation_euler.y=-0.12
    box('SM_PROP_PowerStrip',(1.80,3.82,0.035),(0.11,0.32,0.045),'Paper',0.012)
    for y in [3.73,3.82,3.91]:
        for x in [1.78,1.82]:ellipsoid('SM_PROP_PowerSocket',(x,y,0.06),(0.008,0.008,0.002),'Ink')

def dressing():
    hinge=(-0.45,4.97);angle=math.radians(75)
    def leaf(name,center,size,key='Wood',collision='none'):
        x,y,z=center
        pos=(hinge[0]+math.cos(angle)*x-math.sin(angle)*y,hinge[1]+math.sin(angle)*x+math.cos(angle)*y,z)
        obj=box(name,pos,size,key,0.008,collision);obj.rotation_euler.z=angle;return obj
    leaf('SM_PROP_OpenDormDoor',(0.45,0,1.01),(0.9,0.045,2.02),collision='box')
    for z,h in [(0.48,0.66),(1.40,0.74)]:leaf('SM_PROP_DoorRaisedPanel',(0.45,-0.03,z),(0.68,0.022,h))
    leaf('SM_PROP_DoorHandlePlate',(0.77,-0.055,1.02),(0.035,0.018,0.17),'Metal')
    leaf('SM_PROP_DoorHandle',(0.80,-0.078,1.06),(0.13,0.025,0.022),'Metal')
    for y,side,num in [(7.1,1,'02'),(11.1,-1,'03'),(15.4,1,'04')]:
        x=side*0.89
        box('SM_PROP_HallClosedDoor',(x,y,1.03),(0.04,0.88,2.06),'Wood',0.009)
        for edge in [-0.48,0.48]:box('SM_PROP_HallDoorJamb',(x-side*0.02,y+edge,1.05),(0.075,0.06,2.1),'Wood',0.008)
        box('SM_PROP_HallDoorTop',(x-side*0.02,y,2.10),(0.075,1.02,0.08),'Wood',0.008)
        ellipsoid('SM_PROP_HallDoorKnob',(x-side*0.05,y+0.3,1.0),(0.028,0.028,0.028),'Metal')
    # Repaired utility cupboards sit within the two cover pockets.
    for x,y in [(-1.44,9.2),(1.44,13.1)]:
        box('SM_PROP_UtilityCabinet',(x,y,0.82),(0.36,0.95,1.64),'Metal',0.018,'box')
        for yy in [y-0.23,y+0.23]:
            box('SM_PROP_CabinetDoor',(x-math.copysign(0.19,x),yy,0.86),(0.018,0.43,1.5),'Metal',0.006)
        for z in [0.19,0.29,1.31,1.41]:
            box('SM_PROP_CabinetVent',(x-math.copysign(0.204,x),y,z),(0.008,0.55,0.013),'Ink',0.001)
    # Restrained plaster chips along wall edges and a few fallen papers.
    for i in range(24):
        y=RNG.uniform(0.4,4.6);x=-1.995 if i%2 else 1.995
        chip=box('SM_PROP_WallFlake',(x,y,RNG.uniform(0.04,0.45)),(0.008,RNG.uniform(0.03,0.1),RNG.uniform(0.02,0.08)),'Paper',0.002)
    for i in range(9):
        x=RNG.choice([-0.62,0.62])+RNG.uniform(-0.10,0.10);y=RNG.uniform(6.2,17.8)
        paper=box('SM_PROP_FallenPaper',(x,y,0.008),(0.16,0.22,0.003),'Paper',0.001)
        paper.rotation_euler.z=RNG.uniform(-0.8,0.8)
    # Damp floor variation is carried by the roughness map, avoiding floating oval geometry.

def frame_view():
    for area in bpy.context.screen.areas:
        if area.type=='VIEW_3D':
            region=area.spaces.active.region_3d
            region.view_location=Vector((0.1,3.1,1.65))
            region.view_rotation=Vector((0,1,0)).to_track_quat('-Z','Y')
            region.view_distance=2.0;region.view_perspective='PERSP'
            area.spaces.active.shading.type='MATERIAL'

def export_assets(reviewed=False):
    EXPORT.parent.mkdir(parents=True,exist_ok=True)
    if (EXPORT.exists() or SOURCE.exists()) and not reviewed:
        raise RuntimeError('Atmosphere export already exists; review before replacing production output.')
    # Convert only geometry in the new working collection. Other Blender scenes remain untouched.
    for obj in SCENE.objects:obj.select_set(False)
    for obj in COLLECTION.objects:obj.select_set(True)
    bpy.context.view_layer.objects.active=next(iter(COLLECTION.objects))
    bpy.ops.object.convert(target='MESH')
    soft_names=('SoftPillow','Shoe','FanMotor','FanBlade','LampShade','LampBase','LampBulb','FoldedUmbrella','StudentBag','WrinkledBlanket')
    for obj in COLLECTION.objects:
        if obj.type=='MESH' and any(n in obj.name for n in soft_names):
            for polygon in obj.data.polygons:polygon.use_smooth=True
    parts=[];triangles=0
    graph=bpy.context.evaluated_depsgraph_get()
    for obj in COLLECTION.objects:
        if obj.type!='MESH':continue
        parts.append({'name':obj.name,'material':obj.data.materials[0].name,'collision':obj.get('nra_collision','none')})
        evaluated=obj.evaluated_get(graph);mesh=evaluated.to_mesh();mesh.calc_loop_triangles()
        triangles+=len(mesh.loop_triangles);evaluated.to_mesh_clear()
    materials=[]
    for key,(color,metal,smooth) in SPECS.items():
        textured=key in ['Wall','Floor','Wood','Cloth','Metal','Paper']
        emission=[c*1.6 for c in color] if key.startswith('Lamp') else [c*0.12 for c in color] if key=='Glass' else [0,0,0]
        materials.append({'name':'MAT_H_'+key,'texturePrefix':key if textured else '',
            'baseColor':[1,1,1,1] if textured else list(color)+[1],
            'metallic':metal,'smoothness':smooth,'emission':emission})
    lights=[
        {'name':'Room Fluorescent','position':[0,2.40,2.60],'color':[0.65,0.72,0.63],'intensity':0.75,'range':4.3},
        {'name':'Desk Lamp','position':[1.61,1.10,3.70],'color':[1.0,0.64,0.34],'intensity':2.5,'range':2.2},
        {'name':'Window Spill','position':[-1.86,1.80,3.4],'color':[0.20,0.43,0.68],'intensity':1.2,'range':4.0},
        {'name':'Corridor Near','position':[0,2.40,6.30],'color':[0.64,0.71,0.53],'intensity':0.8,'range':3.5},
        {'name':'Corridor Middle','position':[0,2.40,10.8],'color':[0.72,0.45,0.26],'intensity':0.55,'range':3.4},
        {'name':'Corridor Far','position':[0,2.40,17.1],'color':[0.25,0.42,0.60],'intensity':0.65,'range':3.5}
    ]
    manifest={'parts':parts,'materials':materials,'lights':lights,'spawn':[0.15,0.04,1.1],
        'meshCount':len(parts),'triangles':triangles,'roomMetres':[4,5,2.7],'corridorMetres':[1.8,14,2.7],
        'source':'Blender 5.2.3 LTS, original geometry; wall and wood albedos from built-in imagegen, other maps procedural'}
    MANIFEST.write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    bpy.ops.export_scene.fbx(filepath=str(EXPORT),use_selection=True,object_types={'MESH'},apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,
        add_leaf_bones=False,bake_anim=False,path_mode='AUTO',use_custom_props=True)
    bpy.data.images['Wall_BaseColor'].reload()
    bpy.ops.file.pack_all()
    frame_view()
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE),copy=True,compress=True)
    print(json.dumps({'source':str(SOURCE),'export':str(EXPORT),'manifest':str(MANIFEST),'meshes':len(parts),'triangles':triangles}))

def rain_audio():
    path=ROOT/'Assets/_NoRainbowAfter/Audio/Ambience/AUD_KosRain.wav'
    path.parent.mkdir(parents=True,exist_ok=True)
    rate=22050;size=rate*12;rng=np.random.default_rng(142)
    noise=rng.standard_normal(size)
    soft=np.convolve(noise,np.ones(9)/9,mode='same')
    low=np.convolve(noise,np.ones(151)/151,mode='same')
    signal=soft*0.36+low*0.75
    for start in rng.integers(0,size-3000,80):
        count=2400;t=np.arange(count)/rate
        drop=np.sin(2*np.pi*(250+rng.random()*900)*t)*np.exp(-t*(30+rng.random()*45))
        signal[start:start+count]+=drop*rng.uniform(0.025,0.06)
    edge=400
    signal[:edge]*=np.linspace(0,1,edge);signal[-edge:]*=np.linspace(1,0,edge)
    signal/=max(1,np.max(np.abs(signal))*1.2)
    with wave.open(str(path),'wb') as audio:
        audio.setnchannels(1);audio.setsampwidth(2);audio.setframerate(rate)
        audio.writeframes((signal*32767).astype('<i2').tobytes())
    print(json.dumps({'originalAudio':str(path),'durationSeconds':12,'rate':rate}))

if globals().get('PHASE',1)==1:
    setup();structure();frame_view()
    print(json.dumps({'stage':'structure','scene':SCENE.name,'objects':len(COLLECTION.objects),'textures':len(list(TEXDIR.glob('*.png')))}))
elif globals().get('PHASE')==2:
    attach();furniture();dressing();frame_view()
    bpy.data.images['Wall_BaseColor'].reload()
    print(json.dumps({'stage':'props','scene':SCENE.name,'objects':len(COLLECTION.objects)}))
elif globals().get('PHASE')==3:
    attach();rain_audio();export_assets()
elif globals().get('PHASE')==4:
    attach()
    for obj in list(COLLECTION.objects):
        if obj.name.startswith('SM_PROP_WetPatch'):bpy.data.objects.remove(obj,do_unlink=True)
    for name in ['Wall_BaseColor','Wood_BaseColor']:
        image=bpy.data.images[name]
        if image.packed_file:image.unpack(method='REMOVE')
        image.reload();image.pack()
    export_assets(reviewed=True)
