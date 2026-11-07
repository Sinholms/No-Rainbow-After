using NoRainbowAfter.Player;
using UnityEngine;

namespace NoRainbowAfter.Level
{
    // Session-local recovery; objectives and interactables remain untouched.
    public sealed class DormCheckpoint : MonoBehaviour
    {
        [SerializeField] private FirstPersonMotor motor;
        [SerializeField] private Transform spawn;
        public void Configure(FirstPersonMotor playerMotor, Transform spawnPoint) { motor = playerMotor; spawn = spawnPoint; }
        public bool TryRestore() => motor != null && spawn != null && motor.TryRestorePose(spawn.position, spawn.rotation);
    }
}
