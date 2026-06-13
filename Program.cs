using Avalonia;
using System;

namespace MacStyleHub;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length >= 3 && args[0] == "--watchdog")
        {
            RunWatchdog(args[1], args[2]);
            return;
        }
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static void RunWatchdog(string pidStr, string deviceId)
    {
        try
        {
            if (int.TryParse(pidStr, out int pid))
            {
                var parent = System.Diagnostics.Process.GetProcessById(pid);
                if (parent != null)
                {
                    parent.WaitForExit();
                }
            }
        }
        catch (Exception)
        {
            // Parent already dead or not found
        }

        try
        {
            Services.AudioDeviceSwitcher.SetDefaultDevice(deviceId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Watchdog restore error: " + ex.Message);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
