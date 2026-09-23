namespace Oppurtunityshield.Domain.RiskAssessment;

/// <summary>
/// Per-signal metadata: how much it counts, whether it's part of the
/// evidence-completeness gate, and any special handling. Weights are a
/// starting point for tuning, not fixed constants — everything else in the
/// engine reads from here rather than hardcoding per-signal behavior.
/// </summary>
public record SignalDefinition
{
    public required SignalCategory Category { get; init; }

    // Magnitude of impact on the weighted score, before SourceQuality scaling.
    // A Negative resolved value subtracts this; Positive adds it (unless
    // PositiveOnly is set). Zero means "not scored" — informational only.
    public required int Weight { get; init; }

    // Part of the 3-signal evidence-completeness gate (see RiskEngine).
    // Reserved for the organization-identity trio.
    public bool IsCritical { get; init; }

    // Can, on its own when Negative, justify overriding the evidence-
    // completeness gate to still produce Medium/High instead of UnableToVerify.
    public bool IsDirectRedFlag { get; init; }

    // Negative value contributes 0 rather than a penalty (e.g. a domain
    // name simply not containing the org's name isn't itself suspicious).
    public bool PositiveOnly { get; init; }

    // ConflictingInformation only: bypasses normal scoring/confidence
    // entirely and is handled as a special case in RiskEngine.
    public bool IsConflictOverride { get; init; }
}

public static class SignalCatalog
{
    public static readonly IReadOnlyDictionary<SignalId, SignalDefinition> Definitions =
        new Dictionary<SignalId, SignalDefinition>
        {
            // --- Domain / technical ---------------------------------------
            // Weak signal in both directions: age alone proves little.
            [SignalId.DomainAge] = new()
            {
                Category = SignalCategory.Domain,
                Weight = 5
            },
            [SignalId.DomainMatchesOrgName] = new()
            {
                Category = SignalCategory.Domain,
                Weight = 5,
                PositiveOnly = true
            },
            // Not scored — an unreachable domain is an evidence gap, not
            // proof of anything. Left at weight 0 deliberately.
            [SignalId.DomainReachable] = new()
            {
                Category = SignalCategory.Domain,
                Weight = 0
            },
            [SignalId.SslValid] = new()
            {
                Category = SignalCategory.Domain,
                Weight = 3
            },
            // False here is a warning, not a verdict — legitimate orgs use
            // third-party application platforms (Greenhouse, Lever, etc.).
            [SignalId.ApplicationDomainMatchesOfficial] = new()
            {
                Category = SignalCategory.Domain,
                Weight = 15
            },

            // --- Organization identity -------------------------------------
            [SignalId.OrganizationIdentified] = new()
            {
                Category = SignalCategory.Organization,
                Weight = 10,
                IsCritical = true
            },
            [SignalId.OfficialWebsiteFound] = new()
            {
                Category = SignalCategory.Organization,
                Weight = 15,
                IsCritical = true
            },
            [SignalId.IndependentSourceCorroboration] = new()
            {
                Category = SignalCategory.Organization,
                Weight = 15,
                IsCritical = true
            },
            [SignalId.ContactEmailDomainMatch] = new()
            {
                Category = SignalCategory.Organization,
                Weight = 8
            },
            // Special-cased entirely in RiskEngine — Weight is unused when
            // IsConflictOverride is set, kept only for documentation.
            [SignalId.ConflictingInformation] = new()
            {
                Category = SignalCategory.Organization,
                Weight = 40,
                IsDirectRedFlag = true,
                IsConflictOverride = true
            },

            // --- Content / request analysis ---------------------------------
            [SignalId.PaymentRequestPresent] = new()
            {
                Category = SignalCategory.Content,
                Weight = 25,
                IsDirectRedFlag = true
            },
            [SignalId.SensitiveInfoRequest] = new()
            {
                Category = SignalCategory.Content,
                Weight = 25,
                IsDirectRedFlag = true
            },
            [SignalId.UrgencyPressureLanguage] = new()
            {
                Category = SignalCategory.Content,
                Weight = 10
            },
            [SignalId.UnrealisticOffer] = new()
            {
                Category = SignalCategory.Content,
                Weight = 10
            }
        };

    // The 3 organization-identity signals that gate evidence completeness.
    public static readonly IReadOnlyList<SignalId> CriticalSignalIds = Definitions
        .Where(kv => kv.Value.IsCritical)
        .Select(kv => kv.Key)
        .ToList();
}
