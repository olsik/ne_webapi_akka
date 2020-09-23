using System.Collections.Generic;

namespace ne_webapi_akka
{
    public interface ISettings
    {
        List<FundGroup> GetFundGroups(out string ErrMes);
    }

    public class Settings : ISettings
    {
        public static string LogFolder = @"./temp";
        public const string NeFileName = @"./ne.dat";
        public const string WebformFileName = @"./webform.htm";
        public List<FundGroup> GetFundGroups(out string ErrMes)
        {
            return FundGroups.LoadFundGroups(NeFileName, out ErrMes);
        }
    }
}




