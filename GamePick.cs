namespace ConfidencePoolAnalyzer
{
    internal class GamePick
    {
        internal string TeamAbbrev;
        internal int Points;

        internal GamePick(string teamAbbrev, int points)
        {
            TeamAbbrev = teamAbbrev;
            Points = points;
        }
    }
}