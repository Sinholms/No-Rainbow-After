using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NoRainbowAfter.AI;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace NoRainbowAfter.Editor
{
    /// <summary>
    /// Builder editor pembangun prefab arena parkir basemen Chapter 3 serta NavMesh-nya.
    ///
    /// [TEKNIS KODING]
    /// Dipecah menjadi 2 file partial class:
    /// 1. ChapterThreeArenaBuilder.cs (orkestrasi prefab, lemari sembunyi, lampu plafon, baking NavMeshData)
    /// 2. ChapterThreeArenaBuilder.ArtAndVolume.cs (material URP/Lit basemen, remap FBX, volume Post-Processing)
    /// </summary>
    public static partial class ChapterThreeArenaBuilder
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
                {
                    throw new FileNotFoundException("Missing expanded basement model at " + ModelPath);
                }

                var envInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, root.transform);
                envInstance.name = "ENV_C3_BasementParking";

                foreach (var renderer in envInstance.GetComponentsInChildren<MeshRenderer>(true))
                {
                    renderer.sharedMaterials = renderer.sharedMaterials
                        .Select(m => m != null && mats.TryGetValue(m.name, out var replacement) ? replacement : m)
                        .ToArray();
                }

                var floorBox = BuildFloorAndBarriers(root.transform);
                var flickerLights = new List<Light>();
                var flickerEmissives = new List<Renderer>();
                var patrolPoints = BuildWardrobes(root.transform, flickerLights, flickerEmissives, out var wardrobesRoot);
                BuildCeilingLights(root.transform, envInstance, flickerLights, flickerEmissives);
                AddBasementVolume(root.transform);

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
                arena.Navigation = BakeArenaNavMesh(floorBox, wardrobesRoot);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[NRA] Built Chapter 3 basement parking arena prefab & NavMesh.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static BoxCollider BuildFloorAndBarriers(Transform root)
        {
            var floorColliderGo = new GameObject("C3_FloorCollider");
            floorColliderGo.transform.SetParent(root, false);
            floorColliderGo.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            var floorBox = floorColliderGo.AddComponent<BoxCollider>();
            floorBox.size = new Vector3(56f, 0.5f, 56f);

            var barrierRoot = new GameObject("C3_InvisibleBarriers");
            barrierRoot.transform.SetParent(root, false);
            AddInvisibleBarrier(barrierRoot.transform, "C3_Barrier_North", new Vector3(0f, 2.25f, BarrierHalfExtent + 0.5f), new Vector3(50f, 4.5f, 1f));
            AddInvisibleBarrier(barrierRoot.transform, "C3_Barrier_South", new Vector3(0f, 2.25f, -(BarrierHalfExtent + 0.5f)), new Vector3(50f, 4.5f, 1f));
            AddInvisibleBarrier(barrierRoot.transform, "C3_Barrier_East", new Vector3(BarrierHalfExtent + 0.5f, 2.25f, 0f), new Vector3(1f, 4.5f, 50f));
            AddInvisibleBarrier(barrierRoot.transform, "C3_Barrier_West", new Vector3(-(BarrierHalfExtent + 0.5f), 2.25f, 0f), new Vector3(1f, 4.5f, 50f));
            return floorBox;
        }

        private static List<Transform> BuildWardrobes(
            Transform root,
            List<Light> flickerLights,
            List<Renderer> flickerEmissives,
            out GameObject wardrobesRoot)
        {
            wardrobesRoot = new GameObject("C3_HidingWardrobes");
            wardrobesRoot.transform.SetParent(root, false);

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
                        {
                            r.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                        }
                    }
                }

                holder.AddComponent<HidingWardrobe>();
                var door = holder.AddComponent<BoxCollider>();
                door.center = new Vector3(0f, 1.15f, -0.61f);
                door.size = new Vector3(1.42f, 2.3f, 0.065f);

                var wardLight = CreatePointLight(holder.transform, "WardrobeGlow", new Vector3(0f, 2.95f, -0.8f),
                    new Color(0.56f, 0.72f, 0.70f), 1.45f, 6.5f);
                if (i % 2 == 1)
                {
                    flickerLights.Add(wardLight);
                    flickerEmissives.Add(null);
                }

                var wp = new GameObject($"C3_PatrolPoint_{i + 1}").transform;
                wp.SetParent(root, false);
                wp.localPosition = spot.pos * 0.72f;
                patrolPoints.Add(wp);
            }
            return patrolPoints;
        }

        private static void BuildCeilingLights(Transform root, GameObject envInstance,
            List<Light> flickerLights, List<Renderer> flickerEmissives)
        {
            var lightBoxes = envInstance.GetComponentsInChildren<MeshRenderer>(true)
                .Where(r => r.name.StartsWith("C3_CeilingLightBox", StringComparison.Ordinal))
                .ToArray();

            for (int i = 0; i < lightBoxes.Length; i++)
            {
                Vector3 p = root.InverseTransformPoint(lightBoxes[i].bounds.center);
                bool centerFixture = Mathf.Abs(p.x) < 1f && Mathf.Abs(p.z) < 1f;
                var ceilingLight = CreatePointLight(
                    root,
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
        }

        private static NavMeshData BakeArenaNavMesh(BoxCollider floorBox, GameObject wardrobesRoot)
        {
            var sources = new List<NavMeshBuildSource>
            {
                new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    size = floorBox.size,
                    transform = floorBox.transform.localToWorldMatrix,
                    area = 0
                }
            };

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

            var nav = NavMeshBuilder.BuildNavMeshData(
                settings,
                sources,
                new Bounds(new Vector3(0f, 1.5f, 0f), new Vector3(54f, 8f, 54f)),
                Vector3.zero,
                Quaternion.identity);
            if (nav == null)
            {
                throw new InvalidOperationException("Chapter 3 NavMesh bake failed.");
            }

            var existingNav = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavPath);
            if (existingNav != null)
            {
                EditorUtility.CopySerialized(nav, existingNav);
                Object.DestroyImmediate(nav);
                EditorUtility.SetDirty(existingNav);
                return existingNav;
            }

            AssetDatabase.CreateAsset(nav, NavPath);
            return nav;
        }
    }
}
