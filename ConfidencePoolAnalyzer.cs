using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Configuration;
using System.Web.Script.Serialization;

namespace ConfidencePoolAnalyzer
{
    internal class ConfidencePoolAnalyzer
    {
        internal static List<Matchup> Matchups = new List<Matchup>();
        internal static List<PlayerEntry> PlayerEntries = new List<PlayerEntry>();
        internal static List<WeekPossibility> Possibilities = new List<WeekPossibility>();
        internal static bool PlayerEntriesKnown = false;
        
        private readonly int _livePollSeconds;
        private readonly int _poolPollMinutes;
        private readonly bool _doUpload;
        private readonly bool _forceUpdates;
        private DateTime _nextScrapeTime;
        private DateTime _poolScrapeTime;
        private DateTime _nextPoolScrapeTime;
        private readonly string _ftpHost, _ftpUser, _ftpPass, _cbsUser, _cbsPass;
        private readonly List<string> _entryWinCheck = new List<string>();

        internal ConfidencePoolAnalyzer()
        {
            _doUpload = "true".Equals(ConfigurationManager.AppSettings["DoUpload"], StringComparison.OrdinalIgnoreCase);
            _forceUpdates = "true".Equals(ConfigurationManager.AppSettings["ForceUpdates"], StringComparison.OrdinalIgnoreCase);
            _livePollSeconds = int.TryParse(ConfigurationManager.AppSettings["LivePollSeconds"], out _livePollSeconds) ? _livePollSeconds : 20;
            _poolPollMinutes = int.TryParse(ConfigurationManager.AppSettings["PoolPollMinutes"], out _poolPollMinutes) ? _poolPollMinutes : 30;
            if (ConfigurationManager.AppSettings["EntryWinCheck"] != null && ConfigurationManager.AppSettings["EntryWinCheck"].Length > 0)
                _entryWinCheck.AddRange(ConfigurationManager.AppSettings["EntryWinCheck"].Split(','));

            ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap { ExeConfigFilename = "passwd.config" };
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap,
                ConfigurationUserLevel.None);
            _ftpHost = config.AppSettings.Settings["ftphost"].Value;
            _ftpUser = config.AppSettings.Settings["ftpuser"].Value;
            _ftpPass = config.AppSettings.Settings["ftppass"].Value;
            _cbsUser = config.AppSettings.Settings["cbsuser"].Value;
            _cbsPass = config.AppSettings.Settings["cbspass"].Value;

            LiveNflData.Instance.Scrape();
            _nextScrapeTime = DateTime.Now.AddSeconds(_livePollSeconds);
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

            DateTime now = DateTime.Now;
            _nextPoolScrapeTime = now.AddMinutes(30);
            _poolScrapeTime = new DateTime(now.Year, now.Month, now.Day, 19, 25, 0);
            while (_poolScrapeTime.DayOfWeek != DayOfWeek.Thursday) _poolScrapeTime += TimeSpan.FromDays(1);

            TryScrapePoolEntries();
            AddRandomEntries(0);
            ValidateLists();
            BuildWeekPossibilities();
        }

