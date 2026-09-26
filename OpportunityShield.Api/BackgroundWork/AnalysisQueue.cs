using System.Threading.Channels;
using Oppurtunityshield.Application.Services;

namespace OpportunityShield.Api.BackgroundWork;

/// <summary>
/// In-memory Channel-based queue — good enough for a single-instance hackathon
/// deployment. Not durable: a restart drops anything mid-flight. If this ever
/// runs on more than one instance or needs to survive restarts, swap this for
/// a real queue (e.g. a Postgres-backed outbox table) without touching the
/// IAnalysisQueue interface or anything that depends on it.
/// </summary>
public class AnalysisQueue : IAnalysisQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public void Enqueue(Guid analysisId) => _channel.Writer.TryWrite(analysisId);

    public async Task<Guid> DequeueAsync(CancellationToken ct) =>
        await _channel.Reader.ReadAsync(ct);
}
