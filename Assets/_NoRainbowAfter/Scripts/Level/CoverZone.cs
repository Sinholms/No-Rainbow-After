using NoRainbowAfter.Player;
using UnityEngine;

namespace NoRainbowAfter.Level
{
    // Geometry marker only; no encounter visibility or hiding resolution.
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
                if (volume == null) volume = GetComponent<BoxCollider>();
                return isActiveAndEnabled && volume != null && volume.enabled && occupant != null &&
                    occupant.enabled && occupant.gameObject.activeInHierarchy && volume.bounds.Intersects(occupant.bounds);
            }
        }
        public void Configure(string id) { stableId = id; }
        private void OnTriggerEnter(Collider other) { if (other.GetComponent<FirstPersonMotor>() != null) occupant = other; }
        private void OnTriggerExit(Collider other) { if (other == occupant) occupant = null; }
        private void OnDisable() { occupant = null; }
    }
}
