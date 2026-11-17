"""Build the editable, multi-view Blender interpretation of the locked Raka turnaround.

Run with Blender in background mode, using --python and this file. Coordinates in
the pattern are pixels on the original front and left-profile drawings. The
turnaround supplies the surface colour. Four fixed UV projections follow the mesh
when posed. Geometry and rig depth are authored in the shared profile space.
"""
from pathlib import Path
import math
import random
import tempfile

import bpy
import numpy as np
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "BlenderSources/Characters/RakaTurnaround"
PREVIEWS = ROOT / "Docs/QA/RakaTurnaround"
OUTPUT.mkdir(exist_ok=True)
PREVIEWS.mkdir(exist_ok=True)
REF = next((ROOT / "Docs/References").glob("*Turnaround.png"))
UNIT = 2.34 / 845
rng = random.Random(173)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
tau = math.tau


def collection(name):
    group = bpy.data.collections.new(name)
    scene.collection.children.link(group)
    return group


body = collection("01  Anatomy")
outfit = collection("02  Garments and harness")
hair = collection("03  Hair and head wraps")
studio = collection("04  Studio")
guides = collection("05  Turnaround guides")
source = bpy.data.images.load(str(REF))
source.pack()
width, height = source.size[:]
# Extend object colours beneath the light-grey sheet background. This prevents a
# pale fringe when interpolating views at an edge. Do this in linear RGB.
pixels = np.empty(width * height * 4, dtype=np.float32)
source.pixels.foreach_get(pixels)
pixels = pixels.reshape(height, width, 4)
rgb = pixels[:, :, :3]
mask = (rgb.max(axis=2) < .115) | ((rgb[:, :, 0] - rgb[:, :, 2]) > .022)
for _ in range(27):
    extended = mask.copy()
    for dx, dy in [(0, 1), (0, -1), (1, 0), (-1, 0)]:
        fill = np.roll(mask, (dy, dx), (0, 1)) & ~extended
        rgb[fill] = np.roll(rgb, (dy, dx), (0, 1))[fill]
        extended |= fill
    mask = extended
atlas = bpy.data.images.new("Turnaround | padded colour", width, height, alpha=False)
atlas.pixels.foreach_set(pixels.ravel())
with tempfile.TemporaryDirectory() as tmp:
    atlas.filepath_raw = str(Path(tmp) / "Raka_Colour.png")
    atlas.file_format = "PNG"
    atlas.save()
    baked = Path(atlas.filepath_raw).read_bytes()
    bpy.data.images.remove(atlas)
    atlas = bpy.data.images.load(str(Path(tmp) / "Raka_Colour.png"))
    atlas.name = "Turnaround | padded colour"
    atlas.filepath = "//Raka_Colour.png"
    atlas.pack(data=baked, data_len=len(baked))


def projected_material(name, rough, spec):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (.17, .10, .075, 1)
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    shader = nodes.get("Principled BSDF")
    shader.inputs["Roughness"].default_value = rough
    shader.inputs["Specular IOR Level"].default_value = spec
    # Normalised pairwise factors interpolate four calibrated projections.
    weights = nodes.new("ShaderNodeAttribute")
    weights.attribute_name = "ViewBlend"
    split = nodes.new("ShaderNodeSeparateColor")
    links.new(weights.outputs["Color"], split.inputs[0])
    samples = []
    for i, label in enumerate(["Front", "Back", "Left", "Right"]):
        uv = nodes.new("ShaderNodeUVMap")
        uv.uv_map = label
        tex = nodes.new("ShaderNodeTexImage")
        tex.image = atlas
        uv.location = (-920, 240 - 260*i)
        tex.location = (-690, 240 - 260*i)
        links.new(uv.outputs[0], tex.inputs[0])
        samples.append(tex.outputs[0])
    weights.location = (-400, 450)
    split.location = (-180, 450)
    for i, (a, b) in enumerate([(0, 1), (2, 3), (4, 5)]):
        blend = nodes.new("ShaderNodeMixRGB")
        blend.location = (40 + (i == 2)*220, 150 - (i == 1)*220)
        links.new(split.outputs[i], blend.inputs[0])
        links.new(samples[a], blend.inputs[1])
        links.new(samples[b], blend.inputs[2])
        samples.append(blend.outputs[0])
    color = samples[-1]
    links.new(color, shader.inputs["Base Color"])
    # Preserve illustrative detail in the shadows while allowing actual relighting.
    links.new(color, shader.inputs["Emission Color"])
    shader.inputs["Emission Strength"].default_value = .17
    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = .17
    bump.inputs["Distance"].default_value = .0015
    bump.location = (480, -220)
    links.new(color, bump.inputs["Height"])
    links.new(bump.outputs["Normal"], shader.inputs["Normal"])
    shader.location = (700, 100)
    nodes.get("Material Output").location = (1040, 100)
    return mat


skin = projected_material("01 | Weathered skin", .55, .16)
cloth = projected_material("02 | Worn linen and pleated wool", .88, .09)
leather = projected_material("03 | Leather, rivets and iron", .52, .17)
paper = projected_material("04 | Marked paper", .84, .09)
locks = projected_material("05 | Wet brown-black hair", .46, .16)


def world(p):
    x, depth, row = p
    return ((x-230)*UNIT, (depth-547)*UNIT, (865-row)*UNIT)


def mirror(p):
    return (460-p[0], p[1], p[2])


