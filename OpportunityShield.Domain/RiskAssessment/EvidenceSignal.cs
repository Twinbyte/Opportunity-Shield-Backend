namespace Oppurtunityshield.Domain.RiskAssessment;

/// <summary>
/// One piece of evidence handed to the RiskEngine. This is the collectors'
/// output format — separate from the Evidence/Signal EF entities, which are
/// the persisted, API-facing shape built AFTER the engine has run.
/// </summary>
public class EvidenceSignal
{
    public required SignalId Id { get; init; }
    public required SignalValue Value { get; init; }

    // Unknown value should always carry SourceQuality.Unknown — there's
    // nothing to rate the quality of when nothing was resolved.
    public SourceQuality SourceQuality { get; init; } = SourceQuality.Unknown;

    // e.g. "official_domain", "university_career_page", "whois", "gemini_search"
    public string? Source { get; init; }

    // Human-readable detail for this specific finding, e.g.
    // "Official site (brightpathmedia.com) confirms this internship is unpaid
    // and free to apply; submission requests a ₦15,000 processing fee."
    public string? Detail { get; init; }
}
