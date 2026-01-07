using CityShob.ToDo.Client.ViewModels;
using CityShob.ToDo.Contract.DTOs;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace CityShob.ToDo.Client.Views
{
    /// <summary>
    /// Interaction logic for TodoItemView.xaml.
    /// Manages UI-specific behaviors like row locking based on focus, 
    /// keyboard shortcuts (Enter/Escape), and context menu interactions.
    /// </summary>
    public partial class TodoItemView : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty LockCommandProperty =
            DependencyProperty.Register("LockCommand", typeof(ICommand), typeof(TodoItemView), new PropertyMetadata(null));

        /// <summary>
        /// Command to execute when the row gains focus (locks the item).
        /// </summary>
        public ICommand LockCommand
        {
            get { return (ICommand)GetValue(LockCommandProperty); }
            set { SetValue(LockCommandProperty, value); }
        }

        public static readonly DependencyProperty UnlockCommandProperty =
            DependencyProperty.Register("UnlockCommand", typeof(ICommand), typeof(TodoItemView), new PropertyMetadata(null));

        /// <summary>
        /// Command to execute when the row loses focus (unlocks the item).
        /// </summary>
        public ICommand UnlockCommand
        {
            get { return (ICommand)GetValue(UnlockCommandProperty); }
            set { SetValue(UnlockCommandProperty, value); }
        }

        #endregion

        #region Constructor

        public TodoItemView()
        {
            InitializeComponent();

            // Hook into the "Focus Within" change event.
            // This handles the main Lock/Unlock logic for the whole row.
            this.IsKeyboardFocusWithinChanged += OnRowFocusChanged;
        }

        #endregion

        #region Focus Management (Locking Logic)

        private void OnRowFocusChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(this.DataContext is TodoItemViewModel itemVM)) return;

            bool isFocusedNow = (bool)e.NewValue;

            if (isFocusedNow)
            {
                // Row gained focus -> Lock the item
                if (LockCommand != null && LockCommand.CanExecute(itemVM))
                {
                    LockCommand.Execute(itemVM);
                }
            }
            else
            {
                // Row lost focus -> Unlock the item

                // CRITICAL: If the focus was lost because we opened the Context Menu (Priority),
                // we do NOT want to unlock yet. The user is still interacting with the item.
                if (PriorityBtn.ContextMenu != null && PriorityBtn.ContextMenu.IsOpen) return;

                if (UnlockCommand != null && UnlockCommand.CanExecute(itemVM))
                {
                    UnlockCommand.Execute(itemVM);
                }
            }
        }

        #endregion

        #region Input Handling

        private void OnInputPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                // Explicitly update the source before clearing focus to ensure the ViewModel has the latest text
                if (sender is TextBox tb)
                {
                    tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                }

                // Clear focus to trigger OnRowFocusChanged -> Unlocks the item
                Keyboard.ClearFocus();
                e.Handled = true;
            }
        }

        #endregion

        #region Priority Menu Logic

        private void PriorityBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!(this.DataContext is TodoItemViewModel itemVM)) return;

            // Ensure the item is locked before opening the menu
            if (LockCommand != null && LockCommand.CanExecute(itemVM))
            {
                LockCommand.Execute(itemVM);
            }

            // Manually open the context menu
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void OnPrioritySelected(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem &&
                this.DataContext is TodoItemViewModel vm &&
                menuItem.Tag != null)
            {
                // 1. Update the data
                if (Enum.TryParse(menuItem.Tag.ToString(), out TodoPriority selectedPriority))
                {
                    vm.Priority = selectedPriority;
                }

                // 2. Manually close the menu.
                // This is vital: We must close the menu BEFORE clearing focus, 
                // otherwise OnRowFocusChanged will see IsOpen=true and refuse to unlock.
                if (menuItem.Parent is ContextMenu parentMenu)
                {
                    parentMenu.IsOpen = false;
                }

                // 3. Clear focus to trigger Unlock/Save logic
                Keyboard.ClearFocus();
            }
        }

        #endregion

        #region Date Picker Logic

        private void OpenDatePicker_Click(object sender, RoutedEventArgs e)
        {
            if (HiddenDatePicker != null)
            {
                HiddenDatePicker.IsDropDownOpen = true;
            }
        }

        private void HiddenDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            // The binding updates automatically.
            // We intentionally do not clear focus here to allow the user to continue editing other fields if desired.
        }

        #endregion
    }
}