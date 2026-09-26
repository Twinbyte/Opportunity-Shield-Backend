using Oppurtunityshield.Domain.Enums;

namespace Oppurtunityshield.Domain.Entities;


public class Analysis
{
    public Guid Id { get; set; } = Guid.NewGuid();

  
    public string? SessionId { get; set; }

    public InputType InputType { get; set; }

    
    public string Content { get; set; } = string.Empty;

    public AnalysisStatus Status { get; set; } = AnalysisStatus.Pending;

    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Live progress feed while Status == Processing. Populated/updated by
    // whatever runs the evidence collectors; polling GET /analyses/:id
    // returns this so the frontend can render "checking domain... done" etc.
    public List<ProgressStep> ProgressSteps { get; set; } = new();

    public AnalysisResult? Result { get; set; }
    public Feedback? Feedback { get; set; }
}
