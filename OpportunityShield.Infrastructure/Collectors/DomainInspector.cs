using System.Text.Json;
using OpportunityShield.Application.Collectors;
using Oppurtunityshield.Domain.Collectors;
using Oppurtunityshield.Domain.Enums;

namespace Oppurtunityshield.Infrastructure.Collectors;

/// <summary>
/// Uses plain HTTP + the free, keyless RDAP protocol (rdap.org) for domain
/// age. No AI provider involved — this collector can never be the reason a
/// live demo fails to rate limits or provider outages.
/// </summary>
public class DomainInspector : IDomainInspector
{
    private readonly HttpClient _http;

    public DomainInspector(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(6);
    }

    public async Task<DomainFindings> InspectAsync(OpportunitySubmission submission, CancellationToken ct = default)
    {
        if (submission.InputType != InputType.Url || string.IsNullOrWhiteSpace(submission.Host))
        {
            // Pasted text with no URL — nothing for this collector to check.
            return new DomainFindings();
        }

        var reachableTask = CheckReachabilityAsync(submission.Content, ct);
        var ageTask = CheckDomainAgeAsync(submission.Host, ct);

        await Task.WhenAll(reachableTask, ageTask);

        return new DomainFindings
        {
            Host = submission.Host,
            IsReachable = reachableTask.Result.Reachable,
            IsHttps = reachableTask.Result.IsHttps,
            DomainAgeDays = ageTask.Result
        };
    }

    private async Task<(bool? Reachable, bool? IsHttps)> CheckReachabilityAsync(string url, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await _http.SendAsync(request, ct);
            return (true, url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            // Unreachable is itself informative (handled by SignalDerivation),
            // but a thrown exception here must never take down the whole analysis.
            return (false, null);
        }
    }

    private async Task<int?> CheckDomainAgeAsync(string host, CancellationToken ct)
    {
        try
        {
            using var response = await _http.GetAsync($"https://rdap.org/domain/{host}", ct);
            if (!response.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            if (!doc.RootElement.TryGetProperty("events", out var events)) return null;

            foreach (var evt in events.EnumerateArray())
            {
                if (evt.TryGetProperty("eventAction", out var action)
                    && action.GetString() == "registration"
                    && evt.TryGetProperty("eventDate", out var dateProp)
                    && DateTime.TryParse(dateProp.GetString(), out var registeredAt))
                {
                    return (int)(DateTime.UtcNow - registeredAt).TotalDays;
                }
            }

            return null;
        }
        catch
        {
            // RDAP coverage isn't universal (varies by TLD/registry) — a
            // miss here just means DomainAge stays Unknown, not a failure.
            return null;
        }
    }
}
