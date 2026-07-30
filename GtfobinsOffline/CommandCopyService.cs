namespace GtfobinsOffline;

/// <summary>
/// Keeps command-copy interaction deterministic: a short click copies the full command,
/// while a drag or a text selection is left to the native TextBox selection behavior.
/// </summary>
public static class CommandCopyService
{
    private const double ClickTolerance = 4.0;

    public static bool ShouldCopyFullCommand(int selectionLength, double horizontalTravel, double verticalTravel)
        => selectionLength == 0
            && Math.Abs(horizontalTravel) <= ClickTolerance
            && Math.Abs(verticalTravel) <= ClickTolerance;

    public static bool TryCopy(string? command, Action<string> setClipboardText)
    {
        if (string.IsNullOrWhiteSpace(command)) return false;
        ArgumentNullException.ThrowIfNull(setClipboardText);
        setClipboardText(command);
        return true;
    }

    public static bool TryCopyIfClick(int selectionLength, double horizontalTravel, double verticalTravel, string? command, Action<string> setClipboardText)
    {
        if (!ShouldCopyFullCommand(selectionLength, horizontalTravel, verticalTravel)) return false;
        return TryCopy(command, setClipboardText);
    }
}
