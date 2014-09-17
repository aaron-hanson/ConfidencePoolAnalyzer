using System;
using System.Net;

namespace ConfidencePoolAnalyzer
{
    class CookieAwareWebClient : WebClient
    {
        internal CookieContainer CookieCont = new CookieContainer();
        internal string LastPage;

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest r = base.GetWebRequest(address);
            HttpWebRequest wr = r as HttpWebRequest;
            if (wr != null)
            {
                wr.CookieContainer = CookieCont;
                if (LastPage != null) wr.Referer = LastPage;
            }
            LastPage = address.ToString();
            return r;
        }

    }
}
