using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GlobomaticsHelpdesk.Api.Auth;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    public const string IsAgentClaim = "gh:is_agent";

    private readonly ApiKeyStore _store;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ApiKeyStore store)
        : base(options, logger, encoder)
    {
        _store = store;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationOptions.HeaderName, out var headerValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var apiKey = headerValues.ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing API key."));
        }

        if (!_store.TryGet(apiKey, out var principal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, principal.SubjectId.ToString()),
            new(ClaimTypes.Name, principal.DisplayName),
            new(ClaimTypes.Role, principal.Role),
            new(IsAgentClaim, principal.IsAgent ? "true" : "false")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers["WWW-Authenticate"] = $"ApiKey realm=\"GlobomaticsHelpdesk\", header=\"{ApiKeyAuthenticationOptions.HeaderName}\"";
        return Task.CompletedTask;
    }
}
