using ApiFluentValidator.Models;

namespace ApiFluentValidator.Services
{
    public interface IUserService
    {
        Task<User> Authenticate(string username, string password);
        Task<IEnumerable<User>> GetAll();
        Task Save(User user);
    }
}
