using Oppurtunityshield.Domain.Enums;

namespace Oppurtunityshield.Domain.Entities;

/// <summary>
/// The structured verdict for a completed Analysis. One-to-one with Analysis.
/// Signals and EvidenceItems are child collections so they can be queried
/// and reported on individually (e.g. "how many results flagged a payment request").
/// </summary>
public class AnalysisResult
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AnalysisId { get; set; }
    public Analysis Analysis { get; set; } = null!;

    public string OpportunityName { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;

    // Null when RiskLevel is UnableToVerify — a numeric score would imply
    public int? TrustScore { get; set; }

    public RiskLevel RiskLevel { get; set; }

    // See Confidence enum: tracked independently from RiskLevel so e.g.
    // "High risk, Low confidence" can be represented and shown honestly.
    public Confidence Confidence { get; set; }

    public string Summary { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;

    public List<Signal> Signals { get; set; } = new();
    public List<Evidence> EvidenceItems { get; set; } = new();
}
