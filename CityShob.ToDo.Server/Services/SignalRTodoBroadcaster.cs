using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using CityShob.ToDo.Server.Hubs;
using CityShob.ToDo.Contract.DTOs;

namespace CityShob.ToDo.Server.Services
{
    /// <summary>
    /// Implementation of <see cref="ITodoBroadcaster"/> using SignalR 2.
    /// Bridges the gap between Repository/Controller logic and the real-time Hub clients.
    /// </summary>
    public class SignalRTodoBroadcaster : ITodoBroadcaster
    {
        #region Fields & Properties

        private IHubContext _hub;

        /// <summary>
        /// Gets the current Hub Context for the TodoHub.
        /// Lazily initialized to ensure SignalR is ready.
        /// </summary>
        private IHubContext Hub
        {
            get
            {
                if (_hub == null)
                {
                    _hub = GlobalHost.ConnectionManager.GetHubContext<TodoHub>();
                }
                return _hub;
            }
        }

        #endregion

        #region ITodoBroadcaster Implementation - Data Sync

        /// <summary>
        /// Invokes the 'taskCreated' method on all connected clients.
        /// </summary>
        public Task NotifyTaskCreatedAsync(TodoItemDto item)
        {
            Hub.Clients.All.taskCreated(item);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Invokes the 'taskUpdated' method on all connected clients.
        /// </summary>
        public Task NotifyTaskUpdatedAsync(TodoItemDto item)
        {
            Hub.Clients.All.taskUpdated(item);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Invokes the 'taskDeleted' method on all connected clients.
        /// </summary>
        public Task NotifyTaskDeletedAsync(int id)
        {
            Hub.Clients.All.taskDeleted(id);
            return Task.CompletedTask;
        }

        #endregion

        #region ITodoBroadcaster Implementation - Locking Sync

        /// <summary>
        /// Invokes the 'taskLocked' method on all connected clients.
        /// Used by the Repository to sync lock state during persistence operations.
        /// </summary>
        /// <param name="id">The ID of the task being locked.</param>
        /// <param name="connectionId">The SignalR Connection ID of the owner.</param>
        public Task NotifyTaskLockedAsync(int id, string connectionId)
        {
            Hub.Clients.All.taskLocked(id, connectionId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Invokes the 'taskUnlocked' method on all connected clients.
        /// </summary>
        /// <param name="id">The ID of the task being released.</param>
        public Task NotifyTaskUnlockedAsync(int id)
        {
            Hub.Clients.All.taskUnlocked(id);
            return Task.CompletedTask;
        }

        #endregion
    }
}