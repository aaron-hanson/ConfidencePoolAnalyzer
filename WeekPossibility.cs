using System;
using System.Collections.Generic;
using System.Linq;

namespace ConfidencePoolAnalyzer
{
    internal class WeekPossibility
    {
        internal List<GamePick> GameWinners;
        internal List<PlayerScore> PlayerScores;
        internal double Probability = 1;
        internal int Bits;

        internal WeekPossibility(int bits)
        {
            Bits = bits;
            GameWinners = new List<GamePick>();
            PlayerScores = new List<PlayerScore>();
            List<Matchup> matchupsLeft = ConfidencePoolAnalyzer.Matchups.Where(x => String.IsNullOrEmpty(x.Winner)).ToList();
            
            int bitsLeft = matchupsLeft.Count();
            while (bitsLeft > 0)
            {
                Matchup m = matchupsLeft[bitsLeft - 1];
                GameWinners.Add((bits & 1) == 1 ? new GamePick(m.Home, 0) : new GamePick(m.Away, 0));
                bits >>= 1;
                bitsLeft--;
            }

            foreach (Matchup m in ConfidencePoolAnalyzer.Matchups.Where(x => !String.IsNullOrEmpty(x.Winner)))
            {
                GameWinners.Add(new GamePick(m.Winner, 0));
            }

            RecalcProbability();
        }

        internal void RecalcProbability()
        {
            Probability = 1;
            foreach (GamePick pick in GameWinners)
            {
                Matchup mh = ConfidencePoolAnalyzer.Matchups.FirstOrDefault(x => String.IsNullOrEmpty(x.Winner) && x.Home.Equals(pick.TeamAbbrev));
                Matchup ma = ConfidencePoolAnalyzer.Matchups.FirstOrDefault(x => String.IsNullOrEmpty(x.Winner) && x.Away.Equals(pick.TeamAbbrev));
                if (mh != null) Probability *= mh.HomeWinPct;
                else if (ma != null) Probability *= (1 - ma.HomeWinPct);
            }
        }

        internal void CalcPlayerScores()
        {
            PlayerScores.Clear();
            foreach (PlayerEntry entry in ConfidencePoolAnalyzer.PlayerEntries)
            {
                PlayerScores.Add(new PlayerScore(entry.Name, entry.GetScore(this)));
            }

            foreach (PlayerScore score in PlayerScores)
            {
                score.Rank = 1 + PlayerScores.Count(x => x.Score > score.Score);
                score.WeightedRank = score.Rank * Probability;
            }
        }

        internal void Print()
        {
            //Console.Write("AH=" + playerScores.Where(x => x.name.Equals("Aaron Hanson")).First().rank + " TM=" + playerScores.Where(x => x.name.Equals("Teresa Mendoz")).First().rank + " ");
            Console.Write("(" + ConfidencePoolAnalyzer.SmartRound(100 * Probability, 3) + "): ");
            foreach (GamePick gp in GameWinners.Where(x => !ConfidencePoolAnalyzer.Matchups.Select(y => y.Winner).Contains(x.TeamAbbrev)))
            {
                Console.Write(gp.TeamAbbrev + "/");
            }
            Console.WriteLine();
        }

        public override string ToString()
        {
            return string.Join("/", GameWinners.Where(x => !ConfidencePoolAnalyzer.Matchups.Select(y => y.Winner).Contains(x.TeamAbbrev)).Select(x => x.TeamAbbrev));
        }
    }
}