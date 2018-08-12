using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;

namespace DataApi.Consumer
{
    public class EndpointTester
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IPolicyRegistry<string> _policyRegistr;
        private readonly ILogger<EndpointTester> _logger;

        public EndpointTester(IHttpClientFactory httpClientFactory, IPolicyRegistry<string> policyRegistr, ILogger<EndpointTester> logger)
        {
            _httpClientFactory = httpClientFactory;
            _policyRegistr = policyRegistr;
            _logger = logger;
        }

        public async void ExecuteGetCall(string name, string endpoint)
        {
            //Thread.Sleep(5000);
            try
            {
                using (var httpClient = _httpClientFactory.CreateClient(name))
                {
                    var result = await httpClient.GetAsync(endpoint);

                    if (result == null)
                    {
                        _logger.LogWarning("Result was null");
                    }
                    else if (result.Content == null)
                    {
                        _logger.LogWarning("Result.Content was null");
                    }
                    else {
                        var content = await result.Content.ReadAsStringAsync();
                        _logger.LogInformation($"Result is <{content}>");
                    }
                }
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError($"HttpRequestException occurred <{hre.Message}>");
            }
            catch (Polly.CircuitBreaker.BrokenCircuitException bce)
            {
                _logger.LogError($"BrokenCircuitException occurred <{bce.Message}>");
            }
        }
    }
}