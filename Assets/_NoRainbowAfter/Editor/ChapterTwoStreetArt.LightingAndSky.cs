using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NoRainbowAfter.Editor
{
    /// <summary>
    /// Konfigurasi pencahayaan bulan, Reflection Probe aspal basah, URP Post-Processing Volume,
    /// kabel udara melengkung (LineRenderer), dan kubah langit mendung Chapter 2.
    ///
    /// [GRAFIKA KOMPUTER — CHAPTER 2]
    /// 1. Directional Moonlight & Soft Shadows: Cahaya bulan dingin (biru pucat) membentuk siluet
    ///    ruko, peti, dan polisi.
    /// 2. Box-Projected Realtime ReflectionProbe: Memproyeksikan pantulan lingkungan malam ke aspal basah.
    /// 3. URP Post-Processing Stack:
    ///    - Tonemapping ACES (kompresi rentang dinamis filmis HDR -> LDR).
    ///    - ColorAdjustments (kurangi saturasi, naikkan kontras, filter biru dingin).
    ///    - Bloom (halo cahaya pada lentera, kaca bus, & rotator polisi).
    ///    - Vignette, FilmGrain, ChromaticAberration, & LiftGammaGain untuk nuansa horor malam.
    /// 4. Geometri Prosedural: LineRenderer (kabel listrik melengkung fungsi Sinus) + Sphere Langit.
    /// </summary>
    internal static partial class ChapterTwoStreetArt
    {
        private static void AddStreetLight(Transform root)
        {
            // Cahaya bulan dingin tengah malam: cukup kuat membentuk siluet, tetap cukup gelap untuk ketegangan.
            var go = new GameObject("Cahaya langit setelah hujan");
            go.transform.SetParent(root, false);
            go.transform.localRotation = Quaternion.Euler(49, -34, 0);
            var moon = go.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(.44f, .58f, .76f);
            moon.intensity = .20f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = .86f;

            // Reflection Probe lokal dengan Box Projection untuk pantulan aspal basah jalan pasar.
            var probe = new GameObject("Pantulan jalan").AddComponent<ReflectionProbe>();
            probe.transform.SetParent(root, false);
            probe.transform.localPosition = new Vector3(0, 2.1f, 18.8f);
            probe.size = new Vector3(17, 13, 47);
            probe.resolution = 128;
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.boxProjection = true;
            probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
            probe.backgroundColor = new Color(.026f, .040f, .056f);

            // Local Post-Processing Volume agar color grading pasar malam tidak memengaruhi kamar kos Chapter 1.
            var grade = new GameObject("Suasana pasar malam").AddComponent<Volume>();
            grade.transform.SetParent(root, false);
            grade.transform.localPosition = new Vector3(0f, 4f, 52f);
            grade.isGlobal = false;
            grade.priority = 5f;
            grade.weight = 1f;

            string gradePath = Mats + "/PasarMalam.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(gradePath);
            if (profile == null)
            {
                AssetDatabase.CopyAsset(ChapterTwoStreetBuilder.Art + "/Materials/KosAtmosphere/KosAtmosphereVolume.asset", gradePath);
                profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(gradePath);
            }

            var tonemapping = GetOrAdd<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.ACES);

            var color = GetOrAdd<ColorAdjustments>(profile);
            color.postExposure.Override(-0.08f);
            color.contrast.Override(14f);
            color.saturation.Override(-18f);
            color.colorFilter.Override(new Color(0.92f, 0.96f, 1.0f));

            var bloom = GetOrAdd<Bloom>(profile);
            bloom.threshold.Override(0.90f);
            bloom.intensity.Override(0.48f);
            bloom.scatter.Override(0.68f);
            bloom.highQualityFiltering.Override(true);

            var vignette = GetOrAdd<Vignette>(profile);
            vignette.color.Override(new Color(0.015f, 0.025f, 0.04f));
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.45f);

            var grain = GetOrAdd<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Medium1);
            grain.intensity.Override(0.20f);
            grain.response.Override(0.72f);

            var chromatic = GetOrAdd<ChromaticAberration>(profile);
            chromatic.intensity.Override(0.06f);

            var shadows = GetOrAdd<LiftGammaGain>(profile);
            shadows.lift.Override(new Vector4(0.985f, 0.995f, 1.015f, 0.0f));

            EditorUtility.SetDirty(tonemapping);
            EditorUtility.SetDirty(color);
            EditorUtility.SetDirty(bloom);
            EditorUtility.SetDirty(vignette);
            EditorUtility.SetDirty(grain);
            EditorUtility.SetDirty(chromatic);
            EditorUtility.SetDirty(shadows);
            EditorUtility.SetDirty(profile);

            grade.sharedProfile = profile;
            var bounds = grade.gameObject.AddComponent<BoxCollider>();
            bounds.isTrigger = true;
            bounds.size = new Vector3(36f, 16f, 160f);
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T component))
            {
                component = profile.Add<T>();
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            return component;
        }

        private static void AddCables(Transform root, Material cable)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                for (int wire = 0; wire < 3; wire++)
                {
                    var line = new GameObject("Kabel udara").AddComponent<LineRenderer>();
                    line.transform.SetParent(root, false);
                    line.useWorldSpace = false;
                    line.sharedMaterial = cable;
                    line.startWidth = line.endWidth = .022f;
                    line.positionCount = 37;

                    for (int i = 0; i < 37; i++)
                    {
                        float sag = Mathf.Sin(Mathf.PI * (i % 12) / 12f);
                        line.SetPosition(i, new Vector3(
                            side * (5.65f + .17f * wire),
                            6.8f - .76f * sag + .10f * wire,
                            -4 + i * 1.34f));
                    }
                }
            }
        }

        private static void AddOvercastSkyDome(Transform root)
        {
            var sky = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sky.name = "Langit mendung";
            sky.transform.SetParent(root, false);
            sky.transform.localPosition = new Vector3(0, 0, 19);
            sky.transform.localScale = Vector3.one * 128;
            Object.DestroyImmediate(sky.GetComponent<Collider>());

            var skyMat = AssetDatabase.LoadAssetAtPath<Material>(Mats + "/Overcast.mat");
            if (skyMat == null)
            {
                skyMat = new Material(Shader.Find("NoRainbowAfter/OvercastStreet"));
                AssetDatabase.CreateAsset(skyMat, Mats + "/Overcast.mat");
            }
            var skyRenderer = sky.GetComponent<Renderer>();
            skyRenderer.sharedMaterial = skyMat;
            skyRenderer.shadowCastingMode = ShadowCastingMode.Off;
            skyRenderer.receiveShadows = false;
        }
    }
}
