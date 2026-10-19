var builder = DistributedApplication.CreateBuilder(args);

// Local credentials come from the repo-root .env (also used by Docker Compose), so there is one place
// to change them. Without a .env the defaults below apply. Never use these values outside development.
var env = ReadDotEnv(Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../../../.env")));
string Setting(string key, string fallback) => env.TryGetValue(key, out var value) && value.Length > 0 ? value : fallback;

var postgresUser = builder.AddParameter("postgres-user", Setting("POSTGRES_USER", "postgres"));
var postgresPassword = builder.AddParameter("postgres-password", Setting("POSTGRES_PASSWORD", "postgres"), secret: true);
var keycloakUser = builder.AddParameter("keycloak-admin-user", Setting("KEYCLOAK_ADMIN", "admin"));
var keycloakPassword = builder.AddParameter("keycloak-admin-password", Setting("KEYCLOAK_ADMIN_PASSWORD", "admin"), secret: true);

var postgres = builder.AddPostgres("postgres", postgresUser, postgresPassword)
    .WithDataVolume()
    .WithPgAdmin();

var database = postgres.AddDatabase("smartraildb");

// Identity provider. Users, passwords and roles live here; the API only validates tokens.
// The realm (clients, roles, seeded dev users) is imported from infra/keycloak on first start.
var keycloak = builder.AddKeycloak("keycloak", 8080, keycloakUser, keycloakPassword)
    .WithDataVolume()
    .WithRealmImport("../../../infra/keycloak");

// The API validates tokens from Keycloak. Keycloak is reached on the host at its fixed port.
var api = builder.AddProject<Projects.SmartRail_Api>("api")
    .WithReference(database)
    .WithEnvironment("Authentication__Authority", "https://localhost:8080/realms/smartrail")
    .WithEnvironment("Authentication__Audience", "smartrail-api")
    .WithEnvironment("Diagnostics__Identity", "true")
    .WithEnvironment("Database__ApplyMigrationsOnStartup", "true")
    .WithEnvironment("Network__Seed__ReferenceData", "true")
    .WithEnvironment("Journeys__Generation__RunOnStartup", "true")
    .WithEnvironment("Journeys__Seed__DemoFleet", "true")
    // Development feed sources for the simulator (SPEC-005). Never use these keys outside development.
    .WithEnvironment("Identity__FeedSources__Seed__0__Code", "SIM")
    .WithEnvironment("Identity__FeedSources__Seed__0__Name", "Development feed")
    .WithEnvironment("Identity__FeedSources__Seed__0__Key", "dev-feed-key-sim-change-me")
    .WithEnvironment("Identity__FeedSources__Seed__0__Rank", "1")
    .WithEnvironment("Identity__FeedSources__Seed__1__Code", "SIM-B")
    .WithEnvironment("Identity__FeedSources__Seed__1__Name", "Development second feed")
    .WithEnvironment("Identity__FeedSources__Seed__1__Key", "dev-feed-key-simb-change-me")
    .WithEnvironment("Identity__FeedSources__Seed__1__Rank", "2")
    .WaitFor(database)
    .WaitFor(keycloak);

// Optional Grafana stack (collector, Tempo, Loki, Prometheus, Grafana) for a production-like view.
// Enable with:  dotnet run --project src/Host/SmartRail.AppHost -- --Observability:Enabled=true
// When enabled the API exports telemetry to the collector, so traces, logs and metrics appear in
// Grafana (http://localhost:3002) instead of the Aspire dashboard's telemetry pages.
if (string.Equals(builder.Configuration["Observability:Enabled"], "true", StringComparison.OrdinalIgnoreCase))
{
    const string obs = "../../../infra/observability";

    var tempo = builder.AddContainer("tempo", "grafana/tempo", "2.6.1")
        .WithBindMount($"{obs}/tempo.yaml", "/etc/tempo.yaml", isReadOnly: true)
        .WithArgs("-config.file=/etc/tempo.yaml")
        .WithHttpEndpoint(targetPort: 3200, name: "http");

    var loki = builder.AddContainer("loki", "grafana/loki", "3.3.0")
        .WithBindMount($"{obs}/loki.yaml", "/etc/loki/config.yaml", isReadOnly: true)
        .WithArgs("-config.file=/etc/loki/config.yaml")
        .WithHttpEndpoint(targetPort: 3100, name: "http");

    var collector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib", "0.116.1")
        .WithBindMount($"{obs}/otel-collector.yaml", "/etc/otelcol/config.yaml", isReadOnly: true)
        .WithArgs("--config=/etc/otelcol/config.yaml")
        .WithEndpoint(port: 4317, targetPort: 4317, scheme: "http", name: "otlp-grpc")
        .WaitFor(tempo)
        .WaitFor(loki);

    var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "v2.55.1")
        .WithBindMount($"{obs}/prometheus.yml", "/etc/prometheus/prometheus.yml", isReadOnly: true)
        .WithBindMount($"{obs}/alerts.yml", "/etc/prometheus/alerts.yml", isReadOnly: true)
        .WithHttpEndpoint(port: 9090, targetPort: 9090, name: "http")
        .WaitFor(collector);

    builder.AddContainer("grafana", "grafana/grafana", "11.3.1")
        .WithBindMount($"{obs}/grafana/provisioning", "/etc/grafana/provisioning", isReadOnly: true)
        .WithBindMount($"{obs}/grafana/dashboards", "/var/lib/grafana/dashboards", isReadOnly: true)
        .WithEnvironment("GF_SECURITY_ADMIN_USER", Setting("GRAFANA_ADMIN_USER", "admin"))
        .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", Setting("GRAFANA_ADMIN_PASSWORD", "admin"))
        .WithEnvironment("GF_USERS_ALLOW_SIGN_UP", "false")
        .WithHttpEndpoint(port: 3002, targetPort: 3000, name: "http")
        .WaitFor(prometheus);

    // The API runs on the host, so it reaches the collector through the published port.
    api.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317")
       .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")
       .WaitFor(collector);
}

builder.AddProject<Projects.SmartRail_Simulator>("simulator")
    .WithReference(api)
    .WaitFor(api);

// External catering platform: separate runtime, separate data, reached only over HTTP.
builder.AddNodeApp("catering", "../../../services/catering", "src/index.ts")
    .WithHttpEndpoint(env: "PORT")
    .WithReference(api);

builder.AddViteApp("console", "../../../apps/web/console")
    .WithReference(api)
    .WithReference(keycloak)
    .WaitFor(api);

builder.AddViteApp("kiosk", "../../../apps/web/kiosk")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();

static Dictionary<string, string> ReadDotEnv(string path)
{
    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    if (!File.Exists(path))
        return values;

    foreach (var raw in File.ReadAllLines(path))
    {
        var line = raw.Trim();
        if (line.Length == 0 || line.StartsWith('#'))
            continue;

        var index = line.IndexOf('=');
        if (index <= 0)
            continue;

        values[line[..index].Trim()] = line[(index + 1)..].Trim().Trim('"');
    }

    return values;
}
