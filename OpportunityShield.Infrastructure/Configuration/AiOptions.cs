namespace Oppurtunityshield.Infrastructure.Configuration;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";

    
    public string Model { get; set; } = "gemini-3.8-flash";

    public int TimeoutSeconds { get; set; } = 12;
}

public class GroqOptions
{
    public const string SectionName = "Groq";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
    public string Model { get; set; } = "llama-3.1-8b-instant";

    public int TimeoutSeconds { get; set; } = 8;
}

public class AnalysisOptions
{
    public const string SectionName = "Analysis";

    // Global fallback only. Individual submissions can still trigger a
    // canned demo scenario via a [DEMO:...] marker regardless of this
    // setting. Kept for completeness/env-based control.
    public string Mode { get; set; } = "Live"; // "Live" | "Demo"
}
