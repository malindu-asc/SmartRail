using SmartRail.Simulator;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// The API is resolved through Aspire service discovery ("https+http://api").
builder.Services.AddHttpClient<IngestionClient>(client => client.BaseAddress = new Uri("https+http://api"));
builder.Services.AddHostedService<SimulatorWorker>();

builder.Build().Run();
