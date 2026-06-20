namespace PodOSphere.Worker;

public sealed class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Pod-o-Sphere worker started; no Phase 0 jobs are registered.");

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}

