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
        
        private readonly int _livePollSeconds;
        private readonly bool _doUpload;
        private readonly bool _forceUpdates;
        private DateTime _nextScrapeTime;
        private DateTime _nextPoolScrapeTime;
        private DateTime _poolScrapeTimeFinal;
        private DateTime _poolScrapeStart;
        private bool _poolEntriesDirty;
        private bool _playerEntriesKnown;
        private readonly string _ftpHost, _ftpUser, _ftpPass, _cbsUser, _cbsPass;
        private readonly List<string> _entryWinCheck = new List<string>();

        private List<GameChangerLine> gameChangers;
        private string gameChangerTable;

        internal ConfidencePoolAnalyzer()
        {
            _doUpload = "true".Equals(ConfigurationManager.AppSettings["DoUpload"], StringComparison.OrdinalIgnoreCase);
            _forceUpdates = "true".Equals(ConfigurationManager.AppSettings["ForceUpdates"], StringComparison.OrdinalIgnoreCase);
            _livePollSeconds = int.TryParse(ConfigurationManager.AppSettings["LivePollSeconds"], out _livePollSeconds) ? _livePollSeconds : 20;
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
            _poolScrapeTimeFinal = new DateTime(now.Year, now.Month, now.Day, 19, 20, 0);
            while (_poolScrapeTimeFinal.DayOfWeek != DayOfWeek.Thursday) _poolScrapeTimeFinal += TimeSpan.FromDays(1);
            _poolScrapeStart = new DateTime(_poolScrapeTimeFinal.Year, _poolScrapeTimeFinal.Month, _poolScrapeTimeFinal.Day, 8, 0, 0).AddDays(-1);
            _nextPoolScrapeTime = now.AddMinutes(30);

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
            bool firstRun = true;
            StringBuilder buf = new StringBuilder();
            while (true)
            {
                try
                {
                    buf.Clear();

                    Matchups.ForEach(LiveNflData.Instance.UpdateMatchup);
                    if (_forceUpdates || _poolEntriesDirty || Matchups.Any(x => x.IsDirty || x.IsWinnerDirty))
                    {
                        Console.WriteLine();
                        buf.AppendLine("UPDATED: " + DateTime.Now);
                        buf.AppendLine();

                        Matchups.Where(x => x.IsDirty).ToList().ForEach(x => x.Recalc());
                        if (firstRun)
                        {
                            Matchups.ForEach(x => x.PrevHomeWinPct = x.HomeWinPct);
                            firstRun = false;
                        }

                        buf.AppendLine("STATUS     AWAY SCORE HOME  LINE HOMEWIN%");
                        buf.AppendLine("-----------------------------------------");
                        Matchups.ForEach(x => buf.AppendLine(x.ToString()));
                        buf.AppendLine();

                        if (Matchups.Any(x => x.IsWinnerDirty))
                        {
                            Console.WriteLine("Dirty winners: " + string.Join(" ", Matchups.Where(x => x.IsWinnerDirty).Select(x => x.Home)));
                            BuildWeekPossibilities();
                            CalculateOutcomes();
                        }
                        else if (Matchups.Any(x => x.IsWinPctDirty))
                        {
                            Console.WriteLine("Dirty win pcts: " + string.Join(" ", Matchups.Where(x => x.IsWinPctDirty).Select(x => x.Home)));
                            Possibilities.ForEach(x => x.RecalcProbability());
                            CalculateOutcomes();
                        }
                        else if (_poolEntriesDirty)
                        {
                            Console.WriteLine("Pool entries dirty.");
                            CalculateOutcomes();
                        }

                        buf.Append(GetTable());
                        Console.Write(buf.ToString());

                        if (Matchups.Any(x => x.IsWinnerDirty || x.IsWinPctDirty))
                        {
                            gameChangerTable = GetGameChangersTable();
                            Matchups.ForEach(x => x.IsWinnerDirty = false);
                            Matchups.ForEach(x => x.IsWinPctDirty = false);
                            Matchups.ForEach(x => x.PrevHomeWinPct = x.HomeWinPct);
                        }
                        buf.AppendLine(gameChangerTable);

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
                    else Console.WriteLine("  No changes.");

                    while (DateTime.Now < _nextScrapeTime) Thread.Sleep(1000);
                    _nextScrapeTime = DateTime.Now + TimeSpan.FromSeconds(_livePollSeconds);
                    LiveNflData.Instance.Scrape();

                    DateTime now = DateTime.Now;
                    if (now < _poolScrapeTimeFinal && (now < _poolScrapeStart || now < _nextPoolScrapeTime))
                    {
                        _poolEntriesDirty = false;
                        continue;
                    }

                    TryScrapePoolEntries();
                    _nextPoolScrapeTime = now + TimeSpan.FromMinutes(30);
                    if (DateTime.Now - _poolScrapeTimeFinal > TimeSpan.FromMinutes(5))
                    {
                        _poolScrapeTimeFinal += TimeSpan.FromDays(7);
                        _poolScrapeStart += TimeSpan.FromDays(7);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        internal void TryScrapePoolEntries()
        {
            bool entriesKnown = true;
            List<PlayerEntry> newEntries = new List<PlayerEntry>();
            try
            {
                _poolEntriesDirty = false;
                Console.Write("Scraping CBS: ");
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

                    if (!((object[])result["teams"]).Any()) return;
                    _poolEntriesDirty = true;
                    foreach (Dictionary<string, object> team in (object[])result["teams"])
                    {
                        if (!team.ContainsKey("picks")) continue;
                        string name = team["name"].ToString();
                        PlayerEntry entry = new PlayerEntry(name);
                        foreach (KeyValuePair<string, object> pick in (Dictionary<string, Object>)team["picks"])
                        {
                            if (pick.Key == "mnf") continue;
                            Dictionary<string, object> pickData = (Dictionary<string, object>)pick.Value;
                            string winner = pickData["winner"].ToString();
                            int points = int.Parse(pickData["weight"].ToString(), CultureInfo.InvariantCulture);
                            entry.AddPick(winner, points);
                        }
                        newEntries.Add(entry);
                        if (!entry.GamePicks.Any()) entriesKnown = false;
                    }
                    Console.Write(newEntries.Count + ".  ");
                }
                PlayerEntries = newEntries;
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex);
            }
            finally
            {
                _playerEntriesKnown = entriesKnown;
            }
        }

        internal void UploadLatestToAltdex(string contents)
        {
            FtpWebResponse resp = null;
            Stream reqStream = null;
            FtpWebResponse renameResp = null;

            try
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

                resp = (FtpWebResponse)req.GetResponse();
                byte[] bytes = Encoding.UTF8.GetBytes(contents);
                reqStream = req.GetRequestStream();
                reqStream.Write(bytes, 0, bytes.Length);
                reqStream.Dispose();
                resp.Close();

                renameResp = (FtpWebResponse)renameReq.GetResponse();
                renameResp.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                if (reqStream != null) try { reqStream.Dispose(); } catch { }
                if (resp != null) try { resp.Dispose(); } catch { }
                if (renameResp != null) try { renameResp.Dispose(); } catch { }
            }
        }

        internal static void PrintWinningWeekPossibilities()
        {
            List<string> entriesToPrint = new List<string> {"Aaron Hanson"};
            foreach (WeekPossibility wp in Possibilities.Where(x => x.PlayerScores.Count(y => entriesToPrint.Contains(y.Name) && y.Rank == 1) > 0
                                                               && x.PlayerScores.Count(y => !entriesToPrint.Contains(y.Name) && y.Rank == 1) == 0)
                                                        .OrderBy(x => x.Probability)) wp.Print();
        }

        internal string GetTable()
        {
            StringBuilder buf = new StringBuilder();
            buf.AppendLine("\t\tOVERALL\tSOLO\tTIED\t\tAVG.\tMAX\tCURRENT\tAVG.");
            buf.AppendLine("ENTRY NAME\tWIN%\tWIN%\tWIN%\tTREE%\tPOINTS\tPOINTS\tPOINTS\tRANK");
            buf.AppendLine("-------------------------------------------------------------------------------");
            foreach (PlayerEntry entry in PlayerEntries.OrderByDescending(x => x.WinProb).ThenByDescending(x => x.LikelyScore).ThenBy(x => x.Name))
            {
                if (_playerEntriesKnown)
                {
                    int wins = Possibilities.Count(x => x.PlayerScores.Count(y => y.Name.Equals(entry.Name) && y.Rank == 1) == 1);
                    double pct = (double) wins*100/Possibilities.Count();
                    buf.AppendLine(String.Format(CultureInfo.InvariantCulture,
                        "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                        entry.Name.PadRight(15).Substring(0, 15),
                        SmartRound(100*entry.OverallWinProb, 2).PadLeft(7),
                        SmartRound(100 * entry.OutrightWinProb, 2).PadLeft(7),
                        SmartRound(100 * entry.TiedProb, 2).PadLeft(7),
                        SmartRound(pct, 1).PadLeft(6),
                        SmartRound(entry.LikelyScore, 0).PadLeft(3),
                        entry.MaxScore.ToString(CultureInfo.InvariantCulture).PadLeft(3),
                        entry.CurScore.ToString(CultureInfo.InvariantCulture).PadLeft(3),
                        SmartRound(entry.WeightedRank, 1).PadLeft(4)));
                }
                else buf.AppendLine(String.Format(CultureInfo.InvariantCulture, "{0}\t?\t?\t?\t?\t?\t?\t?\t?", entry.Name.PadRight(15).Substring(0,15)));
            }
            buf.AppendLine();
            return buf.ToString();
        }

        internal static string SmartRound(double value, int digits)
        {
            string digitFormat = "0" + (digits > 0 ? "." + new string('0', digits) : "");
            double rounded = Math.Round(value, digits);
            if ((value > 0 && rounded == 0) || (value < 100 && rounded == 100)) return "~" + rounded.ToString(digitFormat, CultureInfo.InvariantCulture);
            return rounded.ToString(digitFormat, CultureInfo.InvariantCulture);
        }

        internal class GameChangerLine
        {
            internal string EntryName { get; set; }
            internal Dictionary<string, double> Winners { get; set; }

            internal GameChangerLine(string entryName)
            {
                EntryName = entryName;
                Winners = new Dictionary<string, double>();
            }
        }

        internal string GetGameChangersTable()
        {
            if (!Matchups.Any(x => string.IsNullOrEmpty(x.Winner))) return string.Empty;

            Console.Write("Building Game Changers");

            gameChangers = new List<GameChangerLine>();
            StringBuilder buf = new StringBuilder();
            buf.AppendLine("WIN PERCENTAGES BASED ON SINGLE GAME OUTCOMES");
            buf.AppendLine("ENTRY NAME          " + string.Join("|", Matchups.Where(x => String.IsNullOrEmpty(x.Winner))
                                                              .Select(x => string.Format("{0}   {1} ", x.Away.PadLeft(5), x.Home.PadLeft(4))).ToArray()));
            buf.AppendLine(new string('-', 20 + 14*Matchups.Count(x => String.IsNullOrEmpty(x.Winner))));

            foreach (Matchup m in Matchups.Where(x => String.IsNullOrEmpty(x.Winner)))
            {
                Console.Write('.');
                m.Winner = m.Away;
                m.Recalc();
                BuildWeekPossibilities();
                CalculateOutcomes();
                foreach (PlayerEntry e in PlayerEntries)
                {
                    GameChangerLine line = gameChangers.FirstOrDefault(x => x.EntryName == e.Name);
                    if (line == null)
                    {
                        line = new GameChangerLine(e.Name);
                        gameChangers.Add(line);
                    }
                    line.Winners[m.Winner] = 100*GetOverallWinProbability(line.EntryName);
                }

                m.Winner = m.Home;
                m.Recalc();
                BuildWeekPossibilities();
                CalculateOutcomes();
                foreach (PlayerEntry e in PlayerEntries)
                {
                    GameChangerLine line = gameChangers.FirstOrDefault(x => x.EntryName == e.Name);
                    if (line == null)
                    {
                        line = new GameChangerLine(e.Name);
                        gameChangers.Add(line);
                    }
                    line.Winners[m.Winner] = 100*GetOverallWinProbability(line.EntryName);
                }

                m.Winner = String.Empty;
                m.Recalc();
            }

            foreach (GameChangerLine line in gameChangers.OrderBy(x => x.EntryName))
            {
                buf.Append(line.EntryName.PadRight(20));
                foreach (Matchup mm in Matchups.Where(x => String.IsNullOrEmpty(x.Winner)))
                {
                    buf.Append(Math.Round(line.Winners[mm.Away], 2).ToString("0.00").PadLeft(6) + ' ' + Math.Round(line.Winners[mm.Home], 2).ToString("0.00").PadLeft(6) + '|');
                }
                buf.Remove(buf.Length - 1, 1);
                buf.AppendLine();
            }

            BuildWeekPossibilities();
            CalculateOutcomes();
            Console.WriteLine("done.");
            return buf.ToString();
        }

        internal double GetOverallWinProbability()
        {
            return Possibilities.Where(x => x.PlayerScores.Count(y => _entryWinCheck.Contains(y.Name) && y.Rank == 1) > 0)
                                .Sum(x => x.Probability * (double)x.PlayerScores.Count(y => _entryWinCheck.Contains(y.Name) && y.Rank == 1) / (double)x.PlayerScores.Count(y => y.Rank == 1));
        }

        internal double GetOverallWinProbability(string entryName)
        {
            return Possibilities.Where(x => x.PlayerScores.Count(y => y.Name == entryName && y.Rank == 1) > 0)
                                .Sum(x => x.Probability * (double)x.PlayerScores.Count(y => y.Name == entryName && y.Rank == 1) / (double)x.PlayerScores.Count(y => y.Rank == 1));
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

        internal void CalculateOutcomes()
        {
            if (!_playerEntriesKnown) return;
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
