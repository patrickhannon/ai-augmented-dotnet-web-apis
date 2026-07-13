# Module 2 · Demo — Test vector search

Branch: **`module-2/demo/2-test-vector-search`** · Course: **Building AI-Augmented ASP.NET Core 10 Web APIs**

This demo proves the vector store works *before* wiring it into anything. It exposes a diagnostic search
endpoint and a Blazor panel so you can type a query and watch semantic search return the right articles —
even when your words don't match the article's words.

> See [`main`](https://github.com/codemillmatt/ai-augmented-dotnet-web-apis/tree/main) for the course overview. Previous branch: `module-2/demo/1-build-knowledge-base`.

## What Module 2 teaches (this step)

Retrieval is the "R" in RAG, and it's worth validating on its own. The lesson here: **you're matching on
meaning, not on spelling.** A query and an article can share zero keywords and still be the top match,
because their embeddings are close. Similarity search also returns a *ranked* list with scores — relevance
is a spectrum, not a yes/no.

## What this demo adds

| Piece | File | Why it matters |
| ----- | ---- | -------------- |
| Search endpoint | `Endpoints/KnowledgeEndpoints.cs` | `GET /api/knowledge/search-test?query=...` calls `collection.SearchAsync(query, top: 3)` and returns titles, snippets, and similarity scores |
| Search DTOs | `Contracts/KnowledgeDtos.cs` | Shapes the search results |
| Web panels | `Web/Components/Shared/VectorSearchPanel.razor`, `ModePicker.razor`, `Services/ModeState.cs` | A mode switch adds a **Vector Search** view that hits the diagnostic endpoint and shows ranked results |

This diagnostic endpoint is **not throwaway scaffolding** — it stays in the app and is used again in the
next branch to reveal exactly which articles the chat endpoint retrieved.

## What it looks like

![Grounded chat answer from retrieved KB articles](docs/images/chat-grounded.png)

*Retrieval end to end: the VPN answer is assembled from the top-matching KB articles, and when nothing else matches the model stops instead of inventing steps.*

## Setup

Same as the previous branch — chat + embedding Foundry deployments and the `ChatProvider:ApiKey` user
secret on `src/GlobomaticsHelpdesk.Api`:

```bash
cd src/GlobomaticsHelpdesk.Api
dotnet user-secrets set "ChatProvider:ApiKey" "<your-foundry-api-key>"
```

## Run it

```bash
aspire run
```

Open **web** from the Aspire dashboard and switch to **Vector Search** mode.

## Try it

Try queries whose wording differs from the article titles and watch the ranking:

```http
GET http://localhost:5119/api/knowledge/search-test?query=my VPN keeps timing out
X-Api-Key: gh_agent_key_0001

### zero keyword overlap, still the right hit
GET http://localhost:5119/api/knowledge/search-test?query=can't get email on my phone
X-Api-Key: gh_agent_key_0001
```

- *"my VPN keeps timing out"* → **Connecting to the Globomatics VPN**
- *"can't get email on my phone"* → **Email is not syncing on mobile** (no shared keywords!)

Each result carries a similarity score so you can see how confident the match is.

## Previous
[`module-2/demo/1-build-knowledge-base`](https://github.com/codemillmatt/ai-augmented-dotnet-web-apis/tree/module-2/demo/1-build-knowledge-base) — build and embed the Globomatics knowledge base.

## Next
[`module-2/demo/3-integrating-rag`](https://github.com/codemillmatt/ai-augmented-dotnet-web-apis/tree/module-2/demo/3-integrating-rag) — inject the retrieved articles into the chat prompt.
