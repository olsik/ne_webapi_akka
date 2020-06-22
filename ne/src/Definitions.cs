using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ne
{
    public class FundRequest
    {
        public int FundId;
        public DateTime From;
        public DateTime To;
        public Fund Tag;
        public override string ToString()
        {
            return FundId.ToString() + "-" + From.ToString("yyyy/MM/dd") + "-" + To.ToString("yyyy/MM/dd");
        }
    }
    public class FundResponce
    {
        public int FundId;
        public string WebResponce;
        public List<DayValue> DayValues;
        public Fund Tag;
        public DayValue this[int i] { get { return i >= 0 && i < DayValues.Count ? DayValues[i] : null; } }
        float MinValue = 0;
        public int Count = 0;
        public void DayValuesReorganize()
        {
            if (DayValues != null && DayValues.Count > 0)
            {
                DayValues = DayValues.OrderBy(x => x.Date).ToList();
                MinValue = DayValues[0].Value;
                Count = DayValues.Count;
                DayValues.Select(x => x.Percent = -100.0F + 100.0F * x.Value / MinValue).Count();
            }
            else
                MinValue = 0;
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
                res += FundId.ToString() + "," + dv.Date.ToString("yyyy/MM/dd",CultureInfo.InvariantCulture)
                + "," + dv.Value.ToString(CultureInfo.InvariantCulture) 
                + "," + dv.Percent.ToString(CultureInfo.InvariantCulture)
                + Environment.NewLine;
            return res;
        }
    }
    public class DayValue
    {
        public int Index;
        public DateTime Date;
        public float Value;
        public float Percent;
    }
    public class FundGroup
    {
        public string Name;
        public List<Fund> Funds = new List<Fund>();
    }
    public class Fund
    {
        //id,группа,дата покупки, кол-во покупки, сумма покупки,
        const string NeDateFormat = @"yyyy-MM-dd";
        public static string CommonGroupName = "Common";
        public int Id;
        public string GroupName;
        public DateTime? BuyDate;
        public int? BuyCount;
        public float? BuyCash;
        public DateTime? SaleDate;
        public bool IdBuyed { get { return BuyCash.HasValue; } }

        public Fund Clone(string groupName)
        {
            return new Fund
            {
                Id = this.Id,
                GroupName = groupName ?? this.GroupName,
                BuyDate = this.BuyDate,
                BuyCount = this.BuyCount,
                BuyCash = this.BuyCash,
                SaleDate=this.SaleDate,
            };
        }
        public static Fund CreateFromLine(string line)
        {
            Fund res; int temp_i; DateTime temp_d; float temp_f;
            char[] splitters = new char[] { ',' };
            string[] parts = line.Split(splitters, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 0 && int.TryParse(parts[0], out temp_i))
                res = new Fund { Id = temp_i };
            else
                return null;

            if (parts.Length > 1 && !parts[1].StartsWith(CommonGroupName))
                res.GroupName = parts[1];
            else
                res.GroupName = CommonGroupName;

            if (parts.Length > 2 && DateTime.TryParseExact(parts[2], NeDateFormat, null, DateTimeStyles.None, out temp_d))
                res.BuyDate = temp_d;

            if (parts.Length > 3 && int.TryParse(parts[3], out temp_i))
                res.BuyCount = temp_i;

            if (parts.Length > 4 && float.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out temp_f))
                res.BuyCash = temp_f;

            if (parts.Length > 5 && DateTime.TryParseExact(parts[5], NeDateFormat, null, DateTimeStyles.None, out temp_d))
                res.SaleDate = temp_d;

            return res;
        }
        public override string ToString()
        {
            string res = Id.ToString() + "," + GroupName;
            if (IdBuyed)
                res += "," + BuyDate?.ToString(NeDateFormat) + "," + BuyCount?.ToString() + "," + BuyCash?.ToString();
            return res;
        }
    }
    public class FundGroups
    {
        // string Header;
        // public List<FundGroup> Groups = new List<FundGroup>();
        public static List<FundGroup> LoadFundGroups(out string ErrMes)
        {
            ErrMes = null;
            List<FundGroup> res = null;
            if (!File.Exists(BusinesLogic.NeFileName))
            {
                ErrMes = "File '" + BusinesLogic.NeFileName + "' not found";
                return res;
            }

            string[] lines = File.ReadAllLines(BusinesLogic.NeFileName);
            char[] splitters = new char[] { ',' };
            if (lines.Length == 0)
                return res;

            res = new List<FundGroup>();
            // res.Header = lines[0];
            for (int i = 1; i < lines.Length; i++)
            {
                Fund curFund = Fund.CreateFromLine(lines[i]);
                if (curFund != null && curFund.Id != 5127790)
                {
                    FundGroup curGroup = res.FirstOrDefault(x => x.Name == curFund.GroupName);
                    if (curGroup == null)
                        res.Add((curGroup = new FundGroup() { Name = curFund.GroupName }));
                    curGroup.Funds.Add(curFund);
                }
            }
            FundGroup CommonGroup = res.FirstOrDefault(x => x.Name == Fund.CommonGroupName);
            if (CommonGroup != null)
            {
                //split the "Common" group
                int CommonGroupSize = 8;
                
                //!!! CommonGroup.Funds = CommonGroup.Funds.OrderBy(x => x.Id).ToList();

                // int CurSubGroupIndex = 0;
                // while(CommonGroup.Funds.Count>0)
                // {
                //     FundGroup curGroup = new FundGroup() { Name = Fund.CommonGroupName +"_"+CurSubGroupIndex.ToString()};
                //     curGroup.Funds = CommonGroup.Funds.Take(CommonGroupSize).ToList();
                //     res.Add(curGroup);
                //     foreach (Fund f in curGroup.Funds)
                //     {
                //         CommonGroup.Funds.Remove(f);
                //         f.GroupName = curGroup.Name;
                //     }
                //     CurSubGroupIndex++;
                // }
                for (int CurSubGroupIndex = 0;
                    CurSubGroupIndex <= CommonGroup.Funds.Count / CommonGroupSize; CurSubGroupIndex++)
                {
                    FundGroup curGroup = new FundGroup()
                    {
                        Name = Fund.CommonGroupName + "_" + CurSubGroupIndex.ToString(),
                        Funds = new List<Fund>(),
                    };
                    res.Add(curGroup);
                    for (int i = CurSubGroupIndex * CommonGroupSize;
                      i < CommonGroup.Funds.Count && i < (CurSubGroupIndex + 1) * CommonGroupSize; i++)
                        curGroup.Funds.Add(CommonGroup.Funds[i].Clone(curGroup.Name));
                }
            }
            //res.SaveToFile();
            return res;
        }

        // public bool UpdateFund(Fund f, string NewGroupName)
        // {
        //     try
        //     {
        //         FundGroup OldGroup = Groups.FirstOrDefault(x => x.Name == f.GroupName);
        //         FundGroup NewGroup = Groups.FirstOrDefault(x => x.Name == NewGroupName);

        //         if (NewGroup == null)
        //             Groups.Add((NewGroup = new FundGroup { Name = NewGroupName }));

        //         if (OldGroup == null || OldGroup.Name == NewGroup.Name)
        //             return false;

        //         f.GroupName = NewGroupName;
        //         OldGroup.Funds.Remove(f);
        //         NewGroup.Funds.Add(f);
        //         return true;
        //     }
        //     catch (Exception ex)
        //     {
        //         return false;
        //     }
        // }
        // public bool SaveToFile()
        // {
        //     try
        //     {
        //         List<string> data = new List<string>();
        //         data.Add(Header);
        //         foreach (FundGroup g in Groups)
        //             foreach (Fund f in g.Funds)
        //                 data.Add(f.ToString());
        //         string t = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        //         if (File.Exists(NeFileName))
        //             File.Move(NeFileName, NeFileName + "." + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
        //         File.WriteAllLines(NeFileName, data);
        //         return true;
        //     }
        //     catch (Exception ex)
        //     {
        //         return false;
        //     }
        // }
    }
    // public class DayValue2 : DayValue
    // {
    // }
    public class AverageSerieData
    {
        // public List<DayValue2> dayValues;
        public List<DayValue> dayValues;
        public int SeriesCount = 0;
        public AverageSerieData(DateTime From, DateTime To)
        {
            // dayValues = new List<DayValue2>();
            dayValues = new List<DayValue>();
            int CurIndex = 0;
            for (DateTime d = From; d <= To; d = d.AddDays(1))
                // dayValues.Add(new DayValue2 { Date = d, Percent = 0, Value = 0, Index = CurIndex++ });
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