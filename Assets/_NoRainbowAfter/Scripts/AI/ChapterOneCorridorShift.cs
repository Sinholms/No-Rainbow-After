using System.Collections;
using NoRainbowAfter.Narrative;
using NoRainbowAfter.Player;
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
        private void Start()
        {
            director=GetComponent<ChapterDirector>();player=GetComponent<FirstPersonPlayer>();motor=GetComponent<FirstPersonMotor>();
            if(Navigation != null) instance=NavMesh.AddNavMeshData(Navigation);
            if(Corridor != null) Corridor.SetActive(false);
        }
        private void OnDestroy() { if(instance.valid) instance.Remove(); }
        private void LateUpdate()
        {
            if(director == null || player.IsPaused || player.IsInspecting || player.IsTransitionLocked) return;
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
            if(motor.TryRestorePose(Entry,Quaternion.Euler(0,180,0))) Expanded=true;
            else { Corridor.SetActive(false); Debug.LogError("[NRA] Horror corridor entry is blocked."); }
            yield return new WaitForSeconds(.2f);
            Destroy(root);player.SetTransitionLocked(false);shifting=false;
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
}
