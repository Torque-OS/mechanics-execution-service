using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;
using MechanicsSoftware.ExecutionService.UnitTests.Helpers;

namespace MechanicsSoftware.ExecutionService.UnitTests.Api;

public class ExecutionsEndpointTests
{
    [Fact]
    public async Task GetQueue_ReturnsEntriesOrderedByPriority()
    {
        await using var factory = new ExecutionApiFactory();
        await factory.Queue.EnqueueAsync(new ExecutionQueueEntry(Guid.NewGuid(), Guid.NewGuid(), Priority: 5, QueuedAt: DateTime.UtcNow));
        await factory.Queue.EnqueueAsync(new ExecutionQueueEntry(Guid.NewGuid(), Guid.NewGuid(), Priority: 1, QueuedAt: DateTime.UtcNow));
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/executions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var entries = await response.Content.ReadFromJsonAsync<List<ExecutionQueueEntryResponse>>();
        entries.Should().NotBeNull().And.HaveCount(2);
        entries![0].Priority.Should().Be(1);
    }

    [Fact]
    public async Task Get_UnknownId_Returns404()
    {
        await using var factory = new ExecutionApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/executions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
