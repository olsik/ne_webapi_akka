using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.Logging;
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

            return new ContentResult()
            {
                Content = _content.Result,
                ContentType = "text/html",
            };
        }

    }
}
