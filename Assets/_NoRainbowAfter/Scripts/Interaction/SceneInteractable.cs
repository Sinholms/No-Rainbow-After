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

    // Runtime-only seam for a future narrative director; no persistence or clue registry.
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
        public string StableId => stableId;
        public string DisplayName => displayName;
        public string Prompt => used && singleUse ? null : prompt;
        public void Configure(string id, string name, string action, string flag, string heading, string text, bool once)
        { stableId = id; displayName = name; prompt = action; objectiveFlag = flag; title = heading; body = text; singleUse = once; }
        public void SetFocused(bool focused) { }
        public void Interact(PlayerInteractor interactor)
        {
            if (used && singleUse) return;
            interactor.Flags.Set(objectiveFlag);
            used = true;
            interactor.ShowInspection(title, body);
        }
    }
}
