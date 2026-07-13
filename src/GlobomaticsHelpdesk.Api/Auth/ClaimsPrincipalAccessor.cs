using System.Security.Claims;

namespace GlobomaticsHelpdesk.Api.Auth;

// Convenience DI wrapper around IHttpContextAccessor so endpoint handlers
// can ask for the current ClaimsPrincipal without taking IHttpContextAccessor directly.
public class ClaimsPrincipalAccessor
{
    private readonly IHttpContextAccessor _accessor;

    public ClaimsPrincipalAccessor(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public ClaimsPrincipal User =>
        _accessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
}
