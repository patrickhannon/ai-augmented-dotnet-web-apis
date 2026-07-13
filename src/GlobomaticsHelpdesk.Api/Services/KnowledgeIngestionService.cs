using GlobomaticsHelpdesk.Api.Data;
using GlobomaticsHelpdesk.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.VectorData;

namespace GlobomaticsHelpdesk.Api.Services;

public class KnowledgeIngestionService(
    IServiceProvider serviceProvider,
    VectorStoreCollection<Guid, KnowledgeArticleSearchRecord> collection,
    ILogger<KnowledgeIngestionService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

        var articles = await db.KnowledgeArticles.ToListAsync(cancellationToken);

        var records = articles.Select(article => new KnowledgeArticleSearchRecord
        {
            Id = article.Id,
            Title = article.Title,
            Body = article.Body,
            Tags = string.Join(", ", article.Tags),
            Content = $"{article.Title}\n\n{article.Body}"
        });

        await collection.EnsureCollectionExistsAsync(cancellationToken);
        await collection.UpsertAsync(records, cancellationToken);

        logger.LogInformation("Ingested {Count} knowledge articles into vector store.", articles.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
