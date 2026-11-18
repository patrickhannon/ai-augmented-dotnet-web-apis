using FluentValidation;
using GlobomaticsHelpdesk.Api.Auth;
using GlobomaticsHelpdesk.Api.Data;
using GlobomaticsHelpdesk.Api.Endpoints;
using GlobomaticsHelpdesk.Api.OpenApiTransformers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
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

static string GetRequiredConfigurationValue(IConfiguration configuration, string key, string? guidance = null)
{
    var value = configuration[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Configuration '{key}' is required. {guidance}".TrimEnd());
    }

    return value;
}

static Uri GetRequiredAbsoluteUri(IConfiguration configuration, string key)
{
    var value = GetRequiredConfigurationValue(configuration, key);
    if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
    {
        throw new InvalidOperationException($"Configuration '{key}' must be an absolute URI.");
    }

    return uri;
}

public partial class Program;
