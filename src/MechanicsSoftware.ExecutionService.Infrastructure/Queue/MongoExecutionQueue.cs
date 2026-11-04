using System.Diagnostics.CodeAnalysis;
using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MechanicsSoftware.ExecutionService.Infrastructure.Queue;

/// <summary>
/// Read-optimized view of the execution queue in MongoDB (ADR-012, collection
/// <c>execution_queue</c>). PostgreSQL (<c>ExecutionDbContext</c>) is the system of record for an
/// <c>ExecutionOrder</c>; this projection exists so "what's next" reads don't compete with the
/// transactional writes, and so the document shape can evolve independently (e.g. diagnosis
/// notes in F4-19) without a relational migration.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Thin MongoDB adapter; exercised manually against the compose stack.")]
public sealed class MongoExecutionQueue(IMongoDatabase database) : IExecutionQueue
{
    public const string CollectionName = "execution_queue";

    private readonly IMongoCollection<BsonDocument> _collection = database.GetCollection<BsonDocument>(CollectionName);

    public Task EnqueueAsync(ExecutionQueueEntry entry, CancellationToken cancellationToken = default)
    {
        var document = new BsonDocument
        {
            ["_id"] = entry.ExecutionOrderId.ToString(),
            ["serviceOrderId"] = entry.ServiceOrderId.ToString(),
            ["priority"] = entry.Priority,
            ["queuedAt"] = entry.QueuedAt,
        };

        return _collection.ReplaceOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", document["_id"]),
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<IReadOnlyList<ExecutionQueueEntry>> GetQueueAsync(CancellationToken cancellationToken = default)
    {
        var sort = Builders<BsonDocument>.Sort.Ascending("priority").Ascending("queuedAt");

        var documents = await _collection.Find(FilterDefinition<BsonDocument>.Empty)
            .Sort(sort)
            .ToListAsync(cancellationToken);

        return documents.Select(ToEntry).ToList();
    }

    public Task RemoveAsync(Guid executionOrderId, CancellationToken cancellationToken = default) =>
        _collection.DeleteOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", executionOrderId.ToString()),
            cancellationToken);

    private static ExecutionQueueEntry ToEntry(BsonDocument document) => new(
        Guid.Parse(document["_id"].AsString),
        Guid.Parse(document["serviceOrderId"].AsString),
        document["priority"].AsInt32,
        document["queuedAt"].ToUniversalTime());
}
