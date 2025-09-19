using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherApi.Controllers;
using WeatherApi.DTOs;
using WeatherApi.Services.Interfaces;

namespace WeatherApi.Tests
{
    public class WeatherControllerTests
    {
        private readonly Mock<IWeatherService> _svcMock;
        private readonly Mock<ILogger<WeatherController>> _loggerMock;
        private readonly WeatherController _controller;

        public WeatherControllerTests()
        {
            _svcMock = new Mock<IWeatherService>();
            _loggerMock = new Mock<ILogger<WeatherController>>();
            _controller = new WeatherController(_svcMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetCurrent_ReturnsOk_WithCurrentWeatherDto()
        {
            // Arrange
            var city = "London";
            var dto = new CurrentWeatherDto
            {
                City = city,
                TemperatureC = 25,
                Humidity = 50,
                Description = "Sunny",
                WindKph = 15
            };

            _svcMock.Setup(s => s.GetCurrentWeather(city)).ReturnsAsync(dto);

            // Act
            var result = await _controller.GetCurrent(city);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedDto = Assert.IsType<CurrentWeatherDto>(okResult.Value);

            Assert.Equal(dto.City, returnedDto.City);
            Assert.Equal(dto.TemperatureC, returnedDto.TemperatureC);
            Assert.Equal(dto.Humidity, returnedDto.Humidity);
        }

        [Fact]
        public async Task GetCurrent_Returns503_OnHttpRequestException()
        {
            // Arrange
            var city = "London";
            _svcMock.Setup(s => s.GetCurrentWeather(city))
                    .ThrowsAsync(new HttpRequestException("External API failed"));

            // Act
            var result = await _controller.GetCurrent(city);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(503, statusResult.StatusCode);
            Assert.Equal("Error fetching data from external weather service.", statusResult.Value);
        }

        [Fact]
        public async Task GetCurrent_Returns500_OnException()
        {
            // Arrange
            var city = "London";
            _svcMock.Setup(s => s.GetCurrentWeather(city))
                    .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.GetCurrent(city);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
            Assert.Equal("Internal server error", statusResult.Value);
        }

        [Fact]
        public async Task GetForecast_ReturnsOk_WithForecastDto()
        {
            // Arrange
            var city = "London";
            var days = 3;

            var forecast = new ForecastDto
            {
                City = city,
                Days = new List<ForecastDayDto>
                {
                    new ForecastDayDto
                    {
                        Date = System.DateTime.Today,
                        MaxTempC = 25,
                        MinTempC = 15,
                        AvgTempC = 20,
                        AvgHumidity = 60,
                        MaxWindKph = 15,
                        Condition = "Sunny"
                    }
                }
            };

            _svcMock.Setup(s => s.GetDayForecast(city, days)).ReturnsAsync(forecast);

            // Act
            var result = await _controller.GetForecast(city, days);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedDto = Assert.IsType<ForecastDto>(okResult.Value);

            Assert.Equal(forecast.City, returnedDto.City);
            Assert.Single(returnedDto.Days);
            Assert.Equal(forecast.Days[0].MaxTempC, returnedDto.Days[0].MaxTempC);
        }

        [Fact]
        public async Task GetForecast_Returns503_OnHttpRequestException()
        {
            // Arrange
            var city = "London";
            var daysQuantity = 3;
            _svcMock.Setup(s => s.GetDayForecast(city, daysQuantity))
                    .ThrowsAsync(new HttpRequestException("External API failed"));

            // Act
            var result = await _controller.GetForecast(city, daysQuantity);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(503, statusResult.StatusCode);
            Assert.Equal("Error fetching data from external weather service.", statusResult.Value);
        }

        [Fact]
        public async Task GetForecast_Returns500_OnException()
        {
            // Arrange
            var city = "London";
            var daysQuantity = 3;
            _svcMock.Setup(s => s.GetDayForecast(city, daysQuantity))
                    .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.GetForecast(city, daysQuantity);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
            Assert.Equal("Internal server error", statusResult.Value);
        }



        [Fact]
        public async Task History_ReturnsOk_WithListOfSearchHistoryDto()
        {
            // Arrange
            var history = new List<SearchHistoryDto>
            {
                new SearchHistoryDto { City = "London", Type = "current", RetrievedAtUtc = DateTime.UtcNow },
                new SearchHistoryDto { City = "Paris", Type = "forecast", RetrievedAtUtc = DateTime.UtcNow }
            };

            _svcMock.Setup(s => s.GetSearchHistory())
                    .ReturnsAsync(history);

            // Act
            var result = await _controller.History();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedList = Assert.IsType<List<SearchHistoryDto>>(okResult.Value);

            Assert.Equal(history.Count, returnedList.Count);
            Assert.Equal(history[0].City, returnedList[0].City);
            Assert.Equal(history[0].Type, returnedList[0].Type);
            Assert.Equal(history[1].City, returnedList[1].City);
            Assert.Equal(history[1].Type, returnedList[1].Type);
        }

    }
}
