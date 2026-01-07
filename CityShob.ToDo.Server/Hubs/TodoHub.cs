using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace CityShob.ToDo.Server.Hubs
{
    #region Nested Types

    /// <summary>
    /// Represents the metadata for a task lock held by a client.
    /// </summary>
    public class LockInfo
    {
        public string ConnectionId { get; set; }
        public string MachineName { get; set; }
    }

    #endregion

    /// <summary>
    /// SignalR Hub managing real-time task locking and notifications.
    /// </summary>
    [HubName("todoHub")]
    public class TodoHub : Hub
    {
        #region Fields

        // Thread-safe dictionary to track active locks: TaskID -> LockInfo
        private static readonly ConcurrentDictionary<int, LockInfo> _activeLocks = new ConcurrentDictionary<int, LockInfo>();
        private readonly ILogger _logger;

        #endregion

        #region Constructor

        public TodoHub(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Hub Methods

        /// <summary>
        /// Attempts to acquire a lock for a specific task.
        /// </summary>
        /// <param name="id">The Task ID to lock.</param>
        public void LockTask(int id)
        {
            var machineName = Context.QueryString["machineName"];
            var currentLock = _activeLocks.TryGetValue(id, out var info) ? info : null;

            // 1. Validation: specific task is already locked by a DIFFERENT connection
            if (currentLock != null && currentLock.ConnectionId != Context.ConnectionId)
            {
                _logger.Warning("Lock rejected for Task {TaskId}. Locked by {Owner} (Req: {Requester})",
                    id, currentLock.ConnectionId, Context.ConnectionId);
                return;
            }

            // 2. Create Lock Info
            var newLock = new LockInfo
            {
                ConnectionId = Context.ConnectionId,
                MachineName = machineName
            };

            // 3. Try to add to dictionary
            if (_activeLocks.TryAdd(id, newLock))
            {
                _logger.Information("Task {TaskId} locked by {ConnectionId} ({Machine})", id, Context.ConnectionId, machineName);
                Clients.All.taskLocked(id, Context.ConnectionId);
            }
        }

        /// <summary>
        /// Releases the lock for a specific task.
        /// </summary>
        /// <param name="id">The Task ID to unlock.</param>
        public void UnlockTask(int id)
        {
            if (_activeLocks.TryGetValue(id, out var info))
            {
                // Security: Only the owner can unlock
                if (info.ConnectionId == Context.ConnectionId)
                {
                    if (_activeLocks.TryRemove(id, out var _))
                    {
                        _logger.Information("Task {TaskId} unlocked by {ConnectionId}", id, Context.ConnectionId);
                        Clients.All.taskUnlocked(id);
                    }
                }
                else
                {
                    _logger.Warning("Unauthorized unlock attempt for Task {TaskId} by {Requester} (Owner: {Owner})",
                        id, Context.ConnectionId, info.ConnectionId);
                }
            }
        }

        #endregion

        #region Lifecycle Overrides

        /// <summary>
        /// Called when a client connects. 
        /// Performs "Ghost Lock" cleanup to release locks held by the same machine/user from a previous (stale) session.
        /// </summary>
        public override Task OnConnected()
        {
            var myMachineName = Context.QueryString["machineName"];
            var connectionId = Context.ConnectionId;

            _logger.Debug("Client Connected: {ConnectionId} (Machine: {Machine})", connectionId, myMachineName);

            // 1. Cleanup Ghost Locks
            // If the user refreshed the browser, they have a NEW ConnectionID but the SAME MachineName.
            // We unlock any tasks still held by that MachineName to prevent them from locking themselves out.
            if (!string.IsNullOrEmpty(myMachineName))
            {
                var myGhostLocks = _activeLocks.Where(x => x.Value.MachineName == myMachineName).ToList();

                if (myGhostLocks.Any())
                {
                    _logger.Information("Cleaning up {Count} ghost locks for machine {Machine}", myGhostLocks.Count, myMachineName);

                    foreach (var kvp in myGhostLocks)
                    {
                        // Remove from memory
                        if (_activeLocks.TryRemove(kvp.Key, out var _))
                        {
                            // Broadcast unlock
                            Clients.All.taskUnlocked(kvp.Key);
                        }
                    }
                }
            }

            // 2. Sync State
            // Send the new user the current state of ALL active locks so their UI is accurate.
            foreach (var kvp in _activeLocks)
            {
                Clients.Caller.taskLocked(kvp.Key, kvp.Value.ConnectionId);
            }

            return base.OnConnected();
        }

        /// <summary>
        /// Called when a client disconnects.
        /// Releases all locks held by the disconnecting connection.
        /// </summary>
        public override Task OnDisconnected(bool stopCalled)
        {
            var connectionId = Context.ConnectionId;

            // Find all locks owned by this connection
            var userLocks = _activeLocks.Where(x => x.Value.ConnectionId == connectionId).ToList();

            if (userLocks.Any())
            {
                _logger.Information("Client Disconnected: {ConnectionId}. Releasing {Count} locks.", connectionId, userLocks.Count);

                foreach (var kvp in userLocks)
                {
                    if (_activeLocks.TryRemove(kvp.Key, out var _))
                    {
                        Clients.All.taskUnlocked(kvp.Key);
                    }
                }
            }

            return base.OnDisconnected(stopCalled);
        }

        #endregion
    }
}