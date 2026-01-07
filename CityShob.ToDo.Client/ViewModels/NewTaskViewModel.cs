using CityShob.ToDo.Client.Commands;
using CityShob.ToDo.Contract.DTOs;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CityShob.ToDo.Client.ViewModels
{
    /// <summary>
    /// Event arguments containing the data for a new task request.
    /// </summary>
    public class NewTaskEventArgs : EventArgs
    {
        public string Title { get; set; }
        public DateTime? DueDate { get; set; }
        public TodoPriority Priority { get; set; }
        public string Tags { get; set; }
    }

    /// <summary>
    /// ViewModel responsible for capturing user input for a new task.
    /// Handles temporary persistence (drafts) and validation.
    /// </summary>
    public class NewTaskViewModel : INotifyPropertyChanged
    {
        #region Fields 
        private string _title;
        private DateTime? _dueDate;
        private TodoPriority _priority = TodoPriority.Medium;
        private string _tags;
        #endregion

        #region Events & Commands

        /// <summary>
        /// Occurs when the user requests to create the task (e.g., presses Enter or clicks Add).
        /// </summary>
        public event EventHandler<NewTaskEventArgs> RequestCreateTask;

        public ICommand SubmitCommand { get; }
        #endregion

        public NewTaskViewModel()
        {
            SubmitCommand = new RelayCommand((_) => ExecuteSubmit());

            // Attempt to restore drafts. 
            // We swallow exceptions here because corrupted settings should not crash the app startup.
            try
            {
                _title = Properties.Settings.Default.DraftTitle;
                _tags = Properties.Settings.Default.DraftTags;
            }
            catch
            {
                // Reset to defaults if settings are unreadable
                _title = string.Empty;
                _tags = string.Empty;
            }
        }


        #region Properties

        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged();
                    SaveDraftSafely();
                }
            }
        }

        public DateTime? DueDate
        {
            get => _dueDate;
            set
            {
                _dueDate = value;
                OnPropertyChanged();
            }
        }

        public TodoPriority Priority
        {
            get => _priority;
            set
            {
                _priority = value;
                OnPropertyChanged();
            }
        }

        public string Tags
        {
            get => _tags;
            set
            {
                if (_tags != value)
                {
                    _tags = value;
                    OnPropertyChanged();
                    SaveDraftSafely();
                }
            }
        }

        #endregion

        #region Private Methods

        private void ExecuteSubmit()
        {
            if (string.IsNullOrWhiteSpace(Title)) return;

            // Notify listeners (Parent ViewModel)
            RequestCreateTask?.Invoke(this, new NewTaskEventArgs
            {
                Title = this.Title,
                DueDate = this.DueDate,
                Priority = this.Priority,
                Tags = this.Tags
            });

            // Reset inputs for next entry
            ClearInput();
        }

        private void ClearInput()
        {
            Title = string.Empty;
            DueDate = null;
            Priority = TodoPriority.Medium;
            Tags = string.Empty;
        }

        /// <summary>
        /// Persists the current input to application settings to survive restarts.
        /// </summary>
        private void SaveDraftSafely()
        {
            try
            {
                Properties.Settings.Default.DraftTitle = _title;
                Properties.Settings.Default.DraftTags = _tags;
                Properties.Settings.Default.Save();
            }
            catch
            {
                // Ignore persistence errors for drafts to prevent UI lag or crashes.
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