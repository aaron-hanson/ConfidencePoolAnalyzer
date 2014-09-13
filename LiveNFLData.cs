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

        public void UpdateMatchup(Matchup m)
        {
            try {
                NFLGame game = ScoreStrip.Games.First(x => x.Home.Equals(m.Home) && x.Away.Equals(m.Away));
                m.AwayScore = game.AwayScore;
                m.HomeScore = game.HomeScore;
                m.Quarter = game.Quarter;
                m.TimeLeft = game.TimeLeft;
            }
            catch (Exception) {}

            try {
                double spread = Odds.Events.First(x => x.Participants.Any(y => y.Name.Equals(m.Home)))
                    .Periods.First(x => x.PeriodNumber == 0).Spread.SpreadHome;
                m.Spread = spread;
            }
            catch (Exception) {}
        }

        public void Scrape()
        {
            //TODO: don't blindly overwrite Odds/games, so we can keep last known odds/games
            try
            {
                Odds = (PinnacleOdds)OddsSerializer.Deserialize(Client.OpenRead(@"http://xml.pinnaclesports.com/pinnacleFeed.aspx?sporttype=Football&sportsubtype=NFL"));
            }
            catch (Exception) { }
            try
            {
                ScoreStrip = (NFLScoreStrip)NFLSerializer.Deserialize(Client.OpenRead(@"http://www.nfl.com/liveupdate/scorestrip/ss.xml"));
            }
            catch (Exception) { }
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
        private string _name;
        [XmlElement("participant_name")]
        public string Name {
            get { return _name; }
            set {
                switch (value)
                {
                    case "New York Giants": _name = "NYG"; break;
                    case "New Englang Patriots": _name = "NE"; break;
                    case "New Orleans Saints": _name = "NO"; break;
                    case "Saint Louis Rams": _name = "STL"; break;
                    case "Tampa Bay Buccaneers": _name = "TB"; break;
                    case "San Diego Chargers": _name = "SD"; break;
                    case "New York Jets": _name = "NYJ"; break;
                    case "Green Bay Packers": _name = "GB"; break;
                    case "Kansas City Chiefs": _name = "KC"; break;
                    case "San Francisco 49ers": _name = "SF"; break;
                    default: _name = value.Substring(0, 3).ToUpper(); break;
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
