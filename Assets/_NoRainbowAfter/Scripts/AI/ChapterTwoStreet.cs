using System;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Player;
using UnityEngine;

namespace NoRainbowAfter.AI
{
    public sealed class ChapterTwoStreet : MonoBehaviour
    {
        public Transform[] Lamps, ShadowWrappers, Checkpoints;
        public BoxCollider Shelter;
        public SceneInteractable Bag;
        public event Action SafeZoneReached;
        public ChapterTwoSpotlightEncounter Encounter { get; private set; }
        public int Segment { get; private set; }
        public bool AtShelter { get; private set; }
        private FirstPersonMotor motor;

        public bool Enter(FirstPersonMotor target)
        {
            if (motor != null) return true;
            transform.position = new Vector3(240f, 0f, 180f);
            gameObject.SetActive(true);
            Physics.SyncTransforms();
            if (!target.TryRestorePose(Checkpoints[0].position, Checkpoints[0].rotation, false))
            {
                gameObject.SetActive(false);
                return false;
            }
            motor = target;
            Segment = 0;
            AtShelter = false;
            Bag.SetAvailable(false);
            Encounter = gameObject.AddComponent<ChapterTwoSpotlightEncounter>();
            Encounter.Configure(target, Lamps, ShadowWrappers, Checkpoints);
            for (int i = 0; i < Lamps.Length; i++)
                Encounter.ConfigureBeam(i, ShadowWrappers[i] == null ? 29f : 38f, ShadowWrappers[i] == null ? 13f : 12f);
            Encounter.ConfigureSweep(2, 45f, 8f, Vector3.zero);
            Encounter.ConfigureSweep(3, 38f, 7f, transform.forward * 3.8f);
            return true;
        }

        private void Update() => CheckProgress();

        public void CheckProgress()
        {
            if (motor == null || AtShelter) return;
            Vector3 feet = motor.transform.position;
            if (Segment + 1 < Checkpoints.Length &&
                transform.InverseTransformPoint(feet).z >= Checkpoints[Segment + 1].localPosition.z)
                Encounter.SetSegment(++Segment);
            if (Segment != 2 || !Shelter.bounds.Contains(feet + Vector3.up * .5f)) return;
            AtShelter = true;
            Encounter.enabled = false;
            Bag.SetAvailable(true);
            SafeZoneReached?.Invoke();
        }

        public bool TryRestoreCheckpoint()
        {
            if (motor == null) return false;
            if (!AtShelter) return Encounter.TryRestoreCheckpoint();
            Vector3 entrance = Shelter.transform.TransformPoint(new Vector3(0, -1.01f, -.92f));
            return motor.TryRestorePose(entrance, Shelter.transform.rotation, false);
        }

        public void Leave()
        {
            motor = null;
            if (Encounter != null) Destroy(Encounter);
            gameObject.SetActive(false);
        }
    }
}
