using System;
using System.Collections.Generic;
using System.Net;
using Serilog;
using Akka.Actor;

namespace ne_webapi_akka
{
    public class Downloader : ReceiveActor
    {
        private static object _indexSync = new object();
        private static int _lastIndex = 0;
        private int _index = 0;

        public Downloader()
        {
            lock (_indexSync)
            {
                _index = ++_lastIndex;
            }
            InitialReceives();
        }


        private void InitialReceives()
        {
            Receive<FundRequest>(job =>
            {
                try
                {
                    Log.Information("Downloader #" + _index.ToString() + " Start download job");

                    FundResponce responce = new FundResponce
                    {
                        FundId = job.FundId,
                        Tag = job.Tag,
                        From = job.From,
                        To = job.To,
                        DayValues = new List<DayValue>()
                    };
                    using (WebClient client = new WebClient())
                    {
                        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                        client.Headers["User-Agent"] = Tase.UserAgent;
                        string url = string.Format(Tase.RequestFormat,
                            job.FundId.ToString(Tase.FundIdFormat), job.From.ToString(Tase.RequestDateFormat),
                            job.To.ToString(Tase.RequestDateFormat));
                        // Program.Log.Logging(Program.OP, url);
                        responce.WebResponce = client.DownloadString(url);
                        Log.Information("Downloader #" + _index.ToString() + " Received chars:" + responce.WebResponce.Length.ToString());
                        Sender.Tell(responce);

                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Downloader:FundRequest Downloader #" + _index.ToString() + ex.Message);
                    Sender.Tell(new FailedFun { FundId = job.FundId });
                }
            });
            /*
                        Receive<Stop>(job =>
                        {
                            try
                            {
                                // D.Tell(new PoisonPill());
                                BusinesLogic.Log.Logging(BusinesLogic.OP, "Downloader #" + m_Index.ToString() + " - STOP");
                            }
                            catch (Exception ex)
                            {
                            }
                        });
              */

        }
    }
}
