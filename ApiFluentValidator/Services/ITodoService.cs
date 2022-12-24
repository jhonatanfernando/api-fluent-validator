using ApiFluentValidator.Models;

namespace ApiFluentValidator.Services;

public interface ITodoService
{
    Task CompleteTodo(Todo todo);

    Task<List<Todo>> GetAllNotCompleted();
}
