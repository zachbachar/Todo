
namespace CityShob.ToDo.Contract.DTOs
{
    /// <summary>
    /// Data Transfer Object representing a tag assigned to a Todo item.
    /// Used for communication between Client and Server.
    /// </summary>
    public class TagDto
    {
        #region Properties

        /// <summary>
        /// Gets or sets the unique identifier for the tag.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of the tag (e.g., "Work", "Urgent").
        /// </summary>
        public string Name { get; set; }

        #endregion
    }
}