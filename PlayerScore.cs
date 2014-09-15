namespace ConfidencePoolAnalyzer
{
    class PlayerScore
    {
        public string Name;
        public int Score;
        public int Rank;
        public double WeightedRank;

        public PlayerScore(string name, int score)
        {
            Name = name;
            Score = score;
        }
    }
}