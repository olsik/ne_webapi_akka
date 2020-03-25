using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Akka.Actor;

namespace ne
{
    public class Program
    {
        // public static ActorSystem AS;
        // static IActorRef DM;
        public static void Main(string[] args)
        {
            // DM.Tell(new DownloadManager.Start());
            BusinesLogic.BL=new BusinesLogic();

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
