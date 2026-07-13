using System.Text;
using Microsoft.Extensions.AI;

namespace GlobomaticsHelpdesk.Api.Middleware;

/// <summary>
/// Chat client middleware that inspects model output before it reaches the caller.
/// Detects leaked system prompt content, raw internal tool data, and off-topic responses.
/// </summary>
public sealed class OutputFilteringChatClient : DelegatingChatClient
{
    private const string SafeFallbackMessage =
        "I'm sorry, I wasn't able to generate an appropriate response. " +
        "Please try rephrasing your helpdesk question.";

    private static readonly string[] SystemPromptLeakPatterns =
    [
        "you are an it helpdesk assistant",
        "my system prompt",
        "my instructions are",
        "here is my system prompt",
        "my programming tells me",
        "as an ai, my instructions",
        "important rules you must always follow"
    ];

    private static readonly string[] RawToolDataPatterns =
    [
        "internal tool details",
        "aifunction",
        "ticketdetaildto",
        "ticketsummarydto",
        "commentdto",
        "ticketid",
        "requesterid",
        "assignedagentid",
        "authoruserid",
        "authoragentid",
        "isinternalnote",
        "serviceprovider.getrequiredservice"
    ];

    private static readonly string[] OffTopicResponsePatterns =
    [
        "here is a poem",
        "here's a poem",
        "once upon a time",
        "here is a recipe",
        "here's a recipe",
        "as a stock analyst",
        "here is the code",
        "here's the code"
    ];

    public OutputFilteringChatClient(IChatClient innerClient) : base(innerClient)
    {
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await base.GetResponseAsync(messages, options, 
            cancellationToken);

        if (ContainsBlockedContent(response.Text))
        {
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, 
                SafeFallbackMessage));
        }

        return response;
    }

    // For streaming, we buffer the full response to inspect it before yielding.
    // This trades some latency for safety — the alternative (inspecting each chunk)
    // risks false positives on partial text. In production you might use a hybrid
    // approach, but for clarity this buffers then yields.
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var updates = new List<ChatResponseUpdate>();
        var fullText = new StringBuilder();

        await foreach (var update in base.GetStreamingResponseAsync(messages, 
            options, cancellationToken))
        {
            updates.Add(update);
            if (!string.IsNullOrEmpty(update.Text))
            {
                fullText.Append(update.Text);
            }
        }

        if (ContainsBlockedContent(fullText.ToString()))
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, 
                SafeFallbackMessage);
            yield break;
        }

        foreach (var update in updates)
        {
            yield return update;
        }
    }

    private static bool ContainsBlockedContent(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        return ContainsAny(text, SystemPromptLeakPatterns)
            || ContainsAny(text, RawToolDataPatterns)
            || ContainsAny(text, OffTopicResponsePatterns);
    }

    private static bool ContainsAny(string text, IEnumerable<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            if (text.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

public static class OutputFilteringChatClientExtensions
{
    public static ChatClientBuilder UseOutputFiltering(this ChatClientBuilder builder)
    {
        return builder.Use(innerClient => new OutputFilteringChatClient(innerClient));
    }
}
