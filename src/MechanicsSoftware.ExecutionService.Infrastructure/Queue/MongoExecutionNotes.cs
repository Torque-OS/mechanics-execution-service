using System.Diagnostics.CodeAnalysis;
using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MechanicsSoftware.ExecutionService.Infrastructure.Queue;

/// <summary>Diagnosis/repair notes in MongoDB (ADR-012, collection <c>execution_notes</c>, append-only, F4-19).</summary>
[ExcludeFromCodeCoverage(Justification = "Thin MongoDB adapter; exercised manually against the compose stack.")]
public sealed class MongoExecutionNotes(IMongoDatabase database) : IExecutionNotes
{
    public const string CollectionName = "execution_notes";

    private readonly IMongoCollection<BsonDocument> _collection = database.GetCollection<BsonDocument>(CollectionName);

    public Task AddAsync(ExecutionNote note, CancellationToken cancellationToken = default)
    {
        var document = new BsonDocument
        {
            ["executionOrderId"] = note.ExecutionOrderId.ToString(),
            ["text"] = note.Text,
            ["recordedAt"] = note.RecordedAt,
        };

        return _collection.InsertOneAsync(document, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<ExecutionNote>> ListAsync(
        Guid executionOrderId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("executionOrderId", executionOrderId.ToString());
        var sort = Builders<BsonDocument>.Sort.Ascending("recordedAt");

        var documents = await _collection.Find(filter).Sort(sort).ToListAsync(cancellationToken);

        return documents.Select(ToNote).ToList();
    }

    private static ExecutionNote ToNote(BsonDocument document) => new(
        Guid.Parse(document["executionOrderId"].AsString),
        document["text"].AsString,
        document["recordedAt"].ToUniversalTime());
}
