using Oppurtunityshield.Domain.Entities;
using Oppurtunityshield.Domain.Enums;

namespace Oppurtunityshield.Domain.Demo;

/// <summary>
/// A fully pre-built result, bypassing every external call. Triggered by a
/// [DEMO:KEY] marker anywhere in the submitted content — a presenter can
/// force a reliable result on demand mid-pitch without a global mode switch
/// or redeploy. IMPORTANT: the frontend/presenter must say "this is a
/// prepared example" when using one — never pass it off as a live result.
/// </summary>
public record DemoScenario(
    string Key,
    string OpportunityName,
    string Organization,
    RiskLevel RiskLevel,
    Confidence Confidence,
    int? TrustScore,
    string Summary,
    string Recommendation,
    List<Signal> Signals,
    List<Evidence> Evidence);

public static class DemoCatalog
{
    private static readonly List<DemoScenario> Scenarios = new()
    {
        new DemoScenario(
            Key: "LEGIT",
            OpportunityName: "Summer Software Internship",
            Organization: "Nimbus Analytics Ltd",
            RiskLevel: RiskLevel.Low,
            Confidence: Confidence.High,
            TrustScore: 88,
            Summary: "This posting matches strong independent evidence of a real, established organization with no concerning requests.",
            Recommendation: "This opportunity looks legitimate. Standard due diligence still applies: never share banking details before an offer is signed.",
            Signals: new List<Signal>
            {
                new() { Type = SignalType.Positive, Description = "Organization confirmed via independent university career page listing", SortOrder = 0 },
                new() { Type = SignalType.Positive, Description = "Application hosted on the organization's own verified domain", SortOrder = 1 },
                new() { Type = SignalType.Positive, Description = "No payment or sensitive information requested", SortOrder = 2 }
            },
            Evidence: new List<Evidence>
            {
                new() { EvidenceType = "OrganizationIdentified", Detail = "Nimbus Analytics Ltd, registered fintech company", Impact = EvidenceImpact.Positive, SortOrder = 0 },
                new() { EvidenceType = "IndependentSourceCorroboration", Detail = "Listed on 3 independent university career pages", Impact = EvidenceImpact.Positive, SortOrder = 1 }
            }),

        new DemoScenario(
            Key: "SCAM",
            OpportunityName: "\"Remote Data Entry\" Opportunity",
            Organization: "Unverified — claims to be \"Global Career Access\"",
            RiskLevel: RiskLevel.High,
            Confidence: Confidence.Medium,
            TrustScore: 8,
            Summary: "This submission requests an upfront payment and could not be matched to any verifiable organization.",
            Recommendation: "Do not send any payment or personal financial information. We could not confirm this organization exists. Verify independently before proceeding, if at all.",
            Signals: new List<Signal>
            {
                new() { Type = SignalType.Warning, Description = "Requests a ₦15,000 \"registration fee\" before starting", SortOrder = 0 },
                new() { Type = SignalType.Warning, Description = "No independently verifiable organization found", SortOrder = 1 },
                new() { Type = SignalType.Warning, Description = "Uses urgency language (\"only 3 slots left today\")", SortOrder = 2 }
            },
            Evidence: new List<Evidence>
            {
                new() { EvidenceType = "PaymentRequestPresent", Detail = "Explicit request for ₦15,000 before onboarding", Impact = EvidenceImpact.Negative, SortOrder = 0 },
                new() { EvidenceType = "OrganizationIdentified", Detail = "No matching organization could be found", Impact = EvidenceImpact.Negative, SortOrder = 1 },
                new() { EvidenceType = "UrgencyPressureLanguage", Detail = "\"Only 3 slots left today\" pressure tactic", Impact = EvidenceImpact.Negative, SortOrder = 2 }
            }),

        new DemoScenario(
            Key: "UNCERTAIN",
            OpportunityName: "New Scholarship Program",
            Organization: "Could not be independently confirmed",
            RiskLevel: RiskLevel.UnableToVerify,
            Confidence: Confidence.Low,
            TrustScore: null,
            Summary: "We couldn't gather enough reliable information to determine whether this opportunity is legitimate.",
            Recommendation: "Don't submit sensitive information or make any payment yet. Try to verify this opportunity through an independently confirmed channel before proceeding.",
            Signals: new List<Signal>
            {
                new() { Type = SignalType.Warning, Description = "The organization could not be independently confirmed", SortOrder = 0 },
                new() { Type = SignalType.Warning, Description = "No official source could be located for this opportunity", SortOrder = 1 }
            },
            Evidence: new List<Evidence>
            {
                new() { EvidenceType = "OrganizationIdentified", Detail = "No independent confirmation found", Impact = EvidenceImpact.Neutral, SortOrder = 0 },
                new() { EvidenceType = "IndependentSourceCorroboration", Detail = "No independent sources located", Impact = EvidenceImpact.Neutral, SortOrder = 1 }
            })
    };

    /// <summary>
    /// Looks for a [DEMO:KEY] marker (case-insensitive) anywhere in the
    /// submitted content, e.g. "[DEMO:SCAM]". Returns null if none found or
    /// unrecognized — caller proceeds with the live pipeline as normal.
    /// </summary>
    public static DemoScenario? TryMatch(string content)
    {
        foreach (var scenario in Scenarios)
        {
            if (content.Contains($"[DEMO:{scenario.Key}]", StringComparison.OrdinalIgnoreCase))
                return scenario;
        }
        return null;
    }
}
