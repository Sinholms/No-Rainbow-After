import bpy
from pathlib import Path
ROOT=Path('D:/GRAVKOM/No rainbow after')
bpy.ops.wm.read_factory_settings(use_empty=True)
wood=bpy.data.materials.new('MAT_H_Wood')
def panel(name,loc,size):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=bpy.context.object;o.name=name;o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    bevel=o.modifiers.new('Rounded worn cabinet edges','BEVEL');bevel.width=.012;bevel.segments=2
    o.data.materials.append(wood)
for name,loc,size in [
 ('Wardrobe_Left',(-.73,0,1.15),(.08,1.3,2.3)),('Wardrobe_Right',(.73,0,1.15),(.08,1.3,2.3)),
 ('Wardrobe_Back',(0,.61,1.15),(1.46,.08,2.3)),('Wardrobe_Top',(0,0,2.3),(1.5,1.3,.08)),
 ('Wardrobe_Floor',(0,0,.02),(1.5,1.3,.04)),
 ('Door_Lower',(0,-.61,.75),(1.4,.055,1.50)),('Door_Upper',(0,-.61,2.03),(1.4,.055,.54)),
 ('Door_SlitLeft',(-.39,-.61,1.63),(.62,.055,.26)),('Door_SlitRight',(.39,-.61,1.63),(.62,.055,.26)),
 ('Door_Handle',(.48,-.67,1.1),(.035,.08,.20))]:panel(name,loc,size)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/_NoRainbowAfter/Art/Models/Environment/PROP_C1_HidingWardrobe.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'BlenderSources/Environment/C1_HidingWardrobe.blend'))
