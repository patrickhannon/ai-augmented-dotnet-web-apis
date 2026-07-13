namespace GlobomaticsHelpdesk.Api.Domain;

public class Ticket
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.New;
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;

    public Guid RequesterId { get; set; }
    public User? Requester { get; set; }

    public Guid? AssignedAgentId { get; set; }
    public Agent? AssignedAgent { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<Comment> Comments { get; set; } = new();
}
