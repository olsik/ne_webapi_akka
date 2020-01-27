using Akka.Actor;
using Akka.Routing;
using System;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using System.Globalization;

namespace ne
{
    public class Downloader : ReceiveActor
    {
        const string TaseRequestFormat
            = @"https://info.tase.co.il/_layouts/tase/Handlers/ChartsDataHandler.ashx?fn=advchartdata&ct=3&ot=4&lang=0&cf=0&cp=8&cv=0&cl=0&cgt=1&dFrom={1}&dTo={2}&oid={0}&_=0";
        const string TaseFundIdFormat = "D8";
        const string TaseRequestDateFormat = @"dd-MM-yyyy";
        const string TaseResponceDateFormat = @"dd/MM/yyyy";

        #region Message classes
        // public class Start
        // { }
        public class Stop
        { }
        #endregion Message classes

        public static object syncObj = new object();
        int m_Index;
        public Downloader()
        {
            // lock (syncObj)
            // {
            //     m_Index = DownloadManager.Index;
            //     DownloadManager.Index++;
            // }
            // BusinesLogic.Log.Logging(BusinesLogic.OP, "Downloader #" + m_Index.ToString() + " - constructor");
            InitialReceives();
        }
        private List<string> ParseWebResponce(string WebResponce, int startIndex)
        {
            List<string> res = new List<string>();
            char s = '['; char e = ']'; string cur_res; int s_ind; int e_ind; int s2_ind; bool isEnd = false;

            while (!isEnd)
            {
                s_ind = WebResponce.IndexOf(s, startIndex);
                e_ind = WebResponce.IndexOf(e, startIndex);
                s2_ind = WebResponce.IndexOf(s, startIndex + 1);
                if (s2_ind != -1 && s2_ind < e_ind)
                    startIndex++;
                else if (e_ind != -1 && s_ind != -1)
                {
                    startIndex = e_ind + 2;
                    cur_res = WebResponce.Substring(s_ind + 1, e_ind - 1 - s_ind);
                    res.Add(cur_res);
                }
                else if (s_ind == -1)
                    isEnd = true;
            }
            return res;
        }
        private void InitialReceives()
        {
            Receive<FundRequest>(job =>
            {
                try
                {
                    BusinesLogic.Log.Logging(BusinesLogic.OP, "Downloader #" + m_Index.ToString() + " - START - " + job.ToString());

                    FundResponce responce = new FundResponce { FundId = job.FundId, Tag = job.Tag, DayValues = new List<DayValue>() };
                    using (WebClient client = new WebClient()) // WebClient class inherits IDisposable
                    {
                        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                        client.Headers["User-Agent"] = "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.2.15) Gecko/20110303 Firefox/3.6.15";
                        string url = string.Format(TaseRequestFormat,
                            job.FundId.ToString(TaseFundIdFormat), job.From.ToString(TaseRequestDateFormat), job.To.ToString(TaseRequestDateFormat));
                        // Program.Log.Logging(Program.OP, url);
                        responce.WebResponce = client.DownloadString(url);
                        List<string> parts = ParseWebResponce(responce.WebResponce, 0);
                        foreach (string part in parts)
                        {
                            string[] Values = part.Split(new char[] { ',', ' ', '"' }, StringSplitOptions.RemoveEmptyEntries);
                            DateTime Date;
                            float Value;

                            if (!float.TryParse(Values[0], NumberStyles.Any, CultureInfo.InvariantCulture, out Value)
                            || !DateTime.TryParseExact(Values[1], TaseResponceDateFormat, null, DateTimeStyles.None, out Date))
                                continue;
                            DayValue dv = new DayValue { Date = Date, Value = Value };

                            responce.DayValues.Add(dv);
                        }

                        responce.DayValues = responce.DayValues.OrderBy(x => x.Date).ToList();
                        for (int i = 0; i < responce.DayValues.Count; i++)
                            responce.DayValues[i].Index = i;

                        if (responce.Tag != null && responce.Tag.IdBuyed)
                            for (int i = responce.DayValues.Count - 1; i >= 0; i--)
                                if (responce.DayValues[i].Date < responce.Tag.BuyDate)
                                    responce.DayValues.RemoveAt(i);

                        responce.DayValuesReorganize();

                        BusinesLogic.Log.Logging(BusinesLogic.OP, "Downloader #" + m_Index.ToString() + " - RECEIVED - " + responce.WebResponce);//.Length.ToString());
                        Sender.Tell(responce);
                    }
                }
                catch (Exception ex)
                {
                    BusinesLogic.Log.Logging(BusinesLogic.OP, "Downloader #" + m_Index.ToString() + " - error - " + ex.Message);
                }
            });

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
        }
    }
}