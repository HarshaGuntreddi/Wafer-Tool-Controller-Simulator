using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Shared.Protocol;

public static class Framing
{
    public static async Task WriteAsync<T>(NetworkStream stream, T message, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message, options);
        var data = Encoding.UTF8.GetBytes(json + "\n");
        await stream.WriteAsync(data, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public static async IAsyncEnumerable<T?> ReadAsync<T>(NetworkStream stream, JsonSerializerOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var buffer = new byte[1024];
        var sb = new StringBuilder();
        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                if (sb.Length > 0 && TryDeserialize<T>(sb.ToString(), options, out var final))
                    yield return final;
                yield break;
            }

            var chunk = Encoding.UTF8.GetString(buffer, 0, read);
            foreach (var ch in chunk)
            {
                if (ch == '\n')
                {
                    if (TryDeserialize<T>(sb.ToString(), options, out var message))
                        yield return message;
                    sb.Clear();
                }
                else if (ch != '\r')
                {
                    sb.Append(ch);
                }
            }
        }
    }

    private static bool TryDeserialize<T>(string json, JsonSerializerOptions? options, out T? value)
    {
        try
        {
            value = JsonSerializer.Deserialize<T>(json, options);
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }
}
