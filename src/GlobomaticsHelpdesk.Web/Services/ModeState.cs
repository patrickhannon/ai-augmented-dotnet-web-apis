namespace GlobomaticsHelpdesk.Web.Services;

public enum AppMode
{
    WithTools,
    WithoutTools
}

public class ModeState
{
    private AppMode _current = AppMode.WithTools;

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
        (AppMode.WithTools, "Streaming + Tools"),
        (AppMode.WithoutTools, "Streaming (no tools)"),
    ];
}