def sample(rows, z):
    """Linear interpolation between traced sections of a garment or limb."""
    if z <= rows[0][0]:
        return np.asarray(rows[0][1:], float)
    for a, b in zip(rows, rows[1:]):
        if z <= b[0]:
            t = (z-a[0]) / (b[0]-a[0])
            return np.asarray(a[1:])*(1-t) + np.asarray(b[1:])*t
    return np.asarray(rows[-1][1:], float)


def coords(rows, z, angle, relief=0):
    lo, hi, front, back = sample(rows, z)
    return ((lo+hi)/2 + ((hi-lo)/2+relief)*math.cos(angle),
            (front+back)/2 + ((back-front)/2+relief)*math.sin(angle), z)


# The two profiles were painted separately. Register the right profile at the
# head, elbow, knee and toes, and the back at each limb before interpolation.
right_axis = [(20, 1680), (126, 1683), (220, 1682), (340, 1685),
              (475, 1686), (610, 1680), (790, 1678), (865, 1682)]
rear_axis = [(20,1064), (150,1065), (245,1066), (345,1064), (485,1063),
             (600,1066), (800,1063), (865,1063)]
parts = []


def object_mesh(name, points, faces, angles, mat, group, bind, profile_bias=1,
                rear_offset=0, profile_offset=0):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([world(p) for p in points], [], faces)
    mesh.materials.append(mat)
    obj = bpy.data.objects.new(name, mesh)
    group.objects.link(obj)
    for poly in mesh.polygons:
        poly.use_smooth = True
    pts = np.asarray(points)
    x, d, z = pts.T
    rear = np.interp(z, *np.asarray(rear_axis).T) - x + rear_offset
    other = np.interp(z, *np.asarray(right_axis).T) - d - profile_offset
    corners = np.empty(len(mesh.loops), np.int32)
    mesh.loops.foreach_get("vertex_index", corners)
    for label, column in zip(("Front", "Back", "Left", "Right"),
                             (x, rear, d+profile_offset, other)):
        uvs = np.column_stack(((column+.5)/width, 1-(z+.5)/height))
        mesh.uv_layers.new(name=label).data.foreach_set("uv", uvs[corners].astype("f").ravel())
    a = np.asarray(angles)
    # Front/back retain priority across chest and face; transition on flanks.
    side_weight = np.abs(np.cos(a))**5 * profile_bias
    axial_weight = np.abs(np.sin(a))**3
    mix = side_weight / np.maximum(.0001, side_weight+axial_weight)
    weight = np.column_stack((np.sin(a)>0, np.cos(a)<0, mix, np.ones(len(a))))
    mesh.color_attributes.new(name="ViewBlend", type="FLOAT_COLOR", domain="POINT").data.foreach_set(
        "color", weight.astype("f").ravel())
    parts.append((obj, bind, z.copy()))
    return obj


def loft(name, rows, mat, group, bind, *, segments=48, step=4, bias=1,
         sculpt=None, reverse=False, rear_offset=0, open_hem=False):
    points, angles, faces = [], [], []
    levels = [rows[0][0]]
    for first, last in zip(rows, rows[1:]):
        n = math.ceil((last[0]-first[0])/step)
        levels.extend(np.linspace(first[0], last[0], n+1)[1:])
    for j, z in enumerate(levels):
        for k in range(segments):
            a = tau*k/segments
            p = coords(rows, z, a)
            if sculpt:
                p = sculpt(*p, a, (z-levels[0])/(levels[-1]-levels[0]))
            points.append(mirror(p) if reverse else p)
            angles.append(math.pi-a if reverse else a)
            if j:
                i = j*segments+k
                h = j*segments+(k+1)%segments
                faces.append((i-segments, i, h, h-segments))
    faces.append(tuple(range(segments)))
    if not open_hem:
        faces.append(tuple(reversed(range(len(points)-segments,len(points)))))
    if reverse:
        faces = [tuple(reversed(f)) for f in faces]
    return object_mesh(name, points, faces, angles, mat, group, bind, bias, rear_offset)


def grid(name, rows, mat, group, bind, bias=1):
    points, angles, faces = [], [], []
    count = len(rows[0])
    for j, row in enumerate(rows):
        for k, (p, a) in enumerate(row):
            points.append(p)
            angles.append(a)
            i = j*count+k
            if j and k:
                faces.append((i-count-1, i-1, i, i-count))
    return object_mesh(name, points, faces, angles, mat, group, bind, bias)


torso = [(102,209,240,511,546),(120,211,255,520,559),(132,205,268,524,568),(139,179,287,521,577),
 (152,160,300,514,580),(173,158,302,509,581),(193,165,298,506,579),
 (213,173,292,500,573),(235,177,288,501,568),(254,186,281,504,561),
 (272,192,278,504,558),(287,192,280,497,561),(304,188,284,493,566),
 (325,184,290,491,569),(349,182,290,489,573),(376,185,283,490,566)]
head = [(20,202,209,489,496),(21,198,214,482,505),(24,190,220,475,510),(30,183,228,467,518),
 (43,175,235,459,526),(58,172,240,455,529),(72,174,241,456,528),
 (86,179,240,461,527),(99,188,238,469,523),(107,201,235,484,520),
 (118,209,232,492,514),(126,216,226,499,508)]
def face_sculpt(x,d,z,a,t):
    f=max(0,-math.sin(a))**3
    nose=math.exp(-((x-210)/6)**2-((z-97)/6)**2)
    lips=math.exp(-((x-216)/8)**2-((z-111)/2.2)**2)
    return x,d-f*(4*nose+2*lips),z
