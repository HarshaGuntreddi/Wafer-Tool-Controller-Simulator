using System.Net;
using System.Net.Sockets;
using Shared.Protocol;
using Shared.Util;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ToolSimulator.Simulator;

public class ToolServer : BackgroundService
{
    private readonly ILogger<ToolServer> _logger;
    private readonly ToolCore _core;
    private readonly FaultInjection _fault;
    private readonly int _port;

    public ToolServer(ILogger<ToolServer> logger, IConfiguration config, ToolCore core, FaultInjection fault)
    {
        _logger = logger;
        _core = core;
        _fault = fault;
        _port = config.GetValue<int>("Port");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        _logger.LogInformation("ToolServer listening on {Port}", _port);

        using var client = await listener.AcceptTcpClientAsync(stoppingToken);
        _logger.LogInformation("Client connected");

        using var stream = client.GetStream();

        _ = SendHeartbeats(stream, stoppingToken);

        await foreach (var envelope in Framing.ReadAsync<Envelope>(stream, Json.Options, stoppingToken))
        {
            if (envelope != null)
                await _core.HandleAsync(envelope, stream, stoppingToken);
        }
    }

    private async Task SendHeartbeats(NetworkStream stream, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), token);
            var hb = new Heartbeat(DateTime.UtcNow);
            await SendAsync(stream, hb, token);
        }
    }

    public async Task SendAsync<T>(NetworkStream stream, T message, CancellationToken token)
    {
        if (await _fault.ApplyAsync(stream, token))
            await Framing.WriteAsync(stream, message, Json.Options, token);
    }
}
