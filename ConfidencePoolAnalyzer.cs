using System;
using System.Collections.Generic;
using System.Linq;

namespace ConfidencePoolAnalyzer
{
    class ConfidencePoolAnalyzer
    {
        public static List<Matchup> Matchups = new List<Matchup>();
        public static List<PlayerEntry> PlayerEntries = new List<PlayerEntry>();
        public static List<WeekPossibility> Possibilities;
        public static List<string> EntryWinCheck = new List<string> { "A.J. Smith" };

        static void Main()
        {
            Matchups.Add(new Matchup("PIT", "BAL", .573, "BAL"));
            Matchups.Add(new Matchup("DET", "CAR", .550, ""));
            Matchups.Add(new Matchup("ATL", "CIN", .639, ""));
            Matchups.Add(new Matchup("NE", "MIN", .397, ""));
            Matchups.Add(new Matchup("DAL", "TEN", .592, ""));
            Matchups.Add(new Matchup("JAC", "WAS", .725, ""));
            Matchups.Add(new Matchup("ARI", "NYG", .480, ""));
            Matchups.Add(new Matchup("MIA", "BUF", .443, ""));
            Matchups.Add(new Matchup("NO", "CLE", .345, ""));
            Matchups.Add(new Matchup("STL", "TB", .686, ""));
            Matchups.Add(new Matchup("SEA", "SD", .306, ""));
            Matchups.Add(new Matchup("KC", "DEN", .809, ""));
            Matchups.Add(new Matchup("NYJ", "GB", .715, ""));
            Matchups.Add(new Matchup("HOU", "OAK", .428, ""));
            Matchups.Add(new Matchup("CHI", "SF", .709, ""));
            Matchups.Add(new Matchup("PHI", "IND", .564, ""));

            PlayerEntries.Add(new PlayerEntry("Aaron Hanson", -1, "BAL", 14, "CAR", 11, "CIN", 13, "NE", 2, "TEN", 6, "WAS", 15, "NYG", 7, "MIA", 4, "NO", 3, "TB", 8, "SEA", 10, "DEN", 16, "GB", 9, "OAK", 5, "SF", 12, "PHI", 1));
            PlayerEntries.Add(new PlayerEntry("A.J. Smith", -1, "BAL", 3, "DET", 1, "CIN", 7, "NE", 14, "DAL", 5, "WAS", 8, "ARI", 10, "BUF", 6, "NO", 12, "TB", 11, "SEA", 13, "DEN", 16, "GB", 15, "HOU", 4, "SF", 9, "IND", 2));
            PlayerEntries.Add(new PlayerEntry("Antonio Cerda", -1, "PIT", 16, "DET", 15, "ATL", 14, "NE", 13, "TEN", 12, "WAS", 11, "ARI", 2, "MIA", 9, "NO", 8, "TB", 7, "SEA", 6, "DEN", 5, "GB", 4, "HOU", 3, "CHI", 10, "IND", 1));
            PlayerEntries.Add(new PlayerEntry("ashley chambe", -1, "PIT", 9, "DET", 2, "CIN", 3, "NE", 10, "TEN", 7, "WAS", 6, "ARI", 8, "BUF", 1, "NO", 12, "TB", 13, "SEA", 15, "DEN", 16, "GB", 11, "HOU", 5, "SF", 14, "IND", 4));
            PlayerEntries.Add(new PlayerEntry("Chris Vodicka", -1, "PIT", 16, "CAR", 3, "CIN", 6, "NE", 11, "TEN", 8, "JAC", 5, "ARI", 12, "BUF", 4, "NO", 13, "TB", 7, "SEA", 14, "DEN", 15, "GB", 9, "HOU", 2, "SF", 10, "PHI", 1));
            PlayerEntries.Add(new PlayerEntry("Desmond Hui", -1, "BAL", 10, "CAR", 6, "CIN", 7, "NE", 12, "DAL", 3, "JAC", 2, "NYG", 1, "MIA", 8, "NO", 13, "TB", 11, "SEA", 15, "DEN", 16, "GB", 14, "OAK", 4, "SF", 9, "PHI", 5));
            PlayerEntries.Add(new PlayerEntry("Jessica Kopic", -1, "PIT", 2, "DET", 15, "CIN", 3, "NE", 9, "TEN", 8, "WAS", 7, "ARI", 5, "MIA", 1, "NO", 12, "TB", 10, "SEA", 11, "DEN", 16, "GB", 13, "HOU", 4, "SF", 14, "IND", 6));
            PlayerEntries.Add(new PlayerEntry("Khayam Masud", -1, "PIT", 4, "DET", 5, "CIN", 9, "NE", 10, "TEN", 7, "JAC", 11, "ARI", 3, "MIA", 2, "NO", 13, "TB", 8, "SEA", 12, "DEN", 16, "GB", 14, "HOU", 1, "SF", 15, "IND", 6));
            PlayerEntries.Add(new PlayerEntry("kurt n", -1, "PIT", 1, "CAR", 2, "CIN", 4, "NE", 14, "DAL", 7, "WAS", 8, "ARI", 9, "MIA", 3, "NO", 15, "TB", 12, "SEA", 13, "DEN", 10, "GB", 16, "HOU", 11, "SF", 6, "IND", 5));
            PlayerEntries.Add(new PlayerEntry("Marisol Magan", -1, "PIT", 10, "DET", 8, "CIN", 3, "NE", 5, "TEN", 1, "WAS", 11, "ARI", 4, "MIA", 9, "NO", 16, "TB", 6, "SEA", 12, "DEN", 14, "GB", 15, "HOU", 7, "SF", 2, "IND", 13));
            PlayerEntries.Add(new PlayerEntry("Paul Nix", -1, "BAL", 5, "DET", 9, "ATL", 7, "NE", 6, "TEN", 10, "JAC", 3, "ARI", 8, "MIA", 2, "NO", 15, "TB", 4, "SEA", 16, "KC", 13, "GB", 14, "HOU", 11, "SF", 12, "IND", 1));
            PlayerEntries.Add(new PlayerEntry("Teresa Mendoz", -1, "BAL", 6, "CAR", 12, "CIN", 5, "NE", 13, "TEN", 4, "WAS", 3, "NYG", 2, "BUF", 8, "NO", 15, "STL", 1, "SEA", 11, "DEN", 14, "GB", 7, "OAK", 9, "SF", 16, "IND", 10));

            AddRandomEntries(0);

            try
            {
                ValidateLists();
                BuildWeekPossibilities();
                CalculateOutcomes();
                PrintResults();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.ReadLine();
        }

        static void PrintResults()
        {
            List<string> entriesToPrint = new List<string> { /*"Aaron Hanson"*/ };
            foreach (WeekPossibility wp in Possibilities.Where(x => x.PlayerScores.Count(y => entriesToPrint.Contains(y.Name) && y.Rank == 1) > 0
                                                                && x.PlayerScores.Count(y => !entriesToPrint.Contains(y.Name) && y.Rank == 1) == 0)
                                                        .OrderBy(x => x.Probability)) wp.Print();

            // both tied for win, no others tied
            //foreach (WeekPossibility wp in possibilities.Where(x => x.playerScores.Count(y => entriesToPrint.Contains(y.name) && y.rank == 1) == 2
            //                                                    && x.playerScores.Count(y => !entriesToPrint.Contains(y.name) && y.rank == 1) == 0)
            //                                            .OrderByDescending(x => x.probability)) wp.print();

            double overallWinProb = 100 * Possibilities.Where(x => x.PlayerScores.Count(y => EntryWinCheck.Contains(y.Name) && y.Rank == 1) > 0)
                                                        .Sum(x => x.Probability * (double)x.PlayerScores.Count(y => EntryWinCheck.Contains(y.Name) && y.Rank == 1) / (double)x.PlayerScores.Count(y => y.Rank == 1));

            Console.WriteLine("\n" + overallWinProb);

            Console.WriteLine("\nEntry Name\tTree\tLikely\tMax\tCur\tRank\tOWin%\tTie%\tWin%\n");
            foreach (PlayerEntry entry in PlayerEntries.OrderByDescending(x => x.WinProb).ThenBy(x => x.WeightedRank))
            {
                int wins = Possibilities.Count(x => x.PlayerScores.Count(y => y.Name.Equals(entry.Name) && y.Rank == 1) == 1);
                double pct = (double)wins * 100 / Possibilities.Count();
                Console.WriteLine(
                    entry.Name.PadRight(13) + "\t"
                    + Math.Round(pct, 2) + "\t" +
                    Math.Round(entry.LikelyScore) + "\t" +
                    entry.MaxScore + "\t" +
                    entry.CurScore + "\t" +
                    Math.Round(entry.WeightedRank, 2) + "\t" +
                    Math.Round(100 * entry.OutrightWinProb, 3) + "\t" +
                    Math.Round(100 * entry.TiedProb, 3) + "\t" +
                    Math.Round(100 * entry.OverallWinProb, 3));
            }

            Console.WriteLine();

            foreach (Matchup m in Matchups.Where(x => String.IsNullOrEmpty(x.Winner)))
            {
                m.Winner = m.Away;
                BuildWeekPossibilities();
                CalculateOutcomes();
                overallWinProb = 100 * Possibilities.Where(x => x.PlayerScores.Count(y => EntryWinCheck.Contains(y.Name) && y.Rank == 1) > 0)
                                                            .Sum(x => x.Probability * (double)x.PlayerScores.Count(y => EntryWinCheck.Contains(y.Name) && y.Rank == 1) / (double)x.PlayerScores.Count(y => y.Rank == 1));
                Console.WriteLine(m.Winner + ": " + Math.Round(overallWinProb, 3) + "%");

                m.Winner = m.Home;
                BuildWeekPossibilities();
                CalculateOutcomes();
                overallWinProb = 100 * Possibilities.Where(x => x.PlayerScores.Count(y => EntryWinCheck.Contains(y.Name) && y.Rank == 1) > 0)
                                                            .Sum(x => x.Probability * (double)x.PlayerScores.Count(y => EntryWinCheck.Contains(y.Name) && y.Rank == 1) / (double)x.PlayerScores.Count(y => y.Rank == 1));
                Console.WriteLine(m.Winner + ": " + Math.Round(overallWinProb, 3) + "%\n");

                m.Winner = "";
            }
        }

        static void AddRandomEntries(int numEntries)
        {
            Random rand = new Random();
            while (numEntries > 0)
            {
                List<object> picks = new List<object>();
                List<int> points = new List<int>();
                int mnum = 16;
                for (int y = 0; y < Matchups.Count; y++)
                {
                    points.Add(mnum);
                    mnum--;
                }
                foreach (Matchup m in Matchups)
                {
                    if (rand.NextDouble() <= m.HomeWinPct)
                    {
                        picks.Add(m.Home);

                        double orig = 2 * (1 - m.HomeWinPct) - 1;
                        int sign = Math.Sign(orig);
                        double adj = (Math.Sqrt(Math.Abs(orig)) * sign + 1) / 2;

                        int pointidx = (int)(adj * points.Count());
                        //int pointidx = rand.Next(points.Count());
                        int pointval = points[pointidx];
                        points.RemoveAt(pointidx);
                        picks.Add(pointval);
                    }
                    else
                    {
                        picks.Add(m.Away);

                        double orig = 2 * m.HomeWinPct - 1;
                        int sign = Math.Sign(orig);
                        double adj = (Math.Sqrt(Math.Abs(orig)) * sign + 1) / 2;

                        int pointidx = (int)(adj * points.Count());
                        int pointval = points[pointidx];
                        points.RemoveAt(pointidx);
                        picks.Add(pointval);
                    }
                }
                PlayerEntries.Add(new PlayerEntry("RAND " + numEntries, -1, picks.ToArray()));
                numEntries--;
            }
        }

        static void BuildWeekPossibilities()
        {
            Possibilities = new List<WeekPossibility>();

            int gamesLeft = Matchups.Count(x => String.IsNullOrEmpty(x.Winner));
            int max = (int)Math.Pow(2, gamesLeft) - 1;
            for (int i = 0; i <= max; i++) Possibilities.Add(new WeekPossibility(i));
        }

        static void CalculateOutcomes()
        {
            // now calc entry scores and whatnot
            foreach (WeekPossibility week in Possibilities) week.CalcPlayerScores();
            foreach (PlayerEntry entry in PlayerEntries) entry.SetScoreData();
        }

        static void ValidateLists()
        {
            foreach (string name in EntryWinCheck)
            {
                if (!PlayerEntries.Any(x => x.Name.Equals(name))) throw new Exception("entrywincheck name doesn't match a player entry: " + name);
            }

            List<string> matchupTeams = new List<string>();
            foreach (Matchup m in Matchups)
            {
                if (matchupTeams.Contains(m.Home)) throw new Exception("duplicate matchup team: " + m.Home);
                matchupTeams.Add(m.Home);
                if (matchupTeams.Contains(m.Away)) throw new Exception("duplicate matchup team: " + m.Away);
                matchupTeams.Add(m.Away);

                if (!String.IsNullOrEmpty(m.Winner) && !m.Winner.Equals(m.Home) && !m.Winner.Equals(m.Away) && !m.Winner.Equals("T")) throw new Exception("matchup winner not valid: " + m.Winner);
            }

            List<string> entryNames = new List<string>();
            int numMatchups = Matchups.Count();
            foreach (PlayerEntry entry in PlayerEntries)
            {
                if (entryNames.Contains(entry.Name)) throw new Exception("duplicate entry name: " + entry.Name);
                entryNames.Add(entry.Name);

                if (entry.GamePicks.Count() != numMatchups) throw new Exception(entry.Name + " has wrong number of picks: " + entry.GamePicks.Count());

                int pointsTotExpected = (int)((16.5 * numMatchups) - (Math.Pow(numMatchups, 2) / 2));
                if (entry.GamePicks.Sum(x => x.Points) != pointsTotExpected) throw new Exception(entry.Name + " has invalid points total");

                List<string> teamsPicked = new List<string>();
                List<double> pointsPicked = new List<double>();
                List<Matchup> matchupsPicked = new List<Matchup>();
                foreach (GamePick pick in entry.GamePicks)
                {
                    if (teamsPicked.Contains(pick.TeamAbbrev)) throw new Exception(entry.Name + " has duplicate pick: " + pick.TeamAbbrev);
                    teamsPicked.Add(pick.TeamAbbrev);

                    if (pointsPicked.Contains(pick.Points)) throw new Exception(entry.Name + " has duplicate points: " + pick.Points);
                    pointsPicked.Add(pick.Points);

                    Matchup m = Matchups.FirstOrDefault(x => x.Away.Equals(pick.TeamAbbrev) || x.Home.Equals(pick.TeamAbbrev));
                    if (m == null) throw new Exception(entry.Name + " has a picked team not found in matchups: " + pick.TeamAbbrev);
                    if (matchupsPicked.Contains(m)) throw new Exception(entry.Name + " has more than one pick for a matchup: " + pick.TeamAbbrev);
                    matchupsPicked.Add(m);
                }
            }
        }


    }
}
