using System.Windows;
using Application = System.Windows.Application;

namespace CloudNeko;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnLastWindowClose;
        var window = new MainWindow();
        window.Show();
    }
}
