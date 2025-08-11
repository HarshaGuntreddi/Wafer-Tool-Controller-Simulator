using ControllerService.Api;
using FluentAssertions;
using Shared.Domain;

namespace IntegrationTests;

public class ApiTests
{
    [Fact]
    public async Task Status_and_run_endpoints_work()
    {
        var env = await TestEnvironment.StartAsync();
        try
        {
            var status = await env.Client.GetFromJsonAsync<StatusDto>("/api/status", Shared.Util.Json.Options);
            status.Should().NotBeNull();
            status!.State.Should().Be(ToolState.IDLE);

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
