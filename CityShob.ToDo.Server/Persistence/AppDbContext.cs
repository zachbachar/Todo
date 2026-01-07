using CityShob.ToDo.Server.Migrations;
using CityShob.ToDo.Server.Models;
using System.Data.Entity;

namespace CityShob.ToDo.Server.Persistence
{
    /// <summary>
    /// Represents the primary database context for the application.
    /// Manages the connection to the database and provides access to entity sets.
    /// </summary>
    public class AppDbContext : DbContext
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the context using the "DefaultConnection" connection string.
        /// </summary>
        public AppDbContext() : base("name=DefaultConnection")
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<AppDbContext, Configuration>());
        }

        #endregion

        #region Entity Sets

        // 'virtual' keyword is added to enable mocking in Unit Tests.
        public virtual DbSet<TodoItem> TodoItems { get; set; }
        public virtual DbSet<Tag> Tags { get; set; }

        #endregion
    }
}