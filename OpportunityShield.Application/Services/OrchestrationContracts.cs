using Oppurtunityshield.Domain.RiskAssessment;

namespace Oppurtunityshield.Application.Services;

/// <summary>
/// Turns an already-decided RiskAssessmentResult into readable prose.
/// Cannot change RiskLevel or Confidence — only explain them. The contract
/// signature enforces this: it receives the verdict, it returns text.
/// </summary>
public interface IExplanationGenerator
{
    Task<(string Summary, string Recommendation)> GenerateAsync(
        string opportunityName,
        string organization,
        RiskAssessmentResult result,
        CancellationToken ct = default);
}

/// <summary>
/// Drives one Analysis from Pending through to Completed or Failed:
/// runs collectors concurrently, feeds RiskEngine, persists the result.
/// </summary>
public interface IAnalysisOrchestrator
{
    Task RunAsync(Guid analysisId, CancellationToken ct = default);
}

/// <summary>
/// Lets a controller hand off "go analyze this" without blocking the HTTP
/// response on the full pipeline. The concrete implementation (a Channel +
/// BackgroundService) lives in Api, since it's tied to ASP.NET Core's
/// hosting model — this interface is the only thing the controller sees.
/// </summary>
public interface IAnalysisQueue
{
    void Enqueue(Guid analysisId);
    Task<Guid> DequeueAsync(CancellationToken ct);
}