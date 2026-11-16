using UnityEngine;

namespace NoRainbowAfter.Quality
{
    /// <summary>
    /// Continuously spins the electric fan rotor blades around their local motor shaft axis.
    /// </summary>
    public sealed class FanBladeSpinner : MonoBehaviour
    {
        [SerializeField] private float degreesPerSecond = 960f;
        private Vector3 localSpinAxis = Vector3.up;
        private bool axisResolved;

        private void Awake() => ResolveAxis();

        private void ResolveAxis()
        {
            if (axisResolved) return;
            var mf = GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Vector3 sz = mf.sharedMesh.bounds.size;
                if (sz.x <= sz.y && sz.x <= sz.z) localSpinAxis = Vector3.right;
                else if (sz.y <= sz.x && sz.y <= sz.z) localSpinAxis = Vector3.up;
                else localSpinAxis = Vector3.forward;
                axisResolved = true;
            }
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            ResolveAxis();
            transform.Rotate(localSpinAxis, degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
