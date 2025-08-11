using ControllerService.Api;
using FluentAssertions;
using Shared.Domain;

namespace IntegrationTests;

public class FaultInjection_Tests
{
    [Fact]
    public async Task Retries_with_fault_injection()
    {
        var env = await TestEnvironment.StartAsync(drop: 0.5);
        try
        {
            var start = await env.Client.PostAsync("/api/run/start", null);
            start.EnsureSuccessStatusCode();
            await env.WaitForState(ToolState.PROCESSING);

            var stop = await env.Client.PostAsync("/api/run/stop", null);
            stop.EnsureSuccessStatusCode();
            await env.WaitForState(ToolState.IDLE);
        }
        finally
        {
            await env.DisposeAsync();
        }
    }
}
