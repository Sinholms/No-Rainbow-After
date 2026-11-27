using NoRainbowAfter.Interaction;
using NoRainbowAfter.Narrative;
using UnityEngine;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Pembuatan properti cerita kamar kos (foto wisuda & galon air) serta registrasi clue Chapter 2.
    ///
    /// [GRAFIKA KOMPUTER]
    /// CreateGraduationPhoto() merakit bingkai kayu, kaki, tepi, dan cetakan foto dari primitif 3D,
    /// lalu mengonfigurasi shader "Universal Render Pipeline/Lit" dengan Texture2D pada _BaseMap
    /// dan _EmissionMap agar foto tetap terbaca meski berada di bawah lampu meja yang hangat.
    /// </summary>
    public sealed partial class ChapterTwoFlow
    {
        private void CreateGraduationPhoto()
        {
            var portrait = Resources.Load<Texture2D>("ChapterTwoArt/T_RakaEllaPhoto");
            if (portrait == null)
            {
                Debug.LogError("[NRA] Missing the Raka and Ella photograph.", this);
                return;
            }

            var photo = new GameObject("Foto Raka dan Ella", typeof(BoxCollider), typeof(SceneInteractable));
            photo.transform.SetParent(laptop.transform.parent, false);

            // Di kanan ponsel, di belakang catatan terbuka, tepat di bawah cahaya lampu meja belajar.
            photo.transform.SetPositionAndRotation(
                laptop.transform.position + new Vector3(.58f, .109f, .235f),
                Quaternion.Euler(14f, -10f, 0f));
            photo.GetComponent<BoxCollider>().size = new Vector3(.218f, .222f, .055f);

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var frame = new Material(shader) { color = new Color(.13f, .078f, .049f) };
            var border = new Material(shader) { color = new Color(.88f, .81f, .67f) };
            var print = new Material(shader);
            print.SetTexture("_BaseMap", portrait);

            // Kertas matte dengan emisi tipis mencegah gambar washed-out di bawah lampu meja yang terang.
            print.SetColor("_BaseColor", new Color(.31f, .34f, .38f));
            print.SetTexture("_EmissionMap", portrait);
            print.SetColor("_EmissionColor", new Color(.12f, .12f, .12f));
            print.SetFloat("_Smoothness", .08f);
            print.EnableKeyword("_EMISSION");
            photoMaterials = new[] { frame, border, print };

            PhotoPiece(photo.transform, "Bingkai kayu", PrimitiveType.Cube, Vector3.zero,
                new Vector3(.218f, .222f, .022f), frame);
            PhotoPiece(photo.transform, "Kaki bingkai", PrimitiveType.Cube, new Vector3(0, -.104f, .029f),
                new Vector3(.166f, .013f, .071f), frame);
            PhotoPiece(photo.transform, "Tepi foto", PrimitiveType.Cube, new Vector3(0, 0, -.0135f),
                new Vector3(.194f, .198f, .004f), border);
            PhotoPiece(photo.transform, "Cetakan Raka dan Ella", PrimitiveType.Quad, new Vector3(0, .003f, -.016f),
                new Vector3(.178f, .178f, 1f), print);

            photo.GetComponent<SceneInteractable>().Configure(
                "INT_C2_PHOTO",
                "Foto Raka dan Ella",
                "Periksa foto",
                "c2.photo",
                "Foto Raka dan Ella",
                "Di belakang foto ada tulisan tangan Ella: “Wisuda bareng, ya.”",
                false,
                portrait);
        }

        private static void PhotoPiece(Transform parent, string name, PrimitiveType shape, Vector3 position, Vector3 scale, Material material)
        {
            var piece = GameObject.CreatePrimitive(shape);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = scale;

            var collider = piece.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            piece.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void SetupWaterGallon()
        {
            var jug = GameObject.Find("SM_T07_WaterJugBody");
            if (jug == null)
            {
                return;
            }
            if (jug.GetComponent<Collider>() == null)
            {
                addedGallonCollider = jug.AddComponent<BoxCollider>();
                var mf = jug.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    var b = mf.sharedMesh.bounds;
                    addedGallonCollider.center = b.center + new Vector3(0f, 0.05f, 0f);
                    addedGallonCollider.size = new Vector3(Mathf.Max(b.size.x, 0.28f), b.size.y + 0.14f, Mathf.Max(b.size.z, 0.28f));
                }
                else
                {
                    addedGallonCollider.center = new Vector3(0f, 0.06f, 0f);
                    addedGallonCollider.size = new Vector3(0.32f, 0.48f, 0.32f);
                }
            }

            waterGallon = jug.GetComponent<SceneInteractable>() ?? jug.AddComponent<SceneInteractable>();
            waterGallon.Configure(
                "INT_C2_GALLON",
                "Galon air",
                "Periksa galon",
                "c2.gallon",
                "Galon air",
                "Air galon biasa. Masih tersisa lebih dari setengah di sudut kamar.",
                false);
        }

        private void RegisterOptionalClue(string id)
        {
            if (id == "INT_C2_PHOTO")
            {
                director.State.AddClue("CLUE_01");
            }
            if (id == "INT_C2_BAG")
            {
                director.State.AddClue("CLUE_04");
            }
            if (id == "INT_C2_GALLON" && !transitioning && Beat == ChapterTwoBeat.WokeInDorm)
            {
                laptop?.SkipEssay();
                pendingGallonSkip = true;
            }
        }
    }
}
