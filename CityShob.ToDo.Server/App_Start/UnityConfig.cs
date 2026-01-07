using CityShob.ToDo.Server.Persistence;
using CityShob.ToDo.Server.Repositories;
using CityShob.ToDo.Server.Services;
using System;
using System.Diagnostics;
using Unity;
using Unity.Injection;
using Unity.Lifetime;

namespace CityShob.ToDo.Server
{
    /// <summary>
    /// Specifies the Unity IoC (Inversion of Control) configuration for the server.
    /// Responsible for registering dependencies and their lifecycles.
    /// </summary>
    public static class UnityConfig
    {
        #region Container Initialization

        private static readonly Lazy<IUnityContainer> _container =
          new Lazy<IUnityContainer>(() =>
          {
              var container = new UnityContainer();
              try
              {
                  RegisterTypes(container);
              }
              catch (Exception ex)
              {
                  // Critical failure: If IoC fails, the app cannot start.
                  // We log to the system debugger since no logger is available yet.
                  Trace.TraceError($"Unity Registration Failed: {ex}");
                  throw;
              }
              return container;
          });

        /// <summary>
        /// Gets the configured Unity Container instance.
        /// </summary>
        public static IUnityContainer Container => _container.Value;

        #endregion

        #region Registration Logic

        /// <summary>
        /// Registers the type mappings with the Unity container.
        /// </summary>
        /// <param name="container">The unity container to configure.</param>
        public static void RegisterTypes(IUnityContainer container)
        {
            // 1. Persistence: Use Transient so every resolution gets a fresh context
            container.RegisterType<AppDbContext>(new TransientLifetimeManager(), new InjectionConstructor());

            // 2. Repositories: Also Transient to match the context lifetime
            container.RegisterType<ITodoRepository, SqlTodoRepository>(new TransientLifetimeManager());

            // 3. Broadcaster: Singleton for Hub connectivity
            container.RegisterType<ITodoBroadcaster, SignalRTodoBroadcaster>(new ContainerControlledLifetimeManager());
        }

        #endregion
    }
}