using System.Globalization;

namespace LifeManagementApp.Converters;

/// <summary>
/// Gives the completion action a state-aware label for both sighted users and
/// screen readers. A completed item offers Undo; an open item offers Done.
/// </summary>
public sealed class TaskCompletionTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "Undo" : "Done";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("Task completion text is one-way UI state.");
}
