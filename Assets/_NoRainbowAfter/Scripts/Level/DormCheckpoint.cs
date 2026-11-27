using System;
using NoRainbowAfter.Player;
using UnityEngine;

namespace NoRainbowAfter.Level
{
    /// <summary>
    /// Menyimpan titik pemulihan (checkpoint) di kamar kos atau mendelegasikan pemulihan ke
    /// pengatur bab aktif (chapterRestore), serta mewajibkan latihan napas saat stres penuh di kamar.
    /// </summary>
    public sealed class DormCheckpoint : MonoBehaviour
    {
        [SerializeField] private FirstPersonMotor motor;
        [SerializeField] private Transform spawn;

        // Batas kamar dalam ruang lokal DormRoot: dari lantai kamar hingga ambang pintu di z = 5.08.
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
            if (player != null && player.IsBreathRecoveryRequired)
            {
                player.SetBreathRecoveryRequired(false);
            }
        }

        private void LateUpdate()
        {
            if (stress != null && stress.CurrentStress >= StressBarometer.MaxStress)
            {
                RefreshRoomRecovery();
            }
        }

        private void RefreshRoomRecovery()
        {
            if (stress != null)
            {
                UpdateRoomRecovery(stress.CurrentStress);
            }
        }

        private void UpdateRoomRecovery(float value)
        {
            // Saat stres mencapai maksimum di kamar kos, pemain wajib mengatur napas sebelum bergerak lagi.
            if (player != null)
            {
                player.SetBreathRecoveryRequired(value >= StressBarometer.MaxStress && IsInOwnRoom);
            }
        }

        public void Configure(FirstPersonMotor playerMotor, Transform spawnPoint)
        {
            motor = playerMotor;
            spawn = spawnPoint;
        }

        public void SetChapterRestore(Func<bool> restore) => chapterRestore = restore;

        // Gunakan pemulihan bab bila tersedia; jika tidak, pulihkan pose ke checkpoint kamar.
        public bool TryRestore() => chapterRestore != null ? chapterRestore() : TryRestoreDorm();

        public bool TryRestoreDorm() =>
            motor != null && spawn != null && motor.TryRestorePose(spawn.position, spawn.rotation);
    }
}
