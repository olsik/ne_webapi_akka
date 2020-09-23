using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ne_webapi_akka
{
    public static class Tase
    {
        public const string RequestFormat
            = @"https://info.tase.co.il/_layouts/tase/Handlers/ChartsDataHandler.ashx?fn=advchartdata&ct=3&ot=4&lang=0&cf=0&cp=8&cv=0&cl=0&cgt=1&dFrom={1}&dTo={2}&oid={0}&_=0";
        public const string FundIdFormat = "D8";
        public const string RequestDateFormat = @"dd-MM-yyyy";
        public const string ResponceDateFormat = @"dd/MM/yyyy";
        public const string UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.2.15) Gecko/20110303 Firefox/3.6.15";
    }
    public enum Status
    {
        Ready = 0,
        Successfuly = 1,
        Failed = 2,
        InProcess = 3,
    }
    public class FundRequest
    {
        public int FundId;
        public DateTime From;
        public DateTime To;
        public Fund Tag;
        public int retry;
        public Status status;
        public override string ToString()
        {
            return FundId.ToString() + "-" + From.ToString("yyyy/MM/dd") + "-" + To.ToString("yyyy/MM/dd");
        }
    }
    public class FundResponce : FundRequest
    {
        // public int FundId;
        public string WebResponce;
        public List<DayValue> DayValues;
        // public Fund Tag;
        public DayValue this[int i] { get { return i >= 0 && i < DayValues.Count ? DayValues[i] : null; } }
        float MinValue = 0;
        // public int Count = 0;
        public bool DotStyle = false;
        public string Color;
        public int FillDayValues_0()
        {
            List<string> parts = ParseWebResponce(0);
            foreach (string part in parts)
            {
                string[] Values = part.Split(new char[] { ',', ' ', '"' }, StringSplitOptions.RemoveEmptyEntries);
                DateTime Date;
                float Value;

                if (!float.TryParse(Values[0], NumberStyles.Any, CultureInfo.InvariantCulture, out Value)
                || !DateTime.TryParseExact(Values[1], Tase.ResponceDateFormat, null, DateTimeStyles.None, out Date))
                    continue;
                DayValue dv = new DayValue { Date = Date, Value = Value };

                DayValues.Add(dv);
            }

            DayValues = DayValues.OrderBy(x => x.Date).ToList();
            for (int i = DayValues.Count - 1; i > 0; i--)
            {
                int DaysGap = (int)(DayValues[i].Date - DayValues[i - 1].Date).TotalDays;
                float ValueDelta = (DayValues[i].Value - DayValues[i - 1].Value) / DaysGap;
                for (int d = 1; d < DaysGap; d++)
                    DayValues.Add(new DayValue
                    {
                        Date = DayValues[i - 1].Date.AddDays(d),
                        Value = DayValues[i - 1].Value + d * ValueDelta,
                    });
            }
            // DayValues = DayValues.OrderBy(x => x.Date).ToList();
            // for (int i = 0; i < DayValues.Count; i++)
            //     DayValues[i].Index = i;

            //remove before bay date???
            // if (responce.Tag != null && responce.Tag.IdBuyed)
            //     for (int i = responce.DayValues.Count - 1; i >= 0; i--)
            //         if (responce.DayValues[i].Date < responce.Tag.BuyDate)
            //             responce.DayValues.RemoveAt(i);

            DayValuesReorganize();
            return 0;
        }
        private List<string> ParseWebResponce(int startIndex)
        {
            List<string> res = new List<string>();
            char s = '['; char e = ']'; string cur_res; int s_ind; int e_ind; int s2_ind; bool isEnd = false;

            while (!isEnd)
            {
                s_ind = WebResponce.IndexOf(s, startIndex);
                e_ind = WebResponce.IndexOf(e, startIndex);
                s2_ind = WebResponce.IndexOf(s, startIndex + 1);
                if (s2_ind != -1 && s2_ind < e_ind)
                    startIndex++;
                else if (e_ind != -1 && s_ind != -1)
                {
                    startIndex = e_ind + 2;
                    cur_res = WebResponce.Substring(s_ind + 1, e_ind - 1 - s_ind);
                    res.Add(cur_res);
                }
                else if (s_ind == -1)
                    isEnd = true;
            }
            return res;
        }

        public int DayValuesReorganize()
        {
            MinValue = 0;
            if (DayValues != null && DayValues.Count > 0)
            {
                DayValues = DayValues.OrderBy(x => x.Date).ToList();
                for (int i = 0; i < DayValues.Count; i++)
                    DayValues[i].Index = i;

                DayValue dvWithMinValue = DayValues[0];
                if (this.Tag.BuyDate.HasValue && this.Tag.BuyDate.Value > From && this.Tag.BuyDate.Value < To)
                    dvWithMinValue = DayValues.FirstOrDefault(x => x.Date == this.Tag.BuyDate.Value);

                MinValue = dvWithMinValue.Value;
                // Count = DayValues.Count;

                // for (int i = 0; i < DayValues.Count; i++)
                // DayValues[i].Percent=-100.0F + 100.0F * x.Value / MinValue;
                DayValues.Select(x => x.Percent = -100.0F + 100.0F * x.Value / MinValue).Count();
                return dvWithMinValue.Index;
            }
            else
                return 0;
        }
        public FundResponce CreateBeforeBuyResonce(int BuyDateIndex)
        {
            FundResponce res = new FundResponce
            {
                FundId = this.FundId,
                DotStyle = true,
                Color = this.Color,
                DayValues = new List<DayValue>(),
            };

            for (int i = 0; i < BuyDateIndex; i++)
            {
                res.DayValues.Add(this.DayValues[0]);
                this.DayValues.RemoveAt(0);
            }
            res.DayValues.Add(this.DayValues[0]);
            return res;
        }
        public void SaveToFile()
        {
            string fn = "./Data/" + FundId.ToString() + "_" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".txt";
            Directory.CreateDirectory(Path.GetDirectoryName(fn));
            File.WriteAllText(fn, WebResponce);
        }
        public override string ToString()
        {
            string res = "";
            foreach (DayValue dv in DayValues)
                res += FundId.ToString() + "," + dv.Date.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)
                + "," + dv.Value.ToString(CultureInfo.InvariantCulture)
                + "," + dv.Percent.ToString(CultureInfo.InvariantCulture)
                + Environment.NewLine;
            return res;
        }
        public int FillDayValues()
        {
            List<string> parts = ParseWebResponce(0);
            foreach (string part in parts)
            {
                string[] Values = part.Split(new char[] { ',', ' ', '"' }, StringSplitOptions.RemoveEmptyEntries);
                DateTime Date;
                float Value;

                if (!float.TryParse(Values[0], NumberStyles.Any, CultureInfo.InvariantCulture, out Value)
                || !DateTime.TryParseExact(Values[1], Tase.ResponceDateFormat, null, DateTimeStyles.None, out Date))
                    continue;
                DayValue dv = new DayValue { Date = Date, Value = Value };

                DayValues.Add(dv);
            }

            DayValues = DayValues.OrderBy(x => x.Date).ToList();
            for (int i = DayValues.Count - 1; i > 0; i--)
            {
                int DaysGap = (int)(DayValues[i].Date - DayValues[i - 1].Date).TotalDays;
                float ValueDelta = (DayValues[i].Value - DayValues[i - 1].Value) / DaysGap;
                for (int d = 1; d < DaysGap; d++)
                    DayValues.Add(new DayValue
                    {
                        Date = DayValues[i - 1].Date.AddDays(d),
                        Value = DayValues[i - 1].Value + d * ValueDelta,
                    });
            }
            // DayValues = DayValues.OrderBy(x => x.Date).ToList();
            // for (int i = 0; i < DayValues.Count; i++)
            //     DayValues[i].Index = i;

            //remove before bay date???
            // if (responce.Tag != null && responce.Tag.IdBuyed)
            //     for (int i = responce.DayValues.Count - 1; i >= 0; i--)
            //         if (responce.DayValues[i].Date < responce.Tag.BuyDate)
            //             responce.DayValues.RemoveAt(i);

            int BuyDateIndex = DayValuesReorganize();
            return BuyDateIndex;
        }
    }
    public class DayValue
    {
        public int Index;
        public DateTime Date;
        public float Value;
        public float Percent;
    }
    public class AverageSerieData
    {
        public List<DayValue> dayValues;
        public int SeriesCount = 0;
        public AverageSerieData(DateTime From, DateTime To)
        {
            dayValues = new List<DayValue>();
            int CurIndex = 0;
            for (DateTime d = From; d <= To; d = d.AddDays(1))
                dayValues.Add(new DayValue { Date = d, Percent = 0, Value = 0, Index = CurIndex++ });
        }
        public void AddSerie(List<DayValue> SerieValues)
        {
            SeriesCount++;
            var q_List = (from v in dayValues
                          join s in SerieValues on v.Date equals s.Date into v_l_s
                          from s2 in v_l_s.DefaultIfEmpty()
                          select new { v, s2 }).ToList();
            for (int i = 0; i < q_List.Count; i++)
            {
                float Percent;
                if (q_List[i].s2 == null)
                {
                    int beforeInd, afterInd;
                    for (beforeInd = i - 1; beforeInd >= 0; beforeInd--)
                        if (q_List[beforeInd].s2 != null)
                            break;
                    for (afterInd = i + 1; afterInd < q_List.Count; afterInd++)
                        if (q_List[afterInd].s2 != null)
                            break;
                    if (beforeInd < 0 && afterInd >= q_List.Count)
                    {
                        ///Logging2???
                        return;
                    }
                    else if (beforeInd < 0)
                        Percent = q_List[afterInd].s2.Percent;
                    else if (afterInd >= q_List.Count)
                        Percent = q_List[beforeInd].s2.Percent;
                    else
                    {
                        Percent = (q_List[afterInd].s2.Percent - q_List[beforeInd].s2.Percent) / (afterInd - beforeInd);//delta
                        Percent = q_List[beforeInd].s2.Percent + Percent * (i - beforeInd);
                    }
                }
                else
                    Percent = q_List[i].s2.Percent;

                Percent = q_List[i].v.Value * q_List[i].v.Percent + Percent;//SumPercent 
                q_List[i].v.Value = q_List[i].v.Value + 1;
                q_List[i].v.Percent = Percent / q_List[i].v.Value;
            }
        }
    }
}