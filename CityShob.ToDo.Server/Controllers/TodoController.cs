using System;
using System.Threading.Tasks;
using System.Web.Http;
using CityShob.ToDo.Contract.DTOs;
using CityShob.ToDo.Server.Repositories;
using Serilog;

namespace CityShob.ToDo.Server.Controllers
{
    /// <summary>
    /// API Controller for managing Todo items.
    /// Handles CRUD operations, logging, and delegation to the repository.
    /// </summary>
    [RoutePrefix("api/todo")]
    public class TodoController : ApiController
    {
        private readonly ITodoRepository _repository;
        private readonly ILogger _logger;

        #region Constructor

        public TodoController(ITodoRepository repository, ILogger logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Public Actions

        /// <summary>
        /// Retrieves all Todo items, optionally filtered by a specific tag.
        /// </summary>
        /// <param name="tag">The tag name to filter by (optional).</param>
        /// <returns>A list of Todo items.</returns>
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetAll([FromUri] string tag = null)
        {
            try
            {
                var dtos = await _repository.GetAllAsync(tag);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to retrieve all tasks (Tag: {Tag}).", tag);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Creates a new Todo item.
        /// </summary>
        /// <param name="dto">The Todo item to create.</param>
        /// <returns>The created item with its new ID.</returns>
        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> Create(TodoItemDto dto)
        {
            if (dto == null)
            {
                _logger.Warning("Create task failed: DTO is null.");
                return BadRequest("Task cannot be null");
            }

            if (!ModelState.IsValid)
            {
                _logger.Warning("Create task failed: Invalid Model State. {@ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                // Repository handles DTO->Entity mapping, Saving, and Broadcasting.
                // It updates 'dto.Id' by reference upon success.
                await _repository.AddAsync(dto);

                return Created($"api/todo/{dto.Id}", dto);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to create task {@TaskDto}.", dto);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Updates an existing Todo item.
        /// </summary>
        /// <param name="id">The ID of the item to update.</param>
        /// <param name="dto">The updated item data.</param>
        /// <returns>The updated item.</returns>
        [HttpPut]
        [Route("{id}")]
        public async Task<IHttpActionResult> Update(int id, TodoItemDto dto)
        {
            if (dto == null)
            {
                _logger.Warning("Update task failed: DTO is null.");
                return BadRequest("Task cannot be null");
            }

            if (id != dto.Id)
            {
                _logger.Warning("Update task failed: ID mismatch (URL: {UrlId}, Body: {BodyId}).", id, dto.Id);
                return BadRequest("ID mismatch");
            }

            if (!ModelState.IsValid)
            {
                _logger.Warning("Update task failed: Invalid Model State. {@ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                await _repository.UpdateAsync(dto);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to update task {TaskId}.", id);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Deletes a Todo item by its ID.
        /// </summary>
        /// <param name="id">The ID of the item to delete.</param>
        /// <returns>HTTP 200 OK if successful.</returns>
        [HttpDelete]
        [Route("{id}")]
        public async Task<IHttpActionResult> Delete(int id)
        {
            try
            {
                await _repository.DeleteAsync(id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to delete task {TaskId}.", id);
                return InternalServerError(ex);
            }
        }

        #endregion
    }
}