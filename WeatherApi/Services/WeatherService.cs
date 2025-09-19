using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WeatherApi.Data;
using WeatherApi.DTOs;
using WeatherApi.Services.Interfaces;

namespace WeatherApi.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly WeatherDbContext _db;
        private readonly IWeatherClient _client;
        private readonly ILogger<WeatherService> _logger;
        private readonly int _cacheMinutes;

        public WeatherService(WeatherDbContext db, IWeatherClient client, IConfiguration config, ILogger<WeatherService> logger)
        {
            _db = db;
            _client = client;
            _logger = logger;
            _cacheMinutes = int.Parse(config["Cache:ExpirationMinutes"] ?? "60");
        }

        public async Task<CurrentWeatherDto> GetCurrentWeather(string city)
        {
            try
            {
                var key = "current";

                //VERIFICA SE TEM CACHE
                var cached = await _db.CachedWeathers
                    .Where(c => c.City.ToLower() == city.ToLower() && c.Type == key)
                    .OrderByDescending(c => c.RetrievedAtUtc)
                    .FirstOrDefaultAsync();

                //SE TIVER CACHE, RETORNA INFO DO BANCO
                if (cached != null && (DateTime.UtcNow - cached.RetrievedAtUtc).TotalMinutes < _cacheMinutes)
                {
                    _logger.LogInformation("Returning cached current weather for {city}", city);

                    using var cachedDoc = JsonDocument.Parse(cached.PayloadJson);
                    var cachedRoot = cachedDoc.RootElement;

                    var cachedCurrent = cachedRoot.GetProperty("current");
                    var cachedLocation = cachedRoot.GetProperty("location");

                    return new CurrentWeatherDto
                    {
                        City = cachedLocation.GetProperty("name").GetString(),
                        TemperatureC = cachedCurrent.GetProperty("temp_c").GetDouble(),
                        Humidity = cachedCurrent.GetProperty("humidity").GetInt32(),
                        Description = cachedCurrent.GetProperty("condition").GetProperty("text").GetString(),
                        WindKph = cachedCurrent.GetProperty("wind_kph").GetDouble()
                    };
                }

                //SE NAO TIVER CACHE CHAMA A API 
                _logger.LogInformation("Fetching current weather for {city} from external API", city);
                var payload = await _client.GetCurrentWeatherApi(city);

                var entity = new CachedWeather
                {
                    City = city,
                    Type = key,
                    PayloadJson = payload,
                    RetrievedAtUtc = DateTime.UtcNow
                };

                _db.CachedWeathers.Add(entity);
                await _db.SaveChangesAsync();

                // Desserializa o JSON para pegar os dados que precisa
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;

                var current = root.GetProperty("current");
                var location = root.GetProperty("location");

                var dto = new CurrentWeatherDto
                {
                    City = location.GetProperty("name").GetString(),
                    TemperatureC = current.GetProperty("temp_c").GetDouble(),
                    Humidity = current.GetProperty("humidity").GetInt32(),
                    Description = current.GetProperty("condition").GetProperty("text").GetString(),
                    WindKph = current.GetProperty("wind_kph").GetDouble()
                };

                return dto;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching forecast for {city}", city);
                throw; 
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing forecast JSON for {city}", city);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Get5DayForecastAsync for {city}", city);
                throw;
            }

        }

        public async Task<ForecastDto> GetDayForecast(string city, int daysQuantity)
        {
            try
            {
                var key = $"forecast_{daysQuantity}";
                
                //VERIFICA SE TEM CACHE
                var cached = await _db.CachedWeathers
                    .Where(c => c.City.ToLower() == city.ToLower() && c.Type == key)
                    .OrderByDescending(c => c.RetrievedAtUtc)
                    .FirstOrDefaultAsync();

                if (cached != null && (DateTime.UtcNow - cached.RetrievedAtUtc).TotalMinutes < _cacheMinutes)
                {
                    _logger.LogInformation("Returning cached forecast for {city}", city);

                    return ParseForecastDto(cached.PayloadJson, daysQuantity);
                }

                //SE NÃO TIVER CACHE, CHAMA A API 
                _logger.LogInformation("Fetching forecast for {city} from external API", city);
                var payload = await _client.GetDayForecastApi(city, daysQuantity);

                var entity = new CachedWeather
                {
                    City = city,
                    Type = key,
                    PayloadJson = payload,
                    RetrievedAtUtc = DateTime.UtcNow
                };

                _db.CachedWeathers.Add(entity);
                await _db.SaveChangesAsync();

                return ParseForecastDto(payload, daysQuantity);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error fetching forecast for {city}", city);
                throw; // mantém stack trace original
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing forecast JSON for {city}", city);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Get5DayForecastAsync for {city}", city);
                throw;
            }
        }

        //MODIFICA O FORMATO QUE RETORNA AO CLIENTE
        private ForecastDto ParseForecastDto(string payload, int daysQuantity)
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            var location = root.GetProperty("location");
            var forecastDays = root
                .GetProperty("forecast")
                .GetProperty("forecastday")
                .EnumerateArray()
                .Take(daysQuantity);

            var dto = new ForecastDto
            {
                City = location.GetProperty("name").GetString(),
                Days = forecastDays.Select(day => new ForecastDayDto
                {
                    Date = day.GetProperty("date").GetDateTime(),
                    MaxTempC = day.GetProperty("day").GetProperty("maxtemp_c").GetDouble(),
                    MinTempC = day.GetProperty("day").GetProperty("mintemp_c").GetDouble(),
                    AvgTempC = day.GetProperty("day").GetProperty("avgtemp_c").GetDouble(),
                    AvgHumidity = day.GetProperty("day").GetProperty("avghumidity").GetInt32(),
                    MaxWindKph = day.GetProperty("day").GetProperty("maxwind_kph").GetDouble(),
                    Condition = day.GetProperty("day").GetProperty("condition").GetProperty("text").GetString()
                }).ToList()
            };

            return dto;
        }


        public async Task<IEnumerable<SearchHistoryDto>> GetSearchHistory()
        {
            try
            {
                // Retorna as últimas buscas, mais recentes primeiro
                var items = await _db.CachedWeathers
                    .OrderByDescending(c => c.RetrievedAtUtc)
                    .Select(c => new SearchHistoryDto
                    {
                        City = c.City,
                        Type = c.Type,
                        RetrievedAtUtc = c.RetrievedAtUtc.AddHours(-3) //HORARIO DE BRASILIA
                    })
                    .ToListAsync();

                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching search history");
                throw;
            }
        }

    }
}
