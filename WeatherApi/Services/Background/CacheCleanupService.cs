using Microsoft.EntityFrameworkCore;
using WeatherApi.Data;

namespace WeatherApi.Services.Background
{
    public class CacheCleanupService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<CacheCleanupService> _logger;
        private readonly TimeSpan _interval;
        private readonly int _expirationMinutes;

        public CacheCleanupService(IServiceProvider sp, IConfiguration config, ILogger<CacheCleanupService> logger)
        {
            _sp = sp;
            _logger = logger;
            _interval = TimeSpan.FromHours(1); // roda a cada 1 hora
            _expirationMinutes = int.Parse(config["Cache:ExpirationMinutes"] ?? "60");
        }

        /// <summary>
        /// Executa a limpeza do cache uma única vez.
        /// Pode ser usado em testes unitários.
        /// </summary>
        internal async Task CleanupOnceAsync(WeatherDbContext db)
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-_expirationMinutes);
            var expired = await db.CachedWeathers
                .Where(c => c.RetrievedAtUtc < cutoff)
                .ToListAsync();

            if (expired.Any())
            {
                db.CachedWeathers.RemoveRange(expired);
                await db.SaveChangesAsync();
                _logger.LogInformation("Removed {count} expired cached entries", expired.Count);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CacheCleanupService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();

                    await CleanupOnceAsync(db);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during cache cleanup");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("CacheCleanupService stopped");
        }
    }
}
