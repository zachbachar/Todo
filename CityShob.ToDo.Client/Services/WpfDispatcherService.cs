using System;
using System.Threading.Tasks;
using System.Windows;

namespace CityShob.ToDo.Client.Services
{
    /// <summary>
    /// Implementation of IDispatcherService that uses the WPF Application.Current.Dispatcher
    /// to marshal actions onto the UI thread.
    /// </summary>
    public class WpfDispatcherService : IDispatcherService
    {
        public void Invoke(Action action)
        {
            if (action == null) return;

            // If the application is running and has a dispatcher, use it.
            if (Application.Current?.Dispatcher != null)
            {
                // CheckAccess returns true if we are already on the UI thread.
                // In that case, we can just execute the action directly to avoid overhead.
                if (Application.Current.Dispatcher.CheckAccess())
                {
                    action();
                }
                else
                {
                    try
                    {
                        Application.Current.Dispatcher.Invoke(action);
                    }
                    catch (TaskCanceledException)
                    {
                        // Ignore: This can happen if the app shuts down 
                        // while the dispatcher is trying to process the action.
                    }
                }
            }
            else
            {
                // Fallback for scenarios without a message loop (e.g., Unit Tests)
                action();
            }
        }

        public bool HasShutdownStarted
        {
            get
            {
                if (Application.Current?.Dispatcher == null)
                    return false;

                return Application.Current.Dispatcher.HasShutdownStarted;
            }
        }
    }
}