# Module 2 · Demo — Build the knowledge base vector store

Branch: **`module-2/demo/1-build-knowledge-base`** · Course: **Building AI-Augmented ASP.NET Core 10 Web APIs**

This demo stands up the retrieval half of RAG: it generates **embeddings** for the seeded knowledge base
articles and loads them into an in-memory **vector store** at startup, so the app can later find articles
by *meaning* instead of keywords.

> See [`main`](https://github.com/codemillmatt/ai-augmented-dotnet-web-apis/tree/main) for the course overview. Previous branch: `module-1/demo/3-stream`.

## What Module 2 teaches

The problem: ask an LLM about your organization's internal data and it **confabulates** — it answers
confidently and wrong, because your data was never in its training set. The fix is **Retrieval-Augmented
Generation (RAG)**, built on two ideas introduced in this module:

- **Embeddings** — turn text into a vector of numbers that captures its *meaning*, so two passages about
  the same topic land close together even if they share no words.
- **Vector similarity search** — compare those vectors by distance to find the most relevant content.

This branch builds and verifies the vector store. The next two test it in isolation and then wire it into
the chat endpoint.

## What this demo adds

| Piece | File | Why it matters |
| ----- | ---- | -------------- |
| Search record | `Domain/KnowledgeArticleSearchRecord.cs` | A vector-store model (separate from the EF entity) with `[VectorStoreKey]`, `[VectorStoreData]`, and `[VectorStoreVector(1536)]` |
| Vector store | `Data/InMemoryVectorStore.cs` | A hand-rolled `VectorStore` (from `Microsoft.Extensions.VectorData.Abstractions`) that searches by cosine similarity |
| Ingestion | `Services/KnowledgeIngestionService.cs` | A hosted service that, at startup, loads the 10 articles, maps them to records, and `UpsertAsync`es them |
| DI wiring | `Program.cs` | `AddEmbeddingGenerator(...)`, `AddSingleton<VectorStore>()`, a typed `GetCollection<Guid, KnowledgeArticleSearchRecord>("knowledge-articles")`, and `AddHostedService<KnowledgeIngestionService>()` |
| Config | `appsettings.json` | Adds `EmbeddingProvider:Endpoint` + `EmbeddingProvider:Model` (`text-embedding-3-small`, 1536 dims) |

The key trick: because the record's `[VectorStoreVector(1536)]` property is a **`string`**, the store
generates the embedding for you on upsert and on search — *you never call the embedding generator
directly*. The vector text is `"{Title}\n\n{Body}"`, so the vector captures the whole article.

## What it looks like

![Chat answer grounded in the knowledge base](docs/images/chat-grounded.png)

*Where Module 2 leads: once the vector store you build here is wired into the chat, answers are grounded in your KB articles (GloboConnect/VPN, MFA, SSO) — and the model says so when the KB runs out.*

## Setup

From here on you need **two** Azure AI Foundry deployments: your chat model **and** an **embedding** model
(`text-embedding-3-small`). Set both in `appsettings.json` (`ChatProvider` / `EmbeddingProvider`). The
embedding client reuses the chat API key, so you still set only one secret:

```bash
cd src/GlobomaticsHelpdesk.Api
dotnet user-secrets set "ChatProvider:ApiKey" "<your-foundry-api-key>"
```

> Full setup, including how to create the embedding deployment, is on the [`main`](https://github.com/codemillmatt/ai-augmented-dotnet-web-apis/tree/main) branch.

## Run it

```bash
aspire run
```

At startup you should see the ingestion service log that it loaded the knowledge articles into the vector
store. If the app fails to start, check that your `EmbeddingProvider` endpoint/model and API key are set.

## Try it

The store is verified through a diagnostic endpoint and the web UI (fully explored in the next branch).
The takeaway to confirm here: the app starts, embeds all 10 articles, and is ready to search by meaning.

## Previous
[`module-1/demo/3-stream`](https://github.com/codemillmatt/ai-augmented-dotnet-web-apis/tree/module-1/demo/3-stream) — streaming chat, not yet grounded in your data.

## Next
[`module-2/demo/2-test-vector-search`](https://github.com/codemillmatt/ai-augmented-dotnet-web-apis/tree/module-2/demo/2-test-vector-search) — prove semantic search finds the right article.
