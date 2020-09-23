using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ne_webapi_akka
{
    public class Fund
    {
        //id,группа,дата покупки, кол-во покупки, сумма покупки,
        public const string NeDateFormat = @"yyyy-MM-dd";
        public static string CommonGroupName = "Common";
        public static string AllGroupName ="ALL";
        public int Id;
        public string GroupName;
        public DateTime? BuyDate;
        public int? BuyCount;
        public float? BuyCash;

        public bool IdBuyed { get { return BuyCash.HasValue; } }

        // public Fund Clone(string groupName)
        // {
        //     return new Fund
        //     {
        //         Id = this.Id,
        //         GroupName = groupName ?? this.GroupName,
        //         BuyDate = this.BuyDate,
        //         BuyCount = this.BuyCount,
        //         BuyCash = this.BuyCash,

        //     };
        // }
        public Fund Clone(string groupName)
        {
            Fund res = null;
            return Clone(ref res, groupName);
        }
        public Fund Clone(ref Fund res, string groupName)
        {
            if (res == null)
                res = new Fund();

            res.Id = this.Id;
            res.GroupName = groupName ?? this.GroupName;
            res.BuyDate = this.BuyDate;
            res.BuyCount = this.BuyCount;
            res.BuyCash = this.BuyCash;

            return res;
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
    public class FundGroup
    {
        public string Name;
        public List<Fund> Funds = new List<Fund>();
    }
    public class FundGroups
    {
        private static List<FundGroup> _Groups;
        public static List<FundGroup> Groups
        {
            get
            {
                if (_Groups == null)
                {
                    string ErrMes;
                    ISettings settings = new Settings();
                    _Groups = settings.GetFundGroups(out ErrMes);
                    // if (!string.IsNullOrEmpty(ErrMes))
                    //     Log.Logging(OP, ErrMes, null, LogLevel.Hi);
                }

                return _Groups;
            }
        }

        public static List<string> GroupNames
        {
            get { return Groups?.Select(x => x.Name).ToList(); }
        }
        public static List<FundGroup> LoadFundGroups(string NeFileName, out string ErrMes)
        {
            ErrMes = null;
            List<FundGroup> res = null;
            if (!File.Exists(NeFileName))
            {
                ErrMes = "File '" + NeFileName + "' not found";
                return res;
            }

            string[] lines = File.ReadAllLines(NeFileName);
            char[] splitters = new char[] { ',' };
            if (lines.Length == 0)
                return res;

            res = new List<FundGroup>();
            FundGroup allGroup = new FundGroup() { Name = Fund.AllGroupName};
            res.Add(allGroup);
            for (int i = 1; i < lines.Length; i++)
            {
                Fund curFund = Fund.CreateFromLine(lines[i]);
                if (curFund != null)
                {
                    if (allGroup.Funds.FirstOrDefault(x => x.Id == curFund.Id) != null)
                        continue;
                    FundGroup curGroup = res.FirstOrDefault(x => x.Name == curFund.GroupName);
                    if (curGroup == null)
                        res.Add((curGroup = new FundGroup() { Name = curFund.GroupName }));
                    curGroup.Funds.Add(curFund);
                    allGroup.Funds.Add(curFund);
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
}