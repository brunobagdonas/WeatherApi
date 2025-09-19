using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace WeatherApi.Data
{
    public class WeatherDbContext : DbContext
    {
        public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options) { }

        public virtual DbSet<CachedWeather> CachedWeathers { get; set; } = null!;
    }
}

