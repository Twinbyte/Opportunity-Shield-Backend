using Oppurtunityshield.Domain.Enums;

namespace Oppurtunityshield.Domain.RiskAssessment;

/// <summary>
/// The engine's complete, final verdict. Nothing downstream (Groq included)
/// is allowed to change RiskLevel or Confidence — only explain them.
/// </summary>
public class RiskAssessmentResult
{
    public required RiskLevel RiskLevel { get; init; }
    public required Confidence Confidence { get; init; }

    // Raw weighted score, kept for debugging/tuning — not shown to the student.
    public required int Score { get; init; }

    public required bool EvidenceComplete { get; init; }
    public required bool DirectRedFlagPresent { get; init; }

    // Whether ConflictingInformation triggered the override path.
    public required bool ConflictOverrideApplied { get; init; }

    // Every signal that was actually resolved (Value != Unknown), for
    // building the persisted Signal/Evidence rows afterward.
    public required IReadOnlyList<EvidenceSignal> ResolvedSignals { get; init; }

    // Signals the collectors couldn't resolve at all — surfaced to the
    // student as "what we couldn't check" rather than silently dropped.
    public required IReadOnlyList<SignalId> UnresolvedSignals { get; init; }
}
