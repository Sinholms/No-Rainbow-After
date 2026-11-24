using System.Collections.Generic;
using System.IO;
using NoRainbowAfter.AI;
using NoRainbowAfter.Interaction;
using UnityEditor;
using UnityEngine;

namespace NoRainbowAfter.Editor
{
    // The editable prefab is also the street shared by the crash memory and the encounter.
    public static class ChapterTwoStreetBuilder
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

                var ground = Node(t, "Jalan", 0, 0, 0);
                const float roadStart = -4f;
                float roadLength = RoadEndZ - roadStart;
                float roadMiddle = (roadStart + RoadEndZ) * .5f;
                Box(ground, "Two lanes", 0, -.18f, roadMiddle, 9.6f, .36f, roadLength, tar);
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(ground, "Trotoar", side * 5.8f, -.06f, roadMiddle, 2, .36f, roadLength, concrete);
                    // The shoulders carry the ground beneath the more widely spaced roadside buildings.
                    const float shoulderStart = CordonZ + 2f;
                    Box(ground, "Bahu jalan raya", side * 18.8f, -.16f, (RoadEndZ + shoulderStart) * .5f,
                        28f, .38f, RoadEndZ - shoulderStart, concrete, false);
                    for (int z = -3; z < 42; z += 2)
                        Box(ground, "Kerb joint", side * 4.87f, .126f, z, .14f, .008f, 1.92f, concrete, false);
                    Box(ground, "Edge line", side * 4.53f, .008f, roadMiddle, .08f, .01f, roadLength, stripe, false);
                    Box(ground, "Batas gang", side * 6.92f, 2.1f, (CordonZ + roadStart) * .5f, .25f, 4.2f, CordonZ - roadStart + .4f, wall);
                }
                for (int z = -1; z < RoadEndZ - 2; z += 5)
                    Box(ground, "Lane marking", 0, .008f, z, .12f, .012f, 2.3f, stripe, false);
                Box(ground, "Di belakang warung", 0, 2, -4, 14, 4, .35f, wall);

                var shop = Node(t, "Warung", -6.56f, .12f, 3.8f, 90);
                Box(shop, "Facade", 0, 1.8f, 0, 7.2f, 3.6f, .44f, teal);
                Box(shop, "Kios tutup", 0, 1.26f, .27f, 4.35f, 2.5f, .14f, ink);
                Box(shop, "Kanopi", 0, 3.18f, .53f, 7.65f, .15f, 1.78f, teal);
                Box(shop, "Papan nama", 0, 2.84f, .35f, 5.9f, .52f, .11f, sand);
                Sign(shop, "WARUNG  SRI", 0, 2.86f, .42f, .34f, ink.color, 180);
                var shopLamp = LightAt(shop, "Lampu warung", 0, 2.65f, 1.0f, new Color(.96f, .64f, .34f), 3.1f, 7.8f);
                flickerLights.Add(shopLamp);
                flickerEmissives.Add(null);

                var halteSign = Node(ground, "Rambu halte", 6.05f, .12f, 35.5f);
                Box(halteSign, "Tiang rambu", 0, 1.37f, 0, .055f, 2.74f, .055f, ink, false);
                Box(halteSign, "Papan halte", 0, 2.61f, 0, 1.06f, .61f, .07f, teal, false);
                ChapterTwoStreetArt.Lettering(halteSign, "HALTE", new Vector3(0, 2.69f, -.04f),
                    Quaternion.identity, 1f, .29f, .24f, new Color(.95f, .91f, .75f));
                ChapterTwoStreetArt.Lettering(halteSign, "PASAR  04", new Vector3(0, 2.45f, -.04f),
                    Quaternion.identity, 1f, .19f, .13f, new Color(.95f, .91f, .75f));

                var stop = Node(t, "Halte", 5.60f, .12f, 38.65f, 90f);
                Box(stop, "Plinth", 0, .07f, 0, 5.45f, .14f, 2.85f, concrete);
                Box(stop, "Rear panel", 0, 1.3f, 1.12f, 5.1f, 2.35f, .18f, teal);
                Box(stop, "Bench", 0, .56f, .55f, 3.8f, .20f, .52f, timber);
                foreach (float x in new[] { -2.42f, 2.42f })
                    Box(stop, "Shelter post", x, 1.52f, -.85f, .14f, 3, .14f, teal);
                Box(stop, "Canopy", 0, 3.02f, 0, 5.6f, .2f, 3.12f, teal);
                Box(stop, "Illuminated destination", 0, 3.36f, -1.40f, 5.4f, .77f, .17f, warm);
                Sign(stop, "HALTE   PASAR", 0, 3.36f, -1.50f, .48f, ink.color, 0);
                // Lonely teal-green safe haven glow visible from spawn through the night fog.
                LightAt(stop, "Lampu tunggu", 0, 2.67f, -.3f, new Color(.48f, .86f, .74f), 4.8f, 9.5f);
                var zone = Node(stop, "Zona aman", 0, 1.2f, -.10f).gameObject.AddComponent<BoxCollider>();
                zone.isTrigger = true; zone.size = new Vector3(5.5f, 2.6f, 3.15f);
                street.Shelter = zone;
                var bag = Box(stop, "Tas Raka", .50f, .99f, .48f, .50f, .67f, .30f, ink);
                var trinket = Box(bag, "Gantungan reflektor", .34f, -.12f, -.56f, .17f, .14f, .10f, warm, false);
                trinket.localRotation = Quaternion.Euler(0, 0, 45);
                street.Bag = bag.gameObject.AddComponent<SceneInteractable>();
                street.Bag.Configure("INT_C2_BAG", "Tas di bangku", "Periksa tas", "c2.bag", "Tas Raka",
                    "Di saku tas Raka ada rekaman lama. Di sela hujan terdengar, ‘Ella … masih …’ lalu suara itu terputus.", true);

                var cover = Node(t, "Cover", 0, 0, 0);
                Crate(cover, "Peti rendah A", -1.9f, 6.9f, 1.22f, 1.12f, 3.5f, timber);
                Crate(cover, "Peti warung", -4.8f, 2.9f, 1.35f, 1.85f, 1.55f, timber);
                Crate(cover, "Peti B", 2.1f, 14.25f, 2.5f, 1.88f, 1.2f, timber);
                Crate(cover, "Peti seberang", -2.65f, 20f, 1.4f, 1.40f, 1.6f, timber);
                Crate(cover, "Peti C", -.55f, 25.25f, 2.3f, 1.85f, 1.2f, timber);
                Crate(cover, "Peti jalur pinggir", 4.82f, 27.35f, 1.85f, 1.60f, 1.25f, timber);
                Crate(cover, "Peti pinggir kedua", 4.34f, 32.6f, 1.75f, 1.8f, 1.2f, timber);
                Barrier(cover, 1.4f, 30.3f, 0, concrete, sand);
                Barrier(cover, -4.75f, 26.5f, 90, concrete, sand);

                var fence = Material("C2 Sentry Fence", "3F4644", .63f);
                street.PatrolCar = Car(t, "Patroli TKP", 4.9f, 7.4f, -22f, ink, warm);
                street.Lamps = new[] {
                    PolicePost(t, "Polisi 1", 5.36f, 15.8f, -90f, fence, ink, warm),
                    PolicePost(t, "Polisi 2", -5.36f, 29.1f, 90f, fence, ink, warm)
                };
                street.Checkpoints = new[] {
                    Node(t, "Checkpoint sebelum polisi 1", -3.55f, .04f, 1.28f, 8f),
                    Node(t, "Checkpoint antara dua pos", .94f, .04f, 21.9f, 4f)
                };

                // Moody, uneven market streetlamps with deep pockets of shadow between pools.
                for (int i = 0; i < 3; i++)
                {
                    float x = i % 2 == 0 ? 5.96f : -5.96f, z = 6 + i * 12;
                    Box(t, "Tiang PJU", x, 2.7f, z, .15f, 5.4f, .15f, ink);
                    Box(t, "Lengan PJU", x - Mathf.Sign(x) * .6f, 5.32f, z, 1.3f, .08f, .11f, ink, false);
                    var lantern = Box(t, "Lentera", x - Mathf.Sign(x) * 1.21f, 5.26f, z, .47f, .12f, .73f, warm, false);
                    var lamp = LightAt(t, "Cahaya jalan", x - Mathf.Sign(x) * 1.21f, 5.05f, z,
                        i == 1 ? new Color(.56f, .82f, .78f) : new Color(.96f, .68f, .38f),
                        i == 1 ? 13.5f : 11.5f, 14.5f);
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

                // Distant streetlamps beyond the cordon are dimmer so the road vanishes into the storm fog.
                for (int i = 0; i < 6; i++)
                {
                    float side = i % 2 == 0 ? -1f : 1f;
                    float z = 45f + i * 14f;
                    var pole = Node(ground, "PJU jalan raya", side * 5.85f, 0, z);
                    Box(pole, "Tiang", 0, 3.2f, 0, .115f, 6.4f, .115f, ink, false);
                    Box(pole, "Lengan", -side * .68f, 6.35f, 0, 1.45f, .085f, .10f, ink, false);
                    Box(pole, "Lampu", -side * 1.34f, 6.28f, 0, .46f, .11f, .63f, warm, false);
                    var lamp = LightAt(pole, "Cahaya aspal", -side * 1.34f, 6.16f, 0,
                        i % 3 == 0 ? new Color(.90f, .60f, .32f) : new Color(.48f, .68f, .76f), 9.2f, 15f);
                    lamp.type = LightType.Spot;
                    lamp.spotAngle = 96f;
                    lamp.transform.localRotation = Quaternion.LookRotation(new Vector3(-side * .48f, -1, 0));
                }

                AddCordon(ground, sand, ink, concrete);
                AddCrimeSceneDressing(ground, coneOrange, warm, timber, trashBag, sand, ink);

                ChapterTwoStreetArt.Dress(street, flickerLights, flickerEmissives);
                street.GazePivots = System.Array.ConvertAll(street.Lamps, lamp =>
                    lamp.parent.Find("ACT_TKPSentry/Sentry gaze") ??
                    throw new InvalidDataException("Missing neck pivot on " + lamp.parent.name));
                AddBus(street, t, ink, warm);

                street.FlickerLights = flickerLights.ToArray();
                street.FlickerEmissives = flickerEmissives.ToArray();
                street.RainAmbienceClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_NoRainbowAfter/Audio/Ambience/AUD_KosRain.wav");
                street.ElectricTickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_NoRainbowAfter/Audio/SFX/AUD_C1_ElectricTick.wav");

                PrefabUtility.SaveAsPrefabAsset(root, Destination);
                AssetDatabase.SaveAssets();
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void AddCrimeSceneDressing(Transform ground, Material coneOrange, Material reflective,
            Material timber, Material trashBag, Material tape, Material metal)
        {
            var dressing = Node(ground, "Dekorasi TKP", 0, 0, 0);

            // Non-blocking police cones near patrol car and cordon perimeter.
            var conePositions = new[]
            {
                new Vector3(3.05f, 0f, 4.95f),
                new Vector3(2.85f, 0f, 9.35f),
                new Vector3(-3.35f, 0f, 44.85f),
                new Vector3(3.15f, 0f, 44.85f)
            };
            for (int i = 0; i < conePositions.Length; i++)
                HazardCone(dressing, "Kerucut TKP " + (i + 1), conePositions[i], coneOrange, reflective, metal);

            // Overturned abandoned market crates & wet garbage sacks tucked tightly against sidewalks (collider-free).
            var spilled1 = Node(dressing, "Peti tumpah kiri", -5.25f, .15f, 8.5f, 24f);
            Box(spilled1, "Peti miring", 0, .24f, 0, .62f, .44f, .86f, timber, false).localRotation = Quaternion.Euler(12f, 18f, -34f);
            Box(spilled1, "Kantong sampah A", .32f, .20f, .62f, .46f, .40f, .48f, trashBag, false).localRotation = Quaternion.Euler(0, 32f, 8f);
            Box(spilled1, "Kantong sampah B", -.18f, .17f, .78f, .38f, .34f, .42f, trashBag, false).localRotation = Quaternion.Euler(6f, -15f, -5f);

            var spilled2 = Node(dressing, "Sampah trotoar kanan", 5.45f, .15f, 24.4f, -12f);
            Box(spilled2, "Kardus basah", 0, .03f, 0, .78f, .05f, .92f, timber, false).localRotation = Quaternion.Euler(0, 19f, 0);
            Box(spilled2, "Kantong sampah C", .18f, .22f, -.52f, .50f, .44f, .52f, trashBag, false);
            Box(spilled2, "Peti pecah", -.24f, .19f, .55f, .52f, .38f, .64f, timber, false).localRotation = Quaternion.Euler(-8f, 28f, 22f);

            // Short crime-scene police ribbon staked next to the patrol car (kept tight to right edge, clear of path).
            var tapePost = Node(dressing, "Pita TKP samping", 3.65f, 0f, 3.6f, 16f);
            Box(tapePost, "Tiang pita", 0, .55f, 0, .045f, 1.1f, .045f, metal, false);
            var ribbon = Box(tapePost, "Pita sobek", .72f, .92f, -.22f, 1.55f, .10f, .012f, tape, false);
            ribbon.localRotation = Quaternion.Euler(0, 18f, -9f);
        }

        private static void HazardCone(Transform parent, string name, Vector3 pos, Material orange, Material stripe, Material darkBase)
        {
            var root = Node(parent, name, pos.x, pos.y, pos.z);
            Box(root, "Alas", 0, .025f, 0, .36f, .05f, .36f, darkBase, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Kerucut";
            body.transform.SetParent(root, false);
            body.transform.localPosition = new Vector3(0, .32f, 0);
            body.transform.localScale = new Vector3(.22f, .28f, .22f);
            body.GetComponent<Renderer>().sharedMaterial = orange;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Reflektor";
            ring.transform.SetParent(root, false);
            ring.transform.localPosition = new Vector3(0, .40f, 0);
            ring.transform.localScale = new Vector3(.232f, .055f, .232f);
            ring.GetComponent<Renderer>().sharedMaterial = stripe;
            Object.DestroyImmediate(ring.GetComponent<Collider>());
        }

        static void AddCordon(Transform ground, Material tape, Material steel, Material concrete)
        {
            var line = Node(ground, "Garis polisi jalan raya", 0, 0, CordonZ);
            const float spacing = 3.45f;
            for (int i = -2; i <= 2; i++)
            {
                float x = i * spacing;
                float height = i % 2 == 0 ? 1.22f : 1.10f;
                Box(line, "Kaki tiang", x, .13f, 0, .34f, .26f, .34f, concrete, false);
                Box(line, "Tiang pengikat", x, height * .5f, 0, .055f, height + .12f, .055f, steel, false);
                if (i == 2) break;
                float rise = i % 2 == 0 ? -.12f : .12f;
                var span = Node(line, "Rentang pita", x + spacing * .5f, height + rise * .5f, 0);
                span.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(rise, spacing) * Mathf.Rad2Deg);
                Box(span, "Pita kuning", 0, 0, 0, spacing + .012f, .15f, .018f, tape, false);
                ChapterTwoStreetArt.Lettering(span, "///   GARIS POLISI   ///   DILARANG MELINTAS   ///",
                    new Vector3(0, 0, -.013f), Quaternion.identity, spacing - .05f, .15f, .10f, Color.black);
            }
            ColliderAt(line, "Batas pita", new Vector3(0, 2, 0), new Vector3(14f, 4f, .15f));
        }

        static Transform Car(Transform t, string name, float x, float z, float yaw, Material metal, Material glass)
        {
            var car = Node(t, name, x, 0, z, yaw);
            Box(car, "Body", 0, .65f, 0, 1.84f, .77f, 4.14f, metal);
            Box(car, "Cabin", 0, 1.30f, -.22f, 1.59f, .62f, 2.06f, metal);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(car, "Headlight", side * .70f, .82f, 2.085f, .35f, .17f, .05f, glass, false);
                var beacon = Node(car, side < 0 ? "Rotator biru" : "Rotator merah", side * .38f, 1.72f, -.26f)
                    .gameObject.AddComponent<Light>();
                beacon.type = LightType.Point;
                beacon.color = side < 0 ? new Color(.08f, .34f, 1f) : new Color(1f, .08f, .10f);
                beacon.range = 15.5f;
                beacon.intensity = 2.2f;
            }
            return car;
        }

        private static Transform PolicePost(Transform root, string name, float x, float z, float yaw,
            Material fence, Material flashlight, Material lens)
        {
            var post = Node(root, name, x, .12f, z, yaw);
            float center = Mathf.Sign(x) * 6.16f;
            // The solid fencing overlaps the officer's body and meets the existing curb wall.
            Box(root, name + " pagar", center, 1.11f, z, 1.69f, 2.22f, .31f, fence);
            Box(root, name + " kaki pagar", center, .24f, z, 1.69f, .48f, .60f, fence);
            Box(root, name + " pita reflektif", center, 1.31f, z - .162f, 1.68f, .105f, .026f, lens, false);
            var body = post.gameObject.AddComponent<BoxCollider>();
            body.center = new Vector3(0, 1.20f, .02f);
            body.size = new Vector3(1.02f, 2.40f, .83f);

            var art = PlaceModel("ACT_TKPSentry", "Cap brim", post);
            art.localScale = new Vector3(1.11f, x > 0 ? 1.12f : 1.20f, 1.11f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(art);
            var aim = Node(post, "Senter", -.28f, 1.60f * art.localScale.y, .69f);
            aim.localRotation = Quaternion.Euler(11.5f, 0f, 0f);
            TorchRing(aim, "Kepala senter", -.053f, .062f, .133f, flashlight);
            TorchRing(aim, "Bibir reflektor", .018f, .067f, .027f, flashlight);
            TorchRing(aim, "Lensa senter", .033f, .054f, .007f, lens);

            // Subtle cold grazing rim lamp so the towering sentry silhouette reads menacingly in the dark.
            Box(post, "Tiang lampu pos", -.64f, 1.62f, -.36f, .055f, 3.24f, .055f, fence, false);
            Box(post, "Siku lampu", -.64f, 3.22f, .46f, .055f, .055f, 1.69f, fence, false);
            Box(post, "Kap lampu pos", -.64f, 3.17f, 1.25f, .26f, .12f, .27f, flashlight, false);
            Box(post, "Kaca lampu pos", -.64f, 3.10f, 1.25f, .20f, .038f, .21f, lens, false);
            LightAt(post, "Lampu pemeriksaan", -.64f, 3.06f, 1.25f,
                new Color(.64f, .79f, .90f), 3.3f, 5.6f);
            return aim;
        }

        private static void TorchRing(Transform aim, string name, float z, float radius, float depth, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            part.name = name;
            part.transform.SetParent(aim, false);
            part.transform.localPosition = Vector3.forward * z;
            part.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            part.transform.localScale = new Vector3(radius * 2f, depth * .5f, radius * 2f);
            part.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(part.GetComponent<Collider>());
        }

        private static void AddBus(ChapterTwoStreet street, Transform root, Material trim, Material warm)
        {
            var bus = Node(root, "Bus terakhir", 2.78f, 0f, 38.62f);
            var model = PlaceModel("PROP_TKPShuttle", "Front windshield", bus);
            model.localRotation *= Quaternion.Euler(0f, 180f, 0f);
            Box(bus, "Lantai kabin", 0, .48f, 0, 2.32f, .50f, 5.78f, trim, false);
            ColliderAt(bus, "Pijakan kabin", new Vector3(0, .42f, 0), new Vector3(2.32f, .58f, 5.74f));
            ColliderAt(bus, "Dinding kiri", new Vector3(-1.1f, 1.65f, 0), new Vector3(.13f, 1.85f, 5.78f));
            ColliderAt(bus, "Dinding kanan belakang", new Vector3(1.10f, 1.65f, .73f), new Vector3(.13f, 1.85f, 4.25f));
            ColliderAt(bus, "Dinding kanan depan", new Vector3(1.10f, 1.65f, -2.58f), new Vector3(.13f, 1.85f, .53f));
            ColliderAt(bus, "Kaca depan", new Vector3(0, 1.65f, -2.80f), new Vector3(2.25f, 1.90f, .19f));
            ColliderAt(bus, "Dinding belakang", new Vector3(0, 1.65f, 2.80f), new Vector3(2.25f, 1.90f, .19f));
            ColliderAt(bus, "Atap kabin", new Vector3(0, 2.58f, 0), new Vector3(2.2f, .12f, 5.5f));

            var door = ColliderAt(bus, "Pintu bus", new Vector3(1.21f, 1.20f, -1.64f), new Vector3(.12f, 2.40f, 1.13f));
            street.BusDoor = door.gameObject.AddComponent<ChapterTwoBusDoor>();
            street.BusSeat = Node(bus, "Lorong kabin bus", 0, .734f, .05f, 180f);
            var queueLight = LightAt(root.Find("Halte"), "Lampu antre", 1.6f, 2.72f, -.98f,
                new Color(.56f, .86f, .76f), 6.2f, 8.5f);
            queueLight.type = LightType.Spot;
            queueLight.spotAngle = 102f;
            queueLight.transform.localRotation = Quaternion.LookRotation(new Vector3(.10f, -.64f, -1f));
            LightAt(bus, "Lampu kabin", 0, 2.35f, 0, new Color(.78f, .90f, .78f), 2.1f, 4.2f);
            Box(bus, "Panel rute", 0, 2.37f, -2.98f, 1.44f, .20f, .022f, trim, false);
            ChapterTwoStreetArt.Lettering(bus, "04   PASAR  -  TERMINAL", new Vector3(0, 2.37f, -3.004f),
                Quaternion.identity, 1.41f, .19f, .105f, new Color(1f, .72f, .33f));
            ChapterTwoStreetArt.Lettering(bus, "B  7104  SR", new Vector3(0, .594f, -3.082f),
                Quaternion.identity, .48f, .13f, .069f, new Color(.07f, .065f, .05f));
            foreach (float row in new[] { -.80f, .15f, 1.10f, 2.02f })
            {
                Box(bus, "Dudukan kursi", -.77f, .97f, row, .54f, .15f, .49f, trim, false);
                Box(bus, "Sandaran kursi", -.77f, 1.24f, row + .19f, .54f, .52f, .10f, trim, false);
            }
        }

        private static Transform ColliderAt(Transform parent, string name, Vector3 center, Vector3 size)
        {
            var collider = Node(parent, name, center.x, center.y, center.z).gameObject.AddComponent<BoxCollider>();
            collider.size = size;
            return collider.transform;
        }

        private static Transform PlaceModel(string model, string frontMarker, Transform parent)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Models/Environment/ChapterTwo/" + model + ".fbx");
            if (asset == null) throw new FileNotFoundException(model + ".fbx");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            foreach (var part in instance.GetComponentsInChildren<MeshRenderer>())
                if (part.name == frontMarker && parent.InverseTransformPoint(part.bounds.center).z < 0f)
                {
                    instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    break;
                }
            return instance.transform;
        }

        static void Crate(Transform t, string name, float x, float z, float w, float h, float d, Material mat)
            => Box(t, name, x, h * .5f, z, w, h, d, mat);

        static void Barrier(Transform t, float x, float z, float yaw, Material mat, Material band)
        {
            var root = Node(t, "Pembatas jalan", x, 0, z, yaw);
            // 1.04m height reliably shields a crouching player while exposing a standing head.
            Box(root, "Blok beton", 0, .52f, 0, 2.65f, 1.04f, .64f, mat);
            Box(root, "Pita polisi", 0, .82f, -.326f, 2.65f, .13f, .012f, band, false);
        }

        static Transform Node(Transform parent, string name, float x, float y, float z, float yaw = 0)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false);
            t.localPosition = new Vector3(x, y, z); t.localRotation = Quaternion.Euler(0, yaw, 0);
            return t;
        }

        static Transform Box(Transform parent, string name, float x, float y, float z, float w, float h, float d, Material mat, bool solid = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            var t = go.transform; t.SetParent(parent, false); t.localPosition = new Vector3(x, y, z); t.localScale = new Vector3(w, h, d);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!solid) Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            return t;
        }

        static Light LightAt(Transform t, string name, float x, float y, float z, Color color, float intensity, float range)
        {
            var l = Node(t, name, x, y, z).gameObject.AddComponent<Light>();
            l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = range;
            return l;
        }

        static void Sign(Transform t, string label, float x, float y, float z, float height, Color color, float yaw)
        {
            var text = Node(t, label, x, y, z, yaw).gameObject.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.fontSize = 96;
            text.characterSize = height * .12f; text.color = color; text.text = label;
        }

        static Material Material(string name, string hex, float smoothness, float emission = 0)
        {
            string path = MatDir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            mat.SetColor("_BaseColor", color); mat.SetFloat("_Smoothness", smoothness);
            if (emission > 0) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * emission); }
            EditorUtility.SetDirty(mat); return mat;
        }
    }
}
