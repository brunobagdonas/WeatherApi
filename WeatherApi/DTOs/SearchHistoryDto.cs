namespace WeatherApi.DTOs
{
    public class SearchHistoryDto
    {
        public string City
        {
            get; set;
        }
        public string Type
        {
            get; set;
        } 
        public DateTime RetrievedAtUtc
        {
            get; set;
        }
    }
}
