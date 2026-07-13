using System.ComponentModel;
using GlobomaticsHelpdesk.Api.Auth;
using GlobomaticsHelpdesk.Api.Contracts;
using GlobomaticsHelpdesk.Api.Data;
using GlobomaticsHelpdesk.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace GlobomaticsHelpdesk.Api.Services;

public class HelpdeskTools
{
    [Description("Retrieves the full details of a helpdesk support ticket by its numeric ticket number, including status, priority, assignee, and description.")]
    public async Task<TicketDetailDto?> GetTicketByIdAsync(
        [Description("The numeric ticket number (e.g., 1001) that identifies the ticket.")] int ticketNumber,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (ticketNumber <= 0)
        {
            return null;
        }

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

    [Description("Retrieves all helpdesk tickets that match the specified status. Valid statuses are: New, Open, Pending, Resolved, Closed.")]
    public async Task<object> GetTicketsByStatusAsync(
        [Description("The ticket status to filter by (New, Open, Pending, Resolved, or Closed).")] TicketStatus status,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(status))
        {
            return $"Invalid status value '{status}'. Valid values are: {string.Join(", ", Enum.GetNames<TicketStatus>())}.";
        }

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

        var tickets = await db.Tickets
            .Include(t => t.Requester)
            .Include(t => t.AssignedAgent)
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.UpdatedAt)
            .Select(t => new TicketSummaryDto(
                t.Id,
                t.TicketNumber,
                t.Title,
                t.Status,
                t.Priority,
                t.RequesterId,
                t.Requester!.DisplayName,
                t.AssignedAgentId,
                t.AssignedAgent != null ? t.AssignedAgent.DisplayName : null,
                t.CreatedAt,
                t.UpdatedAt))
            .ToListAsync(cancellationToken);

        return tickets;
    }

    [Description("Updates the priority of an existing helpdesk ticket. This tool is only offered to authorized agent or admin callers. Valid priorities are: Low, Normal, High, Urgent.")]
    public async Task<string> UpdateTicketPriorityAsync(
        [Description("The numeric ticket number (e.g., 1001) that identifies the ticket to update.")] int ticketNumber,
        [Description("The new priority level to set (Low, Normal, High, or Urgent).")] TicketPriority newPriority,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (ticketNumber <= 0)
        {
            return $"Invalid ticket number '{ticketNumber}'. Ticket numbers must be positive integers (e.g., 1001).";
        }

        if (!Enum.IsDefined(newPriority))
        {
            return $"Invalid priority value '{newPriority}'. Valid values are: {string.Join(", ", Enum.GetNames<TicketPriority>())}.";
        }

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();

        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber, cancellationToken);

        if (ticket is null)
        {
            return $"Ticket {ticketNumber} not found.";
        }

        var accessor = scope.ServiceProvider.GetRequiredService<ClaimsPrincipalAccessor>();
        var user = accessor.User;

        if (!user.IsAgent() && !user.IsAdmin())
        {
            return $"Permission denied. You do not have authorization to update ticket priorities.";
        }

        var previousPriority = ticket.Priority;
        ticket.Priority = newPriority;
        ticket.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return $"Ticket {ticketNumber} priority updated from {previousPriority} to {newPriority}.";
    }
}
