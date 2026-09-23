using Microsoft.EntityFrameworkCore;
using Oppurtunityshield.Domain.Entities;
using Oppurtunityshield.Domain.Enums;
using Oppurtunityshield.Domain.Repositories;
using Oppurtunityshield.Infrastructure.Data;

namespace Oppurtunityshield.Infrastructure.Repositories;

public class AnalysisRepository : IAnalysisRepository
{
    private readonly AppDbContext _db;

    public AnalysisRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Analysis> CreateAsync(Analysis analysis, CancellationToken ct = default)
    {
        _db.Analyses.Add(analysis);
        await _db.SaveChangesAsync(ct);
        return analysis;
    }

    public Task<Analysis?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Analyses.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Analysis?> GetByIdWithResultAsync(Guid id, CancellationToken ct = default)
        => _db.Analyses
            .Include(a => a.Result)
                .ThenInclude(r => r!.Signals.OrderBy(s => s.SortOrder))
            .Include(a => a.Result)
                .ThenInclude(r => r!.EvidenceItems.OrderBy(e => e.SortOrder))
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<(IReadOnlyList<Analysis> Items, string? NextCursor)> GetPagedBySessionAsync(
        string sessionId, int limit, string? cursor, CancellationToken ct = default)
    {
        var query = _db.Analyses
            .Where(a => a.SessionId == sessionId)
            .OrderByDescending(a => a.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrEmpty(cursor) && DateTime.TryParse(cursor, out var cursorDate))
        {
            query = query.Where(a => a.CreatedAt < cursorDate);
        }

        // Fetch one extra row so we know whether a next page exists.
        var items = await query.Take(limit + 1).ToListAsync(ct);

        string? nextCursor = null;
        if (items.Count > limit)
        {
            nextCursor = items[limit - 1].CreatedAt.ToString("O");
            items = items.Take(limit).ToList();
        }

        return (items, nextCursor);
    }

    public async Task UpdateStatusAsync(
        Guid id,
        AnalysisStatus status,
        string? errorCode = null,
        string? errorMessage = null,
        CancellationToken ct = default)
    {
        var analysis = await _db.Analyses.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (analysis is null) return;

        analysis.Status = status;
        analysis.ErrorCode = errorCode;
        analysis.ErrorMessage = errorMessage;

        if (status is AnalysisStatus.Completed or AnalysisStatus.Failed)
        {
            analysis.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateProgressAsync(Guid id, List<ProgressStep> steps, CancellationToken ct = default)
    {
        var analysis = await _db.Analyses.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (analysis is null) return;

        analysis.ProgressSteps = steps;
        await _db.SaveChangesAsync(ct);
    }

    public async Task AttachResultAsync(Guid analysisId, AnalysisResult result, CancellationToken ct = default)
    {
        result.AnalysisId = analysisId;
        _db.AnalysisResults.Add(result);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
