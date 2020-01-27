using System;
using Akka.Actor;
using Akka.Routing;
using Logging2;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ne
{
    public class BusinesLogic
    {
        public static string LogFolder = @"./temp";
        public const string NeFileName = @"./ne.dat";
        public static ActorSystem AS;
        // static IActorRef DM;
        static IActorRef D;//Downloader

        public static Log2 Log;
        public static OutputPoints2 OP;
        public static BusinesLogic BL;

        public BusinesLogic()
        {
            InitBL();
        }
        private List<FundGroup> _Groups;
        public List<FundGroup> Groups
        {
            get
            {
                if (_Groups == null)
                {
                    string ErrMes;
                    ISettings settings = new Settings();
                    _Groups = settings.GetFundGroups(out ErrMes);
                    if (!string.IsNullOrEmpty(ErrMes))
                        Log.Logging(OP, ErrMes, null, LogLevel.Hi);
                }
                return _Groups;
            }
        }

        // FundGroups _FundGroups;
        // public FundGroups FundGroups
        // {
        //     get
        //     {
        //         if (_FundGroups == null)
        //             _FundGroups = FundGroups.LoadFromFile();
        //         return _FundGroups;
        //     }
        // }
        public List<string> GroupNames { get { return Groups?.Select(x => x.Name).ToList(); } }
        void InitBL()
        {
            Log = new Log2();
            OP = new OutputPoints2();
            OutputPoints2.UsingEncodingForFileOut(-1);
            OutputPoints2.DefaultSerializeType = MessageSerializeType.DateAndMes;
            OP.CreatePoint_OutputToFile(null, LogFolder);
            OP.CreatePoint_OutputToConsoleScreen();
            Log.Logging(OP, "start");
            AS = ActorSystem.Create("ne-akka");
            // DM = AS.ActorOf(Props.Create(() => new DownloadManager())
            //     // .WithDispatcher("akka.actor.synchronized-dispatcher")
            //     );
            D = AS.ActorOf(Props.Create(() => new Downloader())
                .WithRouter(new RoundRobinPool(1))
            );
        }
        public void GetDataForPage(DateTime? FromDate, DateTime? ToDate, string FundGroupName,
            out string FundsList, out string ValuesList, out string GroupsList, out string FromValue, out string ToValue)
        {
            const string DateFormat = @"yyyy-MM-dd";
            FundsList = ""; ValuesList = ""; GroupsList = ""; FromValue = ""; ToValue = "";
            if (Groups == null)
                return;
            if (!FromDate.HasValue)
                FromDate = new DateTime(2020, 1, 1);
            if (!ToDate.HasValue)
                ToDate = new DateTime(2020, 2, 1);
            if (string.IsNullOrEmpty(FundGroupName))
                FundGroupName = GroupNames[GroupNames.Count - 1];
            FromValue = FromDate.Value.ToString(DateFormat);
            ToValue = ToDate.Value.ToString(DateFormat);

            foreach (string gn in GroupNames)
            {
                if (gn == FundGroupName)
                    GroupsList += "<option value='" + gn + "' selected>" + gn + "</option>";
                else
                    GroupsList += "<option value='" + gn + "'>" + gn + "</option>";
            }

            FundGroup fg = Groups.FirstOrDefault(x => x.Name == FundGroupName);
            if (fg != null)
            {
                Task<object>[] tasks = new Task<object>[2];

                for (int i = 0; i < 2; i++)
                    // Fund f = fg.Funds[0];
                    // Fund f = fg.Funds[0];
                    // foreach (Fund f in fg.Funds)
                    tasks[i] = D.Ask(new FundRequest { FundId = fg.Funds[i].Id, From = FromDate.Value, To = ToDate.Value, Tag = fg.Funds[i] });
                Task.WaitAll(tasks);

                List<FundResponce> res = new List<FundResponce>();
                FundResponce CurResponce;
                foreach (Task<object> t in tasks)
                    if (null != (CurResponce = t.Result as FundResponce))
                        res.Add(CurResponce);


                // DownloadManager.Start st = new DownloadManager.Start(fg) { From = FromDate.Value, To = ToDate.Value };
                // Task<object> res = DM.Ask(st);
                // res.Wait();
                // List<FundResponce> responces= res.Result as List<FundResponce>;
                if (res != null)
                    for (int i = 0; i < res.Count; i++)
                    {
                        string gn = res[i].FundId.ToString();
                        FundsList += "data.addColumn('number', '" + gn + "'); ";
                        // GroupsList="<option value='"+gn+"'>"+gn+"</option>";

                    }
            }
            ValuesList = "data.addRows([ [0, 0, 10], [1, 10, 12], [2, 7, 8], [3, 17, 13], [4, 18, 7], [5, 9, 14] ]);";
        }
    }
}