using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ne_webapi_akka
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger  = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("./logs/myapp.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            Log.Information("-------------");
            Log.Information("ne_webapi_akka STARTED");

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
