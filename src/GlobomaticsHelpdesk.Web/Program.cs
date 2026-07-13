using GlobomaticsHelpdesk.Web.Components;
using GlobomaticsHelpdesk.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Per-circuit selected API key.
builder.Services.AddScoped<ApiKeyState>();

// Per-circuit selected app mode (Chat / Vector Search / Knowledge Base).
builder.Services.AddScoped<ModeState>();

// Typed HttpClient for the helpdesk API. The base address comes from Aspire
// service discovery via the "http://api" pseudo-URL when launched by the
// AppHost. We deliberately request http to avoid local dev-cert trust issues
// since the API and Web both run on localhost. When running standalone, fall
// back to the configured value.
builder.Services.AddHttpClient<HelpdeskApiClient>(client =>
{
    var baseUrl = builder.Configuration["HelpdeskApi:BaseUrl"]
                  ?? "http://api";
    client.BaseAddress = new Uri(baseUrl);
});
builder.Services.AddSingleton<MarkdownRenderer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapDefaultEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
