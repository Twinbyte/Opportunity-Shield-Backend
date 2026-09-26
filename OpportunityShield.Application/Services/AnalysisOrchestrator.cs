using OpportunityShield.Application.Collectors;
using Oppurtunityshield.Domain.Collectors;
using Oppurtunityshield.Domain.Demo;
using Oppurtunityshield.Domain.Entities;
using Oppurtunityshield.Domain.Enums;
using Oppurtunityshield.Domain.Repositories;
using Oppurtunityshield.Domain.RiskAssessment;

namespace Oppurtunityshield.Application.Services;



public class AnalysisOrchestrator : IAnalysisOrchestrator
{
    private readonly IAnalysisRepository _repository;
    private readonly IDomainInspector _domainInspector;
    private readonly IOrganizationResearcher _organizationResearcher;
    private readonly IContentAnalyzer _contentAnalyzer;
    private readonly IExplanationGenerator _explanationGenerator;
    private readonly SemaphoreSlim _progressLock = new(1, 1);

    public AnalysisOrchestrator(
        IAnalysisRepository repository,
        IDomainInspector domainInspector,
        IOrganizationResearcher organizationResearcher,
        IContentAnalyzer contentAnalyzer,
        IExplanationGenerator explanationGenerator)
    {
        _repository = repository;
        _domainInspector = domainInspector;
        _organizationResearcher = organizationResearcher;
        _contentAnalyzer = contentAnalyzer;
        _explanationGenerator = explanationGenerator;
    }

    public async Task RunAsync(Guid analysisId, CancellationToken ct = default)
    {
        var analysis = await _repository.GetByIdAsync(analysisId, ct);
        if (analysis is null) return;

        try
        {
            // Demo fallback: a [DEMO:KEY] marker bypasses every external
            // call entirely.
            // safety net, never used silently (frontend must label it).
            var demo = DemoCatalog.TryMatch(analysis.Content);
            if (demo is not null)
            {
                await CompleteWithDemoAsync(analysisId, demo, ct);
                return;
            }

            await RunLiveAsync(analysis, ct);
        }
        catch (Exception ex)
        {
            // Anything unexpected reaching here (not already caught inside
            // a collector) is a genuine system failure, not a verdict.
            await _repository.UpdateStatusAsync(
                analysisId, AnalysisStatus.Failed, errorCode: "analysis_failed", errorMessage: ex.Message, ct: ct);
        }
    }

    private async Task RunLiveAsync(Analysis analysis, CancellationToken ct)
    {
        var submission = BuildSubmission(analysis);

        await _repository.UpdateStatusAsync(analysis.Id, AnalysisStatus.Processing, ct: ct);

        var steps = new List<ProgressStep>
        {
            new() { Label = "Checked submitted domain", Status = "pending" },
            new() { Label = "Researched organization", Status = "pending" },
            new() { Label = "Analyzed submitted content", Status = "pending" },
            new() { Label = "Preparing risk assessment", Status = "pending" }
        };
        await _repository.UpdateProgressAsync(analysis.Id, steps, ct);

        var domainTask = RunStepAsync(analysis.Id, steps, 0,
            () => _domainInspector.InspectAsync(submission, ct), ct);
        var orgTask = RunStepAsync(analysis.Id, steps, 1,
            () => _organizationResearcher.ResearchAsync(submission, ct), ct);
        var contentTask = RunStepAsync(analysis.Id, steps, 2,
            () => _contentAnalyzer.AnalyzeAsync(submission, ct), ct);

        await Task.WhenAll(domainTask, orgTask, contentTask);

        await MarkStepAsync(analysis.Id, steps, 3, "in_progress", ct);

        var domainFindings = domainTask.Result;
        var orgFindings = orgTask.Result;
        var contentSignals = contentTask.Result;

        var allSignals = SignalDerivation.FromDomainFindings(domainFindings)
            .Concat(SignalDerivation.FromOrganizationFindings(orgFindings, submission.Host))
            .Concat(contentSignals)
            .ToList();

        // Nothing resolved at all — including the non-AI domain checks —
        // means our systems couldn't gather any evidence, not that the
        // opportunity itself is unverifiable. That's a Failed analysis,
        // never a silent UnableToVerify.
        if (allSignals.Count == 0)
        {
            await _repository.UpdateStatusAsync(
                analysis.Id, AnalysisStatus.Failed,
                errorCode: "no_evidence_gathered",
                errorMessage: "No evidence providers returned any usable result.",
                ct: ct);
            return;
        }

        var assessment = RiskEngine.Assess(allSignals);

        var opportunityName = orgFindings.OpportunityName ?? "Unspecified opportunity";
        var organizationName = orgFindings.OrganizationName ?? "Unknown organization";

        var (summary, recommendation) = await _explanationGenerator.GenerateAsync(
            opportunityName, organizationName, assessment, ct);

        await MarkStepAsync(analysis.Id, steps, 3, "done", ct);

        var result = new AnalysisResult
        {
            OpportunityName = opportunityName,
            Organization = organizationName,
            TrustScore = SignalPresentation.ToDisplayTrustScore(assessment),
            RiskLevel = assessment.RiskLevel,
            Confidence = assessment.Confidence,
            Summary = summary,
            Recommendation = recommendation,
            Signals = SignalPresentation.ToSignalEntities(assessment.ResolvedSignals),
            EvidenceItems = SignalPresentation.ToEvidenceEntities(assessment.ResolvedSignals)
        };

        await _repository.AttachResultAsync(analysis.Id, result, ct);
        await _repository.UpdateStatusAsync(analysis.Id, AnalysisStatus.Completed, ct: ct);
    }

    private async Task CompleteWithDemoAsync(Guid analysisId, DemoScenario demo, CancellationToken ct)
    {
        await _repository.UpdateStatusAsync(analysisId, AnalysisStatus.Processing, ct: ct);

        var result = new AnalysisResult
        {
            OpportunityName = demo.OpportunityName,
            Organization = demo.Organization,
            TrustScore = demo.TrustScore,
            RiskLevel = demo.RiskLevel,
            Confidence = demo.Confidence,
            Summary = demo.Summary,
            Recommendation = demo.Recommendation,
            Signals = demo.Signals,
            EvidenceItems = demo.Evidence
        };

        await _repository.AttachResultAsync(analysisId, result, ct);
        await _repository.UpdateStatusAsync(analysisId, AnalysisStatus.Completed, ct: ct);
    }

    private static OpportunitySubmission BuildSubmission(Analysis analysis)
    {
        string? host = null;
        if (analysis.InputType == InputType.Url
            && Uri.TryCreate(analysis.Content, UriKind.Absolute, out var uri))
        {
            host = uri.Host.Replace("www.", "", StringComparison.OrdinalIgnoreCase);
        }

        return new OpportunitySubmission(analysis.Content, analysis.InputType) { Host = host };
    }

    private async Task<T> RunStepAsync<T>(
        Guid analysisId, List<ProgressStep> steps, int index, Func<Task<T>> action, CancellationToken ct)
    {
        await MarkStepAsync(analysisId, steps, index, "in_progress", ct);
        var result = await action();
        await MarkStepAsync(analysisId, steps, index, "done", ct);
        return result;
    }

    private async Task MarkStepAsync(
        Guid analysisId, List<ProgressStep> steps, int index, string status, CancellationToken ct)
    {
        await _progressLock.WaitAsync(ct);
        try
        {
            steps[index].Status = status;
            steps[index].UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateProgressAsync(analysisId, steps, ct);
        }
        finally
        {
            _progressLock.Release();
        }
    }
}