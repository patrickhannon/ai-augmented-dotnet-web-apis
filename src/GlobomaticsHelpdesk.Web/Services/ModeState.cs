namespace GlobomaticsHelpdesk.Web.Services;

public enum AppMode
{
    Chat,
    VectorSearch,
    KnowledgeBase
}

public class ModeState
{
    private AppMode _current = AppMode.Chat;

    public AppMode Current
    {
        get => _current;
        set
        {
            if (_current == value) return;
            _current = value;
            OnChanged?.Invoke();
        }
    }

    public event Action? OnChanged;

    public static readonly (AppMode Mode, string Label)[] Options =
    [
        (AppMode.Chat, "Chat"),
        (AppMode.VectorSearch, "Vector Search"),
        (AppMode.KnowledgeBase, "Knowledge Base"),
    ];
}
