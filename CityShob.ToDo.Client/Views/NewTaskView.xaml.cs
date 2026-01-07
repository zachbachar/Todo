using CityShob.ToDo.Client.ViewModels;
using CityShob.ToDo.Contract.DTOs;
using System;
using System.Windows;
using System.Windows.Controls;

namespace CityShob.ToDo.Client.Views
{
    /// <summary>
    /// Interaction logic for NewTaskView.xaml.
    /// Handles specific UI events like opening the date picker or context menus that are difficult to bind purely via MVVM.
    /// </summary>
    public partial class NewTaskView : UserControl
    {
        #region Constructor

        public NewTaskView()
        {
            InitializeComponent();
        }

        #endregion

        #region Event Handlers

        private void OpenDatePicker_Click(object sender, RoutedEventArgs e)
        {
            // Programmatically open the date picker dropdown when the custom button is clicked
            if (HiddenDatePicker != null)
            {
                HiddenDatePicker.IsDropDownOpen = true;
            }
        }

        private void PriorityButton_Click(object sender, RoutedEventArgs e)
        {
            // Programmatically open the context menu attached to the button
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.SetCurrentValue(ContextMenu.IsOpenProperty, true);
            }
        }

        private void SetPriority_Click(object sender, RoutedEventArgs e)
        {
            // Updates the ViewModel's Priority based on the Tag of the clicked MenuItem
            if (sender is MenuItem menuItem && DataContext is NewTaskViewModel vm)
            {
                if (menuItem.Tag != null && Enum.TryParse(menuItem.Tag.ToString(), out TodoPriority selectedPriority))
                {
                    vm.Priority = selectedPriority;
                }
            }
        }

        #endregion
    }
}