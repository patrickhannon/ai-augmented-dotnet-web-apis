using FluentValidation;
using GlobomaticsHelpdesk.Api.Auth;
using GlobomaticsHelpdesk.Api.Data;
using GlobomaticsHelpdesk.Api.Domain;
using GlobomaticsHelpdesk.Api.Endpoints;
using GlobomaticsHelpdesk.Api.OpenApiTransformers;
using GlobomaticsHelpdesk.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<HelpdeskDbContext>(options =>
    options.UseInMemoryDatabase("GlobomaticsHelpdesk"));

builder.Services.AddSingleton<ApiKeyStore>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ClaimsPrincipalAccessor>();

builder.Services
    .AddAuthentication(ApiKeyAuthenticationOptions.DefaultScheme)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.DefaultScheme, _ => { });

builder.Services
    .AddAuthorization(options => options.AddHelpdeskPolicies());

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var chatProviderEndpoint = GetRequiredAbsoluteUri(builder.Configuration, "ChatProvider:Endpoint");
var chatProviderModel = GetRequiredConfigurationValue(builder.Configuration, "ChatProvider:Model");
var chatProviderApiKey = GetRequiredConfigurationValue(
    builder.Configuration,
    "ChatProvider:ApiKey",
    "Set it via user-secrets.");

builder.Services.AddChatClient(_ =>
{
    var openAiOptions = new OpenAIClientOptions { Endpoint = chatProviderEndpoint };
    return new OpenAI.Chat.ChatClient(
            chatProviderModel,
            new System.ClientModel.ApiKeyCredential(chatProviderApiKey),
            openAiOptions)
        .AsIChatClient();
});

var embeddingProviderEndpoint = GetRequiredAbsoluteUri(builder.Configuration, "EmbeddingProvider:Endpoint");
var embeddingProviderModel = GetRequiredConfigurationValue(builder.Configuration, "EmbeddingProvider:Model");

builder.Services.AddEmbeddingGenerator(services =>
{
    var openAiOptions = new OpenAIClientOptions { Endpoint = embeddingProviderEndpoint };
    return new OpenAI.Embeddings.EmbeddingClient(
            embeddingProviderModel,
            new System.ClientModel.ApiKeyCredential(chatProviderApiKey),
            openAiOptions)
        .AsIEmbeddingGenerator();
});

builder.Services.AddSingleton<VectorStore>(sp =>
    new InMemoryVectorStore(
        sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>()));

const string KnowledgeCollectionName = "knowledge-articles";
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<VectorStore>()
        .GetCollection<Guid, KnowledgeArticleSearchRecord>(KnowledgeCollectionName));

builder.Services.AddHostedService<KnowledgeIngestionService>();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<ApiKeySecuritySchemeTransformer>();
});

builder.Services.AddProblemDetails();

var app = builder.Build();

// Seed the in-memory database on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HelpdeskDbContext>();
    DataSeeder.Seed(db);
}

app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();

app.MapTicketsEndpoints();
app.MapKnowledgeEndpoints();
app.MapChatEndpoints();

app.Run();

static string GetRequiredConfigurationValue(
    IConfiguration config,
    string key,
    string? guidance = null)
{
    var value = config[key];
    if (!string.IsNullOrWhiteSpace(value))
    {
        return value;
    }

    var message = $"{key} is not configured.";
    if (!string.IsNullOrWhiteSpace(guidance))
    {
        message += $" {guidance}";
    }

    throw new InvalidOperationException(message);
}

static Uri GetRequiredAbsoluteUri(IConfiguration config, string key)
{
    var value = GetRequiredConfigurationValue(config, key);
    return Uri.TryCreate(value, UriKind.Absolute, out var uri)
        ? uri
        : throw new InvalidOperationException($"{key} must be an absolute URI.");
}

public partial class Program;
