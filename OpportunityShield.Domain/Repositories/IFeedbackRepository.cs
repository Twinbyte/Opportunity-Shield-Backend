using Oppurtunityshield.Domain.Entities;

namespace Oppurtunityshield.Domain.Repositories;

public interface IFeedbackRepository
{
    Task<Feedback> AddAsync(Feedback feedback, CancellationToken ct = default);

    Task<Feedback?> GetByAnalysisIdAsync(Guid analysisId, CancellationToken ct = default);
}
