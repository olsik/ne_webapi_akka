using System;
using Akka.Actor;
using System.Collections.Generic;

namespace ne_webapi_akka
{
    public static class AS
    {
        static ActorSystem _AS = ActorSystem.Create("ne-akka");
        public static IActorRef _businesLogicRef
            = _AS.ActorOf(Props.Create(() => new BL()));
    }
    public class StartDownload
    {
        public DateTime? From;
        public DateTime? To;
        public string FundGroupName;
    }
    public class StartDownload_Research:StartDownload
    {}
    public class StartResearch_Step1
    {}
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
    public class DownloadFinished_Research_Step1 : DownloadFinished
    { }
    public class DownloadFinished_Research_Step2 : DownloadFinished
    { }
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