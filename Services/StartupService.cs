using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MacStyleHub.Services
{
    public partial class StartupItem : ObservableObject
    {
        public string Name { get; set; } = "";
        public string Command { get; set; } = "";
        public string Location { get; set; } = "User"; // User or System

        [ObservableProperty]
        private bool _isEnabled = true;
    }

    public class StartupService
    {
        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string DisabledKeyPath = @"Software\SystemHub\DisabledStartup";
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
                            Location = LocalizationService.Instance.StartupLocationUser,
                            IsEnabled = true
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
                            Location = LocalizationService.Instance.StartupLocationSystem,
                            IsEnabled = true
                        });
                    }
                }
            }
            catch { }

            // Read Disabled Key
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(DisabledKeyPath, false);
                if (key != null)
                {
                    foreach (var valueName in key.GetValueNames())
                    {
                        var val = key.GetValue(valueName)?.ToString() ?? "";
                        int separatorIndex = val.IndexOf('|');
                        if (separatorIndex > 0)
                        {
                            string locStr = val.Substring(0, separatorIndex);
                            string cmd = val.Substring(separatorIndex + 1);
                            string localizedLoc = locStr == "System" 
                                ? LocalizationService.Instance.StartupLocationSystem 
                                : LocalizationService.Instance.StartupLocationUser;

                            list.Add(new StartupItem
                            {
                                Name = valueName,
                                Command = cmd,
                                Location = localizedLoc,
                                IsEnabled = false
                            });
                        }
                    }
                }
            }
            catch { }

            return list;
        }

        public bool ToggleStartupItem(string name, string location, string command, bool enable)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            try
            {
                using var disabledKey = Registry.CurrentUser.CreateSubKey(DisabledKeyPath, true);
                bool isSystem = (location == LocalizationService.Instance.StartupLocationSystem);
                var rootKey = isSystem ? Registry.LocalMachine : Registry.CurrentUser;

                if (enable)
                {
                    // Enabling: write back to Run, delete from Disabled
                    using var runKey = rootKey.OpenSubKey(RunRegistryKey, true);
                    if (runKey != null)
                    {
                        runKey.SetValue(name, command);
                        disabledKey.DeleteValue(name, false);
                        return true;
                    }
                }
                else
                {
                    // Disabling: write to Disabled, delete from Run
                    using var runKey = rootKey.OpenSubKey(RunRegistryKey, true);
                    if (runKey != null)
                    {
                        string locStr = isSystem ? "System" : "User";
                        disabledKey.SetValue(name, $"{locStr}|{command}");
                        runKey.DeleteValue(name, false);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error toggling startup item: " + ex.Message);
            }
            return false;
        }

        public bool RemoveStartupItem(string name, string location)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            try
            {
                // Remove from both Run keys and Disabled key to ensure it is completely gone
                using (var disabledKey = Registry.CurrentUser.OpenSubKey(DisabledKeyPath, true))
                {
                    disabledKey?.DeleteValue(name, false);
                }

                bool isSystem = (location == LocalizationService.Instance.StartupLocationSystem);
                if (isSystem)
                {
                    using var key = Registry.LocalMachine.OpenSubKey(RunRegistryKey, true);
                    if (key != null)
                    {
                        key.DeleteValue(name, false);
                        return true;
                    }
                }
                else
                {
                    using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                    if (key != null)
                    {
                        key.DeleteValue(name, false);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error removing registry startup item: " + ex.Message);
            }
            return false;
        }
    }
}
