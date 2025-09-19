namespace WeatherApi.Services.Interfaces
{
    public interface IWeatherClient
    {
        Task<string> GetCurrentWeatherApi(string city);
        Task<string> GetDayForecastApi(string city, int daysQuantity);
    }
}
