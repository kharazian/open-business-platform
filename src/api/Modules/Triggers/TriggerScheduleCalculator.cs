namespace OpenBusinessPlatform.Api.Modules.Triggers;

public static class TriggerScheduleCalculator
{
    public static DateTimeOffset? CalculateNextRun(TriggerScheduleDefinition? schedule, DateTimeOffset now)
    {
        if (schedule is null)
        {
            return null;
        }

        var next = schedule.StartAt.ToUniversalTime();

        if (next > now)
        {
            return next;
        }

        return schedule.Kind switch
        {
            TriggerScheduleKinds.Once => null,
            TriggerScheduleKinds.Daily => AdvanceDaily(next, now, schedule.Interval),
            TriggerScheduleKinds.Weekly => AdvanceWeekly(next, now, schedule.Interval, schedule.DayOfWeek),
            TriggerScheduleKinds.Monthly => AdvanceMonthly(next, now, schedule.Interval, schedule.DayOfMonth),
            _ => null
        };
    }

    private static DateTimeOffset AdvanceDaily(DateTimeOffset next, DateTimeOffset now, int scheduleInterval)
    {
        var interval = Math.Max(1, scheduleInterval);
        var days = Math.Max(interval, ((int)Math.Floor((now - next).TotalDays / interval) + 1) * interval);
        return next.AddDays(days);
    }

    private static DateTimeOffset AdvanceWeekly(DateTimeOffset next, DateTimeOffset now, int scheduleInterval, int? dayOfWeek)
    {
        var interval = Math.Max(1, scheduleInterval);
        var firstRun = dayOfWeek is null
            ? next
            : next.AddDays(((dayOfWeek.Value - (int)next.DayOfWeek) + 7) % 7);

        if (firstRun > now)
        {
            return firstRun;
        }

        var weeks = Math.Max(interval, ((int)Math.Floor((now - firstRun).TotalDays / (7 * interval)) + 1) * interval);
        return firstRun.AddDays(weeks * 7);
    }

    private static DateTimeOffset AdvanceMonthly(DateTimeOffset next, DateTimeOffset now, int scheduleInterval, int? dayOfMonth)
    {
        var interval = Math.Max(1, scheduleInterval);
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

        return new DateTimeOffset(
            monthStart.Year,
            monthStart.Month,
            day,
            start.Hour,
            start.Minute,
            start.Second,
            start.Offset).AddTicks(start.Ticks % TimeSpan.TicksPerSecond);
    }
}
