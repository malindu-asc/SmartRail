using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Modules.Operations.Application.Abstractions;
using SmartRail.Modules.Operations.Application.Disruptions;
using SmartRail.Modules.Operations.Infrastructure.Persistence;
using SmartRail.Application.Core.Abstractions;
using SmartRail.Infrastructure.Core.Audit;
using SmartRail.Infrastructure.Core.Outbox;
using SmartRail.Infrastructure.Core.Persistence;

namespace SmartRail.Modules.Operations.Infrastructure.Extensions;

public static class OperationsModule
{
    public static Assembly ApplicationAssembly => typeof(Application.AssemblyReference).Assembly;

    public static IServiceCollection AddOperationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OperationsDbContext>((sp, options) => options
            .UseNpgsql(
                configuration.GetConnectionString("smartraildb"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", OperationsDbContext.Schema))
            .AddInterceptors(
                sp.GetRequiredService<AuditableEntitiesInterceptor>(),
                sp.GetRequiredService<OutboxMessagesInterceptor>()));

        services.AddScoped<IOperationsUnitOfWork>(sp => sp.GetRequiredService<OperationsDbContext>());
        services.AddScoped<IOutboxMessageProcessor, OutboxMessageProcessor<OperationsDbContext>>();

        services.AddScoped<IDisruptionStore, DisruptionStore>();
        services.AddScoped<IIncidentStore, IncidentStore>();
        services.AddScoped<IMessageStore, MessageStore>();
        services.AddScoped<IAssistanceStore, AssistanceStore>();
        services.AddScoped<IOperationsAuditLog, OperationsAuditLog>();
        services.AddScoped<IStationLookup, StationLookup>();
        services.AddScoped<IAuditSource>(sp => new AuditSource<OperationsDbContext>(sp.GetRequiredService<OperationsDbContext>(), "operations", false));

        return services;
    }
}
