using GlobomaticsHelpdesk.Api.Auth;
using GlobomaticsHelpdesk.Api.Contracts;
using GlobomaticsHelpdesk.Api.Data;
using GlobomaticsHelpdesk.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.VectorData;

namespace GlobomaticsHelpdesk.Api.Endpoints;

public static class KnowledgeEndpoints
{
    public static IEndpointRouteBuilder MapKnowledgeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/knowledge")
            .WithTags("Knowledge")
            .RequireAuthorization(AuthorizationPolicies.AnyAuthenticated);

        group.MapGet("/", Search)
            .WithName("SearchKnowledge")
            .WithSummary("Keyword search across knowledge base articles (title, body, tags).");

        group.MapGet("/{id:guid}", GetArticle)
            .WithName("GetKnowledgeArticle")
            .WithSummary("Get a knowledge base article by id.");

        // Temporary diagnostic endpoint — proves vector search works before wiring into chat.
        group.MapGet("/search-test", VectorSearchTest)
            .WithName("VectorSearchTest")
            .WithSummary("Diagnostic: test semantic search against the knowledge base vector store.");

        return app;
    }

    private static async Task<Ok<PagedResult<KnowledgeArticleSummaryDto>>> Search(
        HelpdeskDbContext db,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 25 : pageSize;

        // KB articles are small and Tags has a value converter (which the
        // in-memory provider can't translate inside Where), so we materialize
        // and filter in memory.
        var all = await db.KnowledgeArticles.ToListAsync(ct);

        IEnumerable<KnowledgeArticle> filtered = all;
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            filtered = filtered.Where(a =>
                a.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                a.Body.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                a.Tags.Any(t => t.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        var ordered = filtered.OrderByDescending(a => a.UpdatedAt).ToList();
        var total = ordered.Count;
        var items = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new KnowledgeArticleSummaryDto(a.Id, a.Title, a.Tags, a.UpdatedAt))
            .ToList();

        return TypedResults.Ok(new PagedResult<KnowledgeArticleSummaryDto>(items, page, pageSize, total));
    }

    private static async Task<Results<Ok<KnowledgeArticleDetailDto>, NotFound>> GetArticle(
        Guid id,
        HelpdeskDbContext db,
        CancellationToken ct)
    {
        var article = await db.KnowledgeArticles.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (article is null) return TypedResults.NotFound();

        return TypedResults.Ok(new KnowledgeArticleDetailDto(article.Id, article.Title, article.Body, article.Tags, article.UpdatedAt));
    }

    private static async Task<Ok<List<VectorSearchResultDto>>> VectorSearchTest(
        [FromQuery] string q,
        VectorStoreCollection<Guid, KnowledgeArticleSearchRecord> collection,
        CancellationToken ct)
    {
        const int topResults = 3;

        var results = new List<VectorSearchResultDto>();

        await foreach (var result in collection.SearchAsync(q, topResults, cancellationToken: ct))
        {
            var snippet = result.Record.Body.Length > 150
                ? result.Record.Body[..150] + "…"
                : result.Record.Body;

            results.Add(new VectorSearchResultDto(result.Record.Title, snippet, result.Score));
        }

        return TypedResults.Ok(results);
    }
}
