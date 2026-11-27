using UnityEngine;

namespace NoRainbowAfter.Core
{
    public sealed class BootEntry : MonoBehaviour
    {
        // Boot menyiapkan pengelola perpindahan scene, lalu membuka menu tempat pemain memilih Mulai Baru.
        private void Start() => SceneFlow.EnsureInstance().Load(GameScene.MainMenu);
    }
}
