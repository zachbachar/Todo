using CityShob.ToDo.Client.Services;
using CityShob.ToDo.Client.ViewModels;
using CityShob.ToDo.Contract.DTOs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace CityShob.ToDo.Client.Tests
{
    /// <summary>
    /// Unit tests for the MainViewModel class.
    /// Verifies initialization, command execution, SignalR event handling, and auto-save logic.
    /// </summary>
    public class MainViewModelTests
    {
        #region Fields

        private readonly Mock<ITodoService> _mockService;
        private readonly Mock<IDispatcherService> _mockDispatcher;
        private readonly Mock<IDataCacheService> _mockCache;
        private readonly Mock<ILogger<MainViewModel>> _mockLogger;
        private readonly Mock<ILogger> _mockItemLogger; // For TodoItemViewModels
        private readonly MainViewModel _viewModel;

        #endregion

        #region Constructor / Setup

        public MainViewModelTests()
        {
            // 1. Setup Dependencies
            _mockService = new Mock<ITodoService>();
            _mockDispatcher = new Mock<IDispatcherService>();
            _mockCache = new Mock<IDataCacheService>();
            _mockLogger = new Mock<ILogger<MainViewModel>>();
            _mockItemLogger = new Mock<ILogger>();

            // 2. Default Behavior Configuration
            _mockService.Setup(s => s.GetAllAsync(null)).ReturnsAsync(new List<TodoItemDto>());
            _mockService.Setup(s => s.MyConnectionId).Returns("TestConnectionId");

            // Mock Dispatcher to run actions synchronously for predictable testing
            _mockDispatcher.Setup(d => d.HasShutdownStarted).Returns(false);
            _mockDispatcher.Setup(d => d.Invoke(It.IsAny<Action>())).Callback<Action>(a => a());

            _mockCache.Setup(c => c.LoadAsync()).ReturnsAsync(new List<TodoItemDto>());

            // 3. Instantiate SUT (System Under Test)
            _viewModel = new MainViewModel(
                _mockService.Object,
                _mockCache.Object,
                _mockDispatcher.Object,
                _mockLogger.Object);
        }

        #endregion

        #region Initialization Tests

        [Fact]
        public async Task LoadDataAsync_Should_SetConnected_When_ServiceSucceeds()
        {
            // Arrange
            var testData = new List<TodoItemDto>
            {
                new TodoItemDto { Id = 1, Title = "Server Task" }
            };

            _mockService.Setup(s => s.ConnectAsync()).Returns(Task.CompletedTask);
            _mockService.Setup(s => s.GetAllAsync(null)).ReturnsAsync(testData);

            // Act
            await _viewModel.LoadDataAsync();

            // Assert
            _viewModel.IsConnected.Should().BeTrue();
            _viewModel.Tasks.Should().ContainSingle(t => t.Title == "Server Task");
            _viewModel.MyConnectionId.Should().Be("TestConnectionId");
        }

        [Fact]
        public async Task LoadDataAsync_Should_HandleConnectionFailure_Gracefully()
        {
            // Arrange
            _mockService.Setup(s => s.ConnectAsync()).ThrowsAsync(new Exception("Network Error"));

            // Act
            await _viewModel.LoadDataAsync();

            // Assert
            _viewModel.IsConnected.Should().BeFalse();

            // Verify structural logging for the offline mode warning
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Offline Mode")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region Command Execution Tests

        [Fact]
        public void AddTask_Should_PerformOptimisticUpdate_And_CallService()
        {
            // Arrange
            var newTaskArgs = new NewTaskEventArgs { Title = "New Task", Priority = TodoPriority.High };

            // Act
            _viewModel.NewTaskInput.TriggerRequestCreateTask(newTaskArgs);

            // Assert
            // Verify UI updated immediately (Optimistic)
            _viewModel.Tasks.Should().Contain(t => t.Title == "New Task");

            // Verify Backend service call
            _mockService.Verify(s => s.AddAsync(It.Is<TodoItemDto>(d => d.Title == "New Task")), Times.Once);
        }

        [Fact]
        public void DeleteCommand_Should_CallService_WithCorrectId()
        {
            // Arrange
            var taskToDelete = new TodoItemDto { Id = 10, Title = "Delete Me" };
            _viewModel.Tasks.Add(new TodoItemViewModel(taskToDelete, _mockService.Object, _mockItemLogger.Object));

            // Act
            if (_viewModel.DeleteCommand.CanExecute(10))
            {
                _viewModel.DeleteCommand.Execute(10);
            }

            // Assert
            _mockService.Verify(s => s.DeleteAsync(10), Times.Once);
        }

        #endregion

        #region Locking Logic Tests

        [Fact]
        public void LockItemCommand_Should_SetLocalConnectionId_And_CallService()
        {
            // Arrange
            var item = new TodoItemViewModel(new TodoItemDto { Id = 5 }, _mockService.Object, _mockItemLogger.Object);
            _viewModel.Tasks.Add(item);

            // Act
            _viewModel.LockItemCommand.Execute(item);

            // Assert
            _mockService.Verify(s => s.LockTaskAsync(5), Times.Once);
            item.LockedByConnectionId.Should().Be("TestConnectionId");
        }

        [Fact]
        public void UnlockItemCommand_Should_InvokeService_WhenConnected()
        {
            // Arrange
            _viewModel.IsConnected = true;
            var item = new TodoItemViewModel(new TodoItemDto { Id = 5 }, _mockService.Object, _mockItemLogger.Object);

            // Act
            _viewModel.UnlockItemCommand.Execute(item);

            // Assert
            _mockService.Verify(s => s.UnlockTaskAsync(5), Times.Once);
        }

        #endregion

        #region Auto-Save / Watcher Tests

        [Fact]
        public void Changing_WatchedProperty_Should_Trigger_ServiceUpdate()
        {
            // Arrange
            var itemVM = new TodoItemViewModel(new TodoItemDto { Id = 99, Title = "Original" }, _mockService.Object, _mockItemLogger.Object);
            _viewModel.Tasks.Add(itemVM); // Adding wires up the PropertyChanged handler

            // Act
            itemVM.Title = "Updated Title";

            // Assert
            _mockService.Verify(s => s.UpdateAsync(It.Is<TodoItemDto>(d => d.Id == 99 && d.Title == "Updated Title")), Times.AtLeastOnce);
        }

        [Fact]
        public void Changing_NonWatchedProperty_Should_Not_Trigger_ServiceUpdate()
        {
            // Arrange
            var itemVM = new TodoItemViewModel(new TodoItemDto { Id = 99 }, _mockService.Object, _mockItemLogger.Object);
            _viewModel.Tasks.Add(itemVM);

            // Act
            // CurrentUserConnectionId is used for UI logic, not persisted in the DB WatchList
            itemVM.CurrentUserConnectionId = "Test_ID";

            // Assert
            _mockService.Verify(s => s.UpdateAsync(It.IsAny<TodoItemDto>()), Times.Never);
        }

        #endregion
    }

    #region Test Helpers

    public static class TestExtensions
    {
        /// <summary>
        /// Simulates a user submitting the New Task form by triggering the private event delegate.
        /// </summary>
        public static void TriggerRequestCreateTask(this NewTaskViewModel vm, NewTaskEventArgs args)
        {
            var eventField = typeof(NewTaskViewModel).GetField("RequestCreateTask", BindingFlags.Instance | BindingFlags.NonPublic);
            var eventDelegate = (EventHandler<NewTaskEventArgs>)eventField?.GetValue(vm);
            eventDelegate?.Invoke(vm, args);
        }
    }

    #endregion
}