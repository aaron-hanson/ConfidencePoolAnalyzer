namespace ConfidencePoolAnalyzer
{
    public class Matchup
    {
        public string Away, Home;
        public string Winner;
        public double HomeWinPct;

        public Matchup(string away, string home, double homeWinPct, string winner = "")
        {
            Away = away;
            Home = home;
            HomeWinPct = homeWinPct;
            Winner = winner;
        }
    }
}