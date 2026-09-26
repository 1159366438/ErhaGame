# 二哈游戏盒（ErhaGame）

Windows 本地游戏启动器，用 C# WPF（.NET 8）开发。

## 功能

- 拖拽 `.exe` 进窗口，或点击「+ 添加游戏」选择游戏
- 自动提取游戏名称与图标作为封面
- 右键「更换封面」可从本地选择图片
- 双击卡片（或右键）启动游戏，自动统计游玩时长
- 游戏列表与时长用 JSON 持久化，重启后自动恢复
- 右键「删除游戏」（自动清理对应封面文件）

## 技术栈

- C# / WPF / .NET 8
- MVVM 架构
- System.Text.Json 持久化

## 运行

需要安装 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)：

```bash
dotnet run
```

## 打包（自包含单文件）

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

输出：`bin/Release/net8.0-windows/win-x64/publish/二哈游戏盒.exe`

## 数据存储

游戏列表与封面保存在 `%AppData%\WindowsGameApp\`。