using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Akka.Actor;

namespace ne_webapi_akka.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetPage()
        {
            Task<string> _content = AS._businesLogicRef.Ask<string>(
                new StartResearch_Step1());
            _content.Wait();

            return new ContentResult()
            {
                Content = _content.Result,
                ContentType = "text/html",
            };
        }

    }
}
