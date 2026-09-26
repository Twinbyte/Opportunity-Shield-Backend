using Oppurtunityshield.Domain.Entities;
using Oppurtunityshield.Domain.Enums;

namespace Oppurtunityshield.Domain.Repositories;

public interface IAnalysisRepository
{
    Task<Analysis> CreateAsync(Analysis analysis, CancellationToken ct = default);

    Task<Analysis?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Analysis?> GetByIdWithResultAsync(Guid id, CancellationToken ct = default);

    
    Task<(IReadOnlyList<Analysis> Items, string? NextCursor)> GetPagedBySessionAsync(
        string sessionId, int limit, string? cursor, CancellationToken ct = default);

    Task UpdateStatusAsync(
        Guid id,
        AnalysisStatus status,
        string? errorCode = null,
        string? errorMessage = null,
        CancellationToken ct = default);

    
    Task UpdateProgressAsync(Guid id, List<ProgressStep> steps, CancellationToken ct = default);

    
    
    Task AttachResultAsync(Guid analysisId, AnalysisResult result, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
