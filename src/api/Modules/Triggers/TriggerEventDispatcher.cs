using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed class TriggerEventDispatcher
{
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly TriggerExecutionService executionService;
    private readonly ILogger<TriggerEventDispatcher> logger;

    public TriggerEventDispatcher(
        OpenBusinessPlatformDbContext dbContext,
        TriggerExecutionService executionService,
        ILogger<TriggerEventDispatcher> logger)
    {
        this.dbContext = dbContext;
        this.executionService = executionService;
        this.logger = logger;
    }

    public async Task DispatchAsync(TriggerEventContext context, CancellationToken cancellationToken)
    {
        try
        {
            var triggers = await dbContext.Triggers
                .AsNoTracking()
                .Where(trigger =>
                    trigger.FormId == context.FormId
                    && trigger.EventName == context.EventName
                    && trigger.IsEnabled
                    && !trigger.IsDeleted)
                .OrderBy(trigger => trigger.CreatedAt)
                .ThenBy(trigger => trigger.Name)
                .ToArrayAsync(cancellationToken);

            foreach (var trigger in triggers)
            {
                try
                {
                    await executionService.ExecuteAsync(trigger, context, cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Trigger {TriggerId} failed during dispatch.", trigger.Id);
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Trigger dispatch failed for event {EventName} on record {RecordId}.", context.EventName, context.RecordId);
        }
    }
}
