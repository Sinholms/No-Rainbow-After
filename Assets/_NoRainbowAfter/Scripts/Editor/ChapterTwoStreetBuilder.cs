using System.IO;
using NoRainbowAfter.AI;
using NoRainbowAfter.Interaction;
using UnityEditor;
using UnityEngine;

namespace NoRainbowAfter.Editor
{
    // The editable prefab is also the street loaded by the report transition.
    public static class ChapterTwoStreetBuilder
    {
        internal const string Art = "Assets/_NoRainbowAfter/Art";
        private const string MatDir = Art + "/Materials/ChapterTwo";
        private const string Destination = "Assets/_NoRainbowAfter/Resources/ChapterTwoStreet.prefab";

        [MenuItem("No Rainbow After/Chapter 2/Rebuild Street")]
        public static void Build()
        {
            Directory.CreateDirectory(MatDir);
            AssetDatabase.Refresh();
            var root = new GameObject("ChapterTwoStreet");
            try
            {
                var street = root.AddComponent<ChapterTwoStreet>();
                var t = root.transform;
                var tar = Material("Wet asphalt", "424954", .89f);
                var concrete = Material("Kerb", "878278", .52f);
                var timber = Material("Packing wood", "A78357", .30f);
                var ink = Material("Night metal", "283847", .57f);
                var teal = Material("Enamel green", "327875", .62f);
                var stripe = Material("Worn road paint", "C8BA93", .70f);
                var sand = Material("Police ochre", "D5AC50", .40f);
                var wall = Material("Shophouse plaster", "697475", .31f);
                var warm = Material("Warm lightbox", "FFE4AD", .45f, 1.1f);

                var ground = Node(t, "Jalan", 0, 0, 0);
                Box(ground, "Two lanes", 0,-.18f,19, 9.6f,.36f,46,tar);
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(ground, "Trotoar", side*5.8f,-.06f,19,2,.36f,46,concrete);
                    for (int z = -3; z < 42; z += 2)
                        Box(ground, "Kerb joint", side*4.87f,.126f,z,.14f,.008f,1.92f,concrete,false);
                    Box(ground, "Edge line", side*4.53f,.008f,19,.08f,.01f,45,stripe,false);
                    Box(ground, "Batas gang", side*6.92f,2.1f,19,.25f,4.2f,46,wall);
                }
                for (int z = -1; z < 38; z += 5)
                    Box(ground,"Lane marking",0,.008f,z,.12f,.012f,2.3f,stripe,false);
                Box(ground,"Ujung jalan",0,2,42,14,4,.35f,wall);
                Box(ground,"Di belakang warung",0,2,-4,14,4,.35f,wall);

                var shop = Node(t,"Warung", -6.56f,.12f,3.8f,90);
                Box(shop,"Facade",0,1.8f,0,7.2f,3.6f,.44f,teal);
                Box(shop,"Kios tutup",0,1.26f,.27f,4.35f,2.5f,.14f,ink);
                Box(shop,"Kanopi",0,3.18f,.53f,7.65f,.15f,1.78f,teal);
                Box(shop,"Papan nama",0,2.84f,.35f,5.9f,.52f,.11f,sand);
                Sign(shop,"WARUNG  SRI",0,2.86f,.42f,.34f,ink.color,180);
                LightAt(shop, "Lampu warung", 0,2.65f,1.0f,new Color(1,.72f,.42f), 3f,8);
                var stop = Node(t,"Halte",2.55f,0,38.65f);
                Box(stop,"Plinth",0,.07f,0,5.45f,.14f,2.85f,concrete);
                Box(stop,"Rear panel",0,1.3f,1.12f,5.1f,2.35f,.18f,teal);
                Box(stop,"Bench",0,.56f,.55f,3.8f,.20f,.52f,timber);
                foreach (float x in new[]{-2.42f,2.42f})
                    Box(stop,"Shelter post",x,1.52f,-.85f,.14f,3,.14f,teal);
                Box(stop,"Canopy",0,3.02f,0,5.6f,.2f,3.12f,teal);
                Box(stop,"Illuminated destination",0,3.36f,-1.40f,5.4f,.77f,.17f,warm);
                Sign(stop,"HALTE   PASAR",0,3.36f,-1.50f,.48f,ink.color,0);
                LightAt(stop,"Lampu tunggu",0,2.67f,-.3f,new Color(.76f,1f,.91f),4,9);
                var zone = Node(stop,"Zona aman",0,1.2f,-.10f).gameObject.AddComponent<BoxCollider>();
                zone.isTrigger=true; zone.size=new Vector3(5.5f,2.6f,3.15f);
                street.Shelter=zone;
                var bag=Box(stop,"Tas Raka",.50f,.99f,.48f,.50f,.67f,.30f,ink);
                var trinket=Box(bag,"Gantungan reflektor",.34f,-.12f,-.56f,.17f,.14f,.10f,warm,false);
                trinket.localRotation=Quaternion.Euler(0,0,45);
                street.Bag=bag.gameObject.AddComponent<SceneInteractable>();
                street.Bag.Configure("INT_C2_BAG","Tas di bangku","Periksa tas","c2.bag","Tas Raka",
                    "Gantungan reflektor bentuk bintang itu lecet di ujungnya. Ini tas gue. Kenapa bisa ada di halte ini?",true);

                var cover=Node(t,"Cover",0,0,0);
                Crate(cover,"Peti rendah A",-1.9f,6.9f,1.22f,1.12f,3.5f,timber);
                Crate(cover,"Peti warung",-4.8f,2.9f,1.35f,1.85f,1.55f,timber);
                Crate(cover,"Peti B",2.1f,14.25f,2.5f,1.88f,1.2f,timber);
                Crate(cover,"Peti seberang",-2.65f,20f,1.4f,1.40f,1.6f,timber);
                Crate(cover,"Peti C",-.55f,25.25f,2.3f,1.85f,1.2f,timber);
                Crate(cover,"Peti jalur pinggir",4.82f,27.35f,1.85f,1.60f,1.25f,timber);
                Crate(cover,"Peti pinggir kedua",4.34f,32.6f,1.75f,1.8f,1.2f,timber);
                Barrier(cover,1.4f,30.3f,0,concrete,sand);
                Barrier(cover,-4.75f,26.5f,90,concrete,sand);
                Barrier(cover,-2.4f,35.4f,0,concrete,sand);

                var a=Car(t,"Patroli depan warung",3.5f,8.4f,-101,ink,warm);
                var c=Car(t,"Patroli garis polisi", -3.85f,37.2f,126,ink,warm);
                var guard=Shadow(t,"Shadow berjaga",4.38f,19.3f,-103);
                var patrol=Shadow(t,"Shadow berpatroli",-3.8f,28.5f,69);
                street.Lamps=new[]{a.Find("Lampu kiri"),a.Find("Lampu kanan"),guard.Find("Sorotan"),
                    patrol.Find("Sorotan"),c.Find("Lampu kiri"),c.Find("Lampu kanan")};
                street.ShadowWrappers=new Transform[]{null,null,guard.Find("VisualWrapper"),patrol.Find("VisualWrapper"),null,null};
                street.Checkpoints=new[]{Node(t,"Checkpoint A",-3.6f,.04f,1.3f,10),
                    Node(t,"Checkpoint B",1.38f,.04f,12.95f,-10),Node(t,"Checkpoint C",-.58f,.04f,23.92f,12)};
                for(int i=0;i<3;i++)
                {
                    float x=i%2==0 ? 5.96f : -5.96f, z=6+i*12;
                    Box(t,"Tiang PJU",x,2.7f,z,.15f,5.4f,.15f,ink);
                    Box(t,"Lengan PJU",x-Mathf.Sign(x)*.6f,5.32f,z,1.3f,.08f,.11f,ink,false);
                    Box(t,"Lentera",x-Mathf.Sign(x)*1.21f,5.26f,z,.47f,.12f,.73f,warm,false);
                    LightAt(t,"Cahaya jalan",x-Mathf.Sign(x)*1.2f,4.99f,z,new Color(1f,.81f,.55f),7f,14);
                }
                PrefabUtility.SaveAsPrefabAsset(root,Destination);
                AssetDatabase.SaveAssets();
            }
            finally { Object.DestroyImmediate(root); }
        }

