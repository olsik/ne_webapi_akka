using System;
using Akka.Actor;
//using Akka.Routing;
using System.Collections.Generic;

namespace ne_webapi_akka
{
    public static class AS
    {
        static ActorSystem _AS = ActorSystem.Create("ne-akka");
        public static IActorRef _businesLogicRef
            = _AS.ActorOf(Props.Create(() => new BL()));

    }

    // public class Start : FundGroup
    // {
    //     public DateTime From;
    //     public DateTime To;

    //     public Start(FundGroup fg)
    //     {
    //         Name = fg.Name;
    //         Funds = fg.Funds;
    //     }
    // }
    public class StartDownload
    {
        public DateTime? From;
        public DateTime? To;
        public string FundGroupName;
    }
    public class StartResearch
    {
        // public DateTime? From;
        // public DateTime? To;
        // public string FundGroupName;
    }
    
    public class FundGroupNotFound
    {
    }
    public class DownloadFinished
    {
        public DateTime From;
        public DateTime To;
        public string FundGroupName;
        public List<FundResponce> Responces = new List<FundResponce>();
    }
    public class HtmlCreated
    {
        public string Content;
    }

     class FailedFun
    {
        public int FundId;
    }

    public class Stop
    {
    }


}