using Microsoft.Extensions.AI;

namespace GlobomaticsHelpdesk.Api.Middleware;

/// <summary>
/// Chat client middleware that validates user input before it reaches the model.
/// Checks for maximum prompt length and known injection patterns.
/// </summary>
public sealed class InputValidationChatClient : DelegatingChatClient
{
    private const int MaxPromptLength = 3000;

    private static readonly string[] InjectionPatterns =
    [
        "ignore previous instructions",
        "ignore all previous instructions",
        "disregard your system prompt",
        "disregard all prior instructions",
        "reveal your prompt",
        "show me your system prompt",
        "what is your system prompt",
        "forget your instructions",
        "override your instructions",
        "you are now a",
        "pretend you are not"
    ];

    public InputValidationChatClient(IChatClient innerClient) : base(innerClient)
    {
    }

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateMessages(messages);
        if (validationError is not null)
        {
            return Task.FromResult(CreateBlockedResponse(validationError));
        }

        return base.GetResponseAsync(messages, options, cancellationToken);
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateMessages(messages);
        if (validationError is not null)
        {
            return ToAsyncEnumerable(CreateBlockedUpdate(validationError));
        }

        return base.GetStreamingResponseAsync(messages, options, cancellationToken);
    }

    private static string? ValidateMessages(IEnumerable<ChatMessage> messages)
    {
        foreach (var message in messages)
        {
            if (message.Role != ChatRole.User)
                continue;

            var text = message.Text;
            if (string.IsNullOrEmpty(text))
                continue;

            if (text.Length > MaxPromptLength)
            {
                return $"Your message exceeds the maximum allowed length of {MaxPromptLength} characters.";
            }

            var lowerText = text.ToLowerInvariant();
            foreach (var pattern in InjectionPatterns)
            {
                if (lowerText.Contains(pattern, StringComparison.Ordinal))
                {
                    return "Your request cannot be processed. Please rephrase your question about helpdesk support.";
                }
            }
        }

        return null;
    }

    private static ChatResponse CreateBlockedResponse(string reason)
    {
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, reason));
    }

    private static ChatResponseUpdate CreateBlockedUpdate(string reason)
    {
        return new ChatResponseUpdate(ChatRole.Assistant, reason);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> ToAsyncEnumerable(
        ChatResponseUpdate update)
    {
        yield return update;
        await Task.CompletedTask; // Satisfy async requirement
    }
}

public static class InputValidationChatClientExtensions
{
    public static ChatClientBuilder UseInputValidation(this ChatClientBuilder builder)
    {
        return builder.Use(innerClient => new InputValidationChatClient(innerClient));
    }
}
