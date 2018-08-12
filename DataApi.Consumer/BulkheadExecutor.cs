using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;

namespace DataApi.Consumer
{
    public class BulkheadExecutor
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IPolicyRegistry<string> _policyRegistr;
        private readonly ILogger<EndpointTester> _logger;

        int goodRequestsMade = 0;
        int goodRequestsSucceeded = 0;
        int goodRequestsFailed = 0;
        int faultingRequestsMade = 0;
        int faultingRequestsSucceeded = 0;
        int faultingRequestsFailed = 0;

        public BulkheadExecutor(IHttpClientFactory httpClientFactory, IPolicyRegistry<string> policyRegistr, ILogger<EndpointTester> logger)
        {
            _httpClientFactory = httpClientFactory;
            _policyRegistr = policyRegistr;
            _logger = logger;
        }

        
        public async void ExectueBulkheadCalls(CancellationToken externalCancellationToken, string name, string goodEndpoint, string faultingEndpoint, bool useBulkhead = true)
        {
            // code extracted from https://github.com/App-vNext/Polly-Samples/blob/master/PollyTestClient/Samples/BulkheadAsyncDemo01_WithBulkheads.cs
            
            // Let's imagine this caller has some theoretically limited capacity.
            const int callerParallelCapacity = 8; // (artificially low - but easier to follow, to illustrate principle)
            var limitedCapacityCaller = new LimitedConcurrencyLevelTaskScheduler(callerParallelCapacity);

            Policy bulkheadForGoodCalls = Policy.BulkheadAsync(callerParallelCapacity/2, int.MaxValue);
            Policy bulkheadForFaultingCalls = Policy.BulkheadAsync(callerParallelCapacity - callerParallelCapacity/2, int.MaxValue); // In this demo we let any number (int.MaxValue) of calls _queue for an execution slot in the bulkhead (simulating a system still _trying to accept/process as many of the calls as possible).  A subsequent demo will look at using no queue (and bulkhead rejections) to simulate automated horizontal scaling.

            if (!useBulkhead) 
            {
                bulkheadForGoodCalls = Policy.NoOpAsync();
                bulkheadForFaultingCalls = Policy.NoOpAsync();
            }

            var client = _httpClientFactory.CreateClient(name);
            var rand = new Random();
            int i = 0;

            IList<Task> tasks = new List<Task>();
            CancellationTokenSource internalCancellationTokenSource = new CancellationTokenSource();
            CancellationToken combinedToken = CancellationTokenSource.CreateLinkedTokenSource(
                externalCancellationToken, internalCancellationTokenSource.Token).Token;

            while (!externalCancellationToken.IsCancellationRequested)
            {
                i++;

                // Randomly make either 'good' or 'faulting' calls.
                if (rand.Next(0, 2) == 0)
                //if (i % 2 == 0)
                {
                    goodRequestsMade++;
                    tasks.Add(Task.Factory.StartNew(j =>

                        // Call 'good' endpoint: through the bulkhead.
                        bulkheadForGoodCalls.ExecuteAsync(async () =>
                        {

                            try
                            {
                                var result = await client.GetAsync(goodEndpoint, combinedToken);
                                // Make a request and get a response, from the good endpoint
                                string msg = result.Content.ReadAsStringAsync().Result;
                                if (!combinedToken.IsCancellationRequested) 
                                {
                                    _logger.LogInformation("Response : " + msg);
                                }

                                goodRequestsSucceeded++;
                            }
                            catch (Exception e)
                            {
                                if (!combinedToken.IsCancellationRequested) 
                                {
                                    _logger.LogWarning("Request " + j + " eventually failed with: " + e.Message);
                                }

                                goodRequestsFailed++;
                            }
                        }), i, combinedToken, TaskCreationOptions.LongRunning, limitedCapacityCaller).Unwrap()
                    );

                }
                else
                {
                    faultingRequestsMade++;
                    
                    tasks.Add(Task.Factory.StartNew(j =>

                        // call 'faulting' endpoint: through the bulkhead.
                        bulkheadForFaultingCalls.ExecuteAsync(async () =>
                        {
                            try
                            {
                                // Make a request and get a response, from the faulting endpoint
                                var result = await client.GetAsync(faultingEndpoint, combinedToken);
                                if (result.IsSuccessStatusCode)
                                {
                                    string msg = result.Content.ReadAsStringAsync().Result;
                                    if (!combinedToken.IsCancellationRequested) 
                                    {
                                        //_logger.LogInformation("Response : " + msg);
                                    }

                                    faultingRequestsSucceeded++;
                                }
                                else
                                {
                                    throw new Exception();
                                }
                            }
                            catch (Exception e)
                            {
                                if (!combinedToken.IsCancellationRequested) 
                                {
                                    _logger.LogWarning("Request " + j + " eventually failed with: " + e.Message);
                                }

                                faultingRequestsFailed++;
                            }
                        }), i, combinedToken, TaskCreationOptions.LongRunning, limitedCapacityCaller).Unwrap()
                    );

                }

                //OutputState();

                try
                {
                    // Wait briefly
                    await Task.Delay(TimeSpan.FromSeconds(0.1), externalCancellationToken);
                }
                catch (Exception e)
                {
                    OutputState();
                    _logger.LogWarning("Task cancelled");
                }
            }  
        }

        public void OutputState()
        {
            _logger.LogInformation(String.Format("Good endpoint: requested {0:00}, ", goodRequestsMade));
            _logger.LogInformation(String.Format("succeeded {0:00}, ", goodRequestsSucceeded));
            _logger.LogInformation(String.Format("pending {0:00}, ", goodRequestsMade - goodRequestsSucceeded - goodRequestsFailed));
            _logger.LogInformation(String.Format("failed {0:00}.", goodRequestsFailed));
            
            _logger.LogInformation(String.Format("Faulting endpoint: requested {0:00}, ", faultingRequestsMade));
            _logger.LogInformation(String.Format("succeeded {0:00}, ", faultingRequestsSucceeded));
            _logger.LogInformation(String.Format("pending {0:00}, ", faultingRequestsMade - faultingRequestsSucceeded - faultingRequestsFailed));
            _logger.LogInformation(String.Format("failed {0:00}.", faultingRequestsFailed));

            _logger.LogInformation("");
        }
    }
}