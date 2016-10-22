using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using System.Text;
using System.Net;
using System.Web;
using System.IO;
using System.Xml.Linq;
using System.Threading;
using System.Xml;
using System.Security;

namespace PokUtility
{
    public class Translator
    {
        public class AdmAccessToken
        {
            public string access_token { get; set; }
            public string token_type { get; set; }
            public string expires_in { get; set; }
            public string scope { get; set; }
        }

        class TranslatorDataPack
        {
            public string[] Reuqests;
            public string[] Translated;
            public string From;
            public string To;
            public bool Finished;

            public TranslatorDataPack(string[] data, string from, string to)
            {
                Reuqests = data;
                From = from;
                To = to;
                Translated = null;
                Finished = false;
            }
        }

        public static readonly string DatamarketAccessUri = "https://datamarket.accesscontrol.windows.net/v2/OAuth2-13";
        //Access token expires every 10 minutes. Renew it every 9 minutes only.
        private const int RefreshTokenDuration = 9;
        private const int RequestLine = 50;
        private const int MaxCharNum = 1000;
        private const int MaxLine = 100;

        private string clientId;
        private string cientSecret;
        private string request;
        private Timer accessTokenRenewer;
        AdmAccessToken admToken;
        List<TranslatorDataPack> mTranslatorPacks = new List<TranslatorDataPack>();
        Dictionary<string, string> mRequestData = null;

        public int Finished { get; set; }
        public int Failed { get; set; }
        public int Progress { get; set; }
        public Dictionary<string, string> RequestData { get { return mRequestData; } }

