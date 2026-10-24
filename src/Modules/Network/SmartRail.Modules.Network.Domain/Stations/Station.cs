using System.Text.RegularExpressions;
using SmartRail.Modules.Network.Domain.Errors;
using SmartRail.SharedKernel.Primitives;
using SmartRail.SharedKernel.Results;

namespace SmartRail.Modules.Network.Domain.Stations;

public sealed partial class Station : AuditableEntity
{
    private readonly List<Platform> _platforms = [];
    private readonly List<StationTranslation> _translations = [];
    private string[] _facilities = [];

    private Station() { }

    private Station(Guid id) : base(id) { }

    public string Code { get; private set; } = string.Empty;

    // The default-language name. Other languages are in Translations.
    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public IReadOnlyList<string> Facilities => _facilities;
    public IReadOnlyList<Platform> Platforms => _platforms;
    public IReadOnlyList<StationTranslation> Translations => _translations;

    public static Result<Station> Create(string code, string name, IEnumerable<string> facilities, Guid? id = null)
    {
        if (!StationCodePattern().IsMatch(code))
            return NetworkErrors.InvalidStationCode(code);

        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            return NetworkErrors.InvalidName;

        var distinct = facilities.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        foreach (var facility in distinct)
        {
            if (!FacilityCodes.All.Contains(facility))
                return NetworkErrors.UnknownFacility(facility);
        }

        return new Station(id ?? Guid.NewGuid())
        {
            Code = code,
            Name = name.Trim(),
            _facilities = distinct
        };
    }

    public Result<Platform> AddPlatform(string code, bool isAvailable = true, Guid? id = null)
    {
        if (!PlatformCodePattern().IsMatch(code))
            return NetworkErrors.InvalidPlatformCode(code);

        if (_platforms.Any(p => string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase)))
            return NetworkErrors.DuplicatePlatform(code);

        var platform = new Platform(id ?? Guid.NewGuid(), Id, code, isAvailable);
        _platforms.Add(platform);
        return platform;
    }

    public Result AddTranslation(string languageCode, string name, string defaultLanguageCode)
    {
        if (string.Equals(languageCode, defaultLanguageCode, StringComparison.OrdinalIgnoreCase))
            return NetworkErrors.DefaultLanguageTranslation(languageCode);

        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            return NetworkErrors.InvalidName;

        if (_translations.Any(t => string.Equals(t.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase)))
            return NetworkErrors.DuplicateTranslation(languageCode);

        _translations.Add(new StationTranslation(Id, languageCode, name.Trim()));
        return Result.Success();
    }

    [GeneratedRegex("^[A-Z]{3,5}$")]
    private static partial Regex StationCodePattern();

    [GeneratedRegex("^[A-Za-z0-9]{1,3}$")]
    private static partial Regex PlatformCodePattern();
}
