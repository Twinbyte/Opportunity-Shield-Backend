using Oppurtunityshield.Application.Services;

namespace OpportunityShield.Api.BackgroundWork;

/// <summary>
/// Drains the queue and runs the orchestrator for each item. AnalysisQueue
/// is a singleton but IAnalysisOrchestrator (and everything it depends on —
/// the DbContext, repositories, HTTP clients) is Scoped, so each dequeued
/// item gets its own DI scope rather than sharing one across the whole
/// app lifetime.
/// </summary>
public class AnalysisProcessingService : BackgroundService
{
    private readonly IAnalysisQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnalysisProcessingService> _logger;

    public AnalysisProcessingService(
        IAnalysisQueue queue, IServiceScopeFactory scopeFactory, ILogger<AnalysisProcessingService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Guid analysisId;
            try
            {
                analysisId = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break; // shutting down
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IAnalysisOrchestrator>();
                await orchestrator.RunAsync(analysisId, stoppingToken);
            }
            catch (Exception ex)
            {
                // The orchestrator already catches its own failures and writes
                // AnalysisStatus.Failed. Reaching here means something went wrong
                // even outside that — e.g. the DB itself is unreachable. Log and
                // keep the worker alive rather than taking down all future analyses.
                _logger.LogError(ex, "Unhandled error processing analysis {AnalysisId}", analysisId);
            }
        }
    }
}
