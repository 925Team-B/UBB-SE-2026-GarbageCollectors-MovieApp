#nullable enable
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MovieApp.UI;

/// <summary>
/// Converts a boolean to Visibility (true=Visible, false=Collapsed).
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>Converts bool to Visibility.</summary>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    /// <summary>Converts Visibility to bool.</summary>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility visibility && visibility == Visibility.Visible;
    }
}

/// <summary>
/// Converts a boolean to inverted Visibility (true=Collapsed, false=Visible).
/// </summary>
public class BoolToVisibilityInverterConverter : IValueConverter
{
    /// <summary>Converts bool to inverted Visibility.</summary>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Visible;
    }

    /// <summary>Converts inverted Visibility to bool.</summary>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility visibility && visibility == Visibility.Collapsed;
    }
}

/// <summary>
/// Converts an integer to Visibility (> 0 = Visible, 0 = Collapsed).
/// </summary>
public class IntToVisibilityConverter : IValueConverter
{
    /// <summary>Converts int to Visibility.</summary>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int intValue)
            return intValue > 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    /// <summary>Converts Visibility to int.</summary>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility visibility && visibility == Visibility.Visible ? 1 : 0;
    }
}

/// <summary>
/// Converts a nullable int to Visibility (non-null and > 0 = Visible).
/// </summary>
public class NullableIntToVisibilityConverter : IValueConverter
{
    /// <summary>Converts nullable int to Visibility.</summary>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int intValue)
            return intValue > 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    /// <summary>Converts Visibility to nullable int.</summary>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility visibility && visibility == Visibility.Visible ? 1 : 0;
    }
}

/// <summary>
/// Converts a boolean to an opacity value (true=1.0 fully visible, false=0.4 dimmed).
/// Used to dim locked badges while keeping them visible.
/// </summary>
public class BoolToOpacityConverter : IValueConverter
{
    /// <summary>Converts bool to opacity double.</summary>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool b && b ? 1.0 : 0.4;
    }

    /// <summary>Not supported.</summary>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a string to bool (non-empty = true).
/// </summary>
public class StringNotEmptyToBoolConverter : IValueConverter
{
    /// <summary>Converts string to bool.</summary>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string str)
            return !string.IsNullOrEmpty(str);
        return false;
    }

    /// <summary>Not supported.</summary>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

