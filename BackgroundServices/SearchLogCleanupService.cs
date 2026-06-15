using ClothingStore.Data;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.BackgroundServices;

public class SearchLogCleanupService(IServiceProvider serviceProvider, ILogger<SearchLogCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
                
                var cutoff = DateTime.UtcNow.AddDays(-180);
                const int batchSize = 5000;
                int totalDeleted = 0;

                while (!stoppingToken.IsCancellationRequested)
                {
                    // EF Core 8 ExecuteDeleteAsync with Take
                    var affected = await dbContext.SearchLogs
                        .Where(x => x.SearchedAt < cutoff)
                        .OrderBy(x => x.SearchLogId)
                        .Take(batchSize)
                        .ExecuteDeleteAsync(stoppingToken);

                    if (affected == 0)
                        break;

                    totalDeleted += affected;
                    
                    // Small delay to prevent locking the database completely
                    await Task.Delay(100, stoppingToken);
                }

                if (totalDeleted > 0)
                {
                    logger.LogInformation("SearchLogCleanupService removed {TotalDeleted} old search logs.", totalDeleted);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing SearchLogCleanupService.");
            }

            // Run once a day
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
