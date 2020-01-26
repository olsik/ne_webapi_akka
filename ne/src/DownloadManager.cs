using Akka.Actor;
using Akka.Routing;
using System;
using System.Collections.Generic;

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

        // IActorRef D;//Downloader
        // public static int Index = 0;

        public DownloadManager()
        {
            InitialReceives();
            // D = Context.ActorOf(Props.Create(() => new Downloader())
            //     .WithRouter(new RoundRobinPool(5))
            // );
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
                    // List<FundGroup> Groups2=this.Groups;

                    // BusinesLogic.Log.Logging(BusinesLogic.OP, "START");
                    // List<FundRequest> RL = CreateRequestList(par);
                    // for (int i = 0; i < RL.Count; i++)
                    //     D.Tell(RL[i]);
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