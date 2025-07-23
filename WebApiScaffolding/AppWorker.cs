using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebApiScaffolding.Interfaces;

namespace WebApiScaffolding;

public class AppWorker : BackgroundService
{
    private readonly ILogger<AppWorker> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IAnalyzeSolutionService _analyzeSolutionService;

    public AppWorker(
        ILogger<AppWorker> logger,
        IHostApplicationLifetime hostApplicationLifetime,
        IAnalyzeSolutionService analyzeSolutionService)
    {
        _logger = logger;
        _hostApplicationLifetime = hostApplicationLifetime;
        _analyzeSolutionService = analyzeSolutionService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("AppWorker is starting.");
            await _analyzeSolutionService.AnalyzeSolution();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while executing the background service.");
        }
        finally
        {
            await Task.CompletedTask; // Ensure the method is async-compliant
        }

        _hostApplicationLifetime.StopApplication();
    }
}