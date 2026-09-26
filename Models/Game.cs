using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace ErhaGame.Models;

/// <summary>
/// 单个游戏条目。
/// </summary>
public class Game : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private ImageSource? _cover;
    private double _totalPlaySeconds;
    private bool _isRunning;
    private DateTime? _lastPlayedAt;

    /// <summary>显示名称（取自 EXE 产品名或文件名）。</summary>
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>游戏 EXE 的完整路径。</summary>
    public string ExePath { get; set; } = string.Empty;

    /// <summary>封面图（运行时对象，不参与序列化）。</summary>
    [JsonIgnore]
    public ImageSource? Cover
    {
        get => _cover;
        set
        {
            if (!ReferenceEquals(_cover, value))
            {
                _cover = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>封面图持久化路径；为空表示使用 EXE 图标。</summary>
    public string CoverPath { get; set; } = string.Empty;

    /// <summary>累计游玩时长（秒）。</summary>
    public double TotalPlaySeconds
    {
        get => _totalPlaySeconds;
        set
        {
            if (Math.Abs(_totalPlaySeconds - value) > 0.001)
            {
                _totalPlaySeconds = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>是否正在运行（运行时状态，不参与序列化）。</summary>
    [JsonIgnore]
    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (_isRunning != value)
            {
                _isRunning = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>最后游玩时间。</summary>
    public DateTime? LastPlayedAt
    {
        get => _lastPlayedAt;
        set
        {
            if (_lastPlayedAt != value)
            {
                _lastPlayedAt = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}