loft("Face | bowed jaw, nose and brow",head,skin,body,"Head",sculpt=face_sculpt,step=2)
def muscle(x,d,z,a,t):
    f=max(0,-math.sin(a))**2
    ridge=math.cos((z-.32*abs(x-231))/5)*math.exp(-((z-220)/54)**2)
    sternum=1.6*math.exp(-((x-232)/5)**2)
    return x,d+(sternum-1.8*ridge)*f,z
loft("Trunk | clavicles, ribs and abdomen",torso,skin,body,"trunk",sculpt=muscle,bias=.29)
arm = [(146,155,178,544,567),(154,145,184,538,572),(166,139,184,536,574),
 (180,139,179,535,573),(198,139,174,540,569),(219,141,168,544,571),
 (242,139,160,546,569),(271,132,154,548,573),(294,126,148,550,574),
 (311,118,143,552,576),(324,113,140,547,573),(340,105,136,540,570),
 (365,98,126,531,561),(395,89,115,522,547),(425,80,101,513,533),
 (453,72,88,504,522),(473,65,82,499,514),(486,63,80,496,514)]
leg = [(362,176,226,504,567),(409,171,220,500,563),(460,171,216,508,566),
 (487,174,215,513,561),(508,175,214,514,555),(530,179,214,513,552),
 (551,183,214,515,550),(568,181,214,517,551),(585,181,211,521,554),
 (601,176,204,524,560),(621,170,203,525,567),(647,165,203,526,573),
 (670,164,202,529,571),(700,166,198,532,568),(733,170,194,535,566),
 (764,171,191,538,564),(789,171,190,540,566),(807,165,197,537,572),
 (820,165,199,528,574),(838,167,199,509,573)]
palm = [(461,69,84,501,518),(467,67,82,500,518),(477,62,81,500,514),(492,61,84,493,513),
 (507,61,88,483,518),(521,59,90,478,520),(531,58,81,479,521),(539,60,77,482,518),(544,63,73,489,511)]
# Radial and ulnar digits curl independently. Their depth spacing separates
# fingers which overlap almost completely in the front drawing.
digits = [
 [(81,488,513,7),(87,481,525,6),(94,479,539,4.8),(98,477,553,3.3),(101,476,563,.65)],
 [(73,490,531,5.8),(72,486,553,5.3),(76,485,569,4.1),(89,484,577,3),(100,485,580,.6)],
 [(69,500,532,6),(67,497,555,5.3),(70,496,573,4.7),(83,495,588,3.4),(96,495,594,2.6),(108,496,598,.55)],
 [(65,508,533,5.2),(62,510,548,5.2),(67,510,570,4.3),(76,509,589,3.9),(88,509,601,2.9),(99,510,608,.55)],
 [(63,516,531,4.8),(59,519,544,4.3),(64,522,561,3.7),(70,522,577,3.1),(80,522,591,2.5),(91,522,600,.55)]]
foot = [(801,166,194,540,568),(812,166,196,535,573),(828,164,198,522,579),
 (841,160,202,501,582),(850,156,203,484,582),(856,155,205,477,580),
 (862,156,205,476,575),(864,161,202,482,558)]
def tendon(x,d,z,a,t):
    swell=1.0*math.sin(5*a+.018*z)+.65*math.sin(8*a-.013*z)
    return x+swell*math.cos(a),d+swell*math.sin(a),z
digit_mat=skin.copy()
digit_mat.name="08 | Fingers and dark nails"
nodes=digit_mat.node_tree.nodes
links=digit_mat.node_tree.links
shader=nodes.get("Principled BSDF")
pattern=shader.inputs["Base Color"].links[0].from_socket
pigment=nodes.new("ShaderNodeAttribute")
pigment.attribute_name="Phalanx"
mix=nodes.new("ShaderNodeMixRGB")
mix.inputs[0].default_value=.84
links.new(pattern,mix.inputs[1])
links.new(pigment.outputs["Color"],mix.inputs[2])
links.new(mix.outputs[0],shader.inputs["Base Color"])
links.new(mix.outputs[0],shader.inputs["Emission Color"])
for suffix,rev in [("R",False),("L",True)]:
    loft("Arm."+suffix,arm,skin,body,"arm."+suffix,reverse=rev,sculpt=tendon)
    loft("Palm."+suffix,palm,skin,body,"Hand."+suffix,reverse=rev,step=2)
    for n, path in enumerate(digits):
        if n:
            x,d,z,r=path[0]
            path=[((x+71)/2,(d+501)/2,514,r*.85)]+path
        rows=[(z,x-r,x+r,d-r*.90,d+r*.90) for x,d,z,r in path]
        def finger_section(x,d,z,a,t):
            # Orient the cross-section to the direction of a curling digit.
            # Horizontal sections collapse to knife edges at the fingertips.
            lo,hi,_,_=sample(rows,z)
            cx=(lo+hi)/2
            before=sample(rows,z-1)[:2].mean()
            after=sample(rows,z+1)[:2].mean()
            slope=(after-before)/max(.01,min(z+1,rows[-1][0])-max(z-1,rows[0][0]))
            tilt=math.atan(slope)
            return cx+(x-cx)*math.cos(tilt),d,z-(x-cx)*math.sin(tilt)
        finger=loft(["Thumb","Index","Middle","Ring","Little"][n]+"."+suffix,rows,
             digit_mat,body,"Hand."+suffix,segments=18,step=2,reverse=rev,sculpt=finger_section)
        # The fingers occlude one another in the side drawings. Use hand-painted
        # longitudinal tones and dorsal nails in that occluded region.
        tones=[]
        for k,level in enumerate(parts[-1][2]):
            t=np.clip((level-path[0][2])/(path[-1][2]-path[0][2]),0,1)
            az=tau*(k%18)/18
            ridge=(.88+.16*math.cos(az-.6)+.08*math.cos(4*az+2*t))
            crease=sum(.22*math.exp(-((t-j)/.024)**2) for j in (.29,.61))
            nail=np.clip((t-.75)/.19,0,1)*np.clip((-.1-math.cos(az))*1.7,0,1)
            value=(ridge-crease)*(1-.57*nail)
            tones.append((.099*value,.061*value,.047*value,1))
        finger.data.color_attributes.new(name="Phalanx",type="FLOAT_COLOR",domain="POINT").data.foreach_set(
            "color",np.asarray(tones,dtype="f").ravel())
    loft("Leg."+suffix,leg,skin,body,"leg."+suffix,reverse=rev,sculpt=tendon)
    loft("Heel and instep."+suffix,foot,skin,body,"Foot."+suffix,reverse=rev)
    for n,(x,d,r) in enumerate([(199,454,6.4),(188,457,5.4),(178,461,4.9),
                                  (169,466,4.3),(159,472,3.9)]):
        rows=[(847,x-2,x+2,d+18,d+37),(853,x-r*.8,x+r*.8,d+6,d+42),
              (858,x-r,x+r,d,d+44),(862,x-r,x+r,d+1,d+39),(865,x-2,x+2,d+4,d+26)]
        loft("Toe %d.%s"%(n+1,suffix),rows,skin,body,"Foot."+suffix,
             reverse=rev,segments=16,step=2)

