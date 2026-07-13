using GlobomaticsHelpdesk.Api.Auth;
using GlobomaticsHelpdesk.Api.Contracts;
using GlobomaticsHelpdesk.Api.Data;
using GlobomaticsHelpdesk.Api.Domain;
using GlobomaticsHelpdesk.Api.Validation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GlobomaticsHelpdesk.Api.Endpoints;

public static class TicketsEndpoints
{
    public static IEndpointRouteBuilder MapTicketsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tickets")
            .WithTags("Tickets")
            .RequireAuthorization(AuthorizationPolicies.AnyAuthenticated);

        group.MapGet("/", ListTickets)
            .WithName("ListTickets")
            .WithSummary("List tickets, optionally filtered by status, priority, assigned agent, or keyword.");

        group.MapGet("/{id:guid}", GetTicket)
            .WithName("GetTicket")
            .WithSummary("Get a ticket by id, including comments.");

        group.MapPost("/", CreateTicket)
            .AddEndpointFilter<ValidationFilter<CreateTicketRequest>>()
            .RequireAuthorization(AuthorizationPolicies.AgentOrAdmin)
            .WithName("CreateTicket")
            .WithSummary("Create a new ticket. Requires agent or admin.");

        group.MapPatch("/{id:guid}/status", UpdateStatus)
            .AddEndpointFilter<ValidationFilter<UpdateTicketStatusRequest>>()
            .RequireAuthorization(AuthorizationPolicies.AgentOrAdmin)
            .WithName("UpdateTicketStatus")
            .WithSummary("Update the status of a ticket.");

        group.MapPatch("/{id:guid}/assignment", ReassignTicket)
            .AddEndpointFilter<ValidationFilter<ReassignTicketRequest>>()
            .RequireAuthorization(AuthorizationPolicies.AgentOrAdmin)
            .WithName("ReassignTicket")
            .WithSummary("Reassign a ticket. Agents may only assign to themselves or unassign.");

        group.MapPost("/{id:guid}/comments", AddComment)
            .AddEndpointFilter<ValidationFilter<AddCommentRequest>>()
            .RequireAuthorization(AuthorizationPolicies.AgentOrAdmin)
            .WithName("AddTicketComment")
            .WithSummary("Add a comment (optionally an internal note) to a ticket.");

