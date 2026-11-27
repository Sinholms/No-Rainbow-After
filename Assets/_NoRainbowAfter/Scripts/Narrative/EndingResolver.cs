namespace NoRainbowAfter.Narrative
{
    public enum FinalChoice { None, Fight, Hide, Approach }
    public enum StoryEnding { None, Good, Bad, Hidden }

    public static class EndingResolver
    {
        public static StoryEnding Resolve(FinalChoice choice, GameState state)
        {
            if (choice == FinalChoice.Approach) return StoryEnding.Good;
            if (choice != FinalChoice.Fight && choice != FinalChoice.Hide) return StoryEnding.None;
            return state != null && state.EllaMessageSent ? StoryEnding.Hidden : StoryEnding.Bad;
        }
    }
}
