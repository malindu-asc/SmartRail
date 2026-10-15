using System.Diagnostics.Metrics;
using SmartRail.Infrastructure.Core;

namespace SmartRail.Api.Authentication;

internal static class AuthMetrics
{
    private static readonly Meter Meter = new(SmartRailTelemetry.Name);

    public static readonly Counter<long> Failures =
        Meter.CreateCounter<long>("smartrail.auth.failures", description: "Rejected bearer tokens, by reason");
}
