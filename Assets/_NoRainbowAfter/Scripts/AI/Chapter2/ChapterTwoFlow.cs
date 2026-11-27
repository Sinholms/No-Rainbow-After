using System;
using System.Collections;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Level;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NoRainbowAfter.AI
{
    /// <summary>
    /// Sutradara alur cerita Chapter 2 dari kamar kos hingga naik bus menuju Chapter 3.
    ///
    /// [TEKNIS KODING]
    /// Dipecah menjadi 3 file partial class:
    /// 1. ChapterTwoFlow.cs (state machine, kilas balik patroli, transisi kamar -> jalan -> bus)
    /// 2. ChapterTwoFlow.DormProps.cs (pembuatan foto wisuda PBR & interaksi galon/clue)
    /// 3. ChapterTwoFlow.CameraAndUi.cs (animasi kamera bangun dari kasur, panel ulangi, & layar fade)
    /// </summary>
    public sealed partial class ChapterTwoFlow : MonoBehaviour
    {
        // =========================================================================
        // Konstanta Posisi & Kalimat Esai Laptop Chapter 2
        // =========================================================================

        private static readonly Vector3 BedsidePose = new Vector3(.12f, .04f, 1.66f);
        private static readonly Quaternion RoomFacing = Quaternion.Euler(0f, 18f, 0f);
        private static readonly string[] Essay =
        {
            "hujan mengurangi jarak pandang di persimpangan dekat halte",
            "kejadian itu menunjukkan betapa pentingnya perhatian saat berkendara",
            "menjaga jarak memberi kita waktu untuk bereaksi di jalan yang basah"
        };

        // =========================================================================
        // Referensi Komponen, Properti, & Audio Langkah Berat
        // =========================================================================

        private FirstPersonPlayer player;
        private FirstPersonMotor motor;
        private PlayerInteractor interactor;
        private ChapterDirector director;
        private DormCheckpoint checkpoint;
        private StressBarometer stress;
        private SubtitlePlaceholder subtitles;
        private LaptopTypingMinigame laptop;
        private GameObject dorm;
        private GameObject roomPostProcessing;
        private ChapterTwoPhone phone;
        private ChapterTwoStreet street;
        private SceneInteractable waterGallon;
        private BoxCollider addedGallonCollider;
        private Canvas transitionCanvas;
        private Image fadeImage;
        private Button retryButton;
        private AudioSource footsteps;
        private AudioClip stepClip;
        private Vector3 lastStepPosition;
        private float stepDistance;
        private bool transitioning;
        private bool retryVisible;
        private bool pendingGallonSkip;
        private Material[] photoMaterials;

        // Beat adalah tahap cerita yang disimpan oleh ChapterDirector. Pintu jalan terbuka setelah pesan lembur.
        private ChapterTwoBeat Beat => director.State.Chapter2Beat;
        internal bool CanEnterStreet => !transitioning && Beat == ChapterTwoBeat.OvertimeRequested;

        // =========================================================================
        // Inisialisasi, Pengikatan Jalan Scene, & Pembersihan
        // =========================================================================

        private void Awake()
        {
            player = GetComponent<FirstPersonPlayer>();
            motor = GetComponent<FirstPersonMotor>();
            interactor = GetComponent<PlayerInteractor>();
            director = GetComponent<ChapterDirector>();
            checkpoint = GetComponent<DormCheckpoint>();
            stress = GetComponent<StressBarometer>();
            subtitles = GetComponent<SubtitlePlaceholder>();

            // Jika scene Chapter3 tidak tersedia, siapkan komponen ChapterThreeFlow sebagai fallback.
            const string chapterThreePath = "Assets/_NoRainbowAfter/Scenes/Chapter3.unity";
            if (!Application.CanStreamedLevelBeLoaded(chapterThreePath) && GetComponent<ChapterThreeFlow>() == null)
            {
                gameObject.AddComponent<ChapterThreeFlow>();
            }

            // Jika ChapterTwoStreet sudah ada di scene Chapter2.unity agar terlihat di Edit Mode,
            // gunakan ulang instance tersebut dan nonaktifkan sementara selama masih di kamar kos.
            foreach (var existing in FindObjectsByType<ChapterTwoStreet>(FindObjectsInactive.Include))
            {
                if (existing.gameObject.scene != gameObject.scene)
                {
                    continue;
                }
                BindStreet(existing);
                existing.gameObject.SetActive(false);
                break;
            }
        }

        private void Start() => StartCoroutine(AutoStartIfChapterTwoScene());

        private IEnumerator AutoStartIfChapterTwoScene()
        {
            // Tunggu 1 frame agar ChapterDirector.Start() selesai menginisialisasi GameState.
            yield return null;
            if (director != null && director.SceneChapter == 2 && director.PreviewChapter == 0 && Beat == ChapterTwoBeat.None)
            {
                yield return WakeInDorm(null);
            }
        }

        private void BindStreet(ChapterTwoStreet instance)
        {
            if (instance == null)
            {
                return;
            }
            street = instance;
            street.SafeZoneReached += () => director.AdvanceChapterTwo(ChapterTwoBeat.ShelterReached);
            street.BoardRequested += OnBoardRequested;
        }

        private void OnDestroy()
        {
            if (checkpoint != null && laptop != null) checkpoint.SetChapterRestore(null);
            if (interactor != null) interactor.InteractionCompleted -= RegisterOptionalClue;
            if (laptop != null) laptop.EssayFinished -= OnEssayFinished;
            if (waterGallon != null) Destroy(waterGallon);
            if (addedGallonCollider != null) Destroy(addedGallonCollider);
            if (street != null) Destroy(street.gameObject);
            if (photoMaterials != null)
            {
                foreach (var material in photoMaterials)
                {
                    Destroy(material);
                }
            }
        }

        // =========================================================================
        // Memori Kecelakaan & Masuk ke Kamar Kos Chapter 2
        // =========================================================================

        public void PlayOpeningMemory() => StartCoroutine(OpeningMemory());

        // Pakai salinan jalan sementara untuk cuplikan kecelakaan, lalu kembalikan pose kamera semula.
        private IEnumerator OpeningMemory()
        {
            var memory = CreateStreet();
            if (memory == null || memory.PatrolCar == null)
            {
                if (memory != null)
                {
                    Destroy(memory.gameObject);
                }
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
            if (director.ChapterOneFinished)
            {
                yield break;
            }
            yield return WakeInDorm(() =>
            {
                leaveCorridor();
                director.FinishChapterOne();
            });
        }

        public IEnumerator StartChapterTwoPreview()
        {
            if (director.PreviewChapter == 2)
            {
                yield return WakeInDorm(null);
            }
        }

        // Pastikan pemain berhasil dipindah ke kamar sebelum menutup bab pertama dan menyiapkan tugas laptop.
        private IEnumerator WakeInDorm(Action leaveChapterOne)
        {
            if (transitioning || Beat != ChapterTwoBeat.None)
            {
                yield break;
            }
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

            phone = ChapterTwoPhone.Create(
                laptop.transform,
                player,
                director.RecordEllaMessage,
                () => director.State.AddClue("CLUE_03"));
            laptop.ConfigureEssay(Essay);
            laptop.EssayFinished += OnEssayFinished;
            SetupWaterGallon();
            CreateGraduationPhoto();
            interactor.InteractionCompleted += RegisterOptionalClue;

            // Chapter 2 memberi efek lelah: kecepatan gerak turun menjadi 75% dengan langkah berat.
            motor.MovementScale = .75f;
            lastStepPosition = motor.transform.position;
            footsteps = gameObject.AddComponent<AudioSource>();
            footsteps.playOnAwake = false;
            stepClip = Resources.Load<AudioClip>("ChapterTwoAudio/HeavyStep");

            Physics.SyncTransforms();
            yield return GetOutOfBed();
        }

        // =========================================================================
        // Cabang Esai / Rebahan & Transisi Menuju Jalan Pasar Malam -> Bus
        // =========================================================================

        // Jumlah kalimat yang selesai menentukan cabang cerita. Nol kalimat memicu adegan rebahan.
        private void OnEssayFinished(int completed)
        {
            if (transitioning || Beat != ChapterTwoBeat.WokeInDorm)
            {
                return;
            }
            if (completed == 0)
            {
                director.AdvanceChapterTwo(ChapterTwoBeat.StayedInBed);
                StartCoroutine(StayInBed());
                return;
            }

            AdvanceToOvertimeRequested();
        }

        private void AdvanceToOvertimeRequested()
        {
            if (transitioning || Beat != ChapterTwoBeat.WokeInDorm)
            {
                return;
            }
            director.AdvanceChapterTwo(ChapterTwoBeat.OvertimeRequested);
            interactor.ShowInspection(
                "Pesan dari tempat kerja",
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

        private void RetryFromBed()
        {
            if (!retryVisible || player.IsPaused || !RestoreDorm())
            {
                return;
            }
            retryVisible = false;
            pendingGallonSkip = false;
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
            if (CanEnterStreet)
            {
                StartCoroutine(EnterStreet());
            }
        }

        private static ChapterTwoStreet CreateStreet()
        {
            var prefab = Resources.Load<GameObject>("ChapterTwoStreet");
            if (prefab != null)
            {
                return Instantiate(prefab).GetComponent<ChapterTwoStreet>();
            }
            Debug.LogError("[NRA] The Chapter 2 street prefab is missing.");
            return null;
        }

        // Pasang event jalan saat prefab dibuat. Tahap cerita maju setelah posisi pemain berhasil dipindah.
        private IEnumerator EnterStreet()
        {
            SetTransition(true);
            subtitles?.ClearQueuedCues();

            if (street == null)
            {
                var created = CreateStreet();
                if (created != null)
                {
                    BindStreet(created);
                }
            }

            if (street != null)
            {
                street.PlaySiren(.46f);
            }
            yield return Fade(0f, 1f, .34f);

            if (street == null || !street.Enter(motor))
            {
                if (street != null)
                {
                    street.Leave();
                }
                Debug.LogWarning("[NRA] Chapter 2 street could not be entered.", this);
                yield return Fade(1f, 0f, .4f);
                SetTransition(false);
                yield break;
            }

            dorm.SetActive(false);
            if (roomPostProcessing != null)
            {
                roomPostProcessing.SetActive(false);
            }
            director.AdvanceChapterTwo(ChapterTwoBeat.StreetEntered);
            yield return Fade(1f, 0f, .65f);
            SetTransition(false);
        }

        private void OnBoardRequested()
        {
            if (!transitioning && Beat == ChapterTwoBeat.ShelterReached)
            {
                StartCoroutine(BoardBus());
            }
        }

        // Pemain naik bus setelah mencapai tempat aman. Gerak normal dipulihkan jika berhasil naik,
        // lalu berlanjut ke scene Chapter3.unity (bila tersedia di Build Settings).
        private IEnumerator BoardBus()
        {
            SetTransition(true);
            yield return Fade(0f, 1f, .45f);
            if (street.TryBoard())
            {
                motor.MovementScale = 1f;
                director.AdvanceChapterTwo(ChapterTwoBeat.BoardedBus);
                const string chapterThreePath = "Assets/_NoRainbowAfter/Scenes/Chapter3.unity";
                if (Application.CanStreamedLevelBeLoaded(chapterThreePath))
                {
                    ChapterDirector.CarryStateToNextChapter(director.State);
                    Time.timeScale = 1f;
                    var load = SceneManager.LoadSceneAsync(chapterThreePath, LoadSceneMode.Single);
                    while (load != null && !load.isDone)
                    {
                        yield return null;
                    }
                    yield break;
                }
            }
            yield return Fade(1f, 0f, .65f);
            SetTransition(false);
        }

        // =========================================================================
        // Pemulihan Checkpoint & Audio Langkah Berat di Kamar/Jalan
        // =========================================================================

        // Pilih checkpoint jalan atau kamar berdasarkan tahap cerita. Tolak pemulihan selama transisi.
        private bool RestoreCheckpoint()
        {
            if (transitioning)
            {
                return false;
            }
            if (Beat >= ChapterTwoBeat.StreetEntered)
            {
                return street != null && street.TryRestoreCheckpoint();
            }
            return RestoreDorm();
        }

        private bool RestoreDorm()
        {
            return motor.TryRestorePose(BedsidePose, RoomFacing) ||
                   (checkpoint != null && checkpoint.TryRestoreDorm()) ||
                   motor.TryRestorePose(new Vector3(.15f, .04f, 1.1f), RoomFacing);
        }

        private void Update()
        {
            if (pendingGallonSkip && !transitioning && Beat == ChapterTwoBeat.WokeInDorm && (interactor == null || !interactor.IsInspecting))
            {
                pendingGallonSkip = false;
                AdvanceToOvertimeRequested();
                return;
            }

            if (retryVisible)
            {
                transitionCanvas.enabled = !player.IsPaused;
                return;
            }

            if (footsteps == null || stepClip == null || motor.MovementScale >= 1f)
            {
                return;
            }

            Vector3 current = motor.transform.position;
            float distance = Vector3.ProjectOnPlane(current - lastStepPosition, Vector3.up).magnitude;
            lastStepPosition = current;

            if (transitioning || player.IsPaused || player.IsInspecting || distance > .6f)
            {
                stepDistance = 0f;
                return;
            }

            stepDistance += distance;
            if (stepDistance < (motor.IsCrouched ? .46f : .82f))
            {
                return;
            }
            stepDistance = 0f;
            footsteps.PlayOneShot(stepClip, motor.IsCrouched ? .16f : .38f);
        }
    }
}
