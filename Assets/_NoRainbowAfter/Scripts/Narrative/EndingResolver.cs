namespace NoRainbowAfter.Narrative
{
    public enum FinalChoice { None, Fight, Hide, Approach }
    public enum StoryEnding { None, Good, Bad, Hidden }

    public static class EndingResolver
    {
        public static StoryEnding Resolve(FinalChoice choice, GameState state)
        {
            // Approach selalu Good. Fight/Hide menjadi Hidden hanya jika pesan Ella terkirim dan CLUE_01..04 ada;
            // jika tidak, hasilnya Bad. None atau pilihan tak dikenal belum menghasilkan ending.
            if (choice == FinalChoice.Approach) return StoryEnding.Good;
            if (choice != FinalChoice.Fight && choice != FinalChoice.Hide) return StoryEnding.None;
            return state != null && state.EllaMessageSent && state.HasClue("CLUE_01") &&
                state.HasClue("CLUE_02") && state.HasClue("CLUE_03") && state.HasClue("CLUE_04")
                ? StoryEnding.Hidden : StoryEnding.Bad;
        }
    }
}
