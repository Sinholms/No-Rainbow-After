using UnityEngine;

namespace NoRainbowAfter.Core
{
    /// <summary>Titik masuk awal di scene Boot: memastikan SceneFlow ada lalu membuka MainMenu.</summary>
    public sealed class BootEntry : MonoBehaviour
    {
        private void Start() => SceneFlow.EnsureInstance().Load(GameScene.MainMenu);
    }
}
