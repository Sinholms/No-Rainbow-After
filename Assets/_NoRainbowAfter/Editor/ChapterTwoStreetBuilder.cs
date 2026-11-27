using System.Collections.Generic;
using System.IO;
using NoRainbowAfter.AI;
using NoRainbowAfter.Interaction;
using UnityEditor;
using UnityEngine;

namespace NoRainbowAfter.Editor
{
    /// <summary>
    /// Builder editor pembangun prefab lingkungan Chapter 2 (Assets/_NoRainbowAfter/Resources/ChapterTwoStreet.prefab).
    ///
    /// [GRAFIKA KOMPUTER]
    /// Membangun struktur geometris jalan pasar malam 1:1 (aspal dua lajur, trotoar, warung,
    /// halte zona aman, rintangan cover tinggi 1.04m, PJU, serta penanda polisi), lalu
    /// memanggil ChapterTwoStreetArt.Dress() untuk memasang model FBX, PBR, langit, dan Post-Processing.
    ///
    /// [TEKNIS KODING]
    /// Dipecah menjadi 2 file partial class:
    /// 1. ChapterTwoStreetBuilder.cs (geometri jalan, halte, warung, cover, PJU, utilitas primitif)
    /// 2. ChapterTwoStreetBuilder.PropsAndPosts.cs (dekorasi TKP, pita polisi, mobil patroli, pos polisi, bus)
    /// </summary>
    public static partial class ChapterTwoStreetBuilder
    {
        internal const string Art = "Assets/_NoRainbowAfter/Art";
        private const string MatDir = Art + "/Materials/ChapterTwo";
        private const string Destination = "Assets/_NoRainbowAfter/Resources/ChapterTwoStreet.prefab";

        internal const float CordonZ = 46f;
        internal const float RoadEndZ = 134f;

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

                var tar = Material("Wet asphalt", "303640", .91f);
                var concrete = Material("Kerb", "78746C", .52f);
                var timber = Material("Packing wood", "8C6C47", .30f);
                var ink = Material("Night metal", "1E2A36", .57f);
                var teal = Material("Enamel green", "28615F", .62f);
                var stripe = Material("Worn road paint", "B0A37E", .68f);
                var sand = Material("Police ochre", "C99F42", .40f);
                var wall = Material("Shophouse plaster", "576062", .31f);
                var warm = Material("Warm lightbox", "FFE4AD", .45f, 1.15f);
                var coneOrange = Material("Hazard cone", "C4461E", .52f);
                var trashBag = Material("Dark market plastic", "141A1F", .74f);

                var flickerLights = new List<Light>();
                var flickerEmissives = new List<Renderer>();

                // 1. Geometri dasar jalan aspal dua lajur, trotoar, marka, dan batas gang.
                var ground = BuildRoadAndSidewalks(t, tar, concrete, ink, stripe, wall);

                // 2. Warung Sri di awal jalan + lampu warung berkedip.
                BuildWarung(t, teal, ink, sand, flickerLights, flickerEmissives);

                // 3. Rambu halte & Halte Pasar (Zona Aman + Tas Raka interaktif).
                BuildHalteAndSafeZone(street, t, ground, concrete, timber, teal, ink, warm);

                // 4. Peti kayu dan pembatas beton 1.04m sebagai perlindungan dari sorot polisi.
                BuildCoverObjects(t, timber, concrete, sand);

                // 5. Mobil patroli, 2 pos polisi bersenter, & 2 titik checkpoint.
                street.PatrolCar = Car(t, "Patroli TKP", 4.9f, 7.4f, -22f, ink, warm);
                var fence = Material("C2 Sentry Fence", "3F4644", .63f);
                street.Lamps = new[]
                {
                    PolicePost(t, "Polisi 1", 5.36f, 15.8f, -90f, fence, ink, warm),
                    PolicePost(t, "Polisi 2", -5.36f, 29.1f, 90f, fence, ink, warm)
                };
                street.Checkpoints = new[]
                {
                    Node(t, "Checkpoint sebelum polisi 1", -3.55f, .04f, 1.28f, 8f),
                    Node(t, "Checkpoint antara dua pos", .94f, .04f, 21.9f, 4f)
                };

                // 6. Penerangan jalan umum (PJU) area pasar & PJU redup di jalan raya jauh.
                BuildStreetLamps(t, ground, ink, warm, flickerLights, flickerEmissives);

                // 7. Garis polisi & dekorasi TKP (kerucut oranye, sampah, pita).
                AddCordon(ground, sand, ink, concrete);
                AddCrimeSceneDressing(ground, coneOrange, warm, timber, trashBag, sand, ink);

