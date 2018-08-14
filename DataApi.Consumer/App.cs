using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;

namespace DataApi.Consumer
{
    internal class App : IHostedService, IDisposable
    {
        private readonly ILogger<App> _logger;
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly EndpointTester _endpointTester;
        private readonly Func<BulkheadExecutor> _bulkheadExecutorFactory;
        private readonly PolicyRegistryExecutor _policyRegistryExecutor;

        public App(ILogger<App> logger, 
            IApplicationLifetime applicationLifetime, 
            EndpointTester endpointTester, Func<BulkheadExecutor> bulkheadExecutorFactory, 
            PolicyRegistryExecutor policyRegistryExecutor)
        {
            _logger = logger;
            _applicationLifetime = applicationLifetime;
            _endpointTester = endpointTester;
            _bulkheadExecutorFactory = bulkheadExecutorFactory;
            _policyRegistryExecutor = policyRegistryExecutor;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting.");

            _applicationLifetime.ApplicationStarted.Register(ApplicationStarted);

            return Task.CompletedTask;
        }

        private void ApplicationStarted()
        {

            CancellationTokenSource cancellationTokenSource5 = new CancellationTokenSource();
            Task.Factory.StartNew(() =>
            {
                PrintOptions();

                bool carryOn = true;
                do
                {
                    _logger.LogInformation("Please select an option...");
                    string option = Console.ReadLine();
                    if (string.Compare(option, "Q", StringComparison.CurrentCultureIgnoreCase) == 0)
                        carryOn = false;
                    else if (string.Compare(option, "O", StringComparison.CurrentCultureIgnoreCase) == 0)
                        PrintOptions();
                    else if (string.Compare(option, "1", StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        _endpointTester.ExecuteGetCall("5000_exponentialbackoff", "/api/resilient/exponentialbackoff");
                    }
                    else if (string.Compare(option, "2", StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        _endpointTester.ExecuteGetCall("5000_circuitbreaker", "/api/resilient/circuitbreaker");
                    }
                    else if (string.Compare(option, "3", StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        _endpointTester.ExecuteGetCall("5000_circuitbreaker", "/api/resilient/circuitbreaker_aux");
                    }
                    else if (string.Compare(option, "4", StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        _endpointTester.ExecuteGetCall("5000_jitter", "/api/resilient/jitter");
                    }
                    else if (string.Compare(option, "5", StringComparison.CurrentCultureIgnoreCase) == 0 ||
                        string.Compare(option, "5a", StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        bool useBulkhead = true;
                        if (string.Compare(option, "5a", StringComparison.CurrentCultureIgnoreCase) == 0)
                        {
                            useBulkhead = false;
                        }
                        _bulkheadExecutorFactory().ExectueBulkheadCalls(cancellationTokenSource5.Token, "5000_bulkhead", "/api/resilient/bulkhead", "/api/resilient/faultingbulkhead", useBulkhead);
                        
                        cancellationTokenSource5.CancelAfter(10000);
                    }
                    else if (string.Compare(option, "6", StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        _endpointTester.ExecuteGetCall("5000_fallback", "/api/resilient/fallback");
                    }
                    else if (string.Compare(option, "7", StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        _policyRegistryExecutor.ExecuteGetCall("5000_cache", "/api/resilient/cache", "cachePolicy");
                    }
                    else if (string.Compare(option, "8", StringComparison.CurrentCultureIgnoreCase) == 0)
                    {
                        _policyRegistryExecutor.ExecuteGetCall("5000_timout", "/api/resilient/timeout", "timeoutPolicy");
                    }
                    else
                    {
                        _logger.LogInformation($"Invalid option selected {option}");
                    }
                    _logger.LogInformation($"Finished option <{option}>");
                } while (carryOn);

                _applicationLifetime.StopApplication();
            });
        }

        private void PrintOptions()
        {
            _logger.LogInformation("Press 1 for exponentialbackoff");
            _logger.LogInformation("Press 2 for circuitbreaker");
            _logger.LogInformation("Press 3 for circuitbreaker_aux");
            _logger.LogInformation("Press 4 for jitter");
            _logger.LogInformation("Press 5 for bulkhead");
            _logger.LogInformation("Press 5a for bulkhead (w/o bulkhead)");
            _logger.LogInformation("Press 6 for fallback");
            _logger.LogInformation("Press 7 for cache");
            _logger.LogInformation("Press 8 for timeout");
            _logger.LogInformation("Press O for reprinting options");
            _logger.LogInformation("Press Q to quit");
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