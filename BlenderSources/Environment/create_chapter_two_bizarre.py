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

coat = paint("C2 Sentry Raincoat", (.231, .188, .144))
vest = paint("C2 Sentry Vest", (.048, .06, .078))
rubber = paint("C2 Rubber", (.026, .036, .047), .38)
skin = paint("C2 Sentry Skin", (.407, .216, .147))
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

# The sentry is authored in metres facing +Y. Its cap brim is the Unity orientation marker.
import sys
sys.dont_write_bytecode = True
from sys import path as module_path
module_path.insert(0, str(source))
from chapter_two_uniform import build_sentry
build_sentry(root, coat, vest, rubber, skin, gold, trim, paint("C2_SmokedGlass", (.032,.046,.057), .19, .43), box, limb, ellipsoid)
sentry = list(bpy.context.scene.objects)
if "--bus-update" not in sys.argv:
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
# Rounded corner posts and rubber seals enclose the glass at the ends of the coach.
for end in (-1,1):
    for side in (-1,1):
        box("Coach corner pillar",(side*1.035,end*2.83,1.94),(.207,.185,.84),ivory,.075)
    box("Glass lower seal",(0,end*2.912,1.555),(2.12,.047,.057),rubber,.017)
    box("Roof drip rail",(0,end*2.855,2.514),(2.20,.12,.059),bus_paint,.027)
box("Front grille recess",(0,2.934,1.055),(1.10,.085,.354),rubber,.046)
for j in range(6):
    box("Vent louvre",(0,2.989,.916+j*.052),(1.018,.025,.014),trim,.006)
box("Pressed front belt",(0,2.929,1.426),(2.16,.057,.091),bus_paint,.035)
box("Coach badge surround",(0,2.965,1.395),(.292,.032,.098),gold,.033)
box("Coach badge enamel",(0,2.986,1.395),(.245,.018,.069),bus_paint,.021)
box("Route registration",(0,3.058,.594),(.50,.032,.132),gold,.012)
for side in (-1,1):
    x=side*.79
    box("Recessed headlight bezel",(x,2.94,.862),(.49,.144,.264),rubber,.057)
    box("Headlight reflector",(x,3.015,.881),(.421,.022,.157),ivory,.037)
    for dx in (-.122,-.061,0,.061,.122):
        box("Headlight lens rib",(x+dx,3.032,.881),(.009,.009,.122),trim,.003)
    box("Amber direction lens",(x,3.016,.781),(.315,.027,.040),amber,.014)
    # Parked wipers lie on the glass, with both arms anchored below the seal.
    limb("Wiper arm",(side*.70,2.971,1.573),(side*.41,2.967,1.783),.013,.009,rubber,10)
    limb("Wiper blade",(side*.71,2.974,1.713),(side*.17,2.974,1.842),.012,.012,rubber,10)
    for y in (-1.8,1.82):
        limb("Steel wheel recess",(side*1.225,y,.43),(side*1.237,y,.43),.176,.176,rubber,24)
        limb("Hub cover",(side*1.237,y,.43),(side*1.251,y,.43),.098,.083,trim,20)
        for j in range(6):
            a=j*math.tau/6
            yy=y+.128*math.sin(a); zz=.43+.128*math.cos(a)
            limb("Wheel lug",(side*1.236,yy,zz),(side*1.248,yy,zz),.018,.016,gold,6)
    for z in (.707,.782,.856):
        box("Engine side vent",(side*1.206,-2.36,z),(.021,.75,.031),rubber,.009)
    for y in (-.7,.34):
        box("Service panel",(side*1.202,y,.866),(.028,.92,.426),rubber,.018)
        box("Service hatch",(side*1.217,y,.866),(.025,.898,.407),bus_paint,.018)
        box("Hatch latch",(side*1.231,y,.988),(.019,.134,.024),trim,.007)
    for z in (1.365,2.272):
        box("Window rubber seal",(side*1.204,-.037,z),(.029,5.49,.029),rubber,.008)
box("Driver instrument binnacle",(-.43,2.48,1.53),(1.13,.48,.218),rubber,.078)
limb("Steering column",(-.43,2.41,1.43),(-.43,2.17,1.59),.032,.042,rubber)
bpy.ops.mesh.primitive_torus_add(major_radius=.166,minor_radius=.016,location=(-.43,2.152,1.606),
                                rotation=(math.radians(53),0,0),major_segments=36,minor_segments=8)
finish(bpy.context.object,"Steering wheel",rubber)
for side in (-1,1):
    limb("Boarding handrail",(side*.98,.99,.78),(side*.98,.99,2.29),.025,.025,gold)
    box("Ceiling light",(side*.53,-.27,2.445),(.083,2.7,.036),ivory,.014)
bus = [obj for obj in bpy.context.scene.objects if obj not in sentry]
# World-metre UVs keep rain trails the same size across stamped panels and glass.
bpy.context.view_layer.update()
for obj in bus:
    mesh = obj.data
    uv = mesh.uv_layers.active or mesh.uv_layers.new(name="Coachwork")
    normal_matrix = obj.matrix_world.to_3x3().inverted_safe().transposed()
    for polygon in mesh.polygons:
        normal = (normal_matrix @ polygon.normal).normalized()
        axes = (0,1) if abs(normal.z)>.7 else (1,2) if abs(normal.x)>abs(normal.y) else (0,2)
        for loop in polygon.loop_indices:
            p = obj.matrix_world @ mesh.vertices[mesh.loops[loop].vertex_index].co
            uv.data[loop].uv = (p[axes[0]],p[axes[1]])
# Keep the coach editable in the source; combine fixed opaque fittings per material in the FBX.
# Glass panes stay separate for sorting and the windshield is the import orientation marker.
graph = bpy.context.evaluated_depsgraph_get()
groups = {}
window_panes = []
for part in bus:
    material = part.data.materials[0]
    if material == glass:
        window_panes.append(part)
        continue
    baked = bpy.data.meshes.new_from_object(part.evaluated_get(graph), depsgraph=graph)
    copy = bpy.data.objects.new("Coachwork " + material.name, baked)
    bpy.context.collection.objects.link(copy)
    copy.matrix_world = part.matrix_world
    groups.setdefault(material, []).append(copy)
shells = []
for parts in groups.values():
    bpy.ops.object.select_all(action="DESELECT")
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    if len(parts) > 1:
        bpy.ops.object.join()
    shells.append(parts[0])
export("PROP_TKPShuttle", window_panes + shells)
for shell in shells:
    bpy.data.objects.remove(shell, do_unlink=True)

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
