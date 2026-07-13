namespace GlobomaticsHelpdesk.Web.Services;

public enum AppMode
{
    Chat,
    PlainChat,
    VectorSearch
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
        (AppMode.Chat, "RAG Chat"),
        (AppMode.PlainChat, "Plain Chat"),
        (AppMode.VectorSearch, "Vector Search"),
    ];
}
