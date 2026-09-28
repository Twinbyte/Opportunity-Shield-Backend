using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oppurtunityshield.Infrastructure.Configuration;

namespace Oppurtunityshield.Infrastructure.AiProviders;

/// <summary>
/// Raw REST wrapper — deliberately not the official SDK, to keep the
/// project's dependency footprint (and package-restore surface) minimal for
/// a hackathon build. Verify the request shape against current Gemini docs
/// before relying on this in production; the google_search grounding tool
/// syntax has shifted between API/model versions before.
/// </summary>
public class GeminiClient
{
    private readonly HttpClient _http;
    private readonly GeminiOptions _options;
    private readonly ILogger<GroqClient> _logger;
    
    public GeminiClient(HttpClient http, IOptions<GeminiOptions> options, ILogger<GroqClient> logger)
    {
        _http = http;
        _options = options.Value;
        _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        _logger = logger;
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            _logger.LogWarning("Groq ApiKey is not configured");
    }

    /// <summary>
    /// Sends a prompt with Google Search grounding enabled and returns the
    /// raw text response. Caller is responsible for parsing structure out
    /// of it (ask for strict JSON in the prompt and parse defensively).
    /// </summary>
    public async Task<string> GenerateWithSearchAsync(string prompt, CancellationToken ct = default)
    {
        var url = $"{_options.BaseUrl}/models/{_options.Model}:generateContent?key={_options.ApiKey}";

        var body = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            tools = new object[] { new { google_search = new { } } },
            generationConfig = new { temperature = 0.1 }
        };

        using var response = await _http.PostAsync(
            url,
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Groq request failed: {Status} model={Model} body={Body}",
                (int)response.StatusCode, _options.Model,
                errorBody.Length > 300 ? errorBody[..300] : errorBody);
        }
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }
}
