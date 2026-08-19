namespace OpenBusinessPlatform.Api.Modules.Processing;

public sealed class ProcessingJobOptions
{
    public const string SectionName = "ProcessingJobs";
    public int PollingIntervalSeconds { get; set; } = 30;
}
