using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Akka.Actor;

namespace ne_webapi_akka.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NeController : ControllerBase
    {
        // private readonly ILogger<WeatherForecastController> _logger;

        // public WeatherForecastController(ILogger<WeatherForecastController> logger)
        // {
        //     _logger = logger;
        // }
        [HttpGet]
        public IActionResult GetPage(DateTime? FromDate, DateTime? ToDate, string FundGroupName)
        {
            Task<string> _content = AS._businesLogicRef.Ask<string>(
                new StartDownload { From = FromDate, To = ToDate, FundGroupName = FundGroupName });
            _content.Wait();

            // BusinesLogic.Log.Logging(BusinesLogic.OP, string.Format("FromDate: {0}, ToDate: {1}, FundGroupName: {2}", FromDate, ToDate, FundGroupName));
            // BusinesLogic.Log.Logging(BusinesLogic.OP, TemplateFileName[TemplateIndex]);
            // if (!System.IO.File.Exists(TemplateFileName[TemplateIndex]))
            //     return NotFound();
            // string Template = System.IO.File.ReadAllText(TemplateFileName[TemplateIndex]);
            // // string FundsList="data.addColumn('number', 'Dogs'); data.addColumn('number', 'Cats');";
            // // string ValuesList="data.addRows([ [0, 0, 10], [1, 10, 12], [2, 23, 8], [3, 17, 13], [4, 18, 7], [5, 9, 14] ]);";
            // string FundsList, ValuesList, GroupsList, FromValue, ToValue;
            // BusinesLogic.GetDataForPage(TemplateIndex, FromDate, ToDate, FundGroupName,
            //     out FundsList, out ValuesList, out GroupsList, out FromValue, out ToValue);

            // string content = Template
            //     .Replace("<FundsList/>", FundsList)
            //     .Replace("<ValuesList/>", ValuesList)
            //     .Replace("//##ValuesList##", ValuesList)
            //     .Replace("//##Options##", FundsList)
            //     .Replace("//##GroupsList##", GroupsList)
            //     .Replace("FromValue", FromValue)
            //     .Replace("ToValue", ToValue);
            // System.IO.File.WriteAllText(@"./temp/out.htm", content);

            return new ContentResult()
            {
                Content = _content.Result,
                ContentType = "text/html",
            };
        }

    }
}
