using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed class TriggerEventDispatcher
{
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly TriggerExecutionService executionService;

    public TriggerEventDispatcher(
        OpenBusinessPlatformDbContext dbContext,
        TriggerExecutionService executionService)
    {
        this.dbContext = dbContext;
        this.executionService = executionService;
    }

    public async Task DispatchAsync(TriggerEventContext context, CancellationToken cancellationToken)
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
            await executionService.ExecuteAsync(trigger, context, cancellationToken);
        }
    }
}
