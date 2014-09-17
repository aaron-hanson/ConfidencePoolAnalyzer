namespace ConfidencePoolAnalyzer
{
    internal class PlayerScore
    {
        internal string Name;
        internal int Score;
        internal int Rank;
        internal double WeightedRank;

        internal PlayerScore(string name, int score)
        {
            Name = name;
            Score = score;
        }
    }
}