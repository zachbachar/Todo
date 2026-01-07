namespace CityShob.ToDo.Client.Services
{
    /// <summary>
    /// Contains constant string literals used for SignalR communication.
    /// These must match the Hub name and method names defined on the Server.
    /// </summary>
    public static class SignalRConstants
    {
        #region Hub Configuration

        /// <summary>
        /// The name of the Hub class on the server (used for connection).
        /// </summary>
        public const string HubName = "todoHub";

        #endregion

        #region Server Methods (Invoked by Client)

        /// <summary>
        /// Method name to call on the server to lock a task.
        /// </summary>
        public const string LockTask = "LockTask";

        /// <summary>
        /// Method name to call on the server to unlock a task.
        /// </summary>
        public const string UnlockTask = "UnlockTask";

        #endregion

        #region Client Events (Invoked by Server)

        /// <summary>
        /// Event fired by the server when a new task is created.
        /// </summary>
        public const string TaskCreated = "taskCreated";

        /// <summary>
        /// Event fired by the server when an existing task is updated.
        /// </summary>
        public const string TaskUpdated = "taskUpdated";

        /// <summary>
        /// Event fired by the server when a task is deleted.
        /// </summary>
        public const string TaskDeleted = "taskDeleted";

        /// <summary>
        /// Event fired by the server when a task is successfully locked by a user.
        /// </summary>
        public const string TaskLocked = "taskLocked";

        /// <summary>
        /// Event fired by the server when a task is unlocked.
        /// </summary>
        public const string TaskUnlocked = "taskUnlocked";

        #endregion
    }
}