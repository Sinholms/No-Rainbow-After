using System.Collections.Generic;
using UnityEngine;

namespace NoRainbowAfter.Interaction
{
    public interface IInteractable
    {
        string StableId { get; }
        string DisplayName { get; }
        string Prompt { get; }
        void SetFocused(bool focused);
        void Interact(PlayerInteractor interactor);
    }

    /// <summary>Properti interaktif tersembunyi yang tetap dapat di-raycast tanpa menampilkan prompt di crosshair.</summary>
    public interface IHiddenInteractable : IInteractable { }

    /// <summary>Menyimpan flag objektif yang telah diselesaikan pemain pada sesi berjalan.</summary>
    public interface IObjectiveFlags
    {
        bool Set(string flag);
        bool Contains(string flag);
    }

    public sealed class SessionObjectiveFlags : IObjectiveFlags
    {
        private readonly HashSet<string> flags = new HashSet<string>();
        public bool Set(string flag) => flags.Add(flag);
        public bool Contains(string flag) => flags.Contains(flag);
    }

    /// <summary>Komponen properti dunia yang dapat diperiksa untuk menampilkan teks/foto inspeksi dan memicu objectiveFlag.</summary>
    public sealed class SceneInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string stableId;
        [SerializeField] private string displayName;
        [SerializeField] private string prompt;
        [SerializeField] private string objectiveFlag;
        [SerializeField] private string title;
        [SerializeField, TextArea(3, 8)] private string body;
        [SerializeField] private bool singleUse;

        private Texture2D inspectionPhoto;
        private bool used;
        private bool available = true;

        public string StableId => stableId;
        public string DisplayName => displayName;
        public string Prompt => !available || (used && singleUse) ? null : prompt;

        public void SetAvailable(bool value) => available = value;

        public void Configure(string id, string name, string action, string flag, string heading, string text, bool once, Texture2D photo = null)
        {
            stableId = id;
            displayName = name;
            prompt = action;
            objectiveFlag = flag;
            title = heading;
            body = text;
            singleUse = once;
            inspectionPhoto = photo;
        }

        public void SetFocused(bool focused) { }

        public void Interact(PlayerInteractor interactor)
        {
            if (!available || (used && singleUse))
            {
                return;
            }
            interactor.Flags.Set(objectiveFlag);
            used = true;
            interactor.ShowInspection(title, body, inspectionPhoto);
            interactor.NotifyInteraction(stableId);
        }
    }
}
