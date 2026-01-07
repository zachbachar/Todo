using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CityShob.ToDo.Client.Converters
{
    /// <summary>
    /// A MultiValueConverter that determines if an item is locked by a user other than the current one.
    /// Expects two values:
    /// 1. The ConnectionID of the user who locked the item (string).
    /// 2. The ConnectionID of the current user (string).
    /// </summary>
    public class IsLockedByOtherConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Safety: Ensure array is valid
            if (values == null || values.Length < 2)
                return false;

            // Safety: Handle WPF initialization state where bindings might be unset
            if (values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
                return false;

            // Value[0]: The ConnectionID who locked the item
            // Value[1]: My ConnectionID
            string lockedBy = values[0] as string;
            string myId = values[1] as string;

            // If nobody locked it, it's not locked by other
            if (string.IsNullOrEmpty(lockedBy))
                return false;

            // If I locked it, it's not locked by "other" (I can still edit)
            // Case-insensitive comparison is safer for ID strings, though SignalR IDs are usually case-sensitive.
            if (string.Equals(lockedBy, myId, StringComparison.OrdinalIgnoreCase))
                return false;

            // Otherwise, it is locked by someone else
            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}