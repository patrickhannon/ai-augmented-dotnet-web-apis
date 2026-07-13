using Microsoft.AspNetCore.Authentication;

namespace GlobomaticsHelpdesk.Api.Auth;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public const string HeaderName = "X-Api-Key";
}
