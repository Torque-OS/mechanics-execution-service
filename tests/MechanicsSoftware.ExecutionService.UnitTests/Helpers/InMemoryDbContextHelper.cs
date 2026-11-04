using MechanicsSoftware.ExecutionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MechanicsSoftware.ExecutionService.UnitTests.Helpers;

public static class InMemoryDbContextHelper
{
    public static ExecutionDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ExecutionDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new ExecutionDbContext(options);
    }
}
