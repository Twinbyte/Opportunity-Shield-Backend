using Microsoft.EntityFrameworkCore;
using Oppurtunityshield.Domain.Entities;
using Oppurtunityshield.Domain.Repositories;
using Oppurtunityshield.Infrastructure.Data;

namespace Oppurtunityshield.Infrastructure.Repositories;

public class FeedbackRepository : IFeedbackRepository
{
    private readonly AppDbContext _db;

    public FeedbackRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Feedback> AddAsync(Feedback feedback, CancellationToken ct = default)
    {
        _db.Feedbacks.Add(feedback);
        await _db.SaveChangesAsync(ct);
        return feedback;
    }

    public Task<Feedback?> GetByAnalysisIdAsync(Guid analysisId, CancellationToken ct = default)
        => _db.Feedbacks.FirstOrDefaultAsync(f => f.AnalysisId == analysisId, ct);
}
