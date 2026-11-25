using NoRainbowAfter.Interaction;
using UnityEngine;

namespace NoRainbowAfter.AI
{
    public sealed class ChapterTwoBusDoor : MonoBehaviour, IInteractable
    {
        private ChapterTwoStreet street;
        public string StableId => "INT_C2_BUS";
        public string DisplayName => "Bus pasar";
        public string Prompt => street != null && street.AtShelter && !street.Boarded ? "Naik bus" : null;

        internal void Bind(ChapterTwoStreet owner) => street = owner;
        public void SetFocused(bool focused) { }
        public void Interact(PlayerInteractor interactor) => street?.RequestBoard();
    }
}
