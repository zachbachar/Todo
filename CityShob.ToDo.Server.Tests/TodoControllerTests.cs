using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using CityShob.ToDo.Server.Controllers;
using CityShob.ToDo.Contract.DTOs;
using CityShob.ToDo.Server.Repositories;
using Serilog;

namespace CityShob.ToDo.Server.Tests
{
    /// <summary>
    /// Unit tests for the TodoController.
    /// Focuses on validating HTTP response codes, Modelstate handling, and Repository delegation.
    /// </summary>
    [TestClass]
    public class TodoControllerTests
    {
        #region Fields & Setup

        private Mock<ITodoRepository> _mockRepo;
        private Mock<ILogger> _mockLogger;
        private TodoController _controller;

        [TestInitialize]
        public void Setup()
        {
            _mockRepo = new Mock<ITodoRepository>();
            _mockLogger = new Mock<ILogger>();

            _controller = new TodoController(_mockRepo.Object, _mockLogger.Object);
        }

        #endregion

        #region GetAll Tests

        [TestMethod]
        public async Task GetAll_ShouldReturnOkWithDtos()
        {
            // --- ARRANGE ---
            var fakeDtos = new List<TodoItemDto>
            {
                new TodoItemDto { Id = 1, Title = "Task 1", IsCompleted = false },
                new TodoItemDto { Id = 2, Title = "Task 2", IsCompleted = true }
            };

            _mockRepo.Setup(repo => repo.GetAllAsync(It.IsAny<string>()))
                     .ReturnsAsync(fakeDtos);

            // --- ACT ---
            var actionResult = await _controller.GetAll();

            // --- ASSERT ---
            var contentResult = actionResult as OkNegotiatedContentResult<List<TodoItemDto>>;

            Assert.IsNotNull(contentResult);
            Assert.HasCount(2, contentResult.Content);
            Assert.AreEqual("Task 1", contentResult.Content[0].Title);
        }

        [TestMethod]
        public async Task GetAll_ShouldReturnInternalServerError_OnException()
        {
            // --- ARRANGE ---
            var expectedException = new Exception("DB Failure");
            _mockRepo.Setup(r => r.GetAllAsync(It.IsAny<string>()))
                     .ThrowsAsync(expectedException);

            // --- ACT ---
            var actionResult = await _controller.GetAll();

            // --- ASSERT ---
            // Change from InternalServerErrorResult to ExceptionResult
            var errorResult = actionResult as ExceptionResult;

            Assert.IsNotNull(errorResult, "Expected result to be of type ExceptionResult");
            Assert.AreEqual(expectedException, errorResult.Exception);
        }

        #endregion

        #region Create Tests

        [TestMethod]
        public async Task Create_ShouldReturnCreated_AndAssignId()
        {
            // --- ARRANGE ---
            var newDto = new TodoItemDto { Title = "New Task" };

            _mockRepo.Setup(r => r.AddAsync(It.IsAny<TodoItemDto>()))
                     .Callback<TodoItemDto>(dto => dto.Id = 100)
                     .Returns(Task.CompletedTask);

            // --- ACT ---
            var actionResult = await _controller.Create(newDto);

            // --- ASSERT ---
            _mockRepo.Verify(r => r.AddAsync(newDto), Times.Once);

            var result = actionResult as CreatedNegotiatedContentResult<TodoItemDto>;
            Assert.IsNotNull(result);
            Assert.AreEqual(100, result.Content.Id);
            Assert.AreEqual("api/todo/100", result.Location.ToString());
        }

        [TestMethod]
        public async Task Create_ShouldReturnBadRequest_WhenDtoIsNull()
        {
            // --- ACT ---
            var actionResult = await _controller.Create(null);

            // --- ASSERT ---
            Assert.IsInstanceOfType(actionResult, typeof(BadRequestErrorMessageResult));
        }

        #endregion

        #region Update Tests

        [TestMethod]
        public async Task Update_ShouldReturnOk_WhenSuccessful()
        {
            // --- ARRANGE ---
            int targetId = 5;
            var updateDto = new TodoItemDto { Id = targetId, Title = "Updated" };

            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<TodoItemDto>()))
                     .Returns(Task.CompletedTask);

            // --- ACT ---
            var actionResult = await _controller.Update(targetId, updateDto);

            // --- ASSERT ---
            _mockRepo.Verify(r => r.UpdateAsync(updateDto), Times.Once);
            Assert.IsInstanceOfType(actionResult, typeof(OkNegotiatedContentResult<TodoItemDto>));
        }

        [TestMethod]
        public async Task Update_ShouldReturnBadRequest_IfIdMismatch()
        {
            // --- ARRANGE ---
            int urlId = 5;
            var dto = new TodoItemDto { Id = 999 };

            // --- ACT ---
            var actionResult = await _controller.Update(urlId, dto);

            // --- ASSERT ---
            Assert.IsInstanceOfType(actionResult, typeof(BadRequestErrorMessageResult));
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<TodoItemDto>()), Times.Never);
        }

        #endregion

        #region Delete Tests

        [TestMethod]
        public async Task Delete_ShouldReturnOk_AfterRepoCall()
        {
            // --- ARRANGE ---
            int targetId = 10;
            _mockRepo.Setup(r => r.DeleteAsync(targetId)).Returns(Task.CompletedTask);

            // --- ACT ---
            var actionResult = await _controller.Delete(targetId);

            // --- ASSERT ---
            _mockRepo.Verify(r => r.DeleteAsync(targetId), Times.Once);
            Assert.IsInstanceOfType(actionResult, typeof(OkResult));
        }

        #endregion
    }
}