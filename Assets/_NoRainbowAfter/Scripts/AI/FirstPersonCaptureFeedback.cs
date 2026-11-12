using NoRainbowAfter.Player;
using UnityEngine;
using UnityEngine.UI;

namespace NoRainbowAfter.AI
{
    // Procedural FPP struggle and a soft damage border; driven by the encounter clock so pause freezes it.
    public sealed class FirstPersonCaptureFeedback : MonoBehaviour
    {
        private Camera view;
        private Transform attacker;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private Canvas canvas;
        private Image border;
        private Texture2D texture;
        private Sprite sprite;
        private bool animating, injured;
        private float phase;
        public float DamageAlpha => border != null ? border.color.a : 0f;

        public void Configure(FirstPersonMotor motor)
        {
            view=motor != null ? motor.ViewCamera : null;
            if(canvas != null) return;
            var root=new GameObject("Capture damage feedback",typeof(Canvas));root.transform.SetParent(transform,false);
            canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=18;
            var panel=new GameObject("Red screen edges",typeof(RectTransform),typeof(Image));panel.transform.SetParent(root.transform,false);
            var rect=(RectTransform)panel.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            border=panel.GetComponent<Image>();border.raycastTarget=false;
            const int size=256;texture=new Texture2D(size,size,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;
            var pixels=new Color[size*size];float aspect=(float)Screen.width/Mathf.Max(1,Screen.height);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float radius=new Vector2(((x+.5f)/size-.5f)*aspect,(y+.5f)/size-.5f).magnitude;
                float a=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.48f,radius));
                pixels[y*size+x]=new Color(1,1,1,a);
            }
            texture.SetPixels(pixels);texture.Apply();
            sprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f));border.sprite=sprite;
            border.color=new Color(.65f,.015f,.01f,0);
        }
        public void BeginGrab(Transform monster)
        {
            if(animating) FinishGrab();
            attacker=monster;injured=true;animating=true;phase=0;
            if(view != null) { restPosition=view.transform.localPosition;restRotation=view.transform.localRotation; }
        }
        public void Tick(float dt, bool suspended, float progress)
        {
            if(canvas != null) canvas.enabled=!suspended;
            if(suspended) return;
            phase+=dt;
            if(border != null) border.color=new Color(.65f,.015f,.01f,injured ? (animating ? .68f : .27f+.035f*Mathf.Sin(phase*2.4f)) : 0f);
            if(!animating || view == null || attacker == null) return;
            float strength=Mathf.Sin(Mathf.Clamp01(progress)*Mathf.PI);
            var target=attacker.position+Vector3.up*1.85f-view.transform.position;
            Quaternion look=target.sqrMagnitude>.01f ? Quaternion.Inverse(view.transform.parent.rotation)*Quaternion.LookRotation(target,Vector3.up) : restRotation;
            float aim=Mathf.SmoothStep(0,1,Mathf.Min(progress*5,(1-progress)*5));
            view.transform.localRotation=Quaternion.Slerp(restRotation,look,aim)*Quaternion.Euler(Mathf.Sin(phase*22)*3*strength,Mathf.Sin(phase*17)*2*strength,Mathf.Sin(phase*19)*5*strength);
            view.transform.localPosition=restPosition+new Vector3(Mathf.Sin(phase*25)*.018f,-.045f*strength,Mathf.Sin(phase*21)*.025f)*strength;
        }
        public void FinishGrab()
        {
            if(animating && view != null) { view.transform.localPosition=restPosition;view.transform.localRotation=restRotation; }
            animating=false;attacker=null;
        }
        public void ResetFeedback() { FinishGrab();injured=false;if(border != null)border.color=new Color(.65f,.015f,.01f,0); }
        private void OnDestroy() { FinishGrab();if(sprite != null)Destroy(sprite);if(texture != null)Destroy(texture); }
    }
}
