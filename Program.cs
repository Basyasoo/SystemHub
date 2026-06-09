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
        Console.WriteLine("=== DIAGNOSTICS: Testing VolumeService ===");
        try
        {
            float vol = Services.VolumeService.GetVolume();
            bool mute = Services.VolumeService.GetMute();
            Console.WriteLine($"VolumeService.GetVolume(): {vol}");
            Console.WriteLine($"VolumeService.GetMute(): {mute}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"VolumeService DIAGNOSTICS FAILED: {ex}");
        }
        Console.WriteLine("=== DIAGNOSTICS END ===");

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
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
