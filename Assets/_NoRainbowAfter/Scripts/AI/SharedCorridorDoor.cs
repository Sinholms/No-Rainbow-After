using System;
using NoRainbowAfter.Interaction;
using NoRainbowAfter.Player;
using UnityEngine;

namespace NoRainbowAfter.AI
{
    public sealed class SharedCorridorDoor : MonoBehaviour, IInteractable
    {
        private const float Threshold = .28f;
        private FirstPersonPlayer player;
        private Transform hinge;
        private Material metal;
        private Func<bool> canExit;
        private Action crossed;
        private float previousZ;
        private float angle;

        public string StableId => "INT_KOS_EXIT";
        public string DisplayName => "Pintu keluar kos";
        public string Prompt => IsOpen ? null : canExit() ? "Buka pintu keluar" : "Pintu terkunci";
        public bool IsOpen { get; private set; }

        public static SharedCorridorDoor ReplaceEndWall(FirstPersonPlayer owner, Func<bool> gate, Action exit)
        {
            var wall = GameObject.Find("SM_ENV_CorridorEnd");
            var renderer = wall != null ? wall.GetComponent<Renderer>() : null;
            if (renderer == null) return null;

            Bounds bounds = renderer.bounds;
            var host = new GameObject("Pintu Ujung Lorong", typeof(SharedCorridorDoor));
            host.transform.SetParent(wall.transform.parent, false);
            host.transform.SetPositionAndRotation(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z), Quaternion.identity);
            var door = host.GetComponent<SharedCorridorDoor>();
            door.player = owner;
            door.canExit = gate;
            door.crossed = exit;
            door.Build(bounds.size, renderer.sharedMaterial);
            wall.SetActive(false);
            door.ResetDoor();
            return door;
        }

        private void Build(Vector3 wallSize, Material plaster)
        {
            metal = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.115f, .17f, .18f) };
            metal.SetFloat("_Metallic", .6f);
            metal.SetFloat("_Smoothness", .46f);

            const float opening = 1.20f;
            const float head = 2.17f;
            float sideWidth = (wallSize.x - opening) / 2f;
            foreach (int side in new[] { -1, 1 })
            {
                Cube(transform, "Dinding samping", new Vector3(side * (opening + sideWidth) / 2f, wallSize.y / 2f, 0),
                    new Vector3(sideWidth, wallSize.y, wallSize.z), plaster);
                Cube(transform, "Kusen", new Vector3(side * .605f, head / 2f, -.045f),
                    new Vector3(.09f, head, .22f), metal);
                Cube(transform, "Sisi ambang", new Vector3(side * .66f, 1.10f, .49f),
                    new Vector3(.17f, 2.2f, 1.02f), plaster);
            }
            Cube(transform, "Dinding atas", new Vector3(0, (wallSize.y + head) / 2f, 0),
                new Vector3(opening, wallSize.y - head, wallSize.z), plaster);
            Cube(transform, "Kusen atas", new Vector3(0, head, -.045f), new Vector3(1.26f, .09f, .22f), metal);
            Cube(transform, "Pijakan luar", new Vector3(0, -.075f, .49f), new Vector3(1.37f, .15f, 1.05f), plaster);
            Cube(transform, "Dinding luar", new Vector3(0, 1.10f, .97f), new Vector3(1.35f, 2.2f, .13f), plaster);
            Cube(transform, "Plafon ambang", new Vector3(0, 2.21f, .49f), new Vector3(1.35f, .12f, 1.02f), plaster);

            hinge = new GameObject("Engsel").transform;
            hinge.SetParent(transform, false);
            hinge.localPosition = new Vector3(-.56f, 0, 0);
            Cube(hinge, "Daun pintu logam", new Vector3(.56f, 1.06f, 0), new Vector3(1.12f, 2.12f, .095f), metal);
            Cube(hinge, "Panel pintu", new Vector3(.56f, 1.29f, -.052f), new Vector3(.86f, 1.30f, .012f), metal);
            Cube(hinge, "Plat gagang", new Vector3(.97f, 1.05f, -.06f), new Vector3(.10f, .24f, .025f), metal);
            Cube(hinge, "Gagang", new Vector3(.93f, 1.08f, -.115f), new Vector3(.16f, .046f, .085f), metal);
        }

        private static void Cube(Transform parent, string label, Vector3 position, Vector3 dimensions, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = label;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = dimensions;
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        public void SetFocused(bool focused) { }

        public void Interact(PlayerInteractor interactor)
        {
            if (IsOpen || !canExit()) return;
            IsOpen = true;
            previousZ = transform.InverseTransformPoint(player.transform.position).z;
        }

        public void ResetDoor()
        {
            IsOpen = false;
            angle = 0f;
            hinge.localRotation = Quaternion.identity;
            previousZ = transform.InverseTransformPoint(player.transform.position).z;
        }

        private void LateUpdate()
        {
            if (!IsOpen || player.IsPaused || player.IsTransitionLocked) return;
            angle = Mathf.MoveTowards(angle, 92f, 235f * Time.deltaTime);
            hinge.localRotation = Quaternion.Euler(0f, -angle, 0f);
            Vector3 feet = transform.InverseTransformPoint(player.transform.position);
            bool steppedThrough = previousZ < Threshold && feet.z >= Threshold && feet.z < 1f &&
                Mathf.Abs(feet.x) < .62f && feet.y > -.15f && feet.y < .45f;
            previousZ = feet.z;
            if (steppedThrough && canExit()) crossed();
        }

        private void OnDestroy()
        {
            if (metal != null) Destroy(metal);
        }
    }
}
