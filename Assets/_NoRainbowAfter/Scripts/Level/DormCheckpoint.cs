using System;
using NoRainbowAfter.Player;
using UnityEngine;

namespace NoRainbowAfter.Level
{
    // Session-local recovery; objectives and interactables remain untouched.
    public sealed class DormCheckpoint : MonoBehaviour
    {
        [SerializeField] private FirstPersonMotor motor;
        [SerializeField] private Transform spawn;
        private Func<bool> chapterRestore;
        public void Configure(FirstPersonMotor playerMotor, Transform spawnPoint) { motor = playerMotor; spawn = spawnPoint; }
        public void SetChapterRestore(Func<bool> restore) => chapterRestore = restore;
        public bool TryRestore() => chapterRestore != null ? chapterRestore() : TryRestoreDorm();
        public bool TryRestoreDorm() => motor != null && spawn != null && motor.TryRestorePose(spawn.position, spawn.rotation);
    }
}
