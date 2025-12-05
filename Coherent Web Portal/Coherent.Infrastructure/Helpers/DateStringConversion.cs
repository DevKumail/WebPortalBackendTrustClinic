namespace Coherent.Infrastructure.Helpers;

/// <summary>
/// Helper class for date string conversions matching the existing system format
/// </summary>
public static class DateStringConversion
{
    /// <summary>
    /// Convert string date (YYYYMMDDHHMMSS) to DateTime
    /// </summary>
    public static DateTime StringToDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString))
            return DateTime.MinValue;

        try
        {
            if (dateString.Length < 8)
                return DateTime.MinValue;

            if (dateString.Length == 8)
                dateString = dateString.PadRight(14, '0');

            int year = int.Parse(dateString.Substring(0, 4));
            int month = int.Parse(dateString.Substring(4, 2));
            int day = int.Parse(dateString.Substring(6, 2));
            int hour = dateString.Length >= 10 ? int.Parse(dateString.Substring(8, 2)) : 0;
            int minute = dateString.Length >= 12 ? int.Parse(dateString.Substring(10, 2)) : 0;
            int second = dateString.Length >= 14 ? int.Parse(dateString.Substring(12, 2)) : 0;

            return new DateTime(year, month, day, hour, minute, second);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// Convert DateTime to string (YYYYMMDDHHMMSS)
    /// </summary>
    public static string DateToString(DateTime date)
    {
        return date.ToString("yyyyMMddHHmmss");
    }

    /// <summary>
    /// Convert DateTime to short string (YYYYMMDD)
    /// </summary>
    public static string DateToShortString(DateTime date)
    {
        return date.ToString("yyyyMMdd");
    }

    /// <summary>
    /// Check if date is weekend
    /// </summary>
    public static bool IsWeekend(DateTime date)
    {
        return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
    }

    /// <summary>
    /// Check if day exists in the Days bit flags
    /// </summary>
    public static bool DayExists(string dayName, long days)
    {
        var dayValue = dayName switch
        {
            "Monday" => 1,
            "Tuesday" => 2,
            "Wednesday" => 4,
            "Thursday" => 8,
            "Friday" => 16,
            "Saturday" => 32,
            "Sunday" => 64,
            _ => 0
        };

        return (days & dayValue) > 0;
    }

    /// <summary>
    /// Convert time string to DateTime on specific date
    /// </summary>
    public static DateTime TimeStringToDate(DateTime date, string timeString)
    {
        if (string.IsNullOrEmpty(timeString))
            return date;

        try
        {
            var time = DateTime.ParseExact(timeString, "hh:mm tt", System.Globalization.CultureInfo.InvariantCulture);
            return new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second);
        }
        catch
        {
            return date;
        }
    }

    /// <summary>
    /// Get list of day names from bit flags
    /// </summary>
    public static List<string> GetDayNames(long days)
    {
        var daysList = new List<string>();
        var daysMap = new Dictionary<string, int>
        {
            {"Monday", 1},
            {"Tuesday", 2},
            {"Wednesday", 4},
            {"Thursday", 8},
            {"Friday", 16},
            {"Saturday", 32},
            {"Sunday", 64}
        };

        foreach (var day in daysMap)
        {
            if ((days & day.Value) > 0)
            {
                daysList.Add(day.Key);
            }
        }

        return daysList;
    }
}
