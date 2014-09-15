namespace ConfidencePoolAnalyzer
{
    class GamePick
    {
        public string TeamAbbrev;
        public int Points;

        public GamePick(string teamAbbrev, int points)
        {
            TeamAbbrev = teamAbbrev;
            Points = points;
        }
    }
}