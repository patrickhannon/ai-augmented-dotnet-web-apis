using FluentValidation;
using GlobomaticsHelpdesk.Api.Auth;
using GlobomaticsHelpdesk.Api.Data;
using GlobomaticsHelpdesk.Api.Endpoints;
using GlobomaticsHelpdesk.Api.OpenApiTransformers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

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

app.Run();

public partial class Program;
