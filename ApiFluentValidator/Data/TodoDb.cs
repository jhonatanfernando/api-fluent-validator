using ApiFluentValidator.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiFluentValidator.Data;

public class TodoDb : DbContext
{
    public TodoDb(DbContextOptions<TodoDb> options)
        : base(options) { }

    public DbSet<Todo> Todos => Set<Todo>();
}
