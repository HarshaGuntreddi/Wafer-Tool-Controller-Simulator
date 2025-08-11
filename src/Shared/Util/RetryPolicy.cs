namespace Shared.Util;

public static class RetryPolicy
{
    public static Task ExecuteAsync(Func<Task> action, int attempts, TimeSpan baseDelay, TimeSpan maxDelay, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () => { await action(); return true; }, attempts, baseDelay, maxDelay, cancellationToken);

    public static async Task<T> ExecuteAsync<T>(Func<Task<T>> action, int attempts, TimeSpan baseDelay, TimeSpan maxDelay, CancellationToken cancellationToken = default)
    {
        var delay = baseDelay;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await action();
            }
            catch when (attempt < attempts)
            {
                await Task.Delay(delay, cancellationToken);
                var next = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, maxDelay.TotalMilliseconds));
                delay = next;
            }
        }
    }
}
