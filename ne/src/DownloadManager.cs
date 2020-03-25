using Akka.Actor;
using Akka.Routing;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ne
{
    public class DownloadManager : ReceiveActor
    {
        // private List<FundGroup> _Groups;
        // public List<FundGroup> Groups
        // {
        //     get
        //     {
        //         if (_Groups == null)
        //         {
        //             ISettings settings = new Settings();
        //             _Groups = settings.GetFundGroups();
        //         }
        //         return _Groups;
        //     }
        // }
        #region Message classes
        // public class LoadConfig
        // {}
        public class Start : FundGroup
        {
            public DateTime From;
            public DateTime To;
            public Start(FundGroup fg)
            {
                Name = fg.Name;
                Funds = fg.Funds;
            }
        }
        // public class Stop
        // { }
        #endregion Message classes

        IActorRef D;//Downloader
        // public static int Index = 0;

        public DownloadManager()
        {
            InitialReceives();
            D = Context.ActorOf(Props.Create(() => new Downloader())
                .WithRouter(new RoundRobinPool(1))
            );
        }
        // private List<FundRequest> CreateRequestList(Start par)
        // {
        //     List<FundRequest> res = new List<FundRequest>();
        //     foreach (Fund f in par.Funds)
        //         res.Add(new FundRequest { FundId = f.Id, From = par.From, To = par.To, Tag = f });
        //     return res;
        // }
        private void InitialReceives()
        {
            Receive<Start>(par =>
            {
                try
                {
                    // BusinesLogic.Log.Logging(BusinesLogic.OP, "START");
                    // List<FundRequest> RL = CreateRequestList(par);
                    // for (int i = 0; i < RL.Count; i++)
                    //     D.Tell(RL[i]);
                    
                    // Task[] res = new Task[par.Funds.Count];
                    Task<object>[] tasks = new Task<object>[1];

                    Fund f = par.Funds[0];
                    // foreach (Fund f in par.Funds)
                        tasks[0] = D.Ask(new FundRequest { FundId = f.Id, From = par.From, To = par.To, Tag = f });
                    Task.WaitAll(tasks);
                    
                    List<FundResponce> res = new List<FundResponce>();
                    FundResponce CurResponce;
                    foreach(Task<object> t in tasks)
                        if(null != (CurResponce=t.Result as FundResponce))
                            res.Add(CurResponce);
                    Sender.Tell(res);
                }
                catch (Exception ex)
                {
                    // BusinesLogic.Log.Logging(BusinesLogic.OP, ex.Message);
                }
            });

            //     Receive<Stop>(job =>
            //     {
            //         try
            //         {
            //             D.Tell(PoisonPill.Instance);
            //             // D.Tell(new Downloader.Stop());
            //             BusinesLogic.Log.Logging(BusinesLogic.OP, "STOP");
            //         }
            //         catch (Exception ex)
            //         {
            //         }
            //     });
            //     Receive<FundResponce>(responce =>
            //     {
            //         try
            //         {
            //             BusinesLogic.Log.Logging(BusinesLogic.OP, "FundResponce");
            //             responce.SaveToFile();
            //             MainWindowViewModel.VP.AddNewSerie(responce);
            //         }
            //         catch (Exception ex)
            //         {
            //         }
            //     });
        }

    }
}