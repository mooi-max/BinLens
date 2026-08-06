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
2. 在 BinLens 的“批量分析”中选择对应模式，粘贴原始输出，一键定位其中被授权的二进制程序或 SUID 二进制。
3. 查看对应的 Sudo、SUID 或 Capabilities 场景，核对版本、环境与授权范围。
4. 将结果用于风险验证、修复建议和审计报告。实际执行任何命令前，请始终确认测试范围与授权边界。

## 功能特性

- **面向提权核对**：以 Sudo、SUID、受限 SUID、Capabilities 和普通用户等权限场景为核心组织方式，减少人工翻查成本；
- **命令检索**：按二进制名、官方别名或功能关键词搜索，例如 `find`、`python`、`Sudo`；
- **场景筛选**：顶部筛选按钮可只看 Sudo / SUID / Capabilities / 普通用户场景；
- **批量分析（sudo -l）**：粘贴 `sudo -l` 原始输出，集中查看精确匹配、版本待确认、未收录与禁止规则；
- **批量分析（SUID 清单）**：粘贴 `find` 输出的 SUID 文件绝对路径，逐条匹配 GTFOBins 的 SUID 用法；
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

点击主界面右上角的“批量分析”进入批量分析页，左上角可切换两种分析模式：**`sudo -l` 输出** 与 **SUID 清单**。所有解析均在本机完成，粘贴内容不会离开设备。

#### 模式一：分析 `sudo -l` 输出

在目标主机执行：

```bash
sudo -l
```

将完整输出（从 `User ... may run the following commands on target:` 之后的内容开始粘贴即可，整段粘贴也可）复制到输入框，点击“分析 sudo -l 输出”。

程序会逐条解析被授权的命令，并保留原始授权行、RunAs 与 sudo 标签（如 `NOPASSWD`、`SETENV`、`LOG_INPUT` 等）。例如：

```text
(ALL) NOPASSWD: /usr/bin/find
(ALL) NOPASSWD: /usr/bin/python3
(www-data : www-data) SETENV: /usr/bin/python3, /bin/kill
```

结果状态说明：

| 状态 | 含义 |
| --- | --- |
| 精确匹配 | 命令名与 GTFOBins 条目完全一致，直接查看对应 Sudo 用法 |
| 官方别名 | 命令是 GTFOBins 条目的官方别名 |
| 需确认版本 | 命令名带版本号（如 `python3` → `python`），需按实际版本核对 |
| 已禁止 | 该规则被 `!` 明确禁止（如 `!/bin/su`） |
| 未收录 | GTFOBins 没有该命令条目，不代表无风险，建议人工核对 |

点击左侧任一结果，右侧会展示该命令的 **Sudo 场景官方命令**。注意：`sudo -l` 输出中的 `secure_path`、`Matching Defaults` 等配置行会被自动忽略，不会误判为命令。

#### 模式二：分析 SUID 清单

在目标主机执行（推荐写法）：

```bash
find / -perm -u=s -type f 2>/dev/null
# 或
find / -user root -perm -4000 -type f 2>/dev/null
```

把输出（每行一个绝对路径，例如 `/usr/bin/find`）粘贴到输入框，切换到“SUID 清单”模式，点击“分析 SUID 清单”。

> **为什么推荐 `-perm -4000` 而不是 `-perm 4000`？**
> `-perm 4000` 是“权限位精确等于 4000”，只会命中只带 SUID 位、没有 rwx 权限的文件，很多真实 SUID 文件（如 4755）会被漏掉；`-perm -4000` 表示“只要设置了 SUID 位”，与 `-perm -u=s` 等价，是更稳妥的枚举方式。另外建议保留 `-type f`，避免把 SUID 目录也列进来造成干扰。

结果状态说明：

| 状态 | 含义 |
| --- | --- |
| 精确匹配 | GTFOBins 收录了该二进制的 SUID 用法，直接查看 |
| 官方别名 | 该路径对应 GTFOBins 条目的官方别名，且条目有 SUID 用法 |
| 需确认版本 | 路径带版本号（如 `/usr/bin/python3.11` → `python`），按实际版本核对 SUID 用法 |
| 无 SUID 用法 | 该二进制在 GTFOBins 中，但只有其他场景（如 Sudo）的用法 |
| 未收录 | GTFOBins 没有该命令条目 |

点击任一结果，右侧展示该二进制的 **SUID 场景官方命令**（与搜索页的 SUID 筛选一致），而不是 Sudo 场景。原始路径会显示在详情顶部，便于与靶机输出一一对应。

常见系统 SUID 二进制（如 `/usr/bin/passwd`、`/usr/bin/sudo`）往往在 GTFOBins 中只有 Sudo 场景或未被收录，会被标记为“无 SUID 用法”或“未收录”——这本身也是有效信息，说明该文件没有已知的 GTFOBins SUID 提权路径，但仍应结合系统版本、SELinux、AppArmor 等实际环境人工评估。

#### 批量分析注意事项

- 两种模式分别接收不同的输入格式：`sudo -l` 模式接收规则行，SUID 模式接收纯路径列表；混用可能导致结果为空；
- 单次粘贴上限为 1 MB，超出会提示且不会执行分析；
- 分析仅做文本匹配，不执行、不生成、不上传任何命令；请只在拥有明确授权的系统中收集数据。

### 4. 界面与设置

- **语言**：右上角“English / 中文”切换界面语言，命令、路径、参数保持官方原文；
- **主题**：右上角切换浅色/深色主题；
- **检查更新**：点击后从 GitHub Releases 获取新版本，下载并校验 SHA-256 后自动替换当前 EXE 并重启；仅在你主动点击时才访问网络；
- **快捷键**：`Ctrl+F` 聚焦搜索框；批量分析页中 `Ctrl+V` 可直接粘贴剪贴板内容。

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
