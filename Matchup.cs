using System;

namespace ConfidencePoolAnalyzer
{
    public class Matchup
    {
        private readonly string _away;
        public string Away { get { return _away; } }

        private readonly string _home;
        public string Home { get { return _home; } }

        public bool IsDirty { get; set; }
        public bool IsWinnerDirty { get; set; }
        public bool IsWinPctDirty { get; set; }

        private string _winner;
        public string Winner {
            get { return _winner; }
            set { if (value != _winner) { _winner = value; IsWinnerDirty = true; } }
        }

        private double _homeWinPct;
        public double HomeWinPct
        {
            get { return _homeWinPct; }
            set { if (value != _homeWinPct) { _homeWinPct = value; IsWinPctDirty = true; } }
        }

        private double _spread;
        public double Spread
        {
            get { return _spread; }
            set { if (value != _spread) { _spread = value; IsDirty = true; } }
        }

        private int _homeScore;
        public int HomeScore
        {
            get { return _homeScore; }
            set { if (value != _homeScore) { _homeScore = value; IsDirty = true; } }
        }

        private int _awayScore;
        public int AwayScore
        {
            get { return _awayScore; }
            set { if (value != _awayScore) { _awayScore = value; IsDirty = true; } }
        }

        private string _quarter;
        public string Quarter
        {
            get { return _quarter; }
            set { if (value != _quarter) { _quarter = value; IsDirty = true; } }
        }

        private string _timeLeft;
        public string TimeLeft
        {
            get { return _timeLeft; }
            set { if (value != _timeLeft) { _timeLeft = value; IsDirty = true; } }
        }

        private int _minutesLeft;
        public int MinutesLeft
        {
            get { return _minutesLeft; }
            set { if (value != _minutesLeft) { _minutesLeft = value; IsDirty = true; } }
        }

        public Matchup(string away, string home, double homeWinPct, string winner = "")
        {
            _away = away;
            _home = home;
            HomeWinPct = homeWinPct;
            Winner = winner;
            Spread = 0;
            HomeScore = 0;
            AwayScore = 0;
            Quarter = "P";
            TimeLeft = "";
            MinutesLeft = 60;

            Recalc();
        }

        public void Recalc()
        {
            IsDirty = false;
            if (Winner == Home) HomeWinPct = 1;
            else if (Winner == Away) HomeWinPct = 0;
            else if (Quarter == "F")
            {
                Winner = HomeScore > AwayScore ? Home : Away;
                HomeWinPct = HomeScore > AwayScore ? 1 : 0; //TODO: account for ties?
            }
            else HomeWinPct = LiveWinProbability.Estimate(AwayScore, HomeScore, Spread, MinutesLeft);
        }

        public override string ToString()
        {
            return string.Format("{0}\t{1} {2}-{3} {4} {5}\t{6}",
                (Quarter == "F" ? "FINAL" : (Quarter == "P" ? "PREGAME" : TimeLeft + " " + Quarter)).PadRight(7),
                Away.PadLeft(3),
                AwayScore.ToString().PadLeft(2),
                HomeScore.ToString().PadRight(2),
                Home.PadRight(3),
                Spread.ToString("+0.0;-0.0;PK").PadLeft(5),
                Math.Round(HomeWinPct, 4).ToString("0.00%").PadLeft(7)
                );
        }

    }
}