using System;
using System.Net;

namespace ConfidencePoolAnalyzer
{
    class CookieAwareWebClient : WebClient
    {
        private CookieContainer cc = new CookieContainer();
        private string _lastPage;

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest r = base.GetWebRequest(address);
            HttpWebRequest wr = r as HttpWebRequest;
            if (wr != null)
            {
                wr.CookieContainer = cc;
                if (_lastPage != null) wr.Referer = _lastPage;
            }
            _lastPage = address.ToString();
            return r;
        }

    }
}