        internal static void Main()
        {
            try
            {
                ConfidencePoolAnalyzer analyzer = new ConfidencePoolAnalyzer();
                analyzer.LiveUpdateMode();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.ReadLine();
        }

        internal void LiveUpdateMode()
        {
            bool poolEntriesDirty = false;
            StringBuilder buf = new StringBuilder();
            while (true)
            {
                buf.Clear();

                Matchups.ForEach(LiveNflData.Instance.UpdateMatchup);
                if (_forceUpdates || poolEntriesDirty || Matchups.Any(x => x.IsDirty))
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
                        Console.WriteLine("Dirty winners: " + string.Join(" ", Matchups.Where(x => x.IsWinnerDirty).Select(x => x.Home)));
                        BuildWeekPossibilities();
                        Matchups.ForEach(x => x.IsWinnerDirty = false);
                        CalculateOutcomes();
                    }
                    else if (Matchups.Any(x => x.IsWinPctDirty))
                    {
                        Console.WriteLine("Dirty win pcts: " + string.Join(" ", Matchups.Where(x => x.IsWinPctDirty).Select(x => x.Home)));
                        Possibilities.ForEach(x => x.RecalcProbability());
                        Matchups.ForEach(x => x.IsWinPctDirty = false);
                        CalculateOutcomes();
                    }
                    else if (poolEntriesDirty)
                    {
                        Console.WriteLine("Pool entries dirty.");
                        CalculateOutcomes();
                    }

                    buf.Append(GetTable());
                    Console.Write(buf.ToString());

                    if (_doUpload)
                    {
                        buf.Insert(0,
                            @"<!DOCTYPE html><html><head><title>STATS Conf Pool LIVE!</title><meta http-equiv=""refresh"" content=""20""/>" +
                            @"<meta name=""HandheldFriendly"" content=""True"" />" +
                            @"<meta name=""MobileOptimized"" content=""320"" />" +
                            @"<meta name=""viewport"" content=""width=device-width, initial-scale=1"" />" +
                            @"<meta name=""viewport"" content=""initial-scale=1"" media=""(device-height: 568px)"" />" +
                            @"<meta http-equiv=""cleartype"" content=""on"" />" +
                            @"</head><body><pre>" + Environment.NewLine);
                        buf.AppendLine("LEGEND:");
                        buf.AppendLine("   OVERALL WIN%:  The probability of winning the pool either outright or tied");
                        buf.AppendLine("                  with others (tied scenario's chances are divided equally");
                        buf.AppendLine("                  between all players tied).");
                        buf.AppendLine("      SOLO WIN%:  The probability of winning the pool outright, with no ties.");
                        buf.AppendLine("      TIED WIN%:  The probability of tying for the win with other players.");
                        buf.AppendLine("          TREE%:  The probability of winning the pool outright or tied, if all");
                        buf.AppendLine("                  unfinished games were coin flips (50/50 chance).");
                        buf.AppendLine("    AVG. POINTS:  The average number of points expected for this player.");
                        buf.AppendLine("     MAX POINTS:  The maximum possible number of points for this player.");
                        buf.AppendLine(" CURRENT POINTS:  The current number of points for this player.");
                        buf.AppendLine("      AVG. RANK:  The average finishing place expected for this player.");
                        buf.Append(Environment.NewLine + @"</pre></body></html>");
                        UploadLatestToAltdex(buf.ToString());
                    }
                }
                else Console.WriteLine("No changes.");

                while (DateTime.Now < _nextScrapeTime) Thread.Sleep(1000);
                _nextScrapeTime += TimeSpan.FromSeconds(_livePollSeconds);
                LiveNflData.Instance.Scrape();

                DateTime now = DateTime.Now;
                if (now < _nextPoolScrapeTime && now < _poolScrapeTime)
                {
                    poolEntriesDirty = false;
                    continue;
                }
                poolEntriesDirty = true;
                TryScrapePoolEntries();
                _nextPoolScrapeTime += TimeSpan.FromMinutes(_poolPollMinutes);
                if (DateTime.Now - _poolScrapeTime > TimeSpan.FromMinutes(5)) _poolScrapeTime += TimeSpan.FromDays(7);
            }
        }

        internal void TryScrapePoolEntries()
        {
            bool entriesKnown = true;
            Console.Write("Scraping CBS entries...");
            using (CookieAwareWebClient wc = new CookieAwareWebClient())
            {
                wc.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/37.0.2062.103 Safari/537.36");
                var loginData = new NameValueCollection
                {
                    {"dummy::login_form", "1"},
                    {"form::login_form", "login_form"},
                    {"xurl", @"http://statsnfl.football.cbssports.com/"},
                    {"master_product", "150"},
                    {"vendor", "cbssports"},
                    {"userid", _cbsUser},
                    {"password", _cbsPass},
                    {"_submit", "Sign In"}
                };

                wc.UploadValues(@"http://www.cbssports.com/login", "POST", loginData);
                string fullHtml = wc.DownloadString(@"http://statsnfl.football.cbssports.com/office-pool/standings/live");

                Match data = Regex.Match(fullHtml, @"new CBSi.app.OPMLiveStandings\(.*?({.*?})\s*\);", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                string cleanData = Regex.Replace(data.Groups[1].ToString(), @"""time"":""[^""]*"",?", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                var ser = new JavaScriptSerializer();
                Dictionary<string, object> result = (Dictionary<string, object>)ser.DeserializeObject(cleanData);

                if (!((object[]) result["teams"]).Any()) return;
                PlayerEntries.Clear();
                foreach (Dictionary<string, object> team in (object[]) result["teams"])
                {
                    if (!team.ContainsKey("picks")) continue;
                    string name = team["name"].ToString();
                    PlayerEntry entry = new PlayerEntry(name);
                    foreach (KeyValuePair<string, object> pick in (Dictionary<string, Object>) team["picks"])
                    {
                        if (pick.Key == "mnf") continue;
                        Dictionary<string, object> pickData = (Dictionary<string, object>) pick.Value;
                        string winner = pickData["winner"].ToString();
                        int points = int.Parse(pickData["weight"].ToString(), CultureInfo.InvariantCulture);
                        entry.AddPick(winner, points);
                    }
                    PlayerEntries.Add(entry);
                    if (!entry.GamePicks.Any()) entriesKnown = false;
                }
                Console.WriteLine(PlayerEntries.Count);
            }
            PlayerEntriesKnown = entriesKnown;
        }

        internal void UploadLatestToAltdex(string contents)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            string reqUri = String.Format(CultureInfo.InvariantCulture, "ftp://{0}/files/new.html", _ftpHost);
            NetworkCredential creds = new NetworkCredential(_ftpUser, _ftpPass);
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

            FtpWebResponse renameResp = (FtpWebResponse)renameReq.GetResponse();
            renameResp.Close();
        }

        internal static void PrintWinningWeekPossibilities()
        {
            List<string> entriesToPrint = new List<string> {"Aaron Hanson"};
            foreach (WeekPossibility wp in Possibilities.Where(x => x.PlayerScores.Count(y => entriesToPrint.Contains(y.Name) && y.Rank == 1) > 0
                                                               && x.PlayerScores.Count(y => !entriesToPrint.Contains(y.Name) && y.Rank == 1) == 0)
                                                        .OrderBy(x => x.Probability)) wp.Print();
        }

        internal static string GetTable()
        {
            StringBuilder buf = new StringBuilder();
            //buf.AppendLine("Confidence Pool Analysis for:  " + string.Join(" OR ", EntryWinCheck.ConvertAll(x => @"""" + x + @"""")));
            //buf.AppendLine("Overall Win % = " + 100 * GetOverallWinProbability());
            buf.AppendLine("\t\tOVERALL\tSOLO\tTIED\t\tAVG.\tMAX\tCURRENT\tAVG.");
            buf.AppendLine("ENTRY NAME\tWIN%\tWIN%\tWIN%\tTREE%\tPOINTS\tPOINTS\tPOINTS\tRANK");
            buf.AppendLine("-------------------------------------------------------------------------------");
            foreach (PlayerEntry entry in PlayerEntries.OrderByDescending(x => x.WinProb).ThenBy(x => x.WeightedRank).ThenBy(x => x.Name))
            {
                if (PlayerEntriesKnown)
                {
                    int wins = Possibilities.Count(x => x.PlayerScores.Count(y => y.Name.Equals(entry.Name) && y.Rank == 1) == 1);
                    double pct = (double) wins*100/Possibilities.Count();
                    buf.AppendLine(String.Format(CultureInfo.InvariantCulture,
                        "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
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
                else buf.AppendLine(String.Format(CultureInfo.InvariantCulture, "{0}\t?\t?\t?\t?\t?\t?\t?\t?", entry.Name.PadRight(13)));
            }
            buf.AppendLine();
            return buf.ToString();
        }

        internal void PrintGameChangers()
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

        internal double GetOverallWinProbability()
        {
            return Possibilities.Where(x => x.PlayerScores.Count(y => _entryWinCheck.Contains(y.Name) && y.Rank == 1) > 0)
                                .Sum(x => x.Probability * (double)x.PlayerScores.Count(y => _entryWinCheck.Contains(y.Name) && y.Rank == 1) / (double)x.PlayerScores.Count(y => y.Rank == 1));
        }

        internal static void AddRandomEntries(int numEntries)
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

        internal static void BuildWeekPossibilities()
        {
            Possibilities.Clear();

            int gamesLeft = Matchups.Count(x => String.IsNullOrEmpty(x.Winner));
            int max = (int)Math.Pow(2, gamesLeft) - 1;
            for (int i = 0; i <= max; i++) Possibilities.Add(new WeekPossibility(i));
        }

        internal static void CalculateOutcomes()
        {
            if (!PlayerEntriesKnown) return;
            // now calc entry scores and whatnot
            Possibilities.ForEach(x => x.CalcPlayerScores());
            PlayerEntries.ForEach(x => x.SetScoreData());
        }

        internal void ValidateLists()
        {
            foreach (string name in _entryWinCheck.Where(name => !PlayerEntries.Any(x => x.Name.Equals(name))))
            {
                throw new Exception("entrywincheck name doesn't match a player entry: " + name);
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

                //if (entry.GamePicks.Count() != numMatchups) throw new Exception(entry.Name + " has wrong number of picks: " + entry.GamePicks.Count());

                int pointsTotExpected = (int)((16.5 * numMatchups) - (Math.Pow(numMatchups, 2) / 2));
                //if (entry.GamePicks.Sum(x => x.Points) != pointsTotExpected) throw new Exception(entry.Name + " has invalid points total");

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
