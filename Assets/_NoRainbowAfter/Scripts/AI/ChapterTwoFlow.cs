using System;
using System.Collections;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Level;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using UnityEngine;
using UnityEngine.UI;

namespace NoRainbowAfter.AI
{
    public sealed class ChapterTwoFlow : MonoBehaviour
    {
        private const string BagId = "INT_C2_BAG";
        private static readonly Vector3 BedsidePose = new Vector3(.12f, .04f, 1.66f);
        private static readonly Quaternion RoomFacing = Quaternion.Euler(0f, 18f, 0f);
        private static readonly string[] Report =
        {
            "kecelakaan terjadi dekat halte di jalan kampus tadi malam.",
            "seorang pemuda dan seorang pemudi terlibat dalam kejadian itu.",
            "polisi memasang garis pembatas saat memeriksa lokasi.",
            "kondisi kedua orang itu belum dikonfirmasi."
        };

        private FirstPersonPlayer player;
        private FirstPersonMotor motor;
        private PlayerInteractor interactor;
        private ChapterDirector director;
        private DormCheckpoint checkpoint;
        private StressBarometer stress;
        private BreathingMinigame breathing;
        private SubtitlePlaceholder subtitles;
        private LaptopTypingMinigame laptop;
        private GameObject dorm, roomPostProcessing;
        private ChapterTwoPhone phone;
        private ChapterTwoStreet street;
        private SceneInteractable bag;
        private Canvas transitionCanvas;
        private Image fadeImage;
        private bool transitioning, wasStressEnabled, wasBreathingEnabled, wasRoomPostProcessingActive;

        private ChapterTwoBeat Beat => director.State.Chapter2Beat;

        private void Awake()
        {
            player = GetComponent<FirstPersonPlayer>();
            motor = GetComponent<FirstPersonMotor>();
            interactor = GetComponent<PlayerInteractor>();
            director = GetComponent<ChapterDirector>();
            checkpoint = GetComponent<DormCheckpoint>();
            stress = GetComponent<StressBarometer>();
            breathing = GetComponent<BreathingMinigame>();
            subtitles = GetComponent<SubtitlePlaceholder>();
        }

        private void OnDestroy()
        {
            if (checkpoint != null) checkpoint.SetChapterRestore(null);
            if (interactor != null) interactor.InteractionCompleted -= OnInteraction;
            if (laptop != null) laptop.ReportContinueRequested -= OnReportContinue;
            if (street != null)
            {
                street.SafeZoneReached -= OnSafeZoneReached;
                Destroy(street.gameObject);
            }
        }

