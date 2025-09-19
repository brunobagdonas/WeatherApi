using Microsoft.AspNetCore.Mvc;
using WeatherApi.Services.Interfaces;

namespace WeatherApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _svc;
        private readonly ILogger<WeatherController> _logger;

        public WeatherController(IWeatherService svc, ILogger<WeatherController> logger)
        {
            _svc = svc;
            _logger = logger;
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent([FromQuery] string city)
        {
            if (string.IsNullOrWhiteSpace(city)) return BadRequest("city is required");

            try
            {
                var json = await _svc.GetCurrentWeather(city);
                return Ok(json);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching current weather for {city}", city);
                return StatusCode(503, "Error fetching data from external weather service.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("forecast")]
        public async Task<IActionResult> GetForecast([FromQuery] string city, [FromQuery] int daysQuantity = 5)
        {
            if (string.IsNullOrWhiteSpace(city)) return BadRequest("city is required");

            if (daysQuantity < 1 || daysQuantity > 5)
                return BadRequest("daysQuantity must be between 1 and 5.");

            try
            {
                var json = await _svc.GetDayForecast(city, daysQuantity);
                return Ok(json);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching forecast for {city}", city);
                return StatusCode(503, "Error fetching data from external weather service.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> History()
        {
            var list = await _svc.GetSearchHistory();
            return Ok(list);
        }
    }
}
