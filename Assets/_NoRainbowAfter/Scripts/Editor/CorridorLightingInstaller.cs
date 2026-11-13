using System;
using System.Linq;
using NoRainbowAfter.AI;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace NoRainbowAfter.Editor
{
    public static class CorridorLightingInstaller
    {
        private const string Folder="Assets/_NoRainbowAfter/Art/Materials/HorrorCorridor3/";
        [MenuItem("No Rainbow After/Install Dim Faulty Corridor Lights")]
        public static void Install()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying || scene.path!="Assets/_NoRainbowAfter/Scenes/MasterGameplay.unity" || scene.isDirty)
                throw new InvalidOperationException("Open saved MasterGameplay first.");
            var root=UnityEngine.Object.FindAnyObjectByType<ChapterOneCorridorShift>().Corridor.transform;
            var previous=root.Find("C1_CeilingFixtures");
            if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
            // Gentle steady fill keeps the route readable between independent lamp blinks.
            foreach(var light in root.GetComponentsInChildren<Light>(true).Where(l=>l.name.StartsWith("HC3_Light_")))
            {light.intensity=.85f;light.range=5.5f;light.color=new Color(.65f,.72f,.68f);}
            var body=Material("MAT_C1_FixtureBody","Universal Render Pipeline/Lit",new Color(.16f,.18f,.17f));
            var tube=Material("MAT_C1_FixtureTube","Universal Render Pipeline/Lit",new Color(.84f,.81f,.67f));
            tube.EnableKeyword("_EMISSION");tube.SetColor("_EmissionColor",new Color(2.2f,2.05f,1.65f));EditorUtility.SetDirty(tube);
            var spark=Material("MAT_C1_ElectricSpark","Universal Render Pipeline/Particles/Unlit",new Color(2.4f,1.8f,.7f));
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_NoRainbowAfter/Audio/SFX/AUD_C1_ElectricTick.wav");
            if(clip==null)throw new InvalidOperationException("Electrical crackle clip is missing.");
            var holder=new GameObject("C1_CeilingFixtures");holder.transform.SetParent(root,false);
            Vector3[] positions={new Vector3(-1.85f,2.59f,7),new Vector3(-1.85f,2.59f,2),
                new Vector3(2,2.59f,-1.65f),new Vector3(7,2.59f,-1.65f),new Vector3(12,2.59f,-1.65f),new Vector3(17,2.59f,-1.65f),new Vector3(22,2.59f,-1.65f),new Vector3(27,2.59f,-1.65f),
                new Vector3(32.6f,2.59f,-1.2f),new Vector3(32.6f,2.59f,3),new Vector3(32.6f,2.59f,7.7f),new Vector3(32.6f,2.59f,12.2f),
                new Vector3(28,2.59f,12.35f),new Vector3(23.5f,2.59f,12.35f),new Vector3(18.6f,2.59f,12.1f),new Vector3(18.6f,2.59f,7.4f),new Vector3(18.6f,2.59f,2.5f)};
            for(int i=0;i<positions.Length;i++)
            {
                var go=new GameObject("CeilingFluorescent_"+i);go.transform.SetParent(holder.transform,false);go.transform.localPosition=positions[i];
                bool alongX=i>=2&&i<=7||i==12||i==13;
                go.transform.localRotation=Quaternion.Euler(0,alongX?90:0,0);
                Cube(go.transform,"Housing",Vector3.zero,new Vector3(.28f,.07f,1.3f),body);
                var tubes=new Renderer[2];
                for(int j=0;j<2;j++)tubes[j]=Cube(go.transform,"Tube_"+j,new Vector3(j==0?-.075f:.075f,-.05f,0),new Vector3(.045f,.035f,1.16f),tube);
                var lampGo=new GameObject("Dim downlight");lampGo.transform.SetParent(go.transform,false);lampGo.transform.localPosition=new Vector3(0,-.22f,0);
                var lamp=lampGo.AddComponent<Light>();lamp.type=LightType.Point;lamp.color=new Color(.86f,.88f,.72f);lamp.intensity=2.6f;lamp.range=5.5f;lamp.shadows=LightShadows.None;
                var arc=new GameObject("Electrical transition sparks");arc.transform.SetParent(go.transform,false);arc.transform.localPosition=new Vector3(.08f,-.09f,.48f);arc.transform.localRotation=Quaternion.Euler(90,0,0);
                var particles=arc.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=particles.main;main.playOnAwake=false;main.loop=false;main.duration=.25f;main.startLifetime=new ParticleSystem.MinMaxCurve(.06f,.18f);main.startSpeed=new ParticleSystem.MinMaxCurve(.6f,1.6f);main.startSize=new ParticleSystem.MinMaxCurve(.012f,.028f);main.startColor=new Color(1,.8f,.35f);main.gravityModifier=.8f;main.maxParticles=32;main.simulationSpace=ParticleSystemSimulationSpace.World;
                var emission=particles.emission;emission.enabled=false;
                var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=48;shape.radius=.025f;
                var pr=particles.GetComponent<ParticleSystemRenderer>();pr.sharedMaterial=spark;pr.renderMode=ParticleSystemRenderMode.Stretch;pr.lengthScale=2;pr.velocityScale=.08f;
                var flashGo=new GameObject("Discharge flash");flashGo.transform.SetParent(arc.transform,false);var flash=flashGo.AddComponent<Light>();flash.type=LightType.Point;flash.color=new Color(.65f,.8f,1);flash.intensity=1.8f;flash.range=1.1f;flash.shadows=LightShadows.None;flash.enabled=false;
                var audio=arc.AddComponent<AudioSource>();audio.clip=clip;audio.playOnAwake=false;audio.spatialBlend=1;audio.volume=.17f;audio.minDistance=.7f;audio.maxDistance=5;audio.rolloffMode=AudioRolloffMode.Linear;audio.dopplerLevel=0;
                var effect=go.AddComponent<CorridorFaultyLight>();effect.Seed=193+i*67;effect.Lamp=lamp;effect.Flash=flash;effect.Tubes=tubes;effect.Sparks=particles;effect.Crackle=audio;effect.RestIntensity=2.6f;
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        private static Material Material(string name,string shader,Color color)
        {
            var path=Folder+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){var s=Shader.Find(shader);if(s==null)throw new InvalidOperationException("Missing shader: "+shader);mat=new Material(s);AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color);EditorUtility.SetDirty(mat);return mat;
        }
        private static Renderer Cube(Transform parent,string name,Vector3 position,Vector3 scale,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=mat;return renderer;
        }
    }
}
