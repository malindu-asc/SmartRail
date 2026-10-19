using Microsoft.OpenApi;
using Scalar.AspNetCore;
using SmartRail.Api.Authentication;
using SmartRail.Api.Development;
using SmartRail.Api.Core;
using SmartRail.Api.Extensions;
using SmartRail.Application.Core;
using SmartRail.Application.Core.Abstractions;
using SmartRail.Infrastructure.Core;
using SmartRail.Modules.Identity.Infrastructure.Extensions;
using SmartRail.Modules.Journeys.Infrastructure.Extensions;
using SmartRail.Modules.Network.Infrastructure.Extensions;
using SmartRail.Modules.Notifications.Infrastructure.Extensions;
using SmartRail.Modules.Operations.Infrastructure.Extensions;
using SmartRail.Modules.Ticketing.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddNetworkModule(builder.Configuration);
builder.Services.AddJourneysModule(builder.Configuration);
builder.Services.AddOperationsModule(builder.Configuration);
builder.Services.AddNotificationsModule(builder.Configuration);
builder.Services.AddTicketingModule(builder.Configuration);

// Once, for every module: MediatR handlers, validators, pipeline behaviours, integration-event handlers.
builder.Services.AddApplicationCore(
    IdentityModule.ApplicationAssembly,
    NetworkModule.ApplicationAssembly,
    JourneysModule.ApplicationAssembly,
    OperationsModule.ApplicationAssembly,
    NotificationsModule.ApplicationAssembly,
    TicketingModule.ApplicationAssembly);

builder.Services.AddInfrastructureCore(builder.Configuration);
builder.Services.AddApiCore();
builder.Services.AddSmartRailAuthentication();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddOpenApi(options =>
{
    // Lets the API docs sign in through Keycloak (authorization code with PKCE) and call endpoints.
    options.AddDocumentTransformer((document, context, _) =>
    {
        var authority = context.ApplicationServices.GetRequiredService<IConfiguration>()["Authentication:Authority"];
        if (string.IsNullOrWhiteSpace(authority))
            return Task.CompletedTask;

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["OAuth2"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{authority.TrimEnd('/')}/protocol/openid-connect/auth"),
                    TokenUrl = new Uri($"{authority.TrimEnd('/')}/protocol/openid-connect/token"),
                    Scopes = new Dictionary<string, string> { ["openid"] = "OpenID Connect" }
                }
            }
        };
        return Task.CompletedTask;
    });
});

// Off by default. Development scenarios: trains that are late, cancelled or silent, tickets and notices to match (see DemoScenarios.cs).
var demoScenarios = builder.Configuration.GetValue<bool>("Demo:Scenarios:Enabled");
if (demoScenarios)
{
    builder.Services.AddScoped<DemoScenarioSeeder>();
    builder.Services.AddHostedService<DemoScenarioWorker>();
}

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference(options => options
        .AddPreferredSecuritySchemes("OAuth2")
        .AddAuthorizationCodeFlow("OAuth2", flow =>
        {
            flow.ClientId = "smartrail-web";
            flow.Pkce = Pkce.Sha256;
            flow.SelectedScopes = ["openid"];
        })).AllowAnonymous();
}

// Off by default. Aspire and Docker Compose turn it on; other environments migrate as an explicit step.
if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await app.ApplyMigrationsAsync();
}

// Off by default. Inserts the fictional demo network only where it is missing; never updates existing rows.
if (app.Configuration.GetValue<bool>("Network:Seed:ReferenceData"))
{
    await app.Services.SeedNetworkReferenceDataAsync();
}

// Inserts the configured development feed sources (Identity:FeedSources:Seed) where they are missing.
if (app.Configuration.GetSection("Identity:FeedSources:Seed").GetChildren().Any())
{
    await app.SeedFeedSourcesAsync();
}

// Applies the scenarios again, for example after a new day's trains were generated.
if (demoScenarios)
{
    app.MapPost("/api/dev/scenarios/apply", async (DemoScenarioSeeder seeder, CancellationToken ct) =>
        Results.Ok(await seeder.ApplyAsync(ct))).AllowAnonymous();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapEndpoints();

app.Run();

// Lets the integration test project host the API in memory.
public partial class Program;
