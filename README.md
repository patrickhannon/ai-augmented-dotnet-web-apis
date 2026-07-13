# Module 5 · Demo — Add telemetry to the AI endpoint

Branch: **`module-5/demo/2-adding-telemetry`** · Course: **Building AI-Augmented ASP.NET Core 10 Web APIs**

This demo makes the AI pipeline **observable**. With one line of middleware, every request emits
OpenTelemetry traces and metrics — token usage, model latency, and the tool-calling loop — visible in the
Aspire dashboard with no manual instrumentation in your endpoint code.

> See [`main`](https://github.com/codemillmatt/ai-augmented-dotnet-web-apis/tree/main) for the course overview. Previous branch: `module-5/demo/1-hardening-endpoints`.

## What Module 5 teaches (observability)

> *"A traditional API gives you response times and error rates. An AI endpoint has a lot more going on, and
> it pays to keep tabs on it."*

Tracking input/output **tokens** per request is what lets you tune prompts and retrieval and keep the bill
down. `UseOpenTelemetry()` follows the OpenTelemetry **semantic conventions for generative AI**, so every
attribute has a standard name any backend can read.

## What this demo adds

| Piece | File | Why it matters |
| ----- | ---- | -------------- |
| Telemetry middleware | `Program.cs` | `.UseOpenTelemetry(sourceName: AiActivitySourceName, configure: c => c.EnableSensitiveData = true)` added to the chat pipeline |
| Source/meter registration | `Program.cs` | An `AiActivitySourceName` constant (`"GlobomaticsHelpdesk.AI"`) wired into `.AddSource(...)` and `.AddMeter(...)` — the same name in all three places |
| Exporter | `GlobomaticsHelpdesk.ServiceDefaults/Extensions.cs` | Exports over OTLP to the Aspire dashboard when `OTEL_EXPORTER_OTLP_ENDPOINT` is set (the AppHost sets it) |

**Placement matters:** `.UseOpenTelemetry()` goes **innermost**, just above the model — that's what lets
the function-invoking client emit its grouping span so you get a per-round-trip breakdown inside each
request.

> `EnableSensitiveData = true` puts the actual prompt/response text into traces — great for a demo, but
> **turn it off in production** so you don't log customer messages.

## What it looks like

![Aspire dashboard trace of an AI request](docs/images/dashboard-trace.png)

*One request in the Aspire dashboard — the model call, the `SearchKnowledgeBaseAsync` tool execution, and the follow-up model call, each a timed span with its outbound HTTP call to the model.*

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

Run via the AppHost (as above) so telemetry is exported to the dashboard. Open the **dashboard** and keep
the **Traces** and **Metrics** tabs handy.

## Try it

Send a request that triggers both RAG and a tool call, then inspect the trace:

```http
POST http://localhost:5119/api/chat/stream
X-Api-Key: gh_admin_key_0001
Content-Type: application/json
{ "prompt": "I'm having VPN issues. Also, what's the current status of ticket 1001?" }
```

In the dashboard's **Traces** tab, open that request and read the span tree top-to-bottom:

- the top-level `POST /api/chat/stream` HTTP span,
- a grouping span for the tool-calling loop,
- a `chat` span per model round-trip, each with `gen_ai.request.model`,
  `gen_ai.usage.input_tokens`, and `gen_ai.usage.output_tokens`.

Because `EnableSensitiveData` is on, you can see the model's tool request in one span's output and the
tool's result in the next span's input — the reasoning chain, laid out. Then send the blunt injection
prompt from the previous branch: its trace is just the HTTP span with **no** nested AI spans — validation
stopped it before the model. In **Metrics**, select the `GlobomaticsHelpdesk.AI` meter to see token-usage
and operation-duration histograms.

## Previous
[`module-5/demo/1-hardening-endpoints`](https://github.com/codemillmatt/ai-augmented-dotnet-web-apis/tree/module-5/demo/1-hardening-endpoints) — input/output guardrails added.

## Next
[`module-6/demo/1-triage-endpoint`](https://github.com/codemillmatt/ai-augmented-dotnet-web-apis/tree/module-6/demo/1-triage-endpoint) — **Module 6** puts everything together into an agentic triage endpoint.
