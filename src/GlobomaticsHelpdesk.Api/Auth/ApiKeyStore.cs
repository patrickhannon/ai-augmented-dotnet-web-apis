namespace GlobomaticsHelpdesk.Api.Auth;

public static class Roles
{
    public const string Admin = "admin";
    public const string Agent = "agent";
    public const string ReadOnly = "read-only";
}

public record ApiKeyPrincipal(
    string ApiKey,
    Guid SubjectId,
    string DisplayName,
    string Role,
    bool IsAgent);

public class ApiKeyStore
{
    // Seeded keys — these are intentionally well-known and visible so the
    // Pluralsight curriculum can demo the API without extra configuration.
    public const string AdminKey = "gh_admin_key_0001";
    public const string AgentKey = "gh_agent_key_0001";
    public const string ReadOnlyKey = "gh_readonly_key_0001";

    private readonly Dictionary<string, ApiKeyPrincipal> _keys;

    public ApiKeyStore()
    {
        _keys = new Dictionary<string, ApiKeyPrincipal>(StringComparer.Ordinal)
        {
            [AdminKey] = new ApiKeyPrincipal(
                AdminKey,
                SeededIdentities.AdminUserId,
                "Avery Admin",
                Roles.Admin,
                IsAgent: false),
            [AgentKey] = new ApiKeyPrincipal(
                AgentKey,
                SeededIdentities.PrimaryAgentId,
                "Riley Reynolds",
                Roles.Agent,
                IsAgent: true),
            [ReadOnlyKey] = new ApiKeyPrincipal(
                ReadOnlyKey,
                SeededIdentities.ReadOnlyUserId,
                "Quinn Viewer",
                Roles.ReadOnly,
                IsAgent: false),
        };
    }

    public bool TryGet(string apiKey, out ApiKeyPrincipal principal)
    {
        return _keys.TryGetValue(apiKey, out principal!);
    }

    public IEnumerable<ApiKeyPrincipal> All => _keys.Values;
}

public static class SeededIdentities
{
    // Stable ids so seeded data + auth always agree.
    public static readonly Guid AdminUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ReadOnlyUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid PrimaryAgentId = Guid.Parse("33333333-3333-3333-3333-333333333333");
}
