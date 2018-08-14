using System.Net.Http;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;

namespace DataApi.Consumer
{
    public class PolicyRegistryExecutor
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IPolicyRegistry<string> _policyRegistry;
        private readonly ILogger<EndpointTester> _logger;

        public PolicyRegistryExecutor(IHttpClientFactory httpClientFactory, IPolicyRegistry<string> policyRegistry, ILogger<EndpointTester> logger)
        {
            _httpClientFactory = httpClientFactory;
            _policyRegistry = policyRegistry;
            _logger = logger;
        }

        public void ExecuteGetCall(string name, string endpoint, string policyName)
        {
            var policy = _policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>(policyName);
            
            policy.ExecuteAsync(async context => 
            {
                HttpResponseMessage result = null;
                try
                {
                    using (var httpClient = _httpClientFactory.CreateClient(name))
                    {
                        result = await httpClient.GetAsync(endpoint);

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

                return result;
            }, new Context("myCachedValue"));

            //await Task.CompletedTask;
        }
    }
}