using UnityEngine;

namespace NoRainbowAfter.Core
{
    public sealed class BootEntry : MonoBehaviour
    {
        private void Start() => SceneFlow.EnsureInstance().Load(GameScene.MainMenu);
    }
}
