using System;

namespace CityShob.ToDo.Client.Services
{
    /// <summary>
    /// Provides an abstraction over the UI thread dispatcher.
    /// This allows background threads (like SignalR callbacks) to update the UI
    /// and facilitates unit testing by decoupling the ViewModel from the specific WPF Dispatcher.
    /// </summary>
    public interface IDispatcherService
    {
        /// <summary>
        /// Executes the specified delegate synchronously on the UI thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        void Invoke(Action action);

        /// <summary>
        /// Gets a value indicating whether the application or dispatcher is in the process of shutting down.
        /// </summary>
        bool HasShutdownStarted { get; }
    }
}