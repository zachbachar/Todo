using System.Collections.Generic;
using System.Linq;
using CityShob.ToDo.Contract.DTOs;
using CityShob.ToDo.Server.Models;

namespace CityShob.ToDo.Server.Extensions
{
    /// <summary>
    /// Provides extension methods for mapping between Server Entities and Data Transfer Objects (DTOs).
    /// </summary>
    public static class MappingExtensions
    {
        #region Entity -> DTO

        /// <summary>
        /// Converts a <see cref="TodoItem"/> entity to a <see cref="TodoItemDto"/>.
        /// </summary>
        /// <param name="item">The source entity.</param>
        /// <returns>The mapped DTO, or null if the source is null.</returns>
        public static TodoItemDto ToDto(this TodoItem item)
        {
            if (item == null) return null;

            return new TodoItemDto
            {
                Id = item.Id,
                Title = item.Title,
                IsCompleted = item.IsCompleted,
                CreatedAt = item.CreatedAt,
                DueDate = item.DueDate,
                Priority = (Contract.DTOs.TodoPriority)item.Priority,

                // Map tags, ensuring we handle potential nulls from the ORM
                Tags = item.Tags?.Select(t => new TagDto
                {
                    Id = t.Id,
                    Name = t.Name
                }).ToList() ?? new List<TagDto>()
            };
        }

        #endregion

        #region DTO -> Entity

        /// <summary>
        /// Converts a <see cref="TodoItemDto"/> to a <see cref="TodoItem"/> entity.
        /// </summary>
        /// <param name="dto">The source DTO.</param>
        /// <returns>The mapped entity, or null if the source is null.</returns>
        public static TodoItem ToEntity(this TodoItemDto dto)
        {
            if (dto == null) return null;

            var item = new TodoItem
            {
                Id = dto.Id,
                Title = dto.Title,
                IsCompleted = dto.IsCompleted,
                CreatedAt = dto.CreatedAt,
                DueDate = dto.DueDate,
                Priority = (Models.TodoPriority)dto.Priority,

                // Create detached Tag entities. 
                // Note: The Repository is responsible for reconciling these with existing DB records.
                Tags = dto.Tags?.Select(t => new Tag
                {
                    Id = t.Id,
                    Name = t.Name
                }).ToList() ?? new List<Tag>()
            };

            return item;
        }

        #endregion
    }
}