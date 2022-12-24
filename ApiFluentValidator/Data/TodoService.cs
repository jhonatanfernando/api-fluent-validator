using ApiFluentValidator.Models;
using ApiFluentValidator.Services;
using Microsoft.EntityFrameworkCore;

namespace ApiFluentValidator.Data;

public class TodoService : ITodoService
{
    private readonly TodoDb _db;

    public TodoService(TodoDb db)
    {
        _db = db;
    }      

    public async Task CompleteTodo(Todo todo)
    {
        todo.IsComplete = true;
        todo.CompletedTimestamp= DateTime.Now;

        await _db.SaveChangesAsync();
    }

    public async Task<List<Todo>> GetAllNotCompleted()
    {
        return await _db.Todos.Where(c=> !c.IsComplete).ToListAsync();
    }
}
