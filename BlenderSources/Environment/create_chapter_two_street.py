"""Author the editable Chapter 2 kit in a fresh Blender scene, then export metre-scale modules."""
import json
import math
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/_NoRainbowAfter/Art/Models/Environment/ChapterTwo"
SOURCE = ROOT / "BlenderSources/Environment"
OUT.mkdir(parents=True, exist_ok=True)

# Coordinates in the drawings are (right, height, length). The FBX front is at +length.
def v(x, y, z):
    return Vector((x, -z, y))

def linear(ch):
    s = int(ch, 16) / 255
    return s / 12.92 if s <= .04045 else ((s + .055) / 1.055) ** 2.4

scene = bpy.data.scenes.new("Di Bawah Sorotan - kit jalan")
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
palette, materials, modules = [], {}, []
def pigment(name, rgb, smooth=.42, metal=0, glow=0):
    color = tuple(linear(rgb[i:i+2]) for i in (0, 2, 4))
    mat = bpy.data.materials.new("C2_" + name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Metallic"].default_value = metal
    bsdf.inputs["Roughness"].default_value = 1-smooth
    bsdf.inputs["Emission Color"].default_value = (*color, 1)
    bsdf.inputs["Emission Strength"].default_value = glow
    materials[name] = mat
    palette.append(dict(name="C2_"+name, hex=rgb, smooth=smooth, metal=metal, glow=glow))

for args in [
    ("Ink","1E262B",.30), ("Petrol","245456",.53,.34),
    ("Enamel","447F78",.65,.48), ("Oxide","925542",.25,.28), ("Aluminium","A3ACAA",.69,.78),
    ("Steel","566166",.56,.69), ("Concrete","A19E91",.28), ("Aggregate","777B75",.35),
    ("Paver","8D8983",.49), ("PaverDark","6B7376",.54), ("Mortar","51595B",.19),
    ("Ivory","E5D9B7",.49), ("Mustard","DAA14A",.52), ("Ochre","A56A33",.41),
    ("Timber","947047",.33), ("TimberLight","AC8556",.36), ("TimberDark","614B36",.29),
    ("PatrolWhite","D0D4C9",.8,.48), ("PatrolBlue","253D57",.81,.55),
    ("SmokedGlass","364954",.93,.63), ("Seal","192024",.22), ("Paper","D0C4A3",.18),
    ("Rubber","292C30",.22), ("Red","C75040",.62,.10,.24), ("Beacon","527EA9",.68,.18,.34),
    ("Lamp","FFE6B7",.48,.08,1.65), ("Signal","DAEAD6",.49,.05,.85),
    ("Canvas","3F5055",.15), ("Strap","283338",.20), ("Plaster","ADAA97",.25),
    ("Tile","AC6E53",.29), ("Charcoal","38434A",.49)
]:
    pigment(*args)

def module(name):
    global collection
    collection = bpy.data.collections.new(name)
    scene.collection.children.link(collection)
    modules.append(collection)

def mesh(name, coords, faces, mat, bevel=0):
    me = bpy.data.meshes.new(name)
    me.from_pydata(coords, [], faces)
    me.update()
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    collection.objects.link(obj)
    me.materials.append(materials[mat])
    if bevel > 0:
        mod = obj.modifiers.new("Machined edge", 'BEVEL')
        mod.width, mod.segments = bevel, 2
        mod.harden_normals = True
        normal = obj.modifiers.new("Face normals", 'WEIGHTED_NORMAL')
        normal.keep_sharp = True
    return obj

cube_faces = [(0,1,2,3),(4,7,6,5),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)]
def box(name, p, size, mat, bevel=.008, tilt=0):
    x,y,z=p
    w,h,d=(s*.5 for s in size)
    coords = [v(dx,dy,dz) for dy in (-h,h) for dx,dz in [(-w,-d),(w,-d),(w,d),(-w,d)]]
    if tilt:
        rot=Matrix.Rotation(math.radians(tilt), 3, 'X')
        coords=[rot @ q for q in coords]
    return mesh(name,[q+v(x,y,z) for q in coords],cube_faces,mat,bevel)

