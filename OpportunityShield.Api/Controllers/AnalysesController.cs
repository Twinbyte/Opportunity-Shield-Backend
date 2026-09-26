using Microsoft.AspNetCore.Mvc;
using OpportunityShield.Api.Dtos;
using OpportunityShield.Api.Session;
using Oppurtunityshield.Application.Services;
using Oppurtunityshield.Domain.Entities;
using Oppurtunityshield.Domain.Enums;
using Oppurtunityshield.Domain.Repositories;

namespace OpportunityShield.Api.Controllers;

[ApiController]
[Route("api/v1/analyses")]
public class AnalysesController : ControllerBase
{
    private readonly IAnalysisRepository _repository;
    private readonly ICurrentSession _session;
    private readonly IAnalysisQueue _queue;

    public AnalysesController(IAnalysisRepository repository, ICurrentSession session, IAnalysisQueue queue)
    {
        _repository = repository;
        _session = session;
        _queue = queue;
    }

    [HttpPost]
    public async Task<ActionResult<CreateAnalysisResponse>> Create(
        [FromBody] CreateAnalysisRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new ErrorDto("invalid_content", "Content must not be empty."));

        var analysis = new Analysis
        {
            SessionId = _session.SessionId,
            InputType = request.InputType,
            Content = request.Content.Trim()
        };

        await _repository.CreateAsync(analysis, ct);

        // Hand off and return immediately — the orchestrator runs on the
        // background worker, not on this request.
        _queue.Enqueue(analysis.Id);

        var response = new CreateAnalysisResponse(analysis.Id, analysis.Status, analysis.CreatedAt);
        return AcceptedAtAction(nameof(GetById), new { id = analysis.Id }, response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AnalysisResponse>> GetById(Guid id, CancellationToken ct)
    {
        var lightweight = await _repository.GetByIdAsync(id, ct);

        // 404 rather than 403 for a session mismatch — doesn't confirm to a
        // caller whether the ID exists at all, just that they can't see it.
        if (lightweight is null || lightweight.SessionId != _session.SessionId)
            return NotFound();

        var analysis = lightweight.Status == AnalysisStatus.Completed
            ? await _repository.GetByIdWithResultAsync(id, ct) ?? lightweight
            : lightweight;

        return Ok(analysis.ToResponse());
    }

    [HttpGet]
    public async Task<ActionResult<PagedAnalysesResponse>> List(
        [FromQuery] int limit = 20, [FromQuery] string? cursor = null, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        var (items, nextCursor) = await _repository.GetPagedBySessionAsync(_session.SessionId, limit, cursor, ct);
        return Ok(new PagedAnalysesResponse(items.Select(a => a.ToSummaryDto()).ToList(), nextCursor));
    }
}
