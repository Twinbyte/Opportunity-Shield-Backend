using Oppurtunityshield.Domain.Entities;
using Oppurtunityshield.Domain.Enums;

namespace OpportunityShield.Api.Dtos;

public static class AnalysisMappingExtensions
{
    public static AnalysisResponse ToResponse(this Analysis analysis)
    {
        var error = analysis.Status == AnalysisStatus.Failed
            ? new ErrorDto(analysis.ErrorCode ?? "unknown_error", analysis.ErrorMessage)
            : null;

        // Only meaningful while Processing — omit once Completed/Failed so the
        // response doesn't imply the analysis is still running.
        var progressSteps = analysis.Status == AnalysisStatus.Processing
            ? analysis.ProgressSteps
                .Select(p => new ProgressStepDto(p.Label, p.Status, p.UpdatedAt))
                .ToList()
            : null;

        return new AnalysisResponse(
            analysis.Id,
            analysis.Status,
            analysis.CreatedAt,
            analysis.CompletedAt,
            progressSteps,
            error,
            analysis.Result?.ToDto());
    }

    public static AnalysisResultDto ToDto(this AnalysisResult result) => new(
        result.OpportunityName,
        result.Organization,
        result.TrustScore,
        result.RiskLevel,
        result.Confidence,
        result.Summary,
        result.Signals
            .Where(s => s.Type == SignalType.Positive)
            .OrderBy(s => s.SortOrder)
            .Select(s => s.Description)
            .ToList(),
        result.Signals
            .Where(s => s.Type == SignalType.Warning)
            .OrderBy(s => s.SortOrder)
            .Select(s => s.Description)
            .ToList(),
        result.EvidenceItems
            .OrderBy(e => e.SortOrder)
            .Select(e => new EvidenceDto(e.EvidenceType, e.Detail, e.Impact))
            .ToList(),
        result.Recommendation);

    public static AnalysisSummaryDto ToSummaryDto(this Analysis analysis) => new(
        analysis.Id,
        analysis.Result?.OpportunityName,
        analysis.Result?.Organization,
        analysis.Result?.RiskLevel,
        analysis.Result?.TrustScore,
        analysis.Status,
        analysis.CreatedAt);
}
