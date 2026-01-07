using System;
using System.Collections.Generic;

namespace CityShob.ToDo.Contract.DTOs
{
    /// <summary>
    /// Data Transfer Object representing a single To-Do task.
    /// Shared between Client and Server for data exchange.
    /// </summary>
    public class TodoItemDto
    {
        #region Properties

        /// <summary>
        /// Gets or sets the unique identifier of the task.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the title or description of the task.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the task is completed.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Gets or sets the priority level of the task.
        /// </summary>
        public TodoPriority Priority { get; set; }

        /// <summary>
        /// Gets or sets the optional due date for the task.
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Gets or sets the list of tags associated with this task.
        /// Initialized to an empty list to avoid null reference issues.
        /// </summary>
        public List<TagDto> Tags { get; set; } = new List<TagDto>();

        /// <summary>
        /// Gets or sets the date and time when the task was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        #endregion
    }
}