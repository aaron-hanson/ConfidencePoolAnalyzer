using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace ConfidencePoolAnalyzer
{
    public class LiveNFLData : IDisposable
    {
        private WebClient Client;
        private XmlSerializer NFLSerializer, OddsSerializer;
        private PinnacleOdds Odds;
        private NFLScoreStrip ScoreStrip;

        private static readonly Lazy<LiveNFLData> instance = new Lazy<LiveNFLData>(() => new LiveNFLData());
        public static LiveNFLData Instance { get { return instance.Value; } }

        private LiveNFLData()
        {
            Client = new WebClient();
            NFLSerializer = new XmlSerializer(typeof(NFLScoreStrip));
            OddsSerializer = new XmlSerializer(typeof(PinnacleOdds));
        }

        public void Dispose()
        {
            Client.Dispose();
        }

        public double GetLiveWinProbabilityForMatchup(Matchup m)
        {
            NFLGame game = null;
            Event odds = null;
            int homeScore = 0;
            int awayScore = 0;
            int minRemaining = 60;
            double spread = 0;

            try {
                game = ScoreStrip.Games.First(x => x.Home.Equals(m.Home) && x.Away.Equals(m.Away));
                awayScore = game.AwayScore;
                homeScore = game.HomeScore;
                if (game.Quarter == "F") return homeScore > awayScore ? 1 : 0; //TODO: account for ties?
            }
            catch (Exception) {}

            try {
                odds = Odds.Events.First(x => x.Participants.Any(y => y.Abbrev.Equals(m.Home)));
                spread = odds.Periods.First(x => x.PeriodNumber == 0).Spread.SpreadHome;
            }
            catch (Exception) {}

            return LiveWinProbability.Estimate(awayScore, homeScore, spread, minRemaining);
        }

        public List<NFLGame> GetFinalGames()
        {
            if (ScoreStrip == null || ScoreStrip.Games == null) return new List<NFLGame>();
            return ScoreStrip.Games.Where(x => x.Quarter == "F").ToList();
        }

        public void Scrape()
        {
            //TODO: don't blindly overwrite Odds, so we can keep last known odds
            Odds = (PinnacleOdds)OddsSerializer.Deserialize(Client.OpenRead(@"http://xml.pinnaclesports.com/pinnacleFeed.aspx?sporttype=Football&sportsubtype=NFL"));
            ScoreStrip = (NFLScoreStrip)NFLSerializer.Deserialize(Client.OpenRead(@"http://www.nfl.com/liveupdate/scorestrip/ss.xml"));
        }
        
    }

    [XmlRoot("ss")]
    public class NFLScoreStrip
    {
        [XmlArray("gms")]
        [XmlArrayItem("g")]
        public List<NFLGame> Games { get; set; }
    }

    public class NFLGame
    {
        public double Spread { get; set; }
        public int MinutesLeft { get; set; }
        public double LiveHomeWinProbability { get; set; }

        public void SetProbability()
        {
            if (Quarter == "F") LiveHomeWinProbability = (HomeScore > AwayScore ? 1 : 0);
            else LiveHomeWinProbability = LiveWinProbability.Estimate(AwayScore, HomeScore, Spread, Quarter == "P" ? 60 : MinutesLeft);
        }

        public string Winner { get {return (Quarter != "F" ? "" : (HomeScore > AwayScore ? Home : Away));} }

        [XmlAttribute("q")] 
        public string Quarter { get; set; }

        [XmlAttribute("k")]
        public string TimeLeft { get; set; }

        [XmlAttribute("rz")]
        public string RedZone { get; set; }

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
        public List<Event> Events { get; set; }
    }

    public class Event
    {
        [XmlElement("league")]
        public string League { get; set; }

        [XmlArray("participants")]
        [XmlArrayItem("participant")]
        public List<Participant> Participants { get; set; }

        [XmlArray("periods")]
        [XmlArrayItem("period")]
        public List<Period> Periods { get; set; }
    }

    public class Participant
    {
        [XmlElement("participant_name")]
        public string Name { get; set; }
        public string Abbrev
        {
            get
            {
                switch (Name)
                {
                    case "New York Giants": return "NYG";
                    case "New Englang Patriots": return "NE";
                    case "New Orleans Saints": return "NO";
                    case "Saint Louis Rams": return "STL";
                    case "Tampa Bay Buccaneers": return "TB";
                    case "San Diego Chargers": return "SD";
                    case "New York Jets": return "NYJ";
                    case "Green Bay Packers": return "GB";
                    case "Kansas City Chiefs": return "KC";
                    case "San Francisco 49ers": return "SF";
                    default: return Name.Substring(0,3).ToUpper();
                }
                        
            }
        }

        [XmlElement("visiting_home_draw")]
        public string HomeVisiting { get; set; }
        public bool IsHome { get { return HomeVisiting == "Home"; } }   
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
