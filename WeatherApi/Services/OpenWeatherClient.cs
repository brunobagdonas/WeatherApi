using WeatherApi.Services.Interfaces;

namespace WeatherApi.Services
{
    public class OpenWeatherClient : IWeatherClient
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<OpenWeatherClient> _logger;
        private readonly string _apiKey;
        private readonly string _units;
        private readonly string _urlWeatherApi;

        public OpenWeatherClient(HttpClient http, IConfiguration config, ILogger<OpenWeatherClient> logger)
        {
            _http = http;
            _config = config;
            _logger = logger;
            _apiKey = _config["OpenWeather:ApiKey"] ?? throw new ArgumentNullException("OpenWeather:ApiKey");
            _units = _config["OpenWeather:DefaultUnits"] ?? "metric";
            _urlWeatherApi = _config["OpenWeather:BaseUrl"] ?? throw new ArgumentNullException("OpenWeather:BaseUrl");
        }

        public async Task<string> GetCurrentWeatherApi(string city)
        {
            var url = _urlWeatherApi + $"current.json?key={_apiKey}&q={Uri.EscapeDataString(city)}&aqi=no";
            var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync();
        }

        public async Task<string> GetDayForecastApi(string city, int daysQuantity)
        {
            var url = _urlWeatherApi + $"forecast.json?key={_apiKey}&q={Uri.EscapeDataString(city)}&days={daysQuantity}&aqi=no&alerts=no";
            var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync();
        }
    }
}
