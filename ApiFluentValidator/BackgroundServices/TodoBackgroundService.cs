using ApiFluentValidator.Models;

namespace ApiFluentValidator.BackgroundServices;

public class TodoBackgroundService : BackgroundService
{
    private readonly ILogger<Todo> _logger;

    public TodoBackgroundService(ILogger<Todo> logger)
    {
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation(
                    $"Executed TodoItemBackgroundService");

            }
            catch (Exception ex)
            {
                _logger.LogInformation(
                    $"Failed to execute TodoItemBackgroundService with exception message {ex.Message}.");
            }
        }

        return Task.CompletedTask;
    }
}
