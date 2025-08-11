using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ToolSimulator.Simulator;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddSingleton<FaultInjection>(sp =>
    builder.Configuration.GetSection("FaultInjection").Get<FaultInjection>() ?? new());
builder.Services.AddSingleton<ToolCore>();
builder.Services.AddHostedService<ToolServer>();
builder.Logging.AddConsole();

using var host = builder.Build();

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

await host.RunAsync(cts.Token);
