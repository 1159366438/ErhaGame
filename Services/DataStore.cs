using System.IO;
using System.Text.Json;
using ErhaGame.Models;

namespace ErhaGame.Services;

/// <summary>
/// 负责游戏库的本地持久化：JSON 存取 + 封面文件保存。
/// </summary>
public static class DataStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>数据根目录：%AppData%\ErhaGame。</summary>
    public static string DataDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ErhaGame");

    /// <summary>封面图片存放目录。</summary>
    public static string CoversDirectory { get; } = Path.Combine(DataDirectory, "Covers");

    private static string GamesFilePath => Path.Combine(DataDirectory, "games.json");

    private static void EnsureDirectories() => Directory.CreateDirectory(CoversDirectory);

    /// <summary>从磁盘加载游戏列表；文件不存在或损坏时返回空列表。</summary>
    public static List<Game> LoadGames()
    {
        try
        {
            if (!File.Exists(GamesFilePath))
            {
                return new List<Game>();
            }

            var json = File.ReadAllText(GamesFilePath);
            return JsonSerializer.Deserialize<List<Game>>(json, SerializerOptions) ?? new List<Game>();
        }
        catch
        {
            return new List<Game>();
        }
    }

    /// <summary>保存游戏列表到磁盘；失败时静默忽略。</summary>
    public static void SaveGames(IEnumerable<Game> games)
    {
        try
        {
            EnsureDirectories();
            var json = JsonSerializer.Serialize(games, SerializerOptions);
            File.WriteAllText(GamesFilePath, json);
        }
        catch
        {
            // 保存失败暂不打断用户体验
        }
    }

    /// <summary>删除封面目录内我们自己管理的封面文件（避免遗留孤儿文件）。</summary>
    public static void DeleteCoverIfOwned(string coverPath)
    {
        if (string.IsNullOrEmpty(coverPath))
        {
            return;
        }

        try
        {
            var full = Path.GetFullPath(coverPath);
            var coversRoot = Path.GetFullPath(CoversDirectory);
            if (full.StartsWith(coversRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(full))
            {
                File.Delete(full);
            }
        }
        catch
        {
            // 删除失败忽略
        }
    }

    /// <summary>把用户选择的封面复制到封面目录，返回持久化路径。</summary>
    public static string SaveCover(string sourceImagePath)
    {
        EnsureDirectories();

        var extension = Path.GetExtension(sourceImagePath);
        if (string.IsNullOrEmpty(extension))
        {
            extension = ".png";
        }

        var destination = Path.Combine(CoversDirectory, Guid.NewGuid().ToString("N") + extension);
        File.Copy(sourceImagePath, destination, overwrite: true);
        return destination;
    }
}