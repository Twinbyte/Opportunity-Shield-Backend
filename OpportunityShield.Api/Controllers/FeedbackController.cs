using Microsoft.AspNetCore.Mvc;
using OpportunityShield.Api.Dtos;
using OpportunityShield.Api.Session;
using Oppurtunityshield.Domain.Entities;
using Oppurtunityshield.Domain.Enums;
using Oppurtunityshield.Domain.Repositories;

namespace OpportunityShield.Api.Controllers;

[ApiController]
[Route("api/v1/analyses/{analysisId:guid}/feedback")]
public class FeedbackController : ControllerBase
{
    private readonly IAnalysisRepository _analysisRepository;
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly ICurrentSession _session;

    public FeedbackController(
        IAnalysisRepository analysisRepository, IFeedbackRepository feedbackRepository, ICurrentSession session)
    {
        _analysisRepository = analysisRepository;
        _feedbackRepository = feedbackRepository;
        _session = session;
    }

    [HttpPost]
    public async Task<IActionResult> Submit(Guid analysisId, [FromBody] FeedbackRequest request, CancellationToken ct)
    {
        var analysis = await _analysisRepository.GetByIdAsync(analysisId, ct);
        if (analysis is null || analysis.SessionId != _session.SessionId)
            return NotFound();

        if (analysis.Status != AnalysisStatus.Completed)
            return BadRequest(new ErrorDto(
                "analysis_not_completed", "Feedback can only be left on a completed analysis."));

        var existing = await _feedbackRepository.GetByAnalysisIdAsync(analysisId, ct);
        if (existing is not null)
            return Conflict(new ErrorDto(
                "feedback_already_submitted", "Feedback has already been submitted for this analysis."));

        var feedback = new Feedback
        {
            AnalysisId = analysisId,
            WasHelpful = request.WasHelpful,
            ActualOutcome = request.ActualOutcome ?? ActualOutcome.Unknown
        };

        await _feedbackRepository.AddAsync(feedback, ct);
        return NoContent();
    }
}
