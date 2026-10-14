using Microsoft.EntityFrameworkCore;
using SmartRail.Modules.Notifications.Application.Abstractions;
using SmartRail.Infrastructure.Core.Persistence;

namespace SmartRail.Modules.Notifications.Infrastructure.Persistence;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
    : BaseDbContext(options), INotificationsUnitOfWork
{
    public const string Schema = "notifications";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
    }
}
