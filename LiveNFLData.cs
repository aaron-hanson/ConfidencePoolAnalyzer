using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Xml.Serialization;

namespace ConfidencePoolAnalyzer
{
    internal class LiveNflData : IDisposable
    {
        private readonly WebClient _client;
        private readonly XmlSerializer _nflSerializer;
        private readonly XmlSerializer _oddsSerializer;
        private PinnacleOdds _odds;
        private NflScoreStrip _scoreStrip;
        private readonly DateTime _thisWeekStart;
        private readonly DateTime _thisWeekEnd;
        private DateTime _nextOddsScrapeTime;
        private readonly int _oddsPollSeconds;

        private static readonly Lazy<LiveNflData> TheInstance = new Lazy<LiveNflData>(() => new LiveNflData());
        internal static LiveNflData Instance { get { return TheInstance.Value; } }

        private LiveNflData()
        {
            _client = new WebClient();
            _client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/37.0.2062.103 Safari/537.36");
            _nflSerializer = new XmlSerializer(typeof(NflScoreStrip));
            _oddsSerializer = new XmlSerializer(typeof(PinnacleOdds));

            DateTime now = DateTime.Now.Date;
            _thisWeekEnd = now.AddDays((((int)DayOfWeek.Tuesday - (int)now.DayOfWeek + 7) % 7) + 1);
            _thisWeekStart = _thisWeekEnd.AddDays(-7);

            _oddsPollSeconds = int.TryParse(ConfigurationManager.AppSettings["OddsPollSeconds"], out _oddsPollSeconds) ? _oddsPollSeconds : 60;
            _nextOddsScrapeTime = DateTime.Now;
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        internal void Scrape()
        {
            //TODO: don't blindly overwrite Odds/games, so we can keep last known odds/games
            if (DateTime.Now >= _nextOddsScrapeTime)
            {
                _nextOddsScrapeTime = DateTime.Now + TimeSpan.FromSeconds(_oddsPollSeconds);
                try
                {
                    if (_client != null)
                    {
                        Console.Write("Scraping Pinnacle Odds...");
                        _odds = (PinnacleOdds)_oddsSerializer.Deserialize(_client.OpenRead(@"http://xml.pinnaclesports.com/pinnacleFeed.aspx?sporttype=Football&sportsubtype=NFL"));
                        _odds.Games.RemoveAll(x => !x.IsValidNflGame);
                        _odds.Games.RemoveAll(x => x.EventDateTime < _thisWeekStart || x.EventDateTime > _thisWeekEnd);
                        Console.WriteLine(_odds.Games.Count);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            try
            {
                if (_client != null)
                {
                    Console.Write("Scraping NFL Games...");
                    _scoreStrip = (NflScoreStrip)_nflSerializer.Deserialize(_client.OpenRead(@"http://www.nfl.com/liveupdate/scorestrip/ss.xml"));
                    Console.WriteLine(_scoreStrip.Games.Count);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        internal void BuildMatchups(List<Matchup> matchups)
        {
            if (_scoreStrip != null && _scoreStrip.Games != null)
            {
                matchups.AddRange(_scoreStrip.Games.Select(x => new Matchup(x.Away, x.Home, 0.5)));
            }

            if (_odds == null || _odds.Games == null) return;
            foreach (Matchup m in _odds.Games.Select(ev => new Matchup(
                ev.Participants.First(x => !x.IsHome).Name, 
                ev.Participants.First(x => x.IsHome).Name, 
                0.5)).Where(m => !matchups.Any(x => x.Away == m.Away && x.Home == m.Home)))
            {
                matchups.Add(m);
            }
        }

        internal void UpdateMatchup(Matchup m)
        {
            try {
                NflGame game = _scoreStrip.Games.First(x => x.Home.Equals(m.Home) && x.Away.Equals(m.Away));
                m.AwayScore = game.AwayScore;
                m.HomeScore = game.HomeScore;
                m.Quarter = game.Quarter;
                m.TimeLeft = game.TimeLeft;
                m.Possession = game.Possession;
                m.RedZone = game.RedZone == 1;
            }
            catch { }

            try {
                double spread = _odds.Games.First(x => x.Participants.Any(y => y.Name.Equals(m.Home)))
                    .Periods.First(x => x.PeriodNumber == 0).Spread.SpreadHome;
                m.Spread = spread;
            }
            catch { }
        }

    }

    [XmlRoot("ss")]
    public class NflScoreStrip
    {
        [XmlArray("gms")]
        [XmlArrayItem("g")]
        public List<NflGame> Games { get; set; }
    }

    public class NflGame
    {
        [XmlAttribute("q")]
        public string Quarter { get; set; }

        [XmlAttribute("k")]
        public string TimeLeft { get; set; }

        [XmlAttribute("p")]
        public string Possession { get; set; }

        [XmlAttribute("rz")]
        public int RedZone { get; set; }

        [XmlAttribute("h")]
        public string Home { get; set; }

        [XmlAttribute("hs")]
        public int HomeScore { get; set; }

        [XmlAttribute("v")]
        public string Away { get; set; }

        [XmlAttribute("vs")]
        public int AwayScore { get; set; }        
    }

    [XmlRoot("pinnacle_line_feed")]
    public class PinnacleOdds
    {
        [XmlArray("events")]
        [XmlArrayItem("event")]
        public List<Game> Games { get; set; }
    }

    public class Game
    {
        [XmlElement("league")]
        public string League { get; set; }

        [XmlElement("event_datetimeGMT")]
        public string EventDateTimeGmt { get; set; }

        [XmlArray("participants")]
        [XmlArrayItem("participant")]
        public List<Participant> Participants { get; set; }

        [XmlArray("periods")]
        [XmlArrayItem("period")]
        public List<Period> Periods { get; set; }

        internal DateTime EventDateTime
        {
            get
            {
                return EventDateTimeGmt == null ? DateTime.Now : DateTime.ParseExact(EventDateTimeGmt, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();
            }
        }

        internal bool IsValidNflGame
        {
            get
            {
                return Participants.TrueForAll(x => NflTeamAbbrevs.Contains(x.Name));
            }
        }

        private static readonly List<string> NflTeamAbbrevs = new List<string> 
        {
            "MIA", "NE", "BUF", "NYJ",
            "CIN", "CLE", "BAL", "PIT",
            "HOU", "TEN", "IND", "JAC",
            "DEN", "OAK", "KC", "SD",
            "PHI", "WAS", "DAL", "NYG",
            "MIN", "DET", "GB", "CHI",
            "ATL", "CAR", "NO", "TB",
            "SEA", "SF", "ARI", "STL"
        };
    }

    public class Participant
    {
        private string _name;
        [XmlElement("participant_name")]
        public string Name
        {
            get { return _name; }
            set {
                switch (value)
                {
                    case "New York Giants": _name = "NYG"; break;
                    case "New England Patriots": _name = "NE"; break;
                    case "New Orleans Saints": _name = "NO"; break;
                    case "Saint Louis Rams": _name = "STL"; break;
                    case "St. Louis Rams": _name = "STL"; break;
                    case "Tampa Bay Buccaneers": _name = "TB"; break;
                    case "San Diego Chargers": _name = "SD"; break;
                    case "New York Jets": _name = "NYJ"; break;
                    case "Green Bay Packers": _name = "GB"; break;
                    case "Kansas City Chiefs": _name = "KC"; break;
                    case "San Francisco 49ers": _name = "SF"; break;
                    default: _name = value.Substring(0, 3).ToUpper(CultureInfo.InvariantCulture); break;
                }
            }
        }

        [XmlElement("visiting_home_draw")]
        public string HomeVisiting { get; set; }

        internal bool IsHome { get { return HomeVisiting == "Home"; } }   
    }

    public class Period
    {
        [XmlElement("period_number")]
        public int PeriodNumber { get; set; }

        [XmlElement("spread")]
        public Spread Spread { get; set; }
    }

    public class Spread
    {
        [XmlElement("spread_home")]
        public double SpreadHome { get; set; }
    }

}
