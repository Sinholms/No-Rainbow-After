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

    // Session objective flags remain independent of the T05 clue profile.
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
        private bool used;
        private bool available = true;
        public string StableId => stableId;
        public string DisplayName => displayName;
        public string Prompt => !available || (used && singleUse) ? null : prompt;
        public void SetAvailable(bool value) => available = value;
        public void Configure(string id, string name, string action, string flag, string heading, string text, bool once)
        { stableId = id; displayName = name; prompt = action; objectiveFlag = flag; title = heading; body = text; singleUse = once; }
        public void SetFocused(bool focused) { }
        public void Interact(PlayerInteractor interactor)
        {
            if (!available || (used && singleUse)) return;
            interactor.Flags.Set(objectiveFlag);
            used = true;
            interactor.ShowInspection(title, body);
            interactor.NotifyInteraction(stableId);
        }
    }
}
