using System.Text;

namespace ToolSimulator.Simulator;

public class FaultInjection
{
    public int DropPercentage { get; set; }
    public int ExtraLatencyMs { get; set; }
    public int InvalidResponseEveryN { get; set; }
    private int _counter;

    public async Task<bool> ApplyAsync(NetworkStream stream, CancellationToken token)
    {
        if (ExtraLatencyMs > 0)
            await Task.Delay(ExtraLatencyMs, token);

        _counter++;
        if (InvalidResponseEveryN > 0 && _counter % InvalidResponseEveryN == 0)
        {
            var junk = Encoding.UTF8.GetBytes("invalid\n");
            await stream.WriteAsync(junk, token);
            return false;
        }

        if (DropPercentage > 0 && Random.Shared.Next(0, 100) < DropPercentage)
            return false;

        return true;
    }
}
