using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConfidencePoolAnalyzer
{
    internal class PlayerEntry
    {
        internal string Name;
        internal List<GamePick> GamePicks = new List<GamePick>();

        internal double LikelyScore;
        internal double MaxScore;
        internal double CurScore;
        internal double OutrightWinProb, TiedProb, WinProb, OverallWinProb;
        internal double WeightedRank;
        internal double Probability;
        internal int Bits = -1;

        internal PlayerEntry(string name)
        {
            Name = name;
        }

        internal PlayerEntry(string name, int bits, params object[] picks) : this(name)
        {
            Bits = bits;
            if (picks != null)
            {
                for (int i = 0; i < picks.Length; i += 2)
                {
                    GamePicks.Add(new GamePick((string)picks[i], (int)picks[i + 1]));
                }
            }
            else if (Bits >= 0)
            {
                int bitsLeft = ConfidencePoolAnalyzer.Matchups.Count();
                while (bitsLeft > 0)
                {
                    Matchup m = ConfidencePoolAnalyzer.Matchups[bitsLeft - 1];
                    GamePicks.Add((bits & 1) == 1 ? new GamePick(m.Home, 0) : new GamePick(m.Away, 0));
                    bits >>= 1;
                    bitsLeft--;
                }
            }
        }

        internal void AddPick(string winner, int points)
        {
            GamePicks.Add(new GamePick(winner, points));
        }

        internal void SetProbability()
        {
            Probability = 1;

            foreach (GamePick pick in GamePicks)
            {
                Matchup mh = ConfidencePoolAnalyzer.Matchups.FirstOrDefault(x => String.IsNullOrEmpty(x.Winner) && x.Home.Equals(pick.TeamAbbrev));
                Matchup ma = ConfidencePoolAnalyzer.Matchups.FirstOrDefault(x => String.IsNullOrEmpty(x.Winner) && x.Away.Equals(pick.TeamAbbrev));
                if (mh != null) Probability *= mh.HomeWinPct;
                else if (ma != null) Probability *= (1 - ma.HomeWinPct);
            }
        }

        internal int GetScore(WeekPossibility wp)
        {
            IEnumerable<string> weekWinners = wp.GameWinners.Select(x => x.TeamAbbrev);
            return GamePicks.Where(x => weekWinners.Contains(x.TeamAbbrev)).Sum(x => x.Points);
        }

        internal void SetScoreData()
        {
            LikelyScore = 0;
            MaxScore = 0;
            CurScore = 0;

            foreach (Matchup m in ConfidencePoolAnalyzer.Matchups)
            {
                if (!String.IsNullOrEmpty(m.Winner))
                {
                    LikelyScore += GamePicks.Where(x => x.TeamAbbrev.Equals(m.Winner)).Sum(x => x.Points);
                    MaxScore += GamePicks.Where(x => x.TeamAbbrev.Equals(m.Winner)).Sum(x => x.Points);
                    CurScore += GamePicks.Where(x => x.TeamAbbrev.Equals(m.Winner)).Sum(x => x.Points);
                }
                else
                {
                    LikelyScore += GamePicks.Where(x => x.TeamAbbrev.Equals(m.Home)).Sum(x => x.Points) * m.HomeWinPct;
                    LikelyScore += GamePicks.Where(x => x.TeamAbbrev.Equals(m.Away)).Sum(x => x.Points) * (1 - m.HomeWinPct);
                    MaxScore += GamePicks.Where(x => x.TeamAbbrev.Equals(m.Home) || x.TeamAbbrev.Equals(m.Away)).Sum(x => x.Points);
                }
            }

            OutrightWinProb = ConfidencePoolAnalyzer.Possibilities.Where(x => x.PlayerScores.Count(y => y.Name.Equals(Name) && y.Rank == 1) == 1 && x.PlayerScores.Count(y => !y.Name.Equals(Name) && y.Rank == 1) == 0)
                .Sum(x => x.Probability);

            TiedProb = ConfidencePoolAnalyzer.Possibilities.Where(x => x.PlayerScores.Count(y => y.Name.Equals(Name) && y.Rank == 1) == 1 && x.PlayerScores.Count(y => !y.Name.Equals(Name) && y.Rank == 1) > 0)
                .Sum(x => x.Probability);

            WinProb = OutrightWinProb;
            for (int tnum = 1; tnum < ConfidencePoolAnalyzer.PlayerEntries.Count(); tnum++)
            {
                WinProb += (1 / (1D + tnum)) * ConfidencePoolAnalyzer.Possibilities.Where(x => x.PlayerScores.Count(y => y.Name.Equals(Name) && y.Rank == 1) == 1 && x.PlayerScores.Count(y => !y.Name.Equals(Name) && y.Rank == 1) == tnum)
                    .Sum(x => x.Probability);
            }

            OverallWinProb = ConfidencePoolAnalyzer.Possibilities.Where(x => x.PlayerScores.Count(y => y.Name.Equals(Name) && y.Rank == 1) > 0)
                .Sum(x => x.Probability * (double)x.PlayerScores.Count(y => y.Name.Equals(Name) && y.Rank == 1) / (double)x.PlayerScores.Count(y => y.Rank == 1));

            WeightedRank = ConfidencePoolAnalyzer.Possibilities.Select(x => x.PlayerScores.Where(y => y.Name.Equals(Name)).Sum(z => z.WeightedRank)).Sum();
        }

        public override string ToString()
        {
            StringBuilder buf = new StringBuilder();
            buf.Append("(");
            buf.Append(Bits);
            buf.Append(") ");
            foreach (GamePick pick in GamePicks.OrderBy(x => 0 - x.Points))
            {
                buf.Append(pick.TeamAbbrev);
                buf.Append(" ");
            }
            return buf.ToString();
        }
    }
}