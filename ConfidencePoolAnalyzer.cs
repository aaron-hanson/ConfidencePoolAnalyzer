using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Configuration;

namespace ConfidencePoolAnalyzer
{
    class ConfidencePoolAnalyzer
    {
        private static readonly int LiveUpdatePollDelay;
        private static readonly bool ForceUpdates;
        private static DateTime _nextScrapeTime;

        public static List<Matchup> Matchups = new List<Matchup>();
        public static List<PlayerEntry> PlayerEntries = new List<PlayerEntry>();
        public static List<WeekPossibility> Possibilities = new List<WeekPossibility>();
        public static List<string> EntryWinCheck = new List<string>();

        public static readonly string FtpHost, FtpUser, FtpPass;

        static ConfidencePoolAnalyzer()
        {
            ForceUpdates = "true".Equals(ConfigurationManager.AppSettings["ForceUpdates"], StringComparison.InvariantCultureIgnoreCase);
            LiveUpdatePollDelay = int.TryParse(ConfigurationManager.AppSettings["LiveUpdatePollDelayMillis"], out LiveUpdatePollDelay) ? LiveUpdatePollDelay : 20000;
            if (ConfigurationManager.AppSettings["EntryWinCheck"] != null && ConfigurationManager.AppSettings["EntryWinCheck"].Length > 0)
                EntryWinCheck.AddRange(ConfigurationManager.AppSettings["EntryWinCheck"].Split(','));

            LiveNflData.Instance.Scrape();
            _nextScrapeTime = DateTime.Now.AddMilliseconds(LiveUpdatePollDelay);
            LiveNflData.Instance.BuildMatchups(Matchups);

            // temp fix for week 2 matchups that are final
            // TODO: find odds data source that includes spreads for final matchups
            if (ConfigurationManager.AppSettings["MissingSpreads"] != null && ConfigurationManager.AppSettings["MissingSpreads"].Length > 0)
            {
                foreach (string[] gamedata in ConfigurationManager.AppSettings["MissingSpreads"].Split(';').Select(game => game.Split('=')))
                {
                    double spread;
                    if (double.TryParse(gamedata[1], out spread)) Matchups.First(x => x.Home == gamedata[0]).Spread = spread;
                }
            }

            ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap {ExeConfigFilename = "ftp.config"};
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap,
                ConfigurationUserLevel.None);
            FtpHost = config.AppSettings.Settings["ftphost"].Value;
            FtpUser = config.AppSettings.Settings["ftpuser"].Value;
            FtpPass = config.AppSettings.Settings["ftppass"].Value;

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
        }

        static void Main()
        {
            try
            {
                ValidateLists();
                BuildWeekPossibilities();                
                LiveUpdateMode();

                //PrintGameChangers();
                //PrintWinningWeekPossibilities();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.ReadLine();
        }

        static void LiveUpdateMode()
        {
            StringBuilder buf = new StringBuilder();
            while (true)
            {
                buf.Clear();

                Matchups.ForEach(LiveNflData.Instance.UpdateMatchup);
                if (ForceUpdates || Matchups.Any(x => x.IsDirty))
                {
                    buf.AppendLine("UPDATED: " + DateTime.Now);
                    buf.AppendLine();
                    Matchups.Where(x => x.IsDirty).ToList().ForEach(x => x.Recalc());

                    buf.AppendLine("STATUS     AWAY SCORE HOME  LINE HOMEWIN%");
                    buf.AppendLine("-----------------------------------------");
                    Matchups.ForEach(x => buf.AppendLine(x.ToString()));
                    buf.AppendLine();

                    if (Matchups.Any(x => x.IsWinnerDirty))
                    {
                        BuildWeekPossibilities();
                        Matchups.ForEach(x => x.IsWinnerDirty = false);
                        CalculateOutcomes();
                    }
                    else if (Matchups.Any(x => x.IsWinPctDirty))
                    {
                        Possibilities.ForEach(x => x.RecalcProbability());
                        Matchups.ForEach(x => x.IsWinPctDirty = false);
                        CalculateOutcomes();
                    }

                    buf.Append(GetTable());
                    Console.Write(buf.ToString());
                    buf.Insert(0, @"<!DOCTYPE html><html><head><title>STATS Conf Pool LIVE!</title><meta http-equiv=""refresh"" content=""20""/></head><body><pre>" + Environment.NewLine);
                    buf.AppendLine("LEGEND:");
                    buf.AppendLine("   OVERALL WIN%:  The probability of winning the pool either outright or tied with others (tied scenario's chances are divided equally between all players tied).");
                    buf.AppendLine("      SOLO WIN%:  The probability of winning the pool outright, with no players tied.");
                    buf.AppendLine("      TIED WIN%:  The probability of ending up tied for the win with other players.");
                    buf.AppendLine("          TREE%:  The probability of winning the pool outright or tied, if all unfinished games were coin flips (50/50 chance).");
                    buf.AppendLine("    AVG. POINTS:  The average number of points expected for this player at the end of all games.");
                    buf.AppendLine("     MAX POINTS:  The maximum possible number of points for this player at the end of all games.");
                    buf.AppendLine(" CURRENT POINTS:  The current number of points for this player, based only on finished games.");
                    buf.AppendLine("      AVG. RANK:  The average finishing place expected for this player at the end of all games.");
                    buf.Append(Environment.NewLine + @"</pre></body></html>");
                    UploadLatestToAltdex(buf.ToString());
                }

                while (DateTime.Now < _nextScrapeTime) Thread.Sleep(1000);
                _nextScrapeTime = DateTime.Now.AddMilliseconds(LiveUpdatePollDelay);
                LiveNflData.Instance.Scrape();
            }
        }

        static void UploadLatestToAltdex(string contents)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            string reqUri = String.Format("ftp://{0}/files/new.html", FtpHost);
            NetworkCredential creds = new NetworkCredential(FtpUser, FtpPass);
            X509Certificate cert = new X509Certificate("ftp.cert");
            X509Certificate2Collection certColl = new X509Certificate2Collection { cert };

            FtpWebRequest req = (FtpWebRequest)WebRequest.Create(reqUri);
            req.Credentials = creds;
            req.EnableSsl = true;
            req.ClientCertificates = certColl;
            req.ContentLength = contents.Length;
            req.Method = WebRequestMethods.Ftp.UploadFile;

            FtpWebRequest renameReq = (FtpWebRequest)WebRequest.Create(reqUri);
            renameReq.Credentials = creds;
            renameReq.EnableSsl = true;
            renameReq.ClientCertificates = certColl;
            renameReq.Method = WebRequestMethods.Ftp.Rename;
            renameReq.RenameTo = "index.html";

            FtpWebResponse resp = (FtpWebResponse)req.GetResponse();
            byte[] bytes = Encoding.UTF8.GetBytes(contents);
            Stream reqStream = req.GetRequestStream();
            reqStream.Write(bytes, 0, bytes.Length);
            reqStream.Close();
            resp.Close();
            resp.Dispose();

            FtpWebResponse renameResp = (FtpWebResponse)renameReq.GetResponse();
            renameResp.Close();
            renameResp.Dispose();
        }

