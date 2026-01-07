using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin;
using Owin;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity;

[assembly: OwinStartup(typeof(CityShob.ToDo.Server.Startup))]

namespace CityShob.ToDo.Server
{
    /// <summary>
    /// OWIN Startup class responsible for configuring the HTTP pipeline, 
    /// logging, Dependency Injection, and Middleware (SignalR).
    /// </summary>
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            #region 1. Logging Configuration (Serilog)

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(
                    path: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs/server-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("Server Starting Up...");

            #endregion

            #region 2. Dependency Injection Setup (Unity)

            var container = UnityConfig.Container;
            container.RegisterInstance<ILogger>(Log.Logger);

            #endregion

            #region 3. SignalR Configuration

            // Configure SignalR to use our safer Unity Resolver
            GlobalHost.DependencyResolver = new UnitySignalRDependencyResolver(container);

            app.MapSignalR(new HubConfiguration
            {
                // Detailed errors can be useful for debugging Hub/Lock issues
                EnableDetailedErrors = true
            });

            #endregion
        }
    }

    #region Helper: Unity SignalR Resolver

    /// <summary>
    /// Adapts the Unity Container to the SignalR DependencyResolver interface.
    /// This implementation proactively checks availability to avoid ResolutionFailedExceptions.
    /// </summary>
    public class UnitySignalRDependencyResolver : DefaultDependencyResolver
    {
        private readonly IUnityContainer _container;

        public UnitySignalRDependencyResolver(IUnityContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public override object GetService(Type serviceType)
        {
            // 1. If the type is explicitly registered in Unity, resolve it.
            if (_container.IsRegistered(serviceType))
            {
                return _container.Resolve(serviceType);
            }

            // 2. If it is a Hub (e.g. TodoHub), we MUST resolve it via Unity to inject dependencies (ILogger).
            //    Hubs are concrete classes and usually not explicitly registered, so IsRegistered returns false.
            if (typeof(IHub).IsAssignableFrom(serviceType))
            {
                return _container.Resolve(serviceType);
            }

            // 3. For everything else (SignalR internals), fall back to the default resolver.
            return base.GetService(serviceType);
        }

        public override IEnumerable<object> GetServices(Type serviceType)
        {
            // Only ask Unity for a list if it actually has registrations for this type.
            if (_container.IsRegistered(serviceType))
            {
                return _container.ResolveAll(serviceType).Concat(base.GetServices(serviceType));
            }

            return base.GetServices(serviceType);
        }
    }

    #endregion
}