using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DataApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResilientController : ControllerBase
    {
        private readonly ILogger<ResilientController> _logger;

        public ResilientController(ILogger<ResilientController> logger)
        {
            _logger = logger;
        }

        [HttpGet, Route("exponentialbackoff")]
        public async Task<IActionResult> ExponentialBackoff()
        {
            _logger.LogInformation("ExponentialBackoff called");
            await Task.CompletedTask;
            return Ok("Successful");
        }

        [HttpGet, Route("circuitbreaker")]
        public async Task<IActionResult> CircuitBreaker()
        {
            _logger.LogInformation("CircuitBreaker called");
            await Task.CompletedTask;
            return Ok("Successful");
        }

        [HttpGet, Route("circuitbreaker_aux")]
        public async Task<IActionResult> CircuitBreakerAux()
        {
            _logger.LogInformation("CircuitBreakerAux called");
            await Task.CompletedTask;
            return Ok("Successful");
        }

        [HttpGet, Route("jitter")]
        public async Task<IActionResult> Jitter()
        {
            _logger.LogInformation("Jitter called");
            await Task.CompletedTask;
            return Ok("Successful");
        }

        [HttpGet, Route("bulkhead")]
        public async Task<IActionResult> BulkHead()
        {
            _logger.LogInformation("BulkHead called");
            await Task.CompletedTask;
            return Ok("Successful");
        }

        [HttpGet, Route("faultingbulkhead")]
        public async Task<IActionResult> FaultingBulkhead()
        {
            _logger.LogInformation("FaultingBulkHead called");
            Thread.Sleep(3000);
            //await Task.CompletedTask;
            //return StatusCode((int)HttpStatusCode.InternalServerError);
            throw new HttpRequestException();
        }

        [HttpGet, Route("fallback")]
        public async Task<IActionResult> Fallback()
        {
            _logger.LogInformation("Fallback called");
            await Task.CompletedTask;
            return Ok("Successful");
        }

        [HttpGet, Route("cache")]
        public async Task<IActionResult> Cache()
        {
            _logger.LogInformation("Cache called");
            await Task.CompletedTask;
            return Ok("Successful");
        }
    }
}
