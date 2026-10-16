using Microsoft.Extensions.Options;
using SmartRail.Modules.Network.Application.Abstractions;

namespace SmartRail.Modules.Network.Infrastructure.Settings;

// Configuration section "Network". The time zone is a demo setting (ASM-017: a single zone).
public sealed class NetworkOptions
{
    public const string SectionName = "Network";

    public string TimeZone { get; set; } = "Asia/Colombo";
}

// Fails the start of the application when the configured zone does not exist.
public sealed class NetworkOptionsValidator : IValidateOptions<NetworkOptions>
{
    public ValidateOptionsResult Validate(string? name, NetworkOptions options)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(options.TimeZone);
            return ValidateOptionsResult.Success;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return ValidateOptionsResult.Fail($"Network:TimeZone '{options.TimeZone}' is not a known time zone id.");
        }
    }
}

internal sealed class NetworkSettings(IOptions<NetworkOptions> options, TimeProvider clock) : INetworkSettings
{
    private readonly TimeZoneInfo _zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

    public string TimeZoneId => options.Value.TimeZone;

    public DateOnly Today() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), _zone).DateTime);
}
