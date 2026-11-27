using NoRainbowAfter.AI;
using UnityEditor;
using UnityEngine;

namespace NoRainbowAfter.Editor
{
    /// <summary>
    /// Dekorasi TKP, pita polisi, mobil patroli, pos polisi, dan bus terakhir pada ChapterTwoStreetBuilder.
    ///
    /// [GRAFIKA KOMPUTER]
    /// Merakit objek prop multi-bagian menggunakan hierarki Transform, silinder & kotak primitif,
    /// Point/Spot Light rotator & pos, serta teks rute World-Space di kaca bus.
    /// </summary>
    public static partial class ChapterTwoStreetBuilder
    {
        // =========================================================================
        // Dekorasi TKP & Batas Pita Garis Polisi (Cordon)
        // =========================================================================

        private static void AddCrimeSceneDressing(Transform ground, Material coneOrange, Material reflective,
            Material timber, Material trashBag, Material tape, Material metal)
        {
            var dressing = Node(ground, "Dekorasi TKP", 0, 0, 0);

            // Kerucut lalu lintas polisi (tanpa collider) di sekitar mobil patroli & ujung jalan.
            var conePositions = new[]
            {
                new Vector3(3.05f, 0f, 4.95f),
                new Vector3(2.85f, 0f, 9.35f),
                new Vector3(-3.35f, 0f, 44.85f),
                new Vector3(3.15f, 0f, 44.85f)
            };
            for (int i = 0; i < conePositions.Length; i++)
            {
                HazardCone(dressing, "Kerucut TKP " + (i + 1), conePositions[i], coneOrange, reflective, metal);
            }

            // Peti pasar terguling & kantong sampah basah rapat ke trotoar (bebas collider agar pemain tidak tersangkut).
            var spilled1 = Node(dressing, "Peti tumpah kiri", -5.25f, .15f, 8.5f, 24f);
            Box(spilled1, "Peti miring", 0, .24f, 0, .62f, .44f, .86f, timber, false).localRotation = Quaternion.Euler(12f, 18f, -34f);
            Box(spilled1, "Kantong sampah A", .32f, .20f, .62f, .46f, .40f, .48f, trashBag, false).localRotation = Quaternion.Euler(0, 32f, 8f);
            Box(spilled1, "Kantong sampah B", -.18f, .17f, .78f, .38f, .34f, .42f, trashBag, false).localRotation = Quaternion.Euler(6f, -15f, -5f);

            var spilled2 = Node(dressing, "Sampah trotoar kanan", 5.45f, .15f, 24.4f, -12f);
            Box(spilled2, "Kardus basah", 0, .03f, 0, .78f, .05f, .92f, timber, false).localRotation = Quaternion.Euler(0, 19f, 0);
            Box(spilled2, "Kantong sampah C", .18f, .22f, -.52f, .50f, .44f, .52f, trashBag, false);
            Box(spilled2, "Peti pecah", -.24f, .19f, .55f, .52f, .38f, .64f, timber, false).localRotation = Quaternion.Euler(-8f, 28f, 22f);

            // Pita polisi pendek di samping mobil patroli.
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

        private static void AddCordon(Transform ground, Material tape, Material steel, Material concrete)
        {
            var line = Node(ground, "Garis polisi jalan raya", 0, 0, CordonZ);
            const float spacing = 3.45f;

            for (int i = -2; i <= 2; i++)
            {
                float x = i * spacing;
                float height = i % 2 == 0 ? 1.22f : 1.10f;
                Box(line, "Kaki tiang", x, .13f, 0, .34f, .26f, .34f, concrete, false);
                Box(line, "Tiang pengikat", x, height * .5f, 0, .055f, height + .12f, .055f, steel, false);
                if (i == 2)
                {
                    break;
                }

                float rise = i % 2 == 0 ? -.12f : .12f;
                var span = Node(line, "Rentang pita", x + spacing * .5f, height + rise * .5f, 0);
                span.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(rise, spacing) * Mathf.Rad2Deg);
                Box(span, "Pita kuning", 0, 0, 0, spacing + .012f, .15f, .018f, tape, false);
                ChapterTwoStreetArt.Lettering(
                    span,
                    "///   GARIS POLISI   ///   DILARANG MELINTAS   ///",
                    new Vector3(0, 0, -.013f),
                    Quaternion.identity,
                    spacing - .05f,
                    .15f,
                    .10f,
                    Color.black);
            }

            ColliderAt(line, "Batas pita", new Vector3(0, 2, 0), new Vector3(14f, 4f, .15f));
        }

        // =========================================================================
        // Mobil Patroli, Pos Polisi (Emitter Senter), & Bus Terakhir
        // =========================================================================

        private static Transform Car(Transform t, string name, float x, float z, float yaw, Material metal, Material glass)
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

            // Pagar solid menutup celah belakang penjaga dan bertemu langsung dengan dinding trotoar.
            Box(root, name + " pagar", center, 1.11f, z, 1.69f, 2.22f, .31f, fence);
            Box(root, name + " kaki pagar", center, .24f, z, 1.69f, .48f, .60f, fence);
            Box(root, name + " pita reflektif", center, 1.31f, z - .162f, 1.68f, .105f, .026f, lens, false);

