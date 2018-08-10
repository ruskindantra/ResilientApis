using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataApi.Consumer
{
    internal class App : IHostedService, IDisposable
    {
        private readonly ILogger<App> _logger;
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly EndpointTester _endpointTester;

        public App(ILogger<App> logger, IApplicationLifetime applicationLifetime, EndpointTester endpointTester)
        {
            _logger = logger;
            _applicationLifetime = applicationLifetime;
            _endpointTester = endpointTester;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting.");

            _applicationLifetime.ApplicationStarted.Register(ApplicationStarted);

            return Task.CompletedTask;
        }

        private void ApplicationStarted()
        {
            Task.Factory.StartNew(() =>
            {
                PrintOptions();

                bool carryOn = true;
                do
                {
                    Console.Write("...");
                    string option = Console.ReadLine();
                    if (string.Compare(option, "Q", StringComparison.CurrentCultureIgnoreCase) == 0)
                        carryOn = false;
                    else if (string.Compare(option, "O", StringComparison.CurrentCultureIgnoreCase) == 0)
                        PrintOptions();
                    else if (string.Compare(option, "1", StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        
                    }
                    else
                    {
                        Console.WriteLine($"Option selected {option}");
                    }
                } while (carryOn);

                _applicationLifetime.StopApplication();
            });
        }

        private void PrintOptions()
        {
            Console.WriteLine("Press 1 for exponentialbackoff");
            Console.WriteLine("Press 2 for shortcircuit");
            Console.WriteLine("Press O for reprinting options");
            Console.WriteLine("Press Q to quit");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping.");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing.");
        }
    }
}