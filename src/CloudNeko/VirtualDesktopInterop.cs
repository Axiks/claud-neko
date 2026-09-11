using System.Runtime.InteropServices;

namespace CloudNeko;

/// <summary>
/// Обгортка над публічним, документованим COM-інтерфейсом IVirtualDesktopManager
/// (доступний з Windows 10 1607+). Потрібен, щоб клік по іконці в треї показував
/// Akari навіть якщо вона зараз "живе" на іншому віртуальному робочому столі —
/// без цього Show() лишив би вікно видимим лише там, де воно було створене.
/// </summary>
internal static class VirtualDesktopInterop
{
    private static readonly Guid ClsidVirtualDesktopManager = new("aa509086-5ca9-4c25-8f95-589d3c07b48a");

    [ComImport]
    [Guid("a5cd92ff-29be-454c-8d04-d82879fb3f1b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IVirtualDesktopManager
    {
        [PreserveSig]
        int IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow, out bool onCurrentDesktop);

        [PreserveSig]
        int GetWindowDesktopId(IntPtr topLevelWindow, out Guid desktopId);

        [PreserveSig]
        int MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    /// <summary>
    /// Якщо вікно не на тому віртуальному столі, де зараз користувач — переносить
    /// його туди (визначаючи "поточний" стіл за столом активного вікна). Тихо нічого
    /// не робить, якщо інтерфейс недоступний (стара Windows) чи щось пішло не так —
    /// наприклад, якщо активне на момент кліка вікно саме не "знає" свій стіл
    /// (буває для деяких системних/службових вікон); тоді Akari просто покажеться
    /// на тому столі, де вона вже є, без телепортації.
    /// </summary>
    public static void MoveToCurrentDesktop(IntPtr hwnd)
    {
        try
        {
            var type = Type.GetTypeFromCLSID(ClsidVirtualDesktopManager)
                ?? throw new NotSupportedException();
            var manager = (IVirtualDesktopManager)Activator.CreateInstance(type)!;

            if (manager.IsWindowOnCurrentVirtualDesktop(hwnd, out bool onCurrent) != 0 || onCurrent)
            {
                return;
            }

            IntPtr foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero)
            {
                return;
            }

            if (manager.GetWindowDesktopId(foreground, out Guid currentDesktopId) != 0)
            {
                return;
            }

            manager.MoveWindowToDesktop(hwnd, ref currentDesktopId);
        }
        catch
        {
            // Немає підтримки на цій версії Windows / вікно ще без handle / щось
            // пішло не так у COM — просто не переносимо, не критично.
        }
    }
}
