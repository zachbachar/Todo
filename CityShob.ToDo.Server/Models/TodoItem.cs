using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CityShob.ToDo.Server.Models
{
    /// <summary>
    /// Represents a single Todo Task entity in the database.
    /// </summary>
    public class TodoItem
    {
        #region Constructor

        public TodoItem()
        {
            // Initialize navigation collection to prevent NullReferenceExceptions
            Tags = new HashSet<Tag>();
        }

        #endregion

        #region Data Properties

        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DueDate { get; set; }

        public TodoPriority Priority { get; set; }

        /// <summary>
        /// The SignalR Connection ID of the user currently editing this item.
        /// If null, the item is not locked.
        /// </summary>
        [StringLength(50)]
        public string LockedByConnectionId { get; set; }

        #endregion

        #region Navigation Properties

        public virtual ICollection<Tag> Tags { get; set; }

        #endregion
    }
}