namespace OpenBusinessPlatform.Api.Application.Common;

public sealed record RecurringSchedule(
    string Kind,
    string TimeZone,
    DateTimeOffset StartAt,
    int Interval = 1,
    int? DayOfWeek = null,
    int? DayOfMonth = null);

public static class RecurringScheduleCalculator
{
    public static DateTimeOffset? CalculateNextRun(RecurringSchedule? schedule, DateTimeOffset now)
    {
        if (schedule is null) return null;
        var next = schedule.StartAt.ToUniversalTime();
        if (next > now) return next;

        return schedule.Kind switch
        {
            "once" => null,
            "daily" => AdvanceDaily(next, now, schedule.Interval),
            "weekly" => AdvanceWeekly(next, now, schedule.Interval, schedule.DayOfWeek),
            "monthly" => AdvanceMonthly(next, now, schedule.Interval, schedule.DayOfMonth),
            _ => null
        };
    }

    private static DateTimeOffset AdvanceDaily(DateTimeOffset next, DateTimeOffset now, int interval)
    {
        interval = Math.Max(1, interval);
        var days = Math.Max(interval, ((int)Math.Floor((now - next).TotalDays / interval) + 1) * interval);
        return next.AddDays(days);
    }

    private static DateTimeOffset AdvanceWeekly(DateTimeOffset next, DateTimeOffset now, int interval, int? dayOfWeek)
    {
        interval = Math.Max(1, interval);
        var firstRun = dayOfWeek is null ? next : next.AddDays(((dayOfWeek.Value - (int)next.DayOfWeek) + 7) % 7);
        if (firstRun > now) return firstRun;
        var weeks = Math.Max(interval, ((int)Math.Floor((now - firstRun).TotalDays / (7 * interval)) + 1) * interval);
        return firstRun.AddDays(weeks * 7);
    }

    private static DateTimeOffset AdvanceMonthly(DateTimeOffset next, DateTimeOffset now, int interval, int? dayOfMonth)
    {
        interval = Math.Max(1, interval);
        var months = 0;
        var candidate = BuildMonthlyCandidate(next, months, dayOfMonth);
        while (candidate <= now)
        {
            months += interval;
            candidate = BuildMonthlyCandidate(next, months, dayOfMonth);
        }
        return candidate;
    }

    private static DateTimeOffset BuildMonthlyCandidate(DateTimeOffset start, int monthsToAdd, int? dayOfMonth)
    {
        var monthStart = new DateTimeOffset(start.Year, start.Month, 1, start.Hour, start.Minute, start.Second, start.Offset)
            .AddTicks(start.Ticks % TimeSpan.TicksPerSecond)
            .AddMonths(monthsToAdd);
        var day = Math.Min(dayOfMonth ?? start.Day, DateTime.DaysInMonth(monthStart.Year, monthStart.Month));
        return new DateTimeOffset(monthStart.Year, monthStart.Month, day, start.Hour, start.Minute, start.Second, start.Offset)
            .AddTicks(start.Ticks % TimeSpan.TicksPerSecond);
    }
}
