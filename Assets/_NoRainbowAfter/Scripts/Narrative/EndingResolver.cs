namespace NoRainbowAfter.Narrative
{
    public enum FinalChoice
    {
        None,
        Fight,
        Hide,
        Approach
    }

    public enum StoryEnding
    {
        None,
        Good,
        Bad,
        Hidden
    }

    /// <summary>
    /// Penentu cabang akhir cerita di Chapter 3 berdasarkan pilihan akhir Raka serta
    /// apakah Raka sempat mengirim pesan ke Ella pada Chapter 2.
    /// </summary>
    public static class EndingResolver
    {
        public static StoryEnding Resolve(FinalChoice choice, GameState state)
        {
            // Mendekat (Approach) menghasilkan Good Ending. Saat bertahan sembunyi atau melawan,
            // pesan yang sempat dikirim ke Ella membuka Hidden Ending; jika tidak, menjadi Bad Ending.
            if (choice == FinalChoice.Approach)
            {
                return StoryEnding.Good;
            }
            if (choice != FinalChoice.Fight && choice != FinalChoice.Hide)
            {
                return StoryEnding.None;
            }
            return state != null && state.EllaMessageSent ? StoryEnding.Hidden : StoryEnding.Bad;
        }
    }
}
