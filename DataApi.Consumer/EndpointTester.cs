using System.Net.Http;

namespace DataApi.Consumer
{
    public class EndpointTester
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public EndpointTester(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
    }
}