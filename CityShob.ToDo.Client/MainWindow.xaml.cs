using CityShob.ToDo.Client.ViewModels;
using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CityShob.ToDo.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// Handles main window lifecycle, window placement persistence, and global focus management.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// Dependencies are injected by the DI container.
        /// </summary>
        /// <param name="viewModel">The main view model.</param>
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            // Auto-scroll logic for the task list
            if (viewModel.Tasks != null)
            {
                viewModel.Tasks.CollectionChanged += (s, e) =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Add)
                    {
                        // Dispatch to UI thread to ensure the visual tree is updated before scrolling
                        this.Dispatcher.InvokeAsync(() =>
                        {
                            TasksScrollViewer?.ScrollToBottom();
                        });
                    }
                };
            }
        }

        #endregion

        #region Global Focus Management

        /// <summary>
        /// Handles mouse clicks at the window level to detect "clicking outside" a focused row.
        /// </summary>
        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var focusedElement = Keyboard.FocusedElement as DependencyObject;
            var clickedElement = e.OriginalSource as DependencyObject;

            if (focusedElement != null && clickedElement != null)
            {
                // If the user clicked inside the currently focused element (or its children), do nothing.
                // The specific row control handles interactions within itself.
                if (IsDescendantOrSelf(focusedElement, clickedElement)) return;

                // If the user clicked outside the active row, clear focus.
                // This triggers the 'IsKeyboardFocusWithinChanged' event on the row, 
                // which executes the Unlock logic.
                Keyboard.ClearFocus();
            }
        }

        /// <summary>
        /// Helper method to determine if the clicked element is part of the focused visual tree.
        /// </summary>
        private bool IsDescendantOrSelf(DependencyObject focused, DependencyObject clicked)
        {
            if (focused == clicked) return true;

            var parent = clicked;
            while (parent != null)
            {
                if (parent == focused) return true;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return false;
        }

        #endregion

        #region Window Persistence (Settings)

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            try
            {
                // Restore window position and size
                this.Top = Properties.Settings.Default.WindowTop;
                this.Left = Properties.Settings.Default.WindowLeft;
                this.Height = Properties.Settings.Default.WindowHeight;
                this.Width = Properties.Settings.Default.WindowWidth;

                // Ensure the window is visible on screen (handling multi-monitor edge cases)
                if (this.Left < SystemParameters.VirtualScreenLeft) this.Left = 0;
                if (this.Top < SystemParameters.VirtualScreenTop) this.Top = 0;

                if (Properties.Settings.Default.WindowState != WindowState.Minimized)
                {
                    this.WindowState = Properties.Settings.Default.WindowState;
                }
            }
            catch
            {
                // Ignore persistence errors to ensure the window still opens
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Save current position only if normalized (not minimized/maximized)
                if (this.WindowState == WindowState.Normal)
                {
                    Properties.Settings.Default.WindowTop = this.Top;
                    Properties.Settings.Default.WindowLeft = this.Left;
                    Properties.Settings.Default.WindowHeight = this.Height;
                    Properties.Settings.Default.WindowWidth = this.Width;
                }

                Properties.Settings.Default.WindowState = this.WindowState;
                Properties.Settings.Default.Save();
            }
            catch
            {
                // Ignore persistence errors on close
            }

            base.OnClosing(e);
        }

        #endregion
    }
}