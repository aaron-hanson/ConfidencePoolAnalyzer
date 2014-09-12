namespace ConfidencePoolAnalyzer
{
    class PlayerScore
    {
        public string Name;
        public double Score;
        public int Rank;
        public double WeightedRank;

        public PlayerScore(string name, double score)
        {
            Name = name;
            Score = score;
        }
    }
}