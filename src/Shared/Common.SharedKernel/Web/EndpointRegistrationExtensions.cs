using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Common.SharedKernel.Web;

/// <summary>
/// Provides extension methods for dynamically registering and mapping API endpoints
/// that implement the <see cref="IEndpoint"/> interface.
/// </summary>
public static class EndpointRegistrationExtensions
{
    /// <summary>
    /// Discovers all loaded <see cref="IEndpoint"/> implementations,
    /// creates them through DI-aware activation, and maps their routes.
    /// </summary>
    /// <param name="app">The endpoint route builder used to define API routes.</param>
    public static void MapDiscoveredEndpoints(this IEndpointRouteBuilder app)
    {
        IHostEnvironment environment = app.ServiceProvider.GetRequiredService<IHostEnvironment>();
        EndpointScope currentScope = EndpointScopeResolver.Resolve(environment.EnvironmentName);

        Type[] endpointTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .SelectMany(assembly =>
            {
                try
                {
                    // Include non-exported/internal types so internal endpoint implementations
                    // are discoverable when the repo enforces internal visibility for concretes.
                    return assembly.GetTypes().AsEnumerable();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    return ex.Types.OfType<Type>();
                }
            })
            .Where(type => ImplementsEndpointContract(type) && IsEndpointEnabledForScope(type, currentScope))
            .OrderBy(type => type.FullName)
            .ToArray();

        using IServiceScope scope = app.ServiceProvider.CreateScope();
        IServiceProvider serviceProvider = scope.ServiceProvider;

        foreach (Type endpointType in endpointTypes)
        {
            var endpoint = (IEndpoint)ActivatorUtilities.CreateInstance(serviceProvider, endpointType);
            endpoint.MapEndpoints(app);
        }
    }

    private static bool IsEndpointEnabledForScope(Type endpointType, EndpointScope currentScope)
    {
        EndpointScopeAttribute? endpointScope = endpointType.GetCustomAttribute<EndpointScopeAttribute>();
        return endpointScope is null || endpointScope.Includes(currentScope);
    }

    private static bool ImplementsEndpointContract(Type endpointType)
    {
        if (endpointType.IsInterface || endpointType.IsAbstract)
        {
            return false;
        }

        string? sharedEndpointTypeName = typeof(IEndpoint).FullName;
        return typeof(IEndpoint).IsAssignableFrom(endpointType)
            || endpointType.GetInterfaces().Any(interfaceType =>
                interfaceType.FullName == sharedEndpointTypeName
                || (interfaceType.Name == nameof(IEndpoint)
                    && interfaceType.GetMethod(nameof(IEndpoint.MapEndpoints), [typeof(IEndpointRouteBuilder)]) is not null));
    }
}
