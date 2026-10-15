using System.Globalization;
using RailwayErp.MvvmDemo.Models;

namespace RailwayErp.MvvmDemo.Converters;

/// <summary>Колір «пігулки» статусу. Насичені середні тони читаються і на світлій, і на темній темі.</summary>
public sealed class TrainStatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        TrainStatus.OnTime => Color.FromArgb("#2E9E4F"),
        TrainStatus.Boarding => Color.FromArgb("#1F7AE0"),
        TrainStatus.Delayed => Color.FromArgb("#D98200"),
        TrainStatus.Departed => Color.FromArgb("#6B7686"),
        TrainStatus.Cancelled => Color.FromArgb("#D33A3A"),
        _ => Colors.Gray
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
