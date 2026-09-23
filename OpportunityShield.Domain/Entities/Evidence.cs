using Oppurtunityshield.Domain.Enums;

namespace Oppurtunityshield.Domain.Entities;

/// <summary>
/// One entry in the API's "evidence" array — a specific check that was run
/// and what it found (e.g. domain_verification, payment_request).
/// </summary>
public class Evidence
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AnalysisResultId { get; set; }
    public AnalysisResult AnalysisResult { get; set; } = null!;

    // e.g. "domain_verification", "payment_request", "sensitive_info_request"
    public string EvidenceType { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;
    public EvidenceImpact Impact { get; set; }

    public int SortOrder { get; set; }
}
