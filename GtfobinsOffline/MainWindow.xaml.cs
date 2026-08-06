using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GtfobinsOffline;

public partial class MainWindow : Window
{
    private enum BatchMode { Sudo, Suid }

    private readonly IReadOnlyList<GtfobinEntry> _entries;
    private readonly ObservableCollection<GtfobinEntry> _results = [];
    private readonly ObservableCollection<BatchMatch> _batchResults = [];
    private bool _isChinese = true;
    private bool _isDark;
    private string _activeContext = string.Empty;
    private BatchMode _batchMode = BatchMode.Sudo;
    private readonly AppSettings _settings;

    private static readonly IReadOnlyDictionary<string, string> FunctionZh = new Dictionary<string, string>
    {
        ["shell"] = "交互式 Shell", ["command"] = "执行命令", ["file-read"] = "读取文件", ["file-write"] = "写入文件",
        ["download"] = "下载文件", ["upload"] = "上传文件", ["reverse-shell"] = "反向 Shell", ["bind-shell"] = "绑定 Shell",
        ["library-load"] = "加载库", ["file-upload"] = "上传文件", ["file-download"] = "下载文件"
    };
    private static readonly IReadOnlyDictionary<string, string> ContextZh = new Dictionary<string, string>
    {
        ["sudo"] = "Sudo", ["suid"] = "SUID", ["capabilities"] = "Capabilities", ["unprivileged"] = "普通用户",
        ["limited-suid"] = "受限 SUID"
    };
    private static readonly IReadOnlyDictionary<string, string> FunctionDescriptionZh = new Dictionary<string, string>
    {
        ["shell"] = "该可执行文件可以启动交互式系统 Shell。",
        ["command"] = "该可执行文件可以执行非交互式系统命令。",
        ["file-read"] = "该可执行文件可以读取本地文件。",
        ["file-write"] = "该可执行文件可以写入本地文件。",
        ["download"] = "该可执行文件可以下载远程文件。",
        ["upload"] = "该可执行文件可以上传本地数据。",
        ["reverse-shell"] = "该可执行文件可以建立反向系统 Shell。",
        ["bind-shell"] = "该可执行文件可以绑定本地端口并等待连接。",
        ["library-load"] = "该可执行文件可以加载本地动态库。"
    };
    private static readonly IReadOnlyDictionary<string, string> ContextDescriptionZh = new Dictionary<string, string>
    {
        ["sudo"] = "通过 sudo 执行时，程序不会主动丢弃获得的权限。",
        ["suid"] = "需要正确的 SUID 位和文件所有权。",
        ["capabilities"] = "需要为该可执行文件设置相应的 Linux Capabilities。",
        ["unprivileged"] = "此用法可在普通用户上下文中使用。"
    };

