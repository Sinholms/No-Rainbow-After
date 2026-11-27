using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NoRainbowAfter.AI;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace NoRainbowAfter.Editor
{
    public static class ChapterThreeArenaBuilder
    {
        private const string ModelPath = "Assets/_NoRainbowAfter/Art/Models/Environment/ChapterThree/ENV_C3_BasementParking.fbx";
        private const string TexDir = "Assets/_NoRainbowAfter/Art/Textures/ChapterThree/";
        private const string MatDir = "Assets/_NoRainbowAfter/Art/Materials/ChapterThree/";
        private const string NavPath = "Assets/_NoRainbowAfter/Art/Models/Environment/C3_ArenaNavigation.asset";
        private const string PrefabPath = "Assets/_NoRainbowAfter/Resources/ChapterThreeArena.prefab";
        private const float BarrierHalfExtent = 23.5f;

        [MenuItem("No Rainbow After/Chapter 3/Rebuild Arena")]
        public static void Build()
        {
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory("Assets/_NoRainbowAfter/Resources");
            AssetDatabase.Refresh();

            var mats = PrepareMaterials();
            ConfigureModelImporter(mats);

            var root = new GameObject("ChapterThreeArena");
            try
            {
                var arena = root.AddComponent<ChapterThreeArena>();
                arena.ArenaHalfExtent = BarrierHalfExtent;

                var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                if (modelAsset == null)
                    throw new FileNotFoundException("Missing expanded basement model at " + ModelPath);

                var envInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, root.transform);
                envInstance.name = "ENV_C3_BasementParking";

                foreach (var renderer in envInstance.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var remapped = renderer.sharedMaterials
                        .Select(m => m != null && mats.TryGetValue(m.name, out var replacement) ? replacement : m)
                        .ToArray();
                    renderer.sharedMaterials = remapped;
                }

                // Floor collider covering the full open arena
                var floorColliderGo = new GameObject("C3_FloorCollider");
                floorColliderGo.transform.SetParent(root.transform, false);
                floorColliderGo.transform.localPosition = new Vector3(0f, -0.25f, 0f);
                var floorBox = floorColliderGo.AddComponent<BoxCollider>();
                floorBox.size = new Vector3(56f, 0.5f, 56f);

                // Invisible perimeter barriers so the player cannot exceed the map boundaries
                var barrierRoot = new GameObject("C3_InvisibleBarriers");
                barrierRoot.transform.SetParent(root.transform, false);
                AddInvisibleBarrier(barrierRoot.transform, "C3_Barrier_North", new Vector3(0f, 2.25f, BarrierHalfExtent + 0.5f), new Vector3(50f, 4.5f, 1f));
                AddInvisibleBarrier(barrierRoot.transform, "C3_Barrier_South", new Vector3(0f, 2.25f, -(BarrierHalfExtent + 0.5f)), new Vector3(50f, 4.5f, 1f));
                AddInvisibleBarrier(barrierRoot.transform, "C3_Barrier_East", new Vector3(BarrierHalfExtent + 0.5f, 2.25f, 0f), new Vector3(1f, 4.5f, 50f));
                AddInvisibleBarrier(barrierRoot.transform, "C3_Barrier_West", new Vector3(-(BarrierHalfExtent + 0.5f), 2.25f, 0f), new Vector3(1f, 4.5f, 50f));

                // Standalone hiding wardrobes in the wide open basement (no corridors or obstacles in between)
                var wardrobesRoot = new GameObject("C3_HidingWardrobes");
                wardrobesRoot.transform.SetParent(root.transform, false);
                var woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_NoRainbowAfter/Art/Materials/KosAtmosphere/MAT_H_Wood.mat");
                var wardrobePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_NoRainbowAfter/Art/Models/Environment/PROP_C1_HidingWardrobe.fbx");

                var spots = new (Vector3 pos, float yaw)[]
                {
                    (new Vector3(-14.5f, 0f, -14.5f), 45f),
                    (new Vector3(14.5f, 0f, -14.5f), -45f),
                    (new Vector3(15.5f, 0f, 14.5f), -135f),
                    (new Vector3(-15.5f, 0f, 14.5f), 135f),
                    (new Vector3(0f, 0f, 17.5f), 180f),
                    (new Vector3(0f, 0f, -17.5f), 0f),
                };

                var flickerLights = new List<Light>();
                var flickerEmissives = new List<Renderer>();
                var patrolPoints = new List<Transform>();

                for (int i = 0; i < spots.Length; i++)
                {
                    var spot = spots[i];
                    var holder = new GameObject($"C3_Wardrobe_{i + 1}");
                    holder.transform.SetParent(wardrobesRoot.transform, false);
                    holder.transform.localPosition = spot.pos;
                    holder.transform.localRotation = Quaternion.Euler(0f, spot.yaw, 0f);

                    if (wardrobePrefab != null)
                    {
                        var visual = (GameObject)PrefabUtility.InstantiatePrefab(wardrobePrefab, holder.transform);
                        visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * visual.transform.localRotation;
                        foreach (var r in visual.GetComponentsInChildren<MeshRenderer>(true))
                        {
                            if (woodMat != null) r.sharedMaterial = woodMat;
                            var mf = r.GetComponent<MeshFilter>();
                            if (mf != null && mf.sharedMesh != null)
                                r.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                        }
                    }

                    holder.AddComponent<HidingWardrobe>();
                    var door = holder.AddComponent<BoxCollider>();
                    door.center = new Vector3(0f, 1.15f, -0.61f);
                    door.size = new Vector3(1.42f, 2.3f, 0.065f);

                    // Dim beacon above each wardrobe so player can read silhouettes through fog
                    var wardLight = CreatePointLight(holder.transform, "WardrobeGlow", new Vector3(0f, 2.95f, -0.8f),
                        new Color(0.56f, 0.72f, 0.70f), 1.45f, 6.5f);
                    if (i % 2 == 1)
                    {
                        flickerLights.Add(wardLight);
                        flickerEmissives.Add(null);
                    }

                    // Patrol point in open space near the wardrobe
                    var wp = new GameObject($"C3_PatrolPoint_{i + 1}").transform;
                    wp.SetParent(root.transform, false);
                    wp.localPosition = spot.pos * 0.72f;
                    patrolPoints.Add(wp);
                }

                // Dim overhead basement lights across the ceiling grid
                var lightBoxes = envInstance.GetComponentsInChildren<MeshRenderer>(true)
                    .Where(r => r.name.StartsWith("C3_CeilingLightBox", StringComparison.Ordinal))
                    .ToArray();
                for (int i = 0; i < lightBoxes.Length; i++)
                {
                    Vector3 p = root.transform.InverseTransformPoint(lightBoxes[i].bounds.center);
                    bool centerFixture = Mathf.Abs(p.x) < 1f && Mathf.Abs(p.z) < 1f;
                    var ceilingLight = CreatePointLight(
                        root.transform,
                        $"C3_CeilingLight_{i}",
                        new Vector3(p.x, 4.05f, p.z),
                        centerFixture ? new Color(0.68f, 0.76f, 0.78f) : new Color(0.52f, 0.62f, 0.68f),
                        centerFixture ? 2.4f : 1.55f,
                        12.5f);

                    if (i % 3 == 0)
                    {
                        flickerLights.Add(ceilingLight);
                        flickerEmissives.Add(lightBoxes[i]);
                    }
                }

                // Atmospheric URP post-processing volume conditioned like Chapter 1 & Chapter 2
                AddBasementVolume(root.transform);

                // Spawn anchors: Raka starts near center-south; Monster spawns far north
                var playerSpawn = new GameObject("C3_PlayerSpawn").transform;
                playerSpawn.SetParent(root.transform, false);
                playerSpawn.localPosition = new Vector3(0f, 0.08f, -4.5f);
                playerSpawn.localRotation = Quaternion.identity;

                var monsterSpawn = new GameObject("C3_MonsterSpawn").transform;
                monsterSpawn.SetParent(root.transform, false);
                monsterSpawn.localPosition = new Vector3(0f, 0.12f, 19.5f);
                monsterSpawn.localRotation = Quaternion.Euler(0f, 180f, 0f);

                arena.PlayerSpawn = playerSpawn;
                arena.MonsterSpawn = monsterSpawn;
                arena.PatrolWaypoints = patrolPoints.ToArray();
                arena.FlickerLights = flickerLights.ToArray();
                arena.FlickerEmissives = flickerEmissives.ToArray();

                // Bake open-arena NavMeshData cleanly around the wardrobe colliders
                var sources = new List<NavMeshBuildSource>();
                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    size = floorBox.size,
                    transform = floorColliderGo.transform.localToWorldMatrix,
                    area = 0
                });
                foreach (var box in wardrobesRoot.GetComponentsInChildren<BoxCollider>(true))
                {
                    sources.Add(new NavMeshBuildSource
                    {
                        shape = NavMeshBuildSourceShape.Box,
                        size = box.size,
                        transform = Matrix4x4.TRS(box.transform.TransformPoint(box.center), box.transform.rotation, box.transform.lossyScale),
                        area = 1
                    });
                }

                var settings = NavMesh.GetSettingsByIndex(0);
                settings.agentRadius = 0.25f;
                settings.agentHeight = 1.8f;
                settings.agentClimb = 0.25f;
                settings.overrideVoxelSize = true;
                settings.voxelSize = 0.12f;
                var nav = NavMeshBuilder.BuildNavMeshData(settings, sources,
                    new Bounds(new Vector3(0f, 1.5f, 0f), new Vector3(54f, 8f, 54f)),
                    Vector3.zero, Quaternion.identity);
                if (nav == null) throw new InvalidOperationException("Chapter 3 NavMesh bake failed.");

                var existingNav = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavPath);
                if (existingNav != null)
                {
                    EditorUtility.CopySerialized(nav, existingNav);
                    Object.DestroyImmediate(nav);
                    EditorUtility.SetDirty(existingNav);
                    arena.Navigation = existingNav;
                }
                else
                {
                    AssetDatabase.CreateAsset(nav, NavPath);
                    arena.Navigation = nav;
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[NRA] Built Chapter 3 basement parking arena prefab & NavMesh.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Dictionary<string, Material> PrepareMaterials()
        {
            var dict = new Dictionary<string, Material>();
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");

            // Condition basement textures to match the dark, damp, desaturated palette of Chapters 1 & 2
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
            if (importer == null) return;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.bakeAxisConversion = true;
            foreach (var id in importer.GetExternalObjectMap().Keys.ToArray())
                importer.RemoveRemap(id);
            foreach (var kv in mats)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
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
