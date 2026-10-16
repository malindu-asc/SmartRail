using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartRail.Modules.Journeys.Application.Generation;
using SmartRail.Modules.Journeys.Infrastructure.Seeding;
using SmartRail.Modules.Network.Contracts;

namespace SmartRail.Modules.Journeys.Infrastructure.Generation;

// Configuration section "Journeys:Generation".
public sealed class JourneyGenerationOptions
{
    public const string SectionName = "Journeys:Generation";

    public bool Enabled { get; set; } = true;               // the daily job (turned off in tests)
    public bool RunOnStartup { get; set; }                  // also run once when the application starts
    public string RunAtLocalTime { get; set; } = "01:00";   // network time zone
    public int HorizonDays { get; set; } = 7;               // ASM-026
}

// One code path for the daily job, the start-up run and the tests (decision D5).
public sealed class JourneyGenerationRunner(
    ISender sender,
    INetworkTimeZone timeZone,
    DemoFleetSeeder demoFleet,
    IConfiguration configuration,
    IOptions<JourneyGenerationOptions> options,
    ILogger<JourneyGenerationRunner> logger)
{
    public async Task<GenerationResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var from = timeZone.Today();
        var days = Math.Max(1, options.Value.HorizonDays);

        var result = await sender.Send(new GenerateJourneysCommand(from, days), cancellationToken);
        if (result.IsFailure)
            throw new InvalidOperationException($"Journey generation failed: {result.Error.Code}");

        logger.LogInformation(
            "Journey generation finished: {Created} created, {Skipped} already existed, {Failed} failed, {Empty} dates without services",
            result.Value.Created, result.Value.Skipped, result.Value.Failed, result.Value.DatesWithoutServices.Count);

        // Demo fleet and roster (flag): every journey without an assignment gets one and becomes Ready.
        if (configuration.GetValue<bool>("Journeys:Seed:DemoFleet"))
            await demoFleet.SeedAsync(Enumerable.Range(0, days).Select(i => from.AddDays(i)).ToList(), cancellationToken);

        return result.Value;
    }
}

// Runs the generation once a day at the configured local time, and once at start-up when asked to.
public sealed class JourneyGenerationWorker(
    IServiceScopeFactory scopes,
    INetworkTimeZone timeZone,
    TimeProvider clock,
    IOptions<JourneyGenerationOptions> options,
    ILogger<JourneyGenerationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
            return;

        if (options.Value.RunOnStartup)
            await RunOnceAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = DelayUntilNextRun();
            logger.LogInformation("Next journey generation in {Delay}", delay);

            try
            {
                await Task.Delay(delay, clock, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        // A concurrent run on another instance is harmless (unique index); try once more after a conflict.
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<JourneyGenerationRunner>().RunAsync(cancellationToken);
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (DbUpdateException ex) when (attempt == 1)
            {
                logger.LogWarning(ex, "Journey generation hit a conflict; retrying once");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Journey generation failed");
                return;
            }
        }
    }

    private TimeSpan DelayUntilNextRun()
    {
        var runAt = TimeOnly.TryParse(options.Value.RunAtLocalTime, out var parsed) ? parsed : new TimeOnly(1, 0);
        var nowLocal = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), timeZone.Zone).DateTime;

        var next = nowLocal.Date.Add(runAt.ToTimeSpan());
        if (next <= nowLocal)
            next = next.AddDays(1);

        return next - nowLocal;
    }
}
