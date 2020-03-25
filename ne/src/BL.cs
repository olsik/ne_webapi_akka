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
        public void GetDataForPage(int TemplateIndex, DateTime? FromDate, DateTime? ToDate, string FundGroupName,
            out string FundsList, out string ValuesList, out string GroupsList, out string FromValue, out string ToValue)
        {
            if (TemplateIndex == 0)
                GetDataForPage_0(FromDate, ToDate, FundGroupName,
                            out FundsList, out ValuesList, out GroupsList, out FromValue, out ToValue);
            else //if(TemplateIndex==1)
                GetDataForPage_1(FromDate, ToDate, FundGroupName,
                            out FundsList, out ValuesList, out GroupsList, out FromValue, out ToValue);
        }
        public void GetDataForPage_0(DateTime? FromDate, DateTime? ToDate, string FundGroupName,
            out string FundsList, out string ValuesList, out string GroupsList, out string FromValue, out string ToValue)
        {
            System.Globalization.NumberFormatInfo myInv = System.Globalization.NumberFormatInfo.InvariantInfo;

            const string DateFormat = @"yyyy-MM-dd";
            FundsList = ""; ValuesList = ""; GroupsList = ""; FromValue = ""; ToValue = "";
            if (Groups == null)
                return;
            if (!FromDate.HasValue)
                FromDate = new DateTime(2020, 1, 1);
            if (!ToDate.HasValue)
                ToDate = DateTime.Now.Date;
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
                Task<object>[] tasks = new Task<object>[fg.Funds.Count];

                for (int i = 0; i < fg.Funds.Count; i++)
                    tasks[i] = D.Ask(new FundRequest { FundId = fg.Funds[i].Id, From = FromDate.Value, To = ToDate.Value, Tag = fg.Funds[i] });
                Task.WaitAll(tasks);

                List<FundResponce> res = new List<FundResponce>();
                FundResponce CurResponce;
                foreach (Task<object> t in tasks)
                    if (null != (CurResponce = t.Result as FundResponce))
                        res.Add(CurResponce);
                res = res.OrderBy(x => x.FundId).ToList();

                // foreach(FundResponce r in res)
                //     r.SaveToFile();

                // DownloadManager.Start st = new DownloadManager.Start(fg) { From = FromDate.Value, To = ToDate.Value };
                // Task<object> res = DM.Ask(st);
                // res.Wait();
                // List<FundResponce> responces= res.Result as List<FundResponce>;

                if (res == null)
                    return;

                for (int f = 0; f < res.Count; f++)
                    FundsList += "data.addColumn('number', '" + res[f].FundId.ToString() + "'); ";
                FundsList += "data.addColumn('number', 'Average'); ";

                List<DateTime> dates = (from fr in res
                                        from dv in fr.DayValues
                                        select dv.Date)
                                    .Distinct().OrderBy(x => x).ToList();
                float averValue;
                ValuesList = "data.addRows([ ";
                for (int d = 0; d < dates.Count; d++)
                {
                    averValue = 0;
                    string curValue = string.Format("[new Date ({0}, {1}, {2}), ", dates[d].Year, dates[d].Month - 1, dates[d].Day);
                    for (int f = 0; f < res.Count; f++)
                    {
                        DayValue dv = res[f].DayValues.FirstOrDefault(x => x.Date == dates[d]);
                        if (dv != null)
                        {
                            curValue += dv.Percent.ToString(myInv) + ",";
                            averValue += dv.Percent;
                        }
                        else
                        {
                            curValue += " ,";
                            int step = 1; DayValue dv2 = null; DateTime prevDate;
                            do
                            {
                                prevDate = dates[d].AddDays(-step);
                                dv2 = res[f].DayValues.FirstOrDefault(x => x.Date == prevDate);
                                step++;
                            } while (dv2 == null && step < 30 && prevDate >= FromDate);
                            if (dv2 != null)
                                averValue += dv2.Percent;
                        }
                    }
                    ValuesList += curValue + (averValue / res.Count).ToString(myInv) + ",], "; //add fiction series
                }
                ValuesList = ValuesList.Substring(0, ValuesList.Length - 2) + " ]);";
            }
        }
        public void GetDataForPage_1(DateTime? FromDate, DateTime? ToDate, string FundGroupName,
            out string FundsList, out string ValuesList, out string GroupsList, out string FromValue, out string ToValue)
        {
            System.Globalization.NumberFormatInfo myInv = System.Globalization.NumberFormatInfo.InvariantInfo;

            const string DateFormat = @"yyyy-MM-dd";
            FundsList = ""; ValuesList = ""; GroupsList = ""; FromValue = ""; ToValue = "";
            if (Groups == null)
                return;
            if (!FromDate.HasValue)
                FromDate = new DateTime(2020, 1, 1);
            if (!ToDate.HasValue)
                ToDate = DateTime.Now.Date;
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
                Task<object>[] tasks = new Task<object>[fg.Funds.Count];

                for (int i = 0; i < fg.Funds.Count; i++)
                    tasks[i] = D.Ask(new FundRequest { FundId = fg.Funds[i].Id, From = FromDate.Value, To = ToDate.Value, Tag = fg.Funds[i] });
                Task.WaitAll(tasks);

                List<FundResponce> res = new List<FundResponce>();
                FundResponce CurResponce;
                foreach (Task<object> t in tasks)
                    if (null != (CurResponce = t.Result as FundResponce))
                        res.Add(CurResponce);
                res = res.OrderBy(x => x.FundId).ToList();

                // foreach(FundResponce r in res)
                //     r.SaveToFile();

                if (res == null)
                    return;

                FundsList = ", colors: [";
                int ResIndex = 0;
                while (ResIndex < res.Count)
                {
                    FundsList += ne.Controllers.NEController.ColorPallete[ResIndex % ne.Controllers.NEController.ColorPallete.Length] + ",";
                    ResIndex++;
                }
                FundsList += "'#000000'], series: {";
                ResIndex = 0;
                while (ResIndex < res.Count)
                {
                    FundsList += ResIndex.ToString() + ": { lineWidth: 1 },";
                    ResIndex++;
                }
                FundsList += ResIndex.ToString() + ": { lineWidth: 2 }}";
                // series: {
                // 	0: { lineWidth: 1 },
                // 	1: { lineWidth: 2 },
                // 	2: { lineWidth: 4 },
                // 	3: { lineWidth: 8 },
                // 	4: { lineWidth: 16 },
                // 	5: { lineWidth: 24 }
                // },

                List<string> vl = new List<string>();
                vl.Add("var data = google.visualization.arrayToDataTable([['X'");
                // ValuesList = "var data = google.visualization.arrayToDataTable([['X'";
                for (int f = 0; f < res.Count; f++)
                    vl.Add(", '" + res[f].FundId.ToString() + "'");
                // ValuesList += ", '" + res[f].FundId.ToString() + "'";
                vl.Add(",'Average']");
                // ValuesList += ",'Average']";
                List<DateTime> dates = (from fr in res
                                        from dv in fr.DayValues
                                        select dv.Date)
                                    .Distinct().OrderBy(x => x).ToList();
                float averValue;
                for (int d = 0; d < dates.Count; d++)
                {
                    averValue = 0;
                    vl.Add(string.Format(", [new Date ({0}, {1}, {2})", dates[d].Year, dates[d].Month - 1, dates[d].Day));
                    // string curValue = string.Format(", [new Date ({0}, {1}, {2})", dates[d].Year, dates[d].Month - 1, dates[d].Day);
                    if (d == 0)
                        for (int f = 0; f < res.Count; f++)
                            vl.Add(",0");
                    else
                        for (int f = 0; f < res.Count; f++)
                        {
                            DayValue dv = res[f].DayValues.FirstOrDefault(x => x.Date == dates[d]);
                            if (dv != null)
                            {
                                vl.Add(", " + dv.Percent.ToString(myInv));
                                // curValue += ", " + dv.Percent.ToString(myInv);
                                averValue += dv.Percent;
                            }
                            else
                            {
                                vl.Add(", ");
                                // curValue += ", ";
                                int step = 1; DayValue dv2 = null; DateTime prevDate;
                                do
                                {
                                    prevDate = dates[d].AddDays(-step);
                                    dv2 = res[f].DayValues.FirstOrDefault(x => x.Date == prevDate);
                                    step++;
                                } while (dv2 == null && step < 30 && prevDate >= FromDate);
                                if (dv2 != null)
                                    averValue += dv2.Percent;
                            }
                        }
                    vl.Add(", " + (averValue / res.Count).ToString(myInv) + "]");
                    // ValuesList += ", " + curValue + (averValue / res.Count).ToString(myInv) + "]"; //add fiction series
                }
                vl.Add(" ]);");
                // ValuesList = ValuesList.Substring(0, ValuesList.Length - 2) + " ]);";
                ValuesList = string.Join(null, vl);
            }
        }
    }
}