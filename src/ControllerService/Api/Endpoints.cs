using ControllerService.Controller;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Shared.Protocol;

namespace ControllerService.Api;

public static class Endpoints
{
    public static void MapApiEndpoints(this WebApplication app)
    {
        app.MapGet("/api/status", (ControllerState state) =>
            new StatusDto(state.Connected, state.LastHeartbeatUtc, state.CurrentState));

        app.MapPost("/api/run/start", async (CommandSender sender, CancellationToken ct) =>
        {
            await sender.SendAsync(Command.LOAD, ct);
            await sender.SendAsync(Command.START, ct);
            return Results.Ok();
        });

        app.MapPost("/api/run/stop", async (CommandSender sender, CancellationToken ct) =>
        {
            await sender.SendAsync(Command.STOP, ct);
            await sender.SendAsync(Command.UNLOAD, ct);
            return Results.Ok();
        });

        app.MapGet("/api/telemetry/latest", (ControllerState state, int? limit) =>
        {
            var items = state.GetTelemetryLatest(limit ?? 100)
                .Select(t => new TelemetryDto(t.Timestamp, t.Key, t.Value));
            return Results.Ok(items);
        });

        app.MapGet("/api/stream", async (ControllerState state, HttpResponse response, CancellationToken ct) =>
        {
            response.Headers.Add("Content-Type", "text/event-stream");
            await foreach (var json in state.StreamAsync(ct))
            {
                await response.WriteAsync($"data: {json}\n\n", ct);
                await response.Body.FlushAsync(ct);
            }
        });
    }
}
