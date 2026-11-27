namespace NoRainbowAfter.Narrative
{
    public enum FinalChoice { None, Fight, Hide, Approach }
    public enum StoryEnding { None, Good, Bad, Hidden }

    public static class EndingResolver
    {
        public static StoryEnding Resolve(FinalChoice choice, GameState state)
        {
            // Mendekat menghasilkan Good. Saat melawan atau bersembunyi, pesan yang dikirim
            // ke Ella membuka Hidden. Jika pesan belum dikirim, keduanya menghasilkan Bad.
            if (choice == FinalChoice.Approach) return StoryEnding.Good;
            if (choice != FinalChoice.Fight && choice != FinalChoice.Hide) return StoryEnding.None;
            return state != null && state.EllaMessageSent ? StoryEnding.Hidden : StoryEnding.Bad;
        }
    }
}
