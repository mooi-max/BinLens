using System.Windows;

namespace GtfobinsOffline;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        if (UpdateApplier.IsApplyRequest(e.Args))
        {
            UpdateApplier.Apply(e.Args);
            Shutdown();
            return;
        }
        if (UpdateApplier.TryGetCleanupPath(e.Args, out var helperPath) && helperPath is not null) UpdateService.TryDelete(helperPath);
        base.OnStartup(e);
        MainWindow = new MainWindow();
        MainWindow.Show();
    }
}
