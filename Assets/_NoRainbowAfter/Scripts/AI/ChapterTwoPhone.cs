using System;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Player;
using UnityEngine;
using UnityEngine.UI;

namespace NoRainbowAfter.AI
{
    public sealed class ChapterTwoPhone : MonoBehaviour, IInteractable
    {
        private const string Draft = "Besok bisa ketemu? Ada yang mau gue ceritain soal kecelakaan itu.";
        private const string Meeting = "Bisa. Besok habis kelas, gue tunggu di depan lab grafkom.";
        private FirstPersonPlayer player;
        private Func<bool> sendReply;
        private Canvas canvas;
        private GameObject draftPanel, replyPanel;
        private Mesh starMesh;
        private Material caseMaterial, displayMaterial, reflectorMaterial;
        private bool open, replyAvailable, sent;

        public string StableId => "INT_C2_PHONE";
        public string DisplayName => "Ponsel Raka";
        public string Prompt => "Buka pesan Bayu";
        public void SetFocused(bool focused) { }

        public static ChapterTwoPhone Create(Transform laptop, FirstPersonPlayer owner, Func<bool> onSend)
        {
            var prop = new GameObject("C2 Raka Phone", typeof(BoxCollider), typeof(ChapterTwoPhone));
            prop.transform.SetParent(laptop.parent, false);
            prop.transform.SetPositionAndRotation(laptop.position + new Vector3(.28f, .02f, -.06f), Quaternion.Euler(0f, 12f, 0f));
            var collider = prop.GetComponent<BoxCollider>();
            collider.center = Vector3.up * .014f;
            collider.size = new Vector3(.12f, .03f, .18f);
            var phone = prop.GetComponent<ChapterTwoPhone>();
            phone.player = owner;
            phone.sendReply = onSend;
            phone.BuildProps(laptop);
            return phone;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (open || player.IsPaused || player.IsInspecting || player.IsTransitionLocked || player.IsUsingLaptop) return;
            if (canvas == null) BuildUi();
            open = true;
            player.SetInspecting(true);
            player.CloseInspectRequested += Close;
            RefreshUi();
        }

        public void SetReplyAvailable() => replyAvailable = true;

        public bool TrySendReply()
        {
            if (!open || !replyAvailable || sent || player.IsPaused || !sendReply()) return false;
            sent = true;
            RefreshUi();
            return true;
        }

        private void Update()
        {
            if (open) canvas.enabled = !player.IsPaused;
        }

        private void Close()
        {
            if (player.IsPaused) return;
            Dismiss();
        }

        private void Dismiss()
        {
            if (!open) return;
            open = false;
            if (player != null)
            {
                player.CloseInspectRequested -= Close;
                player.SetInspecting(false);
            }
            if (canvas != null) canvas.enabled = false;
        }

        private void OnDisable() => Dismiss();
        private void OnDestroy()
        {
            Destroy(starMesh);
            Destroy(caseMaterial);
            Destroy(displayMaterial);
            Destroy(reflectorMaterial);
        }

        private void RefreshUi()
        {
            canvas.enabled = open && !player.IsPaused;
            draftPanel.SetActive(replyAvailable && !sent);
            replyPanel.SetActive(sent);
        }

