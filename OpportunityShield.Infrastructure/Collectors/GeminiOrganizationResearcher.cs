using System.Text.Json;
using OpportunityShield.Application.Collectors;
using Oppurtunityshield.Domain.Collectors;
using Oppurtunityshield.Domain.RiskAssessment;
using Oppurtunityshield.Infrastructure.AiProviders;

namespace Oppurtunityshield.Infrastructure.Collectors;

/// <summary>
/// The one collector that needs live web search. Deliberately asks Gemini
/// for raw facts only (names, URLs, whether sources exist) — never for a
/// verdict on whether something "matches" or "is legitimate". Those
/// comparisons happen deterministically in SignalDerivation instead.
/// </summary>
public class GeminiOrganizationResearcher : IOrganizationResearcher
{
    private readonly GeminiClient _client;

    public GeminiOrganizationResearcher(GeminiClient client)
    {
        _client = client;
    }

    public async Task<OrganizationFindings> ResearchAsync(
        OpportunitySubmission submission, CancellationToken ct = default)
    {
        var prompt = BuildPrompt(submission);

        try
        {
            var raw = await _client.GenerateWithSearchAsync(prompt, ct);
            return ParseFindings(raw);
        }
        catch
        {
            // Provider failure ≠ suspicious opportunity. Return "found
            // nothing" rather than throwing — the orchestrator treats a
            // fully-empty OrganizationFindings as an evidence gap, not a
            // red flag, and confidence is capped accordingly.
            return new OrganizationFindings();
        }
    }

    private static string BuildPrompt(OpportunitySubmission submission) => $$"""
        You are a fact-finding research assistant. Use web search to investigate the
        organization and opportunity described below. Report ONLY facts you can find —
        do not guess, and do not judge whether anything is legitimate or a scam.

        Submitted content:
        ---
        {{submission.TextForAnalysis}}
        ---

        Respond with ONLY a JSON object (no markdown fences, no commentary) matching
        exactly this shape:
        {
          "opportunityName": string or null (a short name for the specific opportunity, e.g. "Summer Marketing Internship"),
          "organizationName": string or null,
          "officialWebsiteUrl": string or null (the organization's own official site, found independently of the submission),
          "independentSources": [ { "url": string, "description": string, "quality": "Low"|"Medium"|"High" } ],
          "hasConflictingInformation": boolean,
          "conflictDetail": string or null (only if hasConflictingInformation is true — describe the specific contradiction, e.g. official site says free vs submission requests payment),
          "conflictSourceQuality": "Low"|"Medium"|"High"|"Unknown",
          "contactEmailDomain": string or null (domain portion only, e.g. "gmail.com", if a contact email appears in the submitted content)
        }
        If you cannot find something, use null or an empty array — never invent a value.
        """;

    private static OrganizationFindings ParseFindings(string raw)
    {
        var json = ExtractJson(raw);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var sources = new List<SourceRef>();
            if (root.TryGetProperty("independentSources", out var sourcesEl) && sourcesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in sourcesEl.EnumerateArray())
                {
                    sources.Add(new SourceRef(
                        Url: s.GetProperty("url").GetString() ?? string.Empty,
                        Description: s.GetProperty("description").GetString() ?? string.Empty,
                        Quality: ParseQuality(s.GetProperty("quality").GetString())));
                }
            }

            return new OrganizationFindings
            {
                OpportunityName = GetStringOrNull(root, "opportunityName"),
                OrganizationName = GetStringOrNull(root, "organizationName"),
                OfficialWebsiteUrl = GetStringOrNull(root, "officialWebsiteUrl"),
                IndependentSources = sources,
                HasConflictingInformation = root.TryGetProperty("hasConflictingInformation", out var c) && c.GetBoolean(),
                ConflictDetail = GetStringOrNull(root, "conflictDetail"),
                ConflictSourceQuality = root.TryGetProperty("conflictSourceQuality", out var q)
                    ? ParseQuality(q.GetString())
                    : SourceQuality.Unknown,
                ContactEmailDomain = GetStringOrNull(root, "contactEmailDomain")
            };
        }
        catch (JsonException)
        {
            // Model didn't return valid JSON despite instructions — treat as
            // an evidence gap, not a system failure.
            return new OrganizationFindings();
        }
    }

    private static string? GetStringOrNull(JsonElement root, string prop) =>
        root.TryGetProperty(prop, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    private static SourceQuality ParseQuality(string? value) =>
        Enum.TryParse<SourceQuality>(value, ignoreCase: true, out var q) ? q : SourceQuality.Unknown;
    
    private static string ExtractJson(string raw)
    {
        var trimmed = raw.Trim();
        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        return start >= 0 && end > start ? trimmed[start..(end + 1)] : trimmed;
    }
}