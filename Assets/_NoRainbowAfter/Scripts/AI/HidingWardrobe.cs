using NoRainbowAfter.Interaction;
using NoRainbowAfter.Player;
using UnityEngine;

namespace NoRainbowAfter.AI
{
    public sealed class HidingWardrobe : MonoBehaviour, IInteractable
    {
        public static HidingWardrobe Occupied { get; private set; }
        public string StableId => "C1_WARDROBE_" + name;
        public string DisplayName => "Lemari";
        public string Prompt => Occupied == this ? "Keluar lemari" : "Masuk dan sembunyi";
        public bool Spotted { get; private set; }
        public FirstPersonMotor Occupant { get; private set; }
        private FirstPersonPlayer player;
        public void SetFocused(bool focused) { }
        public void Interact(PlayerInteractor interactor)
        {
            if (Occupied == this) { TryExit(); return; }
            if (Occupied != null) return;
            var actor=interactor.GetComponent<FirstPersonPlayer>();
            var motor=interactor.GetComponent<FirstPersonMotor>();
            if(actor == null || motor == null || actor.IsPaused || actor.IsInspecting) return;
            // Catat apakah pemain terlihat sebelum dipindah ke dalam lemari.
            var monster=FindAnyObjectByType<ShadowEncounterController>();
            bool spotted=monster != null && monster.State==ShadowEncounterState.Pursue &&
                Vector3.Distance(monster.transform.position,motor.transform.position)<3.1f &&
                ShadowVisibility.HasAnyClearSightlineToPlayer(monster.transform.position+Vector3.up*1.6f,motor,monster.transform);
            if(!motor.TryRestorePose(transform.TransformPoint(new Vector3(0,.06f,0)),transform.rotation*Quaternion.Euler(0,180,0),false)) return;
            Occupant=motor; player=actor; Spotted=spotted; Occupied=this; player.SetHiding(true);
            motor.PoseRestored += Release;
            player.InteractRequested += OnExitRequested;
        }
        private void OnExitRequested() { if(player != null && !player.IsPaused && !player.IsInspecting && !player.IsBreathing) TryExit(); }
        public bool TryExit()
        {
            if(Occupant == null) { Release(); return true; }
            // Pertahankan status sembunyi bila collider menghalangi titik keluar.
            if(!Occupant.TryRestorePose(transform.TransformPoint(new Vector3(0,.06f,-1.35f)),transform.rotation*Quaternion.Euler(0,180,0),false)) return false;
            Release(); return true;
        }
        public void Release()
        {
            if(Occupant != null) Occupant.PoseRestored -= Release;
            if(player != null) { player.InteractRequested -= OnExitRequested; player.SetHiding(false); }
            Occupant=null;player=null;Spotted=false;
            if(Occupied==this) Occupied=null;
        }
        private void OnDisable() => Release();
    }
}
