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
    }
}
