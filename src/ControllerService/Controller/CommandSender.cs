using Shared.Protocol;
using Shared.Util;

namespace ControllerService.Controller;

public class CommandSender
{
    private readonly ClientConnection _connection;

    public CommandSender(ClientConnection connection)
    {
        _connection = connection;
    }

    public Task SendAsync(Command cmd, CancellationToken cancellationToken = default) =>
        RetryPolicy.ExecuteAsync(async () =>
        {
            var id = Guid.NewGuid();
            var envelope = new Envelope(id, cmd.ToString(), DateTime.UtcNow);
            var ack = _connection.RegisterAck(id);
            await _connection.SendAsync(envelope, cancellationToken);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
            await ack.Task.WaitAsync(linked.Token);
        }, attempts: 3, baseDelay: TimeSpan.FromMilliseconds(500), maxDelay: TimeSpan.FromSeconds(2), cancellationToken);
}
