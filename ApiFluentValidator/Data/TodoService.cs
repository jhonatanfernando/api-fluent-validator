using ApiFluentValidator.Models;
using ApiFluentValidator.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace ApiFluentValidator.Data
{
    public class TodoService : ITodoService
    {
        private readonly string key = "get-todos";
        private readonly IMemoryCache _memoryCache;
        private readonly TodoDb _dbContext;
        private readonly IDistributedCache _distributedCache;

        public TodoService(IMemoryCache memoryCache, TodoDb dbContext, IDistributedCache distributedCache)
        {
            _memoryCache = memoryCache;
            _dbContext = dbContext;
            _distributedCache = distributedCache;
        }

        public async Task<List<Todo>> GetAll()
        {
            var todos = await _memoryCache.GetOrCreateAsync<List<Todo>>(key, async entry =>
            {
                entry.SetSlidingExpiration(TimeSpan.FromSeconds(10));
                entry.SetAbsoluteExpiration(TimeSpan.FromSeconds(20));

                return await _dbContext.Todos.ToListAsync();
            });

            return todos;
        }

        public async Task<List<Todo>> GetAllWithDistributedCache()
        {
            //Get the cached data
            byte[] cachedData = await _distributedCache.GetAsync(key);

            //Deserialize the cached response if there are any
            if (cachedData != null)
            {
                var jsonData = System.Text.Encoding.UTF8.GetString(cachedData);
                var dataResult = JsonSerializer.Deserialize<List<Todo>>(jsonData);
                if (dataResult != null)
                {
                    return dataResult;
                }
            }

            var todos = await _dbContext.Todos.ToListAsync();

            // Serialize the response
            byte[] objectToCache = JsonSerializer.SerializeToUtf8Bytes(todos);
            var cacheEntryOptions = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromSeconds(10))
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(30));

            // Cache it
            await _distributedCache.SetAsync(key, objectToCache, cacheEntryOptions);

            return todos;
        }
    }
}
