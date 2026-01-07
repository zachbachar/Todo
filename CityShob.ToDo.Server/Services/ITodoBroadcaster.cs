using System.Threading.Tasks;
using CityShob.ToDo.Contract.DTOs;

namespace CityShob.ToDo.Server.Services
{
    /// <summary>
    /// Defines the contract for broadcasting real-time updates to connected clients.
    /// Typically implemented using SignalR to synchronize state across the system.
    /// </summary>
    public interface ITodoBroadcaster
    {
        #region Data Synchronization

        /// <summary>
        /// Notifies all clients that a new Todo item has been created.
        /// </summary>
        Task NotifyTaskCreatedAsync(TodoItemDto item);

        /// <summary>
        /// Notifies all clients that an existing Todo item has been modified.
        /// </summary>
        Task NotifyTaskUpdatedAsync(TodoItemDto item);

        /// <summary>
        /// Notifies all clients that a Todo item has been removed.
        /// </summary>
        Task NotifyTaskDeletedAsync(int id);

        #endregion

        #region Locking Synchronization

        /// <summary>
        /// Notifies all clients that a specific task is now locked for editing by a user.
        /// </summary>
        /// <param name="id">The unique ID of the task.</param>
        /// <param name="connectionId">The SignalR connection ID of the user holding the lock.</param>
        Task NotifyTaskLockedAsync(int id, string connectionId);

        /// <summary>
        /// Notifies all clients that a specific task is now free to be edited.
        /// </summary>
        /// <param name="id">The unique ID of the task.</param>
        Task NotifyTaskUnlockedAsync(int id);

        #endregion
    }
}