using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Application.Core.Behaviors;
using SmartRail.Application.Core.Events;

namespace SmartRail.Application.Core;

public static class DependencyInjection
{
    // Call once from the host with every module's Application assembly.
    // Registering per module would add the pipeline behaviours once per module.
    public static IServiceCollection AddApplicationCore(this IServiceCollection services, params Assembly[] moduleAssemblies)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(moduleAssemblies);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssemblies(moduleAssemblies, includeInternalTypes: true);

        var handlerType = typeof(IIntegrationEventHandler<>);
        var handlers = moduleAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false });

        foreach (var type in handlers)
        foreach (var contract in type.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType))
            services.AddScoped(contract, type);

        return services;
    }
}
