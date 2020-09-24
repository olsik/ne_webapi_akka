using Akka.Actor;
using Serilog;
using System.Collections.Generic;
using System.Linq;

namespace ne_webapi_akka
{
    public class ResearchResult
    {
        public FundResponce fundResponce;
        public float periodChange;
        public float lastChange;
        public int FundId;
        public bool status { get { return periodChange < 0 && lastChange > 0; } }
    }
    public class Research : ReceiveActor
    {
        public static int CheckPeriod_Days = 7;
        public static string ResearchResultIdent = "ResearchResult";
        public Research()
        {
            InitialReceives();
        }
        private void InitialReceives()
        {
            Receive<DownloadFinished_Research_Step1>(par =>
            {
                Log.Information("Research started");
                List<ResearchResult> researchResults = new List<ResearchResult>();
                foreach (FundResponce fr in par.Responces)
                {
                    ResearchResult rr = CheckFundResponse(fr);
                    if (rr.status)
                        researchResults.Add(rr);
                }
                FundGroup fg = FundGroups.Groups?.FirstOrDefault(x => x.Name == ResearchResultIdent);
                if (fg != null)
                    FundGroups.Groups.Remove(fg);
                fg = new FundGroup
                {
                    Name = ResearchResultIdent,
                    Funds = researchResults.OrderBy(x => x.periodChange).Take(20)
                    .Select(x => new Fund
                    {
                        Id = x.FundId,
                        GroupName = ResearchResultIdent,
                    })
                    .ToList()
                };
                FundGroups.Groups.Add(fg);

                Context.Parent.Tell(new StartDownload_Research { FundGroupName = ResearchResultIdent });
            });
        }
        public ResearchResult CheckFundResponse(FundResponce par)
        {
            if (par.DayValues.Count < 2)
                return new ResearchResult
                {
                    periodChange = +888,
                };
            int index = 0; float[] deltas = new float[CheckPeriod_Days];
            for (int i = par.DayValues.Count - 1; i > 0 && index < CheckPeriod_Days; i--, index++)
                deltas[index] = AverValue(par.DayValues, i) - AverValue(par.DayValues, i - 1);
            float periodChange = deltas.Sum();
            return new ResearchResult
            {
                fundResponce = par,
                FundId = par.FundId,
                periodChange = deltas.Sum(),
                // lastChange = deltas[0],
                lastChange = par.DayValues[par.DayValues.Count - 1].Percent - par.DayValues[par.DayValues.Count - 2].Percent,
            };
        }

        private float AverValue(List<DayValue> DayValues, int ind)
        {
            return DayValues[ind].Percent;

            // if (ind == 0)
            //     return (DayValues[ind].Percent + DayValues[ind + 1].Percent) / 2.0F;
            // else if (ind == DayValues.Count - 1)
            //     return (DayValues[ind - 1].Percent + DayValues[ind].Percent) / 2.0F;
            // else
            //     return (DayValues[ind - 1].Percent + DayValues[ind].Percent + DayValues[ind + 1].Percent) / 3.0F;
        }
    }
}