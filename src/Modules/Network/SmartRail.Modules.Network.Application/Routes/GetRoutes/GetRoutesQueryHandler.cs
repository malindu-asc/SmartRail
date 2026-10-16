using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Network.Application.Abstractions;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Network.Application.Routes.GetRoutes;

public sealed class GetRoutesQueryHandler(INetworkReader reader)
    : IQueryHandler<GetRoutesQuery, RouteListResponse>
{
    public async Task<Result<RouteListResponse>> Handle(GetRoutesQuery query, CancellationToken cancellationToken)
    {
        var routes = await reader.GetActiveRoutesAsync(cancellationToken);

        var items = routes
            .OrderBy(r => r.Code, StringComparer.Ordinal)
            .Select(r => new RouteListItem(r.Code, r.Name, r.Stops.Count(s => s.Kind != "Passing")))
            .ToList();

        return new RouteListResponse(items);
    }
}