def rod(name, a, b, radius, mat, sides=10, end=None):
    p,q=v(*a),v(*b)
    axis=(q-p).normalized()
    u=axis.orthogonal().normalized()
    w=axis.cross(u)
    pts=[center+(u*math.cos(i*math.tau/sides)+w*math.sin(i*math.tau/sides))*r
         for center,r in [(p,radius),(q,end if end is not None else radius)] for i in range(sides)]
    faces=[tuple(range(sides)),tuple(range(sides,2*sides))]
    faces.extend((i,(i+1)%sides,(i+1)%sides+sides,i+sides) for i in range(sides))
    return mesh(name,pts,faces,mat)

def prism(name, corners, delta, mat, bevel=.005):
    points=[v(*p) for p in corners]
    n=len(points)
    faces=[tuple(range(n)),tuple(range(n,2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,points+[p+v(*delta) for p in points],faces,mat,bevel)

def label(name, string, p, size, mat, facing=1):
    curve=bpy.data.curves.new(name,'FONT')
    curve.body=string
    curve.align_x='CENTER'
    curve.align_y='CENTER'
    curve.size=size
    curve.space_character=1.08
    ob=bpy.data.objects.new(name,curve)
    collection.objects.link(ob)
    ob.location=v(*p)
    ob.rotation_euler=(math.pi/2,0,0 if facing==1 else math.pi if facing==-1 else math.pi/2 if facing==2 else -math.pi/2)
    curve.materials.append(materials[mat])
    return ob

def frame(name,a,b,width,depth,mat):
    p,q=v(*a),v(*b)
    t=(q-p).normalized()
    n=t.cross(Vector((0,0,1)))
    if n.length < .01: n=Vector((1,0,0))
    n.normalize()
    m=t.cross(n)
    coords=[o+n*i*width/2+m*j*depth/2 for o in (p,q) for i,j in [(-1,-1),(1,-1),(1,1),(-1,1)]]
    return mesh(name, coords, cube_faces, mat, .004)

def warung():
    module("WarungSri")
    box("Pias dinding", (0,1.9,-.16),(7.32,3.8,.52),"Plaster",.034)
    for end in (-1,1):
        x=end*3.48
        box("Kolom sudut",(x,1.91,.16),(.32,3.82,.44),"Enamel",.018)
        for h in (.23,2.42,3.63):
            box("Lis kolom",(x,h,.19),(.39,.09,.49),"Ivory")
    box("Rangka etalase",(-.39,1.5,.17),(4.77,2.8,.22),"Petrol")
    for i in range(21):
        box("Bilah pintu gulung",(-.4,.4+i*.108,.315),(4.49,.096,.072),"Steel" if i%4 else "Charcoal",.013)
    box("Landasan ambang",(-.4,.22,.37),(4.72,.18,.49),"Tile")
    box("Papan pedagang",(0,3.12,.26),(6.86,.83,.20),"Mustard",.042)
    box("Bingkai gelap",(0,3.12,.373),(6.60,.66,.036),"Petrol",.025)
    label("Nama warung","WARUNG   SRI",(0,3.17,.40),.46,"Ivory")
    label("Jenis dagangan","K O P I     /     S E M B A K O",(.04,2.94,.4),.101,"Mustard")
    for i in range(22):
        x=-3.58+i*.34
        box("Panel terpal",(x,3.66,.63),(.347,.055,1.44),"Oxide" if i%2 else "Ivory",.008,10)
        prism("Rumbai terpal",[(x-.171,3.54,1.337),(x+.171,3.54,1.337),(x+.171,3.38,1.337),
              (x+.10,3.315,1.337),(x-.10,3.315,1.337),(x-.171,3.38,1.337)],(0,0,.022),"Oxide" if i%2 else "Ivory")
    for x in (-3.1,0,3.1):
        frame("Penyiku kanopi",(x,2.95,.30),(x,3.54,1.15),.035,.05,"Steel")
    box("Kios sebelah",(2.60,1.42,.23),(1.53,2.55,.22),"TimberDark",.014)
    for i in range(9):
        box("Papan pintu",(2.0+i*.15,1.36,.36),(.137,2.4,.07),"Timber" if i%3 else "TimberDark")
    box("Gembok",(2.18,1.13,.43),(.13,.16,.07),"Aluminium",.026)
    box("Meja lapak",(-.48,.93,.77),(3.62,.11,.89),"TimberLight",.025)
    for x in (-2.11,1.14):
        for z in (.48,1.05):
            box("Kaki meja",(x,.44,z),(.095,.86,.095),"TimberDark")
    box("Tatakan lapak",(-.49,.34,.75),(3.21,.065,.65),"Timber")
    for i in range(8):
        x=-1.93+i*.16
        rod("Toples kaca",(x,1.01,.81),(x,1.25,.81),.066,"SmokedGlass",14)
        rod("Tutup toples",(x,1.24,.81),(x,1.275,.81),.072,"Mustard",14)
    for i in range(6):
        x=-2.47+i*.16
        for j in range(4):
            box("Renteng sachet kopi",(x,2.46-j*.15,.45),(.137,.145,.036),"Mustard" if i%2 else "Oxide",.012)
            box("Merek sachet",(x,2.48-j*.15,.474),(.079,.028,.006),"Ivory",0)
    box("Lampu TL",(.35,2.65,.53),(1.23,.075,.14),"Signal",.024)
    box("Menu kopi",(.72,1.37,.53),(.67,.65,.048),"Petrol",.014)
    label("Menu tulisan","KOPI\nTUBRUK",(.72,1.39,.559),.13,"Ivory")
    rod("Pipa air",(-3.19,.11,.40),(-3.19,3.64,.40),.044,"Steel")
    box("Meter listrik",(3.12,2.05,.38),(.28,.42,.12),"Aggregate",.025)
    box("Meter kaca",(3.12,2.11,.447),(.19,.105,.015),"SmokedGlass")
    box("Alamat",(-3.04,2.38,.42),(.47,.24,.035),"Petrol")
    label("Nomor rumah","17 A",(-3.04,2.38,.446),.17,"Ivory")


def shelter():
    module("HaltePasar")
    box("Pelat lantai",(0,.07,0),(5.45,.14,2.85),"Aggregate",.035)
    for x in (-2.43,0,2.43):
        box("Tiang belakang",(x,1.51,1.08),(.12,2.9,.14),"Petrol")
        box("Sepatu tiang",(x,.21,1.08),(.25,.14,.28),"Steel")
        frame("Siku atap",(x,2.5,1.07),(x,2.98,.43),.06,.08,"Mustard")
    for x in (-2.42,2.42):
        box("Tiang depan",(x,1.52,-.86),(.105,2.94,.12),"Enamel")
        for z in (-.86,1.06):
            box("Flens",(x,.163,z),(.25,.04,.25),"Steel")
            for dx in (-.082,.082):
                rod("Baut angkur",(x+dx,.18,z+.072),(x+dx,.207,z+.072),.024,"Aluminium",6)
    for x in (-1.24,1.24):
        box("Panel pelindung",(x,1.62,1.13),(2.30,1.91,.06),"SmokedGlass",.024)
        for y in (.72,2.52):
            box("Rel pengapit",(x,y,1.08),(2.35,.07,.10),"Aluminium")
        for i in range(5):
            box("Garis pengaman kaca",(x,.93+i*.074,1.088),(2.25,.018,.008),"Enamel",0)
    for z in (-1.35,0,1.35):
        box("Gording",(0,2.93,z),(5.56,.105,.075),"Steel")
    for i in range(33):
        x=-2.72+i*.17
        box("Seng bergelombang",(x,3.04,-.02),(.18,.055,3.06),"Enamel",.014,3)
        box("Rusuk seng",(x-.07,3.076,-.02),(.025,.037,3.06),"Petrol",.008,3)
    for x in (-2.76,2.76):
        box("Lis samping",(x,3.02,0),(.078,.17,3.12),"Aluminium",.014,3)
    box("Boks papan halte",(0,3.30,-1.42),(5.55,.76,.20),"Petrol",.045)
    box("Bidang papan",(0,3.30,-1.533),(5.36,.61,.032),"Signal",.026)
    box("Panel nomor",(-2.23,3.3,-1.563),(.65,.55,.025),"Petrol",.02)
    label("Nomor trayek","04",(-2.23,3.30,-1.58),.37,"Ivory",-1)
    label("Penanda tujuan","HALTE  PASAR",(.22,3.30,-1.565),.48,"Petrol",-1)
    for x in (-1.65,1.65):
        frame("Penyangga duduk",(x,.21,.66),(x,.59,.25),.065,.065,"Petrol")
        box("Kaki bangku",(x,.36,.70),(.07,.45,.07),"Steel")
        frame("Punggung miring",(x,.55,.76),(x,1.12,.90),.06,.05,"Petrol")
    for j in range(4):
        box("Papan duduk",(0,.61,.32+.13*j),(3.80,.08,.118),"TimberLight" if j%2 else "Timber",.017)
    for j in range(3):
        box("Papan sandaran",(0,.82+j*.12,.79+j*.03),(3.80,.104,.065),"Timber",.014)
    for x in (-1.37,1.37):
        box("Rumah lampu TL",(x,2.87,-.48),(1.62,.09,.16),"Petrol")
        box("Tabung TL",(x,2.81,-.48),(1.51,.05,.095),"Lamp",.018)
    box("Bingkai trayek",(-1.35,1.86,1.045),(1.03,.91,.058),"Aluminium")
    box("Peta trayek",(-1.35,1.86,1.004),(.94,.82,.024),"Paper")
    label("Nama trayek","KORIDOR  04",(-1.35,2.15,.982),.113,"Petrol",-1)
    for i in range(4):
        x=-1.69+i*.223
        box("Garis trayek",(x+.095,1.91,.982),(.21,.019,.01),"Oxide",0)
        rod("Pemberhentian",(x,1.91,.98),(x,1.91,.964),.039,"Petrol",12)
    label("Arah trayek","PASAR    -    KAMPUS",(-1.35,1.65,.974),.076,"Ink",-1)
    rod("Pipa talang",(2.62,.19,.94),(2.62,3.0,.94),.043,"Steel")

def crate():
    module("PetiKayu")
    for side in (-1,1):
        for i in range(6):
            mat=("Timber","TimberLight","Timber","TimberDark","TimberLight","Timber")[i]
            box("Papan sisi",(side*.478,.112+i*.155,0),(.043,.147,.93),mat,.006)
            box("Papan muka",(0,.112+i*.155,side*.478),(.89,.147,.043),mat,.006)
        for edge in (-1,1):
            box("Bingkai sudut",(side*.443,.50,edge*.445),(.11,.99,.11),"TimberLight",.009)
        for i in (0,1):
            box("Pengikat muka",(0,.095+i*.80,side*.495),(.997,.104,.024),"TimberLight",.006)
            box("Pengikat sisi",(side*.495,.095+i*.80,0),(.024,.104,.997),"TimberLight",.006)
        frame("Penyiku silang",(-.385,.19,side*.50),(.385,.81,side*.50),.030,.085,"TimberLight")
        for x in (-.423,.423):
            for y in (.085,.91):
                rod("Kepala paku",(x,y,side*.50),(x,y,side*.511),.015,"Steel",8)
        for z in (-.419,.419):
            for y in (.085,.91):
                rod("Paku pinggir",(side*.50,y,z),(side*.511,y,z),.015,"Steel",8)
    for i in range(7):
        box("Tutup peti",(-.423+i*.141,.974,0),(.134,.049,.98),"Timber" if i%3 else "TimberLight",.006)
    box("Label muatan",(.14,.46,-.508),(.31,.17,.009),"Paper",.004)
    label("Nomor muatan","PSR  17",(.14,.46,-.515),.056,"Ink",-1)
    for x in (-.22,-.11):
        prism("Cetak atas",[(x-.036,.42,-.508),(x+.036,.42,-.508),(x,.50,-.508)],(0,0,-.003),"Ink")
        box("Tangkai simbol",(x,.37,-.508),(.026,.11,.006),"Ink",0)

def barrier():
    module("BetonPolisi")
    side=[(-.32,.045),(-.32,.17),(-.185,.46),(-.135,.91),(.135,.91),(.185,.46),(.32,.17),(.32,.045)]
    prism("Profil beton Jersey",[(-1.325,h,z) for z,h in side],(2.65,0,0),"Concrete",.022)
    for s in (-1,1):
        box("Cat hitam",(0,.69,s*.167),(2.56,.30,.038),"Charcoal",.007, -s*7)
        for i in range(8):
            x=-1.23+i*.32
            prism("Pita peringatan",[(x,.55,s*.201),(x+.143,.55,s*.201),(x+.28,.83,s*.167),(x+.14,.83,s*.167)],
                  (0,0,s*.009),"Mustard",.002)
        for x in (-1.12,1.12):
            rod("Mata kucing",(x,.40,s*.213),(x,.40,s*.23),.055,"Lamp",12)
    for x in (-.83,.83):
        rod("Angkur kiri",(x-.08,.91,0),(x-.08,.99,0),.018,"Steel")
        rod("Angkur kanan",(x+.08,.91,0),(x+.08,.99,0),.018,"Steel")
        rod("Angkur atas",(x-.08,.99,0),(x+.08,.99,0),.018,"Steel")

def sidewalk():
    module("Trotoar")
    box("Bedding",(0,-.08,0),(2,.26,2),"Mortar",.009)
    for row in range(5):
        for col in range(4):
            x=-.70+col*.45
            z=-.8+row*.4
            box("Paving blok",(x,.089,z),(.438,.061,.389),"Paver" if (row*7+col*2)%5 else "PaverDark",.011)
    for side in (-1,1):
        box("Tepi kerb",(side*.947,.068,0),(.106,.105,1.989),"Concrete",.010)
    for i in range(4):
        z=-.75+i*.5
        box("Ubin pemandu",(.33,.122,z),(.305,.020,.488),"Ochre",.006)
        for dx in (-.093,0,.093):
            box("Rusuk taktil",(.33+dx,.141,z),(.022,.022,.42),"Mustard",.007)
    box("Bingkai drainase",(-.65,.127,.46),(.38,.020,.63),"Steel",.008)
    box("Bak drainase",(-.65,.14,.46),(.32,.012,.56),"Ink",.006)
    for i in range(7):
        box("Kisi pembuangan",(-.65,.154,.22+.079*i),(.32,.018,.029),"Steel",.006)

def satchel():
    module("TasRaka")
    box("Badan ransel",(0,.318,.014),(.47,.59,.27),"Canvas",.082)
    box("Kantung depan",(0,.228,-.112),(.40,.27,.095),"Petrol",.045)
    box("Tutup kantung",(0,.347,-.151),(.39,.047,.028),"Strap",.012)
    for x in (-.144,.144):
        frame("Tali punggung",(x,.11,.157),(x,.48,.193),.045,.035,"Strap")
        frame("Ujung tali",(x,.48,.193),(x,.54,.13),.045,.028,"Strap")
    for a,b in [((-.075,.56,.04),(-.07,.659,.02)),((-.07,.659,.02),(.07,.659,.02)),((.07,.659,.02),(.075,.56,.04))]:
        rod("Pegangan",a,b,.015,"Strap")
    for i in range(9):
        box("Jahitan",(-.164+i*.041,.32,-.163),(.019,.004,.002),"TimberLight",0)
    rod("Tarikan ritsleting",(.147,.365,-.17),(.16,.285,-.18),.007,"Aluminium")
    star=[(.16+(.065 if i%2==0 else .028)*math.sin(i*math.pi/5),
           .227+(.065 if i%2==0 else .028)*math.cos(i*math.pi/5),-.183) for i in range(10)]
    prism("Gantungan bintang reflektif",star,(0,0,-.012),"Mustard",.003)
    prism("Inti reflektor",[(.16+(x-.16)*.74,.227+(y-.227)*.74,-.196) for x,y,z in star],(0,0,-.004),"Signal",.001)


def patrol_car():
    module("SedanPatroli")
    # Each station records longitudinal position, lower and upper shoulder heights and half-widths.
    def shell(name, stations, mat, bevel):
        corners = []
        for z,base,top,w0,w1 in stations:
            corners.extend([v(-w0,base,z),v(w0,base,z),v(w1,top,z),v(-w1,top,z)])
        faces=[(0,1,2,3),tuple(range(len(corners)-4,len(corners)))]
        for ring in range(len(stations)-1):
            for j in range(4):
                i=ring*4+j
                n=ring*4+(j+1)%4
                faces.append((i,n,n+4,i+4))
        mesh(name,corners,faces,mat,bevel)
    shell("Carrosserie sedan",[(-2.05,.47,.82,.73,.72),(-1.89,.48,.94,.87,.83),
          (-1.37,.67,.99,.91,.83),(-.76,.69,1.01,.91,.84),(.82,.69,.99,.91,.84),
          (1.39,.66,.96,.915,.83),(1.92,.47,.88,.86,.76),(2.10,.43,.76,.71,.69)],"PatrolWhite",.045)
    shell("Ruang penumpang",[(-1.38,.94,1.04,.81,.72),(-.90,.96,1.46,.83,.63),
          (-.64,.99,1.55,.83,.62),(.33,.98,1.56,.83,.64),(.91,.96,1.04,.81,.77)],"PatrolBlue",.043)
    for side in (-1,1):
        def pane(name, pts):
            prism(name,[(side*x,y,z) for x,y,z in pts],(side*.014,0,0),"SmokedGlass",.013)
        pane("Kaca belakang",[(.827,1.046,-1.20),(.811,1.046,-.38),(.654,1.493,-.39),(.656,1.46,-.83)])
        pane("Kaca depan",[(.829,1.046,-.305),(.822,1.046,.747),(.661,1.49,.27),(.643,1.494,-.305)])
        frame("Lis jendela",(side*.84,1.025,-1.29),(side*.84,1.025,.84),.021,.023,"Aluminium")
        prism("Pintu bawah",[(side*.887,.34,-.84),(side*.887,.34,.83),
              (side*.911,.80,1.04),(side*.911,.82,-1.01)],(side*.012,0,0),"PatrolBlue",.020)
        box("Lis pintu",(side*.917,.80,-.016),(.018,.045,2.27),"Mustard")
        for z in (-.70,.29):
            box("Gagang pintu",(side*.89,.929,z),(.052,.043,.16),"PatrolBlue",.016)
        for z in (-.34,.86):
            frame("Celah pintu",(side*.925,.40,z),(side*.923,.93,z+.065),.014,.012,"Seal")
        label("Marka polisi","POLISI",(side*.93,.604,-.01),.234,"Ivory",side*2)
        frame("Tangkai spion",(side*.80,1.072,.66),(side*.978,1.14,.63),.042,.045,"Seal")
        box("Cangkang spion",(side*1.014,1.153,.62),(.146,.092,.185),"PatrolWhite",.035)
        for z in (-1.37,1.27):
            xc=side*.872
            rod("Ban radial",(xc-side*.106,.355,z),(xc+side*.106,.355,z),.35,"Rubber",32)
            rod("Dinding ban",(xc+side*.105,.355,z),(xc+side*.119,.355,z),.281,"Seal",32)
            rod("Velg mesin",(xc+side*.117,.355,z),(xc+side*.139,.355,z),.228,"Aluminium",24)
            rod("Cakram",(xc+side*.136,.355,z),(xc+side*.146,.355,z),.171,"Charcoal",24)
            for j in range(8):
                a=math.tau*j/8
                dy,dz=math.sin(a),math.cos(a)
                frame("Jari-jari velg",(xc+side*.154,.355+.065*dy,z+.065*dz),
                      (xc+side*.154,.355+.20*dy,z+.20*dz),.031,.024,"Aluminium")
            rod("Tutup poros",(xc+side*.151,.355,z),(xc+side*.168,.355,z),.073,"Steel",12)
            for j in range(5):
                a=math.tau*j/5
                y=.355+.052*math.sin(a)
                zz=z+.052*math.cos(a)
                rod("Mur roda",(xc+side*.168,y,zz),(xc+side*.177,y,zz),.012,"Aluminium",6)
        for z in (-.94,1.66):
            prism("Pita bemper",[(side*.878,.51,z),(side*.914,.71,z+.03),(side*.901,.72,z+.18),(side*.86,.52,z+.16)],
                  (side*.018,0,0),"Mustard",.006)
        rod("Lampu mundur",(side*.647,.77,-2.052),(side*.647,.77,-2.079),.095,"Red",16)
    prism("Kaca depan utama",[(-.736,1.06,.87),(.736,1.06,.87),(.61,1.503,.372),(-.61,1.503,.372)],(0,.011,.006),"SmokedGlass",.018)
    prism("Kaca bagasi",[(-.738,1.073,-1.34),(.738,1.073,-1.34),(.609,1.40,-.92),(-.609,1.40,-.92)],(0,.009,-.009),"SmokedGlass",.018)
    for x in (-.43,.24):
        frame("Wiper",(x-.15,1.083,.835),(x+.28,1.16,.761),.018,.015,"Ink")
    box("Bilah lampu rotator",(0,1.586,-.16),(1.16,.078,.245),"Seal",.025)
    for i,mat in [(-1,"Red"),(1,"Beacon")]:
        box("Lensa rotator",(i*.31,1.664,-.16),(.51,.092,.21),mat,.036)
        for d in (-.15,-.05,.05,.15):
            box("Sekat rotator",(i*.31+d,1.662,-.16),(.014,.086,.218),"Steel",.006)
    box("Radiator",(0,.677,2.085),(.88,.24,.054),"Seal",.026)
    for i in range(5):
        box("Kisi radiator",(0,.586+i*.045,2.115),(.82,.014,.016),"Aluminium",.005)
    for side in (-1,1):
        box("Rumah lampu",(side*.671,.805,2.041),(.34,.177,.119),"Seal",.028)
        box("Mika lampu utama",(side*.671,.819,2.104),(.295,.122,.025),"Lamp",.022)
        box("Sein",(side*.827,.79,2.046),(.052,.104,.054),"Mustard",.013)
    for z in (-2.079,2.104):
        box("Pelat dinas",(0,.476,z),(.49,.13,.035),"Charcoal",.008)
        label("Nomor polisi","1208 - XVII",(0,.475,z+(.023 if z>0 else -.023)),.060,"Ivory",1 if z>0 else -1)
    rod("Antena radio",(-.46,1.00,-1.55),(-.46,1.68,-1.68),.012,"Steel",8,.005)

def shophouse():
    module("RukoPasar")
    box("Dinding ruko",(0,2.72,-.78),(5.50,5.44,1.56),"Plaster",.018)
    box("Rol pintu",(0,1.35,.023),(4.84,2.7,.12),"Charcoal",.012)
    for i in range(17):
        box("Rusuk penutup",(0,.14+i*.153,.096),(4.76,.035,.038),"Steel")
    box("Kanopi ruko",(0,2.93,.36),(5.56,.15,.93),"Tile",.02,4)
    for x in (-1.25,1.25):
        box("Kusen atas",(x,4.20,.022),(1.68,1.54,.10),"Petrol")
        box("Kaca jendela",(x,4.20,.096),(1.49,1.35,.057),"SmokedGlass")
        box("Tiang kusen",(x,4.20,.147),(.056,1.35,.042),"Ivory")
        for i in range(4):
            box("Kisi atas",(x,4.69+i*.086,.151),(1.46,.038,.054),"Timber")
    box("Mesin AC",(1.55,3.27,.24),(.93,.45,.46),"Concrete",.031)
    rod("Kipas outdoor",(1.36,3.26,.476),(1.36,3.26,.498),.17,"Charcoal",24)
    for i in range(5):
        box("Kisi kipas",(1.36,3.14+i*.058,.510),(.29,.016,.016),"Aluminium",.003)
    box("Parapet",(0,5.48,-.26),(5.6,.17,1.1),"Concrete",.024)
    rod("Pipa hujan",(-2.54,.08,.125),(-2.54,5.40,.125),.042,"Steel")
    for y in (.82,2.22,4.76):
        box("Klem pipa",(-2.54,y,.132),(.115,.05,.115),"Oxide")

def asphalt_textures():
    import numpy as np
    rng = np.random.default_rng(1704)
    width = 768
    u = np.arange(width) / width
    def noise(n):
        # Periodic value noise keeps both road edges and successive repeats seamless.
        field = rng.random((n, n))
        p = (u*n).astype(int)
        f = u*n-p
        f = f*f*(3-2*f)
        q = (p+1)%n
        rows = field[:,p]*(1-f) + field[:,q]*f
        return rows[p,:]*(1-f[:,None]) + rows[q,:]*f[:,None]
    grit = rng.random((width,width))
    low = noise(4)*.52 + noise(11)*.28 + noise(28)*.16 + noise(68)*.04
    wet = np.clip((low-.44)*11,0,1)
    blend = wet*wet*(3-2*wet)
    gray = (.31 + (noise(30)-.5)*.083 + (grit-.5)*.12) * (1-.27*blend)
    rgba = np.ones((width,width,4), dtype=np.float32)
    rgba[:,:,0] = gray*.91
    rgba[:,:,1] = gray*.96
    rgba[:,:,2] = gray
    rgba[:,:,3] = .31+.64*blend
    def write(name,data):
        image=bpy.data.images.new(name,width,width,alpha=True)
        image.colorspace_settings.name='Non-Color'
        image.alpha_mode='CHANNEL_PACKED'
        image.pixels.foreach_set(data.ravel())
        image.filepath_raw=str(OUT/(name+'.png'))
        image.file_format='PNG'
        image.save()
        bpy.data.images.remove(image)
    write("WetAsphalt",rgba)
    height=(grit*.22+noise(64))*(1-blend)
    rgba[:,:,0]=.5+(np.roll(height,1,1)-np.roll(height,-1,1))*.44
    rgba[:,:,1]=.5+(np.roll(height,1,0)-np.roll(height,-1,0))*.44
    rgba[:,:,2:]=1
    write("WetAsphaltNormal",rgba)
    # Sawn grain with occasional eccentric knots. Use neutral value maps so all three board stains share it.
    u_grid,v_grid=np.meshgrid(u,u)
    phase=np.sin(math.tau*u_grid)*.24+noise(5)*.92
    radius=np.sqrt((np.sin(math.pi*(u_grid-.35))*.58)**2+np.sin(math.pi*(v_grid-.46))**2)
    knot=np.exp(-radius*radius*44)
    growth=(v_grid*53+phase+noise(14)*.49+2.9*knot)*math.tau
    grain=.88-.19*np.maximum(0,np.sin(growth))**7-.22*knot+.095*(grit-.5)
    for ch in range(3): rgba[:,:,ch]=grain
    rgba[:,:,3]=1
    write("SawnWood",rgba)
    stone=.87+(noise(12)-.5)*.13+(noise(42)-.5)*.09+(grit-.5)*.15
    for ch in range(3): rgba[:,:,ch]=stone
    write("MineralSurface",rgba)

def export():
    # Collapse evaluated geometry for the game. In the .blend every plank, letter and fitting remains editable.
    for index,col in enumerate(modules):
        bpy.context.view_layer.update()
        graph = bpy.context.evaluated_depsgraph_get()
        verts,faces,slots,indices=[],[],[],[]
        for ob in col.objects:
            ev=ob.evaluated_get(graph)
            me=ev.to_mesh()
            if not me: continue
            offset=len(verts)
            verts.extend(ev.matrix_world @ p.co for p in me.vertices)
            slot=me.materials[0] if len(me.materials) else materials["Ink"]
            if slot not in slots: slots.append(slot)
            idx=slots.index(slot)
            for poly in me.polygons:
                faces.append(tuple(offset+i for i in poly.vertices))
                indices.append(idx)
            ev.to_mesh_clear()
        me=bpy.data.meshes.new(col.name+" export")
        me.from_pydata(verts,[],faces)
        me.update()
        for mat in slots: me.materials.append(mat)
        uv=me.uv_layers.new(name="Metre scale")
        for poly,idx in zip(me.polygons,indices):
            poly.material_index=idx
            n=poly.normal
            axes=(1,0) if abs(n.z)>.7 else (1,2) if abs(n.x)>abs(n.y) else (0,2)
            for loop in poly.loop_indices:
                p=me.vertices[me.loops[loop].vertex_index].co
                uv.data[loop].uv=(p[axes[0]],p[axes[1]])
        ob=bpy.data.objects.new(col.name,me)
        scene.collection.objects.link(ob)
        bpy.ops.object.select_all(action="DESELECT")
        ob.select_set(True)
        bpy.context.view_layer.objects.active=ob
        bpy.ops.export_scene.fbx(filepath=str(OUT/(col.name+".fbx")),use_selection=True,axis_forward='-Z',axis_up='Y',
                                 bake_space_transform=True,add_leaf_bones=False,bake_anim=False,object_types={'MESH'})
        bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.meshes.remove(me)
        # Present modules side by side in the source file.
        for part in col.objects:
            part.location += Vector(((index%3)*9, (index//3)*7, 0))
    for area in bpy.context.screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_distance=26
            area.spaces.active.region_3d.view_location=(8,5,1)
            area.spaces.active.shading.color_type='MATERIAL'
            area.spaces.active.overlay.show_overlays=False
    bpy.data.libraries.write(str(SOURCE/"ChapterTwoStreet.blend"), {scene}, fake_user=True, compress=True)
    (SOURCE/"ChapterTwoPalette.json").write_text(json.dumps({"materials":palette},indent=2)+"\n",encoding="utf-8")
    print("Exported",len(modules),"street modules and",sum(len(c.objects) for c in modules),"editable pieces.")

warung()
shelter()
patrol_car()
crate()
barrier()
sidewalk()
satchel()
shophouse()
asphalt_textures()
export()
