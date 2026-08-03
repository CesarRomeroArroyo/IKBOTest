using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ProductRequests.Infrastructure.Persistence.Configurations;

internal static class DateTimeOffsetConverters
{
    public static readonly ValueConverter<DateTimeOffset, DateTime> Utc = new(
        value => value.UtcDateTime,
        value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));
}
