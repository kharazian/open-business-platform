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
            TriggerScheduleKinds.Daily => AdvanceDaily(next, now),
            TriggerScheduleKinds.Weekly => AdvanceWeekly(next, now),
            TriggerScheduleKinds.Monthly => AdvanceMonthly(next, now),
            _ => null
        };
    }

    private static DateTimeOffset AdvanceDaily(DateTimeOffset next, DateTimeOffset now)
    {
        var days = Math.Max(1, (int)Math.Floor((now - next).TotalDays) + 1);
        return next.AddDays(days);
    }

    private static DateTimeOffset AdvanceWeekly(DateTimeOffset next, DateTimeOffset now)
    {
        var weeks = Math.Max(1, (int)Math.Floor((now - next).TotalDays / 7) + 1);
        return next.AddDays(weeks * 7);
    }

    private static DateTimeOffset AdvanceMonthly(DateTimeOffset next, DateTimeOffset now)
    {
        while (next <= now)
        {
            next = next.AddMonths(1);
        }

        return next;
    }
}
