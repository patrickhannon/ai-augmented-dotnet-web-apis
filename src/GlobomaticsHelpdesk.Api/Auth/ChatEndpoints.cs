using GlobomaticsHelpdesk.Api.Contracts;
using GlobomaticsHelpdesk.Api.Validation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.AI;

namespace GlobomaticsHelpdesk.Api.Auth;

public static class ChatEndpoints
{
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

        var response = await chatClient.GetResponseAsync(
            messages,
            cancellationToken: ct);

        return TypedResults.Ok(
            new ChatReplyDto(response.Text));
    }
}
