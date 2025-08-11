using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Shared.Protocol;
using Shared.Util;

namespace UnitTests;

public class ProtocolTests
{
    [Fact]
    public void Envelope_roundtrips_json()
    {
        var env = new Envelope(Guid.NewGuid(), "TEST", DateTime.UtcNow);
        var json = JsonSerializer.Serialize(env, Json.Options);
        var back = JsonSerializer.Deserialize<Envelope>(json, Json.Options);
        back.Should().BeEquivalentTo(env);
    }

    [Fact]
    public async Task Framing_assembles_partial_lines()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var accept = listener.AcceptTcpClientAsync();

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        using var server = await accept;

        var msg = new Envelope(Guid.NewGuid(), "X", DateTime.UtcNow);
        var json = JsonSerializer.Serialize(msg, Json.Options) + "\n";
        var bytes = Encoding.UTF8.GetBytes(json);
        var stream = server.GetStream();
        await stream.WriteAsync(bytes.AsMemory(0, bytes.Length / 2));
        await Task.Delay(10);
        await stream.WriteAsync(bytes.AsMemory(bytes.Length / 2));
        await stream.FlushAsync();

        var ns = client.GetStream();
        await foreach (var env in Framing.ReadAsync<Envelope>(ns, Json.Options))
        {
            env.Should().BeEquivalentTo(msg);
            break;
        }

        listener.Stop();
    }
}
