namespace Oppurtunityshield.Domain.Entities;

/// <summary>
/// One step in the live "what we're checking" progress feed shown while an
/// Analysis is Processing (e.g. "Checked submitted domain" → "done").
/// Stored as a JSON column on Analysis (see AppDbContext) — this is
/// transient UX state, not something that needs relational querying the
/// way Signal/Evidence do.
/// </summary>
public class ProgressStep
{
    public string Label { get; set; } = string.Empty;

    // "pending" | "in_progress" | "done" | "failed"
    public string Status { get; set; } = "pending";

    public DateTime? UpdatedAt { get; set; }
}
