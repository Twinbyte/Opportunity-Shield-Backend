using Oppurtunityshield.Domain.Enums;

namespace Oppurtunityshield.Domain.Entities;

/// <summary>
/// One entry in the API's "positiveSignals" or "warningSignals" array,
/// distinguished by Type. Kept as rows rather than a JSON array so they
/// can be filtered/aggregated later (e.g. most common red flags).
/// </summary>
public class Signal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AnalysisResultId { get; set; }
    public AnalysisResult AnalysisResult { get; set; } = null!;

    public SignalType Type { get; set; }
    public string Description { get; set; } = string.Empty;

    // Preserves the original ordering when rehydrating the array for the API response.
    public int SortOrder { get; set; }
}
