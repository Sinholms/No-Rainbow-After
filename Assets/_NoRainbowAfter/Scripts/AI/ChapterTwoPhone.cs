using System;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoRainbowAfter.AI
{
    public sealed class ChapterTwoPhone : MonoBehaviour, IHiddenInteractable
    {
        private const string Draft = "El, maaf. Aku masih pengen ngobrol.";
        private FirstPersonPlayer player;
        private Func<bool> sendMessage;
        private Action readOldChat;
        private Canvas canvas;
        private GameObject draftPanel, replyPanel;
        private Button declineButton, closeButton;
        private Material caseMaterial, displayMaterial;
        private bool open, sent;

        public string StableId => "INT_C2_PHONE";
        public string DisplayName => "Ponsel Raka";
        public string Prompt => null;
        public void SetFocused(bool focused) { }

        public static ChapterTwoPhone Create(Transform laptop, FirstPersonPlayer owner, Func<bool> onSend, Action onRead)
        {
            var prop = new GameObject("C2 Raka Phone", typeof(BoxCollider), typeof(ChapterTwoPhone));
            prop.transform.SetParent(laptop.parent, false);
            prop.transform.SetPositionAndRotation(laptop.position + new Vector3(.30f, 0f, -.01f), Quaternion.Euler(0f, 12f, 0f));
            var collider = prop.GetComponent<BoxCollider>();
            collider.center = Vector3.up * .014f;
            collider.size = new Vector3(.12f, .03f, .18f);
            var phone = prop.GetComponent<ChapterTwoPhone>();
            phone.player = owner;
            phone.sendMessage = onSend;
            phone.readOldChat = onRead;
            phone.BuildProps();
            return phone;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (open || player.IsPaused || player.IsInspecting || player.IsTransitionLocked || player.IsUsingLaptop) return;
            if (canvas == null) BuildUi();
            open = true;
            player.SetInspecting(true);
            player.CloseInspectRequested += Close;
            readOldChat?.Invoke();
            RefreshUi();
        }

        public void ResetMessage() => sent = false;

        public bool TrySendMessage()
        {
            if (!open || sent || player.IsPaused || !sendMessage()) return false;
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
            Destroy(caseMaterial);
            Destroy(displayMaterial);
        }

        private void RefreshUi()
        {
            canvas.enabled = open && !player.IsPaused;
            draftPanel.SetActive(!sent);
            replyPanel.SetActive(sent);
            if (canvas.enabled && EventSystem.current != null)
            {
                var target = !sent ? declineButton : closeButton;
                if (target != null) EventSystem.current.SetSelectedGameObject(target.gameObject);
            }
        }

        private void BuildUi()
        {
            var root = new GameObject("Ella Chat", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
            Label(header, "Contact Name", "Ella", new Vector2(-132f, 0f), new Vector2(440f, 55f), 34, TextAnchor.MiddleLeft);
            closeButton = Button(header, "Close", "Tutup [F]", new Vector2(264f, 0f), new Vector2(162f, 48f), Close);

            Message(frame, "Ella  ·  Semester 5", "Kalau tugasnya numpuk, besok kita lihat catatannya bareng.", 216f);
            Message(frame, "Ella  ·  Semester 5", "Jangan lupa. Kita wisuda bareng, ya.", 94f);

            var draft = Panel(frame, "Draft", new Vector2(0f, -227f), new Vector2(704f, 214f), new Color(.09f, .14f, .16f));
            draftPanel = draft.gameObject;
            Label(draft, "Draft Text", Draft, new Vector2(0f, 30f), new Vector2(642f, 94f), 28, TextAnchor.MiddleLeft);
            declineButton = Button(draft, "DoNotSend", "Jangan kirim", new Vector2(-110f, -60f), new Vector2(240f, 54f), Close);
            declineButton.image.color = new Color(.13f, .19f, .22f);
            Button(draft, "Send", "Kirim", new Vector2(225f, -60f), new Vector2(224f, 54f), () => TrySendMessage());

            var reply = Rect(frame, "New Messages", Vector2.zero, new Vector2(776f, 744f));
            replyPanel = reply.gameObject;
            Message(reply, "Raka  ·  ✓", Draft, -137f, outgoing: true);
        }

        private static void Message(Transform parent, string name, string message, float y, bool outgoing = false)
        {
            var bubble = Panel(parent, name, new Vector2(outgoing ? 53f : -53f, y), new Vector2(598f, 102f),
                outgoing ? new Color(.115f, .223f, .22f) : new Color(.105f, .143f, .169f));
            var time = Label(bubble, "Sender", name, new Vector2(0f, 32f), new Vector2(550f, 28f), 19, TextAnchor.MiddleLeft);
            time.color = new Color(.61f, .75f, .77f);
            Label(bubble, "Message", message, new Vector2(0f, -12f), new Vector2(550f, 60f), 24, TextAnchor.MiddleLeft);
        }

        private static Button Button(Transform parent, string name, string title, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction click)
        {
            var panel = Panel(parent, name, position, size, new Color(.19f, .37f, .36f));
            var image = panel.GetComponent<Image>();
            image.raycastTarget = true;
            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(click);
            Label(panel, "Label", title, Vector2.zero, size, 22, TextAnchor.MiddleCenter);
            return button;
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

        private void BuildProps()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            caseMaterial = new Material(shader) { color = new Color(.025f, .031f, .043f) };
            displayMaterial = new Material(shader) { color = new Color(.20f, .39f, .46f) };
            displayMaterial.EnableKeyword("_EMISSION");
            displayMaterial.SetColor("_EmissionColor", new Color(.10f, .33f, .38f));
            PhonePiece("Case", new Vector3(0f, .008f, 0f), new Vector3(.083f, .016f, .156f), caseMaterial);
            PhonePiece("Screen", new Vector3(0f, .017f, 0f), new Vector3(.075f, .004f, .142f), displayMaterial);

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

    }
}
