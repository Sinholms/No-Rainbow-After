"""Original police sentry and shuttle for No Rainbow After. Run from the repo in Blender."""
from pathlib import Path
import bpy
from mathutils import Vector
import math
import wave
from array import array

root = Path.cwd()
source = root / "BlenderSources/Environment"
output = root / "Assets/_NoRainbowAfter/Art/Models/Environment/ChapterTwo"
audio = root / "Assets/_NoRainbowAfter/Resources/ChapterTwoAudio"
audio.mkdir(parents=True, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)

def paint(name, rgb, roughness=.62, metallic=0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*rgb, 1)
    mat.use_nodes = True
    surface = mat.node_tree.nodes.get("Principled BSDF")
    surface.inputs["Base Color"].default_value = (*rgb, 1)
    surface.inputs["Roughness"].default_value = roughness
    surface.inputs["Metallic"].default_value = metallic
    return mat

coat = paint("C2 Sentry Raincoat", (.078, .104, .117))
vest = paint("C2 Sentry Vest", (.048, .06, .078))
rubber = paint("C2 Rubber", (.026, .036, .047), .38)
skin = paint("C2 Sentry Skin", (.093, .088, .075))
gold = paint("C2 Tarnished Brass", (.65, .48, .185), .46, .48)
trim = paint("C2 Reflective Tape", (.48, .56, .57), .4)
glass = paint("C2 Smoked Bus Glass", (.078, .19, .215), .24, .25)
bus_paint = paint("C2 Bus Teal", (.15, .37, .36), .37, .24)
ivory = paint("C2 Bus Ivory", (.72, .65, .48), .46)
amber = paint("C2 Lamp Amber", (.93, .48, .16), .27)
red = paint("C2 Lamp Red", (.54, .063, .065), .28)

def finish(obj, name, material):
    obj.name = name
    obj.data.materials.append(material)
    return obj

def box(name, point, dimensions, material, radius=.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=point)
    obj = bpy.context.object
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if radius:
        bevel = obj.modifiers.new("Rolled edges", "BEVEL")
        bevel.width = radius
        bevel.segments = 3
        obj.modifiers.new("Weighted normals", "WEIGHTED_NORMAL")
    return finish(obj, name, material)

def ellipsoid(name, point, radii, material):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=10, location=point)
    obj = bpy.context.object
    obj.scale = radii
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    return finish(obj, name, material)

def limb(name, a, b, r1, r2, material, vertices=14):
    a, b = Vector(a), Vector(b)
    axis = b-a
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=r1, radius2=r2,
                                    depth=axis.length, location=(a+b)/2)
    obj = bpy.context.object
    obj.rotation_euler = axis.to_track_quat("Z", "Y").to_euler()
    return finish(obj, name, material)

def export(name, objects):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(filepath=str(output / (name + ".fbx")),
                            use_selection=True, axis_forward="-Z", axis_up="Y",
                            apply_unit_scale=True, bake_anim=False,
                            add_leaf_bones=False, use_mesh_modifiers=True)

for side in (-1, 1):
    x = side * .22
    box("Long tread boot", (x, .125, .14), (.28, .48, .28), rubber, .05)
    box("Boot sole", (x, .13, .043), (.30, .50, .086), vest, .015)
    limb("Trouser calf", (x, -.015, .24), (x, .015, .68), .113, .138, vest)
    limb("Trouser thigh", (x, .015, .65), (side*.17, -.035, 1.24), .139, .172, coat)
    box("Shin guard", (x, .109, .49), (.18, .09, .31), rubber)
    box("Reflective cuff", (x, .125, .32), (.19, .034, .055), trim, .006)
box("Duty belt", (0, -.025, 1.25), (.68, .43, .13), rubber, .04)
box("Belt clasp", (0, .201, 1.25), (.16, .056, .105), gold)
box("Uniform torso", (0, -.048, 1.59), (.73, .47, .75), coat, .115)

