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
        private XmlSerializer Serializer;

        private static readonly Lazy<LiveNFLData> instance = new Lazy<LiveNFLData>(() => new LiveNFLData());
        public static LiveNFLData Instance { get { return instance.Value; } }

        public NFLScoreStrip Scores;

        private LiveNFLData()
        {
            Client = new WebClient();
            Serializer = new XmlSerializer(typeof(NFLScoreStrip));
        }

        public void Dispose()
        {
            Client.Dispose();
        }

        public void Scrape()
        {
            NFLScoreStrip ss = (NFLScoreStrip)Serializer.Deserialize(Client.OpenRead(@"http://www.nfl.com/liveupdate/scorestrip/ss.xml"));
            foreach (NFLGame game in ss.GameList.Games) game.SetProbability();
        }
        
    }

    [XmlRoot("ss")]
    public class NFLScoreStrip
    {
        [XmlElement("gms")]
        public NFLGameList GameList { get; set; }
    }

    public class NFLGameList
    {
        [XmlElement("g")]
        public List<NFLGame> Games { get; set; }
    }

    public class NFLGame
    {
        public double LiveHomeWinProbability { get; set; }

        public void SetProbability()
        {
            if (Quarter == "F") LiveHomeWinProbability = (HomeScore > AwayScore ? 1 : 0);
            else LiveHomeWinProbability = LiveWinProbability.Estimate(AwayScore, HomeScore, 0, 60);
        }

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

}
