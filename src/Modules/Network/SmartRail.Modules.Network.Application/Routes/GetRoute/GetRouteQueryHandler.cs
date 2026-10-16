using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Network.Application.Abstractions;
using SmartRail.Modules.Network.Application.Languages;
using SmartRail.Modules.Network.Domain.Errors;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Network.Application.Routes.GetRoute;

public sealed class GetRouteQueryHandler(INetworkReader reader)
    : IQueryHandler<GetRouteQuery, RouteResponse>
{
    public async Task<Result<RouteResponse>> Handle(GetRouteQuery query, CancellationToken cancellationToken)
    {
        var route = await reader.GetActiveRouteAsync(query.Code, cancellationToken);
        if (route is null)
            return NetworkErrors.RouteNotFound(query.Code);

        var languages = await reader.GetLanguagesAsync(cancellationToken);
        var language = LanguageResolver.Resolve(query.Language, languages);

        // Public view: passing points are removed and the sequence is renumbered. The running time
        // of a removed passing point is added to the next visible stop so the total stays correct.
        var stops = new List<RouteStopResponse>();
        var carriedMinutes = 0;
        foreach (var stop in route.Stops.OrderBy(s => s.Sequence))
        {
            if (stop.Kind == "Passing")
            {
                carriedMinutes += stop.MinutesFromPrevious + stop.DwellMinutes;
                continue;
            }

            stops.Add(new RouteStopResponse(
                stops.Count + 1,
                stop.StationCode,
                LanguageResolver.Localize(stop.StationName, stop.StationTranslations, language),
                stop.DefaultPlatform,
                stops.Count == 0 ? 0 : stop.MinutesFromPrevious + carriedMinutes,
                stop.DwellMinutes,
                stop.Kind));
            carriedMinutes = 0;
        }

        return new RouteResponse(language, route.Code, route.Name, stops);
    }
}
