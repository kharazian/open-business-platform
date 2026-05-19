namespace OpenBusinessPlatform.Api.Application.Common;

public abstract class ApplicationServiceBase
{
    protected virtual Task CheckGetPermissionAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task CheckListPermissionAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task CheckCreatePermissionAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task CheckUpdatePermissionAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task CheckDeletePermissionAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
