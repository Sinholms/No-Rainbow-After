using System;
using NoRainbowAfter.Player;
using UnityEngine;

namespace NoRainbowAfter.Level
{
    public sealed class DormCheckpoint : MonoBehaviour
    {
        [SerializeField] private FirstPersonMotor motor;
        [SerializeField] private Transform spawn;
        // In the dorm root's space, from the room floor to the doorway at z = 5.08.
        [SerializeField] private Bounds roomBounds = new Bounds(new Vector3(0f, 1.2f, 2.54f), new Vector3(4f, 3f, 5.08f));
        private Func<bool> chapterRestore;
        private FirstPersonPlayer player;
        private StressBarometer stress;

        public bool IsInOwnRoom => motor != null && spawn != null && spawn.parent != null &&
            roomBounds.Contains(spawn.parent.InverseTransformPoint(motor.transform.position));

        private void OnEnable()
        {
            player = GetComponent<FirstPersonPlayer>();
            stress = GetComponent<StressBarometer>();
            if (stress != null) stress.StressChanged += UpdateRoomRecovery;
            if (motor != null) motor.PoseRestored += RefreshRoomRecovery;
            RefreshRoomRecovery();
        }

        private void OnDisable()
        {
            if (stress != null) stress.StressChanged -= UpdateRoomRecovery;
            if (motor != null) motor.PoseRestored -= RefreshRoomRecovery;
            if (player != null && player.IsBreathRecoveryRequired) player.SetBreathRecoveryRequired(false);
        }

        private void LateUpdate()
        {
            if (stress != null && stress.CurrentStress >= StressBarometer.MaxStress) RefreshRoomRecovery();
        }

        private void RefreshRoomRecovery()
        {
            if (stress != null) UpdateRoomRecovery(stress.CurrentStress);
        }

        private void UpdateRoomRecovery(float value)
        {
            if (player != null)
                player.SetBreathRecoveryRequired(value >= StressBarometer.MaxStress && IsInOwnRoom);
        }

        public void Configure(FirstPersonMotor playerMotor, Transform spawnPoint) { motor = playerMotor; spawn = spawnPoint; }
        public void SetChapterRestore(Func<bool> restore) => chapterRestore = restore;
        public bool TryRestore() => chapterRestore != null ? chapterRestore() : TryRestoreDorm();
        public bool TryRestoreDorm() => motor != null && spawn != null && motor.TryRestorePose(spawn.position, spawn.rotation);
    }
}
