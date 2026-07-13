using Microsoft.AspNetCore.Authorization;

namespace GlobomaticsHelpdesk.Api.Auth;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string AgentOrAdmin = "AgentOrAdmin";
    public const string AnyAuthenticated = "AnyAuthenticated";

    public static AuthorizationOptions AddHelpdeskPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(AdminOnly, p => p.RequireRole(Roles.Admin));
        options.AddPolicy(AgentOrAdmin, p => p.RequireRole(Roles.Agent, Roles.Admin));
        options.AddPolicy(AnyAuthenticated, p => p.RequireAuthenticatedUser());
        return options;
    }
}

public static class ClaimsPrincipalExtensions
{
    public static Guid GetSubjectId(this System.Security.Claims.ClaimsPrincipal user)
    {
        var raw = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    public static string? GetRole(this System.Security.Claims.ClaimsPrincipal user)
        => user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

    public static bool IsAdmin(this System.Security.Claims.ClaimsPrincipal user)
        => user.IsInRole(Roles.Admin);

    public static bool IsAgent(this System.Security.Claims.ClaimsPrincipal user)
        => user.IsInRole(Roles.Agent);
}