                // 8. Pasang model FBX Blender, tekstur PBR, bulan, kabut volume, & langit mendung.
                ChapterTwoStreetArt.Dress(street, flickerLights, flickerEmissives);
                street.GazePivots = System.Array.ConvertAll(street.Lamps, lamp =>
                    lamp.parent.Find("ACT_TKPSentry/Sentry gaze") ??
                    throw new InvalidDataException("Missing neck pivot on " + lamp.parent.name));

                // 9. Bus terakhir di depan halte.
                AddBus(street, t, ink, warm);

                street.FlickerLights = flickerLights.ToArray();
                street.FlickerEmissives = flickerEmissives.ToArray();
                street.RainAmbienceClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_NoRainbowAfter/Audio/Ambience/AUD_KosRain.wav");
                street.ElectricTickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_NoRainbowAfter/Audio/SFX/AUD_C1_ElectricTick.wav");

                PrefabUtility.SaveAsPrefabAsset(root, Destination);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // =========================================================================
        // Pembangun Zona Utama Jalan: Aspal, Warung, Halte, Cover, & PJU
        // =========================================================================

        private static Transform BuildRoadAndSidewalks(Transform root, Material tar, Material concrete,
            Material ink, Material stripe, Material wall)
        {
            var ground = Node(root, "Jalan", 0, 0, 0);
            const float roadStart = -4f;
            float roadLength = RoadEndZ - roadStart;
            float roadMiddle = (roadStart + RoadEndZ) * .5f;

            Box(ground, "Two lanes", 0, -.18f, roadMiddle, 9.6f, .36f, roadLength, tar);

            for (int side = -1; side <= 1; side += 2)
            {
                Box(ground, "Trotoar", side * 5.8f, -.06f, roadMiddle, 2, .36f, roadLength, concrete);
                // Apron kontinu di bawah ruko agar tidak ada celah di sepanjang tepi jalan.
                Box(ground, "Apron trotoar", side * 9.8f, -.065f, roadMiddle, 7.8f, .36f, roadLength, concrete, false);
                Box(ground, "Latar belakang ruko", side * 8.55f, 2.8f, (CordonZ + 4f + roadStart) * .5f,
                    .25f, 5.8f, CordonZ + 4f - roadStart, ink, false);

                // Bahu jalan menopang tanah di bawah ruko yang berjarak lebih renggang.
                const float shoulderStart = CordonZ + 2f;
                Box(ground, "Bahu jalan raya", side * 18.8f, -.16f, (RoadEndZ + shoulderStart) * .5f,
                    28f, .38f, RoadEndZ - shoulderStart, concrete, false);

                for (int z = -3; z < 42; z += 2)
                {
                    Box(ground, "Kerb joint", side * 4.87f, .126f, z, .14f, .008f, 1.92f, concrete, false);
                }
                Box(ground, "Edge line", side * 4.53f, .008f, roadMiddle, .08f, .01f, roadLength, stripe, false);
                Box(ground, "Batas gang", side * 6.92f, 2.1f, (CordonZ + roadStart) * .5f, .25f, 4.2f, CordonZ - roadStart + .4f, wall);
            }

            for (int z = -1; z < RoadEndZ - 2; z += 5)
            {
                Box(ground, "Lane marking", 0, .008f, z, .12f, .012f, 2.3f, stripe, false);
            }
            Box(ground, "Di belakang warung", 0, 2, -4, 14, 4, .35f, wall);
            return ground;
        }

        private static void BuildWarung(Transform root, Material teal, Material ink, Material sand,
            List<Light> flickerLights, List<Renderer> flickerEmissives)
        {
            var shop = Node(root, "Warung", -6.56f, .12f, 3.8f, 90);
            Box(shop, "Facade", 0, 1.8f, 0, 7.2f, 3.6f, .44f, teal);
            Box(shop, "Kios tutup", 0, 1.26f, .27f, 4.35f, 2.5f, .14f, ink);
            Box(shop, "Kanopi", 0, 3.18f, .53f, 7.65f, .15f, 1.78f, teal);
            Box(shop, "Papan nama", 0, 2.84f, .35f, 5.9f, .52f, .11f, sand);
            Sign(shop, "WARUNG  SRI", 0, 2.86f, .42f, .34f, ink.color, 180);

            var shopLamp = LightAt(shop, "Lampu warung", 0, 2.65f, 1.0f, new Color(.96f, .64f, .34f), 3.1f, 7.8f);
            flickerLights.Add(shopLamp);
            flickerEmissives.Add(null);
        }

