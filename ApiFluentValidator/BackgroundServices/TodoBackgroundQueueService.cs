using ApiFluentValidator.Models;
using ApiFluentValidator.Services;

namespace ApiFluentValidator.BackgroundServices;

public class TodoBackgroundQueueService : BackgroundService
{
    private readonly IBackgroundQueue<Todo> _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TodoBackgroundQueueService> _logger;

    public TodoBackgroundQueueService(IBackgroundQueue<Todo> queue, IServiceScopeFactory scopeFactory,
      ILogger<TodoBackgroundQueueService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{Type} is now running in the background.", nameof(TodoBackgroundQueueService));

        await TodoProcessing(stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogCritical(
            "The {Type} is stopping, queued items might not be processed anymore.",
            nameof(TodoBackgroundQueueService)
        );

        return base.StopAsync(cancellationToken);
    }

    private async Task TodoProcessing(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, stoppingToken);

                var todo = _queue.Dequeue();

                if (todo == null)
                    continue;

                _logger.LogInformation("Todo Item found! Completing it ..");

                using var scope = _scopeFactory.CreateScope();
                var todoService = scope.ServiceProvider.GetRequiredService<ITodoService>();

                await todoService.CompleteTodo(todo);

                _logger.LogInformation("Todo Item is completed.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred when completing a todo. Exception: {@Exception}", ex);
            }
        }
    }
}
