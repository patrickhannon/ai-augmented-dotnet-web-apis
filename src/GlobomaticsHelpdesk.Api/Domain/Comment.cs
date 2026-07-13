namespace GlobomaticsHelpdesk.Api.Domain;

public class Comment
{
    public Guid Id { get; set; }

    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public Guid? AuthorUserId { get; set; }
    public User? AuthorUser { get; set; }

    public Guid? AuthorAgentId { get; set; }
    public Agent? AuthorAgent { get; set; }

    public string Body { get; set; } = string.Empty;
    public bool IsInternalNote { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
