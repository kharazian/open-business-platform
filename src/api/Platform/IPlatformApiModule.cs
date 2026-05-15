using System.Reflection;

namespace OpenBusinessPlatform.Api.Platform;

public enum ModuleOwner
{
    Core,
    App
}

public interface IPlatformApiModule
{
    string Id { get; }

    string Name { get; }

    ModuleOwner Owner { get; }

    void MapEndpoints(IEndpointRouteBuilder endpoints);
}

public static class PlatformApiModuleExtensions
{
    public static IEndpointRouteBuilder MapPlatformApiModules(this IEndpointRouteBuilder endpoints, Assembly? assembly = null)
    {
        var modules = DiscoverModules(assembly ?? Assembly.GetExecutingAssembly());

        foreach (var module in modules)
        {
            module.MapEndpoints(endpoints);
        }

        return endpoints;
    }

    private static IReadOnlyList<IPlatformApiModule> DiscoverModules(Assembly assembly)
    {
        return assembly
            .GetTypes()
            .Where(type =>
                typeof(IPlatformApiModule).IsAssignableFrom(type)
                && type is { IsAbstract: false, IsInterface: false }
                && type.GetConstructor(Type.EmptyTypes) is not null)
            .Select(type => (IPlatformApiModule)Activator.CreateInstance(type)!)
            .OrderBy(module => module.Owner)
            .ThenBy(module => module.Id, StringComparer.Ordinal)
            .ToArray();
    }
}
