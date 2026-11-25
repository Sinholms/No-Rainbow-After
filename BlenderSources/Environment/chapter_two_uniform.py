"""Tailored standing police uniform with a fitted Infinite head scan; +Y is the front."""
import math
import bpy
import bmesh
from mathutils import Vector


def surface(name, verts, faces, material, smooth=True):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    mesh.materials.append(material)
    uv = mesh.uv_layers.new(name="Tailoring")
    for face in mesh.polygons:
        face.use_smooth = smooth
        n = face.normal
        axes = (0, 1) if abs(n.z) > .75 else (1, 2) if abs(n.x) > abs(n.y) else (0, 2)
        for loop in face.loop_indices:
            p = mesh.vertices[mesh.loops[loop].vertex_index].co
            uv.data[loop].uv = (p[axes[0]], p[axes[1]])
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def tailoring(name, sections, material, folds=0, count=36, follow=False, creases=()):
    """Elliptical cross sections follow the centreline through bends of the cloth."""
    # Intermediate cloth rings let compression creases taper around the body.
    if folds:
        refined = []
        for a, b in zip(sections, sections[1:]):
            distance = (Vector(a[:3])-Vector(b[:3])).length
            steps = max(1, math.ceil(distance/.018))
            for n in range(steps):
                refined.append(tuple(x+(y-x)*n/steps for x,y in zip(a,b)))
        sections = refined + [sections[-1]]
    verts = []
    path = [Vector(row[:3]) for row in sections]
    for i, (_, _, _, w, d) in enumerate(sections):
        tangent = (path[min(i+1, len(path)-1)] - path[max(i-1, 0)]).normalized()
        if not follow:
            tangent = Vector((0, 0, 1))
        across = Vector((0, 1, 0)).cross(tangent).normalized()
        depth = tangent.cross(across)
        for j in range(count):
            a = j * math.tau / count
            z = path[i].z
            ripple = 1 + folds*(math.sin(6*a+4*z) + .46*math.sin(11*a-6*z))
            # Slanted valleys at bends and attachment points taper into uncreased fabric.
            for h in creases:
                offset = z-h-.028*math.sin(a+.8*h)-.014*math.cos(2*a-3*h)
                ridge = math.exp(-((offset-.016)/.018)**2)-math.exp(-(offset/.012)**2)
                ripple += folds*1.85*ridge*(.58+.42*math.cos(a+9*h))

            verts.append(path[i] + ripple*(w*math.cos(a)*across + d*math.sin(a)*depth))
    faces = [tuple(reversed(range(count))), tuple(range(len(verts)-count, len(verts)))]
    faces.extend((i+j, i+(j+1)%count, i+count+(j+1)%count, i+count+j)
                 for i in range(0, len(verts)-count, count) for j in range(count))
    return surface(name, verts, faces, material)


def lettering(name, words, x, y, z, size, material, yaw=0):
    data = bpy.data.curves.new(name, "FONT")
    data.body, data.size, data.align_x, data.align_y = words, size, "CENTER", "CENTER"
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = (x, y, z)
    obj.rotation_euler = (math.pi/2, 0, math.pi+yaw)
    data.materials.append(material)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target="MESH")


