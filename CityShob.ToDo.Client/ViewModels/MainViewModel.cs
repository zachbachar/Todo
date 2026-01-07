using CityShob.ToDo.Client.Commands;
using CityShob.ToDo.Client.Services;
using CityShob.ToDo.Contract.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CityShob.ToDo.Client.ViewModels
{
    /// <summary>
    /// The primary ViewModel for the application, managing the list of Todo items,
    /// real-time synchronization, and user interactions.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly ITodoService _todoService;
        private readonly IDispatcherService _dispatcher;
        private readonly IDataCacheService _cacheService;
        private readonly ILogger<MainViewModel> _logger;

        // Flags to prevent infinite loops during synchronization
        private bool _isUpdatingFromRemote = false;
        private bool _isConnected;
        #endregion

        #region Constructor

        public MainViewModel(ITodoService todoService, IDataCacheService cacheService, IDispatcherService dispatcher, ILogger<MainViewModel> logger)
        {
            _todoService = todoService ?? throw new ArgumentNullException(nameof(todoService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            NewTaskInput = new NewTaskViewModel();
            NewTaskInput.RequestCreateTask += async (s, args) => await AddTaskAsync(args);

            Tasks = new ObservableCollection<TodoItemViewModel>();

            DeleteCommand = new RelayCommand(async (id) => await DeleteTaskAsync((int)id));
            LockItemCommand = new RelayCommand<TodoItemViewModel>(async (item) => await LockItem(item));
            UnlockItemCommand = new RelayCommand<TodoItemViewModel>(async (item) => await UnlockItem(item));
            ToggleCompleteCommand = new RelayCommand(async (item) => await ToggleTaskAsync((TodoItemViewModel)item));
            StressTestCommand = new RelayCommand(_ => RunStressTest());

            _todoService.ItemCreated += OnItemCreated;
            _todoService.ItemUpdated += OnItemUpdated;
            _todoService.ItemDeleted += OnItemDeleted;
            _todoService.TaskLocked += OnTaskLocked;
            _todoService.TaskUnlocked += OnTaskUnlocked;
            _todoService.ConnectionStateChanged += OnConnectionStateChanged;
        }

        #endregion

        #region Properties

        public ObservableCollection<TodoItemViewModel> Tasks { get; set; }

        public string CurrentDateText { get; } = DateTime.Today.ToLongDateString();

        public string MyConnectionId => _todoService.MyConnectionId;

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsNotConnected));
                }
            }
        }

        public bool IsNotConnected => !IsConnected;

        public NewTaskViewModel NewTaskInput { get; }

        #endregion

        #region Commands

        public ICommand DeleteCommand { get; }
        public ICommand ToggleCompleteCommand { get; }
        public ICommand LockItemCommand { get; private set; }
        public ICommand UnlockItemCommand { get; private set; }
        public ICommand StressTestCommand { get; }

        #endregion

        #region Initialization

        /// <summary>
        /// Loads data from the local cache and attempts to connect to the server.
        /// </summary>
        public async Task LoadDataAsync()
        {
            // 1. Offline First: Load cache immediately for UI responsiveness
            try
            {
                var cachedTasks = await _cacheService.LoadAsync();
                if (cachedTasks != null)
                {
                    UpdateTaskList(cachedTasks);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load local cache.");
            }

            // 2. Online Sync: Connect to server and refresh data
            try
            {
                await _todoService.ConnectAsync();
                OnPropertyChanged(nameof(MyConnectionId));

                var freshTasks = await _todoService.GetAllAsync();
                UpdateTaskList(freshTasks);

                IsConnected = true;
                await UpdateLocalCache();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Offline Mode: Connection failed.");
                // Intentionally swallow exception to allow read-only/cached usage.
            }
        }

        #endregion

        #region Command Implementations

        private async Task AddTaskAsync(NewTaskEventArgs args)
        {
            // Parse comma-separated tags
            var tagList = new List<TagDto>();
            if (!string.IsNullOrWhiteSpace(args.Tags))
            {
                var names = args.Tags.Split(',');
                foreach (var name in names)
                {
                    var cleanName = name.Trim();
                    if (!string.IsNullOrEmpty(cleanName))
                        tagList.Add(new TagDto { Name = cleanName });
                }
            }

            var newItemDto = new TodoItemDto
            {
                Title = args.Title.Trim(),
                Priority = args.Priority,
                DueDate = args.DueDate,
                IsCompleted = false,
                Tags = tagList,
                CreatedAt = DateTime.UtcNow
            };

            // Optimistic Update: Add to UI immediately
            var newItemVM = new TodoItemViewModel(newItemDto, _todoService, _logger);
            Tasks.Add(newItemVM);

            try
            {
                await _todoService.AddAsync(newItemDto);
                await UpdateLocalCache();
            }
            catch (Exception ex)
            {
                // Revert UI if server request fails
                Tasks.Remove(newItemVM);
                _logger.LogError(ex, "Failed to create new task.");
            }
        }

        private async Task DeleteTaskAsync(int id)
        {
            var itemToRemove = Tasks.FirstOrDefault(t => t.Id == id);
            if (itemToRemove == null) return;
            
            // Optimistic Update: Remove from UI immediately
            Tasks.Remove(itemToRemove);

            try
            {
                await _todoService.DeleteAsync(id);
                await UpdateLocalCache();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete task {TaskId}", id);
            }
        }

        private async Task ToggleTaskAsync(TodoItemViewModel item)
        {
            if (item == null) return;

            item.IsCompleted = !item.IsCompleted;
        }

        private async Task LockItem(TodoItemViewModel item)
        {
            if (item == null || item.IsLockedByOther) return;

            // Optimistic Lock: Visually lock immediately
            item.LockedByConnectionId = MyConnectionId;

            try
            {
                await _todoService.LockTaskAsync(item.Id);
            }
            catch (Exception ex)
            {
                // Revert lock state on failure
                item.LockedByConnectionId = null;
                _logger.LogError(ex, "Failed to lock task {TaskId}", item.Id);
            }
        }

        private async Task UnlockItem(TodoItemViewModel item)
        {
            if (item == null) return;
            if (!IsConnected) return;

            try
            {
                await _todoService.UnlockTaskAsync(item.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unlock task {TaskId}", item.Id);
            }
        }

        private void RunStressTest()
        {
            if (!IsConnected)
            {
                MessageBox.Show("Cannot run stress test: Offline.");
                return;
            }

            Task.Run(async () =>
            {
                _logger.LogInformation("--- STARTING STRESS TEST (50 Items) ---");

                // 1. Performance Metrics
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                int successCount = 0;
                int failCount = 0;

                int loadCount = 50;
                var random = new Random();
                var tasks = new List<Task>();

                for (int i = 0; i < loadCount; i++)
                {
                    int index = i;
                    tasks.Add(Task.Run(async () =>
                    {
                        var title = $"Stress Test {index}-{Guid.NewGuid().ToString().Substring(0, 4)}";
                        var newItem = new TodoItemDto
                        {
                            Title = title,
                            IsCompleted = false,
                            Priority = (TodoPriority)random.Next(0, 3),
                            CreatedAt = DateTime.UtcNow,
                            Tags = new List<TagDto> { new TagDto { Name = "LoadTest" } }
                        };

                        try
                        {
                            // A. CREATE (Now captures ID thanks to your fix)
                            await _todoService.AddAsync(newItem);

                            // B. UPDATE
                            newItem.Title += " [CHECKED]";
                            newItem.IsCompleted = true;
                            await _todoService.UpdateAsync(newItem);

                            // C. DELETE
                            await _todoService.DeleteAsync(newItem.Id);

                            // Thread-safe increment
                            System.Threading.Interlocked.Increment(ref successCount);
                        }
                        catch (Exception ex)
                        {
                            System.Threading.Interlocked.Increment(ref failCount);
                            _logger.LogError(ex, "❌ Item {Index} failed", index);
                        }
                    }));
                }

                await Task.WhenAll(tasks);
                stopwatch.Stop();

                // 2. Summary Log (The missing piece)
                double seconds = stopwatch.Elapsed.TotalSeconds;
                _logger.LogInformation(
                    "--- STRESS TEST COMPLETE ---\n" +
                    "Total Time: {Seconds:F2}s\n" +
                    "Success: {Success}/{Total}\n" +
                    "Throughput: {TPS:F2} ops/sec",
                    seconds, successCount, loadCount, (loadCount * 3) / seconds);
                // *3 because we do Add+Update+Delete per item
            });
        }

        #endregion

        #region SignalR Event Handlers

        private void OnItemCreated(TodoItemDto dto)
        {
            _dispatcher.Invoke(() =>
            {
                if (Tasks.Any(t => t.Id == dto.Id)) return;

                // Reconcile optimistic item:
                // If we added an item optimistically (ID=0) that matches the new item, replace it.
                var optimisticItem = Tasks.FirstOrDefault(t => t.Id == 0 && t.Title == dto.Title);

                if (optimisticItem != null)
                {
                    int index = Tasks.IndexOf(optimisticItem);
                    Tasks.RemoveAt(index);

                    var realVM = new TodoItemViewModel(dto, _todoService, _logger);
                    realVM.CurrentUserConnectionId = MyConnectionId;

                    Tasks.Insert(index, realVM);
                }
                else
                {
                    var vm = new TodoItemViewModel(dto, _todoService, _logger);
                    vm.CurrentUserConnectionId = MyConnectionId;
                    Tasks.Add(vm);
                }
            });
        }

        private void OnItemUpdated(TodoItemDto updatedDto)
        {
            _dispatcher.Invoke(() =>
            {
                var existing = Tasks.FirstOrDefault(t => t.Id == updatedDto.Id);
                if (existing != null)
                {
                    // Set flag to prevent the OnPropertyChanged handler from triggering an infinite loop
                    _isUpdatingFromRemote = true;
                    try
                    {
                        existing.UpdateFromDto(updatedDto);
                    }
                    finally
                    {
                        _isUpdatingFromRemote = false;
                    }
                }
            });
        }

        private void OnItemDeleted(int id)
        {
            _dispatcher.Invoke(() =>
            {
                var existing = Tasks.FirstOrDefault(t => t.Id == id);
                if (existing != null)
                {
                    Tasks.Remove(existing);
                }
            });
        }

        private void OnTaskLocked(int id, string lockedBy)
        {
            try
            {
                if (Application.Current == null || _dispatcher.HasShutdownStarted)
                    return;

                _dispatcher.Invoke(() =>
                {
                    var task = Tasks.FirstOrDefault(t => t.Id == id);
                    if (task != null)
                    {
                        task.LockedByConnectionId = lockedBy;
                    }
                });
            }
            catch (TaskCanceledException)
            {
                // App is shutting down, ignore.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing TaskLocked event for ID {TaskId}", id);
            }
        }

        private void OnTaskUnlocked(int id)
        {
            try
            {
                _dispatcher.Invoke(() =>
                {
                    var task = Tasks.FirstOrDefault(t => t.Id == id);
                    if (task != null)
                    {
                        task.LockedByConnectionId = null;
                    }
                });
            }
            catch (TaskCanceledException)
            {
                // App is shutting down, ignore.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing TaskUnlocked event for ID {TaskId}", id);
            }
        }

        private void OnConnectionStateChanged(bool isConnected)
        {
            _dispatcher.Invoke(() =>
            {
                IsConnected = isConnected;
            });
        }

        #endregion

        #region Private Helpers

        private void UpdateTaskList(List<TodoItemDto> dtos)
        {
            Tasks.Clear();
            foreach (var dto in dtos)
            {
                var vm = new TodoItemViewModel(dto, _todoService, _logger);
                vm.CurrentUserConnectionId = MyConnectionId;
                Tasks.Add(vm);
            }
        }

        private async Task UpdateLocalCache()
        {
            try
            {
                var currentDtos = Tasks.Select(t => t.ToDto()).ToList();
                await _cacheService.SaveAsync(currentDtos);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update local cache.");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }
}