        public Translator(string clientId, string clientSecret)
        {
            this.clientId = clientId;
            this.cientSecret = clientSecret;
            //If clientid or client secret has special characters, encode before sending request
            this.request = string.Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=http://api.microsofttranslator.com", System.Uri.EscapeDataString(clientId), System.Uri.EscapeDataString(clientSecret));

            admToken = HttpPost(DatamarketAccessUri, this.request);
            accessTokenRenewer = new Timer(new TimerCallback(OnTokenExpiredCallback), this, TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
        }

        public void BeginTranslate(Dictionary<string, string> requestData, string from, string to)
        {
            if (mTranslatorPacks.Count > 0)
                throw new Exception("Processing.");

            mRequestData = requestData;

            // build the source text array for copy.
            var sourceText = new List<string>();
            sourceText.AddRange(requestData.Keys);

            mTranslatorPacks.Clear();
            int textNum = 0, lineNum = 0;
            for (var i = 0; i < sourceText.Count; i++)
            {
                if (textNum + sourceText[i].Length > MaxCharNum || lineNum > MaxLine)
                {
                    var request = new string[lineNum];
                    sourceText.CopyTo(i - lineNum, request, 0, lineNum);
                    var dataPack = new TranslatorDataPack(request, from, to);
                    mTranslatorPacks.Add(dataPack);

                    textNum = 0;
                    lineNum = 0;
                }
                else
                {
                    textNum += sourceText[i].Length;
                    lineNum ++;
                }
            }

            // the rest.
            if (lineNum > 0)
            {
                var request = new string[lineNum];
                sourceText.CopyTo(sourceText.Count - lineNum, request, 0, lineNum);
                var dataPack = new TranslatorDataPack(request, from, to);
                mTranslatorPacks.Add(dataPack);
            }

            // enqueue thread.
            foreach (var dataPack in mTranslatorPacks)
                ThreadPool.QueueUserWorkItem(new WaitCallback(TranslatorThread), dataPack);
        }

        void TranslatorThread(object state)
        {
            var dataPack = state as TranslatorDataPack;
            dataPack.Finished = false;
            try
            {
                dataPack.Translated = Translate(dataPack.Reuqests, dataPack.From, dataPack.To);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            dataPack.Finished = true;
        }

        void OnFinished(int total, int failed)
        {
            foreach (var dataPack in mTranslatorPacks)
            {
                if (dataPack.Translated == null)
                    continue;

                for (var i = 0; i < dataPack.Reuqests.Length; i++)
                {
                    var request = dataPack.Reuqests[i];
                    var translated = dataPack.Translated[i];

                    if (mRequestData.ContainsKey(request))
                        mRequestData[request] = translated;
                }
            }

            // clear the request
            mTranslatorPacks.Clear();
        }

        public bool CheckFinished()
        {
            Failed = 0;
            Finished = 0;

            var finishedNum = 0;
            foreach (var dataPack in mTranslatorPacks)
            {
                finishedNum += dataPack.Finished ? 1 : 0;
                if (dataPack.Finished)
                {
                    Finished += dataPack.Reuqests.Length;
                    Failed += (dataPack.Translated == null) ? dataPack.Reuqests.Length : 0;
                }
            }

            Progress = finishedNum * 100 / mTranslatorPacks.Count;
            if (finishedNum >= mTranslatorPacks.Count)
            {
                OnFinished(Finished, Failed);
                return true;
            }

            return false;
        }

        public string[] Translate(string[] text, string from, string to)
        {
            var uri = "http://api.microsofttranslator.com/v2/Http.svc/TranslateArray";
            var body = "<TranslateArrayRequest>" +
                             "<AppId />" +
                             "<From>{0}</From>" +
                             "<Options>" +
                                " <Category xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                                 "<ContentType xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\">{1}</ContentType>" +
                                 "<ReservedFlags xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                                 "<State xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                                 "<Uri xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                                 "<User xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                             "</Options>" +
                             "<Texts>" +
                                "{2}" +
                             "</Texts>" +
                             "<To>{3}</To>" +
                          "</TranslateArrayRequest>";
            var sources = "";
            foreach (var source in text)
                sources += string.Format("<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\">{0}</string>", SecurityElement.Escape(source));
            var reqBody = string.Format(body, from, "text/plain", sources, to);
            // create the request
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Headers.Add("Authorization", "Bearer " + admToken.access_token);
            request.ContentType = "text/xml";
            request.Method = "POST";
            request.Proxy = null;

            using (var stream = request.GetRequestStream())
            {
                var arrBytes = System.Text.Encoding.UTF8.GetBytes(reqBody);
                stream.Write(arrBytes, 0, arrBytes.Length);
            }

            // Get the response
            var ret = new string[text.Length];
            WebResponse response = null;
            try
            {
                response = request.GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    using (var rdr = new StreamReader(stream, System.Text.Encoding.UTF8))
                    {
                        // Deserialize the response
                        string strResponse = rdr.ReadToEnd();
                        //Console.WriteLine("Result of translate array method is:");
                        var doc = XDocument.Parse(@strResponse);
                        XNamespace ns = "http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2";
                        var soureceTextCounter = 0;
                        foreach (var xe in doc.Descendants(ns + "TranslateArrayResponse"))
                        {
                            foreach (var node in xe.Elements(ns + "TranslatedText"))
                                ret[soureceTextCounter] = node.Value;
                            soureceTextCounter++;
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                ProcessWebException(ex);
                ret = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                ret = null;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
            }

            return ret;
        }

        private static void ProcessWebException(WebException e)
        {
            //Console.WriteLine("{0}", e.ToString());
            // Obtain detailed error information
            string strResponse = string.Empty;
            using (var response = (HttpWebResponse)e.Response)
            {
                using (var responseStream = response.GetResponseStream())
                {
                    using (var sr = new StreamReader(responseStream, System.Text.Encoding.ASCII))
                    {
                        strResponse = sr.ReadToEnd();
                    }
                }
            }
            Console.WriteLine("Http status code={0}, error message={1}", e.Status, strResponse);
        }

        private AdmAccessToken HttpPost(string DatamarketAccessUri, string requestDetails)
        {
            //Prepare OAuth request 
            var webRequest = WebRequest.Create(DatamarketAccessUri);
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Method = "POST";
            var bytes = Encoding.ASCII.GetBytes(requestDetails);
            webRequest.ContentLength = bytes.Length;
            using (var outputStream = webRequest.GetRequestStream())
            {
                outputStream.Write(bytes, 0, bytes.Length);
            }

            using (var webResponse = webRequest.GetResponse())
            {
                var sr = new StreamReader(webResponse.GetResponseStream());
                var text = sr.ReadToEnd();
                sr.Close();

                return LitJson.JsonMapper.ToObject<AdmAccessToken>(text, true);
            }
        }

        private void OnTokenExpiredCallback(object stateInfo)
        {
            try
            {
                RenewAccessToken();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Failed renewing access token. Details: {0}", ex.Message));
            }
            finally
            {
                try
                {
                    accessTokenRenewer.Change(TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Failed to reschedule the timer to renew access token. Details: {0}", ex.Message));
                }
            }
        }

        private void RenewAccessToken()
        {
            var newAccessToken = HttpPost(DatamarketAccessUri, this.request);
            //swap the new token with old one
            //Note: the swap is thread unsafe
            admToken = newAccessToken;
        }
    }
}
