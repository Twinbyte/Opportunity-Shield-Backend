using System.Text.Json;
using OpportunityShield.Application.Collectors;
using Oppurtunityshield.Domain.Collectors;
using Oppurtunityshield.Domain.RiskAssessment;
using Oppurtunityshield.Infrastructure.AiProviders;

namespace Oppurtunityshield.Infrastructure.Collectors;

/// <summary>
/// Analyzes the submitted text directly — no web search dependency, so this
/// stays reliable even if Gemini's grounded search is unavailable. Uses
/// Groq for speed since this sits directly in the user's wait path.
/// </summary>
public class GroqContentAnalyzer : IContentAnalyzer
{
    private const string SystemPrompt = """
        You analyze opportunity postings (internships, jobs, scholarships) for specific,
        objective red flags in the text itself. You are not judging legitimacy overall —
        only reporting whether each specific pattern is present in THIS text.
        """;

    private readonly GroqClient _client;

    public GroqContentAnalyzer(GroqClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlyList<EvidenceSignal>> AnalyzeAsync(
        OpportunitySubmission submission, CancellationToken ct = default)
    {
        var prompt = $$"""
                       Submitted opportunity text:
                       ---
                       {{submission.TextForAnalysis}}
                       ---

                       For each of the four checks below, respond with "Present" (the pattern IS found in
                       this text), "Absent" (the pattern is clearly NOT found), or "Unknown" (the text
                       doesn't give enough information either way). Use exactly these three words — not
                       "Positive"/"Negative". Include a short quoted-or-paraphrased detail only when the
                       value is "Present".
                       Guidance: mark urgencyPressureLanguage "Present" only for pressure meant to rush the 
                       reader into paying or acting quickly (e.g. "only 3 slots left", "pay within 24 hours", 
                       "act now"). Countdown timers, event dates and ordinary registration deadlines are NOT 
                       pressure tactics. If no pay, salary or benefit is described, answer "Unknown" for 
                       unrealisticOffer, not "Absent".
                       Respond with ONLY this JSON shape, no markdown fences:
                       {
                         "paymentRequestPresent": "Present"|"Absent"|"Unknown",
                         "paymentRequestDetail": string or null,
                         "sensitiveInfoRequest": "Present"|"Absent"|"Unknown",
                         "sensitiveInfoDetail": string or null,
                         "urgencyPressureLanguage": "Present"|"Absent"|"Unknown",
                         "urgencyDetail": string or null,
                         "unrealisticOffer": "Present"|"Absent"|"Unknown",
                         "unrealisticOfferDetail": string or null
                       }
                       """;

        try
        {
            var raw = await _client.ChatAsync(SystemPrompt, prompt, jsonMode: true, ct: ct);
            return Parse(raw);
        }
        catch
        {
            // Provider failure — return nothing rather than throwing. The
            // orchestrator treats missing content signals as an evidence
            // gap for this category, not as a red flag.
            return Array.Empty<EvidenceSignal>();
        }
    }

    private static IReadOnlyList<EvidenceSignal> Parse(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var signals = new List<EvidenceSignal>();

            AddIfResolved(signals, root, SignalId.PaymentRequestPresent, "paymentRequestPresent", "paymentRequestDetail");
            AddIfResolved(signals, root, SignalId.SensitiveInfoRequest, "sensitiveInfoRequest", "sensitiveInfoDetail");
            AddIfResolved(signals, root, SignalId.UrgencyPressureLanguage, "urgencyPressureLanguage", "urgencyDetail");
            AddIfResolved(signals, root, SignalId.UnrealisticOffer, "unrealisticOffer", "unrealisticOfferDetail");

            return signals;
        }
        catch (JsonException)
        {
            return Array.Empty<EvidenceSignal>();
        }
    }

    private static void AddIfResolved(
        List<EvidenceSignal> signals, JsonElement root, SignalId id, string valueProp, string detailProp)
    {
        if (!root.TryGetProperty(valueProp, out var valueEl)) return;
        var raw = valueEl.GetString();

        SignalValue value;
        if (string.Equals(raw, "Present", StringComparison.OrdinalIgnoreCase))
            value = SignalValue.Negative;   // pattern found = bad news, our code decides this, not the model
        else if (string.Equals(raw, "Absent", StringComparison.OrdinalIgnoreCase))
            value = SignalValue.Positive;   // pattern not found = good news
        else
            return; // "Unknown" or anything unrecognized — skip, treated as an evidence gap

        var detail = root.TryGetProperty(detailProp, out var d) && d.ValueKind == JsonValueKind.String
            ? d.GetString()
            : null;

        signals.Add(new EvidenceSignal
        {
            Id = id,
            Value = value,
            SourceQuality = SourceQuality.High,
            Source = "submitted_content",
            Detail = detail
        });
    }
}