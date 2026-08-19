using OpenBusinessPlatform.Api.Application.Common;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public static class TriggerScheduleCalculator
{
    public static DateTimeOffset? CalculateNextRun(TriggerScheduleDefinition? schedule, DateTimeOffset now)
    {
        return schedule is null ? null : RecurringScheduleCalculator.CalculateNextRun(
            new RecurringSchedule(schedule.Kind, schedule.TimeZone, schedule.StartAt, schedule.Interval, schedule.DayOfWeek, schedule.DayOfMonth),
            now);
    }
}
