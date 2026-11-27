using System.Collections;
using NoRainbowAfter.Player;
using UnityEngine;
using UnityEngine.UI;

namespace NoRainbowAfter.Narrative
{
    /// <summary>
    /// Saat Raka pertama kali kembali dari lorong di Chapter 1, event LoopChanged memicu
    /// perubahan suasana kamar: layar digelapkan sejenak untuk menyembunyikan pergantian perabot dan cahaya.
    ///
    /// [GRAFIKA KOMPUTER]
    /// Mengubah warna dan intensitas lampu ruangan &amp; lampu meja belajar menjadi lebih dingin dan redup
    /// seiring meningkatnya ketegangan atmosfer kos.
    /// </summary>
    public sealed class ChapterOneRoomShift : MonoBehaviour
    {
        [SerializeField] private ChapterDirector director;
        [SerializeField] private FirstPersonPlayer player;
        [SerializeField] private GameObject disarray;
        [SerializeField] private GameObject[] normalChair;
        [SerializeField] private Light roomLight;
        [SerializeField] private Light deskLight;

        private bool shifted;
        public bool HasShifted => shifted;

        public void Configure(ChapterDirector source, FirstPersonPlayer actor, GameObject dressing,
            GameObject[] chair, Light room, Light desk)
        {
            director = source;
            player = actor;
            disarray = dressing;
            normalChair = chair;
            roomLight = room;
            deskLight = desk;
        }

        private void OnEnable()
        {
            if (director != null)
            {
                director.LoopChanged += OnLoop;
            }
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.LoopChanged -= OnLoop;
            }
        }

        private void Start()
        {
            if (director != null && director.LoopIndex > 0)
            {
                OnLoop(director.LoopIndex);
            }
        }

        private void OnLoop(int loop)
        {
            if (loop < 1 || shifted)
            {
                return;
            }
            shifted = true;
            StartCoroutine(Shift());
        }

        private IEnumerator Shift()
        {
            var root = new GameObject("Room memory blackout", typeof(Canvas));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 35;

            var panel = new GameObject("Fade", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = panel.GetComponent<Image>();
            image.raycastTarget = false;

            for (float t = 0; t < .22f; t += Time.deltaTime)
            {
                image.color = new Color(0, 0, 0, t / .22f);
                yield return null;
            }
            image.color = Color.black;

            if (disarray != null) disarray.SetActive(true);
            if (normalChair != null)
            {
                foreach (var part in normalChair)
                {
                    if (part != null)
                    {
                        part.SetActive(false);
                    }
                }
            }
            if (roomLight != null)
            {
                roomLight.color = new Color(.42f, .64f, .68f);
                roomLight.intensity *= .52f;
            }
            if (deskLight != null)
            {
                deskLight.color = new Color(1f, .47f, .23f);
                deskLight.intensity *= .78f;
            }

            Physics.SyncTransforms();
            yield return new WaitForSeconds(.16f);

            for (float t = 0; t < .6f; t += Time.deltaTime)
            {
                image.color = new Color(0, 0, 0, 1 - t / .6f);
                yield return null;
            }
            Destroy(root);
        }
    }
}