        return app;
    }

    private static async Task<Ok<PagedResult<TicketSummaryDto>>> ListTickets(
        HelpdeskDbContext db,
        [FromQuery] TicketStatus? status,
        [FromQuery] TicketPriority? priority,
        [FromQuery] Guid? assignedAgentId,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 25 : pageSize;

        var query = db.Tickets
            .Include(t => t.Requester)
            .Include(t => t.AssignedAgent)
            .AsQueryable();

        if (status.HasValue) query = query.Where(t => t.Status == status.Value);
        if (priority.HasValue) query = query.Where(t => t.Priority == priority.Value);
        if (assignedAgentId.HasValue) query = query.Where(t => t.AssignedAgentId == assignedAgentId.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(t =>
                t.Title.ToLower().Contains(term) ||
                t.Description.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TicketSummaryDto(
                t.Id,
                t.Title,
                t.Status,
                t.Priority,
                t.RequesterId,
                t.Requester!.DisplayName,
                t.AssignedAgentId,
                t.AssignedAgent != null ? t.AssignedAgent.DisplayName : null,
                t.CreatedAt,
                t.UpdatedAt))
            .ToListAsync(ct);

        return TypedResults.Ok(new PagedResult<TicketSummaryDto>(items, page, pageSize, total));
    }

    private static async Task<Results<Ok<TicketDetailDto>, NotFound>> GetTicket(
        Guid id,
        HelpdeskDbContext db,
        CancellationToken ct)
    {
        var ticket = await db.Tickets
            .Include(t => t.Requester)
            .Include(t => t.AssignedAgent)
            .Include(t => t.Comments).ThenInclude(c => c.AuthorUser)
            .Include(t => t.Comments).ThenInclude(c => c.AuthorAgent)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (ticket is null) return TypedResults.NotFound();

        return TypedResults.Ok(MapDetail(ticket));
    }

    private static async Task<Results<Created<TicketDetailDto>, ValidationProblem, NotFound>> CreateTicket(
        CreateTicketRequest request,
        HelpdeskDbContext db,
        ClaimsPrincipalAccessor accessor,
        CancellationToken ct)
    {
        var user = accessor.User;

        Guid requesterId;
        if (request.RequesterId.HasValue)
        {
            requesterId = request.RequesterId.Value;
        }
        else
        {
            // Caller-as-requester only works when the caller is a user (not an agent/admin).
            var subjectId = user.GetSubjectId();
            var subjectIsUser = await db.Users.AnyAsync(u => u.Id == subjectId, ct);
            if (!subjectIsUser)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["RequesterId"] = new[] { "RequesterId is required when the caller is not a user." }
                });
            }
            requesterId = subjectId;
        }

        var requesterExists = await db.Users.AnyAsync(u => u.Id == requesterId, ct);
        if (!requesterExists)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["RequesterId"] = new[] { "Requester not found." }
            });
        }

        if (request.AssignedAgentId.HasValue)
        {
            var agentExists = await db.Agents.AnyAsync(a => a.Id == request.AssignedAgentId.Value, ct);
            if (!agentExists)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["AssignedAgentId"] = new[] { "Assigned agent not found." }
                });
            }
        }

        var now = DateTimeOffset.UtcNow;
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Priority = request.Priority,
            Status = TicketStatus.New,
            RequesterId = requesterId,
            AssignedAgentId = request.AssignedAgentId,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);

        await db.Entry(ticket).Reference(t => t.Requester).LoadAsync(ct);
        if (ticket.AssignedAgentId.HasValue)
        {
            await db.Entry(ticket).Reference(t => t.AssignedAgent).LoadAsync(ct);
        }

        var detail = MapDetail(ticket);
        return TypedResults.Created($"/api/tickets/{ticket.Id}", detail);
    }

    private static async Task<Results<Ok<TicketDetailDto>, NotFound, ValidationProblem>> UpdateStatus(
        Guid id,
        UpdateTicketStatusRequest request,
        HelpdeskDbContext db,
        CancellationToken ct)
    {
        var ticket = await db.Tickets
            .Include(t => t.Requester)
            .Include(t => t.AssignedAgent)
            .Include(t => t.Comments).ThenInclude(c => c.AuthorUser)
            .Include(t => t.Comments).ThenInclude(c => c.AuthorAgent)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (ticket is null) return TypedResults.NotFound();

        if (ticket.Status == TicketStatus.Closed && request.Status != TicketStatus.Closed)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Status"] = new[] { "Closed tickets cannot be reopened." }
            });
        }

        ticket.Status = request.Status;
        ticket.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(MapDetail(ticket));
    }

    private static async Task<Results<Ok<TicketDetailDto>, NotFound, ForbidHttpResult, ValidationProblem>> ReassignTicket(
        Guid id,
        ReassignTicketRequest request,
        HelpdeskDbContext db,
        ClaimsPrincipalAccessor accessor,
        CancellationToken ct)
    {
        var ticket = await db.Tickets
            .Include(t => t.Requester)
            .Include(t => t.AssignedAgent)
            .Include(t => t.Comments).ThenInclude(c => c.AuthorUser)
            .Include(t => t.Comments).ThenInclude(c => c.AuthorAgent)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (ticket is null) return TypedResults.NotFound();

        var user = accessor.User;
        var isAdmin = user.IsAdmin();
        var isAgent = user.IsAgent();
        var actingSubjectId = user.GetSubjectId();

        if (!isAdmin)
        {
            // Agent rule: may only reassign to self or unassign — and only if currently
            // unassigned or already assigned to themselves.
            if (!isAgent)
            {
                return TypedResults.Forbid();
            }

            var currentAllowed = ticket.AssignedAgentId is null || ticket.AssignedAgentId == actingSubjectId;
            var targetAllowed = request.AssignedAgentId is null || request.AssignedAgentId == actingSubjectId;
            if (!currentAllowed || !targetAllowed)
            {
                return TypedResults.Forbid();
            }
        }

        if (request.AssignedAgentId.HasValue)
        {
            var agentExists = await db.Agents.AnyAsync(a => a.Id == request.AssignedAgentId.Value, ct);
            if (!agentExists)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["AssignedAgentId"] = new[] { "Assigned agent not found." }
                });
            }
        }

        ticket.AssignedAgentId = request.AssignedAgentId;
        ticket.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        if (ticket.AssignedAgentId.HasValue)
        {
            await db.Entry(ticket).Reference(t => t.AssignedAgent).LoadAsync(ct);
        }
        else
        {
            ticket.AssignedAgent = null;
        }

        return TypedResults.Ok(MapDetail(ticket));
    }

    private static async Task<Results<Created<CommentDto>, NotFound>> AddComment(
        Guid id,
        AddCommentRequest request,
        HelpdeskDbContext db,
        ClaimsPrincipalAccessor accessor,
        CancellationToken ct)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null) return TypedResults.NotFound();

        var user = accessor.User;
        var subjectId = user.GetSubjectId();
        var isAgent = user.IsAdmin() || user.IsAgent();

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            Body = request.Body.Trim(),
            IsInternalNote = request.IsInternalNote,
            CreatedAt = DateTimeOffset.UtcNow,
            AuthorAgentId = isAgent && await db.Agents.AnyAsync(a => a.Id == subjectId, ct) ? subjectId : null,
            AuthorUserId = !isAgent && await db.Users.AnyAsync(u => u.Id == subjectId, ct) ? subjectId : null
        };

        // Fall back: if subject isn't found in either table (e.g. admin user), record under AuthorUserId.
        if (comment.AuthorAgentId is null && comment.AuthorUserId is null)
        {
            comment.AuthorUserId = subjectId;
        }

        ticket.UpdatedAt = comment.CreatedAt;
        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);

        await db.Entry(comment).Reference(c => c.AuthorUser).LoadAsync(ct);
        await db.Entry(comment).Reference(c => c.AuthorAgent).LoadAsync(ct);

        var dto = MapComment(comment);
        return TypedResults.Created($"/api/tickets/{ticket.Id}", dto);
    }

    private static TicketDetailDto MapDetail(Ticket ticket) => new(
        ticket.Id,
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
            .Select(MapComment)
            .ToList());

    private static CommentDto MapComment(Comment c) => new(
        c.Id,
        c.TicketId,
        c.AuthorUserId,
        c.AuthorAgentId,
        c.AuthorAgent?.DisplayName ?? c.AuthorUser?.DisplayName ?? "Unknown",
        c.Body,
        c.IsInternalNote,
        c.CreatedAt);
}
