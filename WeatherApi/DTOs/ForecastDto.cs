namespace WeatherApi.DTOs
{
    public class ForecastDto
    {
        public string City
        {
            get; set;
        }
        public string Country
        {
            get; set;
        }
        public List<ForecastDayDto> Days { get; set; } = new();
    }
}
