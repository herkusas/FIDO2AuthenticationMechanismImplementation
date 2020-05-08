using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace FidoFront
{
    public class Program
    {
        public static void Main() => CreateHostBuilder().Build().Run();

        private static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
