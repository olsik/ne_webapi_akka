using Akka.Actor;
using Akka.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace ne_webapi_akka
{
    public class DownloadManager : ReceiveActor
    {

        private IActorRef _downloaderRef; //Downloader
        private List<FundRequest> _requests = new List<FundRequest>();
        private DownloadFinished DF = new DownloadFinished();
        private bool _stopCommand;

        public static Status status = Status.Ready;


        public DownloadManager()
        {
            InitialReceives();
            _downloaderRef = Context.ActorOf(Props.Create(() => new Downloader())
                .WithRouter(new RoundRobinPool(1))
            );
        }

        private void InitialReceives()
        {
            Receive<StartDownload>(par =>
            {
                try
                {
                    _stopCommand = false;
                    _requests.Clear();
                    DF.Responces.Clear();
                    DownloadManager.status = Status.InProcess;

                    FundGroup fg = FundGroups.Groups?.FirstOrDefault(x => x.Name == (par.FundGroupName ?? "nosale"));
                    if (fg == null)
                        Sender.Tell(new FundGroupNotFound());
                    else
                    {
                        DF.From = par.From ?? new DateTime(2020, 03, 29);
                        DF.To = par.To ?? DateTime.Now;
                        DF.FundGroupName = fg.Name;

                        FundRequest cur;
                        foreach (Fund f in fg.Funds)
                        {
                            _requests.Add((cur = new FundRequest
                            {
                                FundId = f.Id,
                                From = DF.From,
                                To = DF.To,
                                Tag = f,
                                status = Status.InProcess,
                                retry = 1,
                            }));
                            if (_downloaderRef != null)
                                _downloaderRef.Tell(cur);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("DownloadManager:StartDownload " + ex.Message);
                }
            });
            Receive<Stop>(job => { _stopCommand = true; });
            Receive<FundResponce>(responce =>
            {
                try
                {
                    if (responce != null)
                    {
                        DF.Responces.Add(responce);
                        FundRequest req = _requests.FirstOrDefault(x => x.FundId == responce.FundId);
                        if (req != null)
                            req.status = Status.Successfuly;
                        if (_requests.Count(x => x.status == Status.Successfuly) == _requests.Count)
                        {
                            status = Status.Successfuly;
                            Context.Parent.Tell(DF);
                        }
                        else if (_requests.Count(x => x.status == Status.InProcess)
                                 + _requests.Count(x => x.status == Status.Failed && x.retry < 3) == 0)
                            status = Status.Failed;
                        else
                            status = Status.InProcess;
                    }

                    // BusinesLogic.Log.Logging(BusinesLogic.OP, "FundResponce");
                    // responce.SaveToFile();
                    // MainWindowViewModel.VP.AddNewSerie(responce);
                }
                catch (Exception ex)
                {
                    Log.Error("DownloadManager:FundResponce " + ex.Message);
                }

            });
            Receive<FailedFun>(res =>
            {
                try
                {
                    if (res != null)
                    {
                        FundRequest req = _requests.FirstOrDefault(x => x.FundId == res.FundId);
                        if (req != null)
                        {
                            req.status = Status.Failed;
                            req.retry++;
                            if (!_stopCommand && req.retry <= 3)
                                _downloaderRef.Tell(req);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("DownloadManager:FailedFun " + ex.Message);
                }
            });
            Receive<StartResearch>(par =>
            {
                try
                {
                    _stopCommand = false;
                    _requests.Clear();
                    DF.Responces.Clear();
                    DownloadManager.status = Status.InProcess;

                    FundGroup fg = FundGroups.Groups?.FirstOrDefault(x => x.Name == Fund.AllGroupName);
                    if (fg == null)
                        Sender.Tell(new FundGroupNotFound());
                    else
                    {
                        DF.From = DateTime.Now.AddMonths(-1);
                        DF.To = DateTime.Now;
                        DF.FundGroupName = fg.Name;

                        FundRequest cur;
                        foreach (Fund f in fg.Funds)
                        {
                            _requests.Add((cur = new FundRequest
                            {
                                FundId = f.Id,
                                From = DF.From,
                                To = DF.To,
                                Tag = f,
                                status = Status.InProcess,
                                retry = 1,
                            }));
                            if (_downloaderRef != null)
                                _downloaderRef.Tell(cur);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("DownloadManager:StartDownload " + ex.Message);
                }
            });
        }
    }
}
