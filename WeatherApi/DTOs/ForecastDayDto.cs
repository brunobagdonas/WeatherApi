namespace WeatherApi.DTOs
{
    public class ForecastDayDto
    {
        public DateTime Date
        {
            get; set;
        }

        // Dados do "day"
        public double MaxTempC
        {
            get; set;
        }
        public double MinTempC
        {
            get; set;
        }
        public double AvgTempC
        {
            get; set;
        }
        public int AvgHumidity
        {
            get; set;
        }
        public double MaxWindKph
        {
            get; set;
        }
        public string Condition
        {
            get; set;
        }
    }
}
