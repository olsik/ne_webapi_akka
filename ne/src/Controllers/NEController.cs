using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ne.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NEController : ControllerBase
    {
        const string TemplateFileName="./test.html";
        [HttpGet]
        public IActionResult GetPage(DateTime? FromDate, DateTime? ToDate, string FundGroupName)
        {
            BusinesLogic.Log.Logging(BusinesLogic.OP, string.Format("FromDate: {0}",FundGroupName));
            BusinesLogic.Log.Logging(BusinesLogic.OP, string.Format("FromDate: {0}, ToDate: {1}",ToDate,FundGroupName));
            BusinesLogic.Log.Logging(BusinesLogic.OP, string.Format("FromDate: {0}, ToDate: {1}, FundGroupName: {2}",FromDate,ToDate,FundGroupName));
            BusinesLogic.Log.Logging(BusinesLogic.OP, string.Format(@"FromDate: {0}, ToDate: {1}, FundGroupName: {2}",FromDate,ToDate,FundGroupName));

            if(!System.IO.File.Exists(TemplateFileName))
                return NotFound();
            string Template = System.IO.File.ReadAllText(TemplateFileName);
            // string FundsList="data.addColumn('number', 'Dogs'); data.addColumn('number', 'Cats');";
            // string ValuesList="data.addRows([ [0, 0, 10], [1, 10, 12], [2, 23, 8], [3, 17, 13], [4, 18, 7], [5, 9, 14] ]);";
            string FundsList,ValuesList,GroupsList;

            BusinesLogic.BL.GetDataForPage(FromDate, ToDate,FundGroupName,out FundsList,out ValuesList,out GroupsList);

            string content = Template
                .Replace("FundsList",FundsList)
                .Replace("ValuesList",ValuesList)
                .Replace("GroupsList",GroupsList);
            return new ContentResult()
            {
                Content = content,
                ContentType = "text/html",
            };
        }
    }
}