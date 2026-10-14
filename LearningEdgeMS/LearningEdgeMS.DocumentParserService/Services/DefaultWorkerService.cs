namespace LearningEdgeMS.DocumentParserService.Services;

public class DefaultWorkerService : BackgroundService
{
    private readonly ILogger<DefaultWorkerService> _logger;        

    public DefaultWorkerService(ILogger<DefaultWorkerService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DocumentParser service has started at: {time}", DateTimeOffset.Now);

        // Intentionally left blank.
        // The consumer class will handle all background work.
        await Task.CompletedTask;
    }    
}