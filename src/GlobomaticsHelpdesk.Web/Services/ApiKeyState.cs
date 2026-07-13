namespace GlobomaticsHelpdesk.Web.Services;

public record ApiKeyOption(string Label, string Role, string Identity, string Key);

public class ApiKeyState
{
    public static readonly IReadOnlyList<ApiKeyOption> Options =
    [
        new("Admin — Avery Admin", "admin", "Avery Admin", "gh_admin_key_0001"),
        new("Agent — Riley Reynolds", "agent", "Riley Reynolds", "gh_agent_key_0001"),
        new("Read-only — Quinn Viewer", "read-only", "Quinn Viewer", "gh_readonly_key_0001"),
        new("(none — unauthenticated)", "anonymous", "n/a", ""),
    ];

    private ApiKeyOption _selected = Options[0];

    public ApiKeyOption Selected
    {
        get => _selected;
        set
        {
            if (_selected == value) return;
            _selected = value;
            OnChanged?.Invoke();
        }
    }

    public string CurrentKey => _selected.Key;

    public event Action? OnChanged;
}
