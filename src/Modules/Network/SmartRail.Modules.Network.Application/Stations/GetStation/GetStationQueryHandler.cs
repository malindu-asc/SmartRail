using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Network.Application.Abstractions;
using SmartRail.Modules.Network.Application.Languages;
using SmartRail.Modules.Network.Domain.Errors;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Network.Application.Stations.GetStation;

public sealed class GetStationQueryHandler(INetworkReader reader)
    : IQueryHandler<GetStationQuery, StationResponse>
{
    public async Task<Result<StationResponse>> Handle(GetStationQuery query, CancellationToken cancellationToken)
    {
        var station = await reader.GetActiveStationAsync(query.Code, cancellationToken);
        if (station is null)
            return NetworkErrors.StationNotFound(query.Code);

        var languages = await reader.GetLanguagesAsync(cancellationToken);
        var language = LanguageResolver.Resolve(query.Language, languages);

        return new StationResponse(
            language,
            station.Code,
            LanguageResolver.Localize(station.Name, station.Translations, language),
            station.Facilities,
            station.Platforms.Select(p => new PlatformResponse(p.Code, p.IsAvailable)).ToList());
    }
}
