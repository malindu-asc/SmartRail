using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Infrastructure.Core.Outbox;
using SmartRail.Infrastructure.Core.Persistence;
using SmartRail.Modules.Identity.Contracts;
using SmartRail.Modules.Ticketing.Application.Abstractions;
using SmartRail.Modules.Ticketing.Application.Tickets;
using SmartRail.Modules.Ticketing.Domain.Bookings;
using SmartRail.Modules.Ticketing.Infrastructure.Persistence;

namespace SmartRail.Modules.Ticketing.Infrastructure.Extensions;

public static class TicketingModule
{
    public static Assembly ApplicationAssembly => typeof(Application.AssemblyReference).Assembly;

    public static IServiceCollection AddTicketingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TicketingDbContext>((sp, options) => options
            .UseNpgsql(
                configuration.GetConnectionString("smartraildb"),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", TicketingDbContext.Schema))
            .AddInterceptors(
                sp.GetRequiredService<AuditableEntitiesInterceptor>(),
                sp.GetRequiredService<OutboxMessagesInterceptor>()));

        services.AddScoped<ITicketingUnitOfWork>(sp => sp.GetRequiredService<TicketingDbContext>());
        services.AddScoped<IOutboxMessageProcessor, OutboxMessageProcessor<TicketingDbContext>>();

        services.AddScoped<IBookingStore, BookingStore>();
        services.AddScoped<SmartRail.Modules.Ticketing.Contracts.ITicketImpact, TicketImpact>();
        services.AddScoped<TicketViewer>();
        services.AddScoped<IPersonalDataContributor, TicketsPersonalData>();

        // Ticketing:SeatReservedRoutes, Ticketing:SeatHoldMinutes, Ticketing:Fares and Ticketing:Signing.
        services.AddSingleton(configuration.GetSection(TicketingOptions.Section).Get<TicketingOptions>() ?? new TicketingOptions());
        services.AddSingleton(configuration.GetSection(FareOptions.Section).Get<FareOptions>() ?? new FareOptions());
        services.Configure<TicketSigningOptions>(configuration.GetSection(TicketSigningOptions.Section));
        services.AddSingleton<ITicketSigner, HmacTicketSigner>();

        return services;
    }
}
