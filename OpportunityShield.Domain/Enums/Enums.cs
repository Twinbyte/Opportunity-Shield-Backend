namespace Oppurtunityshield.Domain.Enums;

public enum InputType
{
    Url,
    Text
}

public enum AnalysisStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public enum RiskLevel
{
    Low,
    Medium,
    High,

    // Evidence was insufficient or conflicting to reach a real verdict.
    // This is a legitimate, first-class outcome — not an error state.
    // Reserve AnalysisStatus.Failed for actual system/API failures instead.
    UnableToVerify
}

// Tracked separately from RiskLevel. A result can be e.g. High risk + Low
// confidence ("concerning signals, but evidence is incomplete") — collapsing
// these into one score would hide that distinction from the student.
public enum Confidence
{
    Low,
    Medium,
    High
}

// Distinguishes a positiveSignals entry from a warningSignals entry
public enum SignalType
{
    Positive,
    Warning
}

public enum EvidenceImpact
{
    Positive,
    Negative,
    Neutral
}

public enum ActualOutcome
{
    Unknown,
    Legit,
    Scam,
    Unsure
}
