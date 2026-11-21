using CommunityToolkit.VectorData.InMemory;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace GlobomaticsHelpdesk.Api.Data;

public sealed class MemoryVectorStore(
    IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator = null)
    : VectorStore
{
    private readonly InMemoryVectorStore _store = new(new InMemoryVectorStoreOptions
    {
        EmbeddingGenerator = embeddingGenerator
    });

    public override VectorStoreCollection<TKey, TRecord> GetCollection<TKey, TRecord>(
        string name,
        VectorStoreCollectionDefinition? definition = null)
        => _store.GetCollection<TKey, TRecord>(name, definition);

    public override VectorStoreCollection<object, Dictionary<string, object?>> GetDynamicCollection(
        string name,
        VectorStoreCollectionDefinition definition)
        => throw new NotSupportedException(
            "Dynamic collections are not supported in this demo implementation.");

    public override IAsyncEnumerable<string> ListCollectionNamesAsync(
        CancellationToken cancellationToken = default)
        => _store.ListCollectionNamesAsync(cancellationToken);

    public override Task<bool> CollectionExistsAsync(
        string name,
        CancellationToken cancellationToken = default)
        => _store.CollectionExistsAsync(name, cancellationToken);

    public override Task EnsureCollectionDeletedAsync(
        string name,
        CancellationToken cancellationToken = default)
        => _store.EnsureCollectionDeletedAsync(name, cancellationToken);

    public override object? GetService(Type serviceType, object? serviceKey = null)
        => _store.GetService(serviceType, serviceKey);
}
