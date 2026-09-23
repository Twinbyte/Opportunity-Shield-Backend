namespace Oppurtunityshield.Domain.RiskAssessment;

/// <summary>
/// The frozen MVP signal set. Deliberately small — resist adding more
/// without a concrete reason; each one needs a defined weight and meaning
/// below in SignalCatalog before it can be used.
/// </summary>
public enum SignalId
{
    // Domain / technical
    DomainAge,
    DomainMatchesOrgName,
    DomainReachable,
    SslValid,
    ApplicationDomainMatchesOfficial,

    // Organization identity
    OrganizationIdentified,
    OfficialWebsiteFound,
    IndependentSourceCorroboration,
    ContactEmailDomainMatch,
    ConflictingInformation,

    // Content / request analysis (extracted from submitted text directly —
    // reliable even if every external API call fails)
    PaymentRequestPresent,
    SensitiveInfoRequest,
    UrgencyPressureLanguage,
    UnrealisticOffer
}

public enum SignalCategory
{
    Domain,
    Organization,
    Content
}

/// <summary>
/// Tri-state, not binary. Unknown means "we couldn't establish this" —
/// distinct from a resolved Negative. Collapsing these loses exactly the
/// information the confidence calculation depends on.
/// </summary>
public enum SignalValue
{
    Unknown,
    Positive,
    Negative
}

/// <summary>
/// How authoritative the source of a resolved signal is. A signal's raw
/// Value carries no weight on its own — it's always Value × SourceQuality.
/// </summary>
public enum SourceQuality
{
    Unknown,
    Low,
    Medium,
    High
}
