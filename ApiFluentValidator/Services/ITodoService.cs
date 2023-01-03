using ApiFluentValidator.Models;

namespace ApiFluentValidator.Services;

public interface ITodoService
{
    Task<List<Todo>> GetAll();
    Task<List<Todo>> GetAllWithDistributedCache();
}
