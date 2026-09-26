using Oppurtunityshield.Domain.Enums;

namespace OpportunityShield.Api.Dtos;

public record CreateAnalysisRequest(InputType InputType, string Content);

public record CreateAnalysisResponse(Guid AnalysisId, AnalysisStatus Status, DateTime CreatedAt);

public record ProgressStepDto(string Label, string Status, DateTime? UpdatedAt);

public record ErrorDto(string Code, string? Message);

public record EvidenceDto(string Type, string Detail, EvidenceImpact Impact);

public record AnalysisResultDto(
    string OpportunityName,
    string Organization,
    int? TrustScore,
    RiskLevel RiskLevel,
    Confidence Confidence,
    string Summary,
    IReadOnlyList<string> PositiveSignals,
    IReadOnlyList<string> WarningSignals,
    IReadOnlyList<EvidenceDto> Evidence,
    string Recommendation);

public record AnalysisResponse(
    Guid AnalysisId,
    AnalysisStatus Status,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    IReadOnlyList<ProgressStepDto>? ProgressSteps,
    ErrorDto? Error,
    AnalysisResultDto? Result);

public record AnalysisSummaryDto(
    Guid AnalysisId,
    string? OpportunityName,
    string? Organization,
    RiskLevel? RiskLevel,
    int? TrustScore,
    AnalysisStatus Status,
    DateTime CreatedAt);

public record PagedAnalysesResponse(IReadOnlyList<AnalysisSummaryDto> Items, string? NextCursor);

public record FeedbackRequest(bool WasHelpful, ActualOutcome? ActualOutcome);
