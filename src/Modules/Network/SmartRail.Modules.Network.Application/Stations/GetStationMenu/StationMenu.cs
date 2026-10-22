using SmartRail.Application.Core.Messaging;
using SmartRail.Modules.Network.Application.Abstractions;
using SmartRail.Modules.Network.Domain.Errors;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Network.Application.Stations.GetStationMenu;

// The catering provider's menu, as SmartRail passes it on. SmartRail stores none of it (REQ-INT-001).
public interface ICateringProvider
{
    // Null when the station has no kitchen. Throws CateringUnavailableException when the provider cannot be reached.
    Task<ProviderMenu?> GetMenuAsync(string stationCode, CancellationToken cancellationToken);
}

public sealed class CateringUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

public sealed record ProviderMenu(string StationCode, string Currency, string OrderPhone, IReadOnlyList<ProviderCategory> Categories);

public sealed record ProviderCategory(string Id, IReadOnlyDictionary<string, string> Name, IReadOnlyList<ProviderItem> Items);

public sealed record ProviderItem(string Id, IReadOnlyDictionary<string, string> Name, decimal Price);

public sealed record GetStationMenuQuery(string StationCode, string? Language) : IQuery<MenuResponse>;

public sealed record MenuResponse(string StationCode, string Currency, string OrderPhone, IReadOnlyList<MenuCategoryResponse> Categories);

public sealed record MenuCategoryResponse(string Id, string Name, IReadOnlyList<MenuItemResponse> Items);

public sealed record MenuItemResponse(string Id, string Name, decimal Price);

public static class CateringErrors
{
    public static Error NoMenu(string code) =>
        Error.NotFound("Network.NoMenu", $"Station '{code}' has no food service.");

    public static readonly Error Unavailable =
        Error.Failure("Network.CateringUnavailable", "The menu is not available right now. Trains and tickets are not affected.");
}

public sealed class GetStationMenuQueryHandler(INetworkReader reader, ICateringProvider catering)
    : IQueryHandler<GetStationMenuQuery, MenuResponse>
{
    public async Task<Result<MenuResponse>> Handle(GetStationMenuQuery query, CancellationToken cancellationToken)
    {
        var code = query.StationCode.Trim().ToUpperInvariant();
        if (await reader.GetActiveStationAsync(code, cancellationToken) is null)
            return NetworkErrors.StationNotFound(code);

        ProviderMenu? menu;
        try
        {
            menu = await catering.GetMenuAsync(code, cancellationToken);
        }
        catch (CateringUnavailableException)
        {
            // The provider can be down on its own: the railway carries on, and only this menu says so.
            return CateringErrors.Unavailable;
        }

        if (menu is null)
            return CateringErrors.NoMenu(code);

        var language = string.IsNullOrWhiteSpace(query.Language) ? "en" : query.Language;
        string Pick(IReadOnlyDictionary<string, string> names) =>
            names.TryGetValue(language, out var n) ? n : names.GetValueOrDefault("en", names.Values.FirstOrDefault() ?? string.Empty);

        return new MenuResponse(
            menu.StationCode, menu.Currency, menu.OrderPhone,
            menu.Categories.Select(c => new MenuCategoryResponse(
                c.Id, Pick(c.Name), c.Items.Select(i => new MenuItemResponse(i.Id, Pick(i.Name), i.Price)).ToList())).ToList());
    }
}
