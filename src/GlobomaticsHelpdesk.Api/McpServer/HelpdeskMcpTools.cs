using System.ComponentModel;
using GlobomaticsHelpdesk.Api.Domain;
using GlobomaticsHelpdesk.Api.Services;
using ModelContextProtocol.Server;

namespace GlobomaticsHelpdesk.Api.McpServer;

[McpServerToolType]
public class HelpdeskMcpTools
{
    private readonly HelpdeskTools _tools;
    private readonly IServiceProvider _serviceProvider;

    public HelpdeskMcpTools(HelpdeskTools tools, IServiceProvider serviceProvider)
    {
        _tools = tools;
        _serviceProvider = serviceProvider;
    }

    [McpServerTool(Name = "get-ticket-by-id"), Description("Retrieves the full details of a helpdesk support ticket by its numeric ticket number, including status, priority, assignee, and description.")]
    public async Task<object?> GetTicketByIdAsync(
        [Description("The numeric ticket number (e.g., 1001) that identifies the ticket.")] int ticketNumber,
        CancellationToken cancellationToken)
    {
        var result = await _tools.GetTicketByIdAsync(ticketNumber, _serviceProvider, cancellationToken);
        return result is null
            ? $"Ticket {ticketNumber} not found."
            : result;
    }

    [McpServerTool(Name = "get-tickets-by-status"), Description("Retrieves all helpdesk tickets that match the specified status. Valid statuses are: New, Open, Pending, Resolved, Closed.")]
    public async Task<object> GetTicketsByStatusAsync(
        [Description("The ticket status to filter by (New, Open, Pending, Resolved, or Closed).")] TicketStatus status,
        CancellationToken cancellationToken)
    {
        return await _tools.GetTicketsByStatusAsync(status, _serviceProvider, cancellationToken);
    }

    [McpServerTool(Name = "update-ticket-priority"), Description("Updates the priority of an existing helpdesk ticket. This tool validates that the caller has agent or admin permissions. Valid priorities are: Low, Normal, High, Urgent.")]
    public async Task<string> UpdateTicketPriorityAsync(
        [Description("The numeric ticket number (e.g., 1001) that identifies the ticket to update.")] int ticketNumber,
        [Description("The new priority level to set (Low, Normal, High, or Urgent).")] TicketPriority newPriority,
        CancellationToken cancellationToken)
    {
        return await _tools.UpdateTicketPriorityAsync(ticketNumber, newPriority, _serviceProvider, cancellationToken);
    }
}
