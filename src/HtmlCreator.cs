using System;
using System.Collections.Generic;
using System.Net;
using Serilog;
using Akka.Actor;
using System.Linq;

namespace ne_webapi_akka
{
    public class HtmlCreator : ReceiveActor
    {
        public static string[] ColorPallete = new string[] {
            "'#3366cc'",
            "'#dc3912'",
            "'#ff9900'",
            "'#109618'",
            "'#990099'",
            "'#0099c6'",
            "'#dd4477'",
            "'#66aa00'",
            "'#b82e2e'",
            "'#316395'",
            "'#994499'",
            "'#22aa99'",
            "'#aaaa11'",
            "'#6633cc'",
            "'#e67300'",
            "'#8b0707'",
            "'#651067'",
            "'#329262'",
            "'#5574a6'",
            "'#3b3eac'",
            "'#b77322'",
            "'#16d620'",
            "'#b91383'",
            "'#f4359e'",
            "'#9c5935'",
            "'#a9c413'",
            "'#2a778d'",
            "'#668d1c'",
            "'#bea413'",
            "'#0c5922'",
            "'#743411'"};

        public HtmlCreator()
        {
            InitialReceives();
        }
        string _Template;
        string Template
        {
            get
            {
                if (string.IsNullOrEmpty(_Template)
                && System.IO.File.Exists(Settings.WebformFileName))
                    _Template = System.IO.File.ReadAllText(Settings.WebformFileName);
                return _Template;
            }
        }

