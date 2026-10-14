var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var database = postgres.AddDatabase("smartraildb");

// Identity provider. Users, passwords and roles live here; the API only validates tokens.
// The realm (clients, roles, seeded dev users) is imported from infra/keycloak on first start.
var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume()
    .WithRealmImport("../../../infra/keycloak");

var api = builder.AddProject<Projects.SmartRail_Api>("api")
    .WithReference(database)
    .WithReference(keycloak)
    .WaitFor(database)
    .WaitFor(keycloak);

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
