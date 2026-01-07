using System.Collections.Generic;
using System.Threading.Tasks;
using CityShob.ToDo.Contract.DTOs;

namespace CityShob.ToDo.Server.Repositories
{
    /// <summary>
    /// Defines the contract for data persistence operations related to Todo Items.
    /// Handles abstraction between the Controller and the Database/ORM.
    /// </summary>
    public interface ITodoRepository
    {
        /// <summary>
        /// Retrieves all Todo items, optionally filtering by a specific tag.
        /// </summary>
        /// <param name="tagFilter">The tag name to filter by. If null or empty, returns all items.</param>
        /// <returns>A list of Todo Item DTOs.</returns>
        Task<List<TodoItemDto>> GetAllAsync(string tagFilter = null);

        /// <summary>
        /// Retrieves a single Todo item by its unique identifier.
        /// </summary>
        /// <param name="id">The unique ID of the item.</param>
        /// <returns>The matching Todo Item DTO, or null if not found.</returns>
        Task<TodoItemDto> GetByIdAsync(int id);

        /// <summary>
        /// Adds a new Todo item to the persistent store.
        /// </summary>
        /// <param name="item">The DTO containing the data to create.</param>
        Task AddAsync(TodoItemDto item);

        /// <summary>
        /// Updates an existing Todo item in the persistent store.
        /// </summary>
        /// <param name="item">The DTO containing the updated data.</param>
        Task UpdateAsync(TodoItemDto item);

        /// <summary>
        /// Deletes a Todo item from the persistent store.
        /// </summary>
        /// <param name="id">The unique ID of the item to delete.</param>
        Task DeleteAsync(int id);
    }
}