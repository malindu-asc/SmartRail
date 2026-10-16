using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Network.Application.Abstractions;
using SmartRail.Modules.Network.Application.Languages;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Network.Application.Stations.GetStations;

public sealed class GetStationsQueryHandler(INetworkReader reader)
    : IQueryHandler<GetStationsQuery, StationListResponse>
{
    public async Task<Result<StationListResponse>> Handle(GetStationsQuery query, CancellationToken cancellationToken)
    {
        var languages = await reader.GetLanguagesAsync(cancellationToken);
        var language = LanguageResolver.Resolve(query.Language, languages);
        var stations = await reader.GetActiveStationsAsync(cancellationToken);

        var term = query.Search?.Trim();
        var items = stations
            .Select(s => new { Station = s, Name = LanguageResolver.Localize(s.Name, s.Translations, language) })
            .Where(x => string.IsNullOrEmpty(term)
                        || x.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || x.Station.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || x.Station.Code.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Name, StringComparer.Create(System.Globalization.CultureInfo.InvariantCulture, ignoreCase: true))
            .Select(x => new StationListItem(x.Station.Code, x.Name, x.Station.Facilities))
            .ToList();

        return new StationListResponse(language, items);
    }
}
