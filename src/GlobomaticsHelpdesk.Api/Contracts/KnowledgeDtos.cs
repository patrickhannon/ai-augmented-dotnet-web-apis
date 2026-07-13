namespace GlobomaticsHelpdesk.Api.Contracts;

public record KnowledgeArticleSummaryDto(
    Guid Id,
    string Title,
    IReadOnlyList<string> Tags,
    DateTimeOffset UpdatedAt);

public record KnowledgeArticleDetailDto(
    Guid Id,
    string Title,
    string Body,
    IReadOnlyList<string> Tags,
    DateTimeOffset UpdatedAt);

// Temporary DTO for the vector search diagnostic endpoint.
public record VectorSearchResultDto(
    Guid Id,
    string Title,
    string BodySnippet,
    string Body,
    double? Score);
