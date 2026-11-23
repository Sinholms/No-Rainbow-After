using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NoRainbowAfter.AI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace NoRainbowAfter.Editor
{
    internal static class ChapterTwoStreetArt
    {
        const string Kit = ChapterTwoStreetBuilder.Art + "/Models/Environment/ChapterTwo";
        const string Mats = ChapterTwoStreetBuilder.Art + "/Materials/ChapterTwo";
        const string DormSurfaces = ChapterTwoStreetBuilder.Art + "/Textures/KosAtmosphere/";
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
                if (color.name == "C2_Aggregate" || color.name.StartsWith("C2_Paver", StringComparison.Ordinal))
                    mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Kit + "/MineralSurface.png"));
                if (color.name == "C2 Sentry Raincoat" || color.name == "C2 Sentry Vest")
                    SetSurfaceNormal(mat, "Cloth", .27f);
                if (color.name == "C2 Sentry Skin")
                {
                    mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Kit + "/SentryFace.jpg"));
                    mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Kit + "/SentryFaceNormal.jpg"));
                    mat.SetFloat("_BumpScale", .55f);
                    mat.EnableKeyword("_NORMALMAP");
                }
                DressSurface(mat, color.name);
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

        static void SetSurfaceNormal(Material material, string surface, float strength)
        {
            var path = DormSurfaces + surface + "_Normal.png";
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            material.SetFloat("_BumpScale", strength);
            material.EnableKeyword("_NORMALMAP");
        }

        static void DressSurface(Material material, string name)
        {
            switch (name)
            {
                case "C2_Plaster":
                case "C2_Concrete":
                    ApplyDormSurface(material, "Wall", .45f, .35f, .62f);
                    break;
                case "C2_Timber":
                case "C2_TimberDark":
                case "C2_TimberLight":
                    ApplyDormSurface(material, "Wood", .53f, .5f, .85f);
                    material.SetColor("_BaseColor", Color.Lerp(material.color, Color.white, .51f));
                    break;
                case "C2_SmokedGlass":
                case "C2 Smoked Bus Glass":
                    ApplyDormSurface(material, "WindowRain", .11f, 0f, .68f);
                    break;
                case "C2_Charcoal":
                case "C2_Petrol":
                case "C2_Steel":
                case "C2_PatrolWhite":
                case "C2_PatrolBlue":
                case "C2 Bus Teal":
                case "C2 Bus Ivory":
                case "C2_Enamel":
                    material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Kit + "/WeatheredEnamel.png"));
                    material.SetTextureScale("_BaseMap", Vector2.one * .69f);
                    material.SetFloat("_SmoothnessTextureChannel", 1f);
                    material.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                    SetSurfaceNormal(material, "Metal", .12f);
                    break;
                case "C2_Canvas":
                case "C2_Oxide":
                    SetSurfaceNormal(material, "Cloth", .39f);
                    material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Kit + "/WeatheredEnamel.png"));
                    break;
            }
        }

        static void ApplyDormSurface(Material material, string surface, float relief, float occlusion, float tiling)
        {
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(DormSurfaces + surface + "_BaseColor.png"));
            material.SetTextureScale("_BaseMap", Vector2.one * tiling);
            SetSurfaceNormal(material, surface, relief);
            if (occlusion <= 0f) return;
            material.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(DormSurfaces + surface + "_AO.png"));
            material.SetFloat("_OcclusionStrength", occlusion);
            material.EnableKeyword("_OCCLUSIONMAP");
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
                if (prop.name == "Peti B" || prop.name == "Peti C" || prop.name == "Peti warung")
                    Place("MuatanPasar", prop, Vector3.down * .5f, Vector3.one);
                else if(prop.name.StartsWith("Peti",StringComparison.Ordinal))
                    Place("PetiKayu",prop,Vector3.down*.5f,Vector3.one);
                else
                    Place("BetonPolisi",prop,Vector3.zero,Vector3.one);
            }
            foreach (Transform curb in ground)
                if(curb.name=="Trotoar" || curb.name=="Kerb joint" || curb.name=="Batas gang")
                    RemoveVisuals(curb);
            for(int side=-1;side<=1;side+=2)
            {
                for(int z=-3;z<ChapterTwoStreetBuilder.CordonZ+2;z+=2)
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
            // Beyond the cordon, setbacks and gaps open the market onto a longer street.
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 8; i++)
                {
                    float z = 49f + 8.2f * i + (side > 0 ? 2.7f : 0f);
                    float setback = 8.7f + (i % 3) * 1.2f;
                    Place("RukoPasar", ground, new Vector3(side * setback,0,z),
                        new Vector3(1.15f,.78f + (i % 3)*.17f,1f), -90f*side);
                }
            // The end wall still closes the road behind the spawn, dressed as another bank of shutters.
            RemoveVisuals(ground.Find("Di belakang warung"));
            for (int i = -1; i <= 1; i++)
                Place("RukoPasar", ground, new Vector3(i * 5.55f, 0, -5.2f), Vector3.one);
            Place("GapuraPasar", ground, new Vector3(0, 0, 12.5f), Vector3.one);
            // A small hooded sign lamp makes the gate readable from the doorway.
            var gateLamp = new GameObject("Lampu papan pasar").AddComponent<Light>();
            gateLamp.transform.SetParent(ground, false);
            gateLamp.transform.localPosition = new Vector3(-1.6f, 5.75f, 11.65f);
            gateLamp.transform.localRotation = Quaternion.LookRotation(new Vector3(.85f, -.40f, .72f));
            gateLamp.type = LightType.Spot;
            gateLamp.spotAngle = 108f;
            gateLamp.range = 4.5f;
            gateLamp.color = new Color(.87f, .77f, .57f);
            gateLamp.intensity = 4.5f;
            foreach (var stall in new[] {
                (model: "LapakBumbu", side: -1, z: 12.0f, lit: true),
                (model: "LapakIkan", side: 1, z: 21.7f, lit: true),
                (model: "LapakBumbu", side: 1, z: 33.4f, lit: false),
                (model: "LapakIkan", side: -1, z: 23.7f, lit: false) })
            {
                var centre = new Vector3(stall.side * 6.04f, .15f, stall.z);
                Place(stall.model, ground, centre, Vector3.one, -90f * stall.side);
                var stallCounter = new GameObject("Meja lapak").AddComponent<BoxCollider>();
                stallCounter.transform.SetParent(ground, false);
                stallCounter.transform.localPosition = centre + Vector3.up * .45f;
                stallCounter.size = new Vector3(.99f, .90f, 3.06f);
                if (stall.lit)
                {
                    var bulb = new GameObject("Bohlam lapak").AddComponent<Light>();
                    bulb.transform.SetParent(ground, false);
                    float lampX = stall.model == "LapakIkan" ? -.58f : .49f;
                    bulb.transform.localPosition = centre + new Vector3(-stall.side * .12f, 1.96f, -stall.side * lampX);
                    bulb.type = LightType.Spot;
                    bulb.transform.localRotation = Quaternion.LookRotation(new Vector3(-stall.side * .41f, -1f, 0f));
                    bulb.spotAngle = 105f;
                    bulb.innerSpotAngle = 73f;
                    bulb.color = stall.model == "LapakIkan" ? new Color(.56f,.83f,.75f) : new Color(1f,.64f,.34f);
                    bulb.range = 6.5f;
                    bulb.intensity = 7.8f;
                    bulb.shadows = LightShadows.Soft;
                }
            }
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
            tar.SetTextureScale("_BaseMap",new Vector2(2.2f,ground.Find("Two lanes").localScale.z * (2.2f / 9.6f)));
            tar.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Kit+"/WetAsphaltNormal.png"));
            tar.SetFloat("_BumpScale",.34f);
            tar.SetFloat("_Smoothness",.83f);
            tar.SetFloat("_SmoothnessTextureChannel",1f);
            tar.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            tar.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(tar);
            AddStreetLight(root);
            AddCables(root,mats["C2_Ink"]);
            var sky=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sky.name="Langit mendung";sky.transform.SetParent(root,false);
            sky.transform.localPosition=new Vector3(0,0,19);sky.transform.localScale=Vector3.one*128;
            Object.DestroyImmediate(sky.GetComponent<Collider>());
            var skyMat=AssetDatabase.LoadAssetAtPath<Material>(Mats+"/Overcast.mat");
            if(skyMat==null){skyMat=new Material(Shader.Find("NoRainbowAfter/OvercastStreet"));AssetDatabase.CreateAsset(skyMat,Mats+"/Overcast.mat");}
            var skyRenderer=sky.GetComponent<Renderer>();
            skyRenderer.sharedMaterial=skyMat;skyRenderer.shadowCastingMode=ShadowCastingMode.Off;skyRenderer.receiveShadows=false;
            Debug.Log("[C2 art] Street kit placed. Headlight z = "+lensZ+"; kit yaw = "+yaw);
        }

        // World-space UI depth-tests against cover. Legacy TextMesh uses an overlay font material.
        internal static void Lettering(Transform parent,string value,Vector3 position,Quaternion rotation,
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
            moon.color=new Color(.62f,.74f,.83f);moon.intensity=.88f;
            moon.shadows=LightShadows.Soft;moon.shadowStrength=.74f;
            var probe=new GameObject("Pantulan jalan").AddComponent<ReflectionProbe>();
            probe.transform.SetParent(root,false);probe.transform.localPosition=new Vector3(0,2.1f,18.8f);
            probe.size=new Vector3(17,13,47);probe.resolution=128;
            probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.AllFacesAtOnce;probe.boxProjection=true;
            probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=new Color(.075f,.112f,.14f);

            var grade = new GameObject("Suasana pasar malam").AddComponent<Volume>();
            grade.transform.SetParent(root, false);
            grade.transform.localPosition = new Vector3(0f, 3.5f, 18.8f);
            grade.isGlobal = false;
            grade.priority = 4f;
            grade.weight = .72f;
            string gradePath = Mats + "/PasarMalam.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(gradePath);
            if (profile == null)
            {
                AssetDatabase.CopyAsset(ChapterTwoStreetBuilder.Art + "/Materials/KosAtmosphere/KosAtmosphereVolume.asset", gradePath);
                profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(gradePath);
            }
            if (profile.TryGet(out ColorAdjustments color))
            {
                color.postExposure.Override(.55f);
                color.contrast.Override(3f);
                EditorUtility.SetDirty(color);
            }
            if (!profile.TryGet(out LiftGammaGain shadows))
            {
                shadows = profile.Add<LiftGammaGain>();
                AssetDatabase.AddObjectToAsset(shadows, profile);
            }
            shadows.lift.Override(new Vector4(.999f, 1f, 1.002f, .004f));
            EditorUtility.SetDirty(shadows);
            EditorUtility.SetDirty(profile);
            grade.sharedProfile = profile;
            var bounds = grade.gameObject.AddComponent<BoxCollider>();
            bounds.isTrigger = true;
            bounds.size = new Vector3(21f, 12f, 54f);
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
