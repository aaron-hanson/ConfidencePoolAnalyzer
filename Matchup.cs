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

        private double MinutesLeft
        {
            get
            {
                if (String.IsNullOrEmpty(Quarter) || Quarter == NFLQuarter.Pregame) return 60;
                else if (Quarter == NFLQuarter.Halftime) return 30;
                else if (Quarter == NFLQuarter.Overtime) return 2;
                else if (IsFinal) return 0;

                string[] mmss = TimeLeft.Split(':');
                if (mmss.Length != 2) return 60;
                double minutes = double.Parse(mmss[0]);
                double seconds = double.Parse(mmss[1]);
                int quarter = int.Parse(Quarter);
                return minutes + seconds / 60 + (4 - quarter) * 15;
            }
        }

        public bool IsFinal
        {
            get { return Quarter == NFLQuarter.Final || Quarter == NFLQuarter.FinalOT; }
        }

        public string GameStatus
        {
            get
            {
                if (Quarter == NFLQuarter.Pregame) return "PREGAME";
                else if (Quarter == NFLQuarter.FirstQuarter) return TimeLeft.PadLeft(5) + " 1ST";
                else if (Quarter == NFLQuarter.SecondQuarter) return TimeLeft.PadLeft(5) + " 2ND";
                else if (Quarter == NFLQuarter.Halftime) return "HALFTIME";
                else if (Quarter == NFLQuarter.ThirdQuarter) return TimeLeft.PadLeft(5) + " 3RD";
                else if (Quarter == NFLQuarter.FourthQuarter) return TimeLeft.PadLeft(5) + " 4TH";
                else if (Quarter == NFLQuarter.Overtime) return TimeLeft.PadLeft(5) + " OT ";
                else if (Quarter == NFLQuarter.Final) return "FINAL";
                else if (Quarter == NFLQuarter.FinalOT) return "FINAL(OT)";
                else return Quarter;
            }
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
            Quarter = NFLQuarter.Pregame;
            TimeLeft = "";

            Recalc();
        }

        public void Recalc()
        {
            IsDirty = false;
            if (Winner == Home) HomeWinPct = 1;
            else if (Winner == Away) HomeWinPct = 0;
            else if (IsFinal)
            {
                Winner = HomeScore > AwayScore ? Home : Away;
                HomeWinPct = HomeScore > AwayScore ? 1 : 0; //TODO: account for ties?
            }
            else HomeWinPct = LiveWinProbability.Estimate(AwayScore, HomeScore, Spread, MinutesLeft);
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}-{3} {4} {5} {6}",
                GameStatus.PadRight(10),
                Away.PadLeft(3),
                AwayScore.ToString().PadLeft(2),
                HomeScore.ToString().PadRight(2),
                Home.PadRight(3),
                Spread.ToString("+0.0;-0.0;PK ").PadLeft(5),
                Math.Round(HomeWinPct, 4).ToString("0.00%").PadLeft(7)
                );
        }
    }

    public sealed class NFLQuarter
    {
        public static readonly string Pregame = "P";
        public static readonly string FirstQuarter = "1";
        public static readonly string SecondQuarter = "2";
        public static readonly string Halftime = "H";
        public static readonly string ThirdQuarter = "3";
        public static readonly string FourthQuarter = "4";
        public static readonly string Overtime = "5";
        public static readonly string Final = "F";
        public static readonly string FinalOT = "FO";
    }
}