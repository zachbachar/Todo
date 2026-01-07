using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using CityShob.ToDo.Contract.DTOs;
using CityShob.ToDo.Server.Extensions;
using CityShob.ToDo.Server.Models;
using CityShob.ToDo.Server.Persistence;
using CityShob.ToDo.Server.Services;
using Serilog;

namespace CityShob.ToDo.Server.Repositories
{
    /// <summary>
    /// A SQL-based implementation of the Todo Repository using Entity Framework.
    /// Handles CRUD operations, Many-to-Many Tag relationships, and Real-time Broadcasting.
    /// </summary>
    public class SqlTodoRepository : ITodoRepository
    {
        #region Fields

        private readonly AppDbContext _context;
        private readonly ITodoBroadcaster _broadcaster;
        private readonly ILogger _logger;

        #endregion

        #region Constructor

        public SqlTodoRepository(AppDbContext context, ITodoBroadcaster broadcaster, ILogger logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _broadcaster = broadcaster ?? throw new ArgumentNullException(nameof(broadcaster));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Read Operations

        public async Task<List<TodoItemDto>> GetAllAsync(string tagFilter = null)
        {
            _logger.Debug("GetAllAsync called. Filter: {TagFilter}", tagFilter ?? "None");

            try
            {
                var query = _context.TodoItems
                                    .Include(t => t.Tags)
                                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(tagFilter))
                {
                    // Filter where ANY tag matches the filter string
                    query = query.Where(t => t.Tags.Any(tag => tag.Name == tagFilter));
                }

                var entities = await query.ToListAsync();
                var dtos = entities.Select(e => e.ToDto()).ToList();

                _logger.Information("GetAllAsync returning {Count} items.", dtos.Count);
                return dtos;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetAllAsync failed.");
                throw;
            }
        }

        public async Task<TodoItemDto> GetByIdAsync(int id)
        {
            _logger.Debug("GetByIdAsync called for Id={Id}", id);

            var entity = await _context.TodoItems
                                       .Include(t => t.Tags)
                                       .FirstOrDefaultAsync(t => t.Id == id);

            if (entity == null)
            {
                _logger.Information("GetByIdAsync: Item not found (Id={Id}).", id);
                return null;
            }

            return entity.ToDto();
        }

        #endregion

        #region Write Operations

        public async Task AddAsync(TodoItemDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            _logger.Debug("AddAsync processing new item: {Title}", dto.Title);

            // 1. Map DTO -> Entity
            var entity = dto.ToEntity();

            // 2. Resolve Tags (Optimized Batch Approach)
            // This helper determines which tags are new and which already exist.
            if (dto.Tags != null && dto.Tags.Any())
            {
                entity.Tags = await ResolveTagsAsync(dto.Tags);
            }

            // 3. Persist
            _context.TodoItems.Add(entity);

            try
            {
                await _context.SaveChangesAsync();
                _logger.Information("AddAsync persisted new Item. Id={Id}", entity.Id);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "AddAsync failed to save to database.");
                throw;
            }

            // 4. Update DTO with the generated ID
            dto.Id = entity.Id;

            // 5. Broadcast
            await SafeBroadcastAsync(() => _broadcaster.NotifyTaskCreatedAsync(dto), "NotifyTaskCreated");
        }

        public async Task UpdateAsync(TodoItemDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            _logger.Debug("UpdateAsync processing Id={Id}", dto.Id);

            var existingItem = await _context.TodoItems
                                             .Include(t => t.Tags)
                                             .FirstOrDefaultAsync(t => t.Id == dto.Id);

            if (existingItem == null)
            {
                _logger.Warning("UpdateAsync: Item Id={Id} not found.", dto.Id);
                return;
            }

            // 1. Update Scalar Properties
            existingItem.Title = dto.Title;
            existingItem.IsCompleted = dto.IsCompleted;
            existingItem.Priority = (Models.TodoPriority)dto.Priority;
            existingItem.DueDate = dto.DueDate;

            // 2. Update Tags
            // We clear the current relationships and re-establish them based on the input.
            // EF will handle the join table updates.
            if (dto.Tags != null)
            {
                existingItem.Tags.Clear();
                if (dto.Tags.Any())
                {
                    var resolvedTags = await ResolveTagsAsync(dto.Tags);
                    foreach (var tag in resolvedTags)
                    {
                        existingItem.Tags.Add(tag);
                    }
                }
            }

            // 3. Persist
            try
            {
                await _context.SaveChangesAsync();
                _logger.Information("UpdateAsync saved changes for Id={Id}", dto.Id);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "UpdateAsync failed to save changes for Id={Id}", dto.Id);
                throw;
            }

            // 4. Broadcast
            await SafeBroadcastAsync(() => _broadcaster.NotifyTaskUpdatedAsync(dto), "NotifyTaskUpdated");
        }

        public async Task DeleteAsync(int id)
        {
            _logger.Debug("DeleteAsync called for Id={Id}", id);

            var item = await _context.TodoItems.FindAsync(id);
            if (item == null)
            {
                _logger.Warning("DeleteAsync: Item Id={Id} not found.", id);
                return;
            }

            _context.TodoItems.Remove(item);

            try
            {
                await _context.SaveChangesAsync();
                _logger.Information("DeleteAsync removed Id={Id}", id);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "DeleteAsync failed to remove Id={Id}", id);
                throw;
            }

            // Broadcast
            await SafeBroadcastAsync(() => _broadcaster.NotifyTaskDeletedAsync(id), "NotifyTaskDeleted");
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Analyzes a list of Tag DTOs and resolves them against the database.
        /// - If a tag with the same name exists, returns the existing Entity.
        /// - If not, creates a new (untracked) Tag Entity.
        /// </summary>
        private async Task<List<Tag>> ResolveTagsAsync(List<TagDto> tagDtos)
        {
            var result = new List<Tag>();
            if (tagDtos == null || !tagDtos.Any()) return result;

            // Normalize input names
            var distinctNames = tagDtos.Select(t => t.Name.Trim())
                                       .Where(n => !string.IsNullOrEmpty(n))
                                       .Distinct()
                                       .ToList();

            if (!distinctNames.Any()) return result;

            // Fetch existing tags from DB
            var existingTags = await _context.Tags
                                             .Where(t => distinctNames.Contains(t.Name))
                                             .ToListAsync();

            foreach (var name in distinctNames)
            {
                var match = existingTags.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    result.Add(match); // Reuse existing Tag
                }
                else
                {
                    result.Add(new Tag { Name = name }); // Create new Tag
                }
            }

            return result;
        }

        /// <summary>
        /// Wraps SignalR calls in a try-catch to ensure that broadcasting errors 
        /// do not rollback the successful DB transaction.
        /// </summary>
        private async Task SafeBroadcastAsync(Func<Task> broadcastAction, string actionName)
        {
            try
            {
                await broadcastAction();
            }
            catch (Exception ex)
            {
                // We log as a warning because the DB persistence succeeded, only real-time update failed.
                _logger.Warning(ex, "Broadcasting failed: {ActionName}", actionName);
            }
        }

        #endregion
    }
}