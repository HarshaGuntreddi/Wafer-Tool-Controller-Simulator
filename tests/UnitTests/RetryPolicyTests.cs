using FluentAssertions;
using Shared.Util;

namespace UnitTests;

public class RetryPolicyTests
{
    [Fact]
    public async Task Retries_until_success()
    {
        var attempts = 0;
        await RetryPolicy.ExecuteAsync(async () =>
        {
            attempts++;
            if (attempts < 3)
                throw new InvalidOperationException();
        }, 5, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(2));
        attempts.Should().Be(3);
    }

    [Fact]
    public async Task Throws_after_max_attempts()
    {
        var attempts = 0;
        Func<Task> act = () => RetryPolicy.ExecuteAsync(async () =>
        {
            attempts++;
            throw new InvalidOperationException();
        }, 3, TimeSpan.Zero, TimeSpan.Zero);
        await act.Should().ThrowAsync<InvalidOperationException>();
        attempts.Should().Be(3);
    }
}
