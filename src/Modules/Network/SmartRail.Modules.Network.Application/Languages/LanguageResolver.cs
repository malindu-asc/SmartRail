using SmartRail.Modules.Network.Application.Abstractions;

namespace SmartRail.Modules.Network.Application.Languages;

internal static class LanguageResolver
{
    // The requested language if it exists, otherwise the default one.
    internal static string Resolve(string? requested, IReadOnlyList<LanguageReadModel> languages)
    {
        var match = string.IsNullOrWhiteSpace(requested)
            ? null
            : languages.FirstOrDefault(l => string.Equals(l.Code, requested.Trim(), StringComparison.OrdinalIgnoreCase));

        return (match ?? languages.FirstOrDefault(l => l.IsDefault) ?? languages.FirstOrDefault())?.Code ?? "en";
    }

    internal static string Localize(string defaultName, IReadOnlyDictionary<string, string> translations, string language) =>
        translations.TryGetValue(language, out var translated) ? translated : defaultName;
}
