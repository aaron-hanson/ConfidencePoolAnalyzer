using System;
using System.Globalization;

namespace ConfidencePoolAnalyzer
{
    internal class Matchup
    {
        private readonly string _away;
        internal string Away { get { return _away; } }

        private readonly string _home;
        internal string Home { get { return _home; } }

        internal bool IsDirty { get; set; }
        internal bool IsWinnerDirty { get; set; }
        internal bool IsWinPctDirty { get; set; }

        private string _winner;
        internal string Winner
        {
            get { return _winner; }
            set { if (value != _winner) { _winner = value; IsWinnerDirty = true; } }
        }

        private double _homeWinPct;
        internal double HomeWinPct
        {
            get { return _homeWinPct; }
            set { PrevHomeWinPct = _homeWinPct;  if (value != _homeWinPct) { _homeWinPct = value; IsWinPctDirty = true; } }
        }

        private double _prevHomeWinPct;
        internal double PrevHomeWinPct
        {
            get { return _prevHomeWinPct; }
            set { _prevHomeWinPct = value; }
        }

        private double _spread;
        internal double Spread
        {
            get { return _spread; }
            set { if (value != _spread) { _spread = value; IsDirty = true; } }
        }

        private int _homeScore;
        internal int HomeScore
        {
            get { return _homeScore; }
            set { if (value != _homeScore) { _homeScore = value; IsDirty = true; } }
        }

        private int _awayScore;
        internal int AwayScore
        {
            get { return _awayScore; }
            set { if (value != _awayScore) { _awayScore = value; IsDirty = true; } }
        }

        private string _possession;
        internal string Possession
        {
            get { return _possession; }
            set { if (value != _possession) { _possession = value; IsDirty = true; } }
        }

        private bool _redZone;
        internal bool RedZone
        {
            get { return _redZone; }
            set { if (value != _redZone) { _redZone = value; IsDirty = true; } }
        }

        private string _quarter;
        private string _realQuarter;
        internal string Quarter
        {
            get { return _quarter; }
            set
            {
                if (value != _quarter)
                {
                    _quarter = value;
                    IsDirty = true;
                    if (value != NflQuarter.Suspended) _realQuarter = value;
                }
            }
        }

        private string _timeLeft;
        internal string TimeLeft
        {
            get { return _timeLeft; }
            set { if (value != _timeLeft) { _timeLeft = value; IsDirty = true; } }
        }

        private double MinutesLeft
        {
            get
            {
                if (String.IsNullOrEmpty(_realQuarter) || _realQuarter == NflQuarter.Pregame) return 60;
                if (_realQuarter == NflQuarter.Halftime) return 30;
                if (_realQuarter == NflQuarter.Overtime) return 5;
                if (IsFinal) return 0;

                string[] mmss = TimeLeft.Split(':');
                if (mmss.Length != 2) return 60;
                double minutes = double.Parse(mmss[0], CultureInfo.InvariantCulture);
                double seconds = double.Parse(mmss[1], CultureInfo.InvariantCulture);
                int quarter = int.Parse(_realQuarter, CultureInfo.InvariantCulture);
                return minutes + seconds / 60 + (4 - quarter) * 15;
            }
        }

        internal bool IsFinal
        {
            get { return Quarter == NflQuarter.Final || Quarter == NflQuarter.FinalOt; }
        }

        internal string GameStatus
        {
            get
            {
                if (Quarter == NflQuarter.Pregame) return "PREGAME";
                if (Quarter == NflQuarter.FirstQuarter) return TimeLeft.PadLeft(5) + " 1ST";
                if (Quarter == NflQuarter.SecondQuarter) return TimeLeft.PadLeft(5) + " 2ND";
                if (Quarter == NflQuarter.Halftime) return "HALFTIME";
                if (Quarter == NflQuarter.ThirdQuarter) return TimeLeft.PadLeft(5) + " 3RD";
                if (Quarter == NflQuarter.FourthQuarter) return TimeLeft.PadLeft(5) + " 4TH";
                if (Quarter == NflQuarter.Overtime) return TimeLeft.PadLeft(5) + " OT ";
                if (Quarter == NflQuarter.Final) return "FINAL";
                if (Quarter == NflQuarter.FinalOt) return "FINAL(OT)";
                if (Quarter == NflQuarter.Suspended) return "SUSP";
                return Quarter;
            }
        }

        internal Matchup(string away, string home, double homeWinPct, string winner = "")
        {
            _away = away;
            _home = home;
            _homeWinPct = homeWinPct;
            _winner = winner;
            Spread = 0;
            HomeScore = 0;
            AwayScore = 0;
            Quarter = NflQuarter.Pregame;
            TimeLeft = string.Empty;
            Possession = String.Empty;
            RedZone = false;

            Recalc();
        }

        internal void Recalc()
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
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}-{3} {4} {5} {6}",
                GameStatus.PadRight(10),
                (Away + (Possession == Away ? (RedZone ? "*" : ".") : " ")).PadLeft(4),
                AwayScore.ToString(CultureInfo.InvariantCulture).PadLeft(2),
                HomeScore.ToString(CultureInfo.InvariantCulture).PadRight(2),
                ((Possession == Home ? (RedZone ? "*" : ".") : " ") + Home).PadRight(4),
                Spread.ToString("+0.0;-0.0;PK ", CultureInfo.InvariantCulture).PadLeft(5),
                (ConfidencePoolAnalyzer.SmartRound(100*HomeWinPct, 2) + "%").PadLeft(8)
                ) + (HomeWinPct != PrevHomeWinPct ? "  (PREV: " + (ConfidencePoolAnalyzer.SmartRound(100 * PrevHomeWinPct, 2) + "%)").PadLeft(9) : "");
        }
    }

    internal static class NflQuarter
    {
        internal const string Pregame = "P";
        internal const string FirstQuarter = "1";
        internal const string SecondQuarter = "2";
        internal const string Halftime = "H";
        internal const string ThirdQuarter = "3";
        internal const string FourthQuarter = "4";
        internal const string Overtime = "5";
        internal const string Final = "F";
        internal const string FinalOt = "FO";
        internal const string Suspended = "Suspended";
    }
}