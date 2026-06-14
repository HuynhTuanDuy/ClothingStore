using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClothingStore.Services;

public interface IDateTimeService
{
    DateTime UtcNow { get; }
    DateTime ConvertUtcToLocal(DateTime utc);
    DateTime ConvertLocalToUtc(DateTime local);
}

public class DateTimeService : IDateTimeService
{
    private readonly TimeZoneInfo _timeZone;
    private readonly ILogger<DateTimeService> _logger;

    public DateTimeService(IConfiguration configuration, ILogger<DateTimeService> logger)
    {
        _logger = logger;
        var tzId = configuration["TimeZoneSettings:DisplayTimeZone"];
        
        // Fallback if missing or invalid
        if (string.IsNullOrWhiteSpace(tzId))
        {
            tzId = "SE Asia Standard Time";
        }

        try
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        }
        catch (TimeZoneNotFoundException ex)
        {
            _logger.LogCritical(ex, "Invalid timezone configuration: {TimeZoneId}. Fallback to Local Time.", tzId);
            // If running on Linux and the config wasn't updated to IANA 'Asia/Ho_Chi_Minh', fallback to local
            _timeZone = TimeZoneInfo.Local;
        }
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime ConvertUtcToLocal(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(utc, _timeZone);

    public DateTime ConvertLocalToUtc(DateTime local)
    {
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, _timeZone);
    }
}
