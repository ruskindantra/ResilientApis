﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
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
                    RegisterResilientConsumers(services);
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    // serilog logging
                    logging.AddSerilog(dispose: true);
                });

            await builder.RunConsoleAsync();
        }

        private static void RegisterResilientConsumers(IServiceCollection services)
        {
            const string baseUrl = "http://localhost:5000";
            // Transient errors are: HttpRequestException, 5XX and 408

            var backOffs = new[] { TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5) };

            Action<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context> onRetry = (result, timeSpan, retryCount, context) =>
            {
                Log.Warning($"Request failed with <{result.Result?.StatusCode}>. Waiting <{timeSpan}> before next retry. Retry attempt <{retryCount}>");
            };

            services.AddHttpClient("5000_exponentialbackoff").ConfigureHttpClient(httpClient => {
                httpClient.BaseAddress = new Uri(baseUrl);
            })
            .AddTransientHttpErrorPolicy(builder =>
            {
                //builder.OrResult(msg => msg.StatusCode == HttpStatusCode.Continue);
                return builder.WaitAndRetryAsync(backOffs, onRetry);
            });

            services.AddHttpClient("5000_circuitbreaker").ConfigureHttpClient(httpClient => {
                httpClient.BaseAddress = new Uri(baseUrl);
            })
            .AddTransientHttpErrorPolicy(builder =>
            {
                //builder.OrResult(msg => msg.StatusCode == HttpStatusCode.Continue);
                return builder.CircuitBreakerAsync(2, TimeSpan.FromSeconds(30), (response, timeSpan) => {
                    Log.Information($"Circuit is broken and will remain broken for <{timeSpan}>");
                }, () => {
                    Log.Information("Circuit has been reset");
                });
            });

            services.AddHttpClient("5000_jitter").ConfigureHttpClient(httpClient => {
                httpClient.BaseAddress = new Uri(baseUrl);
            })
            .AddTransientHttpErrorPolicy(builder =>
            {
                Random jitterer = new Random(); 
                return builder.WaitAndRetryAsync(5,  
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))  // exponential back-off: 2, 4, 8 etc
                        + TimeSpan.FromMilliseconds(jitterer.Next(0, 1000)), // plus some jitter: up to 1 second
                onRetry);
            });

            services.AddHttpClient("5000_bulkhead").ConfigureHttpClient(httpClient => {
                httpClient.BaseAddress = new Uri(baseUrl);
            });
        }
    }
}
