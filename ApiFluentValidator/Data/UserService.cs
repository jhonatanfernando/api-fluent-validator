using ApiFluentValidator.Models;
using ApiFluentValidator.Services;
using Microsoft.EntityFrameworkCore;

namespace ApiFluentValidator.Data
{
    public class UserService : IUserService
    {
        private readonly TodoDb _dbContext;

        public UserService(TodoDb todoDb)
        {
            this._dbContext = todoDb;
        }   

        public async Task<User> Authenticate(string username, string password)
        {
            var user = await Task.Run(() => _dbContext.Users.SingleOrDefault(x => x.Username == username && x.Password == password));

            return user;
        }

        public async Task<IEnumerable<User>> GetAll()
        {
            return await _dbContext.Users.ToListAsync();
        }

        public async Task Save(User user)
        {
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
        }
    }
}
