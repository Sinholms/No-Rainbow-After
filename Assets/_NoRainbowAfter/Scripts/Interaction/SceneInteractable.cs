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

    // These props can be used through the normal raycast without revealing a prompt.
    public interface IHiddenInteractable : IInteractable { }

    // Flag seperti objective.laptop memberi tahu sistem cerita bahwa tugas sudah selesai dalam sesi ini.
    public interface IObjectiveFlags { bool Set(string flag); bool Contains(string flag); }
    public sealed class SessionObjectiveFlags : IObjectiveFlags
    {
        private readonly HashSet<string> flags = new HashSet<string>();
        public bool Set(string flag) => flags.Add(flag);
        public bool Contains(string flag) => flags.Contains(flag);
    }

    public sealed class SceneInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string stableId, displayName, prompt, objectiveFlag, title;
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
            stableId = id; displayName = name; prompt = action; objectiveFlag = flag; title = heading; body = text;
            singleUse = once; inspectionPhoto = photo;
        }
        public void SetFocused(bool focused) { }
        public void Interact(PlayerInteractor interactor)
        {
            if (!available || (used && singleUse)) return;
            interactor.Flags.Set(objectiveFlag);
            used = true;
            interactor.ShowInspection(title, body, inspectionPhoto);
            interactor.NotifyInteraction(stableId);
        }
    }
}