        private static void PrintWinningWeekPossibilities()
        {
            List<string> entriesToPrint = new List<string> {"Aaron Hanson"};
            foreach (WeekPossibility wp in Possibilities.Where(x => x.PlayerScores.Count(y => entriesToPrint.Contains(y.Name) && y.Rank == 1) > 0
                                                               && x.PlayerScores.Count(y => !entriesToPrint.Contains(y.Name) && y.Rank == 1) == 0)
                                                        .OrderBy(x => x.Probability)) wp.Print();
        }

        private static string GetTable()
        {
            StringBuilder buf = new StringBuilder();
            //buf.AppendLine("Confidence Pool Analysis for:  " + string.Join(" OR ", EntryWinCheck.ConvertAll(x => @"""" + x + @"""")));
            //buf.AppendLine("Overall Win % = " + 100 * GetOverallWinProbability());
            buf.AppendLine("\t\tOVERALL\tSOLO\tTIED\t\tAVG.\tMAX\tCURRENT\tAVG.");
            buf.AppendLine("ENTRY NAME\tWIN%\tWIN%\tWIN%\tTREE%\tPOINTS\tPOINTS\tPOINTS\tRANK");
            buf.AppendLine("-------------------------------------------------------------------------------");
            foreach (PlayerEntry entry in PlayerEntries.OrderByDescending(x => x.WinProb).ThenBy(x => x.WeightedRank))
            {
                int wins = Possibilities.Count(x => x.PlayerScores.Count(y => y.Name.Equals(entry.Name) && y.Rank == 1) == 1);
                double pct = (double) wins*100/Possibilities.Count();
                buf.AppendLine(String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}", 
                    entry.Name.PadRight(13), 
                    Math.Round(100*entry.OverallWinProb, 3),
                    Math.Round(100*entry.OutrightWinProb, 3), 
                    Math.Round(100*entry.TiedProb, 3),
                    Math.Round(pct, 2), 
                    Math.Round(entry.LikelyScore), 
                    entry.MaxScore, 
                    entry.CurScore, 
                    Math.Round(entry.WeightedRank, 2)));
            }
            buf.AppendLine();
            return buf.ToString();
        }

        static void PrintGameChangers()
        {
            foreach (Matchup m in Matchups.Where(x => String.IsNullOrEmpty(x.Winner)))
            {
                m.Winner = m.Away;
                BuildWeekPossibilities();
                CalculateOutcomes();
                Console.WriteLine(m.Winner + ": " + Math.Round(100 * GetOverallWinProbability(), 3) + "%");

                m.Winner = m.Home;
                BuildWeekPossibilities();
                CalculateOutcomes();
                Console.WriteLine(m.Winner + ": " + Math.Round(100 * GetOverallWinProbability(), 3) + "%");

                Console.WriteLine();
                m.Winner = "";
            }
        }

        static double GetOverallWinProbability()
        {
            return Possibilities.Where(x => x.PlayerScores.Count(y => EntryWinCheck.Contains(y.Name) && y.Rank == 1) > 0)
                                .Sum(x => x.Probability * (double)x.PlayerScores.Count(y => EntryWinCheck.Contains(y.Name) && y.Rank == 1) / (double)x.PlayerScores.Count(y => y.Rank == 1));
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
            Possibilities.Clear();

            int gamesLeft = Matchups.Count(x => String.IsNullOrEmpty(x.Winner));
            int max = (int)Math.Pow(2, gamesLeft) - 1;
            for (int i = 0; i <= max; i++) Possibilities.Add(new WeekPossibility(i));
        }

        static void CalculateOutcomes()
        {
            // now calc entry scores and whatnot
            Possibilities.ForEach(x => x.CalcPlayerScores());
            PlayerEntries.ForEach(x => x.SetScoreData());
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
                List<int> pointsPicked = new List<int>();
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
