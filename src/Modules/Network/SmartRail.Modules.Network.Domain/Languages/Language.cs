using SmartRail.SharedKernel.Primitives;

namespace SmartRail.Modules.Network.Domain.Languages;

// Languages are data. Exactly one is the default; its names live on the station itself.
public sealed class Language : Entity<string>
{
    private Language() { }

    public string Name { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }

    public static Language Create(string code, string name, bool isDefault) =>
        new() { Id = code, Name = name, IsDefault = isDefault };
}
