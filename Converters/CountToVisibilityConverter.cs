using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ErhaGame.Converters;

/// <summary>数量为 0 时显示（用于空状态提示），否则隐藏。</summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var count = value is int i ? i : 0;
        return count > 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}