using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SystemHub.Services
{
    public static class TweaksService
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const uint LWA_ALPHA = 0x2;

        public static bool IsClassicMenuEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"))
                {
                    return key != null;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void SetClassicMenu(bool enable)
        {
            try
            {
                if (enable)
                {
                    using (var clsid = Registry.CurrentUser.CreateSubKey(@"Software\Classes\CLSID"))
                    using (var guidKey = clsid.CreateSubKey(@"{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}"))
                    using (var inproc = guidKey.CreateSubKey("InprocServer32"))
                    {
                        inproc.SetValue("", ""); // Set default value to empty string
                    }
                }
                else
                {
                    try
                    {
                        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}", false);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SetClassicMenu Error: " + ex.Message);
            }
        }

        public static bool IsRemoveArrowsEnabled()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("29");
                        return val != null;
                    }
                }
            }
            catch { }
            return false;
        }

        public static void SetRemoveArrows(bool remove)
        {
            try
            {
                if (remove)
                {
                    using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons"))
                    {
                        // 50 index in shell32.dll points to a blank icon on modern Windows
                        key.SetValue("29", "%windir%\\System32\\shell32.dll,50");
                    }
                }
                else
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons", true))
                    {
                        if (key != null)
                        {
                            key.DeleteValue("29", false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SetRemoveArrows Error: " + ex.Message);
            }
        }

        public static bool IsWindowsUpdatesDisabled()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("NoAutoUpdate");
                        return val != null && Convert.ToInt32(val) == 1;
                    }
                }
            }
            catch { }
            return false;
        }

        public static void SetWindowsUpdatesDisabled(bool disable)
        {
            try
            {
                if (disable)
                {
                    using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU"))
                    {
                        key.SetValue("NoAutoUpdate", 1, RegistryValueKind.DWord);
                    }
                    
                    // Stop & disable service wuauserv
                    RunCommand("sc.exe", "config wuauserv start= disabled");
                    RunCommand("net.exe", "stop wuauserv");
                }
                else
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", true))
                    {
                        if (key != null)
                        {
                            key.DeleteValue("NoAutoUpdate", false);
                        }
                    }
                    
                    // Restore & start service wuauserv
                    RunCommand("sc.exe", "config wuauserv start= demand");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SetWindowsUpdates Error: " + ex.Message);
            }
        }

        public static string GetSystemFont()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\FontSubstitutes"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("Segoe UI");
                        if (val != null) return val.ToString() ?? "Segoe UI";
                    }
                }
            }
            catch { }
            return "Segoe UI";
        }

        public static void SetSystemFont(string fontName)
        {
            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\FontSubstitutes"))
                {
                    if (fontName == "Segoe UI" || string.IsNullOrEmpty(fontName))
                    {
                        key.DeleteValue("Segoe UI", false);
                        key.DeleteValue("Segoe UI Light", false);
                        key.DeleteValue("Segoe UI Semibold", false);
                        key.DeleteValue("Segoe UI Symbol", false);
                    }
                    else
                    {
                        key.SetValue("Segoe UI", fontName);
                        key.SetValue("Segoe UI Light", fontName);
                        key.SetValue("Segoe UI Semibold", fontName);
                        key.SetValue("Segoe UI Symbol", fontName);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SetSystemFont Error: " + ex.Message);
            }
        }

        public static string GetFontFamilyName(string filePath)
        {
            try
            {
                using (var fs = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                using (var br = new System.IO.BinaryReader(fs))
                {
                    fs.Position = 4;
                    ushort numTables = ReadUShortBE(br);
                    fs.Position = 12;

                    for (int i = 0; i < numTables; i++)
                    {
                        char[] tag = br.ReadChars(4);
                        uint checksum = ReadUIntBE(br);
                        uint offset = ReadUIntBE(br);
                        uint length = ReadUIntBE(br);

                        if (new string(tag) == "name")
                        {
                            fs.Position = offset;
                            ushort format = ReadUShortBE(br);
                            ushort count = ReadUShortBE(br);
                            ushort stringOffset = ReadUShortBE(br);

                            for (int j = 0; j < count; j++)
                            {
                                ushort platformId = ReadUShortBE(br);
                                ushort encodingId = ReadUShortBE(br);
                                ushort languageId = ReadUShortBE(br);
                                ushort nameId = ReadUShortBE(br);
                                ushort lengthStr = ReadUShortBE(br);
                                ushort offsetStr = ReadUShortBE(br);

                                if (nameId == 1) // Font Family Name
                                {
                                    long savedPos = fs.Position;
                                    fs.Position = offset + stringOffset + offsetStr;
                                    byte[] stringBytes = br.ReadBytes(lengthStr);
                                    fs.Position = savedPos;

                                    if (platformId == 3 && encodingId == 1)
                                    {
                                        return System.Text.Encoding.BigEndianUnicode.GetString(stringBytes);
                                    }
                                    else if (platformId == 1)
                                    {
                                        return System.Text.Encoding.ASCII.GetString(stringBytes);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetFontFamilyName Error: " + ex.Message);
            }
            return System.IO.Path.GetFileNameWithoutExtension(filePath);
        }

        private static ushort ReadUShortBE(System.IO.BinaryReader br)
        {
            byte[] bytes = br.ReadBytes(2);
            if (bytes.Length < 2) return 0;
            return (ushort)((bytes[0] << 8) | bytes[1]);
        }

        private static uint ReadUIntBE(System.IO.BinaryReader br)
        {
            byte[] bytes = br.ReadBytes(4);
            if (bytes.Length < 4) return 0;
            return (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
        }

        public static void ApplyCustomFont(string fontPath)
        {
            string fontName = GetFontFamilyName(fontPath);
            string fileName = System.IO.Path.GetFileName(fontPath);
            string destPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts", fileName);

            try
            {
                if (!System.IO.File.Exists(destPath))
                {
                    System.IO.File.Copy(fontPath, destPath, true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Copy font file Error: " + ex.Message);
            }

            // Register the custom font in registry
            using (var fontsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts", true))
            {
                if (fontsKey != null)
                {
                    fontsKey.SetValue($"{fontName} (TrueType)", fileName);
                    
                    // Blank out default Segoe UI fonts
                    fontsKey.SetValue("Segoe UI (TrueType)", "");
                    fontsKey.SetValue("Segoe UI Bold (TrueType)", "");
                    fontsKey.SetValue("Segoe UI Bold Italic (TrueType)", "");
                    fontsKey.SetValue("Segoe UI Italic (TrueType)", "");
                    fontsKey.SetValue("Segoe UI Light (TrueType)", "");
                    fontsKey.SetValue("Segoe UI Semibold (TrueType)", "");
                    fontsKey.SetValue("Segoe UI Symbol (TrueType)", "");
                }
            }

            // Add font substitution
            using (var substitutesKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\FontSubstitutes", true))
            {
                if (substitutesKey != null)
                {
                    substitutesKey.SetValue("Segoe UI", fontName);
                }
            }
        }

        public static void RestoreDefaultSegoeFont()
        {
            using (var fontsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts", true))
            {
                if (fontsKey != null)
                {
                    fontsKey.SetValue("Segoe UI (TrueType)", "segoeui.ttf");
                    fontsKey.SetValue("Segoe UI Black (TrueType)", "seguibl.ttf");
                    fontsKey.SetValue("Segoe UI Black Italic (TrueType)", "seguibli.ttf");
                    fontsKey.SetValue("Segoe UI Bold (TrueType)", "segoeuib.ttf");
                    fontsKey.SetValue("Segoe UI Bold Italic (TrueType)", "segoeuiz.ttf");
                    fontsKey.SetValue("Segoe UI Emoji (TrueType)", "seguiemj.ttf");
                    fontsKey.SetValue("Segoe UI Historic (TrueType)", "seguihis.ttf");
                    fontsKey.SetValue("Segoe UI Italic (TrueType)", "segoeuii.ttf");
                    fontsKey.SetValue("Segoe UI Light (TrueType)", "segoeuil.ttf");
                    fontsKey.SetValue("Segoe UI Light Italic (TrueType)", "seguili.ttf");
                    fontsKey.SetValue("Segoe UI Semibold (TrueType)", "seguisb.ttf");
                    fontsKey.SetValue("Segoe UI Semibold Italic (TrueType)", "seguisbi.ttf");
                    fontsKey.SetValue("Segoe UI Semilight (TrueType)", "segoeuisl.ttf");
                    fontsKey.SetValue("Segoe UI Semilight Italic (TrueType)", "seguisli.ttf");
                    fontsKey.SetValue("Segoe UI Symbol (TrueType)", "seguisym.ttf");
                    fontsKey.SetValue("Segoe MDL2 Assets (TrueType)", "segmdl2.ttf");
                    fontsKey.SetValue("Segoe Print (TrueType)", "segoepr.ttf");
                    fontsKey.SetValue("Segoe Print Bold (TrueType)", "segoeprb.ttf");
                    fontsKey.SetValue("Segoe Script (TrueType)", "segoesc.ttf");
                    fontsKey.SetValue("Segoe Script Bold (TrueType)", "segoescb.ttf");
                }
            }

            using (var substitutesKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\FontSubstitutes", true))
            {
                if (substitutesKey != null)
                {
                    try
                    {
                        substitutesKey.DeleteValue("Segoe UI", false);
                    }
                    catch { }
                }
            }
        }

        public static void RestartComputer()
        {
            try
            {
                var psi = new ProcessStartInfo("shutdown.exe", "/r /t 0 /f")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RestartComputer Error: " + ex.Message);
            }
        }

        public static void ApplyTaskbarTransparency(bool transparent, byte alpha = 0)
        {
            try
            {
                IntPtr hwnd = FindWindow("Shell_TrayWnd", null);
                if (hwnd != IntPtr.Zero)
                {
                    int wl = GetWindowLong(hwnd, GWL_EXSTYLE);
                    if (transparent)
                    {
                        SetWindowLong(hwnd, GWL_EXSTYLE, wl | WS_EX_LAYERED);
                        SetLayeredWindowAttributes(hwnd, 0, alpha, LWA_ALPHA);
                    }
                    else
                    {
                        SetWindowLong(hwnd, GWL_EXSTYLE, wl & ~WS_EX_LAYERED);
                    }
                }

                IntPtr hwndSec = FindWindow("Shell_SecondaryTrayWnd", null);
                if (hwndSec != IntPtr.Zero)
                {
                    int wl = GetWindowLong(hwndSec, GWL_EXSTYLE);
                    if (transparent)
                    {
                        SetWindowLong(hwndSec, GWL_EXSTYLE, wl | WS_EX_LAYERED);
                        SetLayeredWindowAttributes(hwndSec, 0, alpha, LWA_ALPHA);
                    }
                    else
                    {
                        SetWindowLong(hwndSec, GWL_EXSTYLE, wl & ~WS_EX_LAYERED);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ApplyTaskbarTransparency Error: " + ex.Message);
            }
        }

        public static void RestartExplorer()
        {
            try
            {
                foreach (var proc in Process.GetProcessesByName("explorer"))
                {
                    try { proc.Kill(); proc.WaitForExit(3000); } catch { }
                }
                Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RestartExplorer Error: " + ex.Message);
            }
        }

        private static void RunCommand(string file, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    Verb = "runas"
                };
                using (var proc = Process.Start(psi))
                {
                    proc?.WaitForExit(3000);
                }
            }
            catch { }
        }

        public static bool GetGlobalDarkMode()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("AppsUseLightTheme");
                        if (val != null) return Convert.ToInt32(val) == 0;
                    }
                }
            }
            catch { }
            return false;
        }

        public static void SetGlobalDarkMode(bool enable)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", true))
                {
                    if (key != null)
                    {
                        key.SetValue("AppsUseLightTheme", enable ? 0 : 1, RegistryValueKind.DWord);
                        key.SetValue("SystemUsesLightTheme", enable ? 0 : 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

        public static bool IsAdministrator()
        {
            try
            {
                using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
                {
                    var principal = new System.Security.Principal.WindowsPrincipal(identity);
                    return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool AreWindowsSoundsEnabled()
        {
            try
            {
                using (var appsKey = Registry.CurrentUser.OpenSubKey(@"AppEvents\Schemes\Apps"))
                {
                    if (appsKey == null) return true;
                    foreach (var appName in appsKey.GetSubKeyNames())
                    {
                        using (var appKey = appsKey.OpenSubKey(appName))
                        {
                            if (appKey == null) continue;
                            foreach (var eventName in appKey.GetSubKeyNames())
                            {
                                using (var eventKey = appKey.OpenSubKey(eventName))
                                {
                                    if (eventKey == null) continue;
                                    using (var currentKey = eventKey.OpenSubKey(".Current"))
                                    {
                                        if (currentKey != null)
                                        {
                                            var val = currentKey.GetValue("");
                                            if (val != null && !string.IsNullOrEmpty(val.ToString()))
                                            {
                                                return true; // Found a sound configured
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        public static void SetWindowsSounds(bool enable)
        {
            try
            {
                using (var appsKey = Registry.CurrentUser.OpenSubKey(@"AppEvents\Schemes\Apps", true))
                {
                    if (appsKey == null) return;
                    foreach (var appName in appsKey.GetSubKeyNames())
                    {
                        using (var appKey = appsKey.OpenSubKey(appName, true))
                        {
                            if (appKey == null) continue;
                            foreach (var eventName in appKey.GetSubKeyNames())
                            {
                                using (var eventKey = appKey.OpenSubKey(eventName, true))
                                {
                                    if (eventKey == null) continue;
                                    using (var currentKey = eventKey.CreateSubKey(".Current", true))
                                    {
                                        if (enable)
                                        {
                                            using (var defaultKey = eventKey.OpenSubKey(".Default"))
                                            {
                                                if (defaultKey != null)
                                                {
                                                    var defaultVal = defaultKey.GetValue("");
                                                    if (defaultVal != null)
                                                    {
                                                        currentKey.SetValue("", defaultVal);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            currentKey.SetValue("", "");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SetWindowsSounds Error: " + ex.Message);
            }
        }
    }
}

