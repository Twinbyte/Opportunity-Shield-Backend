using Oppurtunityshield.Domain.Enums;
using Oppurtunityshield.Domain.RiskAssessment;

namespace Oppurtunityshield.Domain.Collectors;

/// <summary>
/// What a collector actually needs — deliberately not the Analysis EF entity,
/// so collectors stay ignorant of persistence. Plain data, so it stays in
/// Domain even though the interfaces that produce it live in Application.
/// </summary>
public record OpportunitySubmission(string Content, InputType InputType)
{
    public string? Host { get; init; }


    public string? PageText { get; init; }

    public string TextForAnalysis => PageText is { Length: > 0 }
        ? $"Source URL: {Content}\n\nPage text:\n{PageText}"
        : Content;
}
/// <summary>
/// Raw output of deterministic, non-AI domain checks. No judgment calls here —
/// just facts, or null when a fact couldn't be established.
/// </summary>
public record DomainFindings
{
    public bool? IsReachable { get; init; }
    public bool? IsHttps { get; init; }
    public int? DomainAgeDays { get; init; }
    public string? Host { get; init; }
}

/// <summary>
/// One corroborating source Gemini's grounded search turned up.
/// </summary>
public record SourceRef(string Url, string Description, SourceQuality Quality);

/// <summary>
/// Raw facts from Gemini's grounded search — names, URLs, and findings only.
/// No true/false verdicts on "does this match" here; that comparison is done
/// deterministically afterward by SignalDerivation, not by the model.
/// </summary>
public record OrganizationFindings
{
    public string? OpportunityName { get; init; }
    public string? OrganizationName { get; init; }
    public string? OfficialWebsiteUrl { get; init; }
    public IReadOnlyList<SourceRef> IndependentSources { get; init; } = Array.Empty<SourceRef>();

    public bool HasConflictingInformation { get; init; }
    public string? ConflictDetail { get; init; }
    public SourceQuality ConflictSourceQuality { get; init; } = SourceQuality.Unknown;

    // Domain of the contact email found in the submitted content, if any
    // (e.g. "gmail.com" or "brightpathmedia.com") — extracted from raw text,
    // not searched for.
    public string? ContactEmailDomain { get; init; }
}
