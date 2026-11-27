using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Efek atmosfer, pencahayaan dinamis, dan sistem partikel cuaca pada ChapterTwoStreet.
    ///
    /// [GRAFIKA KOMPUTER — CHAPTER 2]
    /// 1. Fog Eksponensial Kuadratik (FogMode.ExponentialSquared):
    ///    Kepadatan kabut meningkat secara non-linear terhadap jarak kamera sehingga ujung jalan TKP
    ///    tenggelam perlahan dalam kabut biru gelap.
    /// 2. Pencahayaan Ambien Trilight (AmbientMode.Trilight):
    ///    Menginterpolasi warna langit, cakrawala (equator), dan pantulan tanah aspal basah.
    /// 3. MaterialPropertyBlock pada Lampu Berkedip:
    ///    Mengubah _EmissionColor bohlam tanpa menggandakan instance Material (menjaga GPU instancing).
    /// 4. Animasi Rotator Polisi Double-Pulse:
    ///    Fungsi sinus berpasangan (DoublePulse) meniru strobo mobil patroli biru-merah yang realistis.
    /// 5. Sistem Partikel Cuaca:
    ///    - Hujan malam: ParticleSystemRenderMode.Stretch mengikuti vektor kecepatan angin.
    ///    - Kabut pasar: partikel berukuran besar dengan kurva Gradient alpha fade-in/out.
    /// </summary>
    public sealed partial class ChapterTwoStreet
    {
        // =========================================================================
        // Manajemen RenderSettings (Fog & Ambient Light Save / Restore)
        // =========================================================================

        private void ApplyHorrorAtmosphere()
        {
            if (!atmosphereSaved)
            {
                savedFog = RenderSettings.fog;
                savedFogColor = RenderSettings.fogColor;
                savedFogMode = RenderSettings.fogMode;
                savedFogDensity = RenderSettings.fogDensity;
                savedAmbientMode = RenderSettings.ambientMode;
                savedAmbientSky = RenderSettings.ambientSkyColor;
                savedAmbientEquator = RenderSettings.ambientEquatorColor;
                savedAmbientGround = RenderSettings.ambientGroundColor;
                atmosphereSaved = true;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.014f, 0.022f, 0.032f);
            RenderSettings.fogDensity = 0.036f;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.055f, 0.078f, 0.11f);
            RenderSettings.ambientEquatorColor = new Color(0.032f, 0.042f, 0.055f);
            RenderSettings.ambientGroundColor = new Color(0.015f, 0.018f, 0.024f);
        }

        private void RestoreAtmosphere()
        {
            if (!atmosphereSaved)
            {
                return;
            }
            RenderSettings.fog = savedFog;
            RenderSettings.fogColor = savedFogColor;
            RenderSettings.fogMode = savedFogMode;
            RenderSettings.fogDensity = savedFogDensity;
            RenderSettings.ambientMode = savedAmbientMode;
            RenderSettings.ambientSkyColor = savedAmbientSky;
            RenderSettings.ambientEquatorColor = savedAmbientEquator;
            RenderSettings.ambientGroundColor = savedAmbientGround;
            atmosphereSaved = false;
        }

        // =========================================================================
        // Inisialisasi State Lampu Kedip, Visual Penjaga, & Partikel Cuaca
        // =========================================================================

        private void InitFlickerState()
        {
            if (FlickerLights == null || FlickerLights.Length == 0 || flickerBaseIntensity != null)
            {
                return;
            }
            int count = FlickerLights.Length;
            flickerBaseIntensity = new float[count];
            flickerNextToggle = new float[count];
            flickerBurstEdges = new int[count];
            flickerOn = new bool[count];
            flickerBlock = new MaterialPropertyBlock();

            for (int i = 0; i < count; i++)
            {
                flickerBaseIntensity[i] = FlickerLights[i] != null ? FlickerLights[i].intensity : 0f;
                flickerNextToggle[i] = 1.4f + i * 1.15f;
                flickerOn[i] = true;
            }
        }

        private void InitSentryVisuals()
        {
            if (Lamps == null || sentryVisuals != null)
            {
                return;
            }
            sentryVisuals = new Transform[Lamps.Length];
            sentryBaseScale = new Vector3[Lamps.Length];

            for (int i = 0; i < Lamps.Length; i++)
            {
                if (Lamps[i] == null || Lamps[i].parent == null)
                {
                    continue;
                }
                var model = Lamps[i].parent.Find("ACT_TKPSentry");
                if (model != null)
                {
                    sentryVisuals[i] = model;
                    sentryBaseScale[i] = model.localScale;
                }
            }
        }

        private void EnsureWeatherParticles()
        {
            if (rainParticles != null)
            {
                return;
            }
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            }
            if (shader == null)
            {
                return;
            }

            particleMaterial = new Material(shader);
            particleMaterial.color = Color.white;

            CreateRainParticles();
            CreateMistParticles();
        }

        private void CreateRainParticles()
        {
            // Hujan malam miring tertiup angin melintasi jalan pasar.
            var rainGo = new GameObject("Hujan malam");
            rainGo.transform.SetParent(transform, false);
            rainGo.transform.localPosition = new Vector3(0f, 10.5f, 20f);

            rainParticles = rainGo.AddComponent<ParticleSystem>();
            var rMain = rainParticles.main;
            rMain.loop = true;
            rMain.prewarm = true;
            rMain.startLifetime = 0.85f;
            rMain.startSpeed = 16f;
            rMain.startSize3D = true;
            rMain.startSizeX = 0.012f;
            rMain.startSizeY = 0.24f;
            rMain.startSizeZ = 0.012f;
            rMain.startColor = new Color(0.58f, 0.70f, 0.82f, 0.18f);
            rMain.maxParticles = 600;
            rMain.simulationSpace = ParticleSystemSimulationSpace.Local;

            var rEmit = rainParticles.emission;
            rEmit.rateOverTime = 460f;

            var rShape = rainParticles.shape;
            rShape.shapeType = ParticleSystemShapeType.Box;
            rShape.scale = new Vector3(16f, 1f, 52f);
            rShape.rotation = new Vector3(101f, 0f, -9f);

            var rRend = rainGo.GetComponent<ParticleSystemRenderer>();
            rRend.sharedMaterial = particleMaterial;
            rRend.renderMode = ParticleSystemRenderMode.Stretch;
            rRend.lengthScale = 1.45f;
            rRend.velocityScale = 0.04f;
            rRend.shadowCastingMode = ShadowCastingMode.Off;
            rRend.receiveShadows = false;
        }

        private void CreateMistParticles()
        {
            // Kabut rendah yang bergerak lambat menyusuri aspal basah.
            var mistGo = new GameObject("Kabut jalan pasar");
            mistGo.transform.SetParent(transform, false);
            mistGo.transform.localPosition = new Vector3(0f, 0.95f, 21f);

            mistParticles = mistGo.AddComponent<ParticleSystem>();
            var mMain = mistParticles.main;
            mMain.loop = true;
            mMain.prewarm = true;
            mMain.startLifetime = new ParticleSystem.MinMaxCurve(6.5f, 10f);
            mMain.startSpeed = new ParticleSystem.MinMaxCurve(0.22f, 0.55f);
            mMain.startSize = new ParticleSystem.MinMaxCurve(3.4f, 6.2f);
            mMain.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            mMain.startColor = new Color(0.18f, 0.25f, 0.32f, 0.075f);
            mMain.maxParticles = 48;
            mMain.simulationSpace = ParticleSystemSimulationSpace.Local;

            var mEmit = mistParticles.emission;
            mEmit.rateOverTime = 5.5f;

            var mShape = mistParticles.shape;
            mShape.shapeType = ParticleSystemShapeType.Box;
            mShape.scale = new Vector3(13f, 1.3f, 44f);

            var mCol = mistParticles.colorOverLifetime;
            mCol.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.22f, 0.30f, 0.38f), 0f),
                    new GradientColorKey(new Color(0.14f, 0.20f, 0.28f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.85f, 0.35f),
                    new GradientAlphaKey(0.75f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                });
            mCol.color = grad;

            var mRend = mistGo.GetComponent<ParticleSystemRenderer>();
            mRend.sharedMaterial = particleMaterial;
            mRend.shadowCastingMode = ShadowCastingMode.Off;
            mRend.receiveShadows = false;
        }

        // =========================================================================
        // Pembaruan Per-Frame: Rotator Patroli, Lampu Kedip, & Napas Penjaga
        // =========================================================================

        private void UpdatePatrolBeacons()
        {
            if (beacons == null || beacons.Length == 0)
            {
                return;
            }
            // Strobo darurat dua-denyut bergantian (biru lalu merah).
            float cycle = (Time.time * 1.45f) % 1f;
            float blueFlash = DoublePulse(cycle);
            float redFlash = DoublePulse((cycle + 0.5f) % 1f);

            for (int i = 0; i < beacons.Length; i++)
            {
                if (beacons[i] == null)
                {
                    continue;
                }
                float flash = i == 0 ? blueFlash : redFlash;
                beacons[i].intensity = 0.25f + 16.5f * flash;
            }
        }

        private static float DoublePulse(float phase)
        {
            // Dua kilatan singkat di [0.00..0.11] dan [0.16..0.28], lalu padam hingga siklus setengah berikutnya.
            float p1 = phase < 0.11f ? Mathf.Sin(phase / 0.11f * Mathf.PI) : 0f;
            float p2 = (phase > 0.16f && phase < 0.28f) ? Mathf.Sin((phase - 0.16f) / 0.12f * Mathf.PI) : 0f;
            float peak = Mathf.Max(p1, p2);
            return peak * peak;
        }

        private void UpdateFlickerLights(float dt)
        {
            if (FlickerLights == null || flickerBaseIntensity == null)
            {
                return;
            }
            for (int i = 0; i < FlickerLights.Length; i++)
            {
                var lamp = FlickerLights[i];
                if (lamp == null)
                {
                    continue;
                }

                flickerNextToggle[i] -= dt;
                if (flickerNextToggle[i] > 0f)
                {
                    continue;
                }

                if (flickerBurstEdges[i] <= 0)
                {
                    flickerBurstEdges[i] = UnityEngine.Random.Range(2, 6) * 2;
                }

                flickerOn[i] = !flickerOn[i];
                flickerBurstEdges[i]--;

                bool on = flickerOn[i];
                lamp.intensity = on
                    ? flickerBaseIntensity[i] * UnityEngine.Random.Range(0.75f, 1.08f)
                    : flickerBaseIntensity[i] * 0.04f;

                if (FlickerEmissives != null && i < FlickerEmissives.Length && FlickerEmissives[i] != null)
                {
                    flickerBlock.SetColor("_EmissionColor", on ? new Color(1.1f, 0.88f, 0.56f) : Color.black);
                    FlickerEmissives[i].SetPropertyBlock(flickerBlock);
                }

                // Bunyikan percikan listrik dengan peluruhan kuadrat jarak (inverse-square attenuation).
                if (!on && crackleAudio != null && ElectricTickClip != null && motor != null)
                {
                    float dist = Vector3.Distance(motor.transform.position, lamp.transform.position);
                    if (dist < 14f)
                    {
                        float atten = Mathf.Clamp01(1f - dist / 14f);
                        crackleAudio.pitch = UnityEngine.Random.Range(0.86f, 1.18f);
                        crackleAudio.PlayOneShot(ElectricTickClip, 0.28f * atten * atten);
                    }
                }

                flickerNextToggle[i] = flickerBurstEdges[i] > 0
                    ? UnityEngine.Random.Range(0.05f, 0.17f)
                    : UnityEngine.Random.Range(1.8f, 4.8f);
            }
        }

        private void UpdateSentryBreathing()
        {
            if (sentryVisuals == null)
            {
                return;
            }
            for (int i = 0; i < sentryVisuals.Length; i++)
            {
                var visual = sentryVisuals[i];
                if (visual == null)
                {
                    continue;
                }
                // Deformasi skala lokal halus memberi kesan jas hujan polisi bergerak saat bernapas.
                float breath = Mathf.Sin(Time.time * 1.55f + i * 2.1f);
                Vector3 baseScale = sentryBaseScale[i];
                visual.localScale = new Vector3(
                    baseScale.x * (1f + breath * 0.012f),
                    baseScale.y * (1f + breath * 0.008f),
                    baseScale.z);
            }
        }
    }
}
