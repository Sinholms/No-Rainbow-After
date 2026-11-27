using System.Collections;
using NoRainbowAfter.Level;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
using NoRainbowAfter.Interaction;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace NoRainbowAfter.AI
{
    public sealed class ChapterOneCorridorShift : MonoBehaviour
    {
        public const float CorridorStressIntervalSeconds = 3.0f;
        public const float CorridorStressIncrement = 4f;

        public GameObject Corridor;
        public NavMeshData Navigation;
        public Vector3 Entry, MonsterSpawn;
        public bool Expanded { get; private set; }
        public bool CanBegin => OwnsPlayer && !shifting && motor.transform.position.x >= 79.8f && motor.transform.position.z < 2f && motor.transform.position.z > -4f;
        public bool OwnsPlayer => Expanded && motor != null && motor.transform.position.x > 45f;
        private ChapterDirector director;
        private FirstPersonPlayer player;
        private FirstPersonMotor motor;
        private StressBarometer stressBarometer;
        private NavMeshDataInstance instance;
        private bool shifting;
        private ObjectiveTracker objectives;
        private ChapterTwoFlow chapterTwo;
        internal SharedCorridorDoor SharedExit { get; private set; }
        public static bool ShowCrashMemoryOnStart { get; set; }
        private bool arrivalNarrated;
        private Vector3 lastSafePosition;
        private float corridorStressTimer;
        public int FallRecoveryCount { get; private set; }
        private void Start()
        {
            director=GetComponent<ChapterDirector>();player=GetComponent<FirstPersonPlayer>();motor=GetComponent<FirstPersonMotor>();
            stressBarometer=GetComponent<StressBarometer>();
            if (GetComponent<ChapterStressRestart>() == null) gameObject.AddComponent<ChapterStressRestart>();
            if (motor != null) motor.PoseRestored += OnPlayerPoseRestored;
            objectives=GetComponent<ObjectiveTracker>();
            chapterTwo = gameObject.AddComponent<ChapterTwoFlow>();
            if (GetComponent<ChapterThreeFlow>() == null) gameObject.AddComponent<ChapterThreeFlow>();
            if(Navigation != null) instance=NavMesh.AddNavMeshData(Navigation);
            if(Corridor != null) { CreateExitDoor(); Corridor.SetActive(false); }
            SharedExit = SharedCorridorDoor.ReplaceEndWall(player, CanCrossSharedExit, CrossSharedExit);
            if (SharedExit == null) Debug.LogError("[NRA] Missing end wall at the normal corridor exit.", this);
            if (ShowCrashMemoryOnStart)
            {
                ShowCrashMemoryOnStart = false;
                chapterTwo.PlayOpeningMemory();
            }
        }
        private void OnDestroy()
        {
            if (motor != null) motor.PoseRestored -= OnPlayerPoseRestored;
            if(instance.valid) instance.Remove();
        }
        private void OnPlayerPoseRestored()
        {
            // Saat checkpoint mengembalikan pemain ke kamar, tutup versi lorong horor.
            if (Expanded && !shifting && !OwnsPlayer) LeaveCorridor();
        }
        private void LateUpdate()
        {
            if(OwnsPlayer && !shifting && !player.IsPaused && !player.IsTransitionLocked)
            {
                if(motor.transform.position.y < -1f)
                {
                    if(motor.TryRestorePose(lastSafePosition+Vector3.up*.08f,motor.transform.rotation)) FallRecoveryCount++;
                }
                else if(!player.IsHiding && motor.GetComponent<CharacterController>().isGrounded)
                {
                    NavMeshHit safe;
                    if(NavMesh.SamplePosition(motor.transform.position,out safe,.35f,NavMesh.AllAreas)) lastSafePosition=safe.position;
                }
                TickCorridorStress(Time.deltaTime);
            }
            if(objectives != null && director != null && !director.ChapterOneFinished)
            {
                if(!OwnsPlayer) objectives.SetChapterGuidance(null,null);
                else if(director.BeatIndex>=6) objectives.SetChapterGuidance("Cari pintu keluar",player.IsHiding ? "Bayangan itu telah pergi. Tekan E untuk keluar dari lemari." : "Temukan pintu keluar di ujung lorong.");
                else if(player.IsHiding) objectives.SetChapterGuidance("Tetap bersembunyi","Tekan [TAB] untuk atur napas. Tunggu langkah menjauh lalu tekan E untuk keluar.");
                else if(director.BeatIndex>=4) objectives.SetChapterGuidance("Lolos dari bayangan","Cari lemari (E) untuk bersembunyi & atur napas [TAB], atau klik kiri saat bayangan dekat untuk melawan.");
                else objectives.SetChapterGuidance("Cari jalan keluar","Berjalan ke ujung lorong. Hati-hati, stres meningkat di dalam koridor.");
            }
            if(director == null || director.ChapterOneFinished || player.IsPaused || player.IsInspecting || player.IsUsingLaptop || player.IsTransitionLocked) return;
            if(CanBegin) director.SamplePosition(6.5f);
        }

        private bool CanCrossSharedExit()
        {
            if (shifting || director == null || director.State == null || player.IsTransitionLocked) return false;
            return director.ChapterOneFinished ? chapterTwo.CanEnterStreet : !Expanded && director.HasReadOvertimeSlip;
        }

        private void CrossSharedExit()
        {
            if (!CanCrossSharedExit()) return;
            if (director.ChapterOneFinished) chapterTwo.UseStreetExit();
            else StartCoroutine(Shift());
        }

        public void TickCorridorStress(float deltaTime)
        {
            if (director == null || director.ChapterOneFinished || player == null || motor == null) return;
            if (!OwnsPlayer || shifting || player.IsPaused || player.IsTransitionLocked) return;
            if (stressBarometer == null) stressBarometer = GetComponent<StressBarometer>();

            // Kenaikan stres berhenti sementara saat pemain di lemari atau sedang mengatur napas.
            if (player.IsHiding || player.IsBreathing) return;

            corridorStressTimer += Mathf.Max(0f, deltaTime);
            while (corridorStressTimer >= CorridorStressIntervalSeconds)
            {
                corridorStressTimer -= CorridorStressIntervalSeconds;
                stressBarometer?.AddStress(CorridorStressIncrement);
            }
        }

        // Layar ditutup sebelum memindahkan pemain ke lorong horor yang dimuat dari scene yang sama.
        private IEnumerator Shift()
        {
            shifting=true;
            player.SetTransitionLocked(true);
            var root=new GameObject("Corridor transition",typeof(Canvas));
            var c=root.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=40;
            var p=new GameObject("Blackout",typeof(RectTransform),typeof(Image));p.transform.SetParent(root.transform,false);
            var r=(RectTransform)p.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            var image=p.GetComponent<Image>();image.color=Color.black;image.raycastTarget=false;
            yield return new WaitForSeconds(.35f);
            Corridor.SetActive(true);Physics.SyncTransforms();
            if(motor.TryRestorePose(Entry,Quaternion.Euler(0,180,0))) { Expanded=true;lastSafePosition=Entry; }
            else { Corridor.SetActive(false); Debug.LogError("[NRA] Horror corridor entry is blocked."); }
            yield return new WaitForSeconds(.2f);
            Destroy(root);player.SetTransitionLocked(false);shifting=false;
            if(Expanded && !arrivalNarrated)
            {
                arrivalNarrated=true;
                GetComponent<SubtitlePlaceholder>()?.ClearQueuedCues();
                director.RequestNarrativeCue(new NarrativeCue("Raka","Aku di mana? Kenapa aku bisa di sini?",3.4f,"AUD_C1_CORRIDOR_ARRIVAL"));
                director.RequestNarrativeCue(new NarrativeCue("Raka","Aku harus menemukan jalan keluar dan pergi kerja.",3.6f,"AUD_C1_CORRIDOR_FIND_EXIT"));
            }
        }
        private void CreateExitDoor()
        {
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "C1_ExitDoor";
            door.transform.SetParent(Corridor.transform, false);
            door.transform.localPosition = new Vector3(18.6f, 1.1f, 0.81f);
            door.transform.localScale = new Vector3(1.15f, 2.2f, .06f);
            var wardrobe = Corridor.GetComponentInChildren<HidingWardrobe>(true);
            var source = wardrobe != null ? wardrobe.GetComponentInChildren<Renderer>(true) : null;
            if (source != null) door.GetComponent<Renderer>().sharedMaterial = source.sharedMaterial;
            door.AddComponent<ChapterOneExitDoor>().Initialize(this, director);

            // Flush wooden/metal door frame jambs around the recessed exit door
            var frameMat = source != null ? source.sharedMaterial : null;
            foreach (float sx in new[] { -0.615f, 0.615f })
            {
                var jamb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                jamb.name = sx < 0 ? "C1_ExitJamb_L" : "C1_ExitJamb_R";
                jamb.transform.SetParent(Corridor.transform, false);
                jamb.transform.localPosition = new Vector3(18.6f + sx, 1.12f, 0.82f);
                jamb.transform.localScale = new Vector3(0.08f, 2.24f, 0.08f);
                if (frameMat != null) jamb.GetComponent<Renderer>().sharedMaterial = frameMat;
                Destroy(jamb.GetComponent<Collider>());
            }
            var lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lintel.name = "C1_ExitJamb_Top";
            lintel.transform.SetParent(Corridor.transform, false);
            lintel.transform.localPosition = new Vector3(18.6f, 2.24f, 0.82f);
            lintel.transform.localScale = new Vector3(1.31f, 0.08f, 0.08f);
            if (frameMat != null) lintel.GetComponent<Renderer>().sharedMaterial = frameMat;
            Destroy(lintel.GetComponent<Collider>());

            var handle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handle.name = "Handle";
            handle.transform.SetParent(door.transform, false);
            handle.transform.localPosition = new Vector3(.35f, -.03f, .95f);
            handle.transform.localScale = new Vector3(.065f, .035f, .65f);
            Destroy(handle.GetComponent<Collider>());
            var lamp = new GameObject("Exit Light");
            lamp.transform.SetParent(Corridor.transform, false);
            lamp.transform.localPosition = new Vector3(18.6f, 2.35f, 1.18f);
            var light = lamp.AddComponent<Light>(); light.type = LightType.Point;
            light.color = new Color(.75f, .87f, 1f); light.range = 3f; light.intensity = 1.4f;
        }
        public void UseExit()
        {
            if (!shifting && director != null && director.BeatIndex >= 6 && !director.ChapterOneFinished)
                StartCoroutine(ExitToChapterTwo());
        }
        private IEnumerator ExitToChapterTwo()
        {
            shifting = true;
            yield return chapterTwo.WakeFromChapterOne(LeaveCorridor);
            shifting = false;
        }
        private void LeaveCorridor()
        {
            HidingWardrobe.Occupied?.Release();
            player.SetBreathRecoveryRequired(false);
            corridorStressTimer = 0f;
            if (Corridor != null) Corridor.SetActive(false);
            Expanded = false;
            SharedExit?.ResetDoor();
            Physics.SyncTransforms();
            director.ResetCrossing();
        }
        // Cari jalur NavMesh lengkap, lalu gunakan belokan berikutnya sebagai tujuan gerak bayangan.
        public Vector3 Navigate(Vector3 from, Vector3 to)
        {
            NavMeshHit a,b;
            if(!NavMesh.SamplePosition(from,out a,2f,NavMesh.AllAreas) || !NavMesh.SamplePosition(to,out b,2f,NavMesh.AllAreas)) return from;
            var path=new NavMeshPath();
            if(!NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path) || path.status!=NavMeshPathStatus.PathComplete) return from;
            foreach(var corner in path.corners) if(Vector3.Distance(new Vector3(from.x,0,from.z),new Vector3(corner.x,0,corner.z))>.2f) return corner;
            return b.position;
        }
    }
    public sealed class ChapterOneExitDoor : MonoBehaviour, IInteractable
    {
        private ChapterOneCorridorShift shift;
        private ChapterDirector director;
        public string StableId => "INT_C1_EXIT";
        public string DisplayName => "Pintu keluar";
        public string Prompt => director != null && director.BeatIndex >= 6 ? "Buka pintu keluar" : "Pintu terkunci — lolos dari bayangan";
        public void Initialize(ChapterOneCorridorShift owner, ChapterDirector story) { shift = owner; director = story; }
        public void SetFocused(bool focused) { }
        public void Interact(PlayerInteractor interactor) { shift?.UseExit(); }
    }

}
