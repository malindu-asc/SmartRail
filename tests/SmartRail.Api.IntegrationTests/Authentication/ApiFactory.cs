using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace SmartRail.Api.IntegrationTests.Authentication;

public class ApiFactory : WebApplicationFactory<Program>
{
    public TestTokenIssuer Tokens { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = TestTokenIssuer.Issuer,
                ["Authentication:Audience"] = TestTokenIssuer.Audience,
                ["Authentication:RequireHttpsMetadata"] = "false",
                ["Authentication:ClockSkewSeconds"] = "30",
                ["Diagnostics:Identity"] = "true",
                ["Outbox:Enabled"] = "false",
                ["Journeys:Generation:Enabled"] = "false",
                ["Database:ApplyMigrationsOnStartup"] = "false",
                // Never opened by these tests; present so the DbContexts can be constructed.
                ["ConnectionStrings:smartraildb"] = "Host=localhost;Port=1;Database=unused;Username=unused;Password=unused"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Use the test key instead of fetching keys from a real identity provider.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var configuration = new OpenIdConnectConfiguration { Issuer = TestTokenIssuer.Issuer };
                configuration.SigningKeys.Add(Tokens.SigningKey);
                options.Configuration = configuration;
                options.TokenValidationParameters.IssuerSigningKey = Tokens.SigningKey;
            });
        });
    }

    public HttpClient CreateAuthenticatedClient(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }
}
