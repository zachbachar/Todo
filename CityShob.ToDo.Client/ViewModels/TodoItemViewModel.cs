using CityShob.ToDo.Contract.DTOs;
using CityShob.ToDo.Client.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CityShob.ToDo.Client.ViewModels
{
    /// <summary>
    /// A ViewModel wrapper for TodoItemDto.
    /// Manages UI-specific state, data transformation (Tags to CSV), and triggers updates via the injected TodoService.
    /// </summary>
    public class TodoItemViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly TodoItemDto _dto;
        private readonly ITodoService _todoService;
        private readonly ILogger _logger;

        private string _tagsText;
        private string _currentUserConnectionId;
        private string _lockedByConnectionId;

        private bool _isSilenced = false;
        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for design-time data or empty initialization.
        /// </summary>
        public TodoItemViewModel()
        {
            _dto = new TodoItemDto();
        }

        /// <summary>
        /// Initializes a new instance with data and service dependencies.
        /// </summary>
        /// <param name="dto">The Data Transfer Object containing the task data.</param>
        /// <param name="todoService">The service used for persistence.</param>
        /// <param name="logger">The logger for capturing runtime errors.</param>
        public TodoItemViewModel(TodoItemDto dto, ITodoService todoService, ILogger logger)
        {
            _dto = dto ?? new TodoItemDto();
            _todoService = todoService;
            _logger = logger;

            // Initialize the UI text representation of tags
            _tagsText = _dto.Tags != null
                ? string.Join(", ", _dto.Tags.Select(t => t.Name))
                : string.Empty;
        }

        #endregion

        #region DTO Wrapper Properties

        public int Id => _dto.Id;

        public string Title
        {
            get => _dto.Title;
            set
            {
                if (_dto.Title != value)
                {
                    _dto.Title = value;
                    OnPropertyChanged();
                    SaveAsync();
                }
            }
        }

        public bool IsCompleted
        {
            get => _dto.IsCompleted;
            set
            {
                if (_dto.IsCompleted != value)
                {
                    _dto.IsCompleted = value;
                    OnPropertyChanged();
                    SaveAsync();
                }
            }
        }

        public TodoPriority Priority
        {
            get => _dto.Priority;
            set
            {
                if (_dto.Priority != value)
                {
                    _dto.Priority = value;
                    OnPropertyChanged();
                    SaveAsync();
                }
            }
        }

        public DateTime? DueDate
        {
            get => _dto.DueDate;
            set
            {
                if (_dto.DueDate != value)
                {
                    _dto.DueDate = value;
                    OnPropertyChanged();
                    SaveAsync();
                }
            }
        }

        #endregion

        #region Tag Management

        /// <summary>
        /// Gets or sets the comma-separated string representation of tags.
        /// Updates the underlying DTO tag list and triggers a save when set.
        /// </summary>
        public string TagsText
        {
            get => _tagsText;
            set
            {
                if (_tagsText != value)
                {
                    // Store exactly what the user typed to preserve spaces/format while editing
                    _tagsText = value;

                    // Parse and update the DTO silently
                    ParseAndApplyTags(_tagsText);

                    OnPropertyChanged();
                }
            }
        }

        public List<TagDto> Tags => _dto.Tags;

        /// <summary>
        /// Parses a comma-separated string and updates the DTO's Tags collection.
        /// </summary>
        private void ParseAndApplyTags(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _dto.Tags = new List<TagDto>();
            }
            else
            {
                _dto.Tags = text.Split(',')
                                .Select(t => t.Trim())
                                .Where(t => !string.IsNullOrEmpty(t))
                                .Select(name => new TagDto { Name = name })
                                .ToList();
            }

            // Notify that the "Tags" collection has changed
            OnPropertyChanged(nameof(Tags));
            SaveAsync();
        }

        #endregion

        #region Locking Logic

        /// <summary>
        /// Gets or sets the Connection ID of the current user. 
        /// Used to determine if the item is locked by "Self" or "Other".
        /// </summary>
        public string CurrentUserConnectionId
        {
            get => _currentUserConnectionId;
            set
            {
                if (_currentUserConnectionId != value)
                {
                    _currentUserConnectionId = value;
                    OnPropertyChanged();
                    // Recalculate lock status when my identity changes
                    OnPropertyChanged(nameof(IsLockedByOther));
                }
            }
        }

        /// <summary>
        /// Gets or sets the Connection ID of the user currently holding the lock.
        /// </summary>
        public string LockedByConnectionId
        {
            get => _lockedByConnectionId;
            set
            {
                if (_lockedByConnectionId != value)
                {
                    _lockedByConnectionId = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsLocked));
                    // Recalculate lock status when the locker changes
                    OnPropertyChanged(nameof(IsLockedByOther));
                }
            }
        }

        /// <summary>
        /// Determines if the item is locked by a different user.
        /// Returns true only if a lock exists AND it is not held by the current user.
        /// </summary>
        public bool IsLockedByOther
        {
            get
            {
                if (string.IsNullOrEmpty(LockedByConnectionId)) return false;
                if (LockedByConnectionId == CurrentUserConnectionId) return false;
                return true;
            }
        }

        public bool IsLocked => !string.IsNullOrEmpty(LockedByConnectionId);

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the underlying DTO for serialization or service transmission.
        /// </summary>
        public TodoItemDto ToDto() => _dto;

        /// <summary>
        /// Updates the ViewModel state from a remote DTO (e.g., from a SignalR update).
        /// </summary>
        public void UpdateFromDto(TodoItemDto remoteDto)
        {
            if (remoteDto == null) return;

            // Silence persistence during remote update to avoid loops
            _isSilenced = true;

            // 1. Update simple properties
            Title = remoteDto.Title;
            IsCompleted = remoteDto.IsCompleted;
            Priority = remoteDto.Priority;
            DueDate = remoteDto.DueDate;

            // 2. Update Tags List
            _dto.Tags = remoteDto.Tags;

            // 3. Synchronize the UI text string to match the new list
            if (_dto.Tags != null && _dto.Tags.Any())
            {
                _tagsText = string.Join(", ", _dto.Tags.Select(t => t.Name));
            }
            else
            {
                _tagsText = string.Empty;
            }

            // 4. Notify UI
            OnPropertyChanged(nameof(Tags));
            OnPropertyChanged(nameof(TagsText));

            // End silencing
            _isSilenced = false;
        }

        #endregion

        #region Private Persistence

        /// <summary>
        /// triggers an asynchronous update to the backend.
        /// This is an async void method intended for property setters.
        /// If the ViewModel is in a silenced state, no action is taken.
        /// </summary>
        private async void SaveAsync()
        {
            if (_todoService == null || _isSilenced) return;

            try
            {
                await _todoService.UpdateAsync(this.ToDto());
            }
            catch (Exception ex)
            {
                // We swallow the exception to keep the UI responsive, but we must log it.
                _logger?.LogError(ex, "Failed to auto-save task {TaskId}", this.Id);
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