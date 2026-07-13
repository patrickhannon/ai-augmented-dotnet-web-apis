using System.Runtime.CompilerServices;
using GlobomaticsHelpdesk.Api.Auth;
using GlobomaticsHelpdesk.Api.Contracts;
using GlobomaticsHelpdesk.Api.Validation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.AI;

namespace GlobomaticsHelpdesk.Api.Endpoints;

public static class ChatEndpoints
{
    // Hard-coded for Module 1. Module 2 replaces this with grounded prompts.
    private const string HelpdeskSystemPrompt =
        "You are an IT helpdesk assistant for Globomatics. " +
        "Be brief, practical, and direct. " +
        "If you don't know something, say so rather than guessing.";

    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chat")
            .WithTags("Chat")
            .RequireAuthorization(AuthorizationPolicies.AnyAuthenticated);

        group.MapPost("/", Chat)
            .AddEndpointFilter<ValidationFilter<ChatRequest>>()
            .WithName("Chat")
            .WithSummary("Send a prompt to the LLM and receive the full reply.")
            .Produces<ChatReplyDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/stream", ChatStream)
            .AddEndpointFilter<ValidationFilter<ChatRequest>>()
            .WithName("ChatStream")
            .WithSummary("Send a prompt to the LLM and stream the reply via SSE.");

        return app;
    }

    private static async Task<Ok<ChatReplyDto>> Chat(
        ChatRequest request,
        IChatClient chatClient,
        CancellationToken ct)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, HelpdeskSystemPrompt),
            new(ChatRole.User, request.Prompt)
        };

        var response = await chatClient.GetResponseAsync(messages, cancellationToken: ct);
        return TypedResults.Ok(new ChatReplyDto(response.Text));
    }

    private static ServerSentEventsResult<string> ChatStream(
        ChatRequest request,
        IChatClient chatClient,
        CancellationToken ct)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, HelpdeskSystemPrompt),
            new(ChatRole.User, request.Prompt)
        };

        return TypedResults.ServerSentEvents(StreamTextDeltas(chatClient, messages, ct));
    }

    private static async IAsyncEnumerable<string> StreamTextDeltas(
        IChatClient chatClient,
        IList<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var update in chatClient.GetStreamingResponseAsync(messages, cancellationToken: ct))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return update.Text;
            }
        }
    }
}