        static Transform Car(Transform t,string name,float x,float z,float yaw,Material metal,Material glass)
        {
            var car=Node(t,name,x,0,z,yaw);
            Box(car,"Body",0,.65f,0,1.84f,.77f,4.14f,metal);
            Box(car,"Cabin",0,1.30f,-.22f,1.59f,.62f,2.06f,metal);
            foreach(float side in new[]{-.70f,.70f})
            {
                Box(car,"Headlight",side,.82f,2.085f,.35f,.17f,.05f,glass,false);
                var lamp=Node(car,side<0?"Lampu kiri":"Lampu kanan",side,.82f,2.16f);
                lamp.localRotation=Quaternion.Euler(2f,0,0);
            }
            return car;
        }

        static Transform Shadow(Transform t,string name,float x,float z,float yaw)
        {
            var actor=Node(t,name,x,0,z,yaw);
            var wrapper=Node(actor,"VisualWrapper",0,0,0);
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Prefabs/Characters/ACT_Shadow_Raka.prefab");
            PrefabUtility.InstantiatePrefab(asset,wrapper);
            Node(actor,"Sorotan",0,1.60f,.18f).localRotation=Quaternion.Euler(7,0,0);
            return actor;
        }

        static void Crate(Transform t,string name,float x,float z,float w,float h,float d,Material mat)
            => Box(t,name,x,h*.5f,z,w,h,d,mat);

