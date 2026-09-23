using Oppurtunityshield.Domain.Enums;

namespace Oppurtunityshield.Domain.Entities;

/// <summary>
/// Optional feedback a student leaves on a completed analysis.
/// Cheap to store now, valuable later for evaluating/improving the risk model.
/// </summary>
public class Feedback
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AnalysisId { get; set; }
    public Analysis Analysis { get; set; } = null!;

    public bool WasHelpful { get; set; }
    public ActualOutcome ActualOutcome { get; set; } = ActualOutcome.Unknown;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

