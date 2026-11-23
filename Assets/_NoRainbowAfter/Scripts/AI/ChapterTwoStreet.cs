using System;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Player;
using UnityEngine;

namespace NoRainbowAfter.AI
{
    public sealed class ChapterTwoStreet : MonoBehaviour
    {
        public Transform[] Lamps, Checkpoints;
        public Transform PatrolCar, BusSeat;
        public BoxCollider Shelter;
        public SceneInteractable Bag;
        public ChapterTwoBusDoor BusDoor;
        public event Action SafeZoneReached;
        public event Action BoardRequested;
        public ChapterTwoSpotlightEncounter Encounter { get; private set; }
        public int Segment { get; private set; }
        public bool AtShelter { get; private set; }
        public bool Boarded { get; private set; }
        private FirstPersonMotor motor;
        private FirstPersonPlayer player;
        private AudioSource siren;
        private Light[] beacons;

        public void Stage()
        {
            transform.position = new Vector3(240f, 0f, 180f);
            gameObject.SetActive(true);
            beacons ??= PatrolCar.GetComponentsInChildren<Light>();
            Physics.SyncTransforms();
        }

        public void PlaySiren(float volume)
        {
            Stage();
            if (siren == null)
            {
                siren = gameObject.AddComponent<AudioSource>();
                siren.playOnAwake = false;
                siren.loop = true;
                siren.clip = Resources.Load<AudioClip>("ChapterTwoAudio/TKPSiren");
            }
            siren.volume = volume;
            if (siren.clip != null && !siren.isPlaying) siren.Play();
        }

        public bool Enter(FirstPersonMotor target)
        {
            if (motor != null) return true;
            if (!PlacePlayer(target, Checkpoints[0])) return false;
            Segment = 0;
            AtShelter = Boarded = false;
            Bag.SetAvailable(false);
            BusDoor.Bind(this);
            Encounter = gameObject.AddComponent<ChapterTwoSpotlightEncounter>();
            Encounter.Configure(target, Lamps, Checkpoints);
            Encounter.ConfigureBeam(0, 42f, 14f);
            Encounter.ConfigureBeam(1, 42f, 14f);
            Encounter.ConfigureSweep(0, 27f, 7.6f);
            Encounter.ConfigureSweep(1, 25f, 8.6f, 1.3f);
            return true;
        }

        internal bool EnterAtBus(FirstPersonMotor target)
        {
            if (!PlacePlayer(target, BusSeat)) return false;
            AtShelter = Boarded = true;
            Bag.SetAvailable(false);
            return true;
        }

        private bool PlacePlayer(FirstPersonMotor target, Transform spawn)
        {
            Stage();
            if (!target.TryRestorePose(spawn.position, spawn.rotation)) return false;
            motor = target;
            player = target.GetComponent<FirstPersonPlayer>();
            return true;
        }

        private void Update()
        {
            bool paused = player != null && player.IsPaused;
            if (siren != null && paused && siren.isPlaying) siren.Pause();
            if (siren != null && !paused && !Boarded && !siren.isPlaying) siren.UnPause();
            if (paused) return;
            if (beacons != null)
            {
                float phase = Mathf.Sin(Time.time * 3.1f);
                for (int i = 0; i < beacons.Length; i++)
                    beacons[i].intensity = 2.2f + phase * (i == 0 ? 1.05f : -1.05f);
            }
            if (player == null || !player.IsTransitionLocked) CheckProgress();
        }

        public void CheckProgress()
        {
            if (motor == null || AtShelter) return;
            Vector3 feet = motor.transform.position;
            if (Segment + 1 < Checkpoints.Length &&
                transform.InverseTransformPoint(feet).z >= Checkpoints[Segment + 1].localPosition.z)
                Encounter.SetSegment(++Segment);
            if (!Shelter.bounds.Contains(feet + Vector3.up * .5f)) return;

            AtShelter = true;
            Encounter.enabled = false;
            Bag.SetAvailable(true);
            SafeZoneReached?.Invoke();
        }

        internal void RequestBoard()
        {
            if (AtShelter && !Boarded) BoardRequested?.Invoke();
        }

        public bool TryBoard()
        {
            if (!AtShelter || Boarded || !motor.TryRestorePose(BusSeat.position, BusSeat.rotation)) return false;
            Boarded = true;
            if (siren != null) siren.Stop();
            return true;
        }

        public bool TryRestoreCheckpoint()
        {
            if (motor == null) return false;
            if (Boarded) return motor.TryRestorePose(BusSeat.position, BusSeat.rotation);
            if (!AtShelter) return Encounter.TryRestoreCheckpoint();
            Vector3 entrance = Shelter.transform.TransformPoint(new Vector3(0, -1.01f, -.92f));
            return motor.TryRestorePose(entrance, Shelter.transform.rotation);
        }

        public void Leave()
        {
            motor = null;
            player = null;
            if (siren != null) siren.Stop();
            if (Encounter != null) Destroy(Encounter);
            gameObject.SetActive(false);
        }
    }
}
