using GlobomaticsHelpdesk.Api.Auth;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GlobomaticsHelpdesk.Api.OpenApiTransformers;

public class ApiKeySecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[ApiKeyAuthenticationOptions.DefaultScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = ApiKeyAuthenticationOptions.HeaderName,
            Description = "API key. Send the key in the X-Api-Key header. Seeded keys: gh_admin_key_0001 (admin), gh_agent_key_0001 (agent), gh_readonly_key_0001 (read-only)."
        };

        var schemeReference = new OpenApiSecuritySchemeReference(
            ApiKeyAuthenticationOptions.DefaultScheme,
            document,
            null);

        var requirement = new OpenApiSecurityRequirement
        {
            [schemeReference] = new List<string>()
        };

        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(requirement);

        return Task.CompletedTask;
    }
}
