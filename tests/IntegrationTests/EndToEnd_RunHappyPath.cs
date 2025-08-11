using System.Net;
using System.Net.Http.Json;
using ControllerService.Api;
using ControllerService.Controller;
using ControllerService.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Shared.Domain;
using Shared.Util;
using ToolSimulator.Simulator;

namespace IntegrationTests;

public class EndToEnd_RunHappyPath
{
    [Fact]
    public async Task Run_happy_path()
    {
        var env = await TestEnvironment.StartAsync();
        try
        {
            var start = await env.Client.PostAsync("/api/run/start", null);
            start.EnsureSuccessStatusCode();
            await env.WaitForState(ToolState.PROCESSING);

            var stop = await env.Client.PostAsync("/api/run/stop", null);
            stop.EnsureSuccessStatusCode();
            await env.WaitForState(ToolState.IDLE);

            using var db = new WaferDbContext(new DbContextOptionsBuilder<WaferDbContext>().UseSqlite($"Data Source={env.DbPath}").Options);
            var count = await db.Telemetry.CountAsync();
            count.Should().BeGreaterThan(10);
        }
        finally
        {
            await env.DisposeAsync();
        }
    }
}

internal static class TestEnvironment
{
    public static async Task<TestEnv> StartAsync(double drop = 0)
    {
        int simPort = GetFreePort();
        var simBuilder = Host.CreateApplicationBuilder();
        simBuilder.Configuration["Port"] = simPort.ToString();
        simBuilder.Services.AddSingleton(new FaultInjection { DropPercentage = drop });
        simBuilder.Services.AddSingleton<ToolCore>();
        simBuilder.Services.AddHostedService<ToolServer>();
        var simHost = simBuilder.Build();
        await simHost.StartAsync();

        var dbPath = Path.GetTempFileName();
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["Simulator:Host"] = "localhost";
        builder.Configuration["Simulator:Port"] = simPort.ToString();
        builder.Configuration["ConnectionStrings:Sqlite"] = $"Data Source={dbPath}";
        builder.Services.AddSingleton<ControllerState>();
        builder.Services.AddSingleton<TelemetryIngestor>();
        builder.Services.AddSingleton<CommandSender>();
        builder.Services.AddHostedService<ClientConnection>();
        builder.Services.AddDbContext<WaferDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Sqlite")));
        builder.Services.AddScoped<Repository>();
        builder.WebHost.UseUrls("http://localhost:0");
        var app = builder.Build();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WaferDbContext>();
            db.Database.Migrate();
        }
        app.MapApiEndpoints();
        await app.StartAsync();
        var baseAddr = app.Urls.First();
        var client = new HttpClient { BaseAddress = new Uri(baseAddr) };
        return new TestEnv(simHost, app, client, dbPath);
    }

    private static int GetFreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }
}

internal sealed class TestEnv : IAsyncDisposable
{
    public IHost SimHost { get; }
    public WebApplication App { get; }
    public HttpClient Client { get; }
    public string DbPath { get; }

    public TestEnv(IHost simHost, WebApplication app, HttpClient client, string dbPath)
    {
        SimHost = simHost;
        App = app;
        Client = client;
        DbPath = dbPath;
    }

    public async Task WaitForState(ToolState state)
    {
        for (var i = 0; i < 40; i++)
        {
            var status = await Client.GetFromJsonAsync<StatusDto>("/api/status", Json.Options);
            if (status?.State == state)
                return;
            await Task.Delay(250);
        }
        throw new TimeoutException("State not reached");
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await App.StopAsync();
        await SimHost.StopAsync();
        App.Dispose();
        SimHost.Dispose();
        File.Delete(DbPath);
    }
}
