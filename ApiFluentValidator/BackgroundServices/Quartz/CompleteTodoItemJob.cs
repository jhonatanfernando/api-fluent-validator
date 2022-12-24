using ApiFluentValidator.Services;
using Quartz;

namespace ApiFluentValidator.BackgroundServices.Quartz;

[DisallowConcurrentExecution]
public class CompleteTodoItemJob : IJob
{
    private readonly ILogger<CompleteTodoItemJob> _logger;
    private readonly ITodoService _todoService;

    public CompleteTodoItemJob(ILogger<CompleteTodoItemJob> logger, ITodoService todoService)
    {
        _logger = logger;
        _todoService = todoService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Getting all not completed Todos!");
        var todos = await _todoService.GetAllNotCompleted();

        if(todos.Count == 0)
            _logger.LogInformation("There is not Todo to be completed!");

        foreach (var todo in todos)
        {
            _logger.LogInformation($"Completing the Todo: {todo.Name}");

            await _todoService.CompleteTodo(todo);
        }
    }
}
