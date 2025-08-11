using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Protocol;
using Shared.Util;

namespace ControllerService.Controller;

public class ClientConnection : BackgroundService
{
    private readonly ILogger<ClientConnection> _logger;
    private readonly IConfiguration _config;
    private readonly TelemetryIngestor _ingestor;
    private readonly ControllerState _state;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> _acks = new();
    private TcpClient? _client;
    private NetworkStream? _stream;

    public ClientConnection(ILogger<ClientConnection> logger, IConfiguration config, TelemetryIngestor ingestor, ControllerState state)
    {
        _logger = logger;
        _config = config;
        _ingestor = ingestor;
        _state = state;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host = _config.GetValue<string>("Simulator:Host") ?? "localhost";
        var port = _config.GetValue<int>("Simulator:Port");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RetryPolicy.ExecuteAsync(async () =>
                {
                    _client = new TcpClient();
                    await _client.ConnectAsync(host, port, stoppingToken);
                    _stream = _client.GetStream();
                }, int.MaxValue, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), stoppingToken);

                _state.UpdateConnection(true);
                await SendAsync(new Envelope(Guid.NewGuid(), Command.HELLO.ToString(), DateTime.UtcNow), stoppingToken);

                var readTask = ReadLoop(_stream, stoppingToken);
                var hbTask = HeartbeatWatchdog(stoppingToken);

                await Task.WhenAny(readTask, hbTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection error");
            }
            finally
            {
                _state.UpdateConnection(false);
                _client?.Close();
                _client = null;
                _stream = null;
            }
        }
    }

    private async Task ReadLoop(NetworkStream stream, CancellationToken ct)
    {
        await foreach (var element in Shared.Protocol.Framing.ReadAsync<JsonElement>(stream, Json.Options, ct))
        {
            if (element.ValueKind != JsonValueKind.Object) continue;

            if (element.TryGetProperty("ts", out var ts) && element.EnumerateObject().Count() == 1)
            {
                _state.UpdateHeartbeat(ts.GetDateTime());
            }
            else if (element.TryGetProperty("metric", out _))
            {
                var telemetry = element.Deserialize<Telemetry>(Json.Options);
                if (telemetry != null)
                    _ingestor.IngestTelemetry(telemetry);
            }
            else if (element.TryGetProperty("name", out _))
            {
                var evt = element.Deserialize<Event>(Json.Options);
                if (evt != null)
                    _ingestor.IngestEvent(evt);
            }
            else if (element.TryGetProperty("id", out var idProp) && element.EnumerateObject().Count() == 1)
            {
                var id = idProp.GetGuid();
                if (_acks.TryRemove(id, out var tcs))
                    tcs.TrySetResult(true);
            }
        }
    }

    private async Task HeartbeatWatchdog(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            var last = _state.LastHeartbeatUtc;
            if (last.HasValue && DateTime.UtcNow - last > TimeSpan.FromSeconds(5))
                break;
        }
    }

    public async Task SendAsync<T>(T message, CancellationToken ct)
    {
        if (_stream != null)
            await Shared.Protocol.Framing.WriteAsync(_stream, message, Json.Options, ct);
    }

    public TaskCompletionSource<bool> RegisterAck(Guid id)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _acks[id] = tcs;
        return tcs;
    }
}