# Open coat with an irregular hanging hem and repeated damp folds.
vertices, faces = [], []
sectors = 30
for height, width, depth in ((.84, .39, .245), (1.23, .34, .235), (1.62, .39, .24), (1.955, .34, .20)):
    for i in range(sectors + 1):
        angle = .48 + (math.tau - .96) * i / sectors
        fold = .012 * math.cos(10 * angle + 2 * height)
        hemdrop = .062 * math.sin(angle * 3) + .03 * math.sin(angle * 11) if height < 1 else 0
        vertices.append(((width + fold) * math.sin(angle), (depth + fold) * math.cos(angle) - .056,
                         height + hemdrop))
for row in range(3):
    for i in range(sectors):
        a = row * (sectors + 1) + i
        faces.append((a, a + 1, a + sectors + 2, a + sectors + 1))
mesh = bpy.data.meshes.new("Uneven raincoat drape")
mesh.from_pydata(vertices, [], faces)
drape = bpy.data.objects.new("Hanging raincoat", mesh)
bpy.context.collection.objects.link(drape)
finish(drape, drape.name, coat)
for face in mesh.polygons:
    face.use_smooth = True
drape.modifiers.new("Shaped folds", "SUBSURF").levels = 1
drape.modifiers.new("Coat thickness", "SOLIDIFY").thickness = .016
box("Armoured chest", (0, .098, 1.71), (.64, .34, .55), vest, .078)
box("Back panel", (0, -.293, 1.71), (.51, .12, .50), vest, .047)
box("Collar", (0, -.015, 1.978), (.46, .36, .16), rubber, .041)
for side in (-1, 1):
    ellipsoid("Angular shoulder", (side*.397, -.022, 1.88), (.205, .22, .183), coat)
    elbow = (side*.50, .06, 1.53 if side<0 else 1.47)
    hand = (side*.25, .51, 1.59) if side<0 else (.49, .255, 1.18)
    limb("Raincoat sleeve", (side*.427, -.028, 1.86), elbow, .148, .114, coat)
    limb("Bent sleeve", elbow, hand, .118, .083, coat)
    ellipsoid("Leather glove", hand, (.082, .097, .089), rubber)
    if side > 0:
        for finger in range(4):
            x = hand[0] + (finger - 1.5) * .032
            tip = hand[2] - (.195 - abs(finger - 1.5) * .026)
            limb("Long gloved finger", (x, hand[1]+.012, hand[2]-.044),
                 (x-.021, hand[1]+.043, tip), .020, .015, rubber, 9)
    box("Shoulder stripe", (side*.403, .147, 1.937), (.215, .08, .048), trim, .011)
    box("Vest pocket", (side*.198, .28, 1.56), (.23, .105, .19), rubber, .028)
box("Shield badge", (-.178, .277, 1.89), (.125, .057, .139), gold, .026)
box("Uniform nameplate", (.17, .273, 1.883), (.208, .03, .042), trim, .007)
box("Vertical coat seam", (0, .282, 1.735), (.027, .042, .41), coat, .005)
limb("Neck", (0, -.025, 1.965), (0, -.01, 2.151), .106, .107, skin)
ellipsoid("Long face", (0, .014, 2.205), (.142, .138, .23), skin)
box("Shadow below the brim", (0, .123, 2.247), (.243, .068, .101), rubber, .022)
box("Cap crown", (0, -.014, 2.405), (.36, .35, .13), vest, .058)
box("Cap brim", (0, .133, 2.358), (.35, .29, .047), rubber, .02)
box("Cap badge", (0, .167, 2.416), (.071, .027, .058), gold, .011)
limb("Flashlight handle", (-.25, .445, 1.60), (-.25, .58, 1.60), .051, .058, rubber)
sentry = list(bpy.context.scene.objects)
export("ACT_TKPSentry", sentry)

