namespace ConfidencePoolAnalyzer
{
    class GamePick
    {
        public string TeamAbbrev;
        public double Points;

        public GamePick(string teamAbbrev, int points)
        {
            TeamAbbrev = teamAbbrev;
            Points = points;
        }
    }
}