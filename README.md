# BinLens

> Windows 离线 GTFOBins 查询工具（非官方）

BinLens 是一个面向 Windows 10/11 x64 的单文件桌面应用。它将 GTFOBins 公开条目内置到本地应用中，用于快速检索二进制程序的已知使用方式，并可在本机解析 `sudo -l` 输出。

![License](https://img.shields.io/badge/license-GPL--3.0--only-1f1f1f)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-1f1f1f)
![Runtime](https://img.shields.io/badge/runtime-.NET%208-1f1f1f)

## 功能

- 完全离线：数据随应用内置，日常检索不需要网络。
- 中英文界面与亮/暗主题切换。
- 搜索二进制名、官方别名与使用场景。
- 支持 Sudo、SUID、Capabilities、普通用户等上下文筛选。
- 点击任意命令代码区域即可复制完整命令；也保留复制按钮。
- 鼠标拖选文本后可使用 `Ctrl+C` 复制局部内容。
- `Ctrl+F` 聚焦搜索框。
- 批量分析 `sudo -l` 输出，区分精确匹配、需确认版本、未收录和禁止规则。
- 不执行命令、不上传粘贴内容、不收集账号、行为数据或遥测。

## 下载与运行

从 [Releases](../../releases) 下载 `BinLens-win-x64.exe`，双击即可运行。无需安装 .NET 运行时。

发布文件同时提供 SHA-256 校验文件。PowerShell 校验示例：

```powershell
Get-FileHash .\BinLens-win-x64.exe -Algorithm SHA256
```

## 本地构建

要求：.NET SDK 8。

```powershell
dotnet run --project .\GtfobinsOffline.SelfTest\GtfobinsOffline.SelfTest.csproj -c Release

dotnet publish .\GtfobinsOffline\GtfobinsOffline.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\publish
```

推送 `v*` 标签会触发 GitHub Actions：运行自测、构建单文件 EXE、生成 SHA-256，并创建 GitHub Release。

## 数据、许可证与安全边界

- 数据源：[GTFOBins](https://github.com/GTFOBins/GTFOBins.github.io)。
- 命令、二进制名、路径与参数保持上游原文；中文只用于界面和说明。
- 本项目为非官方客户端，不隶属于 GTFOBins。
- 本项目遵循 [GPL-3.0-only](LICENSE)；上游许可证与版权声明随源数据保留，详见 [NOTICE.md](NOTICE.md)。
- 请仅在拥有明确授权的环境中使用。BinLens 仅用于信息查询和本地文本解析，不会执行任何命令。

## 贡献

欢迎提交数据同步、翻译、可访问性和界面体验改进。请阅读 [CONTRIBUTING.md](CONTRIBUTING.md) 与 [SECURITY.md](SECURITY.md)。

## 版本记录

见 [CHANGELOG.md](CHANGELOG.md)。