box("Shuttle chassis", (0, 0, .53), (2.33, 5.9, .37), bus_paint, .135)
box("Rear lower panel", (0, -2.841, 1.02), (2.29, .15, .72), bus_paint, .07)
box("Front lower panel", (0, 2.841, 1.11), (2.29, .15, .89), ivory, .07)
box("Rear top panel", (0, -2.84, 2.39), (2.29, .16, .25), ivory, .07)
box("Front top panel", (0, 2.84, 2.40), (2.29, .16, .25), ivory, .07)
box("Roof", (0, -.15, 2.57), (2.26, 5.54, .20), ivory, .085)
box("Front windshield", (0, 2.892, 1.92), (1.96, .064, .67), glass, .074)
box("Rear windshield", (0, -2.892, 1.96), (1.90, .064, .62), glass, .074)
box("Windshield divider", (0, 2.939, 1.92), (.043, .029, .66), rubber, .004)
box("Front route display", (0, 2.905, 2.37), (1.46, .065, .21), vest, .027)
box("Front bumper", (0, 2.951, .535), (2.08, .19, .185), rubber, .061)
box("Rear bumper", (0, -2.951, .535), (2.08, .19, .185), rubber, .061)
for side in (-1, 1):
    box("Lower side skin", (side*1.141, -.61, 1.043), (.116, 4.44, .687), bus_paint, .06)
    box("Front side skin", (side*1.141, 2.46, 1.043), (.116, .72, .687), bus_paint, .05)
    box("Upper side rail", (side*1.14, -.03, 2.37), (.117, 5.65, .28), ivory, .05)
    for seam in (-2.65, -1.48, -.32, .80, 2.32, 2.74):
        box("Window pillar", (side*1.12, seam, 1.85), (.12, .14, 1.21), ivory, .02)
    box("Coach side stripe", (side*1.172, 0, 1.163), (.035, 5.48, .087), gold, .01)
    for y in (-2.03, -.89, .25):
        box("Side window", (side*1.143, y, 1.92), (.06, .94, .60), glass, .06)
    box("Door dark frame", (side*1.153, 1.594, 1.423), (.062, 1.04, 1.79), rubber, .065)
    for y in (1.36, 1.82):
        box("Passenger door", (side*1.191, y, 1.42), (.025, .424, 1.66), bus_paint, .023)
        box("Door glass", (side*1.208, y, 1.78), (.019, .335, .73), glass, .025)
    for y in (-1.8, 1.82):
        limb("Tyre", (side*.92, y, .43), (side*1.19, y, .43), .42, .42, rubber, 24)
        limb("Wheel cap", (side*1.19, y, .43), (side*1.225, y, .43), .217, .217, trim, 20)
    box("Headlamp", (side*.79, 2.962, .848), (.42, .083, .19), trim, .04)
    box("Marker", (side*.961, 2.945, .73), (.16, .083, .109), amber, .026)
    box("Tail lamp", (side*.909, -2.933, .926), (.20, .08, .32), red, .033)
    box("Mirror", (side*1.23, 2.51, 1.97), (.17, .095, .25), rubber, .045)
bus = [obj for obj in bpy.context.scene.objects if obj not in sentry]
export("PROP_TKPShuttle", bus)

# The source retains both original assets on separate collections.
for name, objects in (("Police Sentry", sentry), ("Market Shuttle", bus)):
    group = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(group)
    for obj in objects:
        for old in list(obj.users_collection):
            old.objects.unlink(obj)
        group.objects.link(obj)
for obj in bus:
    obj.location.x += 5
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(source / "ChapterTwoBizarre.blend"))

rate = 22050
tau = math.tau
def save_audio(name, seconds, sample):
    frames = array("h", (int(max(-.98,min(.98,sample(i/rate)))*32767)
                        for i in range(round(rate*seconds))))
    with wave.open(str(audio / (name + ".wav")), "wb") as wav:
        wav.setparams((1, 2, rate, 0, "NONE", "not compressed"))
        wav.writeframes(frames.tobytes())

def siren(t):
    # Integrated periodic frequency, with integer base cycles at the loop seam.
    wave_phase = tau * 372*t - (115/.25) * math.cos(tau*.25*t)
    return (.65 + .35*math.sin(tau*.25*t)) * (
        .26*math.sin(wave_phase) + .082*math.sin(2*wave_phase) +
        .04*math.sin(tau*117*t) + .013*math.sin(tau*38*t))

def footstep(t):
    hit = math.exp(-t*34) * math.sin(tau*(96*t-61*t*t))
    drag = math.exp(-((t-.069)/.048)**2) * (math.sin(tau*345*t)*math.sin(tau*281*t))
    return .31*hit + .07*drag

save_audio("TKPSiren", 8, siren)
save_audio("HeavyStep", .23, footstep)
