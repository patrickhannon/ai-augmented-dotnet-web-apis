using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Numerics.Tensors;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;


namespace GlobomaticsHelpdesk.Api.Data;

/// <summary>
/// A simple in-memory vector store for development and demos.
/// Stores records in memory and uses cosine similarity for vector search.
/// </summary>
public sealed class InMemoryVectorStore(
    IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator = null)
    : VectorStore
{
    private readonly ConcurrentDictionary<string, object> _collections = new();

    public override VectorStoreCollection<TKey, TRecord> GetCollection<TKey, TRecord>(
        string name,
        VectorStoreCollectionDefinition? definition = null)
    {
        return (VectorStoreCollection<TKey, TRecord>)_collections.GetOrAdd(
            name,
            _ => new InMemoryVectorStoreCollection<TKey, TRecord>(name, embeddingGenerator));
    }

    public override VectorStoreCollection<object, Dictionary<string, object?>> GetDynamicCollection(
        string name,
        VectorStoreCollectionDefinition definition)
    {
        throw new NotSupportedException("Dynamic collections are not supported in this demo implementation.");
    }

    public override async IAsyncEnumerable<string> ListCollectionNamesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var name in _collections.Keys)
        {
            yield return name;
        }

        await Task.CompletedTask;
    }

    public override Task<bool> CollectionExistsAsync(string name, CancellationToken cancellationToken = default)
        => Task.FromResult(_collections.ContainsKey(name));

    public override Task EnsureCollectionDeletedAsync(string name, CancellationToken cancellationToken = default)
    {
        _collections.TryRemove(name, out _);
        return Task.CompletedTask;
    }

    public override object? GetService(Type serviceType, object? serviceKey = null)
        => serviceType == typeof(VectorStore) ? this : null;
}

/// <summary>
/// An in-memory collection that supports automatic embedding generation for string vector properties.
/// </summary>
public sealed class InMemoryVectorStoreCollection<TKey, TRecord> : VectorStoreCollection<TKey, TRecord>
    where TKey : notnull
    where TRecord : class
{
    private readonly ConcurrentDictionary<TKey, TRecord> _records = new();
    private readonly ConcurrentDictionary<TKey, ReadOnlyMemory<float>> _embeddings = new();
    private readonly IEmbeddingGenerator<string, Embedding<float>>? _embeddingGenerator;
    private readonly PropertyInfo _keyProperty;
    private readonly PropertyInfo? _vectorProperty;

    public InMemoryVectorStoreCollection(
        string name,
        IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator)
    {
        Name = name;
        _embeddingGenerator = embeddingGenerator;

        var recordType = typeof(TRecord);
        _keyProperty = recordType.GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<VectorStoreKeyAttribute>() is not null)
            ?? throw new InvalidOperationException($"No [VectorStoreKey] property found on {recordType.Name}.");

        _vectorProperty = recordType.GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<VectorStoreVectorAttribute>() is not null);
    }

    public override string Name { get; }

    public override Task<bool> CollectionExistsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public override Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public override Task EnsureCollectionDeletedAsync(CancellationToken cancellationToken = default)
    {
        _records.Clear();
        _embeddings.Clear();
        return Task.CompletedTask;
    }

    public override async Task UpsertAsync(TRecord record, CancellationToken cancellationToken = default)
    {
        var key = GetKey(record);
        _records[key] = record;

        if (_vectorProperty is not null && _embeddingGenerator is not null)
        {
            var text = _vectorProperty.GetValue(record) as string;
            if (!string.IsNullOrEmpty(text))
            {
                var embedding = await _embeddingGenerator.GenerateAsync(text, cancellationToken: cancellationToken);
                _embeddings[key] = embedding.Vector;
            }
        }
    }

    public override async Task UpsertAsync(IEnumerable<TRecord> records, CancellationToken cancellationToken = default)
    {
        foreach (var record in records)
        {
            await UpsertAsync(record, cancellationToken);
        }
    }

    public override async IAsyncEnumerable<VectorSearchResult<TRecord>> SearchAsync<TInput>(
        TInput searchValue,
        int top,
        VectorSearchOptions<TRecord>? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_embeddingGenerator is null)
            throw new InvalidOperationException("No embedding generator configured for vector search.");

        if (searchValue is not string queryText)
            throw new NotSupportedException("Only string search values are supported.");

        var queryEmbedding = await _embeddingGenerator.GenerateAsync(queryText, cancellationToken: cancellationToken);
        var queryVector = queryEmbedding.Vector;

        var scored = new List<(TKey Key, double Score)>();
        foreach (var (key, embedding) in _embeddings)
        {
            var score = TensorPrimitives.CosineSimilarity(queryVector.Span, embedding.Span);
            scored.Add((key, score));
        }

        var ranked = scored
            .OrderByDescending(x => x.Score)
            .Take(top);

        foreach (var (key, score) in ranked)
        {
            if (_records.TryGetValue(key, out var record))
            {
                yield return new VectorSearchResult<TRecord>(record, score);
            }
        }
    }

    public override Task<TRecord?> GetAsync(TKey key, RecordRetrievalOptions? options = null, CancellationToken cancellationToken = default)
        => Task.FromResult(_records.GetValueOrDefault(key));

    public override async IAsyncEnumerable<TRecord> GetAsync(
        IEnumerable<TKey> keys,
        RecordRetrievalOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var key in keys)
        {
            if (_records.TryGetValue(key, out var record))
            {
                yield return record;
            }
        }

        await Task.CompletedTask;
    }

    public override async IAsyncEnumerable<TRecord> GetAsync(
        Expression<Func<TRecord, bool>> filter,
        int top,
        FilteredRecordRetrievalOptions<TRecord>? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var compiled = filter.Compile();
        var results = _records.Values.Where(compiled).Take(top);

        foreach (var record in results)
        {
            yield return record;
        }

        await Task.CompletedTask;
    }

    public override Task DeleteAsync(TKey key, CancellationToken cancellationToken = default)
    {
        _records.TryRemove(key, out _);
        _embeddings.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(IEnumerable<TKey> keys, CancellationToken cancellationToken = default)
    {
        foreach (var key in keys)
        {
            _records.TryRemove(key, out _);
            _embeddings.TryRemove(key, out _);
        }
        return Task.CompletedTask;
    }

    public override object? GetService(Type serviceType, object? serviceKey = null)
        => serviceType == typeof(VectorStoreCollection<TKey, TRecord>) ? this : null;

    private TKey GetKey(TRecord record)
        => (TKey)_keyProperty.GetValue(record)!;
}
