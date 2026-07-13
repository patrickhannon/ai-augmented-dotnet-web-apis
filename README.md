# Module 3 · Demo — Create a tool the model can call

Branch: **`module-3/demo/1-creating-tools`** · Course: **Building AI-Augmented ASP.NET Core 10 Web APIs**

This demo gives the model its first **tool**: a function it can decide to call to fetch live data from your
API. RAG lets the model *read* your documents; tool calling lets it *reach into your running system* — look
up a specific ticket, and (in the next branch) take action.

> See [`main`](https://github.com/codemillmatt/ai-augmented-dotnet-web-apis/tree/main) for the course overview. Previous branch: `module-2/demo/3-integrating-rag`.

## What Module 3 teaches

> *"The model's stuck behind a wall of text, and tool calling is how we let it reach through. The model
> asks, your code runs, the result goes back. That's the whole pattern."*

The mechanics:

- You describe a method with `[Description]` attributes; the model reads those to decide **when** to call
  it and **what** to pass.
- `AIFunctionFactory.Create(...)` turns the method into an `AIFunction`; you hand it to the model via
  `ChatOptions.Tools`.
- `.UseFunctionInvocation()` adds the `FunctionInvokingChatClient` middleware that runs the loop
  automatically: model asks → your function runs → result goes back → repeat until it can answer.

## What this demo adds

| Piece | File | Why it matters |
| ----- | ---- | -------------- |
| The tool | `Services/HelpdeskTools.cs` | `GetTicketByIdAsync(...)` — an instance method with `[Description]` on the method and its parameter; uses `IServiceProvider` to resolve a scoped `DbContext` |
| Tool wiring | `Endpoints/ChatEndpoints.cs` | `AIFunctionFactory.Create(tools.GetTicketByIdAsync, "GetTicket")`, passed via `ChatOptions { Tools = [...] }`; a `useTools` query flag (default `true`) toggles it on/off |
| Pipeline | `Program.cs` | `AddSingleton<HelpdeskTools>()` and `.UseFunctionInvocation()` on the chat client |

Note that `IServiceProvider` and `CancellationToken` are auto-bound by the framework — the model never sees
them in the tool's schema.

## What it looks like

![Model calling a tool to read ticket data](docs/images/chat-tools.png)

*Function calling in action: asked to summarize ticket 1001, the model calls a tool to read live data (status, priority, assignee, latest comment) and answers from the result.*

## Setup

Chat + embedding Foundry deployments and the `ChatProvider:ApiKey` user secret on
`src/GlobomaticsHelpdesk.Api`:

```bash
cd src/GlobomaticsHelpdesk.Api
dotnet user-secrets set "ChatProvider:ApiKey" "<your-foundry-api-key>"
```

## Run it

```bash
aspire run
```

Open **web** from the Aspire dashboard.

## Try it

```http
POST http://localhost:5119/api/chat/stream
X-Api-Key: gh_agent_key_0001
Content-Type: application/json

{ "prompt": "What's the status of ticket 1001? Who is it assigned to?" }
```

**Watch for the pause.** With tools on, the token stream briefly pauses while `GetTicketByIdAsync` runs,
then resumes with the *real* ticket 1001 data ("Cannot connect to VPN from home", Open, assigned to Riley
Reynolds). Turn tools off (`?useTools=false`) and ask the same thing — no pause, and the model has to
guess or admit it can't. A pure knowledge question like *"How do I connect to the VPN?"* triggers **no**
tool call — that's still RAG.

## Previous
[`module-2/demo/3-integrating-rag`](https://github.com/codemillmatt/ai-augmented-dotnet-web-apis/tree/module-2/demo/3-integrating-rag) — the model now answers from your data.

## Next
[`module-3/demo/2-exposing-tools`](https://github.com/codemillmatt/ai-augmented-dotnet-web-apis/tree/module-3/demo/2-exposing-tools) — add more tools, including a write operation, with validation and authorization guardrails.
