using System.Globalization;
using System.Windows.Data;

namespace ErhaGame.Converters;

/// <summary>把游玩秒数格式化为「x 小时 y 分」。</summary>
public class SecondsToPlayTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var seconds = value is double d ? d : 0;
        var total = (int)Math.Floor(seconds);
        if (total <= 0)
        {
            return "未游玩";
        }

        var hours = total / 3600;
        var minutes = (total % 3600) / 60;

        if (hours > 0)
        {
            return $"{hours} 小时 {minutes} 分";
        }

        if (minutes > 0)
        {
            return $"{minutes} 分钟";
        }

        return $"{total} 秒";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}