skirt = [(289,191,280,499,564),(302,185,284,493,570),(325,177,292,488,579),
 (346,168,296,485,583),(373,159,301,481,590),(400,154,304,478,594),
 (430,151,307,477,598),(462,152,309,480,597),(502,156,310,483,595)]
def pleats(x,d,z,a,t):
    fold=(2.0*math.cos(26*a+.007*z)+.85*math.sin(45*a-.014*z))*min(1,.28+3*t)
    # Two deep, unequal notches and short frayed teeth at the hem.
    # Trace the actual upper hem across each thigh and the lower flaps in
    # the gaps. Without this distinction a single cylindrical skirt paints
    # parts of the legs onto fabric at oblique viewing angles.
    front_edge=[(150,464),(158,471),(173,444),(186,434),(198,437),
                (210,446),(219,436),(223,503),(240,506),(243,449),
                (252,459),(272,454),(285,465),(295,519),(309,501)]
    back_edge=[(150,496),(160,469),(176,465),(188,477),(203,481),
               (213,478),(219,493),(230,505),(241,487),(253,470),
               (274,471),(288,478),(298,520),(310,495)]
    edge=front_edge if math.sin(a)<0 else back_edge
    endpoint=np.interp(233+76*math.cos(a),*np.asarray(edge).T)
    tip=endpoint-502+4*math.sin(53*a)+3*math.cos(87*a)+2*math.sin(109*a)
    return x+fold*math.cos(a), d+fold*math.sin(a), z+tip*t**5
lining=cloth.copy()
lining.name="07 | Plaid and dark inner lining"
nodes=lining.node_tree.nodes
links=lining.node_tree.links
outer=nodes.get("Principled BSDF")
inner=nodes.new("ShaderNodeBsdfDiffuse")
inner.inputs["Color"].default_value=(.022,.014,.011,1)
facing=nodes.new("ShaderNodeNewGeometry")
sided=nodes.new("ShaderNodeMixShader")
links.new(facing.outputs["Backfacing"],sided.inputs[0])
links.new(outer.outputs[0],sided.inputs[1])
links.new(inner.outputs[0],sided.inputs[2])
links.new(sided.outputs[0],nodes.get("Material Output").inputs[0])
loft("Kilt | torn pleated plaid",skirt,lining,outfit,"Pelvis",segments=208,
     step=5,sculpt=pleats,bias=.6,open_hem=True)


def band(name, form, center, thickness, lift, mat, bind, group=outfit):
    ring=[]
    for j in range(6):
        line=[]
        for k in range(129):
            a=tau*k/128
            z=center(a)+(j/5-.5)*thickness
            p=coords(form,z,a,lift + .5*math.sin(math.pi*j/5))
            line.append((p,a))
        ring.append(line)
    return grid(name,ring,mat,group,bind)


band("Waist | upper buckled strap",torso,lambda a:298+3*math.cos(a)+5*math.sin(a),14,3,leather,"Pelvis")
band("Hip | lower buckled strap",skirt,lambda a:338+5*math.cos(a)-3*math.sin(2*a),14,3,leather,"Pelvis")
band("Chest | slanted girth",torso,lambda a:235-12*math.cos(a)+4*math.sin(a),14,3,leather,"trunk")
band("Right thigh | garter",leg,lambda a:484+2*math.cos(a),15,2,leather,"Thigh.R")
guard=[(616,258,294,522,567),(624,257,297,521,572),(644,258,299,525,575),
 (674,261,295,528,574),(714,265,292,532,570),(750,267,289,536,567),
 (759,268,288,538,565)]
