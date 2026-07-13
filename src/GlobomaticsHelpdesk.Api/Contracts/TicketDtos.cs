using GlobomaticsHelpdesk.Api.Domain;

namespace GlobomaticsHelpdesk.Api.Contracts;

public record TicketSummaryDto(
    Guid Id,
    string Title,
    TicketStatus Status,
    TicketPriority Priority,
    Guid RequesterId,
    string RequesterName,
    Guid? AssignedAgentId,
    string? AssignedAgentName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record TicketDetailDto(
    Guid Id,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    Guid RequesterId,
    string RequesterName,
    Guid? AssignedAgentId,
    string? AssignedAgentName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<CommentDto> Comments);

public record CreateTicketRequest(
    string Title,
    string Description,
    TicketPriority Priority,
    Guid? RequesterId,
    Guid? AssignedAgentId);

public record UpdateTicketStatusRequest(TicketStatus Status);

public record ReassignTicketRequest(Guid? AssignedAgentId);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
