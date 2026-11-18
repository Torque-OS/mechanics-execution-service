using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MechanicsSoftware.ExecutionService.Api.Transport;
using MechanicsSoftware.ExecutionService.Domain.Entities;
using MechanicsSoftware.ExecutionService.Infrastructure.Persistence;
using MechanicsSoftware.ExecutionService.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MechanicsSoftware.ExecutionService.UnitTests.Api;

public class ExecutionsAuthorizationTests
{
    [Fact]
    public async Task GetQueue_WithoutToken_Returns401()
    {
        await using var factory = new ExecutionApiFactory();

        var response = await factory.CreateClient().GetAsync("/api/executions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetQueue_WithCustomerToken_Returns403()
    {
        await using var factory = new ExecutionApiFactory();
        var client = factory.CreateClient().WithToken(TestTokens.Customer());

        var response = await client.GetAsync("/api/executions");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("ADMIN")]
    [InlineData("ATTENDANT")]
    [InlineData("MECHANIC")]
    public async Task GetQueue_WithStaffToken_Returns200(string role)
    {
        await using var factory = new ExecutionApiFactory();
        var client = factory.CreateClient().WithToken(TestTokens.Staff(role));

        var response = await client.GetAsync("/api/executions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetQueue_WithTokenSignedByAnotherSecret_Returns401()
    {
        await using var factory = new ExecutionApiFactory();
        var token = TestTokens.Create("MECHANIC", Guid.NewGuid(), secret: "another-secret-that-is-at-least-32-chars!");
        var client = factory.CreateClient().WithToken(token);

        var response = await client.GetAsync("/api/executions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetQueue_WithExpiredToken_Returns401()
    {
        await using var factory = new ExecutionApiFactory();
        var token = TestTokens.Create("MECHANIC", Guid.NewGuid(), expires: DateTime.UtcNow.AddMinutes(-1));
        var client = factory.CreateClient().WithToken(token);

        var response = await client.GetAsync("/api/executions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Fail_WithCustomerToken_Returns403AndLeavesTheOrderUntouched()
    {
        await using var factory = new ExecutionApiFactory();
        var orderId = await SeedQueuedOrderAsync(factory);
        var client = factory.CreateClient().WithToken(TestTokens.Customer());

        var response = await client.PostAsJsonAsync(
            $"/api/executions/{orderId}/fail", new FailExecutionRequest("not allowed"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        factory.Events.Failed.Should().BeEmpty();
        await using var scope = factory.Services.CreateAsyncScope();
        var order = await scope.ServiceProvider.GetRequiredService<ExecutionDbContext>()
            .ExecutionOrders.AsNoTracking().SingleAsync(o => o.Id == orderId);
        order.Status.ToString().Should().Be("QUEUED");
    }

    [Fact]
    public async Task Complete_WithoutToken_Returns401()
    {
        await using var factory = new ExecutionApiFactory();
        var orderId = await SeedQueuedOrderAsync(factory);

        var response = await factory.CreateClient().PostAsync($"/api/executions/{orderId}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
