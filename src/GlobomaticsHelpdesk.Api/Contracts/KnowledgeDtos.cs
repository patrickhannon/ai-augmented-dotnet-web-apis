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
