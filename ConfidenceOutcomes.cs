using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace ConfidenceOutcomes
{
    class ConfidenceOutcomes
    {
        public static List<Matchup> Matchups = new List<Matchup>();
        public static List<PlayerEntry> PlayerEntries = new List<PlayerEntry>();
        public static List<WeekPossibility> Possibilities;
        public static List<string> EntryWinCheck = new List<string> { "Aaron Hanson", "Teresa Mendoz" };

        static void Main()
        {
            Matchups.Add(new Matchup("GB", "SEA", .651, "SEA"));
            Matchups.Add(new Matchup("BUF", "CHI", .725, "BUF"));
            Matchups.Add(new Matchup("OAK", "NYJ", .685, "NYJ"));
            Matchups.Add(new Matchup("TEN", "KC", .588, "TEN"));
            Matchups.Add(new Matchup("MIN", "STL", .605, "MIN"));
            Matchups.Add(new Matchup("JAC", "PHI", .792, "PHI"));
            Matchups.Add(new Matchup("WAS", "HOU", .609, "HOU"));
            Matchups.Add(new Matchup("CLE", "PIT", .685, "PIT"));
            Matchups.Add(new Matchup("NE", "MIA", .340, "MIA"));
            Matchups.Add(new Matchup("CIN", "BAL", .525, "CIN"));
            Matchups.Add(new Matchup("NO", "ATL", .364, "ATL"));
            Matchups.Add(new Matchup("CAR", "TB", .497, "CAR"));
            Matchups.Add(new Matchup("SF", "DAL", .348, "SF"));
            Matchups.Add(new Matchup("IND", "DEN", .775, ""));
            Matchups.Add(new Matchup("NYG", "DET", .779, ""));
            Matchups.Add(new Matchup("SD", "ARI", .615, ""));

            PlayerEntries.Add(new PlayerEntry("Aaron Hanson", -1, "SEA", 6, "CHI", 8, "NYJ", 10, "TEN", 1, "STL", 12, "PHI", 14, "HOU", 13, "PIT", 7, "NE", 5, "BAL", 3, "NO", 2, "TB", 4, "SF", 9, "DEN", 15, "DET", 16, "ARI", 11));
            PlayerEntries.Add(new PlayerEntry("Christopher P", -1, "SEA", 8, "CHI", 16, "NYJ", 6, "KC", 15, "MIN", 9, "PHI", 11, "HOU", 4, "PIT", 7, "NE", 12, "BAL", 1, "NO", 14, "CAR", 3, "SF", 13, "DEN", 10, "DET", 2, "ARI", 5));
            PlayerEntries.Add(new PlayerEntry("Collin Shapir", -1, "SEA", 11, "CHI", 7, "OAK", 16, "KC", 14, "MIN", 6, "PHI", 13, "WAS", 12, "PIT", 15, "NE", 5, "BAL", 1, "NO", 4, "CAR", 9, "SF", 8, "DEN", 10, "DET", 3, "ARI", 2));
            PlayerEntries.Add(new PlayerEntry("Desmond Hui", -1, "SEA", 12, "CHI", 16, "OAK", 4, "KC", 11, "STL", 3, "PHI", 14, "WAS", 2, "PIT", 15, "NE", 10, "CIN", 1, "ATL", 6, "TB", 7, "SF", 9, "DEN", 13, "DET", 8, "SD", 5));
            PlayerEntries.Add(new PlayerEntry("Jessica Kopic", -1, "SEA", 16, "CHI", 14, "NYJ", 7, "KC", 11, "MIN", 4, "PHI", 15, "HOU", 2, "PIT", 12, "NE", 9, "BAL", 3, "NO", 6, "CAR", 1, "SF", 8, "DEN", 10, "DET", 13, "ARI", 5));
            PlayerEntries.Add(new PlayerEntry("Khayam Masud", -1, "SEA", 16, "CHI", 15, "NYJ", 14, "KC", 13, "STL", 12, "PHI", 11, "HOU", 10, "PIT", 9, "NE", 8, "BAL", 7, "NO", 6, "TB", 5, "SF", 4, "DEN", 3, "DET", 2, "ARI", 1));
            PlayerEntries.Add(new PlayerEntry("Marc Levine", -1, "SEA", 11, "CHI", 15, "OAK", 2, "KC", 8, "STL", 1, "PHI", 16, "WAS", 7, "PIT", 14, "NE", 13, "BAL", 4, "NO", 10, "CAR", 6, "SF", 5, "DEN", 12, "DET", 9, "SD", 3));
            PlayerEntries.Add(new PlayerEntry("Marisol Magan", -1, "SEA", 4, "CHI", 16, "NYJ", 10, "KC", 9, "MIN", 5, "PHI", 14, "HOU", 3, "PIT", 13, "NE", 12, "BAL", 1, "NO", 8, "CAR", 2, "SF", 6, "DEN", 7, "DET", 11, "ARI", 15));
            PlayerEntries.Add(new PlayerEntry("Paul Nix", -1, "SEA", 14, "CHI", 15, "NYJ", 13, "KC", 8, "MIN", 5, "PHI", 11, "HOU", 9, "PIT", 12, "NE", 7, "BAL", 4, "NO", 2, "TB", 3, "SF", 10, "DEN", 16, "DET", 6, "SD", 1));
            PlayerEntries.Add(new PlayerEntry("Teresa Mendoz", -1, "SEA", 12, "CHI", 13, "NYJ", 11, "KC", 15, "MIN", 5, "PHI", 16, "WAS", 2, "PIT", 6, "NE", 10, "CIN", 1, "NO", 9, "CAR", 3, "SF", 7, "DEN", 14, "DET", 8, "ARI", 4));
            
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
                Console.WriteLine(m.Winner + ": " + overallWinProb);

                m.Winner = m.Home;
                BuildWeekPossibilities();
                CalculateOutcomes();
                overallWinProb = 100 * Possibilities.Where(x => x.PlayerScores.Count(y => EntryWinCheck.Contains(y.Name) && y.Rank == 1) > 0)
                                                            .Sum(x => x.Probability * (double)x.PlayerScores.Count(y => EntryWinCheck.Contains(y.Name) && y.Rank == 1) / (double)x.PlayerScores.Count(y => y.Rank == 1));
                Console.WriteLine(m.Winner + ": " + overallWinProb + "\n");

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
                        //int pointidx = rand.Next(points.Count());
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