loft("Left greave | shaped leather",guard,leather,outfit,"Shin.L",step=3)
for z,w in [(630,12),(746,7)]:
    band("Left greave | fastening %d"%z,guard,lambda a,z=z:z-3*math.cos(a),w,2,leather,"Shin.L")


def panel(name,form,keys,mat,bind,*,lift=2,rag=0,group=outfit,bias=.6):
    # Each key defines the left/right angular edges at one height.
    rows=[]
    for first,last in zip(keys,keys[1:]):
        for z in np.linspace(first[0],last[0],max(2,math.ceil((last[0]-first[0])/3)),endpoint=False):
            left,right=sample(keys,z)
            rows.append((z,left,right))
    rows.append(keys[-1])
    surface=[]
    for z,left,right in rows:
        row=[]
        for j in range(31):
            w=j/30
            a=math.radians(left+(right-left)*w)
            t=(z-keys[0][0])/(keys[-1][0]-keys[0][0])
            zz=z+rag*t**4*(.5*math.sin(j*.9)+.7*math.cos(j*1.52)+.18*math.sin(j*2.7))
            p=coords(form,zz,a,lift+.8*math.sin(w*math.pi)+rag*.16*math.cos(4*math.pi*w))
            row.append((p,a))
        surface.append(row)
    return grid(name,surface,mat,group,bind,bias)


panel("Harness | shoulder to left waist",torso,
 [(142,-163,-143),(163,-152,-133),(202,-128,-111),(240,-104,-89),(270,-83,-66),(291,-63,-42)],
 leather,"trunk",lift=2.5)
for sign in [-1,1]:
    keys=[(138,36,53),(170,48,65),(200,67,84),(228,88,105),(263,118,136),(286,138,157)]
    if sign<0:
        keys=[(z,180-r,180-l) for z,l,r in keys]
    panel("Back harness | cross "+str(sign),torso,keys,leather,"trunk",lift=3+(sign+1)*.7)
# Independent shaped linen panels expose the marked chest and bare shoulders.
panel("Linen | open right bodice",torso,[(147,-158,-147),(166,-187,-144),
 (197,-191,-130),(230,-183,-111),(253,-178,-120),(281,-172,-144)],cloth,"trunk",rag=6)
panel("Linen | left flank",torso,[(129,-33,33),(148,-38,37),(174,-22,53),
 (211,-26,62),(240,-45,66),(275,-42,65),(294,-37,58)],cloth,"trunk",rag=6)
shoulder=[(103,272,280,547,555),(111,270,292,537,562),(122,275,307,528,570),
 (134,281,321,523,580),(147,288,328,522,585),(165,292,334,523,585),
 (183,297,337,525,582),(205,302,336,528,578),(215,303,336,529,578)]
for n,(end,left,right) in enumerate([(150,-175,110),(178,-154,128),(201,-132,102),(211,-85,61)]):
    panel("Left shoulder | torn mantle %02d"%n,shoulder,[(108+n*5,left+20,right-12),
          (134+n*3,left,right),(end,left+5,right-5)],cloth,"UpperArm.L",rag=6, lift=2.4-n*.4)
sleeve=[(185,142,175,533,575),(200,138,172,530,576),(216,136,169,529,577),
        (231,132,168,527,574),(247,131,166,525,573)]
panel("Right sleeve | split linen",sleeve,[(188,44,184),(202,25,230),(217,-8,269),(238,-42,295)],
      cloth,"UpperArm.R",rag=8)
for i,keys in enumerate([
 [(265,-138,-106),(285,-146,-111),(315,-147,-112)],
 [(308,-75,-46),(330,-78,-43),(365,-76,-42)],
 [(321,-170,-142),(343,-178,-145),(376,-183,-153)],
 [(349,-44,-22),(376,-44,-17),(414,-38,-10)],
 [(343,46,70),(374,40,67),(414,37,68)],
 [(331,115,143),(361,119,144),(408,125,148)]]):
    form=torso if i==0 else skirt
    panel("Coursework slip | %02d"%(i+1),form,keys,paper,"Pelvis",lift=5,rag=1.4)
# The crown and blindfold follow the same bowed skull. The wraps overlap in depth
# and tilt in opposite directions, revealing the nose, mouth and triangular jaw.
for n,(z,tilt,width_) in enumerate([(60,-13,10),(71,-16,12),(80,-15,11),(85,-12,9)]):
    band("Eyes | layered wrap %d"%(n+1),head,
         lambda a,z=z,tilt=tilt:z+tilt*math.cos(a)-2*math.sin(2*a),width_,1.1+n*.22,cloth,"Head",hair)
for n,keys in enumerate([[(39,-18,15),(53,-8,20),(64,2,23)],
                          [(64,154,181),(81,151,173),(101,148,159)]]):
    panel("Temple note | %d"%(n+1),head,keys,paper,"Head",group=hair,lift=3.5)
# Broad locks form a continuous nape mass. Their front edges terminate behind
# the wrapped temples. A middle part and wisps break up the silhouette.
mane = [(20,203,208,489,496),(21,198,213,484,505),(25,187,222,473,513),(40,177,235,462,527),
 (58,171,243,454,540),(83,171,249,455,553),(108,171,261,468,573),
 (133,168,279,489,593),(156,161,294,512,608),(180,157,303,536,617),
 (208,155,312,550,623),(237,155,317,558,621),(274,158,315,564,615),
 (316,156,309,566,610),(351,153,301,575,602)]
