# BinLens

> 面向授权渗透测试与安全审计的 Windows 离线 Linux 提权辅助查询工具（GTFOBins 非官方客户端）

[![License](https://img.shields.io/badge/license-GPL--3.0--only-1f1f1f)](LICENSE)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-1f1f1f)
![Runtime](https://img.shields.io/badge/runtime-.NET%208-1f1f1f)

![BinLens 深色主界面](GtfobinsOffline/Assets/readme-screenshot.jpg)

## 它是什么？

BinLens 将 [GTFOBins](https://gtfobins.github.io/) 的公开条目内置到一个可双击运行的 Windows 单文件应用中，帮助授权渗透测试人员和系统审计人员快速核对 Linux 主机上可用二进制程序的**已公开提权路径与权限滥用场景**。

它是一本完全离线的“提权速查手册”：

- **离线可用**：日常检索不依赖网络，458 个 GTFOBins 条目随应用内置，适合断网靶场和内网环境；
- **单文件运行**：下载 `BinLens-win-x64.exe` 后双击即用，无需安装 .NET 运行时；
- **本地分析**：搜索、筛选和 `sudo -l` / SUID 批量分析全部在本机完成，不执行任何命令，也不上传任何粘贴内容。

## 典型工作流

1. 在已获授权的 Linux 主机或靶场中收集权限信息，例如 `sudo -l` 输出，或使用 `find` 枚举 SUID 文件。
2. 在 BinLens 的“批量分析”中粘贴原始输出，程序自动识别格式，一键定位其中被授权的二进制程序或 SUID 二进制。
3. 查看对应的 Sudo、SUID 或 Capabilities 场景，核对版本、环境与授权范围。
4. 将结果用于风险验证、修复建议和审计报告。实际执行任何命令前，请始终确认测试范围与授权边界。

## 功能特性

- **面向提权核对**：以 Sudo、SUID、受限 SUID、Capabilities 和普通用户等权限场景为核心组织方式，减少人工翻查成本；
- **命令检索**：按二进制名、官方别名或功能关键词搜索，例如 `find`、`python`、`Sudo`；
- **场景筛选**：顶部筛选按钮可只看 Sudo / SUID / Capabilities / 普通用户场景；
- **批量分析**：一个输入框自动识别 `sudo -l` 规则与 SUID 路径清单，结果按匹配质量着色排序，详情支持场景筛选；
- **一键复制**：单击命令代码区即可复制完整命令；拖动选中只选文字、不会误触复制，选中后也可 `Ctrl+C`；
- **中英双语与明暗主题**：界面支持中文/English 与浅色/深色切换；命令、路径、参数始终保留官方原文；
- **隐私优先**：不收集账号、行为数据或遥测；检索与文本解析均在本地完成。

## 下载与运行

从 [Releases](../../releases/latest) 下载最新的 `BinLens-win-x64.exe`，双击即可运行。每个 Release 都附带对应的 SHA-256 校验文件 `BinLens-win-x64.exe.sha256`，建议下载后校验：

```powershell
Get-FileHash .\BinLens-win-x64.exe -Algorithm SHA256
```

将输出与 Release 中的校验值对比，一致即可放心使用。

## 使用指南

### 1. 搜索与筛选

在搜索框输入二进制名、官方别名或功能关键词，例如 `find`、`python`、`Sudo`；按 `Ctrl+F` 可随时聚焦搜索框。

结果列表左侧显示匹配条目，右侧显示详情。使用顶部筛选按钮可只看特定场景：

| 筛选 | 含义 |
| --- | --- |
| 全部 | 所有场景 |
| Sudo | 通过 `sudo` 授权执行时的用法 |
| SUID | 设置了 SUID 位时的用法 |
| Capabilities | 设置了 Linux Capabilities 时的用法 |
| 普通用户 | 无需任何特殊权限即可使用的用法 |

### 2. 查看详情与复制命令

详情页按场景分区展示官方命令：

- **单击**命令代码区：复制完整命令，底部状态栏提示“已复制完整命令”；
- **拖动选中**代码：只选择文字，不会触发复制；选中后按 `Ctrl+C` 复制选中内容；
- 说明文字、批量结果与原始授权行均支持拖选和 `Ctrl+C`。

### 3. 批量分析

点击主界面右上角的“批量分析”进入批量分析页。输入框**自动识别格式**：粘贴 `sudo -l` 输出或 SUID 路径清单均可，无需手动切换模式。所有解析均在本机完成，粘贴内容不会离开设备。

#### 收集数据

在目标主机执行：

```bash
sudo -l
find / -perm -u=s -type f 2>/dev/null
```

将输出复制到输入框（`sudo -l` 整段粘贴即可，`find` 输出每行一个绝对路径），点击“开始分析”，或按 `Ctrl+Enter`。

> **SUID 枚举命令为什么推荐这样写？**
> `find / -perm -u=s -type f 2>/dev/null` 与 `find / -user root -perm -4000 -type f 2>/dev/null` 都是推荐写法。`-perm 4000` 是“权限位精确等于 4000”，只会命中只带 SUID 位、没有 rwx 权限的文件，很多真实 SUID 文件（如 4755）会被漏掉；`-perm -4000` 表示“只要设置了 SUID 位”，与 `-perm -u=s` 等价。另外建议保留 `-type f`，避免把 SUID 目录也列进来造成干扰。

#### 结果排序与颜色

匹配结果按重要性从高到低排序：

| 排序 | 颜色 | 分组 |
| --- | --- | --- |
| 最前 | 绿色 | 精确匹配、官方别名 |
| 中间 | 黄色 | 需确认版本、无 SUID 用法 |
| 最后 | 红色 | 未收录、已禁止 |

同一组内按命令名排序。底部状态栏会显示本次解析的统计（sudo 规则数与 SUID 路径数）。

#### 匹配状态说明

程序会逐条解析被授权的命令，并保留原始授权行（或原始路径）、RunAs 与 sudo 标签（如 `NOPASSWD`、`SETENV`、`LOG_INPUT` 等）。例如：

```text
(ALL) NOPASSWD: /usr/bin/find
(ALL) NOPASSWD: /usr/bin/python3
(www-data : www-data) SETENV: /usr/bin/python3, /bin/kill
/usr/bin/find
/usr/bin/python3.11
```

| 状态 | 含义 |
| --- | --- |
| 精确匹配 | 命令名与 GTFOBins 条目完全一致，直接查看对应用法 |
| 官方别名 | 命令是 GTFOBins 条目的官方别名 |
| 需确认版本 | 命令名带版本号（如 `python3.11` → `python`），需按实际版本核对 |
| 无 SUID 用法 | 该二进制在 GTFOBins 中，但只有其他场景（如 Sudo）的用法 |
| 已禁止 | 该规则被 `!` 明确禁止（如 `!/bin/su`） |
| 未收录 | GTFOBins 没有该命令条目，不代表无风险，建议人工核对 |

#### 详情与场景筛选

点击左侧任一结果，右侧展示对应命令：SUID 清单匹配默认展示该二进制的 **SUID 场景官方命令**；“无 SUID 用法”的结果默认展示该命令的全部相关场景，避免有效命令被隐藏。详情顶部会保留原始授权行或原始路径，便于与靶机输出一一对应。

详情区还提供 **全部 / Sudo / SUID / Capabilities / 普通用户** 筛选按钮，可随时切换到任意场景查看官方命令。

注意：`sudo -l` 输出中的 `secure_path`、`Matching Defaults` 等配置行会被自动忽略，不会误判为命令；常见系统 SUID 二进制（如 `/usr/bin/passwd`、`/usr/bin/sudo`）往往在 GTFOBins 中只有 Sudo 场景或未被收录，会被标记为“无 SUID 用法”或“未收录”——这本身也是有效信息，但仍应结合系统版本、SELinux、AppArmor 等实际环境人工评估。

#### 批量分析注意事项

- 自动识别规则：输入中包含 RunAs（`(...)`）或 sudo 标签（如 `NOPASSWD:`）时按 `sudo -l` 输出解析（保留多行续行规则）；纯绝对路径列表按 SUID 路径解析；既不含规则也不含路径的内容会被忽略；
- 单次粘贴上限为 1 MB，超出会提示且不会执行分析；
- 分析仅做文本匹配，不执行、不生成、不上传任何命令；请只在拥有明确授权的系统中收集数据。

### 4. 界面与设置

- **语言**：右上角“English / 中文”切换界面语言，命令、路径、参数保持官方原文；
- **主题**：右上角切换浅色/深色主题；
- **检查更新**：点击后从 GitHub Releases 获取新版本，下载并校验 SHA-256 后自动替换当前 EXE 并重启；仅在你主动点击时才访问网络；
- **快捷键**：`Ctrl+F` 聚焦搜索框；批量分析页中 `Ctrl+V` 粘贴剪贴板内容、`Ctrl+Enter` 开始分析。

## 数据、许可证与安全边界

- 数据来源：[GTFOBins/GTFOBins.github.io](https://github.com/GTFOBins/GTFOBins.github.io)，随应用内置；
- 本项目采用 [GPL-3.0-only](LICENSE)，上游许可证与署名见 [NOTICE.md](NOTICE.md)；
- 命令、二进制名、路径与参数保持上游原文；中文仅用于 BinLens 的界面和说明；
- 内置数据可能滞后、存在遗漏或不适用于特定版本与环境，使用前请自行验证；
- 安全问题请查看 [SECURITY.md](SECURITY.md)。

## 本地构建

要求：.NET SDK 8。

```powershell
# 运行自检（验证内置数据完整性与解析逻辑）
dotnet run --project .\GtfobinsOffline.SelfTest\GtfobinsOffline.SelfTest.csproj -c Release

# 发布单文件 EXE
dotnet publish .\GtfobinsOffline\GtfobinsOffline.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  "-p:GitHubRepository=mooi-max/BinLens" `
  -o .\publish
```

推送 `v*` 标签会触发 GitHub Actions：执行自检、构建单文件 EXE、生成 SHA-256 校验文件，并自动创建 GitHub Release。

## 贡献与致谢

欢迎提交数据同步、翻译、可访问性和界面体验改进。详见 [CONTRIBUTING.md](CONTRIBUTING.md) 与 [CHANGELOG.md](CHANGELOG.md)。

## 免责声明

- BinLens 是独立的非官方客户端，与 GTFOBins 项目没有隶属或背书关系；
- 本项目仅用于信息查询和本地文本解析；它不执行命令，也不提供任何访问授权；
- 请只在拥有明确授权的系统、靶场或实验环境中使用。使用者应自行遵守适用法律、组织政策与测试范围，并对自身操作负责。
