using System.Threading.Tasks;
using Api.Dto.Location;
using HttpApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HttpApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class LocationController : Controller
    {
        private readonly ILogger<LocationController> _logger;
        private readonly ILocationService _service;

        public LocationController(ILogger<LocationController> logger, ILocationService service)
        {
            _logger = logger;
            _service = service;
        }

        [HttpPost]
        public async Task<JsonResult> GetLocation([FromBody] LocationRequestDto request)
        {
            _logger.LogDebug($"GetLocation request with ip string {request.IpAddress}");
            var result = await _service.GetLocation(request);
            return Json(result);
        }
    }
}