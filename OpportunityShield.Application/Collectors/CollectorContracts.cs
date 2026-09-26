using Oppurtunityshield.Domain.Collectors;
using Oppurtunityshield.Domain.RiskAssessment;

namespace OpportunityShield.Application.Collectors;

/// <summary>
/// Fast, non-search checks: DNS/HTTP reachability, TLS, domain age.
/// Implemented in Infrastructure with plain .NET + a free RDAP lookup —
/// deliberately has no AI dependency, so it never contributes to rate-limit
/// or provider-outage risk.
/// </summary>
public interface IDomainInspector
{
    Task<DomainFindings> InspectAsync(OpportunitySubmission submission, CancellationToken ct = default);
}

/// <summary>
/// The one collector that needs live web search (Gemini + Google Search
/// grounding). Kept to a single search-dependent call by design — see the
/// hackathon fallback discussion on minimizing external dependencies.
/// </summary>
public interface IOrganizationResearcher
{
    Task<OrganizationFindings> ResearchAsync(OpportunitySubmission submission, CancellationToken ct = default);
}

/// <summary>
/// Analyzes the submitted text itself (payment asks, sensitive-info
/// requests, pressure language, unrealistic offers). No search needed —
/// reliable even if Gemini's grounded search is unavailable.
/// </summary>
public interface IContentAnalyzer
{
    Task<IReadOnlyList<EvidenceSignal>> AnalyzeAsync(
        OpportunitySubmission submission, CancellationToken ct = default);
}
