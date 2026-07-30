namespace GtfobinsOffline;

public sealed record CommandVariant(string Function, string Context, string Code, string? Comment);

public sealed class GtfobinEntry
{
    public required string Name { get; init; }
    public string? Alias { get; init; }
    public string? Comment { get; init; }
    public List<CommandVariant> Variants { get; } = [];

    public IEnumerable<string> SearchTerms => new[] { Name, Alias ?? string.Empty }.Where(x => !string.IsNullOrWhiteSpace(x));

    public override string ToString() => string.IsNullOrWhiteSpace(Alias) ? Name : $"{Name}  ·  {Alias}";
}

public enum MatchKind { Exact, OfficialAlias, Family, NotFound, Forbidden }

public sealed class BatchMatch
{
    public required string OriginalLine { get; init; }
    public required string Path { get; init; }
    public required string CommandName { get; init; }
    public MatchKind Kind { get; init; }
    public GtfobinEntry? Entry { get; init; }
    public bool IsForbidden { get; init; }
    public string? RunAs { get; init; }
    public string? Tags { get; init; }

    public string Label(bool isChinese)
    {
        var status = isChinese ? Kind switch
        {
            MatchKind.Exact => "精确匹配",
            MatchKind.OfficialAlias => "官方别名",
            MatchKind.Family => "需确认版本",
            MatchKind.Forbidden => "已禁止",
            _ => "未收录"
        } : Kind switch
        {
            MatchKind.Exact => "Exact match",
            MatchKind.OfficialAlias => "Official alias",
            MatchKind.Family => "Confirm version",
            MatchKind.Forbidden => "Forbidden",
            _ => "Not listed"
        };
        return $"{CommandName}  ·  {status}";
    }

    public override string ToString() => Label(true);
}

public sealed record BatchResultItem(BatchMatch Match, string Label)
{
    public System.Windows.Media.Brush StatusBrush => (System.Windows.Media.Brush)System.Windows.Application.Current.Resources[Match.Kind switch
    {
        MatchKind.Exact or MatchKind.OfficialAlias => "Success",
        MatchKind.Family => "Warning",
        MatchKind.Forbidden => "Danger",
        _ => "SecondaryForeground"
    }];

    public override string ToString() => Label;
}