def build_sentry(root, coat, navy, leather, skin, brass, silver, lens, box, rod, oval):
    def button(name, x, y, z, r=.013):
        rod(name, (x,y-.005,z), (x,y+.005,z), r, r*.91, brass, 16)

    def cloth_patch(name, points, material):
        # A stitched applique follows the curved front of the jacket.
        n = len(points)
        return surface(name, points+[(x,y-.012,z) for x,y,z in points],
                       [tuple(range(n)), tuple(range(n,2*n))] +
                       [(i,(i+1)%n,n+(i+1)%n,n+i) for i in range(n)], material, False)

    for side in (-1, 1):
        x = side*.172
        stagger = .058 if side == 1 else -.035
        def leg_profile(rows):
            return [(side*cx, y+stagger, z, w, d) for cx,y,z,w,d in rows]
        tailoring("Laced patrol boot", leg_profile([
            (.18,.085,.028,.105,.196),(.18,.086,.057,.108,.199),
            (.18,.082,.092,.109,.196),(.18,.075,.123,.105,.184),
            (.18,.045,.151,.096,.145),(.18,-.008,.202,.086,.094),
            (.176,-.015,.264,.087,.086),(.173,-.015,.315,.09,.09)]), leather)
        tailoring("Welt and tread", leg_profile([
            (.18,.086,.004,.107,.197),(.18,.086,.016,.113,.203),
            (.18,.086,.043,.113,.203),(.18,.086,.052,.108,.198)]), navy)
        for h in range(4):
            z=.153+.025*h
            y=stagger+.166-.021*h
            for sign in (-1,1):
                rod("Boot lacing",(x+sign*.044,y,z),(x-sign*.042,y-.015,z+.018),.004,.004,navy,6)
        tailoring("Trouser pressed leg", leg_profile([
            (.179,-.008,.253,.096,.095),(.18,-.008,.283,.108,.112),
            (.178,-.018,.307,.106,.115),(.18,-.013,.335,.109,.103),
            (.18,-.028,.419,.097,.102),(.175,-.03,.505,.106,.117),
            (.176,-.01,.606,.115,.125),(.175,.022,.651,.118,.127),
            (.17,.032,.7,.116,.121),(.166,.019,.748,.119,.129),
            (.158,.008,.855,.133,.146),(.148,-.01,1.015,.148,.161),
            (.134,-.017,1.155,.148,.171),(.12,-.019,1.235,.142,.16)]), navy, .036, creases=(.30,.36,.65,.71,.96))
    tailoring("Tunic with shaped waist", [
        (0,-.022,1.105,.277,.19),(0,-.022,1.125,.293,.205),
        (0,-.02,1.175,.295,.207),(0,-.021,1.32,.26,.18),
        (0,-.028,1.46,.273,.194),(0,-.036,1.62,.305,.213),
        (0,-.04,1.753,.33,.215),(0,-.045,1.83,.345,.196),
        (0,-.045,1.878,.316,.175),(0,-.033,1.918,.215,.141),
        (0,-.018,1.95,.123,.102)], coat, .026, creases=(1.17,1.29,1.37,1.44,1.58))
    tailoring("Duty belt", [(0,-.02,z,w,d) for z,w,d in
        [(1.253,.28,.195),(1.262,.284,.202),(1.323,.279,.196),(1.332,.269,.187)]], leather)
    box("Buckle",(0,.185,1.294),(.095,.029,.062),brass,.009)
    for sign in (-1,1):
        box("Belt pouch",(sign*.256,.087,1.265),(.094,.082,.14),leather,.018)
        for z in (1.64,):
            def panel(label, coords, mat):
                cloth_patch(label, [(sign*x,y,h) for x,y,h in coords],mat)
            panel("Pleated breast pocket",[(.059,.175,z+.048),(.228,.115,z+.05),
                (.217,.122,z-.088),(.151,.158,z-.119),(.064,.175,z-.087)],coat)
            panel("Scalloped pocket flap",[(.054,.185,z+.064),(.23,.13,z+.063),
                (.226,.137,z+.018),(.149,.175,z-.008),(.061,.185,z+.008)],navy)
            button("Pocket stud",sign*.15,.178,z+.012,.009)
        panel("Folded collar",[(.026,.154,1.855),(.135,.095,1.947),
              (.193,.104,1.901),(.128,.165,1.785)],coat)
        panel("Epaulette",[(.163,.046,1.929),(.318,.034,1.892),
              (.341,-.051,1.887),(.175,-.052,1.93)],navy)
        button("Shoulder stud",sign*.195,.031,1.927,.009)
        # One working arm, one relaxed arm; the sleeve is a single folded surface.
        if sign<0:
            rows=[(-.245,.45,1.59,.060,.066),(-.254,.416,1.578,.078,.084),
                  (-.269,.37,1.55,.077,.085),(-.332,.24,1.494,.088,.099),
                  (-.379,.115,1.474,.094,.098),(-.392,.064,1.501,.104,.098),
                  (-.391,.009,1.6,.109,.113),(-.366,-.025,1.745,.119,.125),
                  (-.321,-.042,1.825,.111,.128),(-.297,-.043,1.841,.091,.106),(-.259,-.043,1.849,.068,.084),(-.224,-.043,1.849,.042,.06)]
            palm=(-.248,.485,1.592)
        else:
            rows=[(.35,.125,1.091,.061,.070),(.358,.12,1.13,.074,.082),
                  (.372,.104,1.22,.077,.088),(.398,.048,1.399,.091,.099),
                  (.41,.019,1.465,.103,.107),(.402,-.011,1.51,.095,.104),
                  (.382,-.031,1.65,.11,.12),(.354,-.047,1.769,.12,.133),
                  (.315,-.048,1.823,.111,.128),(.291,-.043,1.841,.091,.106),(.255,-.043,1.849,.068,.084),(.224,-.043,1.849,.042,.06)]
            palm=(.35,.135,1.038)
        tailoring("Set-in sleeve",rows,coat,.043,follow=True,creases=(1.19,1.40,1.46,1.52,1.63,1.78))
        x,y,z=palm
        oval("Gloved palm",palm,(.057,.067,.085),leather)
        for j in range(4):
            if sign>0:
                a=(x+.021,y+(j-1.5)*.027,z-.052)
                b=(x+.015,a[1]+.008,z-.104+.008*abs(j-1.4))
                c=(x-.009,b[1]+.004,b[2]-.032)
            else:
                a=(x-.043,y+(j-1.5)*.026,z+.022)
                b=(x-.042,a[1]+.015,z-.036)
                c=(x+.006,b[1],z-.052)
            rod("Finger",a,b,.017,.015,leather,10)
            rod("Finger tip",b,c,.015,.011,leather,10)
        rod("Glove thumb",(x-.03,y+.01,z+.04),(x-.057,y+.042,z-.035),.024,.016,leather)
    cloth_patch("Button fly",[(-.019,.168,1.87),(.012,.17,1.87),(.014,.177,1.34),(-.018,.177,1.34)],coat)
    for i in range(5): button("Tunic button",0,.181,1.394+.097*i)
    plate=box("Embroidered patch",(-.142,.162,1.75),(.178,.02,.047),navy,.008)
    plate.rotation_euler.z=.37
    lettering("Service marking","POLISI",-.145,.182,1.75,.038,silver,.37)
    cloth_patch("Service shield",[(.113,.173,1.785),(.18,.151,1.785),
                 (.175,.154,1.735),(.148,.168,1.712),(.12,.175,1.733)],brass)

    # Radio microphone, curled lead and cartridge loops break up the tunic front.
    mic=box("Speaker microphone",(.216,.14,1.814),(.063,.034,.081),leather,.012)
    for j in range(4):
        box("Microphone grille",(.216,.159,1.798+j*.01),(.040,.005,.003),silver,.001)
    points=[(.225,.14,1.78)]
    for j in range(1,46):
        t=j/45
        point=(.225+.055*math.sin(t*math.pi/2)+.006*math.cos(27*math.pi*t),
               .15-.084*t+.005*math.sin(27*math.pi*t),1.78-.43*t)
        points.append(point)
    cable=bpy.data.curves.new("Coiled lead","CURVE")
    cable.dimensions="3D"
    cable.bevel_depth=.004
    cable.bevel_resolution=1
    spline=cable.splines.new("POLY")
    spline.points.add(len(points)-1)
    for v,co in zip(spline.points,points):
        v.co=(*co,1)
    lead=bpy.data.objects.new("Radio cable",cable)
    bpy.context.collection.objects.link(lead)
    cable.materials.append(leather)
    bpy.ops.object.select_all(action="DESELECT")
    lead.select_set(True)
    bpy.context.view_layer.objects.active=lead
    bpy.ops.object.convert(target="MESH")
    tailoring("Standing shirt collar",[(0,-.018,1.889,.14,.13),
             (0,-.018,1.94,.111,.101),(0,-.019,1.945,.108,.098)],coat)
    for side in (-1,1):
        # Embroidered shoulder flash, wrapped over the outside of each upper arm.
        points=[(side*.488,-.035,1.742),(side*.482,.052,1.738),
                (side*.484,.061,1.677),(side*.497,.008,1.632),(side*.497,-.048,1.678)]
        cloth_patch("Sleeve shield binding",points,brass)
        centre=sum((Vector(q) for q in points),Vector()) / len(points)
        inset=[tuple(centre+(Vector(q)-centre)*.83+Vector((side*.004,0,0))) for q in points]
        cloth_patch("Sleeve shield field",inset,navy)
        x=side*.34
        for j in range(2):
            tab=box("Epaulette braid",(x-side*(.028+j*.027),.001,1.882+.008*j),
                    (.014,.088,.009),brass,.002)
            tab.rotation_euler.y=side*.18
    lettering("Badge engraving","BHAYANGKARA",.147,.182,1.75,.009,navy,-.35)

    # Import the scan with its photographed UVs. Trim the bust below the shirt collar.
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.gltf(filepath=str(root/"ThirdPartySources/InfiniteHead/LeePerrySmith.glb"))
    for obj in set(bpy.context.scene.objects) - before:
        if obj.type != "MESH":
            bpy.data.objects.remove(obj, do_unlink=True)
            continue
        obj.name = "Officer face and neck"
        obj.data.materials.clear()
        obj.data.materials.append(skin)
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bmesh.ops.bisect_plane(bm, geom=list(bm.verts)+list(bm.edges)+list(bm.faces),
                               plane_co=(0,0,-1.77), plane_no=(0,0,1), clear_inner=True)
        for v in bm.verts:
            x,y,z = v.co
            v.co = (-x*.078-.006, -y*.078-.029, 2.027+z*.079)
        bm.to_mesh(obj.data)
        bm.free()
        # These external scan maps use the original (OpenGL) UV convention.
        for loop in obj.data.uv_layers.active.data:
            loop.uv.y = 1.0 - loop.uv.y
        for poly in obj.data.polygons:
            poly.use_smooth = True
        bsdf=skin.node_tree.nodes.get("Principled BSDF")
        kit=root/"Assets/_NoRainbowAfter/Art/Models/Environment/ChapterTwo"
        color=skin.node_tree.nodes.new("ShaderNodeTexImage")
        color.image=bpy.data.images.load(str(kit/"SentryFace.jpg"))
        skin.node_tree.links.new(color.outputs["Color"],bsdf.inputs["Base Color"])

    # Wraparound service glasses follow the cheeks and bridge of the fitted head.
    for side in (-1,1):
        rim=[(.011,2.155),(.018,2.171),(.043,2.177),(.082,2.177),
             (.104,2.17),(.105,2.154),(.093,2.138),(.068,2.128),
             (.049,2.127),(.029,2.135)]
        outer=[(side*x,.163-.039*(x/.105)**1.65,h) for x,h in rim]
        cloth_patch("Spectacle rim",outer,brass)
        center=sum((Vector(pt) for pt in outer),Vector())/len(outer)
        inner=[tuple(center+.94*(Vector(pt)-center)+Vector((0,.0024,0))) for pt in outer]
        surface("Tinted lens",inner+[tuple(center+Vector((0,.009,0)))],
                [(i,(i+1)%len(inner),len(inner)) for i in range(len(inner))],lens)
        rod("Temple hinge",outer[4],(side*.134,.010,2.161),.003,.003,brass,8)
        rod("Ear piece",(side*.134,.010,2.161),(side*.136,-.045,2.143),.003,.004,leather,8)
    rod("Spectacle bridge",(-.018,.160,2.166),(.018,.160,2.166),.003,.003,brass,10)

    tailoring("Peaked service cap",[(0,-.007,2.205,.122,.137),(0,-.007,2.221,.13,.148),
              (0,-.01,2.25,.132,.148),(0,.005,2.269,.149,.169),
              (0,.013,2.307,.157,.178),(0,.009,2.325,.147,.164),
              (0,.004,2.337,.091,.114)],navy)
    tailoring("Woven cap band",[(0,-.005,2.215,.13,.146),(0,-.005,2.242,.136,.151)],leather)
    verts=[]
    for i in range(25):
        a=(i/24-.5)*math.pi
        for edge in (0,1):
            curve=math.cos(a)
            verts.append((.13*math.sin(a)*(1+.1*edge),(.141+.086*edge)*curve-.002,
                          2.221-.015*curve-.019*edge*curve))
    brim=surface("Cap brim",verts,[(2*i,2*i+1,2*i+3,2*i+2) for i in range(24)],leather)
    brim.modifiers.new("Bound edge","SOLIDIFY").thickness=.006
    cloth_patch("Cap escutcheon",[(-.026,.186,2.291),(.026,.186,2.291),(.022,.171,2.26),
                 (0,.161,2.25),(-.022,.171,2.26)],brass)
    head_parts=set(bpy.context.scene.objects)-before
    pivot=bpy.data.objects.new("Sentry gaze",None)
    bpy.context.collection.objects.link(pivot)
    pivot.location=(0,-.02,1.936)
    bpy.context.view_layer.update()
    for part in head_parts:
        part.parent=pivot
        part.matrix_parent_inverse=pivot.matrix_world.inverted()
    rod("Flashlight grip",(-.25,.445,1.6),(-.25,.577,1.6),.037,.044,leather,24)
