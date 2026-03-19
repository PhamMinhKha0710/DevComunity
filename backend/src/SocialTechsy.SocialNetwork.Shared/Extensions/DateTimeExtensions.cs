namespace SocialTechsy.SocialNetwork.Shared.Extensions;

public static class DateTimeExtensions
{
    public static DateTime ToUtc(this DateTime dt)
    {
        return dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
    }

    public static DateTime StartOfDay(this DateTime dt)
    {
        return dt.Date;
    }

    public static DateTime EndOfDay(this DateTime dt)
    {
        return dt.Date.AddDays(1).AddTicks(-1);
    }

    public static string ToIso8601(this DateTime dt)
    {
        return dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    public static bool IsToday(this DateTime dt)
    {
        return dt.Date == DateTime.UtcNow.Date;
    }

    public static string ToRelativeString(this DateTime dt)
    {
        var diff = DateTime.UtcNow - dt;
        if (diff.TotalSeconds < 60)
            return "just now";
        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}d ago";
        if (diff.TotalDays < 30)
            return $"{(int)(diff.TotalDays / 7)}w ago";
        if (diff.TotalDays < 365)
            return $"{(int)(diff.TotalDays / 30)}mo ago";
        return $"{(int)(diff.TotalDays / 365)}y ago";
    }
}
