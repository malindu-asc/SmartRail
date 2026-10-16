using SmartRail.Application.Core.Messaging;

namespace SmartRail.Modules.Network.Application.Routes.GetRoutes;

public sealed record GetRoutesQuery : IQuery<RouteListResponse>;

public sealed record RouteListResponse(IReadOnlyList<RouteListItem> Items);

// StopCount counts the stops passengers can use; passing points are not counted.
public sealed record RouteListItem(string Code, string Name, int StopCount);