    public MainWindow()
    {
        InitializeComponent();
        _settings = SettingsStore.Load();
        _isChinese = _settings.IsChinese;
        _isDark = _settings.IsDark;
        _entries = GtfobinsDataLoader.Load();
        ResultList.ItemsSource = _results;
        ApplyLanguage();
        ApplyTheme();
        RefreshResults();
        PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V && BatchView.Visibility == Visibility.Visible && !BatchInput.IsKeyboardFocusWithin)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    BatchInput.Focus();
                    BatchInput.SelectedText = Clipboard.GetText();
                }
                e.Handled = true;
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                StatusText.Text = _isChinese ? "剪贴板暂时被占用，请重试" : "Clipboard is temporarily unavailable. Please try again.";
                e.Handled = true;
            }
        }
    }

    private void ApplyLanguage()
    {
        Title = $"BinLens v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0"}";
        AppTitle.Text = "BinLens";
        AppSubtitle.Text = _isChinese ? "GTFOBins 离线速查 · 本地数据 · 不执行命令" : "GTFOBins offline lookup · local data · never runs commands";
        LanguageButton.Content = _isChinese ? "English" : "中文";
        ThemeButton.Content = _isChinese ? (_isDark ? "浅色" : "深色") : (_isDark ? "Light" : "Dark");
        AboutButton.Content = _isChinese ? "关于" : "About";
        UpdateButton.Content = _isChinese ? "检查更新" : "Check updates";
        SearchBox.ToolTip = "Ctrl+F";
        SearchHint.Text = _isChinese ? "搜索命令、别名或功能，例如 find、python、Sudo" : "Search commands, aliases, or functions: find, python, Sudo";
        BatchButton.Content = _isChinese ? "批量分析" : "Batch analysis";
        EmptyDetailTitle.Text = _isChinese ? "开始检索" : "Start searching";
        EmptyDetailText.Text = _isChinese ? "输入命令名，或使用批量分析粘贴 sudo -l 输出或 SUID 清单。" : "Enter a command name, or paste sudo -l output or a SUID file list into batch analysis.";
        BatchTitle.Text = _batchMode == BatchMode.Sudo ? (_isChinese ? "批量分析 sudo -l" : "Batch analyze sudo -l") : (_isChinese ? "批量分析 SUID 清单" : "Batch analyze SUID file list");
        BatchDescription.Text = _batchMode == BatchMode.Sudo
            ? (_isChinese ? "粘贴原始输出；所有解析均在本机完成。" : "Paste raw output; all analysis remains on this device.")
            : (_isChinese ? "粘贴 find 输出的 SUID 文件绝对路径（每行一个），例如 /usr/bin/find；所有解析均在本机完成。" : "Paste absolute SUID file paths from find output (one per line), e.g. /usr/bin/find; all analysis remains on this device.");
        BackButton.Content = _isChinese ? "← 返回检索" : "← Back to search";
        BatchModeSudoButton.Content = _isChinese ? "sudo -l 输出" : "sudo -l output";
        BatchModeSuidButton.Content = _isChinese ? "SUID 清单" : "SUID file list";
        AnalyzeButton.Content = _batchMode == BatchMode.Sudo ? (_isChinese ? "分析 sudo -l 输出" : "Analyze sudo -l output") : (_isChinese ? "分析 SUID 清单" : "Analyze SUID file list");
        BatchResultTitle.Text = _isChinese ? "匹配结果" : "Matches";
        StatusText.Text = _isChinese ? $"已内置 {_entries.Count} 个 GTFOBins 条目" : $"{_entries.Count} GTFOBins entries embedded";
        FilterAllButton.Content = _isChinese ? "全部" : "All";
        FilterSudoButton.Content = "Sudo";
        FilterSuidButton.Content = "SUID";
        FilterCapabilitiesButton.Content = "Capabilities";
        FilterUserButton.Content = _isChinese ? "普通用户" : "Unprivileged";
        RefreshFilterButtons();
        RefreshBatchModeButtons();
        RefreshResults();
        RefreshBatchLabels();
        UpdateSearchHint();
    }

    private void ApplyTheme()
    {
        var colors = _isDark
            ? new Dictionary<string, Color>
            {
                ["AppBackground"] = Color.FromRgb(24, 24, 24), ["Surface"] = Color.FromRgb(33, 33, 33), ["SurfaceElevated"] = Color.FromRgb(38, 38, 38), ["SurfaceMuted"] = Color.FromRgb(45, 45, 45),
                ["Foreground"] = Color.FromRgb(245, 245, 245), ["SecondaryForeground"] = Color.FromRgb(180, 180, 180), ["TertiaryForeground"] = Color.FromRgb(140, 140, 140), ["Border"] = Color.FromRgb(58, 58, 58),
                ["Accent"] = Color.FromRgb(59, 59, 59), ["AccentHover"] = Color.FromRgb(72, 72, 72), ["AccentMuted"] = Color.FromRgb(48, 48, 48), ["AccentForeground"] = Colors.White,
                ["Success"] = Color.FromRgb(95, 187, 159), ["Warning"] = Color.FromRgb(224, 173, 95), ["Danger"] = Color.FromRgb(218, 111, 111), ["CodeBackground"] = Color.FromRgb(15, 15, 15), ["CodeForeground"] = Color.FromRgb(245, 245, 245)
            }
            : new Dictionary<string, Color>
            {
                ["AppBackground"] = Color.FromRgb(250, 250, 250), ["Surface"] = Colors.White, ["SurfaceElevated"] = Color.FromRgb(247, 247, 247), ["SurfaceMuted"] = Color.FromRgb(241, 241, 241),
                ["Foreground"] = Color.FromRgb(31, 31, 31), ["SecondaryForeground"] = Color.FromRgb(107, 107, 107), ["TertiaryForeground"] = Color.FromRgb(146, 146, 146), ["Border"] = Color.FromRgb(228, 228, 228),
                ["Accent"] = Color.FromRgb(31, 31, 31), ["AccentHover"] = Color.FromRgb(17, 17, 17), ["AccentMuted"] = Color.FromRgb(236, 236, 236), ["AccentForeground"] = Colors.White,
                ["Success"] = Color.FromRgb(22, 124, 99), ["Warning"] = Color.FromRgb(169, 99, 22), ["Danger"] = Color.FromRgb(182, 73, 73), ["CodeBackground"] = Color.FromRgb(31, 31, 31), ["CodeForeground"] = Color.FromRgb(245, 245, 245)
            };
        foreach (var (key, color) in colors) Application.Current.Resources[key] = new SolidColorBrush(color);
        Background = (Brush)Application.Current.Resources["AppBackground"];
        RefreshFilterButtons();
    }

    private void RefreshResults()
    {
        if (_entries is null) return;
        var term = SearchBox?.Text.Trim() ?? string.Empty;
        var matches = _entries.Where(entry => Matches(entry, term)).ToArray();
        _results.Clear();
        foreach (var entry in matches) _results.Add(entry);
        ResultCountText.Text = _isChinese ? $"{matches.Length} 个匹配条目" : $"{matches.Length} matching entries";
        if (matches.Length > 0)
        {
            ResultList.SelectedIndex = 0;
            RenderEntry(DetailPanel, matches[0], string.IsNullOrEmpty(_activeContext) ? null : _activeContext);
        }
        else RenderEmptyDetail(_isChinese ? "没有匹配条目" : "No matching entries");
    }

    private void RefreshBatchLabels()
    {
        if (BatchResultList is null) return;
        BatchResultList.ItemsSource = _batchResults.Select(match => new BatchResultItem(match, match.Label(_isChinese))).ToArray();
    }

    private bool Matches(GtfobinEntry entry, string term)
    {
        if (!string.IsNullOrEmpty(_activeContext) && !entry.Variants.Any(variant => string.Equals(variant.Context, _activeContext, StringComparison.OrdinalIgnoreCase))) return false;
        if (string.IsNullOrWhiteSpace(term)) return true;
        return entry.SearchTerms.Any(value => value.Contains(term, StringComparison.OrdinalIgnoreCase))
            || entry.Variants.Any(variant => variant.Function.Contains(term, StringComparison.OrdinalIgnoreCase) || ContextLabel(variant.Context).Contains(term, StringComparison.OrdinalIgnoreCase) || FunctionLabel(variant.Function).Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private void RefreshFilterButtons()
    {
        if (FilterAllButton is null) return;
        foreach (var button in new[] { FilterAllButton, FilterSudoButton, FilterSuidButton, FilterCapabilitiesButton, FilterUserButton })
        {
            var active = string.Equals(button.Tag?.ToString(), _activeContext, StringComparison.OrdinalIgnoreCase);
            button.Background = (Brush)Application.Current.Resources[active ? "AccentMuted" : "Surface"];
            button.BorderBrush = (Brush)Application.Current.Resources[active ? "Accent" : "Border"];
            button.Foreground = (Brush)Application.Current.Resources["Foreground"];
        }
    }

    private void RefreshBatchModeButtons()
    {
        if (BatchModeSudoButton is null || BatchModeSuidButton is null) return;
        foreach (var button in new[] { BatchModeSudoButton, BatchModeSuidButton })
        {
            var active = string.Equals(button.Tag?.ToString(), _batchMode == BatchMode.Sudo ? "sudo" : "suid", StringComparison.OrdinalIgnoreCase);
            button.Background = (Brush)Application.Current.Resources[active ? "AccentMuted" : "Surface"];
            button.BorderBrush = (Brush)Application.Current.Resources[active ? "Accent" : "Border"];
            button.Foreground = (Brush)Application.Current.Resources["Foreground"];
        }
    }

    private string FunctionLabel(string function) => _isChinese && FunctionZh.TryGetValue(function, out var label) ? label : function.Replace('-', ' ');
    private string ContextLabel(string context) => _isChinese && ContextZh.TryGetValue(context, out var label) ? label : context.Replace('-', ' ');

    private static int ContextSortOrder(string context) => context.ToLowerInvariant() switch
    {
        "sudo" => 0,
        "suid" => 1,
        "limited-suid" => 2,
        "capabilities" => 3,
        "unprivileged" => 4,
        _ => 99
    };

    private string ContextSectionDescription(string context) => _isChinese ? context.ToLowerInvariant() switch
    {
        "sudo" => "\u901a\u8fc7 sudo \u6388\u6743\u6267\u884c\u3002",
        "suid" => "\u9700\u8981\u6b63\u786e\u7684 SUID \u4f4d\u548c\u6587\u4ef6\u6240\u6709\u6743\u3002",
        "limited-suid" => "\u9002\u7528\u4e8e\u53d7\u9650\u7684 SUID \u573a\u666f\u3002",
        "capabilities" => "\u9700\u8981\u4e3a\u53ef\u6267\u884c\u6587\u4ef6\u8bbe\u7f6e Linux Capabilities\u3002",
        "unprivileged" => "\u666e\u901a\u7528\u6237\u4e0a\u4e0b\u6587\u4e2d\u53ef\u7528\u3002",
        _ => ContextLabel(context)
    } : $"{ContextLabel(context)} context";

    private void AddContextSectionHeader(Panel panel, string context)
    {
        var header = new Border
        {
            Background = (Brush)Application.Current.Resources["SurfaceElevated"],
            BorderBrush = (Brush)Application.Current.Resources["Border"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 18, 0, 0)
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = ContextLabel(context), FontSize = 15, FontWeight = FontWeights.SemiBold });
        stack.Children.Add(new TextBlock
        {
            Text = ContextSectionDescription(context),
            Foreground = (Brush)Application.Current.Resources["SecondaryForeground"],
            Margin = new Thickness(0, 3, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });
        header.Child = stack;
        panel.Children.Add(header);
    }

    private void RenderEmptyDetail(string message)
    {
        DetailPanel.Children.Clear();
        DetailPanel.Children.Add(new TextBlock { Text = message, FontSize = 18, Foreground = (Brush)Application.Current.Resources["SecondaryForeground"], HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 52, 0, 0) });
    }

    private void RenderEntry(Panel panel, GtfobinEntry entry, string? contextFilter = null, BatchMatch? batchMatch = null)
    {
        panel.Children.Clear();
        panel.Children.Add(new TextBlock { Text = entry.Name, FontSize = 26, FontWeight = FontWeights.SemiBold });
        if (!string.IsNullOrWhiteSpace(entry.Alias)) panel.Children.Add(new TextBlock { Text = $"{(_isChinese ? "官方别名" : "Official alias")}: {entry.Alias}", Foreground = (Brush)Application.Current.Resources["SecondaryForeground"], Margin = new Thickness(0, 4, 0, 0) });
        if (batchMatch is not null)
        {
            var sourceLabel = batchMatch.IsSuidAnalysis
                ? (_isChinese ? "原始路径" : "Original path")
                : (_isChinese ? "原始授权行" : "Original rule");
            panel.Children.Add(new TextBlock { Text = $"{sourceLabel}: {batchMatch.OriginalLine}", TextWrapping = TextWrapping.Wrap, Foreground = (Brush)Application.Current.Resources["SecondaryForeground"], Margin = new Thickness(0, 10, 0, 0) });
            var details = string.Join(" · ", new[] { batchMatch.RunAs is null ? null : $"RunAs: {batchMatch.RunAs}", batchMatch.Tags }.Where(x => !string.IsNullOrWhiteSpace(x))!);
            if (!string.IsNullOrWhiteSpace(details)) panel.Children.Add(new TextBlock { Text = details, Foreground = (Brush)Application.Current.Resources["SecondaryForeground"], Margin = new Thickness(0, 3, 0, 0) });
        }
        if (!string.IsNullOrWhiteSpace(entry.Comment) && !_isChinese) panel.Children.Add(new TextBlock { Text = entry.Comment, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 14, 0, 0), Foreground = (Brush)Application.Current.Resources["SecondaryForeground"] });

        var variants = entry.Variants.Where(variant => contextFilter is null || string.Equals(variant.Context, contextFilter, StringComparison.OrdinalIgnoreCase))
            .GroupBy(variant => (variant.Function, variant.Context)).Select(group => group.First())
            .OrderBy(variant => ContextSortOrder(variant.Context)).ThenBy(variant => variant.Context).ThenBy(variant => variant.Function).ToArray();
        string? currentContext = null;
        foreach (var variant in variants)
        {
            if (!string.Equals(currentContext, variant.Context, StringComparison.OrdinalIgnoreCase))
            {
                AddContextSectionHeader(panel, variant.Context);
                currentContext = variant.Context;
            }
            var card = new Border
            {
                Background = (Brush)Application.Current.Resources["SurfaceElevated"],
                BorderBrush = (Brush)Application.Current.Resources["Border"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 16, 0, 0)
            };
            var stack = new StackPanel();
            var header = new DockPanel { LastChildFill = true };
            var copyButton = new Button
            {
                Content = _isChinese ? "复制" : "Copy",
                ToolTip = _isChinese ? "复制完整命令" : "Copy full command",
                Style = (Style)Application.Current.Resources["CopyButton"],
                Tag = variant.Code
            };
            copyButton.Click += (_, _) => CopyCommand((string)copyButton.Tag);
            DockPanel.SetDock(copyButton, Dock.Right);
            header.Children.Add(copyButton);
            header.Children.Add(new TextBlock
            {
                Text = $"{FunctionLabel(variant.Function)} · {ContextLabel(variant.Context)}",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            stack.Children.Add(header);
            var explanation = LocalizedExplanation(variant);
            if (!string.IsNullOrWhiteSpace(explanation)) stack.Children.Add(new TextBlock
            {
                Text = explanation,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Application.Current.Resources["SecondaryForeground"],
                Margin = new Thickness(0, 6, 0, 9)
            });
            stack.Children.Add(CreateCodeBox(variant.Code));
            card.Child = stack;
            panel.Children.Add(card);
        }
        if (variants.Length == 0) panel.Children.Add(new TextBlock { Text = _isChinese ? "该条目没有对应场景的官方命令。" : "No official commands for this context.", Margin = new Thickness(0, 16, 0, 0), Foreground = (Brush)Application.Current.Resources["SecondaryForeground"] });
    }

    private TextBox CreateCodeBox(string code)
    {
        var box = new TextBox
        {
            Text = code,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            BorderThickness = new Thickness(0),
            Background = (Brush)Application.Current.Resources["CodeBackground"],
            Foreground = (Brush)Application.Current.Resources["CodeForeground"],
            FontFamily = new FontFamily("Cascadia Mono, Consolas"),
            FontSize = 12.5,
            Padding = new Thickness(12),
            Cursor = Cursors.Hand,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 220
        };

        Point? pointerDown = null;
        box.PreviewMouseLeftButtonDown += (_, eventArgs) => pointerDown = eventArgs.GetPosition(box);
        box.PreviewMouseLeftButtonUp += (_, eventArgs) =>
        {
            if (pointerDown is Point start)
            {
                var end = eventArgs.GetPosition(box);
                if (CommandCopyService.TryCopyIfClick(box.SelectionLength, end.X - start.X, end.Y - start.Y, box.Text, Clipboard.SetText))
                {
                    StatusText.Text = _isChinese ? "已复制完整命令" : "Full command copied";
                }
            }
            pointerDown = null;
        };
        return box;
    }

    private void CopyCommand(string command)
    {
        try
        {
            if (CommandCopyService.TryCopy(command, Clipboard.SetText))
            {
                StatusText.Text = _isChinese ? "已复制完整命令" : "Full command copied";
            }
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            StatusText.Text = _isChinese ? "剪贴板暂时被占用，请重试" : "Clipboard is temporarily unavailable. Please try again.";
        }
    }

    private string? LocalizedExplanation(CommandVariant variant)
    {
        if (!_isChinese) return variant.Comment;
        var function = FunctionDescriptionZh.TryGetValue(variant.Function, out var functionText)
            ? functionText : $"GTFOBins 收录的“{FunctionLabel(variant.Function)}”用法。";
        var context = ContextDescriptionZh.TryGetValue(variant.Context, out var contextText) ? $" {contextText}" : string.Empty;
        return function + context;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateSearchHint();
        RefreshResults();
    }

    private void UpdateSearchHint()
    {
        if (SearchHint is not null && SearchBox is not null)
        {
            SearchHint.Visibility = string.IsNullOrWhiteSpace(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
    private void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        _activeContext = (sender as Button)?.Tag?.ToString() ?? string.Empty;
        RefreshFilterButtons();
        RefreshResults();
    }
    private void ResultList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResultList.SelectedItem is GtfobinEntry entry) RenderEntry(DetailPanel, entry, string.IsNullOrEmpty(_activeContext) ? null : _activeContext);
    }
    private void LanguageButton_Click(object sender, RoutedEventArgs e) { _isChinese = !_isChinese; SaveSettings(); ApplyLanguage(); if (ResultList.SelectedItem is GtfobinEntry entry) RenderEntry(DetailPanel, entry, string.IsNullOrEmpty(_activeContext) ? null : _activeContext); }
    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        _isDark = !_isDark;
        SaveSettings();
        ApplyTheme();
        ApplyLanguage();
        if (ResultList.SelectedItem is GtfobinEntry entry) RenderEntry(DetailPanel, entry, string.IsNullOrEmpty(_activeContext) ? null : _activeContext);
        if (BatchResultList.SelectedIndex >= 0)
        {
            var selectedIndex = BatchResultList.SelectedIndex;
            BatchResultList.SelectedIndex = -1;
            BatchResultList.SelectedIndex = selectedIndex;
        }
    }
    private void BatchButton_Click(object sender, RoutedEventArgs e) { SearchView.Visibility = Visibility.Collapsed; BatchView.Visibility = Visibility.Visible; BatchInput.Focus(); }
    private void BackButton_Click(object sender, RoutedEventArgs e) { BatchView.Visibility = Visibility.Collapsed; SearchView.Visibility = Visibility.Visible; SearchBox.Focus(); }
    private void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";
        var message = $"BinLens · GTFOBins 离线速查\n版本 {version}\n\n面向授权安全测试、系统审计和学习场景的 Windows 离线查询工具。\n\n• 内置 {_entries.Count} 个 GTFOBins 公开条目\n• 支持命令检索与本地批量分析（sudo -l 输出、SUID 清单）\n• 不执行命令，不上传粘贴内容，不收集账号、行为数据或遥测\n\n数据来源：GTFOBins/GTFOBins.github.io\n项目许可证：GPL-3.0\n\n请仅在拥有明确授权的系统、靶场或实验环境中使用。";
        MessageBox.Show(message, "关于", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void AnalyzeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            AnalyzeBatch();
        }
        catch (ArgumentException ex) { MessageBox.Show(ex.Message, _isChinese ? "无法分析" : "Cannot analyze", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void BatchModeButton_Click(object sender, RoutedEventArgs e)
    {
        _batchMode = string.Equals((sender as Button)?.Tag?.ToString(), "suid", StringComparison.OrdinalIgnoreCase) ? BatchMode.Suid : BatchMode.Sudo;
        RefreshBatchModeButtons();
        ApplyLanguage();
        _batchResults.Clear();
        RefreshBatchLabels();
        BatchDetailPanel.Children.Clear();
        BatchResultTitle.Text = _isChinese ? "匹配结果" : "Matches";
        if (!string.IsNullOrWhiteSpace(BatchInput.Text)) AnalyzeBatch();
    }

    private void AnalyzeBatch()
    {
        var matches = _batchMode == BatchMode.Sudo ? SudoParser.Parse(BatchInput.Text, _entries) : SudoParser.ParseSuid(BatchInput.Text, _entries);
        _batchResults.Clear();
        foreach (var match in matches) _batchResults.Add(match);
        RefreshBatchLabels();
        BatchResultTitle.Text = _isChinese ? $"匹配结果（{matches.Count}）" : $"Matches ({matches.Count})";
        BatchDetailPanel.Children.Clear();
        if (matches.Count == 0)
        {
            var emptyMessage = _batchMode == BatchMode.Sudo
                ? (_isChinese ? "未识别到 sudo 授权规则。请粘贴完整 sudo -l 输出。" : "No sudo authorization rules were found. Paste complete sudo -l output.")
                : (_isChinese ? "未识别到 SUID 文件路径。请粘贴 find 输出，每行一个绝对路径（例如 /usr/bin/find）。" : "No SUID file paths were found. Paste find output with one absolute path per line (e.g. /usr/bin/find).");
            BatchDetailPanel.Children.Add(new TextBlock { Text = emptyMessage, TextWrapping = TextWrapping.Wrap, Foreground = (Brush)Application.Current.Resources["SecondaryForeground"], Margin = new Thickness(0, 40, 0, 0) });
        }
        else BatchResultList.SelectedIndex = 0;
    }

    private void BatchResultList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BatchResultList.SelectedItem is not BatchResultItem item) return;
        var match = item.Match;
        if (match.Entry is null)
        {
            BatchDetailPanel.Children.Clear();
            var status = match.IsForbidden ? (_isChinese ? "该规则明确禁止。" : "This rule is explicitly forbidden.") : (_isChinese ? "GTFOBins 未收录此命令。" : "This command is not listed in GTFOBins.");
            BatchDetailPanel.Children.Add(new TextBlock { Text = status, FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 35, 0, 8) });
            BatchDetailPanel.Children.Add(new TextBox { Text = match.OriginalLine, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, BorderThickness = new Thickness(0), Background = (Brush)Application.Current.Resources["SurfaceMuted"], Padding = new Thickness(10) });
            return;
        }
        RenderEntry(BatchDetailPanel, match.Entry, _batchMode == BatchMode.Sudo ? "sudo" : "suid", match);
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UpdateService.Repository))
        {
            MessageBox.Show(_isChinese ? "当前是本地开发版本，尚未配置 GitHub Release 更新源。" : "This local development build has no GitHub Release update source configured.", _isChinese ? "检查更新" : "Check updates", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        UpdateButton.IsEnabled = false;
        try
        {
            var release = await UpdateService.CheckAsync();
            if (release is null || !UpdateService.IsNewer(release.Version))
            {
                MessageBox.Show(_isChinese ? "当前已是最新版本。" : "You are already on the latest version.", _isChinese ? "检查更新" : "Check updates", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var prompt = _isChinese
                ? $"发现新版本 {release.Version}（{release.Application.Size / 1024.0 / 1024.0:F1} MB）。\n\n下载、校验后将自动替换当前 EXE 并重启。\n\n{release.Notes}"
                : $"Version {release.Version} is available ({release.Application.Size / 1024.0 / 1024.0:F1} MB).\n\nAfter download and verification, the app will replace the current EXE and restart.\n\n{release.Notes}";
            if (MessageBox.Show(prompt, _isChinese ? "发现更新" : "Update available", MessageBoxButton.YesNo, MessageBoxImage.Information) != MessageBoxResult.Yes) return;

            StatusText.Text = _isChinese ? "正在下载并校验更新…" : "Downloading and verifying update…";
            var download = await UpdateService.DownloadAndVerifyAsync(release, new Progress<double>(progress => StatusText.Text = _isChinese ? $"正在下载并校验更新… {progress:P0}" : $"Downloading and verifying update… {progress:P0}"));
            if (!UpdateApplier.TryLaunch(download)) throw new IOException(_isChinese ? "无法启动更新程序。" : "Unable to launch the update helper.");
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show(_isChinese ? $"无法检查更新：{ex.Message}" : $"Unable to check for updates: {ex.Message}", _isChinese ? "检查更新" : "Check updates", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { UpdateButton.IsEnabled = true; }
    }

    private void SaveSettings()
    {
        _settings.IsChinese = _isChinese;
        _settings.IsDark = _isDark;
        SettingsStore.Save(_settings);
    }
}
