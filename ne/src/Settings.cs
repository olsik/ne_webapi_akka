using System.Collections.Generic;

namespace ne
{
    public interface ISettings
    {
        List<FundGroup> GetFundGroups(out string ErrMes);
    }

    public class Settings:ISettings
    {
        public List<FundGroup> GetFundGroups(out string ErrMes)
        {
            return FundGroups.LoadFundGroups(out ErrMes);
        }
    }
}
