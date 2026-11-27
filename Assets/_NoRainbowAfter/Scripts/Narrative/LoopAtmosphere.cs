using NoRainbowAfter.Player;
using UnityEngine;

namespace NoRainbowAfter.Narrative
{
    /// <summary>
    /// Menggeser posisi/rotasi catatan dan slip secara halus, meredupkan lampu lorong,
    /// serta menebalkan RenderSettings.fogDensity di setiap loop Chapter 1.
    ///
    /// [GRAFIKA KOMPUTER]
    /// Menggunakan interpolasi eksponensial (1 - exp(-dt * k)) agar perubahan kepadatan kabut,
    /// warna lampu, dan pergeseran objek sekitar terasa mulus di berbagai frame rate.
    /// </summary>
    public sealed class LoopAtmosphere : MonoBehaviour
    {
        [SerializeField] private ChapterDirector director;
        [SerializeField] private Transform[] notes;
        [SerializeField] private Transform[] slips;
        [SerializeField] private Transform notePivot;
        [SerializeField] private Transform slipPivot;
        [SerializeField] private Light corridorLight;

        private Vector3[] notePositions;
        private Vector3[] slipPositions;
        private Quaternion[] noteRotations;
        private Quaternion[] slipRotations;
        private Vector3 noteOrigin;
        private Vector3 slipOrigin;
        private float intensity;
        private float fog;
        private float progress;
        private float target;
        private Color lightColor;
        private bool captured;

        public void Configure(ChapterDirector source, Transform[] noteParts, Transform[] slipParts,
            Transform notesPivot, Transform slipsPivot, Light lightSource)
        {
            Unsubscribe();
            Restore();
            director = source;
            notes = noteParts;
            slips = slipParts;
            notePivot = notesPivot;
            slipPivot = slipsPivot;
            corridorLight = lightSource;
            if (isActiveAndEnabled)
            {
                Setup();
            }
        }

        private void OnEnable() => Setup();

        private void OnDisable()
        {
            Unsubscribe();
            Restore();
        }

        private void Setup()
        {
            if (captured)
            {
                return;
            }
            Capture(notes, out notePositions, out noteRotations);
            Capture(slips, out slipPositions, out slipRotations);
            noteOrigin = notePivot != null ? notePivot.position : Vector3.zero;
            slipOrigin = slipPivot != null ? slipPivot.position : Vector3.zero;
            if (corridorLight != null)
            {
                intensity = corridorLight.intensity;
                lightColor = corridorLight.color;
            }
            fog = RenderSettings.fogDensity;
            captured = true;
            progress = 0;

            if (director != null)
            {
                director.LoopChanged += Change;
                Change(director.LoopIndex);
            }
        }

        private static void Capture(Transform[] parts, out Vector3[] positions, out Quaternion[] rotations)
        {
            int count = parts == null ? 0 : parts.Length;
            positions = new Vector3[count];
            rotations = new Quaternion[count];
            for (int i = 0; i < count; i++)
            {
                if (parts[i] != null)
                {
                    positions[i] = parts[i].position;
                    rotations[i] = parts[i].rotation;
                }
            }
        }

        private void Unsubscribe()
        {
            if (director != null)
            {
                director.LoopChanged -= Change;
            }
        }

        private void Change(int index) => target = Mathf.Clamp(index, 0, 3);

        private void Update()
        {
            if (!captured)
            {
                return;
            }
            var player = GetComponent<FirstPersonPlayer>();
            if (player != null && (player.IsPaused || player.IsInspecting))
            {
                return;
            }

            float t = 1f - Mathf.Exp(-Time.deltaTime * 1.2f);
            progress = Mathf.Lerp(progress, target, t);
            Apply(notes, notePositions, noteRotations, noteOrigin, .03f * progress, 4f * progress);
            Apply(slips, slipPositions, slipRotations, slipOrigin, .03f * Mathf.Max(0, progress - 1), 0);

            if (corridorLight != null)
            {
                float factor = progress <= 1
                    ? Mathf.Lerp(1, .92f, progress)
                    : progress <= 2
                        ? Mathf.Lerp(.92f, .83f, progress - 1)
                        : Mathf.Lerp(.83f, .74f, progress - 2);
                corridorLight.intensity = intensity * factor;
                corridorLight.color = Color.Lerp(lightColor, new Color(.75f, .83f, 1f), .07f * progress);
            }

            float fogFactor = progress <= 1
                ? Mathf.Lerp(1, 1.12f, progress)
                : progress <= 2
                    ? Mathf.Lerp(1.12f, 1.25f, progress - 1)
                    : Mathf.Lerp(1.25f, 1.38f, progress - 2);
            RenderSettings.fogDensity = fog * fogFactor;
        }

        private static void Apply(Transform[] parts, Vector3[] positions, Quaternion[] rotations,
            Vector3 pivot, float offset, float yaw)
        {
            if (parts == null)
            {
                return;
            }
            Quaternion turn = Quaternion.AngleAxis(yaw, Vector3.up);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] != null)
                {
                    parts[i].SetPositionAndRotation(
                        pivot + turn * (positions[i] - pivot) + Vector3.right * offset,
                        turn * rotations[i]);
                }
            }
        }

        private void Restore()
        {
            if (!captured)
            {
                return;
            }
            Apply(notes, notePositions, noteRotations, noteOrigin, 0, 0);
            Apply(slips, slipPositions, slipRotations, slipOrigin, 0, 0);
            if (corridorLight != null)
            {
                corridorLight.intensity = intensity;
                corridorLight.color = lightColor;
            }
            RenderSettings.fogDensity = fog;
            captured = false;
        }
    }
}
