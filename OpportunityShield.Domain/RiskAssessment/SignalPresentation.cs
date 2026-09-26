using Oppurtunityshield.Domain.Entities;
using Oppurtunityshield.Domain.Enums;

namespace Oppurtunityshield.Domain.RiskAssessment;

public static class SignalPresentation
{
    private static readonly Dictionary<SignalId, string> Labels = new()
    {
        [SignalId.DomainAge] = "Domain registration age",
        [SignalId.DomainMatchesOrgName] = "Domain matches organization name",
        [SignalId.DomainReachable] = "Submitted link is reachable",
        [SignalId.SslValid] = "Secure connection (HTTPS)",
        [SignalId.ApplicationDomainMatchesOfficial] = "Hosted on the organization's official domain",
        [SignalId.OrganizationIdentified] = "Organization could be identified",
        [SignalId.OfficialWebsiteFound] = "Independent official website found",
        [SignalId.IndependentSourceCorroboration] = "Corroborated by independent sources",
        [SignalId.ContactEmailDomainMatch] = "Contact email matches organization domain",
        [SignalId.ConflictingInformation] = "Conflicts with official information",
        [SignalId.PaymentRequestPresent] = "Requests payment before starting",
        [SignalId.SensitiveInfoRequest] = "Requests unnecessary sensitive information",
        [SignalId.UrgencyPressureLanguage] = "Uses urgency/pressure tactics",
        [SignalId.UnrealisticOffer] = "Offer seems implausible for the role"
    };

    public static List<Signal> ToSignalEntities(IReadOnlyList<EvidenceSignal> resolved)
    {
        var result = new List<Signal>();
        var order = 0;

        foreach (var s in resolved.Where(s => s.Value != SignalValue.Unknown))
        {
            var def = SignalCatalog.Definitions[s.Id];

            // Skip signals that resolved Negative but are PositiveOnly (a
            // mismatch that's deliberately neutral, not a red flag) and
            // skip zero-weight informational signals from the headline lists.
            var isNoteworthy = def.Weight > 0 || def.IsConflictOverride;
            if (!isNoteworthy) continue;
            if (s.Value == SignalValue.Negative && def.PositiveOnly) continue;

            result.Add(new Signal
            {
                Type = s.Value == SignalValue.Positive ? SignalType.Positive : SignalType.Warning,
                Description = s.Detail is { Length: > 0 } ? $"{Labels[s.Id]}: {s.Detail}" : Labels[s.Id],
                SortOrder = order++
            });
        }

        return result;
    }

    public static List<Evidence> ToEvidenceEntities(IReadOnlyList<EvidenceSignal> resolved)
    {
        var order = 0;
        return resolved.Select(s =>
        {
            var def = SignalCatalog.Definitions[s.Id];

            var impact = s.Value switch
            {
                SignalValue.Positive => EvidenceImpact.Positive,
                SignalValue.Negative when def.PositiveOnly => EvidenceImpact.Neutral,
                SignalValue.Negative => EvidenceImpact.Negative,
                _ => EvidenceImpact.Neutral
            };

            return new Evidence
            {
                EvidenceType = s.Id.ToString(),
                Detail = s.Detail ?? Labels[s.Id],
                Impact = impact,
                SortOrder = order++
            };
        }).ToList();
    }

    /// <summary>
    /// A 0-100 display score derived purely from the engine's own output —
    /// never a number the LLM invents. Null for UnableToVerify: a numeric
    /// score would imply false precision when no real verdict was reached.
    /// </summary>
    public static int? ToDisplayTrustScore(RiskAssessmentResult result)
    {
        if (result.RiskLevel == RiskLevel.UnableToVerify) return null;

        // Score is roughly bounded by the sum of all positive weights (~90)
        // down to the sum of all negative weights (~-110). Clamp and rescale
        // to a 0-100 band centered so Medium sits near the middle.
        var clamped = Math.Clamp(result.Score, -60, 60);
        return (int)Math.Round((clamped + 60) / 120.0 * 100);
    }
}
