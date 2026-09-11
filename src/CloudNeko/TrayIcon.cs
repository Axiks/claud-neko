using System.Drawing;
using System.Windows.Forms;

namespace CloudNeko;

/// <summary>
/// Іконка в системному треї. Потрібна незалежно від видимості головного вікна —
/// коли Akari прихована (auto-hide), саме трей лишається єдиним способом дістатись
/// до меню (правий клік по прихованому вікну неможливий).
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _autoHideItem;
    private readonly ToolStripMenuItem _autoStartItem;

    public event Action? ToggleAutoHideRequested;
    public event Action? ToggleAutoStartRequested;
    public event Action? ExitRequested;
    public event Action? ShowRequested;

    public TrayIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("CloudNeko 🐾") { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());

        _autoHideItem = new ToolStripMenuItem("Автопоказ під час роботи") { CheckOnClick = true };
        _autoHideItem.Click += (_, _) => ToggleAutoHideRequested?.Invoke();
        menu.Items.Add(_autoHideItem);

        _autoStartItem = new ToolStripMenuItem("Автозапуск з Windows") { CheckOnClick = true };
        _autoStartItem.Click += (_, _) => ToggleAutoStartRequested?.Invoke();
        menu.Items.Add(_autoStartItem);

        menu.Items.Add(new ToolStripSeparator());
        var exitItem = new ToolStripMenuItem("Закрити");
        exitItem.Click += (_, _) => ExitRequested?.Invoke();
        menu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "CloudNeko",
            Visible = true,
            ContextMenuStrip = menu,
        };

        // Подвійний клік по іконці в треї — швидкий спосіб повернути приховану Akari.
        _notifyIcon.DoubleClick += (_, _) => ShowRequested?.Invoke();
    }

    public bool AutoHideChecked
    {
        get => _autoHideItem.Checked;
        set => _autoHideItem.Checked = value;
    }

    public bool AutoStartChecked
    {
        get => _autoStartItem.Checked;
        set => _autoStartItem.Checked = value;
    }

    private static Icon LoadIcon()
    {
        var uri = new Uri("pack://application:,,,/Assets/Akari/akari_sleepy.png");
        var streamInfo = System.Windows.Application.GetResourceStream(uri)
            ?? throw new InvalidOperationException("Tray icon resource not found.");
        using var bmp = new Bitmap(streamInfo.Stream);
        // Один раз за весь час життя застосунку — навмисно не викликаємо DestroyIcon
        // на GetHicon(), витік у межах одного handle не вартий зайвого P/Invoke.
        return Icon.FromHandle(bmp.GetHicon());
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
