using Oppurtunityshield.Domain.Collectors;

namespace Oppurtunityshield.Domain.RiskAssessment;

/// <summary>
/// Turns raw DomainFindings + OrganizationFindings into EvidenceSignals.
/// Every comparison here (does this domain match that org, is this the
/// official domain) is plain string/host logic — never delegated to the AI
/// that produced the raw facts. This is the piece that makes
/// "the LLM only explains, never decides" actually hold at the signal level,
/// not just at the final risk verdict.
/// </summary>
public static class SignalDerivation
{
    public static IReadOnlyList<EvidenceSignal> FromDomainFindings(DomainFindings findings)
    {
        var signals = new List<EvidenceSignal>();

        if (findings.IsReachable is { } reachable)
        {
            signals.Add(new EvidenceSignal
            {
                Id = SignalId.DomainReachable,
                Value = reachable ? SignalValue.Positive : SignalValue.Negative,
                SourceQuality = SourceQuality.High,
                Source = "http_check",
                Detail = reachable ? null : "The submitted URL could not be reached."
            });
        }

        if (findings.IsHttps is { } https)
        {
            signals.Add(new EvidenceSignal
            {
                Id = SignalId.SslValid,
                Value = https ? SignalValue.Positive : SignalValue.Negative,
                SourceQuality = SourceQuality.High,
                Source = "http_check"
            });
        }

        if (findings.DomainAgeDays is { } ageDays)
        {
            // Weak in both directions by design — see earlier discussion:
            // domain age alone proves little either way.
            var value = ageDays switch
            {
                < 30 => SignalValue.Negative,
                > 365 => SignalValue.Positive,
                _ => SignalValue.Unknown // neutral middle ground — not scored
            };

            if (value != SignalValue.Unknown)
            {
                signals.Add(new EvidenceSignal
                {
                    Id = SignalId.DomainAge,
                    Value = value,
                    SourceQuality = SourceQuality.High,
                    Source = "rdap",
                    Detail = $"Domain registered approximately {ageDays} days ago."
                });
            }
        }

        return signals;
    }

    public static IReadOnlyList<EvidenceSignal> FromOrganizationFindings(
        OrganizationFindings findings, string? submittedHost)
    {
        var signals = new List<EvidenceSignal>();

        signals.Add(new EvidenceSignal
        {
            Id = SignalId.OrganizationIdentified,
            Value = string.IsNullOrWhiteSpace(findings.OrganizationName)
                ? SignalValue.Unknown
                : SignalValue.Positive,
            SourceQuality = SourceQuality.Medium,
            Source = "gemini_search",
            Detail = findings.OrganizationName
        });

        signals.Add(new EvidenceSignal
        {
            Id = SignalId.OfficialWebsiteFound,
            Value = string.IsNullOrWhiteSpace(findings.OfficialWebsiteUrl)
                ? SignalValue.Unknown
                : SignalValue.Positive,
            SourceQuality = SourceQuality.High,
            Source = "gemini_search",
            Detail = findings.OfficialWebsiteUrl
        });

        signals.Add(new EvidenceSignal
        {
            Id = SignalId.IndependentSourceCorroboration,
            Value = findings.IndependentSources.Count > 0 ? SignalValue.Positive : SignalValue.Unknown,
            SourceQuality = HighestQuality(findings.IndependentSources),
            Source = "gemini_search",
            Detail = findings.IndependentSources.Count > 0
                ? string.Join("; ", findings.IndependentSources.Select(s => s.Description))
                : null
        });

        if (findings.HasConflictingInformation)
        {
            signals.Add(new EvidenceSignal
            {
                Id = SignalId.ConflictingInformation,
                Value = SignalValue.Negative,
                SourceQuality = findings.ConflictSourceQuality,
                Source = "gemini_search",
                Detail = findings.ConflictDetail
            });
        }

        // --- Deterministic comparisons, not Gemini's call -----------------

        if (!string.IsNullOrWhiteSpace(submittedHost) && !string.IsNullOrWhiteSpace(findings.OrganizationName))
        {
            var nameMatches = submittedHost.Contains(
                Normalize(findings.OrganizationName), StringComparison.OrdinalIgnoreCase);

            // Mismatch is neutral, not negative — many legitimate orgs use
            // unrelated domains. Only a positive match is scored (see
            // SignalDefinition.PositiveOnly for DomainMatchesOrgName).
            signals.Add(new EvidenceSignal
            {
                Id = SignalId.DomainMatchesOrgName,
                Value = nameMatches ? SignalValue.Positive : SignalValue.Negative,
                SourceQuality = SourceQuality.Medium,
                Source = "derived"
            });
        }

        if (!string.IsNullOrWhiteSpace(submittedHost) && !string.IsNullOrWhiteSpace(findings.OfficialWebsiteUrl)
            && Uri.TryCreate(findings.OfficialWebsiteUrl, UriKind.Absolute, out var officialUri))
        {
            var officialHost = officialUri.Host.Replace("www.", "", StringComparison.OrdinalIgnoreCase);
            var matches = submittedHost.Equals(officialHost, StringComparison.OrdinalIgnoreCase);

            signals.Add(new EvidenceSignal
            {
                Id = SignalId.ApplicationDomainMatchesOfficial,
                Value = matches ? SignalValue.Positive : SignalValue.Negative,
                SourceQuality = SourceQuality.High,
                Source = "derived",
                Detail = matches
                    ? null
                    : "Submission was not hosted on the organization's official domain " +
                      "(this alone is common with legitimate third-party application platforms)."
            });
        }

        if (!string.IsNullOrWhiteSpace(findings.ContactEmailDomain))
        {
            var officialHosts = new[] { submittedHost, TryGetHost(findings.OfficialWebsiteUrl) }
                .Where(h => !string.IsNullOrWhiteSpace(h));

            var matches = officialHosts.Any(h =>
                findings.ContactEmailDomain!.Equals(h, StringComparison.OrdinalIgnoreCase));

            signals.Add(new EvidenceSignal
            {
                Id = SignalId.ContactEmailDomainMatch,
                Value = matches ? SignalValue.Positive : SignalValue.Negative,
                SourceQuality = SourceQuality.Medium,
                Source = "derived",
                Detail = matches ? null : $"Contact email uses '{findings.ContactEmailDomain}', not the org's own domain."
            });
        }

        return signals;
    }

    private static SourceQuality HighestQuality(IReadOnlyList<SourceRef> sources) =>
        sources.Count == 0 ? SourceQuality.Unknown : sources.Max(s => s.Quality);

    private static string Normalize(string orgName) =>
        new string(orgName.Where(char.IsLetterOrDigit).ToArray());

    private static string? TryGetHost(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host.Replace("www.", "") : null;
}
