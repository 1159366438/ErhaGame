using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ErhaGame.Services;

/// <summary>
/// 从 EXE 文件提取游戏名称与图标。
/// </summary>
public static class GameMetadataService
{
    /// <summary>优先取 EXE 的产品名，失败则回退到文件名（不含扩展名）。</summary>
    public static string GetDisplayName(string exePath)
    {
        try
        {
            var info = FileVersionInfo.GetVersionInfo(exePath);
            if (!string.IsNullOrWhiteSpace(info.ProductName))
            {
                return info.ProductName.Trim();
            }
        }
        catch
        {
            // 忽略版本信息读取失败，回退到文件名
        }

        return Path.GetFileNameWithoutExtension(exePath);
    }

    /// <summary>提取 EXE 关联图标（32x32），返回可绑定到 Image 的对象。</summary>
    public static ImageSource? GetIcon(string exePath)
    {
        IntPtr hIcon = IntPtr.Zero;
        try
        {
            var shfi = new Shell32.SHFILEINFO();
            IntPtr result = Shell32.SHGetFileInfo(
                exePath,
                0,
                ref shfi,
                (uint)Marshal.SizeOf<Shell32.SHFILEINFO>(),
                Shell32.SHGFI_ICON | Shell32.SHGFI_LARGEICON);

            if (result == IntPtr.Zero)
            {
                return null;
            }

            hIcon = shfi.hIcon;
            var image = Imaging.CreateBitmapSourceFromHIcon(
                hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            image.Freeze();
            return image;
        }
        finally
        {
            if (hIcon != IntPtr.Zero)
            {
                Shell32.DestroyIcon(hIcon);
            }
        }
    }

    private static class Shell32
    {
        public const uint SHGFI_ICON = 0x000000100;
        public const uint SHGFI_LARGEICON = 0x000000000;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbSizeFileInfo,
            uint uFlags);

        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }
    }
}