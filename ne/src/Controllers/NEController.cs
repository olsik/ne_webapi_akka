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
        const int TemplateIndex = 1;
        static string[] TemplateFileName = new string[] { @"./test.html", @"./webform.htm" };
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

        [HttpGet]
        public IActionResult GetPage(DateTime? FromDate, DateTime? ToDate, string FundGroupName)
        {
            BusinesLogic.Log.Logging(BusinesLogic.OP, string.Format("FromDate: {0}, ToDate: {1}, FundGroupName: {2}", FromDate, ToDate, FundGroupName));

            if (!System.IO.File.Exists(TemplateFileName[TemplateIndex]))
                return NotFound();
            string Template = System.IO.File.ReadAllText(TemplateFileName[TemplateIndex]);
            // string FundsList="data.addColumn('number', 'Dogs'); data.addColumn('number', 'Cats');";
            // string ValuesList="data.addRows([ [0, 0, 10], [1, 10, 12], [2, 23, 8], [3, 17, 13], [4, 18, 7], [5, 9, 14] ]);";
            string FundsList, ValuesList, GroupsList, FromValue, ToValue;
            BusinesLogic.BL.GetDataForPage(TemplateIndex, FromDate, ToDate, FundGroupName,
                out FundsList, out ValuesList, out GroupsList, out FromValue, out ToValue);

            string content = Template
                .Replace("<FundsList/>", FundsList)
                .Replace("<ValuesList/>", ValuesList)
                .Replace("//##ValuesList##", ValuesList)
                .Replace("//##Options##", FundsList)
                .Replace("//##GroupsList##", GroupsList)
                .Replace("FromValue", FromValue)
                .Replace("ToValue", ToValue);
            System.IO.File.WriteAllText(@"./temp/out.htm", content);

            return new ContentResult()
            {
                Content = content,
                ContentType = "text/html",
            };
        }
    }
}