            var body = post.gameObject.AddComponent<BoxCollider>();
            body.center = new Vector3(0, 1.20f, .02f);
            body.size = new Vector3(1.02f, 2.40f, .83f);

            var art = PlaceModel("ACT_TKPSentry", "Cap brim", post);
            art.localScale = new Vector3(1.11f, x > 0 ? 1.12f : 1.20f, 1.11f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(art);

            // Node Senter miring ke bawah 11.5 derajat — menjadi Transform emitter untuk SpotlightEncounter!
            var aim = Node(post, "Senter", -.28f, 1.60f * art.localScale.y, .69f);
            aim.localRotation = Quaternion.Euler(11.5f, 0f, 0f);
            TorchRing(aim, "Kepala senter", -.053f, .062f, .133f, flashlight);
            TorchRing(aim, "Bibir reflektor", .018f, .067f, .027f, flashlight);
            TorchRing(aim, "Lensa senter", .033f, .054f, .007f, lens);

            // Lampu pos pemeriksaan (rim light dingin) agar siluet penjaga tetap terbaca dalam gelap.
            Box(post, "Tiang lampu pos", -.64f, 1.62f, -.36f, .055f, 3.24f, .055f, fence, false);
            Box(post, "Siku lampu", -.64f, 3.22f, .46f, .055f, .055f, 1.69f, fence, false);
            Box(post, "Kap lampu pos", -.64f, 3.17f, 1.25f, .26f, .12f, .27f, flashlight, false);
            Box(post, "Kaca lampu pos", -.64f, 3.10f, 1.25f, .20f, .038f, .21f, lens, false);
            LightAt(post, "Lampu pemeriksaan", -.64f, 3.06f, 1.25f, new Color(.64f, .79f, .90f), 3.3f, 5.6f);
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
            var bus = Node(root, "Bus terakhir", 2.78f, 0f, 37.80f);
            PlaceModel("PROP_TKPShuttle", "Front windshield", bus);
            Box(bus, "Lantai kabin", 0, .48f, 0, 2.32f, .50f, 5.78f, trim, false);

            ColliderAt(bus, "Pijakan kabin", new Vector3(0, .42f, 0), new Vector3(2.32f, .58f, 5.74f));
            ColliderAt(bus, "Dinding kiri", new Vector3(-1.1f, 1.65f, 0), new Vector3(.13f, 1.85f, 5.78f));
            ColliderAt(bus, "Dinding kanan belakang", new Vector3(1.10f, 1.65f, -.73f), new Vector3(.13f, 1.85f, 4.25f));
            ColliderAt(bus, "Dinding kanan depan", new Vector3(1.10f, 1.65f, 2.58f), new Vector3(.13f, 1.85f, .53f));
            ColliderAt(bus, "Kaca depan", new Vector3(0, 1.65f, 2.80f), new Vector3(2.25f, 1.90f, .19f));
            ColliderAt(bus, "Dinding belakang", new Vector3(0, 1.65f, -2.80f), new Vector3(2.25f, 1.90f, .19f));
            ColliderAt(bus, "Atap kabin", new Vector3(0, 2.58f, 0), new Vector3(2.2f, .12f, 5.5f));

            var door = ColliderAt(bus, "Pintu bus", new Vector3(1.21f, 1.20f, 1.64f), new Vector3(.12f, 2.40f, 1.13f));
            street.BusDoor = door.gameObject.AddComponent<ChapterTwoBusDoor>();
            street.BusSeat = Node(bus, "Lorong kabin bus", 0, .734f, -.05f, 0f);

            var queueLight = LightAt(root.Find("Halte"), "Lampu antre", 1.6f, 2.72f, -.98f,
                new Color(.56f, .86f, .76f), 2.4f, 6.2f);
            queueLight.type = LightType.Spot;
            queueLight.spotAngle = 102f;
            queueLight.transform.localRotation = Quaternion.LookRotation(new Vector3(.10f, -.64f, -1f));
            LightAt(bus, "Lampu kabin", 0, 2.35f, 0, new Color(.78f, .90f, .78f), 1.8f, 4.0f);

            Box(bus, "Panel rute", 0, 2.37f, 2.98f, 1.44f, .20f, .022f, trim, false);
            ChapterTwoStreetArt.Lettering(
                bus,
                "04   PASAR  -  TERMINAL",
                new Vector3(0, 2.37f, 3.004f),
                Quaternion.Euler(0f, 180f, 0f),
                1.41f,
                .19f,
                .105f,
                new Color(1f, .72f, .33f));
            ChapterTwoStreetArt.Lettering(
                bus,
                "B  7104  SR",
                new Vector3(0, .594f, 3.082f),
                Quaternion.Euler(0f, 180f, 0f),
                .48f,
                .13f,
                .069f,
                new Color(.07f, .065f, .05f));

            foreach (float row in new[] { .80f, -.15f, -1.10f, -2.02f })
            {
                Box(bus, "Dudukan kursi", -.77f, .97f, row, .54f, .15f, .49f, trim, false);
                Box(bus, "Sandaran kursi", -.77f, 1.24f, row - .19f, .54f, .52f, .10f, trim, false);
            }
        }
    }
}