        static void Barrier(Transform t,float x,float z,float yaw,Material mat,Material band)
        {
            var root=Node(t,"Pembatas jalan",x,0,z,yaw);
            Box(root,"Blok beton",0,.47f,0,2.65f,.94f,.64f,mat);
            Box(root,"Pita polisi",0,.74f,-.326f,2.65f,.13f,.012f,band,false);
        }

        static Transform Node(Transform parent,string name,float x,float y,float z,float yaw=0)
        {
            var t=new GameObject(name).transform;t.SetParent(parent,false);
            t.localPosition=new Vector3(x,y,z);t.localRotation=Quaternion.Euler(0,yaw,0);
            return t;
        }
        static Transform Box(Transform parent,string name,float x,float y,float z,float w,float h,float d,Material mat,bool solid=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;
            var t=go.transform;t.SetParent(parent,false);t.localPosition=new Vector3(x,y,z);t.localScale=new Vector3(w,h,d);
            go.GetComponent<Renderer>().sharedMaterial=mat;
            if(!solid) Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            return t;
        }
        static void LightAt(Transform t,string name,float x,float y,float z,Color color,float intensity,float range)
        {
            var l=Node(t,name,x,y,z).gameObject.AddComponent<Light>();
            l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;
        }
        static void Sign(Transform t,string label,float x,float y,float z,float height,Color color,float yaw)
        {
            var text=Node(t,label,x,y,z,yaw).gameObject.AddComponent<TextMesh>();
            text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=96;
            text.characterSize=height*.12f;text.color=color;text.text=label;
        }
        static Material Material(string name,string hex,float smoothness,float emission=0)
        {
            string path=MatDir+"/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            ColorUtility.TryParseHtmlString("#"+hex,out Color color);
            mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",smoothness);
            if(emission>0){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*emission);}
            EditorUtility.SetDirty(mat);return mat;
        }
    }
}
