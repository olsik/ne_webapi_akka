using System;
using Akka.Actor;
using Serilog;
// using Logging2;
using System.Collections.Generic;
using System.Linq;
// using System.Threading.Tasks;
using System.IO;
// using System.Globalization;
using System.Threading;

namespace ne_webapi_akka
{
    public class BL : ReceiveActor
    {
        private IActorRef _downloadManagerRef;
        private IActorRef _htmlCreatorRef;
        private IActorRef _neControllerRef;
        private IActorRef _researchRef;

        public BL()
        {
            _downloadManagerRef = Context.ActorOf(Props.Create(() => new DownloadManager()));
            _htmlCreatorRef = Context.ActorOf(Props.Create(() => new HtmlCreator()));
            _researchRef = Context.ActorOf(Props.Create(() => new Research()));
            InitialReceives();
        }
        private void InitialReceives()
        {
            Receive<StartDownload>(par =>
            {
                Log.Information("Download started");
                if (_downloadManagerRef != null)
                    _downloadManagerRef.Tell(par);

                StartDownload_Research par_Research = par as StartDownload_Research;
                if (par_Research == null)
                    _neControllerRef = Sender;
            });
            Receive<FundGroupNotFound>(par =>
            {
                if (_neControllerRef != null)
                    _neControllerRef.Tell(null, Self);
            });
            Receive<Status>(par =>
            {
                if (_neControllerRef != null && par == Status.Failed)
                    _neControllerRef.Tell(null, Self);
            });
            Receive<DownloadFinished>(par =>
            {
                Log.Information("Download finished");
                DownloadFinished_Research_Step1 par_Research = par as DownloadFinished_Research_Step1;
                if (par_Research == null)
                {
                    CreateSeries(par);

                    if (_htmlCreatorRef != null)
                        _htmlCreatorRef.Tell(par);
                }
                else
                {
                    Research(par_Research);
                    if (_researchRef != null)
                        _researchRef.Tell(par_Research);
                }
            });
            Receive<HtmlCreated>(par =>
            {
                Log.Information("Html created");
                if (_neControllerRef != null)
                {
                    _neControllerRef.Tell(par.Content, Self);
                    Directory.CreateDirectory(@"./temp");
                    System.IO.File.WriteAllText(@"./temp/out.htm", par.Content);
                }
            });
            Receive<StartResearch_Step1>(par =>
            {
                Log.Information("Research step 1 started");
                if (_downloadManagerRef != null)
                    _downloadManagerRef.Tell(par);
                _neControllerRef = Sender;
            });
            // Receive<Stop>(job => { _stopCommand = true; });
            //     Receive<List<FundResponce>>(responces =>
            //     {
            //         try
            //         {
            //             _responces = responces;
            //             status = Status.Successfuly;

            //         }
            //         catch (Exception ex)
            //         {
            //             BusinesLogic.Log.Logging(BusinesLogic.OP, ex.Message);
            //         }

            //     });
        }

        private void CalculateAverageSerie(ref List<FundResponce> responces)
        {
            // System.Globalization.NumberFormatInfo myInv = System.Globalization.NumberFormatInfo.InvariantInfo;
            List<string> res = new List<string>();
            FundResponce AverData = new FundResponce
            {
                Color = "'#000000'",
                DotStyle = false,
                DayValues = new List<DayValue>(),
                FundId = -1,
            };

            List<DateTime> dates = (from fr in responces
                                    from dv in fr.DayValues
                                    select dv.Date)
                    .Distinct().OrderBy(x => x).ToList();

            for (int d = 0; d < dates.Count; d++)
            {
                float PercentValue = 0; int ValuesCount = 0;
                for (int f = 0; f < responces.Count; f++)
                {
                    DayValue dv = responces[f].DayValues.FirstOrDefault(x => x.Date == dates[d]);
                    if (dv != null)
                    {
                        if (!responces[f].DotStyle)
                        {
                            PercentValue += dv.Percent;
                            ValuesCount++;
                        }
                    }
                }
                float Percent = ValuesCount > 0 ? PercentValue / ValuesCount : 0;
                AverData.DayValues.Add(new DayValue { Date = dates[d], Index = d, Percent = Percent });
            }
            responces.Add(AverData);
        }

        private void CreateSeries(DownloadFinished par)
        {
            List<FundResponce> ResponcesBeforeBuy = new List<FundResponce>();

            for (int i = 0; i < par.Responces.Count; i++)
            {
                par.Responces[i].Color = HtmlCreator.ColorPallete[i % HtmlCreator.ColorPallete.Length];
                int BuyDateIndex = par.Responces[i].FillDayValues();
                if (BuyDateIndex > 0)
                    ResponcesBeforeBuy.Add(par.Responces[i].CreateBeforeBuyResonce(BuyDateIndex));
            }
            par.Responces.AddRange(ResponcesBeforeBuy);

            CalculateAverageSerie(ref par.Responces);
        }
        private void Research(DownloadFinished par)
        {
            for (int i = 0; i < par.Responces.Count; i++)
            {
                par.Responces[i].FillDayValues();

            }

        }

    }
}