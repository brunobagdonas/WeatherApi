using WeatherApi.DTOs;

namespace WeatherApi.Services.Interfaces
{
    public interface IWeatherService
    {
        Task<CurrentWeatherDto> GetCurrentWeather(string city);
        Task<ForecastDto> GetDayForecast(string city, int daysQuantity);
        Task<IEnumerable<SearchHistoryDto>> GetSearchHistory();

    }
}
