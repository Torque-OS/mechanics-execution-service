using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MechanicsSoftware.ExecutionService.Api.Transport;
using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MechanicsSoftware.ExecutionService.Application.UseCases.Executions;
using MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;
using MechanicsSoftware.ExecutionService.Domain.Entities;
using MechanicsSoftware.ExecutionService.Infrastructure.Persistence;
using MechanicsSoftware.ExecutionService.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;

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

    [Fact]
    public async Task StartDiagnosis_QueuedOrder_StatusBecomesInProgress()
    {
        await using var factory = new ExecutionApiFactory();
        var orderId = await SeedQueuedOrderAsync(factory);
        var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/executions/{orderId}/start-diagnosis", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ExecutionOrderResponse>();
        result!.Status.Should().Be("IN_PROGRESS");
    }

    [Fact]
    public async Task AddNote_InProgressOrder_Returns204AndListable()
    {
        await using var factory = new ExecutionApiFactory();
        var orderId = await SeedQueuedOrderAsync(factory);
        var client = factory.CreateClient();
        await client.PostAsync($"/api/executions/{orderId}/start-diagnosis", null);

        var addResponse = await client.PostAsJsonAsync(
            $"/api/executions/{orderId}/notes", new AddExecutionNoteRequest("Oil leak found."));
        addResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var notesResponse = await client.GetAsync($"/api/executions/{orderId}/notes");
        var notes = await notesResponse.Content.ReadFromJsonAsync<List<ExecutionNoteResponse>>();
        notes.Should().NotBeNull().And.ContainSingle(n => n.Text == "Oil leak found.");
    }

    [Fact]
    public async Task AddNote_StillQueued_Returns422()
    {
        await using var factory = new ExecutionApiFactory();
        var orderId = await SeedQueuedOrderAsync(factory);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/executions/{orderId}/notes", new AddExecutionNoteRequest("too early"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private static async Task<Guid> SeedQueuedOrderAsync(ExecutionApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExecutionDbContext>();
        var order = ExecutionOrder.Create(Guid.NewGuid());
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    }
}
