using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherApi.Data;
using WeatherApi.Services;
using WeatherApi.Services.Interfaces;

namespace WeatherApi.Tests
{
    public class WeatherServiceTests
    {
        private readonly WeatherService _svc;
        private readonly Mock<IWeatherClient> _clientMock;
        private readonly WeatherDbContext _db;
        private readonly ILogger<WeatherService> _logger;

        public WeatherServiceTests()
        {
            // Mock do cliente
            _clientMock = new Mock<IWeatherClient>();

            // Configuração do DbContext com InMemoryDatabase
            var options = new DbContextOptionsBuilder<WeatherDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // banco isolado por teste
                .Options;

            _db = new WeatherDbContext(options);

            // Logger fake
            _logger = Mock.Of<ILogger<WeatherService>>();

            // Configuração do cache
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Cache:ExpirationMinutes"]).Returns("60");

            // Criação do serviço
            _svc = new WeatherService(_db, _clientMock.Object, configMock.Object, _logger);
        }


        [Fact]
        public async Task GetCurrentWeather_ReturnsCachedDto_WhenCacheExists()
        {
            var city = "London";
            var payloadJson = "{\"location\":{\"name\":\"London\"},\"current\":{\"temp_c\":20,\"humidity\":50,\"condition\":{\"text\":\"Sunny\"},\"wind_kph\":10}}";

            _db.CachedWeathers.Add(new CachedWeather { City = city, Type = "current", PayloadJson = payloadJson, RetrievedAtUtc = DateTime.UtcNow });
            await _db.SaveChangesAsync();

            var result = await _svc.GetCurrentWeather(city);

            Assert.Equal("London", result.City);
            Assert.Equal(20, result.TemperatureC);
            Assert.Equal(50, result.Humidity);
            Assert.Equal("Sunny", result.Description);
            Assert.Equal(10, result.WindKph);
        }

        [Fact]
        public async Task GetCurrentWeather_CallsApi_WhenNoCache()
        {
            var city = "Paris";
            var payloadJson = "{\"location\":{\"name\":\"Paris\"},\"current\":{\"temp_c\":22,\"humidity\":60,\"condition\":{\"text\":\"Cloudy\"},\"wind_kph\":5}}";

            _clientMock.Setup(c => c.GetCurrentWeatherApi(city)).ReturnsAsync(payloadJson);

            var result = await _svc.GetCurrentWeather(city);

            Assert.Equal("Paris", result.City);
            Assert.Equal(22, result.TemperatureC);
            Assert.Equal(60, result.Humidity);
            Assert.Equal("Cloudy", result.Description);
            Assert.Equal(5, result.WindKph);
        }

        [Fact]
        public async Task GetCurrentWeather_Throws_WhenClientFails()
        {
            var city = "Paris";
            _clientMock.Setup(c => c.GetCurrentWeatherApi(city)).ThrowsAsync(new HttpRequestException());

            await Assert.ThrowsAsync<HttpRequestException>(() => _svc.GetCurrentWeather(city));
        }

        [Fact]
        public async Task GetDayForecast_ReturnsCachedDto_WhenCacheExists()
        {
            var city = "London";
            var payloadJson = "{\"location\":{\"name\":\"London\"},\"forecast\":{\"forecastday\":[{\"date\":\"2025-09-19\",\"day\":{\"maxtemp_c\":25.0,\"mintemp_c\":15.0,\"avgtemp_c\":20.0,\"avghumidity\":60,\"maxwind_kph\":15,\"condition\":{\"text\":\"Sunny\"}}}]}}";

            _db.CachedWeathers.Add(new CachedWeather { City = city, Type = "forecast", PayloadJson = payloadJson, RetrievedAtUtc = DateTime.UtcNow });
            await _db.SaveChangesAsync();

            var result = await _svc.GetDayForecast(city, 1);

            var day = result.Days.First();
            Assert.Equal(new DateTime(2025, 9, 19), day.Date);
            Assert.Equal(25, day.MaxTempC);
        }

        [Fact]
        public async Task GetDayForecast_CallsApi_WhenNoCache()
        {
            var city = "Paris";
            var payloadJson = "{\"location\":{\"name\":\"Paris\"},\"forecast\":{\"forecastday\":[{\"date\":\"2025-09-19\",\"day\":{\"maxtemp_c\":26.0,\"mintemp_c\":16.0,\"avgtemp_c\":21.0,\"avghumidity\":55,\"maxwind_kph\":10,\"condition\":{\"text\":\"Cloudy\"}}}]}}";

            _clientMock.Setup(c => c.GetDayForecastApi(city, 1)).ReturnsAsync(payloadJson);

            var result = await _svc.GetDayForecast(city, 1);

            var day = result.Days.First();
            Assert.Equal(26, day.MaxTempC);
        }

        [Fact]
        public async Task GetDayForecast_Throws_WhenClientFails()
        {
            var city = "Paris";
            _clientMock.Setup(c => c.GetDayForecastApi(city, 1)).ThrowsAsync(new HttpRequestException());

            await Assert.ThrowsAsync<HttpRequestException>(() => _svc.GetDayForecast(city, 1));
        }


        [Fact]
        public async Task GetSearchHistory_ReturnsListOfCities()
        {
            // Arrange
            _db.CachedWeathers.AddRange(new[]
            {
                new CachedWeather
                {
                    City = "London",
                    Type = "current",
                    PayloadJson = "{}", // necessário por ser [Required]
                    RetrievedAtUtc = DateTime.UtcNow
                },
                new CachedWeather
                {
                    City = "Paris",
                    Type = "forecast",
                    PayloadJson = "{}", // necessário por ser [Required]
                    RetrievedAtUtc = DateTime.UtcNow
                }
            });
            await _db.SaveChangesAsync();

            // Act
            var result = await _svc.GetSearchHistory();
            var cities = result.Select(x => x.City).ToList();

            // Assert
            Assert.Contains("London", cities);
            Assert.Contains("Paris", cities);
        }


        [Fact]
        public async Task GetSearchHistory_Throws_WhenDbFails()
        {
            // Arrange: Criar uma classe de DbContext que lança exceção ao acessar CachedWeathers
            var options = new DbContextOptionsBuilder<WeatherDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var db = new FaultyWeatherDbContext(options); // DbContext customizado
            var svc = new WeatherService(db, _clientMock.Object, Mock.Of<IConfiguration>(), _logger);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => svc.GetSearchHistory());
        }

        // DbContext customizado que lança exceção
        public class FaultyWeatherDbContext : WeatherDbContext
        {
            public FaultyWeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options) { }

            public override DbSet<CachedWeather> CachedWeathers => throw new Exception("DB failure");
        }


    }

}
