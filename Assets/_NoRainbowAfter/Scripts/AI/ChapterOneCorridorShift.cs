using System.Collections;
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
        public GameObject Corridor;
        public NavMeshData Navigation;
        public Vector3 Entry, MonsterSpawn;
        public bool Expanded { get; private set; }
        public bool CanBegin => OwnsPlayer && !shifting && motor.transform.position.x >= 79.8f && motor.transform.position.z < 2f && motor.transform.position.z > -4f;
        public bool OwnsPlayer => Expanded && motor != null && motor.transform.position.x > 45f;
        private ChapterDirector director;
        private FirstPersonPlayer player;
        private FirstPersonMotor motor;
        private NavMeshDataInstance instance;
        private bool shifting;
        private ObjectiveTracker objectives;
        private bool arrivalNarrated;
        private Vector3 lastSafePosition;
        public int FallRecoveryCount { get; private set; }
        private void Start()
        {
            director=GetComponent<ChapterDirector>();player=GetComponent<FirstPersonPlayer>();motor=GetComponent<FirstPersonMotor>();
            objectives=GetComponent<ObjectiveTracker>();
            if(Navigation != null) instance=NavMesh.AddNavMeshData(Navigation);
            if(Corridor != null) { CreateExitDoor(); Corridor.SetActive(false); }
        }
        private void OnDestroy() { if(instance.valid) instance.Remove(); }
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
            }
            if(objectives != null && director != null)
            {
                if(!OwnsPlayer) objectives.SetChapterGuidance(null,null);
                else if(director.BeatIndex>=6) objectives.SetChapterGuidance("Cari pintu keluar",player.IsHiding ? "Bayangan itu telah pergi. Tekan E untuk keluar dari lemari." : "Temukan pintu keluar di ujung lorong.");
                else if(player.IsHiding) objectives.SetChapterGuidance("Tetap bersembunyi","Tunggu sampai langkahnya menjauh. Tekan E untuk keluar dari lemari.");
                else if(director.BeatIndex>=4) objectives.SetChapterGuidance("Lolos dari bayangan","Cari lemari dan tekan E untuk bersembunyi, atau klik kiri saat bayangan dekat untuk melawan.");
                else objectives.SetChapterGuidance("Cari jalan keluar","Berjalan ke ujung lorong.");
            }
            if(director == null || director.ChapterOneFinished || player.IsPaused || player.IsInspecting || player.IsTransitionLocked) return;
            if(!OwnsPlayer && !shifting && director.BeatIndex>=3 && motor.transform.position.z>=5.75f) StartCoroutine(Shift());
            if(CanBegin) director.SamplePosition(6.5f);
        }
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
            door.transform.localPosition = new Vector3(18.6f, 1.1f, 1.02f);
            door.transform.localScale = new Vector3(1.15f, 2.2f, .12f);
            var wardrobe = Corridor.GetComponentInChildren<HidingWardrobe>(true);
            var source = wardrobe != null ? wardrobe.GetComponentInChildren<Renderer>(true) : null;
            if (source != null) door.GetComponent<Renderer>().sharedMaterial = source.sharedMaterial;
            door.AddComponent<ChapterOneExitDoor>().Initialize(this, director);
            var handle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handle.name = "Handle";
            handle.transform.SetParent(door.transform, false);
            handle.transform.localPosition = new Vector3(.35f, -.03f, .7f);
            handle.transform.localScale = new Vector3(.065f, .035f, .45f);
            Destroy(handle.GetComponent<Collider>());
            var lamp = new GameObject("Exit Light");
            lamp.transform.SetParent(Corridor.transform, false);
            lamp.transform.localPosition = new Vector3(18.6f, 2.35f, 1.6f);
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
            shifting = true; player.SetTransitionLocked(true);
            var root = new GameObject("Chapter transition", typeof(Canvas));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
            var panel = new GameObject("Blackout", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(root.transform, false);
            var rect = (RectTransform)panel.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = panel.GetComponent<Image>(); image.color = Color.black; image.raycastTarget = false;
            yield return new WaitForSeconds(.5f);
            director.FinishChapterOne();
            GetComponent<SubtitlePlaceholder>()?.ClearQueuedCues();
            var titleObject = new GameObject("Chapter complete", typeof(RectTransform), typeof(Text));
            titleObject.transform.SetParent(root.transform, false);
            var titleRect = (RectTransform)titleObject.transform;
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(.5f, .5f);
            titleRect.sizeDelta = new Vector2(900f, 220f);
            var title = titleObject.GetComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.alignment = TextAnchor.MiddleCenter; title.fontSize = 36;
            title.color = Color.white; title.raycastTarget = false;
            title.text = "CHAPTER 1 SELESAI\n\nChapter 2 akan berlanjut\n\nKembali ke menu utama...";
            yield return new WaitForSeconds(6f);
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");

        }
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
