using System.Threading.Tasks;
using CityShob.ToDo.Contract.DTOs;
using CityShob.ToDo.Server.Repositories;

namespace CityShob.ToDo.Server.Services
{
    // 1. Define Interface
    public interface ITodoManager
    {
        Task AddItemAsync(TodoItemDto dto);
        Task UpdateItemAsync(TodoItemDto dto);
        Task DeleteItemAsync(int id);
    }

    // 2. Implementation
    public class TodoManager : ITodoManager
    {
        private readonly ITodoRepository _repository;
        private readonly ITodoBroadcaster _broadcaster;

        public TodoManager(ITodoRepository repository, ITodoBroadcaster broadcaster)
        {
            _repository = repository;
            _broadcaster = broadcaster;
        }

        public async Task AddItemAsync(TodoItemDto dto)
        {
            // A. Save to DB (Repository's ONLY job now)
            await _repository.AddAsync(dto);

            // B. Broadcast (Manager's job to coordinate)
            await _broadcaster.NotifyTaskCreatedAsync(dto);
        }

        // Implement Update/Delete similarly...
    }
}