def hair_edge(a):
    # Forehead hairline, hanging temple locks, and longer layers at the nape.
    return (55 + 118*(.5+.5*math.sin(a))**.49
            + 39*abs(math.cos(a))**5 + 8*math.sin(7*a))
def mane_strip(index, count, flyaway=False):
    a0=tau*index/count
    end=hair_edge(a0)
    # Long trailing locks cluster at the back and around the temples.
    if flyaway:
        end += rng.uniform(12,140)*(max(0,math.sin(a0))+.25)
    if math.sin(a0)<-.18:
        reveal=np.clip((-math.sin(a0)-.18)/.32,0,1)
        end=end*(1-reveal)+min(end,61+10*math.cos(a0))*reveal
    start=20+rng.uniform(0,.8)
    width_a=tau/count*(1.24 if not flyaway else .52)
    wave=rng.uniform(0,tau)
    rows=[]
    for t in np.linspace(0,1,34):
        z=start+(end-start)*t
        a=a0+.11*math.sin(5*t+wave)*t+.13*math.sin(a0)*t
        taper=1-.98*t**4
        line=[]
        for w in np.linspace(-1,1,5):
            aa=a+w*width_a*.5*taper
            disp=1.3 + (1-w*w)*1.9 + (1.2 if flyaway else 0) + 1.5*t*math.sin(t*8+wave)
            x,d,zz=coords(mane,z,aa,disp)
            # From below the skull onward, the hair follows the side of the neck;
            # the central rear locks hang down along the spine.
            if z>103:
                blend=min(1,(z-103)/56)
                if math.sin(aa)<0:
                    x+=(8 if math.cos(aa)<0 else -11)*blend
                if math.sin(aa)>0:
                    x+=(229-x)*.62*blend
            line.append(((x,d,zz),aa))
        rows.append(line)
    return rows
# Merge strands into two editable meshes to keep the outliner manageable.
def combined_strips(name,strips):
    points,angles,faces=[],[],[]
    for strip in strips:
        start=len(points)
        cols=len(strip[0])
        for j,row in enumerate(strip):
            for k,(p,a) in enumerate(row):
                points.append(p)
                angles.append(a)
                i=start+j*cols+k
                if j and k:
                    faces.append((i-cols-1,i-1,i,i-cols))
    return object_mesh(name,points,faces,angles,locks,hair,"Head",.75)
combined_strips("Hair | parted crown and heavy locks",[mane_strip(i,82) for i in range(82)])
combined_strips("Hair | loose tapered wisps",[mane_strip(i,127,True) for i in range(127)])


def fitted_front(form,x,z,offset):
    lo,hi,_,_=sample(form,z)
    angle=-math.acos(np.clip((2*x-lo-hi)/(hi-lo),-.999,.999))
    _,depth,_=coords(form,z,angle)
    return (x,depth-offset,z),angle


def piping(name,form,outline,radius,mat,bind):
    # Oval wire section for independently editable buckle frames.
    points,angles,faces=[],[],[]
    for i,(x,z) in enumerate(outline):
        before=outline[max(0,i-1)]
        after=outline[min(i+1,len(outline)-1)]
        dx,dz=after[0]-before[0],after[1]-before[1]
        length=max(.1,math.hypot(dx,dz))
        for j in range(8):
            a=tau*j/8
            xx=x-radius*math.sin(a)*dz/length
            zz=z+radius*math.sin(a)*dx/length
            pt,view=fitted_front(form,xx,zz,5.2+radius*math.cos(a))
            points.append(pt)
            angles.append(view)
            if i:
                k=i*8+j
                h=i*8+(j+1)%8
                faces.append((k-8,k,h,h-8))
    return object_mesh(name,points,faces,angles,mat,outfit,bind,.18)


for name,form,x,z,w,h,tilt,bind in [
    ("Chest",torso,260,235,17,17,-.14,"trunk"),
    ("Waist",torso,232,298,15,14,.03,"Pelvis"),
    ("Hip",skirt,217,341,15,14,.05,"Pelvis"),
    ("Thigh",leg,176,485,10,13,-.10,"Thigh.R")]:
    outline=[(-w/2+1,-h/2),(w/2-1,-h/2),(w/2,-h/2+1),(w/2,h/2-1),
             (w/2-1,h/2),(-w/2+1,h/2),(-w/2,h/2-1),(-w/2,-h/2+1),(-w/2+1,-h/2)]
    piping(name+" | buckle frame",form,[(x+dx,z+dz+tilt*dx) for dx,dz in outline],.95,leather,bind)
    piping(name+" | buckle tongue",form,[(x-w*.45,z),(x+1,z+tilt*w*.4)],.75,leather,bind)
badge=[]
for v in np.linspace(-1,1,17):
    row=[]
    for u in np.linspace(-1,1,17):
        x=271+u*11.7-v*3.7
        z=183+u*4.1+v*13
        row.append(fitted_front(torso,x,z,1.8+.6*(1-u*u)))
    badge.append(row)
grid("Coursework | circled F patch",badge,paper,outfit,"trunk",.02)

