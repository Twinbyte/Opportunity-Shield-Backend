using Oppurtunityshield.Domain.Enums;

namespace Oppurtunityshield.Domain.RiskAssessment;

/// <summary>
/// Deterministic, stateless, unit-testable. Takes whatever evidence the
/// collectors managed to gather and produces a RiskLevel + Confidence.
/// No AI model is involved anywhere in this class — Groq's only job,
/// downstream, is to explain what this engine already decided.
/// </summary>
public static class RiskEngine
{
    // How many of the 3 critical organization-identity signals must resolve
    // (non-Unknown) before we consider evidence "complete enough" to commit
    // to a verdict on its own. Below this, only a direct red flag can still
    // justify Medium/High instead of UnableToVerify.
    //
    // Deliberately set to 2, not 1: confirming just organization_identified
    // ("we know their name") is not treated as enough on its own to call
    // something Low risk — that case now correctly falls to UnableToVerify
    // rather than a false-comfort Low/Low. Revisit only with a clear reason.
    private const int MinCriticalSignalsResolved = 2;

    // Weighted-score thresholds. Deliberately a wide Medium band so isolated
    // weak signals don't flip the verdict on their own.
    private const int LowRiskThreshold = 20;
    private const int HighRiskThreshold = -20;

    public static RiskAssessmentResult Assess(IReadOnlyList<EvidenceSignal> signals)
    {
        var bySignalId = signals
            .GroupBy(s => s.Id)
            .ToDictionary(g => g.Key, g => g.First());

        var resolved = signals.Where(s => s.Value != SignalValue.Unknown).ToList();

        var unresolved = SignalCatalog.Definitions.Keys
            .Where(id => !bySignalId.TryGetValue(id, out var s) || s.Value == SignalValue.Unknown)
            .ToList();

        // --- Conflict override: bypasses everything else ---------------------
        if (bySignalId.TryGetValue(SignalId.ConflictingInformation, out var conflict)
            && conflict.Value == SignalValue.Negative)
        {
            var conflictConfidence = conflict.SourceQuality == SourceQuality.High
                ? Confidence.High
                : Confidence.Medium; // Medium/Low source quality still floors at Medium —
                                      // the contradiction itself was directly observed.

            return new RiskAssessmentResult
            {
                RiskLevel = RiskLevel.High,
                Confidence = conflictConfidence,
                Score = SignalCatalog.Definitions[SignalId.ConflictingInformation].Weight * -1,
                EvidenceComplete = true, // a confirmed contradiction is itself sufficient evidence
                DirectRedFlagPresent = true,
                ConflictOverrideApplied = true,
                ResolvedSignals = resolved,
                UnresolvedSignals = unresolved
            };
        }

        // --- Evidence completeness gate --------------------------------------
        var criticalResolvedCount = SignalCatalog.CriticalSignalIds
            .Count(id => bySignalId.TryGetValue(id, out var s) && s.Value != SignalValue.Unknown);

        var evidenceComplete = criticalResolvedCount >= MinCriticalSignalsResolved;

        var directRedFlagPresent = resolved.Any(s =>
            s.Value == SignalValue.Negative
            && SignalCatalog.Definitions[s.Id].IsDirectRedFlag);

        if (!evidenceComplete && !directRedFlagPresent)
        {
            return new RiskAssessmentResult
            {
                RiskLevel = RiskLevel.UnableToVerify,
                Confidence = Confidence.Low,
                Score = 0,
                EvidenceComplete = false,
                DirectRedFlagPresent = false,
                ConflictOverrideApplied = false,
                ResolvedSignals = resolved,
                UnresolvedSignals = unresolved
            };
        }

        // --- Weighted score ----------------------------------------------------
        var score = resolved.Sum(signal =>
        {
            var def = SignalCatalog.Definitions[signal.Id];
            if (def.Weight == 0) return 0; // e.g. DomainReachable — informational only

            var effectiveWeight = def.Weight * SourceQualityMultiplier(signal.SourceQuality);

            return signal.Value switch
            {
                SignalValue.Positive => effectiveWeight,
                SignalValue.Negative when def.PositiveOnly => 0,
                SignalValue.Negative => -effectiveWeight,
                _ => 0
            };
        });

        var roundedScore = (int)Math.Round(score);

        var riskLevel = roundedScore switch
        {
            >= LowRiskThreshold => RiskLevel.Low,
            <= HighRiskThreshold => RiskLevel.High,
            _ => RiskLevel.Medium
        };

        // --- Confidence ----------------------------------------------------------
        // Base confidence follows how much of the critical evidence resolved.
        // Note this already caps at Low when evidenceComplete is false (< 2
        // critical signals resolved), which is exactly the "confidence remains
        // Low/Medium" rule for the direct-red-flag-only path — no extra
        // capping logic needed here.
        var confidence = criticalResolvedCount switch
        {
            3 => Confidence.High,
            2 => Confidence.Medium,
            _ => Confidence.Low
        };

        return new RiskAssessmentResult
        {
            RiskLevel = riskLevel,
            Confidence = confidence,
            Score = roundedScore,
            EvidenceComplete = evidenceComplete,
            DirectRedFlagPresent = directRedFlagPresent,
            ConflictOverrideApplied = false,
            ResolvedSignals = resolved,
            UnresolvedSignals = unresolved
        };
    }

    private static double SourceQualityMultiplier(SourceQuality quality) => quality switch
    {
        SourceQuality.High => 1.0,
        SourceQuality.Medium => 0.7,
        SourceQuality.Low => 0.4,
        _ => 0.0
    };
}
