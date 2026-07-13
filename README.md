# Module 4 · Demo — Expose tools over an MCP server

Branch: **`module-4/demo/1-create-mcp-server`** · Course: **Building AI-Augmented ASP.NET Core 10 Web APIs**

This demo turns the helpdesk's tools into an **MCP server**, so *external* AI agent clients — VS Code
Copilot, the Copilot CLI, Claude, or any custom agent — can discover and call them over an open standard,
with no per-client integration code.

> See [`main`](https://github.com/codemillmatt/ai-augmented-dotnet-web-apis/tree/main) for the course overview. Previous branch: `module-3/demo/2-exposing-tools`.

## What Module 4 teaches

> *"MCP — the Model Context Protocol — is an open standard for how an agent client talks to a tool server.
> You build the server once and any MCP client connects."*

MCP gives you three things the raw tool calls from Module 3 didn't:

- **Tool discovery** — a client asks "what can you do?" and gets every tool's name, description, and JSON
  schema at runtime.
- **Structured invocation** — clients send a tool name plus typed arguments and get structured results
  back.
- **Protocol reuse** — one server, many clients. If it speaks MCP, it can use your tools.

## What this demo adds

| Piece | File | Why it matters |
| ----- | ---- | -------------- |
| MCP tools | `McpServer/HelpdeskMcpTools.cs` | A `[McpServerToolType]` class exposing `get-ticket-by-id`, `get-tickets-by-status`, and `update-ticket-priority` via `[McpServerTool]` + `[Description]`; it delegates to the same `HelpdeskTools` used by the chat endpoint |
| Server wiring | `Program.cs` | `AddMcpServer().WithHttpTransport().WithToolsFromAssembly()`, CORS for browser/extension clients, and `app.MapMcp("/mcp")` |
| Package | `GlobomaticsHelpdesk.Api.csproj` | `ModelContextProtocol.AspNetCore` |

The business logic isn't duplicated — the MCP tools are a thin, discoverable façade over the tool class you
already built. The `[Description]` attributes are what drive tool selection in every client.

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

The MCP server is served at **`http://localhost:5119/mcp`** (Streamable HTTP transport). Browse to that URL
to confirm it's reachable.

## Try it — connect an MCP client

Point any MCP client at `http://localhost:5119/mcp`, then ask it about tickets in natural language.

**VS Code + GitHub Copilot** — create `.vscode/mcp.json` (root key is `servers`):

```json
{ "servers": { "globomatics-helpdesk": { "type": "http", "url": "http://localhost:5119/mcp" } } }
```

**GitHub Copilot CLI** — in `~/.copilot/mcp-config.json` (root key is `mcpServers`):

```json
{ "mcpServers": { "globomatics-helpdesk": { "type": "http", "url": "http://localhost:5119/mcp" } } }
```

**Claude Code** — `claude mcp add --transport http globomatics-helpdesk http://localhost:5119/mcp`

Then try prompts like:

- *"What's the status of ticket 1001? Who's it assigned to?"* → calls `get-ticket-by-id`
- *"Show me all open tickets"* → calls `get-tickets-by-status`
- *"Set ticket 1006 to urgent priority"* → calls `update-ticket-priority`
- *"Update ticket 9999 to high priority"* → tool returns a "not found" error, handled gracefully

The same server answers all three clients — you write zero client-specific code.

## Previous
[`module-3/demo/2-exposing-tools`](https://github.com/codemillmatt/ai-augmented-dotnet-web-apis/tree/module-3/demo/2-exposing-tools) — tools wired directly into the chat endpoint.

## Next
[`module-5/demo/1-hardening-endpoints`](https://github.com/codemillmatt/ai-augmented-dotnet-web-apis/tree/module-5/demo/1-hardening-endpoints) — **Module 5** hardens the endpoints and adds observability.
