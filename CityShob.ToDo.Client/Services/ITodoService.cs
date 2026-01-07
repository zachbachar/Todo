using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CityShob.ToDo.Contract.DTOs;

namespace CityShob.ToDo.Client.Services
{
    /// <summary>
    /// Defines the contract for the main business service handling ToDo items.
    /// This includes CRUD operations via Web API and real-time synchronization via SignalR.
    /// </summary>
    public interface ITodoService
    {
        /// <summary>
        /// Gets the unique Connection ID assigned to this client by the real-time server.
        /// Used to identify self-initiated locks versus locks by other users.
        /// </summary>
        string MyConnectionId { get; }

        /// <summary>
        /// Establishes the connection to the server (SignalR hub) and initializes the service.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ConnectAsync();

        /// <summary>
        /// Retrieves all Todo items from the server, optionally filtered by a tag.
        /// </summary>
        /// <param name="tagFilter">The tag to filter by, or null to retrieve all items.</param>
        /// <returns>A task containing the list of Todo items.</returns>
        Task<List<TodoItemDto>> GetAllAsync(string tagFilter = null);

        /// <summary>
        /// Adds a new Todo item.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddAsync(TodoItemDto item);

        /// <summary>
        /// Updates an existing Todo item.
        /// </summary>
        /// <param name="item">The item with updated values.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateAsync(TodoItemDto item);

        /// <summary>
        /// Deletes a Todo item by its identifier.
        /// </summary>
        /// <param name="id">The identifier of the item to delete.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteAsync(int id);

        /// <summary>
        /// Requests a lock on a specific task to prevent other users from editing it.
        /// </summary>
        /// <param name="id">The identifier of the task to lock.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task LockTaskAsync(int id);

        /// <summary>
        /// Releases the lock on a specific task, allowing others to edit it.
        /// </summary>
        /// <param name="id">The identifier of the task to unlock.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UnlockTaskAsync(int id);

        /// <summary>
        /// Occurs when a new item is created by any client (including this one).
        /// </summary>
        event Action<TodoItemDto> ItemCreated;

        /// <summary>
        /// Occurs when an item is updated by any client.
        /// </summary>
        event Action<TodoItemDto> ItemUpdated;

        /// <summary>
        /// Occurs when an item is deleted by any client.
        /// </summary>
        event Action<int> ItemDeleted;

        /// <summary>
        /// Occurs when a task is locked by a user.
        /// Arguments: (TaskId, ConnectionId of the user who locked it).
        /// </summary>
        event Action<int, string> TaskLocked;

        /// <summary>
        /// Occurs when a task is unlocked.
        /// Arguments: (TaskId).
        /// </summary>
        event Action<int> TaskUnlocked;

        /// <summary>
        /// Occurs when the connection state to the real-time server changes.
        /// Arguments: (IsConnected).
        /// </summary>
        event Action<bool> ConnectionStateChanged;
    }
}