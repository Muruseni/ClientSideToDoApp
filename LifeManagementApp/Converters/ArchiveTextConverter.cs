using System.Globalization;

namespace LifeManagementApp.Converters;

/// <summary>
/// Changes the task action label according to the current archive state.
/// The same button can therefore archive an active task and unarchive an archived task.
/// </summary>
public sealed class ArchiveTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "Unarchive" : "Archive";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("Archive text is one-way UI state.");
}
