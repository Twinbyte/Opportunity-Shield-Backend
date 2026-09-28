using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OpportunityShield.Api.BackgroundWork;
using OpportunityShield.Api.Middleware;
using OpportunityShield.Api.Session;
using OpportunityShield.Application.Collectors;
using Oppurtunityshield.Application.Services;
using Oppurtunityshield.Domain.Repositories;
using Oppurtunityshield.Infrastructure.AiProviders;
using Oppurtunityshield.Infrastructure.Collectors;
using Oppurtunityshield.Infrastructure.Configuration;
using Oppurtunityshield.Infrastructure.Data;
using Oppurtunityshield.Infrastructure.Repositories;
using Oppurtunityshield.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// --- JSON: enums serialize as camelCase strings (e.g. "unableToVerify") -----
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

// --- Database ----------------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();

// --- Anonymous session (see Api/Session, Api/Middleware) ----------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentSession, CurrentSession>();

// --- AI provider configuration -------------------------------------------------
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));
builder.Services.Configure<GroqOptions>(builder.Configuration.GetSection(GroqOptions.SectionName));
builder.Services.Configure<AnalysisOptions>(builder.Configuration.GetSection(AnalysisOptions.SectionName));

builder.Services.AddHttpClient<GeminiClient>();
builder.Services.AddHttpClient<GroqClient>();
builder.Services.AddHttpClient<DomainInspector>();

// --- Collectors and orchestration (Application interfaces, Infrastructure impls) --
builder.Services.AddScoped<IDomainInspector, DomainInspector>();
builder.Services.AddScoped<IOrganizationResearcher, GeminiOrganizationResearcher>();
builder.Services.AddScoped<IContentAnalyzer, GroqContentAnalyzer>();
builder.Services.AddScoped<IExplanationGenerator, GroqExplanationGenerator>();
builder.Services.AddScoped<IAnalysisOrchestrator, AnalysisOrchestrator>();
builder.Services.AddHttpClient<IPageContentFetcher, HttpPageContentFetcher>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

// --- Background processing: queue is a singleton, worker drains it -----------
builder.Services.AddSingleton<IAnalysisQueue, AnalysisQueue>();
builder.Services.AddHostedService<AnalysisProcessingService>();

// --- CORS ----------------------------------------------------------------------
// Set Cors:AllowedOrigins in appsettings before the pitch — AllowAnyOrigin()
// below is a dev-only fallback, not something to ship with.
// X-Session-Id must be exposed, or the frontend can send it but never read the
// server-issued value back on a brand-new session.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins);
        else
            policy.AllowAnyOrigin(); // dev fallback only

        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders(SessionIdMiddleware.HeaderName);
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    
    options.AddSecurityDefinition("SessionId", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "X-Session-Id",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Paste a session GUID here (or leave any value on your first call, " +
                      "then replace it with the X-Session-Id value the server returns)."
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "SessionId"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Frontend");
app.UseMiddleware<SessionIdMiddleware>();

app.MapControllers();

app.Run();
