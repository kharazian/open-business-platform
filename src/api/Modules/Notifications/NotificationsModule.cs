using OpenBusinessPlatform.Api.Platform;

namespace OpenBusinessPlatform.Api.Modules.Notifications;

public sealed class NotificationsModule : IPlatformApiModule
{
    public string Id => "app.notifications";

    public string Name => "Notifications";

    public ModuleOwner Owner => ModuleOwner.App;

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapNotificationsEndpoints();
    }
}
