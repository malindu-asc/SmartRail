namespace SmartRail.Modules.Network.Domain.Stations;

// A station name in a non-default language. Key: (StationId, LanguageCode).
public sealed class StationTranslation
{
    private StationTranslation() { }

    internal StationTranslation(Guid stationId, string languageCode, string name)
    {
        StationId = stationId;
        LanguageCode = languageCode;
        Name = name;
    }

    public Guid StationId { get; private set; }
    public string LanguageCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
}