        private static void BuildHalteAndSafeZone(ChapterTwoStreet street, Transform root, Transform ground,
            Material concrete, Material timber, Material teal, Material ink, Material warm)
        {
            var halteSign = Node(ground, "Rambu halte", 6.05f, .12f, 35.5f);
            Box(halteSign, "Tiang rambu", 0, 1.37f, 0, .055f, 2.74f, .055f, ink, false);
            Box(halteSign, "Papan halte", 0, 2.61f, 0, 1.06f, .61f, .07f, teal, false);
            ChapterTwoStreetArt.Lettering(halteSign, "HALTE", new Vector3(0, 2.69f, -.04f),
                Quaternion.identity, 1f, .29f, .24f, new Color(.95f, .91f, .75f));
            ChapterTwoStreetArt.Lettering(halteSign, "PASAR  04", new Vector3(0, 2.45f, -.04f),
                Quaternion.identity, 1f, .19f, .13f, new Color(.95f, .91f, .75f));

            var stop = Node(root, "Halte", 5.60f, .12f, 38.65f, 90f);
            Box(stop, "Plinth", 0, .07f, 0, 5.45f, .14f, 2.85f, concrete);
            Box(stop, "Rear panel", 0, 1.3f, 1.12f, 5.1f, 2.35f, .18f, teal);
            Box(stop, "Bench", 0, .56f, .55f, 3.8f, .20f, .52f, timber);
            foreach (float x in new[] { -2.42f, 2.42f })
            {
                Box(stop, "Shelter post", x, 1.52f, -.85f, .14f, 3, .14f, teal);
            }
            Box(stop, "Canopy", 0, 3.02f, 0, 5.6f, .2f, 3.12f, teal);
            Box(stop, "Illuminated destination", 0, 3.36f, -1.40f, 5.4f, .77f, .17f, warm);
            Sign(stop, "HALTE   PASAR", 0, 3.36f, -1.50f, .48f, ink.color, 0);

            // Cahaya hijau-toska redup menandakan halte sebagai zona aman dari kejaran sorot.
            LightAt(stop, "Lampu tunggu", 0, 2.67f, -.3f, new Color(.48f, .86f, .74f), 2.1f, 6.8f);

            var zone = Node(stop, "Zona aman", 0, 1.2f, -.10f).gameObject.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.size = new Vector3(5.5f, 2.6f, 3.15f);
            street.Shelter = zone;

            var bag = Box(stop, "Tas Raka", .50f, .99f, .48f, .50f, .67f, .30f, ink);
            var trinket = Box(bag, "Gantungan reflektor", .34f, -.12f, -.56f, .17f, .14f, .10f, warm, false);
            trinket.localRotation = Quaternion.Euler(0, 0, 45);
            street.Bag = bag.gameObject.AddComponent<SceneInteractable>();
            street.Bag.Configure(
                "INT_C2_BAG",
                "Tas di bangku",
                "Periksa tas",
                "c2.bag",
                "Tas Raka",
                "Di saku tas Raka ada rekaman lama. Di sela hujan terdengar, ‘Ella … masih …’ lalu suara itu terputus.",
                true);
        }

        private static void BuildCoverObjects(Transform root, Material timber, Material concrete, Material sand)
        {
            var cover = Node(root, "Cover", 0, 0, 0);
            Crate(cover, "Peti rendah A", -1.9f, 6.9f, 1.22f, 1.12f, 3.5f, timber);
            Crate(cover, "Peti warung", -4.8f, 2.9f, 1.35f, 1.85f, 1.55f, timber);
            Crate(cover, "Peti B", 2.1f, 14.25f, 2.5f, 1.88f, 1.2f, timber);
            Crate(cover, "Peti seberang", -2.65f, 20f, 1.4f, 1.40f, 1.6f, timber);
            Crate(cover, "Peti C", -.55f, 25.25f, 2.3f, 1.85f, 1.2f, timber);
            Crate(cover, "Peti jalur pinggir", 4.82f, 27.35f, 1.85f, 1.60f, 1.25f, timber);
            Crate(cover, "Peti pinggir kedua", 4.34f, 32.6f, 1.75f, 1.8f, 1.2f, timber);
            Barrier(cover, 1.4f, 30.3f, 0, concrete, sand);
            Barrier(cover, -4.75f, 26.5f, 90, concrete, sand);
        }

