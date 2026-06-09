using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MacStyleHub.Services
{
    public class StartupItem
    {
        public string Name { get; set; } = "";
        public string Command { get; set; } = "";
        public string Location { get; set; } = "User"; // User or System
    }

    public class StartupService
    {
        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "SystemHub";

        public bool IsAutostartEnabled()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
                if (key == null) return false;
                var value = key.GetValue(AppName);
                return value != null;
            }
            catch
            {
                return false;
            }
        }

        public void SetAutostart(bool enable)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                if (key == null) return;

                if (enable)
                {
                    // Get current executable path
                    string? exePath = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppName, $"\"{exePath}\"");
                    }
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error setting registry startup: " + ex.Message);
            }
        }

        public List<StartupItem> GetStartupItems()
        {
            var list = new List<StartupItem>();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return list;

            // Read User Run Key
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                if (key != null)
                {
                    if (key.GetValue("fafgatfa") != null)
                    {
                        key.DeleteValue("fafgatfa", false);
                    }

                    foreach (var valueName in key.GetValueNames())
                    {
                        var val = key.GetValue(valueName)?.ToString() ?? "";
                        list.Add(new StartupItem
                        {
                            Name = valueName,
                            Command = val,
                            Location = LocalizationService.Instance.StartupLocationUser
                        });
                    }
                }
            }
            catch { }

            // Read Local Machine Run Key
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(RunRegistryKey, false);
                if (key != null)
                {
                    foreach (var valueName in key.GetValueNames())
                    {
                        var val = key.GetValue(valueName)?.ToString() ?? "";
                        list.Add(new StartupItem
                        {
                            Name = valueName,
                            Command = val,
                            Location = LocalizationService.Instance.StartupLocationSystem
                        });
                    }
                }
            }
            catch { }

            return list;
        }
    }
}