        private void BuildUi()
        {
            var root = new GameObject("Bayu Chat", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 28;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;

            var backdrop = Panel(root.transform, "Backdrop", Vector2.zero, Vector2.zero, new Color(.018f, .024f, .033f, .96f));
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            var frame = Panel(backdrop, "Messages", Vector2.zero, new Vector2(776f, 744f), new Color(.056f, .076f, .091f));
            var header = Panel(frame, "Contact", new Vector2(0f, 327f), new Vector2(776f, 90f), new Color(.09f, .13f, .16f));
            Label(header, "Contact Name", "Bayu Kopling", new Vector2(-132f, 0f), new Vector2(440f, 55f), 34, TextAnchor.MiddleLeft);
            Button(header, "Close", "Tutup [F]", new Vector2(264f, 0f), new Vector2(162f, 48f), Close);

            Message(frame, "Tiga hari lalu", "Ka, lu enggak masuk grafkom? Pak dosen nyariin.", 229f);
            Message(frame, "Kemarin", "Catatan kelas udah gue simpan. Nanti gue kirim.", 120f);
            Message(frame, "Hari ini", "Ka, lu gimana? Kalau mau ngobrol, kabarin gue.", 11f);

            var draft = Panel(frame, "Draft", new Vector2(0f, -227f), new Vector2(704f, 214f), new Color(.09f, .14f, .16f));
            draftPanel = draft.gameObject;
            Label(draft, "Draft Text", Draft, new Vector2(0f, 30f), new Vector2(642f, 94f), 28, TextAnchor.MiddleLeft);
            Button(draft, "Send", "Kirim balasan", new Vector2(225f, -60f), new Vector2(224f, 54f), () => TrySendReply());

            var reply = Rect(frame, "New Messages", Vector2.zero, new Vector2(776f, 744f));
            replyPanel = reply.gameObject;
            Message(reply, "Raka", Draft, -137f, outgoing: true);
            Message(reply, "Bayu", Meeting, -266f);
        }

        private static void Message(Transform parent, string name, string message, float y, bool outgoing = false)
        {
            var bubble = Panel(parent, name, new Vector2(outgoing ? 53f : -53f, y), new Vector2(598f, 102f),
                outgoing ? new Color(.115f, .223f, .22f) : new Color(.105f, .143f, .169f));
            var time = Label(bubble, "Sender", name, new Vector2(0f, 32f), new Vector2(550f, 28f), 19, TextAnchor.MiddleLeft);
            time.color = new Color(.61f, .75f, .77f);
            Label(bubble, "Message", message, new Vector2(0f, -12f), new Vector2(550f, 60f), 24, TextAnchor.MiddleLeft);
        }

        private static void Button(Transform parent, string name, string title, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction click)
        {
            var panel = Panel(parent, name, position, size, new Color(.19f, .37f, .36f));
            var image = panel.GetComponent<Image>();
            image.raycastTarget = true;
            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(click);
            Label(panel, "Label", title, Vector2.zero, size, 22, TextAnchor.MiddleCenter);
        }

        private static Text Label(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
        {
            var text = Rect(parent, name, position, size).gameObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = new Color(.93f, .95f, .96f);
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform Panel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var rect = Rect(parent, name, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = Vector2.one * .5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private void BuildProps(Transform laptop)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            caseMaterial = new Material(shader) { color = new Color(.025f, .031f, .043f) };
            displayMaterial = new Material(shader) { color = new Color(.20f, .39f, .46f) };
            displayMaterial.EnableKeyword("_EMISSION");
            displayMaterial.SetColor("_EmissionColor", new Color(.10f, .33f, .38f));
            PhonePiece("Case", new Vector3(0f, .008f, 0f), new Vector3(.083f, .016f, .156f), caseMaterial);
            PhonePiece("Screen", new Vector3(0f, .017f, 0f), new Vector3(.075f, .004f, .142f), displayMaterial);

            var reflector = new GameObject("C2 Star Reflector", typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider), typeof(SceneInteractable));
            reflector.transform.SetParent(laptop.parent, false);
            reflector.transform.position = laptop.position + new Vector3(.45f, .032f, -.10f);
            var collider = reflector.GetComponent<BoxCollider>();
            collider.size = new Vector3(.105f, .024f, .105f);
            reflectorMaterial = new Material(shader) { color = new Color(.83f, .68f, .25f) };
            reflector.GetComponent<MeshRenderer>().sharedMaterial = reflectorMaterial;
            starMesh = BuildStar();
            reflector.GetComponent<MeshFilter>().sharedMesh = starMesh;
            reflector.GetComponent<SceneInteractable>().Configure("INT_C2_REFLECTOR", "Reflektor bintang", "Periksa reflektor",
                "inspected.c2.reflector", "Gantungan reflektor", "Satu masih di sini. Pasangannya masih menggantung di ritsleting tas kuliahku.", false);
        }

        private void PhonePiece(string name, Vector3 position, Vector3 scale, Material material)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(transform, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = scale;
            var collider = piece.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            piece.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Mesh BuildStar()
        {
            var vertices = new Vector3[11];
            var triangles = new int[30];
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.PI * i / 5f;
                float radius = i % 2 == 0 ? .038f : .017f;
                vertices[i + 1] = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % 10 + 1;
            }
            var mesh = new Mesh { name = "Star Reflector", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
