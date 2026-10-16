using SmartRail.Application.Core.Messaging;

namespace SmartRail.Modules.Network.Application.Stations.GetStations;

public sealed record GetStationsQuery(string? Search, string? Language) : IQuery<StationListResponse>;

public sealed record StationListResponse(string Language, IReadOnlyList<StationListItem> Items);

public sealed record StationListItem(string Code, string Name, IReadOnlyList<string> Facilities);
