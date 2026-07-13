namespace GlobomaticsHelpdesk.Api.Contracts;

public record TriageRequest(string Message, string? CustomerId = null);

public record TriageResult(
    string Priority,
    string Category,
    string Summary,
    List<string> RecommendedActions);
