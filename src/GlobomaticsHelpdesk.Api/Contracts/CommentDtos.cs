namespace GlobomaticsHelpdesk.Api.Contracts;

public record CommentDto(
    Guid Id,
    Guid TicketId,
    Guid? AuthorUserId,
    Guid? AuthorAgentId,
    string AuthorName,
    string Body,
    bool IsInternalNote,
    DateTimeOffset CreatedAt);

public record AddCommentRequest(string Body, bool IsInternalNote);