        string CreatePage(DownloadFinished df)
        {
            string FundsList; string ValuesList; string GroupsList = "";
            if (string.IsNullOrEmpty(Template))
                return "Template NotFound";

            System.Globalization.NumberFormatInfo myInv = System.Globalization.NumberFormatInfo.InvariantInfo;

            foreach (string gn in FundGroups.GroupNames)
            {
                if (gn == df.FundGroupName)
                    GroupsList += "<option value='" + gn + "' selected>" + gn + "</option>";
                else
                    GroupsList += "<option value='" + gn + "'>" + gn + "</option>";
            }

            FundsList = ", colors: [";
            for (int i = 0; i < df.Responces.Count; i++)
            {
                if (i != 0)
                    FundsList += ",";
                FundsList += df.Responces[i].Color;
            }
            FundsList += "], series: {";

            for (int i = 0; i < df.Responces.Count; i++)
            {
                if (i != 0)
                    FundsList += ",";
                if (df.Responces[i].DotStyle)
                    FundsList += i.ToString() + ": { lineWidth: 1, lineDashStyle: [4, 4] }";
                else
                {
                    if (df.Responces[i].FundId == -1)
                        FundsList += i.ToString() + ": { lineWidth: 2 }";
                    else
                        FundsList += i.ToString() + ": { lineWidth: 1 }";
                }
            }
            FundsList += "}";

            List<string> vl = new List<string>();
            vl.Add("var data = google.visualization.arrayToDataTable([['X'");
            for (int f = 0; f < df.Responces.Count; f++)
                if (df.Responces[f].FundId == -1)
                    vl.Add(",'Average']");
                else
                    vl.Add(", '" + df.Responces[f].FundId.ToString() + "'");

            List<DateTime> dates = (from fr in df.Responces
                                    from dv in fr.DayValues
                                    select dv.Date)
                                .Distinct().OrderBy(x => x).ToList();
            for (int d = 0; d < dates.Count; d++)
            {
                vl.Add(string.Format(", [new Date ({0}, {1}, {2})", dates[d].Year, dates[d].Month - 1, dates[d].Day));
                for (int f = 0; f < df.Responces.Count; f++)
                {
                    DayValue dv = df.Responces[f].DayValues.FirstOrDefault(x => x.Date == dates[d]);
                    if (dv != null)
                        vl.Add(", " + dv.Percent.ToString(myInv));
                    else
                        vl.Add(", ");
                }
                vl.Add("]");
            }
            vl.Add(" ]);");
            ValuesList = string.Join(null, vl);

            string FromValue = df.From.ToString(Fund.NeDateFormat);
            string ToValue = df.To.ToString(Fund.NeDateFormat);

            string content = Template
               .Replace("<FundsList/>", FundsList)
               .Replace("<ValuesList/>", ValuesList)
               .Replace("//##ValuesList##", ValuesList)
               .Replace("//##Options##", FundsList)
               .Replace("//##GroupsList##", GroupsList)
               .Replace("FromValue", FromValue)
               .Replace("ToValue", ToValue);

            return content;
        }
        string CreatePage_0(DownloadFinished df)
        {

            string FundsList; string ValuesList; string GroupsList = "";
            if (string.IsNullOrEmpty(Template))
                return "Template NotFound";

            System.Globalization.NumberFormatInfo myInv = System.Globalization.NumberFormatInfo.InvariantInfo;

            foreach (string gn in FundGroups.GroupNames)
            {
                if (gn == df.FundGroupName)
                    GroupsList += "<option value='" + gn + "' selected>" + gn + "</option>";
                else
                    GroupsList += "<option value='" + gn + "'>" + gn + "</option>";
            }

            FundsList = ", colors: [";
            for (int i = 0; i < df.Responces.Count; i++)
                FundsList += df.Responces[i].Color + ",";

            // int ResIndex = 0;
            // while (ResIndex < df.Responces.Count)
            // {
            //     FundsList += ColorPallete[ResIndex % ColorPallete.Length] + ",";
            //     ResIndex++;
            // }
            FundsList += "'#000000'], series: {";

            for (int i = 0; i < df.Responces.Count; i++)
                if (df.Responces[i].DotStyle)
                    FundsList += i.ToString() + ": { lineWidth: 1, lineDashStyle: [4, 4] },";
                else
                    FundsList += i.ToString() + ": { lineWidth: 1 },";

            FundsList += df.Responces.Count.ToString() + ": { lineWidth: 2 }}";

            // ResIndex = 0;
            // while (ResIndex < df.Responces.Count)
            // {
            //     FundsList += ResIndex.ToString() + ": { lineWidth: 1 },";
            //     ResIndex++;
            // }
            // FundsList += ResIndex.ToString() + ": { lineWidth: 2 }}";

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
            for (int f = 0; f < df.Responces.Count; f++)
                vl.Add(", '" + df.Responces[f].FundId.ToString() + "'");
            // ValuesList += ", '" + res[f].FundId.ToString() + "'";
            vl.Add(",'Average']");
            // ValuesList += ",'Average']";
            List<DateTime> dates = (from fr in df.Responces
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
                    for (int f = 0; f < df.Responces.Count; f++)
                        vl.Add(",0");
                else
                    for (int f = 0; f < df.Responces.Count; f++)
                    {
                        DayValue dv = df.Responces[f].DayValues.FirstOrDefault(x => x.Date == dates[d]);
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
                                dv2 = df.Responces[f].DayValues.FirstOrDefault(x => x.Date == prevDate);
                                step++;
                            } while (dv2 == null && step < 30 && prevDate >= df.From);
                            if (dv2 != null)
                                averValue += dv2.Percent;
                        }
                    }
                vl.Add(", " + (averValue / df.Responces.Count).ToString(myInv) + "]");
                // ValuesList += ", " + curValue + (averValue / res.Count).ToString(myInv) + "]"; //add fiction series
            }
            vl.Add(" ]);");
            // ValuesList = ValuesList.Substring(0, ValuesList.Length - 2) + " ]);";
            ValuesList = string.Join(null, vl);


            string FromValue = df.From.ToString(Fund.NeDateFormat);
            string ToValue = df.To.ToString(Fund.NeDateFormat);

            string content = Template
               .Replace("<FundsList/>", FundsList)
               .Replace("<ValuesList/>", ValuesList)
               .Replace("//##ValuesList##", ValuesList)
               .Replace("//##Options##", FundsList)
               .Replace("//##GroupsList##", GroupsList)
               .Replace("FromValue", FromValue)
               .Replace("ToValue", ToValue);

            return content;
        }
        private void InitialReceives()
        {
            Receive<DownloadFinished>(par =>
            {
                try
                {
                    HtmlCreated res = new HtmlCreated { Content = CreatePage(par) };
                    Sender.Tell(res);
                }
                catch (Exception ex)
                {
                    Log.Error("HtmlCreator:DownloadFinished " + ex.Message);
                }
            });
        }
    }
}