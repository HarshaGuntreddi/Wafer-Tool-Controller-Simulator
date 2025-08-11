using ControllerService.Api;
using ControllerService.Controller;
using ControllerService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Util;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ControllerState>();
builder.Services.AddSingleton<TelemetryIngestor>();
builder.Services.AddSingleton<CommandSender>();
builder.Services.AddHostedService<ClientConnection>();

var sqlConn = builder.Configuration.GetConnectionString("SqlServer");
builder.Services.AddDbContext<WaferDbContext>(opts =>
{
    if (!string.IsNullOrEmpty(sqlConn))
        opts.UseSqlServer(sqlConn);
    else
        opts.UseSqlite(builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=wafer.db");
});
builder.Services.AddScoped<Repository>();

var httpPort = builder.Configuration.GetValue<int?>("Http:Port") ?? 5080;
builder.WebHost.UseUrls($"http://0.0.0.0:{httpPort}");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WaferDbContext>();
    db.Database.Migrate();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapApiEndpoints();

await app.RunAsync();