        private static void BuildStreetLamps(Transform root, Transform ground, Material ink, Material warm,
            List<Light> flickerLights, List<Renderer> flickerEmissives)
        {
            // Lampu jalan pasar dibuat kontras tidak merata agar menyisakan kantong gelap untuk stealth.
            for (int i = 0; i < 3; i++)
            {
                float x = i % 2 == 0 ? 5.96f : -5.96f;
                float z = 6 + i * 12;
                Box(root, "Tiang PJU", x, 2.7f, z, .15f, 5.4f, .15f, ink);
                Box(root, "Lengan PJU", x - Mathf.Sign(x) * .6f, 5.32f, z, 1.3f, .08f, .11f, ink, false);
                var lantern = Box(root, "Lentera", x - Mathf.Sign(x) * 1.21f, 5.26f, z, .47f, .12f, .73f, warm, false);
                var lamp = LightAt(
                    root,
                    "Cahaya jalan",
                    x - Mathf.Sign(x) * 1.21f,
                    5.05f,
                    z,
                    i == 1 ? new Color(.56f, .82f, .78f) : new Color(.96f, .68f, .38f),
                    i == 0 ? 11.5f : (i == 1 ? 8.5f : 5.2f),
                    i == 2 ? 11f : 14.5f);
                lamp.type = LightType.Spot;
                lamp.spotAngle = 100f;
                lamp.innerSpotAngle = 56f;
                lamp.transform.localRotation = Quaternion.LookRotation(new Vector3(-Mathf.Sign(x) * .38f, -1f, 0f));

                if (i == 1 || i == 2)
                {
                    flickerLights.Add(lamp);
                    flickerEmissives.Add(lantern.GetComponent<Renderer>());
                }
            }

            // PJU jalan raya di luar batas pita polisi dibuat sangat redup untuk menjaga kedalaman visual.
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float z = 52f + i * 14f;
                var pole = Node(ground, "PJU jalan raya", side * 5.85f, 0, z);
                Box(pole, "Tiang", 0, 3.2f, 0, .115f, 6.4f, .115f, ink, false);
                Box(pole, "Lengan", -side * .68f, 6.35f, 0, 1.45f, .085f, .10f, ink, false);
                Box(pole, "Lampu", -side * 1.34f, 6.28f, 0, .46f, .11f, .63f, warm, false);
                var lamp = LightAt(
                    pole,
                    "Cahaya aspal",
                    -side * 1.34f,
                    6.16f,
                    0,
                    i % 3 == 0 ? new Color(.90f, .60f, .32f) : new Color(.48f, .68f, .76f),
                    Mathf.Lerp(1.6f, 0.45f, i / 5f),
                    8.5f);
                lamp.type = LightType.Spot;
                lamp.spotAngle = 82f;
                lamp.transform.localRotation = Quaternion.LookRotation(new Vector3(-side * .48f, -1, 0));
            }
        }

        // =========================================================================
        // Helper Primitif 3D, Collider, Lampu, & Material Dasar Editor
        // =========================================================================

        private static Transform ColliderAt(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var collider = Node(parent, name, center.x, center.y, center.z).gameObject.AddComponent<BoxCollider>();
            collider.size = size;
            return collider.transform;
        }

        private static Transform PlaceModel(string model, string frontMarker, Transform parent)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Models/Environment/ChapterTwo/" + model + ".fbx");
            if (asset == null)
            {
                throw new FileNotFoundException(model + ".fbx");
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            foreach (var part in instance.GetComponentsInChildren<MeshRenderer>())
            {
                if (part.name == frontMarker && parent.InverseTransformPoint(part.bounds.center).z < 0f)
                {
                    instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    break;
                }
            }
            return instance.transform;
        }

        private static void Crate(Transform t, string name, float x, float z, float w, float h, float d, Material mat)
            => Box(t, name, x, h * .5f, z, w, h, d, mat);

        private static void Barrier(Transform t, float x, float z, float yaw, Material mat, Material band)
        {
            var root = Node(t, "Pembatas jalan", x, 0, z, yaw);
            // Tinggi 1.04 m melindungi pemain saat jongkok namun tetap mengekspos kepala saat berdiri.
            Box(root, "Blok beton", 0, .52f, 0, 2.65f, 1.04f, .64f, mat);
            Box(root, "Pita polisi", 0, .82f, -.326f, 2.65f, .13f, .012f, band, false);
        }

        private static Transform Node(Transform parent, string name, float x, float y, float z, float yaw = 0)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = new Vector3(x, y, z);
            t.localRotation = Quaternion.Euler(0, yaw, 0);
            return t;
        }

        private static Transform Box(Transform parent, string name, float x, float y, float z, float w, float h, float d, Material mat, bool solid = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = new Vector3(x, y, z);
            t.localScale = new Vector3(w, h, d);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!solid)
            {
                Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            }
            return t;
        }

        private static Light LightAt(Transform t, string name, float x, float y, float z, Color color, float intensity, float range)
        {
            var l = Node(t, name, x, y, z).gameObject.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            return l;
        }

        private static void Sign(Transform t, string label, float x, float y, float z, float height, Color color, float yaw)
        {
            var text = Node(t, label, x, y, z, yaw).gameObject.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 96;
            text.characterSize = height * .12f;
            text.color = color;
            text.text = label;
        }

        private static Material Material(string name, string hex, float smoothness, float emission = 0)
        {
            string path = MatDir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            if (emission > 0)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * emission);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
