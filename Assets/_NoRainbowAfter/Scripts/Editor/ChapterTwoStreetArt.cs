using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NoRainbowAfter.AI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NoRainbowAfter.Editor
{
    internal static class ChapterTwoStreetArt
    {
        const string Kit = ChapterTwoStreetBuilder.Art + "/Models/Environment/ChapterTwo";
        const string Mats = ChapterTwoStreetBuilder.Art + "/Materials/ChapterTwo";
        [Serializable] class Palette { public Swatch[] materials; }
        [Serializable] class Swatch { public string name, hex; public float smooth, metal, glow; }

        static Dictionary<string,Material> ImportKit()
        {
            var palette = JsonUtility.FromJson<Palette>(File.ReadAllText("BlenderSources/Environment/ChapterTwoPalette.json"));
            var result = new Dictionary<string,Material>();
            foreach (var color in palette.materials)
            {
                string path = Mats + "/" + color.name + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(mat,path);
                }
                ColorUtility.TryParseHtmlString("#" + color.hex, out Color tint);
                mat.SetColor("_BaseColor",tint);
                mat.SetFloat("_Smoothness",color.smooth);
                mat.SetFloat("_Metallic",color.metal);
                string texture = color.name.StartsWith("C2_Timber",StringComparison.Ordinal) ? "SawnWood"
                    : (color.name=="C2_Concrete" || color.name=="C2_Plaster" || color.name=="C2_Aggregate" ||
                        color.name.StartsWith("C2_Paver",StringComparison.Ordinal)) ? "MineralSurface" : null;
                if(texture!=null) mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Kit+"/"+texture+".png"));
                if (color.glow > 0f)
                {
                    mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", tint * color.glow);
                }
                if (color.name == "C2 Smoked Bus Glass")
                {
                    tint.a = .49f;
                    mat.SetColor("_BaseColor", tint);
                    mat.SetFloat("_Surface", 1f);
                    mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    mat.SetFloat("_ZWrite", 0f);
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue = (int)RenderQueue.Transparent;
                }
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
                result.Add(color.name, mat);
            }
            foreach (string path in Directory.GetFiles(Kit,"*.fbx"))
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                importer.importCameras = importer.importLights = importer.importAnimation = false;
                importer.bakeAxisConversion = true;
                // Imported slot labels map to the same URP material on every instance of the kit.
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(importer.assetPath);
                var slotNames=model.GetComponentsInChildren<MeshRenderer>().SelectMany(r=>r.sharedMaterials)
                    .Where(m=>m!=null).Select(m=>m.name).Distinct().ToArray();
                foreach(var id in importer.GetExternalObjectMap().Keys.ToArray()) importer.RemoveRemap(id);
                foreach (string slot in slotNames)
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),slot),result[slot]);
                importer.SaveAndReimport();
            }
            return result;
        }

        public static void Dress(ChapterTwoStreet street)
        {
            var mats = ImportKit();
            Transform root = street.transform;
            Transform ground = root.Find("Jalan"), stop=root.Find("Halte"), shop=root.Find("Warung");
            var sedan = AssetDatabase.LoadAssetAtPath<GameObject>(Kit+"/SedanPatroli.fbx");
            // Determine the FBX importer's longitudinal orientation from the actual headlamp mesh.
            var filter = sedan.GetComponentInChildren<MeshFilter>();
            var slots = filter.GetComponent<Renderer>().sharedMaterials;
            int headlight = Array.IndexOf(slots, mats["C2_Lamp"]);
            if(headlight<0) throw new InvalidDataException("Missing imported headlamp material");
            float lensZ = filter.transform.TransformPoint(filter.sharedMesh.GetSubMesh(headlight).bounds.center).z;
            float yaw = lensZ < 0 ? 180f : 0f;

            Transform Place(string model,Transform parent,Vector3 localPosition,Vector3 scale,float angle=0f)
            {
                string path=Kit+"/"+model+".fbx";
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(asset==null) throw new FileNotFoundException(path);
                var obj=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);
                obj.transform.localPosition=localPosition;
                obj.transform.localRotation=Quaternion.Euler(0,yaw+angle,0);
                obj.transform.localScale=scale;
                return obj.transform;
            }

            RemoveVisuals(shop);
            Place("WarungSri",shop,Vector3.zero,Vector3.one);
            var counter=shop.gameObject.AddComponent<BoxCollider>();
            counter.center=new Vector3(-.48f,.49f,.77f);counter.size=new Vector3(3.62f,.97f,.88f);
            RemoveVisuals(stop);
            Place("HaltePasar",stop,Vector3.zero,Vector3.one);
            // Bag's box is also the interactable raycast target.
            Place("TasRaka",stop,new Vector3(.50f,.654f,.48f),Vector3.one);
            RemoveVisuals(street.PatrolCar);
            Place("SedanPatroli",street.PatrolCar,Vector3.zero,Vector3.one);
            foreach (Transform prop in root.Find("Cover"))
            {
                RemoveVisuals(prop);
                if(prop.name.StartsWith("Peti",StringComparison.Ordinal))
                    Place("PetiKayu",prop,Vector3.down*.5f,Vector3.one);
                else
                    Place("BetonPolisi",prop,Vector3.zero,Vector3.one);
            }
            foreach (Transform curb in ground)
                if(curb.name=="Trotoar" || curb.name=="Kerb joint" || curb.name=="Batas gang" || curb.name=="Ujung jalan")
                    RemoveVisuals(curb);
            for(int side=-1;side<=1;side+=2)
            {
                for(int z=-3;z<42;z+=2)
                    Place("Trotoar",ground,new Vector3(side*5.8f,0,z),Vector3.one,side<0?180:0);
                for(int i=0;i<9;i++)
                {
                    float z=-2.15f+5.50f*i;
                    if(side==-1 && i==1) continue;
                    var house=Place("RukoPasar",ground,new Vector3(side*7.51f,0,z),new Vector3(1, .89f+.105f*((i+side+3)%4),1),-90*side);
                    if(i%3==0)
                    {
                        var lamp=new GameObject("Lampu lantai atas").AddComponent<Light>();
                        lamp.transform.SetParent(house,false);
                        lamp.transform.localPosition=new Vector3(0,4.0f, yaw==0 ? .78f : -.78f);
                        lamp.type=LightType.Point;lamp.range=6;lamp.intensity=2.5f;
                        lamp.color=new Color(1f,.73f,.48f);
                    }
                }
            }
            // Shop row beyond the intersection gives the shelter a silhouette above the traffic closure.
            for(int i=-2;i<=2;i++)
                Place("RukoPasar",ground,new Vector3(i*5.65f,0,45f),new Vector3(1,1.0f+.13f*(i%2),1),180);
            var signs=new[]{("BENGKEL SURYA",1,14.35f),("CUCI  SETRIKA",-1,19.85f),("TOKO  BERAS",1,30.85f)};
            foreach(var sign in signs)
            {
                var plate=GameObject.CreatePrimitive(PrimitiveType.Cube);
                plate.name=sign.Item1;plate.transform.SetParent(ground,false);
                plate.transform.localPosition=new Vector3(sign.Item2*7.29f,3.06f,sign.Item3);
                plate.transform.localRotation=Quaternion.Euler(0,90f*sign.Item2,0);
                plate.transform.localScale=new Vector3(4.2f,.52f,.08f);
                plate.GetComponent<Renderer>().sharedMaterial=mats["C2_Petrol"];
                Object.DestroyImmediate(plate.GetComponent<Collider>());
                Lettering(ground,sign.Item1,plate.transform.localPosition-new Vector3(sign.Item2*.052f,0,0),
                    plate.transform.localRotation,4.17f,.50f,.28f,new Color(.81f,.76f,.62f));
            }

            var tar=ground.Find("Two lanes").GetComponent<Renderer>().sharedMaterial;
            var normal=(TextureImporter)AssetImporter.GetAtPath(Kit+"/WetAsphaltNormal.png");
            normal.textureType=TextureImporterType.NormalMap; normal.SaveAndReimport();
            var wetMap=AssetDatabase.LoadAssetAtPath<Texture2D>(Kit+"/WetAsphalt.png");
            tar.SetColor("_BaseColor",Color.white);
            tar.SetTexture("_BaseMap",wetMap);
            tar.SetTextureScale("_BaseMap",new Vector2(2.2f,10.54f));
            tar.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Kit+"/WetAsphaltNormal.png"));
            tar.SetFloat("_BumpScale",.56f);
            tar.SetFloat("_Smoothness",.96f);
            tar.SetFloat("_SmoothnessTextureChannel",1f);
            tar.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            tar.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(tar);
            AddStreetLight(root);
            AddCables(root,mats["C2_Ink"]);
            // The tape closes the kerbs, leaving the central opening and the walkway to the shelter.
            foreach(float x in new[]{-5.52f,5.60f})
            {
                var tape=new GameObject("Garis polisi").transform;
                tape.SetParent(root,false);tape.localPosition=new Vector3(x,.89f,35.5f);
                foreach(float dx in new[]{-1.07f,1.07f})
                {
                    var post=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    post.transform.SetParent(tape,false);post.transform.localPosition=new Vector3(dx,-.36f,0);
                    post.transform.localScale=new Vector3(.05f,.53f,.05f);
                    post.GetComponent<Renderer>().sharedMaterial=mats["C2_Steel"];
                }
                var ribbon=GameObject.CreatePrimitive(PrimitiveType.Cube);
                ribbon.transform.SetParent(tape,false);ribbon.transform.localScale=new Vector3(2.16f,.125f,.012f);
                ribbon.GetComponent<Renderer>().sharedMaterial=mats["C2_Mustard"];
                Lettering(tape,"GARIS POLISI     /     DILARANG MELINTAS",new Vector3(0,0,-.012f),
                    Quaternion.identity,2.12f,.12f,.065f,Color.black);
            }
            var sky=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sky.name="Langit mendung";sky.transform.SetParent(root,false);
            sky.transform.localPosition=new Vector3(0,0,19);sky.transform.localScale=Vector3.one*128;
            Object.DestroyImmediate(sky.GetComponent<Collider>());
            var skyMat=AssetDatabase.LoadAssetAtPath<Material>(Mats+"/Overcast.mat");
            if(skyMat==null){skyMat=new Material(Shader.Find("NoRainbowAfter/OvercastStreet"));AssetDatabase.CreateAsset(skyMat,Mats+"/Overcast.mat");}
            var skyRenderer=sky.GetComponent<Renderer>();
            skyRenderer.sharedMaterial=skyMat;skyRenderer.shadowCastingMode=ShadowCastingMode.Off;skyRenderer.receiveShadows=false;
            Debug.Log("[C2 art] Eight FBX modules placed. Headlight z = "+lensZ+"; kit yaw = "+yaw);
        }

        // World-space UI depth-tests against cover. Legacy TextMesh uses an overlay font material.
        static void Lettering(Transform parent,string value,Vector3 position,Quaternion rotation,
            float width,float height,float typeHeight,Color color)
        {
            const float pixelsPerMetre=200f;
            var go=new GameObject(value,typeof(RectTransform),typeof(Canvas));
            var rect=(RectTransform)go.transform;
            rect.SetParent(parent,false);rect.localPosition=position;rect.localRotation=rotation;
            rect.sizeDelta=new Vector2(width,height)*pixelsPerMetre;
            rect.localScale=Vector3.one/pixelsPerMetre;
            go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var letters=new GameObject("Cetakan",typeof(RectTransform),typeof(UnityEngine.UI.Text));
            var text=letters.GetComponent<UnityEngine.UI.Text>();var area=text.rectTransform;
            area.SetParent(rect,false);area.anchorMin=Vector2.zero;area.anchorMax=Vector2.one;
            area.offsetMin=area.offsetMax=Vector2.zero;
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text=value;
            text.alignment=TextAnchor.MiddleCenter;text.fontSize=Mathf.RoundToInt(typeHeight*pixelsPerMetre);
            text.color=color;text.raycastTarget=false;
        }

        static void RemoveVisuals(Transform target)
        {
            foreach(var r in target.GetComponentsInChildren<Renderer>(true))
            {
                var text=r.GetComponent<TextMesh>();
                if(text!=null) Object.DestroyImmediate(text);
                Object.DestroyImmediate(r);
            }
            foreach(var f in target.GetComponentsInChildren<MeshFilter>(true)) Object.DestroyImmediate(f);
        }

        static void AddStreetLight(Transform root)
        {
            var go=new GameObject("Cahaya langit setelah hujan");
            go.transform.SetParent(root,false);
            go.transform.localRotation=Quaternion.Euler(49,-34,0);
            var moon=go.AddComponent<Light>();moon.type=LightType.Directional;
            moon.color=new Color(.66f,.78f,.93f);moon.intensity=1.24f;
            moon.shadows=LightShadows.Soft;moon.shadowStrength=.56f;
            var probe=new GameObject("Pantulan jalan").AddComponent<ReflectionProbe>();
            probe.transform.SetParent(root,false);probe.transform.localPosition=new Vector3(0,2.1f,18.8f);
            probe.size=new Vector3(17,13,47);probe.resolution=128;
            probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.AllFacesAtOnce;probe.boxProjection=true;
            probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=new Color(.19f,.22f,.26f);
        }

        static void AddCables(Transform root,Material cable)
        {
            for(int side=-1;side<=1;side+=2)
                for(int wire=0;wire<3;wire++)
                {
                    var line=new GameObject("Kabel udara").AddComponent<LineRenderer>();
                    line.transform.SetParent(root,false);line.useWorldSpace=false;
                    line.sharedMaterial=cable;line.startWidth=line.endWidth=.022f;
                    line.positionCount=37;
                    for(int i=0;i<37;i++)
                    {
                        float sag=Mathf.Sin(Mathf.PI*(i%12)/12f);
                        line.SetPosition(i,new Vector3(side*(5.65f+.17f*wire),6.8f-.76f*sag+.10f*wire,-4+i*1.34f));
                    }
                }
        }
    }
}
