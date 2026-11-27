using NoRainbowAfter.Player;
using UnityEngine;

namespace NoRainbowAfter.Level
{
    /// <summary>
    /// Penanda area geometris persembunyian (cover) di lorong Chapter 1.
    /// Hanya memeriksa apakah collider pemain beririsan dengan BoxCollider zona ini.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CoverZone : MonoBehaviour
    {
        [SerializeField] private string stableId;
        private Collider occupant;
        private BoxCollider volume;

        public string StableId => stableId;

        public bool PlayerInside
        {
            get
            {
                // Trigger memberi kandidat penghuni; irisan bounds menjadi cek aktual untuk checkpoint/encounter.
                if (volume == null) volume = GetComponent<BoxCollider>();
                return isActiveAndEnabled &&
                       volume != null &&
                       volume.enabled &&
                       occupant != null &&
                       occupant.enabled &&
                       occupant.gameObject.activeInHierarchy &&
                       volume.bounds.Intersects(occupant.bounds);
            }
        }

        public void Configure(string id) => stableId = id;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<FirstPersonMotor>() != null)
            {
                occupant = other;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == occupant)
            {
                occupant = null;
            }
        }

        private void OnDisable() => occupant = null;
    }
}
