using SmartRail.Application.Core.Messaging;

namespace SmartRail.Modules.Network.Application.Stations.GetStation;

public sealed record GetStationQuery(string Code, string? Language) : IQuery<StationResponse>;

public sealed record StationResponse(
    string Language,
    string Code,
    string Name,
    IReadOnlyList<string> Facilities,
    IReadOnlyList<PlatformResponse> Platforms);

public sealed record PlatformResponse(string Code, bool IsAvailable);