# Narrow side locks curve out into free space. A dedicated hair pigment keeps
# fine strands dark even where the corresponding drawing shows the background.
filament_mat=bpy.data.materials.new("06 | Loose umber strands")
filament_mat.use_nodes=True
shader=filament_mat.node_tree.nodes.get("Principled BSDF")
shader.inputs["Base Color"].default_value=(.022,.013,.009,1)
shader.inputs["Roughness"].default_value=.41
shader.inputs["Specular IOR Level"].default_value=.24
points,angles,faces=[],[],[]
for index in range(74):
    left=index%2==0
    long=index%5==0
    angle=rng.uniform(2.8,3.8) if left else rng.uniform(-.8,.18)
    start=rng.uniform(27,52)
    end=rng.uniform(118,181) if not long else rng.uniform(182,295)
    px=rng.uniform(-3,3)
    phase=rng.uniform(0,tau)
    near_face=rng.random()<.64
    if near_face:
        end=min(end,170+rng.uniform(-16,23))
    start_i=len(points)
    for j,t in enumerate(np.linspace(0,1,35)):
        z=start+(end-start)*t
        a=angle+.11*math.sin(6*t+phase)
        x,d,_=coords(mane,z,a,2)
        blend=np.clip((z-76)/55,0,1)
        front_depth=465+max(0,z-90)*(.15 if near_face else .45)
        d+=(front_depth-d)*blend*(.97 if near_face else .50)
        x+=px+3*math.sin(7*t+phase)*t
        if not left:
            x-=12*blend
        radius=(.5+.45*(index%7==0))*(1-.97*t**2)
        for k in range(5):
            aa=k*tau/5
            points.append((x+radius*math.cos(aa),d+radius*math.sin(aa),z))
            angles.append(a)
            if j:
                k0=start_i+j*5+k
                k1=start_i+j*5+(k+1)%5
                faces.append((k0-5,k0,k1,k1-5))
object_mesh("Hair | fine temple strands",points,faces,angles,filament_mat,hair,"Head")

armature=bpy.data.armatures.new("Raka | Generic")
rig=bpy.data.objects.new("ACT_Shadow_Raka",armature)
body.objects.link(rig)
bpy.context.view_layer.objects.active=rig
rig.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")
def bone(name,head_point,tail_point,parent=None):
    b=armature.edit_bones.new(name)
    b.head=world(head_point)
    b.tail=world(tail_point)
    if parent:
        b.parent=armature.edit_bones[parent]
bone("Root",(230,547,865),(230,547,796))
bone("Pelvis",(230,547,369),(230,544,291),"Root")
bone("Spine",(230,544,291),(230,548,153),"Pelvis")
bone("Neck",(230,548,153),(217,507,105),"Spine")
bone("Head",(217,507,105),(206,490,25),"Neck")
for suffix,rev in [("R",False),("L",True)]:
    def pt(x,d,z):
        return mirror((x,d,z)) if rev else (x,d,z)
    bone("UpperArm."+suffix,pt(162,554,167),pt(131,561,318),"Spine")
    bone("Forearm."+suffix,pt(131,561,318),pt(73,508,474),"UpperArm."+suffix)
    bone("Hand."+suffix,pt(73,508,474),pt(68,500,541),"Forearm."+suffix)
    bone("Thigh."+suffix,pt(198,544,366),pt(197,534,572),"Pelvis")
    bone("Shin."+suffix,pt(197,534,572),pt(181,552,810),"Thigh."+suffix)
    bone("Foot."+suffix,pt(181,552,810),pt(183,466,857),"Shin."+suffix)
bpy.ops.object.mode_set(mode="OBJECT")
for pose_bone in rig.pose.bones:
    pose_bone.rotation_mode="XYZ"
rig.show_in_front=False
rig.hide_set(True)


def weights(obj,bind,z):
    if bind=="trunk":
        names=["Neck","Spine","Pelvis"]
        breaks=[(126,151),(277,311)]
    elif bind.startswith("arm."):
        side=bind[-1]
        names=[b+"."+side for b in ("UpperArm","Forearm","Hand")]
        breaks=[(298,336),(464,487)]
    elif bind.startswith("leg."):
        side=bind[-1]
        names=[b+"."+side for b in ("Thigh","Shin","Foot")]
        breaks=[(548,593),(800,828)]
    else:
        obj.vertex_groups.new(name=bind).add(list(range(len(z))),1,"REPLACE")
        return
    previous=np.ones(len(z))
    for i,name in enumerate(names):
        nxt=np.clip((z-breaks[i][0])/(breaks[i][1]-breaks[i][0]),0,1) if i<len(breaks) else np.zeros(len(z))
        weight=previous-nxt
        group=obj.vertex_groups.new(name=name)
        full=np.flatnonzero(weight>=1)
        if len(full):
            group.add(full.tolist(),1,"REPLACE")
        for idx in np.flatnonzero((weight>0)&(weight<1)):
            group.add([int(idx)],float(weight[idx]),"REPLACE")
        previous=nxt
for obj,bind,z in parts:
    weights(obj,bind,z)
    obj.parent=rig
    deform=obj.modifiers.new("Raka rig","ARMATURE")
    deform.object=rig

baseline=ROOT/"BlenderSources/Characters/ACT_Shadow_Raka.blend"
with bpy.data.libraries.load(str(baseline)) as (src,dst):
    dst.actions=list(src.actions)
animation=rig.animation_data_create()
for action in dst.actions:
    track=animation.nla_tracks.new()
    track.name=action.name
    strip=track.strips.new(action.name,1,action)
    track.mute=True
scene.frame_set(1)

