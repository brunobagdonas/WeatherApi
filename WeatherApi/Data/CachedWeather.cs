using System.ComponentModel.DataAnnotations;

namespace WeatherApi.Data
{
    public class CachedWeather
    {
        [Key]
        public int Id
        {
            get; set;
        }

        [Required]
        public string City { get; set; } = default!;

        // "current" or "forecast"
        [Required]
        public string Type { get; set; } = default!;

        // Raw JSON payload from external API
        [Required]
        public string PayloadJson { get; set; } = default!;

        public DateTime RetrievedAtUtc
        {
            get; set;
        }
    }
}
