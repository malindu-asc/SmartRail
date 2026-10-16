using SmartRail.Infrastructure.Core.Audit;
using SmartRail.Modules.Journeys.Application.Abstractions;

namespace SmartRail.Modules.Journeys.Infrastructure.Persistence;

// Adds audit rows to the Journeys context, so they are saved together with the change.
internal sealed class JourneysAuditLog(JourneysDbContext context, TimeProvider clock)
    : AuditLogWriter(context, clock), IJourneysAuditLog;
