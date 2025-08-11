using ControllerService.Api;
using ControllerService.Controller;
using Shared.Util;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ControllerState>();
builder.Services.AddSingleton<TelemetryIngestor>();
builder.Services.AddSingleton<CommandSender>();
builder.Services.AddHostedService<ClientConnection>();

var httpPort = builder.Configuration.GetValue<int?>("Http:Port") ?? 5080;
builder.WebHost.UseUrls($"http://0.0.0.0:{httpPort}");

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapApiEndpoints();

await app.RunAsync();
