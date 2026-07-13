namespace GlobomaticsHelpdesk.Web.Services;

public enum ChatMode
{
    Streaming,
    NonStreaming
}

public class ChatModeState
{
    private ChatMode _mode = ChatMode.Streaming;

    public ChatMode Mode
    {
        get => _mode;
        set
        {
            if (_mode == value) return;
            _mode = value;
            OnChanged?.Invoke();
        }
    }

    public bool IsStreaming => _mode == ChatMode.Streaming;

    public event Action? OnChanged;
}
