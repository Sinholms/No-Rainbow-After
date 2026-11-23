using System;
using System.Collections;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Level;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoRainbowAfter.AI
{
    public sealed class ChapterTwoFlow : MonoBehaviour
    {
        private static readonly Vector3 BedsidePose = new Vector3(.12f, .04f, 1.66f);
        private static readonly Quaternion RoomFacing = Quaternion.Euler(0f, 18f, 0f);
        private static readonly string[] Essay =
        {
            "hujan mengurangi jarak pandang di persimpangan dekat halte.",
            "kejadian itu menunjukkan betapa pentingnya perhatian saat berkendara.",
            "menjaga jarak memberi kita waktu untuk bereaksi di jalan yang basah."
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
        private Canvas transitionCanvas;
        private Image fadeImage;
        private Button retryButton;
        private AudioSource footsteps;
        private AudioClip stepClip;
        private Vector3 lastStepPosition;
        private float stepDistance;
        private bool transitioning, retryVisible;
        private Material photoMaterial;

        private ChapterTwoBeat Beat => director.State.Chapter2Beat;
        internal bool CanEnterStreet => !transitioning && Beat == ChapterTwoBeat.OvertimeRequested;

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
            if (checkpoint != null && laptop != null) checkpoint.SetChapterRestore(null);
            if (interactor != null) interactor.InteractionCompleted -= RegisterOptionalClue;
            if (laptop != null) laptop.EssayFinished -= OnEssayFinished;
            if (street != null) Destroy(street.gameObject);
            if (photoMaterial != null) Destroy(photoMaterial);
        }

        public void PlayOpeningMemory() => StartCoroutine(OpeningMemory());

        private IEnumerator OpeningMemory()
        {
            var memory = CreateStreet();
            if (memory == null || memory.PatrolCar == null)
            {
                if (memory != null) Destroy(memory.gameObject);
                yield break;
            }

            SetTransition(true);
            var camera = motor.ViewCamera.transform;
            var resting = new Pose(camera.position, camera.rotation);
            memory.Stage();
            var car = memory.PatrolCar;
            camera.position = car.position + car.right * 4f + car.forward * 1.7f + Vector3.up * .58f;
            camera.rotation = Quaternion.LookRotation(car.position + Vector3.up * 1.35f - camera.position) *
                Quaternion.Euler(0f, 0f, -13f);
            memory.PlaySiren(.29f);
            yield return Fade(1f, .28f, .45f);
            yield return Fade(.28f, .38f, .50f);
            yield return Fade(.38f, 1f, .35f);
            camera.SetPositionAndRotation(resting.position, resting.rotation);
            memory.Leave();
            Destroy(memory.gameObject);
            yield return Fade(1f, 0f, .55f);
            SetTransition(false);
        }

        public IEnumerator WakeFromChapterOne(Action leaveCorridor)
        {
            if (director.ChapterOneFinished) yield break;
            yield return WakeInDorm(() =>
            {
                leaveCorridor();
                director.FinishChapterOne();
            });
        }

        public IEnumerator StartChapterTwoPreview()
        {
            if (director.PreviewChapter == 2) yield return WakeInDorm(null);
        }

        private IEnumerator WakeInDorm(Action leaveChapterOne)
        {
            if (transitioning || Beat != ChapterTwoBeat.None) yield break;
            var laptopObject = GameObject.Find("PROP_RakaLaptop");
            laptop = laptopObject != null ? laptopObject.GetComponent<LaptopTypingMinigame>() : null;
            if (laptop == null || laptop.transform.parent == null)
            {
                Debug.LogError("[NRA] Chapter 2 needs the dorm laptop.", this);
                yield break;
            }

            dorm = laptop.transform.parent.gameObject;
            roomPostProcessing = GameObject.Find("Room Post Processing");
            SetTransition(true);
            yield return Fade(0f, 1f, .35f);
            if (!RestoreDorm())
            {
                Debug.LogWarning("[NRA] Chapter 2 could not restore the dorm pose.", this);
                yield return Fade(1f, 0f, .3f);
                SetTransition(false);
                yield break;
            }

            leaveChapterOne?.Invoke();
            checkpoint?.SetChapterRestore(RestoreCheckpoint);
            subtitles?.ClearQueuedCues();
            stress?.ResetToInitial();
            phone = ChapterTwoPhone.Create(laptop.transform, player, director.RecordEllaMessage,
                () => director.State.AddClue("CLUE_03"));
            laptop.ConfigureEssay(Essay);
            laptop.EssayFinished += OnEssayFinished;
            CreateGraduationPhoto();
            interactor.InteractionCompleted += RegisterOptionalClue;
            motor.MovementScale = .75f;
            lastStepPosition = motor.transform.position;
            footsteps = gameObject.AddComponent<AudioSource>();
            footsteps.playOnAwake = false;
            stepClip = Resources.Load<AudioClip>("ChapterTwoAudio/HeavyStep");
            Physics.SyncTransforms();
            yield return GetOutOfBed();
        }

        private IEnumerator GetOutOfBed()
        {
            var camera = motor.ViewCamera.transform;
            var standing = new Pose(camera.position, camera.rotation);
            var lying = BedPose();
            camera.SetPositionAndRotation(lying.position, lying.rotation);
            yield return Fade(1f, 0f, 1.05f, progress =>
            {
                float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.27f, 1f, progress));
                camera.SetPositionAndRotation(Vector3.Lerp(lying.position, standing.position, rise),
                    Quaternion.Slerp(lying.rotation, standing.rotation, rise));
            });
            director.AdvanceChapterTwo(ChapterTwoBeat.WokeInDorm);
            SetTransition(false);
        }

        private static Pose BedPose()
        {
            var pillow = GameObject.Find("SM_PROP_SoftPillow");
            var renderer = pillow != null ? pillow.GetComponent<Renderer>() : null;
            Vector3 center = renderer != null ? renderer.bounds.center : new Vector3(-1.27f, .67f, 2.47f);
            return new Pose(center + Vector3.up * .18f, Quaternion.Euler(-58f, 110f, 30f));
        }

        private void OnEssayFinished(int completed)
        {
            if (transitioning || Beat != ChapterTwoBeat.WokeInDorm) return;
            if (completed == 0)
            {
                director.AdvanceChapterTwo(ChapterTwoBeat.StayedInBed);
                StartCoroutine(StayInBed());
                return;
            }

            director.AdvanceChapterTwo(ChapterTwoBeat.OvertimeRequested);
            interactor.ShowInspection("Pesan dari tempat kerja",
                "Raka, malam ini lembur lagi. Naik bus terakhir dari halte pasar, ya.");
        }

        private IEnumerator StayInBed()
        {
            SetTransition(true);
            yield return Fade(0f, 1f, .55f);
            RestoreDorm();
            var lying = BedPose();
            motor.ViewCamera.transform.SetPositionAndRotation(lying.position, lying.rotation);
            yield return Fade(1f, .22f, .75f);
            yield return Fade(.22f, .88f, 2.25f);
            ShowRetry();
        }

        private void ShowRetry()
        {
            if (retryButton == null)
            {
                var panel = new GameObject("Akhir hari", typeof(RectTransform), typeof(Image));
                var rect = panel.GetComponent<RectTransform>();
                rect.SetParent(transitionCanvas.transform, false);
                rect.sizeDelta = new Vector2(700f, 242f);
                var background = panel.GetComponent<Image>();
                background.color = new Color(.043f, .057f, .077f, .97f);
                Label(rect, "Raka rebahan sampai malam.", 52f, 32);
                var action = new GameObject("Ulangi Chapter 2", typeof(RectTransform), typeof(Image), typeof(Button));
                var actionRect = action.GetComponent<RectTransform>();
                actionRect.SetParent(rect, false);
                actionRect.anchoredPosition = new Vector2(0, -50f);
                actionRect.sizeDelta = new Vector2(300f, 58f);
                action.GetComponent<Image>().color = new Color(.20f, .32f, .36f);
                retryButton = action.GetComponent<Button>();
                retryButton.targetGraphic = action.GetComponent<Image>();
                Label(actionRect, "Ulangi Chapter 2", 0f, 26);
                retryButton.onClick.AddListener(RetryFromBed);
            }
            retryButton.transform.parent.gameObject.SetActive(true);
            retryVisible = true;
            player.SetInspecting(true);
            player.SetTransitionLocked(false);
            EventSystem.current?.SetSelectedGameObject(retryButton.gameObject);
        }

        private static void Label(RectTransform parent, string value, float y, int fontSize)
        {
            var text = new GameObject(value, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.up * y;
            text.rectTransform.offsetMax = Vector2.up * y;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = value;
        }

        private void RetryFromBed()
        {
            if (!retryVisible || player.IsPaused || !RestoreDorm()) return;
            retryVisible = false;
            SetTransition(true);
            player.SetInspecting(false);
            retryButton.transform.parent.gameObject.SetActive(false);
            phone.ResetMessage();
            laptop.ResetEssay();
            stress?.ResetToInitial();
            StartCoroutine(GetOutOfBed());
        }

        internal void UseStreetExit()
        {
            if (CanEnterStreet) StartCoroutine(EnterStreet());
        }

        private static ChapterTwoStreet CreateStreet()
        {
            var prefab = Resources.Load<GameObject>("ChapterTwoStreet");
            if (prefab != null) return Instantiate(prefab).GetComponent<ChapterTwoStreet>();
            Debug.LogError("[NRA] The Chapter 2 street prefab is missing.");
            return null;
        }

        private IEnumerator EnterStreet()
        {
            SetTransition(true);
            subtitles?.ClearQueuedCues();
            if (street == null)
            {
                street = CreateStreet();
                if (street != null)
                {
                    street.SafeZoneReached += () => director.AdvanceChapterTwo(ChapterTwoBeat.ShelterReached);
                    street.BoardRequested += OnBoardRequested;
                }
            }

            if (street != null) street.PlaySiren(.46f);
            yield return Fade(0f, 1f, .34f);
            if (street == null || !street.Enter(motor))
            {
                if (street != null) street.Leave();
                Debug.LogWarning("[NRA] Chapter 2 street could not be entered.", this);
                yield return Fade(1f, 0f, .4f);
                SetTransition(false);
                yield break;
            }

            dorm.SetActive(false);
            if (roomPostProcessing != null) roomPostProcessing.SetActive(false);
            if (stress != null) stress.enabled = false;
            if (breathing != null) breathing.enabled = false;
            director.AdvanceChapterTwo(ChapterTwoBeat.StreetEntered);
            yield return Fade(1f, 0f, .65f);
            SetTransition(false);
        }

        private void OnBoardRequested()
        {
            if (!transitioning && Beat == ChapterTwoBeat.ShelterReached)
                StartCoroutine(BoardBus());
        }

        private IEnumerator BoardBus()
        {
            SetTransition(true);
            yield return Fade(0f, 1f, .45f);
            if (street.TryBoard())
            {
                motor.MovementScale = 1f;
                director.AdvanceChapterTwo(ChapterTwoBeat.BoardedBus);
            }
            yield return Fade(1f, 0f, .65f);
            SetTransition(false);
        }

        private void CreateGraduationPhoto()
        {
            var photo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            photo.name = "Foto Raka dan Ella";
            photo.transform.SetParent(laptop.transform.parent, false);
            photo.transform.SetPositionAndRotation(laptop.transform.position + new Vector3(-.40f, .09f, -.05f),
                Quaternion.Euler(12f, -16f, 0f));
            photo.transform.localScale = new Vector3(.16f, .18f, .031f);
            photoMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = new Color(.68f, .59f, .42f)
            };
            photo.GetComponent<Renderer>().sharedMaterial = photoMaterial;
            var inspection = photo.AddComponent<SceneInteractable>();
            inspection.Configure("INT_C2_PHOTO", "Foto Raka dan Ella", "Periksa foto", "c2.photo",
                "Foto Raka dan Ella", "Di belakang foto ada tulisan tangan Ella: “Wisuda bareng, ya.”", false);
        }

        private void RegisterOptionalClue(string id)
        {
            if (id == "INT_C2_PHOTO") director.State.AddClue("CLUE_01");
            if (id == "INT_C2_BAG") director.State.AddClue("CLUE_04");
        }

        private bool RestoreCheckpoint()
        {
            if (transitioning) return false;
            if (Beat >= ChapterTwoBeat.StreetEntered)
                return street != null && street.TryRestoreCheckpoint();
            return RestoreDorm();
        }

        private bool RestoreDorm()
        {
            return motor.TryRestorePose(BedsidePose, RoomFacing) || (checkpoint != null && checkpoint.TryRestoreDorm()) ||
                motor.TryRestorePose(new Vector3(.15f, .04f, 1.1f), RoomFacing);
        }

        private void Update()
        {
            if (retryVisible)
            {
                transitionCanvas.enabled = !player.IsPaused;
                return;
            }
            if (footsteps == null || stepClip == null || motor.MovementScale >= 1f) return;
            Vector3 current = motor.transform.position;
            float distance = Vector3.ProjectOnPlane(current - lastStepPosition, Vector3.up).magnitude;
            lastStepPosition = current;
            if (transitioning || player.IsPaused || player.IsInspecting || distance > .6f)
            {
                stepDistance = 0f;
                return;
            }
            stepDistance += distance;
            if (stepDistance < (motor.IsCrouched ? .46f : .82f)) return;
            stepDistance = 0f;
            footsteps.PlayOneShot(stepClip, motor.IsCrouched ? .16f : .38f);
        }

        private void SetTransition(bool active)
        {
            if (transitionCanvas == null)
            {
                var root = new GameObject("Chapter Transition", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                root.transform.SetParent(transform, false);
                transitionCanvas = root.GetComponent<Canvas>();
                transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                transitionCanvas.sortingOrder = 50;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                var panel = new GameObject("Blackout", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(root.transform, false);
                var rect = (RectTransform)panel.transform;
                rect.anchorMax = Vector2.one;
                rect.anchorMin = rect.offsetMin = rect.offsetMax = Vector2.zero;
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
