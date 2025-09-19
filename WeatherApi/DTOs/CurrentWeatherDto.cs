namespace WeatherApi.DTOs
{
    public class CurrentWeatherDto
    {
        public string City
        {
            get; set;
        }
        public double TemperatureC
        {
            get; set;
        }
        public int Humidity
        {
            get; set;
        }
        public string Description
        {
            get; set;
        }
        public double WindKph
        {
            get; set;
        }
    }
}