        public IEnumerator WakeFromChapterOne(Action leaveCorridor)
        {
            if (transitioning || director.ChapterOneFinished) yield break;
            var laptopObject = GameObject.Find("PROP_RakaLaptop");
            laptop = laptopObject != null ? laptopObject.GetComponent<LaptopTypingMinigame>() : null;
            if (laptop == null || laptop.transform.parent == null)
            {
                Debug.LogError("[NRA] Chapter 2 needs the dorm laptop.", this);
                yield break;
            }

            dorm = laptop.transform.parent.gameObject;
            roomPostProcessing = GameObject.Find("Room Post Processing");
            checkpoint?.SetChapterRestore(RestoreCheckpoint);
            SetTransition(true);
            yield return Fade(0f, 1f, .35f);
            if (!RestoreDorm())
            {
                Debug.LogWarning("[NRA] Chapter 2 could not restore the dorm pose.", this);
                checkpoint?.SetChapterRestore(null);
                yield return Fade(1f, 0f, .3f);
                SetTransition(false);
                yield break;
            }

            leaveCorridor();
            director.FinishChapterOne();
            subtitles?.ClearQueuedCues();
            stress?.ResetToInitial();
            phone = ChapterTwoPhone.Create(laptop.transform, player, SendBayuReply);
            laptop.ConfigureReport(Report);
            laptop.ReportContinueRequested += OnReportContinue;
            interactor.InteractionCompleted += OnInteraction;
            Physics.SyncTransforms();

            var camera = motor.ViewCamera.transform;
            var standing = new Pose(camera.position, camera.rotation);
            var pillow = GameObject.Find("SM_PROP_SoftPillow");
            var pillowRenderer = pillow != null ? pillow.GetComponent<Renderer>() : null;
            Vector3 pillowCenter = pillowRenderer != null ? pillowRenderer.bounds.center : new Vector3(-1.27f, .67f, 2.47f);
            var lying = new Pose(pillowCenter + Vector3.up * .18f, Quaternion.Euler(-58f, 110f, 30f));
            camera.SetPositionAndRotation(lying.position, lying.rotation);
            yield return Fade(1f, 0f, 1.05f, progress =>
            {
                float sitUp = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.27f, 1f, progress));
                camera.SetPositionAndRotation(Vector3.Lerp(lying.position, standing.position, sitUp),
                    Quaternion.Slerp(lying.rotation, standing.rotation, sitUp));
            });
            SetTransition(false);
            director.AdvanceChapterTwo(ChapterTwoBeat.WokeInDorm);
            director.RequestNarrativeCue(new NarrativeCue("Raka", "Sudah berapa hari aku enggak buka pesan? Tugas grafkom juga belum terkirim.", 4f, "c2_wake"));
        }

        private void OnReportContinue()
        {
            if (transitioning || !laptop.IsReportComplete) return;
            if (Beat == ChapterTwoBeat.WokeInDorm) director.AdvanceChapterTwo(ChapterTwoBeat.ReportSubmitted);
            if (Beat == ChapterTwoBeat.ReportSubmitted) StartCoroutine(EnterStreet());
        }

        private IEnumerator EnterStreet()
        {
            SetTransition(true);
            laptop.SetReportTransitionRetry(false);
            if (stress != null && stress.CurrentStress < StressBarometer.MaxStress) stress.SetStress(StressBarometer.MaxStress);
            subtitles?.ClearQueuedCues();
            yield return Fade(0f, 0f, .60f);
            yield return Fade(0f, 1f, .34f);

            if (street == null)
            {
                var prefab = Resources.Load<GameObject>("ChapterTwoStreet");
                if (prefab != null)
                {
                    var streetObject = Instantiate(prefab);
                    street = streetObject.GetComponent<ChapterTwoStreet>();
                    if (street == null) Destroy(streetObject);
                    else
                    {
                        street.SafeZoneReached += OnSafeZoneReached;
                        bag = street.Bag;
                    }
                }
            }

            if (street == null || bag == null || !street.Enter(motor))
            {
                if (street != null) street.Leave();
                Debug.LogWarning("[NRA] Chapter 2 street could not be entered.", this);
                laptop.SetReportTransitionRetry(true);
                yield return Fade(1f, 0f, .4f);
                SetTransition(false);
                yield break;
            }

            SuspendDorm();
            director.AdvanceChapterTwo(ChapterTwoBeat.StreetEntered);
            yield return Fade(1f, 0f, .65f);
            SetTransition(false);
        }

        private void OnSafeZoneReached()
        {
            if (director.AdvanceChapterTwo(ChapterTwoBeat.ShelterReached)) bag.SetAvailable(true);
        }

        private void OnInteraction(string id)
        {
            if (id == BagId && Beat == ChapterTwoBeat.ShelterReached)
                director.AdvanceChapterTwo(ChapterTwoBeat.BagInspected);
        }

        private void Update()
        {
            if (director.State != null && Beat == ChapterTwoBeat.BagInspected && !transitioning &&
                !player.IsPaused && !player.IsInspecting) StartCoroutine(ReturnToDorm());
        }

        private IEnumerator ReturnToDorm()
        {
            SetTransition(true);
            yield return Fade(0f, 1f, .4f);
            dorm.SetActive(true);
            Physics.SyncTransforms();
            if (!RestoreDorm())
            {
                dorm.SetActive(false);
                interactor.ShowInspection("Kembali ke kos", "Tutup untuk mencoba kembali.");
                Debug.LogWarning("[NRA] Chapter 2 return pose is blocked.", this);
                yield return Fade(1f, 0f, .4f);
                SetTransition(false);
                yield break;
            }

            street.Leave();
            ResumeDorm();
            phone.SetReplyAvailable();
            yield return Fade(1f, 0f, .6f);
            SetTransition(false);
            director.AdvanceChapterTwo(ChapterTwoBeat.ReturnedToDorm);
            director.RequestNarrativeCue(new NarrativeCue("Raka", "Tas itu punyaku. Aku perlu cerita ke Bayu.", 3.5f, "c2_return"));
        }

        private bool SendBayuReply()
        {
            return !transitioning && Beat == ChapterTwoBeat.ReturnedToDorm &&
                director.AdvanceChapterTwo(ChapterTwoBeat.RepliedToBayu);
        }

        private bool RestoreCheckpoint()
        {
            if (transitioning) return false;
            if (Beat >= ChapterTwoBeat.StreetEntered && Beat <= ChapterTwoBeat.BagInspected)
                return street != null && street.TryRestoreCheckpoint();
            return RestoreDorm();
        }

        private bool RestoreDorm()
        {
            return motor.TryRestorePose(BedsidePose, RoomFacing) || (checkpoint != null && checkpoint.TryRestoreDorm()) ||
                motor.TryRestorePose(new Vector3(.15f, .04f, 1.1f), RoomFacing);
        }

        private void SuspendDorm()
        {
            dorm.SetActive(false);
            if (roomPostProcessing != null)
            {
                wasRoomPostProcessingActive = roomPostProcessing.activeSelf;
                roomPostProcessing.SetActive(false);
            }
            if (stress != null)
            {
                wasStressEnabled = stress.enabled;
                stress.enabled = false;
            }
            if (breathing != null)
            {
                wasBreathingEnabled = breathing.enabled;
                breathing.enabled = false;
            }
        }

        private void ResumeDorm()
        {
            if (roomPostProcessing != null) roomPostProcessing.SetActive(wasRoomPostProcessingActive);
            if (stress != null)
            {
                stress.ResetToInitial();
                stress.enabled = wasStressEnabled;
            }
            if (breathing != null) breathing.enabled = wasBreathingEnabled;
        }

        private void SetTransition(bool active)
        {
            if (active && transitionCanvas == null)
            {
                var root = new GameObject("Chapter 2 Transition", typeof(Canvas));
                root.transform.SetParent(transform, false);
                transitionCanvas = root.GetComponent<Canvas>();
                transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                transitionCanvas.sortingOrder = 50;
                var panel = new GameObject("Blackout", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(root.transform, false);
                var rect = (RectTransform)panel.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                fadeImage = panel.GetComponent<Image>();
                fadeImage.raycastTarget = false;
                fadeImage.color = Color.clear;
            }
            transitioning = active;
            player.SetTransitionLocked(active);
            transitionCanvas.enabled = active && !player.IsPaused;
        }

        private IEnumerator Fade(float from, float to, float duration, Action<float> animate = null)
        {
            float elapsed = 0f;
            fadeImage.color = new Color(0f, 0f, 0f, from);
            while (elapsed < duration)
            {
                yield return null;
                transitionCanvas.enabled = !player.IsPaused;
                if (player.IsPaused) continue;
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                fadeImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, progress)));
                animate?.Invoke(progress);
            }
        }
    }
}
