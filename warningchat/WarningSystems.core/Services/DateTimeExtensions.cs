
namespace WarningSystems.core.Services;

public static class DateTimeExtensions
{
    public static DateTime ToSast(this DateTime utcDateTime)
    {
        return utcDateTime.ToUniversalTime().AddHours(2);
    }
}
