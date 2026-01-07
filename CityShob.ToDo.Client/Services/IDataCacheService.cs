using CityShob.ToDo.Contract.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CityShob.ToDo.Client.Services
{
    /// <summary>
    /// Defines the contract for a service responsible for local data persistence/caching.
    /// </summary>
    public interface IDataCacheService
    {
        /// <summary>
        /// Asynchronously saves a list of Todo items to the local cache.
        /// </summary>
        /// <param name="items">The items to save.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SaveAsync(List<TodoItemDto> items);

        /// <summary>
        /// Asynchronously loads the list of Todo items from the local cache.
        /// </summary>
        /// <returns>A task containing the list of cached items, or null if no cache exists.</returns>
        Task<List<TodoItemDto>> LoadAsync();
    }
}