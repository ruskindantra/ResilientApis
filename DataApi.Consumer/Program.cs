using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DataApi.Consumer
{
    partial class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true);
                    config.AddEnvironmentVariables();

                    if (args != null)
                    {
                        config.AddCommandLine(args);
                    }

                    Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(config.Build())
                        .CreateLogger();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();
                    services.AddSingleton<EndpointTester>();
                    services.AddHttpClient();
                    services.AddSingleton<IHostedService, App>();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    // serilog logging
                    logging.AddSerilog(dispose: true);
                });

            await builder.RunConsoleAsync();
        }
    }
}
