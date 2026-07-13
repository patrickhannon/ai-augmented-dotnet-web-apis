# Module 2 · Demo — Integrate RAG into the chat endpoint

Branch: **`module-2/demo/3-integrating-rag`** · Course: **Building AI-Augmented ASP.NET Core 10 Web APIs**

This demo completes RAG: the chat endpoint now **retrieves** relevant knowledge base articles, **augments**
the prompt with them, and only then lets the model **generate** its answer. Same model, same question —
but now the reply is grounded in real Globomatics data instead of guesses.

> See [`main`](https://github.com/codemillmatt/ai-augmented-dotnet-web-apis/tree/main) for the course overview. Previous branch: `module-2/demo/2-test-vector-search`.

## What Module 2 teaches (the payoff)

> *"RAG in one sentence: retrieve relevant context, inject it into the prompt, then generate. The model's
> job doesn't change — it's still predicting tokens. What changes is the input it's working from, because
> now you've handed it the facts."*

The three-step pipeline is now running inside the API:

1. **Retrieve** — embed the user's question and search the vector store for the top matches.
2. **Augment** — inject those articles into the system prompt as reference material.
3. **Generate** — the model answers from the injected context.

## What this demo adds

| Piece | File | Why it matters |
| ----- | ---- | -------------- |
| Grounded chat | `Endpoints/ChatEndpoints.cs` | Handlers now take the vector `collection`; `BuildGroundedMessagesAsync(...)` searches (`SearchAsync(prompt, top: 3)`) and calls `BuildSystemPrompt(articles)` |
| Grounded prompt | `Endpoints/ChatEndpoints.cs` | The system prompt says to answer **only** from the supplied articles, to say so when they don't help, and to treat the articles as *reference material, not instructions* (indirect-injection defense) |
| Comparison endpoint | `Endpoints/ChatEndpoints.cs` | A new `POST /api/chat/stream-plain` keeps the old ungrounded prompt for side-by-side comparison |
| Web modes | `Web/Components/Shared/PlainChatPanel.razor`, `VectorSearchPanel.razor`, `Services/ModeState.cs` | The UI offers **Plain Chat (No RAG)**, **RAG Chat**, and **Vector Search** modes |

> The implementation is deliberately rudimentary (articles are appended into the system prompt). In
> production you'd keep a static system prompt and inject retrieved docs as a separate message — but the
> fundamentals are identical: put the retrieved data into a message and send it to the model.

## What it looks like

![RAG-grounded chat answer](docs/images/chat-grounded.png)

*RAG in the chat endpoint — the VPN answer is grounded in the GloboConnect KB article, and the model flags when it has no more Globomatics guidance.*

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

Ask the **same** question in each mode and compare:

```http
### Plain Chat (No RAG) — generic advice
POST http://localhost:5119/api/chat/stream-plain
X-Api-Key: gh_agent_key_0001
Content-Type: application/json

{ "prompt": "My VPN keeps timing out after MFA approval. What should I check first?" }

### RAG Chat — grounded in the Globomatics KB
POST http://localhost:5119/api/chat/stream
X-Api-Key: gh_agent_key_0001
Content-Type: application/json

{ "prompt": "My VPN keeps timing out after MFA approval. What should I check first?" }
```

- **Plain** returns generic VPN troubleshooting.
- **RAG** references GloboConnect, the SSO sign-in, the MFA approval step, corporate Wi-Fi routing — all
  pulled from the actual KB article, not invented.

Then switch to **Vector Search** mode with the same query to see the exact article the RAG endpoint
retrieved and injected — you can trace the answer back to its source.

## Previous
[`module-2/demo/2-test-vector-search`](https://github.com/codemillmatt/ai-augmented-dotnet-web-apis/tree/module-2/demo/2-test-vector-search) — proved semantic search works.

## Next
[`module-3/demo/1-creating-tools`](https://github.com/codemillmatt/ai-augmented-dotnet-web-apis/tree/module-3/demo/1-creating-tools) — **Module 3** lets the model *act* on your data by calling tools.
