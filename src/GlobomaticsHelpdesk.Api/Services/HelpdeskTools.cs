using System.ComponentModel;
using GlobomaticsHelpdesk.Api.Contracts;
using GlobomaticsHelpdesk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GlobomaticsHelpdesk.Api.Services;

public class HelpdeskTools
{
    [Description(@"Retrieves the full details of a helpdesk support ticket by its numeric ticket number, 
        including status, priority, assignee, and description.")]
    public async Task<TicketDetailDto?> GetTicketByIdAsync(
        [Description("The numeric ticket number (e.g., 1001) that identifies the ticket.")] 
            int ticketNumber,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

        var ticket = await db.Tickets
            .Include(t => t.Requester)
            .Include(t => t.AssignedAgent)
            .Include(t => t.Comments).ThenInclude(c => c.AuthorUser)
            .Include(t => t.Comments).ThenInclude(c => c.AuthorAgent)
            .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber, cancellationToken);

        if (ticket is null)
            return null;

        return new TicketDetailDto(
            ticket.Id,
            ticket.TicketNumber,
            ticket.Title,
            ticket.Description,
            ticket.Status,
            ticket.Priority,
            ticket.RequesterId,
            ticket.Requester?.DisplayName ?? string.Empty,
            ticket.AssignedAgentId,
            ticket.AssignedAgent?.DisplayName,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.Comments
                .OrderBy(c => c.CreatedAt)
                .Select(c => new CommentDto(
                    c.Id,
                    c.TicketId,
                    c.AuthorUserId,
                    c.AuthorAgentId,
                    c.AuthorAgent?.DisplayName ?? c.AuthorUser?.DisplayName ?? "Unknown",
                    c.Body,
                    c.IsInternalNote,
                    c.CreatedAt))
                .ToList());
    }
}
