using System.Text.Json;
using Oppurtunityshield.Application.Services;
using Oppurtunityshield.Domain.Enums;
using Oppurtunityshield.Domain.RiskAssessment;
using Oppurtunityshield.Infrastructure.AiProviders;

namespace Oppurtunityshield.Infrastructure.Services;

/// <summary>
/// Takes an already-computed RiskAssessmentResult and turns it into readable
/// prose. The RiskLevel/Confidence are passed in as fixed facts in the
/// prompt — Groq is asked to explain them, and is never asked to produce or
/// second-guess a verdict of its own.
/// </summary>
public class GroqExplanationGenerator : IExplanationGenerator
{
    private const string SystemPrompt = """
        You write clear, honest explanations of an opportunity-verification result for a
        student. You do NOT decide or restate risk levels differently than given — you
        explain the specific evidence behind the result you're given, in plain language.
        Never use hedging language that undermines a confirmed finding, and never claim
        certainty the evidence doesn't support.
        """;

    private readonly GroqClient _client;

    public GroqExplanationGenerator(GroqClient client)
    {
        _client = client;
    }

    public async Task<(string Summary, string Recommendation)> GenerateAsync(
        string opportunityName, string organization, RiskAssessmentResult result, CancellationToken ct = default)
    {
        var signalLines = result.ResolvedSignals
            .Select(s => $"- {s.Id} = {s.Value} (source quality: {s.SourceQuality}){(s.Detail is null ? "" : $": {s.Detail}")}");

        var prompt = $$"""
            Opportunity: {{opportunityName}}
            Organization: {{organization}}

            Determined result (fixed — do not restate differently):
            Risk level: {{result.RiskLevel}}
            Confidence: {{result.Confidence}}
            Evidence considered sufficient: {{result.EvidenceComplete}}

            Resolved evidence signals:
            {{string.Join('\n', signalLines)}}

            Write:
            1. A 1-2 sentence summary of the finding, matching the given risk level and confidence exactly.
            2. A concrete, actionable recommendation for the student (what to do or check next).

            If risk level is UnableToVerify, be explicit that this is not a "safe" or "risky"
            verdict — it means the evidence available wasn't enough to say either way, and the
            recommendation should focus on independent verification, not on proceeding or not.

            Respond with ONLY this JSON shape, no markdown fences:
            { "summary": string, "recommendation": string }
            """;

        try
        {
            var raw = await _client.ChatAsync(SystemPrompt, prompt, jsonMode: true, ct: ct);
            using var doc = JsonDocument.Parse(raw);
            var summary = doc.RootElement.GetProperty("summary").GetString() ?? FallbackSummary(result);
            var recommendation = doc.RootElement.GetProperty("recommendation").GetString() ?? FallbackRecommendation(result);
            return (summary, recommendation);
        }
        catch
        {
            // Explanation generation failing must never fail the whole
            // analysis — the verdict is already decided; fall back to a
            // template so the student still gets a usable result.
            return (FallbackSummary(result), FallbackRecommendation(result));
        }
    }

    private static string FallbackSummary(RiskAssessmentResult result) => result.RiskLevel switch
    {
        RiskLevel.UnableToVerify =>
            "We couldn't gather enough reliable information to determine whether this opportunity is legitimate.",
        RiskLevel.High =>
            "This opportunity shows significant warning signs based on the evidence we could gather.",
        RiskLevel.Medium =>
            "This opportunity shows some concerns worth reviewing before proceeding.",
        _ => "This opportunity shows no significant warning signs based on the evidence we could gather."
    };

    private static string FallbackRecommendation(RiskAssessmentResult result) => result.RiskLevel switch
    {
        RiskLevel.UnableToVerify =>
            "Don't submit sensitive information or make any payment yet. Try to verify this opportunity through an independently confirmed channel first.",
        RiskLevel.High =>
            "We'd recommend not proceeding without independently verifying this opportunity directly with the organization through an official channel.",
        _ => "Standard caution still applies: never pay money or share sensitive personal/financial details before an offer is signed."
    };
}
