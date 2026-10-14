using Scalar.AspNetCore;
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

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddNetworkModule(builder.Configuration);
builder.Services.AddJourneysModule(builder.Configuration);
builder.Services.AddOperationsModule(builder.Configuration);
builder.Services.AddNotificationsModule(builder.Configuration);

// Once, for every module: MediatR handlers, validators, pipeline behaviours, integration-event handlers.
builder.Services.AddApplicationCore(
    IdentityModule.ApplicationAssembly,
    NetworkModule.ApplicationAssembly,
    JourneysModule.ApplicationAssembly,
    OperationsModule.ApplicationAssembly,
    NotificationsModule.ApplicationAssembly);

builder.Services.AddInfrastructureCore(builder.Configuration);
builder.Services.AddApiCore();
builder.Services
    .AddAuthentication()
    .AddKeycloakJwtBearer("keycloak", realm: "smartrail", options =>
    {
        options.Audience = "smartrail-api";
        options.RequireHttpsMetadata = false; // development container only
        options.TokenValidationParameters.RoleClaimType = "roles";
        options.TokenValidationParameters.NameClaimType = "preferred_username";
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    await app.ApplyMigrationsAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapEndpoints();

app.Run();
