using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CityShob.ToDo.Server.Models
{
    /// <summary>
    /// Represents a category or label that can be assigned to multiple Todo Items.
    /// </summary>
    public class Tag
    {
        #region Constructor

        public Tag()
        {
            // Best Practice: Initialize collection navigation properties 
            // to prevent NullReferenceExceptions when working with new entities.
            TodoItems = new HashSet<TodoItem>();
        }

        #endregion

        #region Properties

        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Collection of Todo Items associated with this tag.
        /// Marked with JsonIgnore to prevent circular reference loops during serialization.
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<TodoItem> TodoItems { get; set; }

        #endregion
    }
}