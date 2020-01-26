using System;
using Akka.Actor;
using Logging2;
using System.Collections.Generic;
using System.Linq;

namespace ne
{
    public class BusinesLogic
    {
        public static string LogFolder = @"./temp";
        public const string NeFileName = @"./ne.dat";
        public static ActorSystem AS;
        static IActorRef DM;

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
            DM = AS.ActorOf(Props.Create(() => new DownloadManager())
                // .WithDispatcher("akka.actor.synchronized-dispatcher")
                );
        }
        public void GetDataForPage(DateTime? FromDate, DateTime? ToDate, string FundGroupName,
            out string FundsList, out string ValuesList, out string GroupsList)
        {
            FundsList = ""; ValuesList = ""; GroupsList = "";
            if (FromDate == null || ToDate == null || string.IsNullOrEmpty(FundGroupName))
                return;
            FundGroup fg = null;
            if (Groups != null)
            {
                foreach (string gn in GroupNames)
                    GroupsList += "<option value='" + gn + "'>" + gn + "</option>";

                fg = Groups.FirstOrDefault(x => x.Name == FundGroupName);
            }
            if (fg != null)
            {
                DownloadManager.Start st = new DownloadManager.Start(fg) { From = FromDate.Value, To = ToDate.Value };

                for (int i = 0; i < 2; i++)
                {
                    string gn = GroupNames[i];
                    FundsList += "data.addColumn('number', '" + gn + "'); ";
                    // GroupsList="<option value='"+gn+"'>"+gn+"</option>";

                }
            }
            ValuesList = "data.addRows([ [0, 0, 10], [1, 10, 12], [2, 23, 8], [3, 17, 13], [4, 18, 7], [5, 9, 14] ]);";
        }
    }
}