# Orthographic overlay guides can be toggled individually in the Outliner.
def guide(name,axis,centre,rotation):
    ob=bpy.data.objects.new(name,None)
    guides.objects.link(ob)
    ob.empty_display_type="IMAGE"
    ob.data=source
    ob.empty_display_size=width*UNIT
    ob.empty_image_offset=(-centre/width,-(height-865)/height)
    ob.rotation_euler=rotation
    ob.location=axis
    ob.hide_render=True
    ob.hide_set(True)
for name,loc,center,rot in [
 ("Front",(0,.55,0),230,(math.pi/2,0,0)),("Back",(0,-.55,0),835,(math.pi/2,0,math.pi)),
 ("Left",(-.75,0,0),547,(math.pi/2,0,math.pi/2)),("Right",(.75,0,0),1135,(math.pi/2,0,-math.pi/2))]:
    guide("Guide | "+name,loc,center,rot)

scene.render.engine="CYCLES"
scene.cycles.samples=8
scene.cycles.use_denoising=True
scene.world=bpy.data.worlds.new("Neutral photographic studio")
scene.world.use_nodes=True
scene.world.node_tree.nodes["Background"].inputs[0].default_value=(.58,.57,.55,1)
scene.world.node_tree.nodes["Background"].inputs[1].default_value=.82
scene.view_settings.view_transform="Standard"
def lamp(name,loc,power,size):
    data=bpy.data.lights.new(name,"AREA")
    data.energy=power
    data.shape="DISK"
    data.size=size
    light=bpy.data.objects.new(name,data)
    studio.objects.link(light)
    light.location=loc
    light.rotation_euler=(Vector((0,0,1.3))-light.location).to_track_quat("-Z","Y").to_euler()
lamp("Softbox",(-2,-3,4),65,3)
lamp("Edge light",(2.5,2,3),44,2.5)
# The illustration supplies reflected light and shadows. Add just a contact
# plane with the same hue as the world for readable feet.
floor_mesh=bpy.data.meshes.new("Studio floor")
floor_mesh.from_pydata([(-200,-200,-.004),(200,-200,-.004),(200,200,-.004),(-200,200,-.004)],[],[(0,1,2,3)])
floor=bpy.data.objects.new("Studio | ground",floor_mesh)
studio.objects.link(floor)
floor_mat=bpy.data.materials.new("Neutral ground")
floor_mat.diffuse_color=(.7,.69,.67,1)
floor_mesh.materials.append(floor_mat)
floor_mat.use_nodes=True
nodes=floor_mat.node_tree.nodes
links=floor_mat.node_tree.links
ground=nodes.get("Principled BSDF")
ground.inputs["Base Color"].default_value=(.62,.61,.60,1)
geometry=nodes.new("ShaderNodeNewGeometry")
distance=nodes.new("ShaderNodeVectorMath")
distance.operation="LENGTH"
links.new(geometry.outputs["Position"],distance.inputs[0])
fade=nodes.new("ShaderNodeMapRange")
fade.interpolation_type="SMOOTHERSTEP"
fade.inputs["From Min"].default_value=.18
fade.inputs["From Max"].default_value=1.15
links.new(distance.outputs["Value"],fade.inputs["Value"])
clear=nodes.new("ShaderNodeBsdfTransparent")
mix=nodes.new("ShaderNodeMixShader")
links.new(fade.outputs[0],mix.inputs[0])
links.new(ground.outputs[0],mix.inputs[1])
links.new(clear.outputs[0],mix.inputs[2])
links.new(mix.outputs[0],nodes.get("Material Output").inputs[0])
target=Vector((0,-.025,1.17))
views=[("Front",0),("Left",90),("Back",180),("Right",-90),("ThreeQuarter",32)]
cameras={}
for name,azimuth in views:
    cam=bpy.data.objects.new("View | "+name,bpy.data.cameras.new(name))
    studio.objects.link(cam)
    a=math.radians(azimuth)
    cam.location=target+Vector((4*math.sin(a),-4*math.cos(a),.035))
    cam.rotation_euler=(target-cam.location).to_track_quat("-Z","Y").to_euler()
    cam.data.type="ORTHO"
    cam.data.ortho_scale=2.51
    cameras[name]=cam
scene.camera=cameras["ThreeQuarter"]
scene.render.resolution_x=460
scene.render.resolution_y=940
scene.render.resolution_percentage=100
scene.render.image_settings.file_format="PNG"
for area in bpy.context.screen.areas:
    if area.type=="VIEW_3D":
        space=area.spaces.active
        space.region_3d.view_rotation=scene.camera.rotation_euler.to_quaternion()
        space.region_3d.view_distance=3.8
        space.region_3d.view_location=target
        space.region_3d.view_perspective="ORTHO"
        space.overlay.show_overlays=False
        space.shading.type="MATERIAL"
for obj in studio.objects:
    obj.hide_set(True)
scene["Source"]="Raka’s Manifestation Character Turnaround | projected colour and authored volume"
scene["Review"]="Editable Blender reconstruction. Projection includes lighting from the original illustration."
scene.render.filepath="//Raka_Render.png"
bpy.context.preferences.filepaths.save_version=0
source.filepath = bpy.path.relpath(str(REF), start=str(OUTPUT))
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT/"Raka_Manifestation.blend"),compress=True)
for name,_ in views:
    scene.camera=cameras[name]
    scene.render.filepath=str(PREVIEWS/(name+".png"))
    bpy.ops.render.render(write_still=True)
print("RAKA_TURNAROUND_COMPLETE",len(parts),len(armature.bones),len(bpy.data.actions))
