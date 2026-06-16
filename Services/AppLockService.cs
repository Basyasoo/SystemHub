using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using SystemHub.Views;

namespace SystemHub.Services
{
    public class AppLockService
    {
        private static AppLockService? _instance;
        public static AppLockService Instance => _instance ??= new AppLockService();

        private bool _isAppLockEnabled;
        private string _password = "1234";
        private readonly List<string> _protectedApps = new();
        private readonly HashSet<int> _unlockedPids = new();
        private readonly HashSet<int> _activePromptPids = new();
        private bool _isMonitoring = false;

        public bool IsLocked { get; set; }

        public event Action? ProtectedAppsChanged;

        public bool IsAppLockEnabled
        {
            get => _isAppLockEnabled;
            set
            {
                if (_isAppLockEnabled == value) return;
                _isAppLockEnabled = value;
                SaveConfig();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                _password = value;
                SaveConfig();
            }
        }

        public IReadOnlyList<string> ProtectedApps => _protectedApps;

        private AppLockService()
        {
            LoadConfig();
            IsLocked = false; // Always unlocked at startup
            StartMonitoring();
        }

        public bool VerifyPassword(string input)
        {
            if (input == _password)
            {
                IsLocked = false;
                return true;
            }
            return false;
        }

        public void AddProtectedApp(string appPath)
        {
            if (string.IsNullOrWhiteSpace(appPath)) return;
            string appName = Path.GetFileNameWithoutExtension(appPath).ToLower();
            if (!_protectedApps.Contains(appName))
            {
                _protectedApps.Add(appName);
                SaveConfig();
                ProtectedAppsChanged?.Invoke();
            }
        }

        public void RemoveProtectedApp(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName)) return;
            string cleanName = appName.ToLower();
            if (_protectedApps.Remove(cleanName))
            {
                SaveConfig();
                ProtectedAppsChanged?.Invoke();
            }
        }

        private void StartMonitoring()
        {
            if (_isMonitoring) return;
            _isMonitoring = true;
            Task.Run(async () =>
            {
                while (_isMonitoring)
                {
                    try
                    {
                        MonitorProcesses();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in process monitor: {ex.Message}");
                    }
                    await Task.Delay(500);
                }
            });
        }

        private void MonitorProcesses()
        {
            // 1. Clean up exited PIDs from _unlockedPids
            lock (_unlockedPids)
            {
                var toRemove = new List<int>();
                foreach (var pid in _unlockedPids)
                {
                    try
                    {
                        using var p = Process.GetProcessById(pid);
                        if (p.HasExited)
                        {
                            toRemove.Add(pid);
                        }
                    }
                    catch
                    {
                        toRemove.Add(pid);
                    }
                }
                foreach (var pid in toRemove)
                {
                    _unlockedPids.Remove(pid);
                }
            }

            // If no apps are protected, do nothing
            List<string> currentProtected;
            lock (_protectedApps)
            {
                currentProtected = _protectedApps.ToList();
            }
            if (currentProtected.Count == 0) return;

            // 2. Scan active processes
            var processes = Process.GetProcesses();
            foreach (var p in processes)
            {
                try
                {
                    string name = p.ProcessName.ToLower();
                    if (currentProtected.Contains(name))
                    {
                        int pid = p.Id;
                        bool isUnlocked;
                        bool isActivePrompt;

                        lock (_unlockedPids)
                        {
                            isUnlocked = _unlockedPids.Contains(pid);
                        }
                        lock (_activePromptPids)
                        {
                            isActivePrompt = _activePromptPids.Contains(pid);
                        }

                        if (!isUnlocked && !isActivePrompt && !p.HasExited)
                        {
                            lock (_activePromptPids)
                            {
                                _activePromptPids.Add(pid);
                            }

                            // Suspend process immediately
                            SuspendProcess(pid);

                            // Dispatch password prompt to UI Thread
                            Dispatcher.UIThread.Post(async () =>
                            {
                                bool success = await PasswordPromptWindow.ShowPromptAsync(p.ProcessName, pid);
                                if (success)
                                {
                                    lock (_unlockedPids)
                                    {
                                        _unlockedPids.Add(pid);
                                    }
                                    ResumeProcess(pid);
                                }
                                else
                                {
                                    try
                                    {
                                        using var proc = Process.GetProcessById(pid);
                                        proc.Kill();
                                    }
                                    catch { }
                                }

                                lock (_activePromptPids)
                                {
                                    _activePromptPids.Remove(pid);
                                }
                            });
                        }
                    }
                }
                catch
                {
                    // Access denied or process exited while accessing properties
                }
                finally
                {
                    p.Dispose();
                }
            }
        }

        #region Win32 P/Invokes
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_SUSPEND_RESUME = 0x0800;

        [DllImport("ntdll.dll", EntryPoint = "NtSuspendProcess", SetLastError = true)]
        private static extern int NtSuspendProcess(IntPtr processHandle);

        [DllImport("ntdll.dll", EntryPoint = "NtResumeProcess", SetLastError = true)]
        private static extern int NtResumeProcess(IntPtr processHandle);

        private static bool SuspendProcess(int pid)
        {
            IntPtr handle = OpenProcess(PROCESS_SUSPEND_RESUME, false, pid);
            if (handle != IntPtr.Zero)
            {
                try
                {
                    int result = NtSuspendProcess(handle);
                    return result == 0;
                }
                finally
                {
                    CloseHandle(handle);
                }
            }
            return false;
        }

        private static bool ResumeProcess(int pid)
        {
            IntPtr handle = OpenProcess(PROCESS_SUSPEND_RESUME, false, pid);
            if (handle != IntPtr.Zero)
            {
                try
                {
                    int result = NtResumeProcess(handle);
                    return result == 0;
                }
                finally
                {
                    CloseHandle(handle);
                }
            }
            return false;
        }
        #endregion

        private void LoadConfig()
        {
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SystemHub");
                Directory.CreateDirectory(appData);
                string configPath = Path.Combine(appData, "applock.json");

                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<AppLockConfig>(json);
                    if (config != null)
                    {
                        _isAppLockEnabled = config.IsAppLockEnabled;
                        _password = config.Password ?? "1234";
                        if (config.ProtectedApps != null)
                        {
                            _protectedApps.Clear();
                            _protectedApps.AddRange(config.ProtectedApps);
                        }
                    }
                }
            }
            catch { }
        }

        private void SaveConfig()
        {
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SystemHub");
                Directory.CreateDirectory(appData);
                string configPath = Path.Combine(appData, "applock.json");

                string json = JsonSerializer.Serialize(new AppLockConfig
                {
                    IsAppLockEnabled = _isAppLockEnabled,
                    Password = _password,
                    ProtectedApps = _protectedApps
                });
                File.WriteAllText(configPath, json);
            }
            catch { }
        }

        private class AppLockConfig
        {
            public bool IsAppLockEnabled { get; set; }
            public string Password { get; set; } = "1234";
            public List<string>? ProtectedApps { get; set; }
        }
    }
}

