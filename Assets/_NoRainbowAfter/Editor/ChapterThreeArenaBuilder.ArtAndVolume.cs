using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NoRainbowAfter.Editor
{
    /// <summary>
    /// Konfigurasi material PBR basemen, pemetaan ulang model FBX, pembatas kolisi,
    /// serta volume Post-Processing URP pada ChapterThreeArenaBuilder.
    /// </summary>
    public static partial class ChapterThreeArenaBuilder
    {
        private static Dictionary<string, Material> PrepareMaterials()
        {
            var dict = new Dictionary<string, Material>();
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");

            dict["C3_Floor"] = CreateOrUpdateMat("C3_Floor", urpLit, new Color(0.24f, 0.27f, 0.30f), 0.72f, 0.05f,
                "metalfloor_diffuseOriginal.png", "metalfloor_normal.png", 0.45f, 10f);
            dict["C3_Ceiling"] = CreateOrUpdateMat("C3_Ceiling", urpLit, new Color(0.18f, 0.21f, 0.24f), 0.22f, 0.0f,
                "imagetextures.png", "Nornalmap.png", 0.35f, 8f);
            dict["C3_Wall"] = CreateOrUpdateMat("C3_Wall", urpLit, new Color(0.32f, 0.36f, 0.39f), 0.28f, 0.0f,
                "wall02_diffuse.jpeg", "wall02_normal.jpeg", 0.50f, 4f);
            dict["C3_ConcreteTrim"] = CreateOrUpdateMat("C3_ConcreteTrim", urpLit, new Color(0.22f, 0.24f, 0.26f), 0.25f, 0.0f,
                "imagetextures.png", "Nornalmap.png", 0.30f, 4f);
            dict["C3_PipeRed"] = CreateOrUpdateMat("C3_PipeRed", urpLit, new Color(0.34f, 0.11f, 0.11f), 0.55f, 0.35f,
                null, null, 0f, 1f);
            dict["C3_LanePaint"] = CreateOrUpdateMat("C3_LanePaint", urpLit, new Color(0.42f, 0.40f, 0.32f), 0.48f, 0.0f,
                null, null, 0f, 1f);

            var emissive = CreateOrUpdateMat("C3_LightEmissive", urpLit, new Color(0.62f, 0.74f, 0.76f), 0.35f, 0.0f,
                null, null, 0f, 1f);
            emissive.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            emissive.EnableKeyword("_EMISSION");
            emissive.SetColor("_EmissionColor", new Color(0.45f, 0.54f, 0.56f));
            EditorUtility.SetDirty(emissive);
            dict["C3_LightEmissive"] = emissive;

            return dict;
        }

        private static Material CreateOrUpdateMat(string name, Shader shader, Color tint, float smoothness, float metallic,
            string albedoFile, string normalFile, float bumpScale, float tiling)
        {
            string path = MatDir + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetColor("_BaseColor", tint);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);

            if (!string.IsNullOrEmpty(albedoFile))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + albedoFile);
                if (tex != null)
                {
                    mat.SetTexture("_BaseMap", tex);
                    mat.SetTextureScale("_BaseMap", Vector2.one * tiling);
                }
            }
            if (!string.IsNullOrEmpty(normalFile))
            {
                string normPath = TexDir + normalFile;
                var importer = AssetImporter.GetAtPath(normPath) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.SaveAndReimport();
                }
                var normTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normPath);
                if (normTex != null)
                {
                    mat.SetTexture("_BumpMap", normTex);
                    mat.SetTextureScale("_BumpMap", Vector2.one * tiling);
                    mat.SetFloat("_BumpScale", bumpScale);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }

            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void ConfigureModelImporter(Dictionary<string, Material> mats)
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
            {
                return;
            }
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.bakeAxisConversion = true;
            foreach (var id in importer.GetExternalObjectMap().Keys.ToArray())
            {
                importer.RemoveRemap(id);
            }
            foreach (var kv in mats)
            {
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
            }
            importer.SaveAndReimport();
        }

        private static void AddInvisibleBarrier(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
        }

        private static Light CreatePointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        private static void AddBasementVolume(Transform parent)
        {
            var volGo = new GameObject("Suasana basement");
            volGo.transform.SetParent(parent, false);
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = false;
            vol.priority = 6f;
            vol.weight = 1f;

            string profilePath = MatDir + "BasementArenaProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            var tone = GetOrAdd<Tonemapping>(profile);
            tone.mode.Override(TonemappingMode.ACES);

            var color = GetOrAdd<ColorAdjustments>(profile);
            color.postExposure.Override(-0.18f);
            color.contrast.Override(16f);
            color.saturation.Override(-24f);
            color.colorFilter.Override(new Color(0.88f, 0.94f, 1.0f));

            var vignette = GetOrAdd<Vignette>(profile);
            vignette.color.Override(new Color(0.01f, 0.015f, 0.025f));
            vignette.intensity.Override(0.34f);
            vignette.smoothness.Override(0.48f);

            var grain = GetOrAdd<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Medium1);
            grain.intensity.Override(0.22f);

            EditorUtility.SetDirty(profile);
            vol.sharedProfile = profile;

            var bounds = volGo.AddComponent<BoxCollider>();
            bounds.isTrigger = true;
            bounds.size = new Vector3(60f, 10f, 60f);
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T comp))
            {
                comp = profile.Add<T>();
                AssetDatabase.AddObjectToAsset(comp, profile);
            }
            return comp;
        }
    }
}
