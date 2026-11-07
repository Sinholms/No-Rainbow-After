using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using UnityEngine;

namespace NoRainbowAfter.Narrative
{
    public enum EncounterResponse { None, Fight, Hide }
    public enum ClueStorageStatus { Ready, InvalidOrUnsupported, ReadFailed, WriteFailed }
    [Serializable] internal sealed class ClueDocument { public int schemaVersion; public string[] clueIds; }

    // This stores clues only, not chapter progress or a complete save game.
    public sealed class GameState
    {
        private readonly HashSet<string> clues = new HashSet<string>(StringComparer.Ordinal);
        private readonly string path;
        private readonly HashSet<string> objectives = new HashSet<string>(StringComparer.Ordinal);
        private bool protectedFile;
        public IReadOnlyCollection<string> ClueIds => new ReadOnlyCollection<string>(new List<string>(clues));
        public EncounterResponse Chapter1Response { get; internal set; }
        public string ActiveChapter => "Chapter1";
        public IReadOnlyCollection<string> CompletedObjectives => new ReadOnlyCollection<string>(new List<string>(objectives));
        public bool HasObjective(string id) => objectives.Contains(id);
        internal bool CompleteObjective(string id) => objectives.Add(id);
        public int Chapter1Beat { get; internal set; }
        public int LoopIndex { get; internal set; }
        public ClueStorageStatus StorageStatus { get; private set; }
        public GameState(string storagePath) { path = storagePath; Load(); }
        public bool HasClue(string id) => clues.Contains(id);
        private static bool Valid(string id) => id == "CLUE_01" || id == "CLUE_02" || id == "CLUE_03" || id == "CLUE_04";
        public bool AddClue(string id)
        {
            if (!Valid(id) || !clues.Add(id)) return false;
            Save();
            return true;
        }
        // Explicit future New Game hook; never called by scene load or checkpoint restore.
        public void ResetClues() { clues.Clear(); if (!protectedFile) Save(); }
        private void Load()
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
                var doc = JsonUtility.FromJson<ClueDocument>(File.ReadAllText(path));
                if (doc == null || doc.schemaVersion != 1 || doc.clueIds == null)
                { protectedFile = true; StorageStatus = ClueStorageStatus.InvalidOrUnsupported; return; }
                foreach (var id in doc.clueIds) if (Valid(id)) clues.Add(id);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            { protectedFile = true; StorageStatus = ClueStorageStatus.ReadFailed; Debug.LogWarning("[NRA] Clue storage read: " + e.Message); }
        }
        private void Save()
        {
            if (protectedFile || string.IsNullOrEmpty(path)) return;
            string temp = path + ".tmp";
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var ids = new List<string>(clues); ids.Sort(StringComparer.Ordinal);
                File.WriteAllText(temp, JsonUtility.ToJson(new ClueDocument { schemaVersion = 1, clueIds = ids.ToArray() }));
                if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
                StorageStatus = ClueStorageStatus.Ready;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is NotSupportedException)
            { StorageStatus = ClueStorageStatus.WriteFailed; Debug.LogWarning("[NRA] Clue storage write: " + e.Message); }
        }
    }
}
