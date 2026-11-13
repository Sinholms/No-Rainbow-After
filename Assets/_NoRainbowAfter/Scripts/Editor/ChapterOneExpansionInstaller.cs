using System;
using System.IO;
using System.Linq;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.AI;
using NoRainbowAfter.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using System.Collections.Generic;

namespace NoRainbowAfter.Editor
{
    public static class ChapterOneExpansionInstaller
    {
        [Serializable] private class MaterialEntry { public string name, texture; }
        [Serializable] private class MaterialList { public MaterialEntry[] materials; }
        [Serializable] private class BoundaryEdge { public float[] a,b,normal; }
        [Serializable] private class WardrobeSpot { public string id;public float[] position;public float yaw; }
        [Serializable] private class RepairManifest { public BoundaryEdge[] boundaryEdges;public WardrobeSpot[] wardrobes; }
        [MenuItem("No Rainbow After/Repair Corridor Bounds And Add Wardrobes")]
        public static void RepairCorridor()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying || scene.path!="Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity" || scene.isDirty) throw new InvalidOperationException("Open saved MasterGameplay first.");
            var shift=UnityEngine.Object.FindAnyObjectByType<ChapterOneCorridorShift>();
            if(shift == null || shift.Corridor == null)throw new InvalidOperationException("Install the extended corridor first.");
            var root=shift.Corridor.transform;
            var manifest=JsonUtility.FromJson<RepairManifest>(File.ReadAllText("BlenderSources/Environment/C1_CorridorRepair_manifest.json"));
            const string modelPath="Assets/_NoRainbowAfter/Art/Models/Environment/ENV_HC3_NoStorage.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);importer.isReadable=true;
            var imported=new SerializedObject(importer);imported.FindProperty("m_PreBakeTriangleCollisionMesh").boolValue=true;imported.ApplyModifiedPropertiesWithoutUndo();importer.SaveAndReimport();
            foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                r.sharedMaterials=r.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_NoRainbowAfter/Art/Materials/HorrorCorridor3/"+m.name+".mat") ?? m).ToArray();
                var collider=r.GetComponent<MeshCollider>();if(collider==null)collider=r.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;
            }
            var old=root.Find("C1_PerimeterGuards");if(old != null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var guards=new GameObject("C1_PerimeterGuards");guards.transform.SetParent(root,false);
            int index=0;
            foreach(var edge in manifest.boundaryEdges)
            {
                Vector3 a=new Vector3(edge.a[0],0,edge.a[1]),b=new Vector3(edge.b[0],0,edge.b[1]);
                var go=new GameObject("Boundary_"+(index++));go.transform.SetParent(guards.transform,false);
                go.transform.localPosition=(a+b)*.5f+new Vector3(edge.normal[0]*.06f,1.55f,edge.normal[1]*.06f);
                var box=go.AddComponent<BoxCollider>();box.size=Mathf.Abs(a.x-b.x)>.001f ? new Vector3(Mathf.Abs(a.x-b.x)+.02f,3.2f,.12f) : new Vector3(.12f,3.2f,Mathf.Abs(a.z-b.z)+.02f);
            }
            var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/_NoRainbowAfter/Art/Materials/KosAtmosphere/MAT_H_Wood.mat");
            foreach(var spot in manifest.wardrobes)
            {
                if(root.Find("Wardrobe_"+spot.id)==null)InstallWardrobe(root,new Vector3(spot.position[0],spot.position[1],spot.position[2]),spot.yaw,wood,spot.id);
                var wardrobe=root.Find("Wardrobe_"+spot.id);
                wardrobe.localPosition=new Vector3(spot.position[0],spot.position[1],spot.position[2]);
                wardrobe.localRotation=Quaternion.Euler(0,spot.yaw,0);
                var lamp=root.Find("WardrobeLight_"+spot.id);
                if(lamp==null){lamp=new GameObject("WardrobeLight_"+spot.id).transform;lamp.SetParent(root,false);lamp.gameObject.AddComponent<Light>();}
                lamp.localPosition=new Vector3(spot.position[0],2.2f,spot.position[2]);
                var light=lamp.GetComponent<Light>();light.type=LightType.Point;light.color=new Color(.95f,.7f,.4f);light.intensity=.7f;light.range=3.5f;
            }
            foreach(var wardrobe in root.GetComponentsInChildren<HidingWardrobe>(true))
            {
                var door=wardrobe.GetComponent<BoxCollider>();if(door==null)door=wardrobe.gameObject.AddComponent<BoxCollider>();
                door.center=new Vector3(0,1.15f,-.61f);door.size=new Vector3(1.42f,2.3f,.065f);
            }
            var sources=new List<NavMeshBuildSource>();
            foreach(var mesh in root.GetComponentsInChildren<MeshFilter>(true))sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Mesh,sourceObject=mesh.sharedMesh,transform=mesh.transform.localToWorldMatrix});
            foreach(var box in guards.GetComponentsInChildren<BoxCollider>())sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,size=box.size,transform=box.transform.localToWorldMatrix});
            var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=.23f;settings.agentHeight=1.8f;settings.agentClimb=.25f;settings.overrideVoxelSize=true;settings.voxelSize=.08f;
            var nav=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(new Vector3(76,1,4),new Vector3(47,12,34)),Vector3.zero,Quaternion.identity);
            if(nav==null)throw new InvalidOperationException("Navigation bake failed.");
            EditorUtility.CopySerialized(nav,shift.Navigation);UnityEngine.Object.DestroyImmediate(nav);EditorUtility.SetDirty(shift.Navigation);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("[NRA] Repaired corridor floor/perimeter and installed three corner wardrobes.");
        }
        [MenuItem("No Rainbow After/Install Extended Horror Corridor")]
        public static void InstallCorridor()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying || scene.path != "Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity" || scene.isDirty)
                throw new InvalidOperationException("Open saved MasterGameplay first.");
            if(GameObject.Find("C1_HorrorCorridor") != null) throw new InvalidOperationException("Already installed.");
            const string modelPath="Assets/_NoRainbowAfter/Art/Models/Environment/ENV_HC3_NoStorage.fbx";
            const string matDir="Assets/_NoRainbowAfter/Art/Materials/HorrorCorridor3";
            Directory.CreateDirectory(matDir);AssetDatabase.Refresh();
            var existing=AssetDatabase.LoadAssetAtPath<Material>("Assets/_NoRainbowAfter/Art/Materials/KosAtmosphere/MAT_H_Wood.mat");
            var materials=new Dictionary<string,Material>();
            foreach(var item in JsonUtility.FromJson<MaterialList>(File.ReadAllText("BlenderSources/Environment/HC3_manifest.json")).materials)
            {
                string path=matDir+"/"+item.name+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(mat == null) { mat=new Material(existing.shader);AssetDatabase.CreateAsset(mat,path); }
                mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",.16f);mat.SetFloat("_Cull",0f);
                if(!string.IsNullOrEmpty(item.texture)) mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(item.texture));
                materials[item.name]=mat;EditorUtility.SetDirty(mat);
            }
            var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.importLights=false;importer.importCameras=false;importer.importAnimation=false;importer.isReadable=true;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
            var container=new GameObject("C1_HorrorCorridor");container.transform.position=new Vector3(60,0,0);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
            model.transform.SetParent(container.transform,false);model.transform.localRotation=Quaternion.Euler(0,180,0)*model.transform.localRotation;
            foreach(var r in model.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterials=r.sharedMaterials.Select(m=>materials.ContainsKey(m.name)?materials[m.name]:existing).ToArray();
                r.gameObject.AddComponent<MeshCollider>().sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;
            }
            var actor=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayer>();
            var shift=actor.gameObject.AddComponent<ChapterOneCorridorShift>();shift.Corridor=container;
            shift.Entry=new Vector3(58.15f,.12f,8.1f);shift.MonsterSpawn=new Vector3(90f,.12f,-1.65f);
            // Give each stretch a distinct practical light color, with bounded realtime shadow cost.
            Vector3[] lamps={new Vector3(-1.85f,2.4f,7),new Vector3(-1.85f,2.4f,2),new Vector3(3,2.4f,-1.8f),new Vector3(9,2.4f,-1.8f),new Vector3(16,2.4f,-1.8f),new Vector3(24,2.4f,-1.8f),new Vector3(32.6f,2.4f,1),new Vector3(32.6f,2.4f,8),new Vector3(27,2.4f,12.35f),new Vector3(21,2.4f,12.35f),new Vector3(18.6f,2.4f,5)};
            for(int i=0;i<lamps.Length;i++)
            {
                var go=new GameObject("HC3_Light_"+i);go.transform.SetParent(container.transform,false);go.transform.localPosition=lamps[i];
                var light=go.AddComponent<Light>();light.type=LightType.Point;light.range=5;light.intensity=1.8f;
                light.color=i<3?new Color(.55f,.74f,.68f):i<6?new Color(1,.55f,.28f):new Color(.4f,.6f,.85f);
            }
            InstallWardrobe(container.transform,new Vector3(-1.85f,.0f,10.45f),0,existing,"Entry");
            InstallWardrobe(container.transform,new Vector3(25.6f,.0f,.12f),0,existing,"East");
            var sources=new List<NavMeshBuildSource>();
            foreach(var filter in container.GetComponentsInChildren<MeshFilter>())
                sources.Add(new NavMeshBuildSource { shape=NavMeshBuildSourceShape.Mesh,sourceObject=filter.sharedMesh,transform=filter.transform.localToWorldMatrix,area=0 });
            var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=.23f;settings.agentHeight=1.8f;settings.agentClimb=.25f;
            settings.overrideVoxelSize=true;settings.voxelSize=.08f;
            var nav=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(new Vector3(76,1,4),new Vector3(47,12,34)),Vector3.zero,Quaternion.identity);
            if(nav == null) throw new InvalidOperationException("Navigation baking failed.");
            const string navPath="Assets/_NoRainbowAfter/Art/Models/Environment/C1_ExtendedNavigation.asset";
            var oldNav=AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
            if(oldNav != null) { EditorUtility.CopySerialized(nav,oldNav);UnityEngine.Object.DestroyImmediate(nav);nav=oldNav;EditorUtility.SetDirty(nav); }
            else AssetDatabase.CreateAsset(nav,navPath);
            shift.Navigation=nav;
            EditorUtility.SetDirty(shift);
            var encounter=UnityEngine.Object.FindFirstObjectByType<ShadowEncounterController>();
            var ep=new SerializedObject(encounter);ep.FindProperty("allowFirstEscape").boolValue=true;ep.ApplyModifiedPropertiesWithoutUndo();
            container.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("[NRA] Extended corridor and wardrobes installed.");
        }
        private static void InstallWardrobe(Transform parent,Vector3 position,float yaw,Material wood,string id)
        {
            var holder=new GameObject("Wardrobe_"+id);holder.transform.SetParent(parent,false);holder.transform.localPosition=position;holder.transform.localRotation=Quaternion.Euler(0,yaw,0);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_NoRainbowAfter/Art/Models/Environment/PROP_C1_HidingWardrobe.fbx"));
            model.transform.SetParent(holder.transform,false);model.transform.localRotation=Quaternion.Euler(0,180,0)*model.transform.localRotation;
            foreach(var r in model.GetComponentsInChildren<MeshRenderer>()) { r.sharedMaterial=wood;r.gameObject.AddComponent<MeshCollider>().sharedMesh=r.GetComponent<MeshFilter>().sharedMesh; }
            holder.AddComponent<HidingWardrobe>();
            var door=holder.AddComponent<BoxCollider>();door.center=new Vector3(0,1.15f,-.61f);door.size=new Vector3(1.42f,2.3f,.065f);
        }
        [MenuItem("No Rainbow After/Install Chapter One Expansion")]
        public static void Apply()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying || scene.path != "Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity" || scene.isDirty)
                throw new InvalidOperationException("Open saved MasterGameplay and stop Play Mode first.");
            const string modelPath="Assets/_NoRainbowAfter/Art/Models/Environment/ENV_C1_RoomDisarray.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.importLights=false; importer.importCameras=false; importer.importAnimation=false;
            foreach(var id in importer.GetExternalObjectMap().Keys.ToArray()) importer.RemoveRemap(id);
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            foreach(var material in AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>())
            {
                var target=AssetDatabase.LoadAssetAtPath<Material>("Assets/_NoRainbowAfter/Art/Materials/KosAtmosphere/"+material.name+".mat");
                if(target != null) importer.AddRemap(new AssetImporter.SourceAssetIdentifier(material),target);
            }
            importer.SaveAndReimport();
            var all=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
            if(all.Any(x=>x.name=="ENV_C1_RoomDisarray")) throw new InvalidOperationException("Expansion already installed.");
            Directory.CreateDirectory("Temp/C1ExpansionBaseline");
            File.Copy(scene.path,"Temp/C1ExpansionBaseline/MasterGameplay.before-expansion.unity",true);
            var root=all.First(x=>x.name=="Chapter1Root");
            var clutter=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath),scene);
            clutter.name="ENV_C1_RoomDisarray"; clutter.transform.SetParent(root,false); clutter.transform.rotation=Quaternion.Euler(0,180,0);
            foreach(var r in clutter.GetComponentsInChildren<Renderer>()) r.gameObject.isStatic=false;
            foreach(var mesh in clutter.GetComponentsInChildren<MeshFilter>())
                if(mesh.name.Contains("OverturnedChair") || mesh.name.Contains("DumpedBag"))
                    mesh.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;
            clutter.SetActive(false);
            var actor=all.Select(x=>x.GetComponent<FirstPersonPlayer>()).First(x=>x != null);
            var shift=actor.gameObject.AddComponent<ChapterOneRoomShift>();
            shift.Configure(actor.GetComponent<ChapterDirector>(),actor,clutter,
                all.Where(x=>x.name.StartsWith("SM_PROP_") && x.name.Contains("Chair")).Select(x=>x.gameObject).ToArray(),
                all.Where(x=>x.name=="Room Fluorescent").Select(x=>x.GetComponent<Light>()).FirstOrDefault(),
                all.Where(x=>x.name=="Desk Lamp").Select(x=>x.GetComponent<Light>()).FirstOrDefault());
            EditorUtility.SetDirty(shift);
            var encounter=all.Select(x=>x.GetComponent<ShadowEncounterController>()).FirstOrDefault(x=>x != null);
            if(encounter != null) { encounter.EnableFirstEscape(); EditorUtility.SetDirty(encounter); }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("[NRA] Chapter 1 room disarray installed (105 papers, 16 books, overturned chair and dumped bag).");
        }
    }
}
