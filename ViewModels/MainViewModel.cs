using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ErhaGame.Models;
using ErhaGame.Services;

namespace ErhaGame.ViewModels;

public class MainViewModel
{
    private readonly DispatcherTimer _timer;
    private readonly Dictionary<Game, RunningSession> _runningSessions = new();

    /// <summary>已添加的游戏列表，绑定到界面。 </summary>
    public ObservableCollection<Game> Games { get; } = new();

    public ICommand AddGameCommand { get; }

    public ICommand ChangeCoverCommand { get; }

    public ICommand LaunchGameCommand { get; }

    public ICommand DeleteGameCommand { get; }

    public MainViewModel()
    {
        AddGameCommand = new RelayCommand(AddGameViaDialog);
        ChangeCoverCommand = new RelayCommand<Game>(ChangeCover);
        LaunchGameCommand = new RelayCommand<Game>(LaunchGame);
        DeleteGameCommand = new RelayCommand<Game>(DeleteGame);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTimerTick;

        LoadGames();
    }

    /// <summary>根据 EXE 路径新增游戏（拖拽入口）。</summary>
    public void AddGameFromPath(string exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath))
        {
            return;
        }

        if (!exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (Games.Any(g => string.Equals(g.ExePath, exePath, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        Games.Add(new Game
        {
            Name = GameMetadataService.GetDisplayName(exePath),
            ExePath = exePath,
            Cover = GameMetadataService.GetIcon(exePath)
        });

        Save();
    }

    private void AddGameViaDialog()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择游戏 EXE",
            Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            AddGameFromPath(dialog.FileName);
        }
    }

    private void ChangeCover(Game? game)
    {
        if (game is null)
        {
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择封面图片",
            Filter = "图片文件 (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|所有文件 (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        string coverPath;
        try
        {
            coverPath = DataStore.SaveCover(dialog.FileName);
        }
        catch
        {
            // 复制失败时退而直接引用原图片路径
            coverPath = dialog.FileName;
        }

        game.CoverPath = coverPath;
        game.Cover = LoadImage(coverPath);
        Save();
    }

    private void LaunchGame(Game? game)
    {
        if (game is null || _runningSessions.ContainsKey(game))
        {
            return;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = game.ExePath,
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(game.ExePath) ?? string.Empty
            };

            var process = new Process { StartInfo = startInfo };
            if (!process.Start())
            {
                return;
            }

            _runningSessions[game] = new RunningSession(process, Stopwatch.StartNew());
            game.IsRunning = true;
            game.LastPlayedAt = DateTime.Now;
            _timer.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"启动失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteGame(Game? game)
    {
        if (game is null)
        {
            return;
        }

        if (_runningSessions.ContainsKey(game))
        {
            MessageBox.Show("请先关闭正在运行的游戏再删除。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"确定删除「{game.Name}」吗？",
            "删除游戏",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        Games.Remove(game);
        DataStore.DeleteCoverIfOwned(game.CoverPath);
        Save();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_runningSessions.Count == 0)
        {
            _timer.Stop();
            return;
        }

        List<Game>? finished = null;
        foreach (var (game, session) in _runningSessions)
        {
            if (!HasProcessExited(session.Process))
            {
                continue;
            }

            CommitSession(game, session);
            (finished ??= new List<Game>()).Add(game);
        }

        if (finished is null)
        {
            return;
        }

        foreach (var game in finished)
        {
            _runningSessions.Remove(game);
        }

        Save();
    }

    /// <summary>应用关闭前调用：提交仍在运行的会话时长并保存。</summary>
    public void Shutdown()
    {
        foreach (var (game, session) in _runningSessions)
        {
            CommitSession(game, session);
        }

        _runningSessions.Clear();
        Save();
    }

    /// <summary>结算一次运行会话：累加时长、置未运行、释放进程句柄。</summary>
    private void CommitSession(Game game, RunningSession session)
    {
        session.Stopwatch.Stop();
        game.TotalPlaySeconds += session.Stopwatch.Elapsed.TotalSeconds;
        game.IsRunning = false;
        try
        {
            session.Process.Dispose();
        }
        catch
        {
            // 忽略 dispose 异常
        }
    }

    private void LoadGames()
    {
        foreach (var game in DataStore.LoadGames())
        {
            game.Cover = ResolveCover(game);
            Games.Add(game);
        }
    }

    private static ImageSource? ResolveCover(Game game)
    {
        if (!string.IsNullOrEmpty(game.CoverPath) && File.Exists(game.CoverPath))
        {
            return LoadImage(game.CoverPath);
        }

        return GameMetadataService.GetIcon(game.ExePath);
    }

    private void Save() => DataStore.SaveGames(Games);

    private static bool HasProcessExited(Process process)
    {
        try
        {
            return process.HasExited;
        }
        catch
        {
            // 进程句柄失效时视为已退出
            return true;
        }
    }

    private static ImageSource? LoadImage(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(path);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private sealed record RunningSession(Process Process, Stopwatch Stopwatch);
}