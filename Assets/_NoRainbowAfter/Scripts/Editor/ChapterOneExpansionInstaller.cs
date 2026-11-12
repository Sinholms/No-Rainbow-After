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
