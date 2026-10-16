using SmartRail.Application.Core.Messaging;

namespace SmartRail.Modules.Network.Application.Routes.GetRoute;

public sealed record GetRouteQuery(string Code, string? Language) : IQuery<RouteResponse>;

public sealed record RouteResponse(string Language, string Code, string Name, IReadOnlyList<RouteStopResponse> Stops);

public sealed record RouteStopResponse(
    int Sequence,
    string StationCode,
    string StationName,
    string? DefaultPlatform,
    int MinutesFromPrevious,
    int DwellMinutes,
    string Kind);
