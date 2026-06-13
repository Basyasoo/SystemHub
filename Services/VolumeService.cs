using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;


namespace MacStyleHub.Services
{
    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    public class MMDeviceEnumerator { }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDeviceEnumerator
    {
        [PreserveSig]
        int EnumAudioEndpoints(int dataFlow, int dwStateMask, out IntPtr ppDevices);
        [PreserveSig]
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppDevice);
        [PreserveSig]
        int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);
        [PreserveSig]
        int RegisterEndpointNotificationCallback(IntPtr pClient);
        [PreserveSig]
        int UnregisterEndpointNotificationCallback(IntPtr pClient);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, int dwClsContext, IntPtr pActivationParams, out IntPtr ppInterface);
        [PreserveSig]
        int OpenPropertyStore(int stgmAccess, out IntPtr ppProperties);
        [PreserveSig]
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
        [PreserveSig]
        int GetState(out int pdwState);
    }

    [ComImport]
    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioSessionManager2
    {
        [PreserveSig]
        int GetAudioSessionControl(IntPtr AudioSessionGuid, uint StreamFlags, IntPtr SessionControl);
        [PreserveSig]
        int GetSimpleAudioVolume(IntPtr AudioSessionGuid, uint StreamFlags, IntPtr AudioVolume);
        [PreserveSig]
        int GetSessionEnumerator(out IAudioSessionEnumerator SessionList);
    }

    [ComImport]
    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioSessionEnumerator
    {
        [PreserveSig]
        int GetCount(out int SessionCount);
        [PreserveSig]
        int GetSession(int SessionCount, out IntPtr Session);
    }

    [ComImport]
    [Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioSessionControl
    {
        [PreserveSig]
        int GetState(out AudioSessionState pRetVal);
        [PreserveSig]
        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
        [PreserveSig]
        int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, ref Guid EventContext);
        [PreserveSig]
        int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
        [PreserveSig]
        int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, ref Guid EventContext);
        [PreserveSig]
        int GetGroupingParam(out Guid pRetVal);
        [PreserveSig]
        int SetGroupingParam(ref Guid Override, ref Guid EventContext);
        [PreserveSig]
        int RegisterAudioSessionNotification(IntPtr Client);
        [PreserveSig]
        int UnregisterAudioSessionNotification(IntPtr Client);
    }

    [ComImport]
    [Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioSessionControl2
    {
        [PreserveSig]
        int GetState(out AudioSessionState pRetVal);
        [PreserveSig]
        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
        [PreserveSig]
        int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, ref Guid EventContext);
        [PreserveSig]
        int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
        [PreserveSig]
        int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, ref Guid EventContext);
        [PreserveSig]
        int GetGroupingParam(out Guid pRetVal);
        [PreserveSig]
        int SetGroupingParam(ref Guid Override, ref Guid EventContext);
        [PreserveSig]
        int RegisterAudioSessionNotification(IntPtr Client);
        [PreserveSig]
        int UnregisterAudioSessionNotification(IntPtr Client);

        [PreserveSig]
        int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
        [PreserveSig]
        int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
        [PreserveSig]
        int GetProcessId(out uint pRetVal);
        [PreserveSig]
        int IsSystemSoundsSession();
        [PreserveSig]
        int SetDuckingPreference(bool optOut);
    }

    [ComImport]
    [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ISimpleAudioVolume
    {
        [PreserveSig]
        int SetMasterVolume(float fLevel, ref Guid EventContext);
        [PreserveSig]
        int GetMasterVolume(out float pfLevel);
        [PreserveSig]
        int SetMute(bool bMute, ref Guid EventContext);
        [PreserveSig]
        int GetMute(out bool pbMute);
    }

    [ComImport]
    [Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAudioMeterInformation
    {
        [PreserveSig]
        int GetPeakValue(out float pfPeak);
        [PreserveSig]
        int GetChannelsPeakValues(uint u32ChannelCount, [Out] float[] afPeakValues);
        [PreserveSig]
        int QueryHardwareSupport(out uint pdwHardwareSupportMask);
    }

    public enum AudioSessionState
    {
        AudioSessionStateInactive = 0,
        AudioSessionStateActive = 1,
        AudioSessionStateExpired = 2
    }

    public class AppAudioSession
    {
        public string ProcessName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public uint ProcessId { get; set; }
        public float Volume { get; set; }
        public bool IsMuted { get; set; }
        public bool IsSystemSounds { get; set; }
        public string SessionInstanceId { get; set; } = "";
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate int GetDefaultAudioEndpointDelegate(IntPtr self, int dataFlow, int role, out IntPtr ppDevice);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate int ActivateDelegate(IntPtr self, ref Guid iid, int dwClsContext, IntPtr pActivationParams, out IntPtr ppInterface);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate int SetMasterVolumeLevelScalarDelegate(IntPtr self, float level, ref Guid eventContext);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate int GetMasterVolumeLevelScalarDelegate(IntPtr self, out float level);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate int SetMuteDelegate(IntPtr self, bool mute, ref Guid eventContext);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate int GetMuteDelegate(IntPtr self, out bool mute);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate int EnumAudioEndpointsDelegate(IntPtr self, int dataFlow, int dwStateMask, out IntPtr ppDevices);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate int GetCountDelegate(IntPtr self, out int count);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate int ItemDelegate(IntPtr self, int index, out IntPtr ppDevice);

    public static class VolumeService
    {
        private static IAudioMeterInformation? _cachedMeter;
        private static IntPtr _cachedMeterPtr;

        public static float GetAudioPeak()
        {
            try
            {
                if (_cachedMeter == null)
                {
                    Type? enumeratorType = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"));
                    object? enumerator = enumeratorType != null ? Activator.CreateInstance(enumeratorType) : null;
                    if (enumerator == null) return 0f;
                    IntPtr pEnumerator = Marshal.GetIUnknownForObject(enumerator);
                    if (pEnumerator == IntPtr.Zero) return 0f;

                    IntPtr vtable = Marshal.ReadIntPtr(pEnumerator);
                    IntPtr pGetDefaultAudioEndpoint = Marshal.ReadIntPtr(vtable, 4 * IntPtr.Size);
                    var getDefaultAudioEndpoint = Marshal.GetDelegateForFunctionPointer<GetDefaultAudioEndpointDelegate>(pGetDefaultAudioEndpoint);

                    int hr = getDefaultAudioEndpoint(pEnumerator, 0, 0, out IntPtr pDevice);
                    Marshal.Release(pEnumerator);
                    Marshal.ReleaseComObject(enumerator);

                    if (hr != 0 || pDevice == IntPtr.Zero) return 0f;

                    IntPtr deviceVtable = Marshal.ReadIntPtr(pDevice);
                    IntPtr pActivate = Marshal.ReadIntPtr(deviceVtable, 3 * IntPtr.Size);
                    var activate = Marshal.GetDelegateForFunctionPointer<ActivateDelegate>(pActivate);

                    var iid = new Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"); // IID_IAudioMeterInformation
                    hr = activate(pDevice, ref iid, 1, IntPtr.Zero, out IntPtr pMeter);
                    Marshal.Release(pDevice);

                    if (hr != 0 || pMeter == IntPtr.Zero) return 0f;

                    var meterObj = Marshal.GetObjectForIUnknown(pMeter) as IAudioMeterInformation;
                    if (meterObj == null)
                    {
                        Marshal.Release(pMeter);
                        return 0f;
                    }

                    _cachedMeter = meterObj;
                    _cachedMeterPtr = pMeter;
                }

                float peak = 0f;
                int hrPeak = _cachedMeter.GetPeakValue(out peak);
                if (hrPeak != 0) // Call failed (device disconnected or changed)
                {
                    // Reset cache
                    if (_cachedMeter != null)
                    {
                        Marshal.ReleaseComObject(_cachedMeter);
                        _cachedMeter = null;
                    }
                    if (_cachedMeterPtr != IntPtr.Zero)
                    {
                        Marshal.Release(_cachedMeterPtr);
                        _cachedMeterPtr = IntPtr.Zero;
                    }
                    return 0f;
                }

                return peak;
            }
            catch
            {
                // Reset cache on exception
                if (_cachedMeter != null)
                {
                    try { Marshal.ReleaseComObject(_cachedMeter); } catch {}
                    _cachedMeter = null;
                }
                if (_cachedMeterPtr != IntPtr.Zero)
                {
                    try { Marshal.Release(_cachedMeterPtr); } catch {}
                    _cachedMeterPtr = IntPtr.Zero;
                }
                return 0f;
            }
        }

        private static readonly Dictionary<uint, (string Name, DateTime CachedTime)> _processNameCache = new();
        private static readonly object _cacheLock = new();

        private static string GetProcessNameCached(uint pid)
        {
            if (pid == 0) return "Unknown Application";
            lock (_cacheLock)
            {
                if (_processNameCache.TryGetValue(pid, out var cached) && (DateTime.UtcNow - cached.CachedTime).TotalSeconds < 5)
                {
                    return cached.Name;
                }

                string name = "Unknown Application";
                try
                {
                    using var proc = System.Diagnostics.Process.GetProcessById((int)pid);
                    name = proc.ProcessName;
                }
                catch
                {
                    if (cached.Name != null) name = cached.Name;
                }

                _processNameCache[pid] = (name, DateTime.UtcNow);
                return name;
            }
        }

        public static float GetAudioPeakForProcess(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return 0f;
            float maxPeak = 0f;
            var managers = GetSessionManagers();

            foreach (IntPtr pSessionManager in managers)
            {
                IAudioSessionManager2 manager = null;
                IAudioSessionEnumerator sessionEnumerator = null;
                try
                {
                    var obj = Marshal.GetObjectForIUnknown(pSessionManager);
                    manager = obj as IAudioSessionManager2;
                    if (manager == null) continue;

                    int hr = manager.GetSessionEnumerator(out sessionEnumerator);
                    if (hr != 0 || sessionEnumerator == null) continue;

                    sessionEnumerator.GetCount(out int count);
                    for (int i = 0; i < count; i++)
                    {
                        IntPtr pSession = IntPtr.Zero;
                        IAudioSessionControl2? sessionControl2 = null;
                        try
                        {
                            hr = sessionEnumerator.GetSession(i, out pSession);
                            if (hr != 0 || pSession == IntPtr.Zero) continue;

                            sessionControl2 = GetSessionControl2(pSession);
                            if (sessionControl2 == null) continue;

                            sessionControl2.GetState(out AudioSessionState state);
                            if (state == AudioSessionState.AudioSessionStateExpired) continue;

                            bool isSystemSounds = sessionControl2.IsSystemSoundsSession() == 0;
                            string name = "";
                            if (isSystemSounds)
                            {
                                name = "System Sounds";
                            }
                            else
                            {
                                uint pid = 0;
                                sessionControl2.GetProcessId(out pid);
                                name = GetProcessNameCached(pid);
                            }

                            if (string.Equals(name, processName, StringComparison.OrdinalIgnoreCase))
                            {
                                Guid iidMeter = new Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064");
                                hr = Marshal.QueryInterface(pSession, ref iidMeter, out IntPtr pMeter);
                                if (hr == 0 && pMeter != IntPtr.Zero)
                                {
                                    try
                                    {
                                        var meter = Marshal.GetObjectForIUnknown(pMeter) as IAudioMeterInformation;
                                        if (meter != null)
                                        {
                                            meter.GetPeakValue(out float peak);
                                            if (peak > maxPeak) maxPeak = peak;
                                            Marshal.ReleaseComObject(meter);
                                        }
                                    }
                                    finally
                                    {
                                        Marshal.Release(pMeter);
                                    }
                                }
                            }
                        }
                        finally
                        {
                            if (sessionControl2 != null) Marshal.ReleaseComObject(sessionControl2);
                            if (pSession != IntPtr.Zero) Marshal.Release(pSession);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GetAudioPeakForProcess exception: {ex}");
                }
                finally
                {
                    if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                    if (manager != null) Marshal.ReleaseComObject(manager);
                    Marshal.Release(pSessionManager);
                }
            }

            return maxPeak;
        }

        private static IntPtr GetVolumeControl()
        {
            try
            {
                Type? enumeratorType = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"));
                object? enumerator = enumeratorType != null ? Activator.CreateInstance(enumeratorType) : null;
                if (enumerator == null) return IntPtr.Zero;
                IntPtr pEnumerator = Marshal.GetIUnknownForObject(enumerator);
                if (pEnumerator == IntPtr.Zero) return IntPtr.Zero;

                IntPtr vtable = Marshal.ReadIntPtr(pEnumerator);
                IntPtr pGetDefaultAudioEndpoint = Marshal.ReadIntPtr(vtable, 4 * IntPtr.Size);
                var getDefaultAudioEndpoint = Marshal.GetDelegateForFunctionPointer<GetDefaultAudioEndpointDelegate>(pGetDefaultAudioEndpoint);

                int hr = getDefaultAudioEndpoint(pEnumerator, 0, 0, out IntPtr pDevice);
                Marshal.Release(pEnumerator);
                Marshal.ReleaseComObject(enumerator);

                if (hr != 0 || pDevice == IntPtr.Zero)
                {
                    Console.WriteLine($"GetDefaultAudioEndpoint failed: hr={hr}");
                    return IntPtr.Zero;
                }

                IntPtr deviceVtable = Marshal.ReadIntPtr(pDevice);
                IntPtr pActivate = Marshal.ReadIntPtr(deviceVtable, 3 * IntPtr.Size);
                var activate = Marshal.GetDelegateForFunctionPointer<ActivateDelegate>(pActivate);

                var iid = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");
                hr = activate(pDevice, ref iid, 1, IntPtr.Zero, out IntPtr pVolume);
                Marshal.Release(pDevice);

                if (hr != 0 || pVolume == IntPtr.Zero)
                {
                    Console.WriteLine($"device.Activate failed: hr={hr}");
                    return IntPtr.Zero;
                }

                return pVolume;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetVolumeControl exception: {ex}");
                return IntPtr.Zero;
            }
        }

        public static float GetVolume()
        {
            IntPtr volume = GetVolumeControl();
            if (volume == IntPtr.Zero) return 0f;
            try
            {
                IntPtr vtable = Marshal.ReadIntPtr(volume);
                IntPtr pGetVolume = Marshal.ReadIntPtr(vtable, 9 * IntPtr.Size);
                var getVolume = Marshal.GetDelegateForFunctionPointer<GetMasterVolumeLevelScalarDelegate>(pGetVolume);
                int hr = getVolume(volume, out float val);
                return hr == 0 ? val * 100f : 0f;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetVolume exception: {ex}");
                return 0f;
            }
            finally
            {
                Marshal.Release(volume);
            }
        }

        public static void SetVolume(float val)
        {
            IntPtr volume = GetVolumeControl();
            if (volume == IntPtr.Zero) return;
            try
            {
                IntPtr vtable = Marshal.ReadIntPtr(volume);
                IntPtr pSetVolume = Marshal.ReadIntPtr(vtable, 7 * IntPtr.Size);
                var setVolume = Marshal.GetDelegateForFunctionPointer<SetMasterVolumeLevelScalarDelegate>(pSetVolume);
                var guid = Guid.Empty;
                setVolume(volume, val / 100f, ref guid);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetVolume exception: {ex}");
            }
            finally
            {
                Marshal.Release(volume);
            }
        }

        public static bool GetMute()
        {
            IntPtr volume = GetVolumeControl();
            if (volume == IntPtr.Zero) return false;
            try
            {
                IntPtr vtable = Marshal.ReadIntPtr(volume);
                IntPtr pGetMute = Marshal.ReadIntPtr(vtable, 11 * IntPtr.Size);
                var getMute = Marshal.GetDelegateForFunctionPointer<GetMuteDelegate>(pGetMute);
                int hr = getMute(volume, out bool mute);
                return hr == 0 && mute;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetMute exception: {ex}");
                return false;
            }
            finally
            {
                Marshal.Release(volume);
            }
        }

        public static void SetMute(bool mute)
        {
            IntPtr volume = GetVolumeControl();
            if (volume == IntPtr.Zero) return;
            try
            {
                IntPtr vtable = Marshal.ReadIntPtr(volume);
                IntPtr pSetMute = Marshal.ReadIntPtr(vtable, 10 * IntPtr.Size);
                var setMute = Marshal.GetDelegateForFunctionPointer<SetMuteDelegate>(pSetMute);
                var guid = Guid.Empty;
                setMute(volume, mute, ref guid);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SetMute exception: {ex}");
            }
            finally
            {
                Marshal.Release(volume);
            }
        }

        private static IntPtr GetSessionManager()
        {
            try
            {
                Type? enumeratorType = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"));
                object? enumerator = enumeratorType != null ? Activator.CreateInstance(enumeratorType) : null;
                if (enumerator == null) return IntPtr.Zero;
                IntPtr pEnumerator = Marshal.GetIUnknownForObject(enumerator);
                if (pEnumerator == IntPtr.Zero) return IntPtr.Zero;

                IntPtr vtable = Marshal.ReadIntPtr(pEnumerator);
                IntPtr pGetDefaultAudioEndpoint = Marshal.ReadIntPtr(vtable, 4 * IntPtr.Size);
                var getDefaultAudioEndpoint = Marshal.GetDelegateForFunctionPointer<GetDefaultAudioEndpointDelegate>(pGetDefaultAudioEndpoint);

                int hr = getDefaultAudioEndpoint(pEnumerator, 0, 0, out IntPtr pDevice);
                Marshal.Release(pEnumerator);
                Marshal.ReleaseComObject(enumerator);

                if (hr != 0 || pDevice == IntPtr.Zero)
                {
                    Console.WriteLine($"GetDefaultAudioEndpoint failed: hr={hr}");
                    return IntPtr.Zero;
                }

                IntPtr deviceVtable = Marshal.ReadIntPtr(pDevice);
                IntPtr pActivate = Marshal.ReadIntPtr(deviceVtable, 3 * IntPtr.Size);
                var activate = Marshal.GetDelegateForFunctionPointer<ActivateDelegate>(pActivate);

                var iid = new Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"); // IID_IAudioSessionManager2
                hr = activate(pDevice, ref iid, 1, IntPtr.Zero, out IntPtr pSessionManager);
                Marshal.Release(pDevice);

                if (hr != 0 || pSessionManager == IntPtr.Zero)
                {
                    Console.WriteLine($"device.Activate (SessionManager) failed: hr=0x{hr:X}");
                    return IntPtr.Zero;
                }

                return pSessionManager;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetSessionManager exception: {ex}");
                return IntPtr.Zero;
            }
        }

        private static IAudioSessionControl2 GetSessionControl2(IntPtr pSession)
        {
            if (pSession == IntPtr.Zero) return null;
            Guid iidControl2 = new Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d");
            int hr = Marshal.QueryInterface(pSession, ref iidControl2, out IntPtr pControl2);
            if (hr == 0 && pControl2 != IntPtr.Zero)
            {
                try
                {
                    var obj = Marshal.GetObjectForIUnknown(pControl2);
                    var casted = obj as IAudioSessionControl2;
                    return casted;
                }
                catch
                {
                }
                finally
                {
                    Marshal.Release(pControl2);
                }
            }
            return null;
        }

        private static ISimpleAudioVolume GetSimpleAudioVolume(IntPtr pSession)
        {
            if (pSession == IntPtr.Zero) return null;
            Guid iidVolume = new Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8");
            int hr = Marshal.QueryInterface(pSession, ref iidVolume, out IntPtr pVolume);
            if (hr == 0 && pVolume != IntPtr.Zero)
            {
                try
                {
                    var obj = Marshal.GetObjectForIUnknown(pVolume);
                    var casted = obj as ISimpleAudioVolume;
                    return casted;
                }
                catch
                {
                }
                finally
                {
                    Marshal.Release(pVolume);
                }
            }
            return null;
        }

        private static List<IntPtr> GetSessionManagers()
        {
            var managers = new List<IntPtr>();
            try
            {
                Type? enumeratorType = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"));
                object? enumerator = enumeratorType != null ? Activator.CreateInstance(enumeratorType) : null;
                if (enumerator == null) return managers;
                IntPtr pEnumerator = Marshal.GetIUnknownForObject(enumerator);
                if (pEnumerator == IntPtr.Zero) return managers;

                IntPtr vtable = Marshal.ReadIntPtr(pEnumerator);
                IntPtr pEnumAudioEndpoints = Marshal.ReadIntPtr(vtable, 3 * IntPtr.Size);
                var enumAudioEndpoints = Marshal.GetDelegateForFunctionPointer<EnumAudioEndpointsDelegate>(pEnumAudioEndpoints);

                int hr = enumAudioEndpoints(pEnumerator, 0, 1, out IntPtr pCollection);
                Marshal.Release(pEnumerator);
                Marshal.ReleaseComObject(enumerator);

                if (hr != 0 || pCollection == IntPtr.Zero) return managers;

                IntPtr colVtable = Marshal.ReadIntPtr(pCollection);
                IntPtr pGetCount = Marshal.ReadIntPtr(colVtable, 3 * IntPtr.Size);
                var getCount = Marshal.GetDelegateForFunctionPointer<GetCountDelegate>(pGetCount);

                IntPtr pItem = Marshal.ReadIntPtr(colVtable, 4 * IntPtr.Size);
                var item = Marshal.GetDelegateForFunctionPointer<ItemDelegate>(pItem);

                hr = getCount(pCollection, out int count);
                if (hr == 0)
                {
                    var iid = new Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F");
                    for (int i = 0; i < count; i++)
                    {
                        hr = item(pCollection, i, out IntPtr pDevice);
                        if (hr == 0 && pDevice != IntPtr.Zero)
                        {
                            IntPtr deviceVtable = Marshal.ReadIntPtr(pDevice);
                            IntPtr pActivate = Marshal.ReadIntPtr(deviceVtable, 3 * IntPtr.Size);
                            var activate = Marshal.GetDelegateForFunctionPointer<ActivateDelegate>(pActivate);

                            hr = activate(pDevice, ref iid, 1, IntPtr.Zero, out IntPtr pSessionManager);
                            Marshal.Release(pDevice);

                            if (hr == 0 && pSessionManager != IntPtr.Zero)
                            {
                                managers.Add(pSessionManager);
                            }
                        }
                    }
                }
                Marshal.Release(pCollection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetSessionManagers exception: {ex}");
            }

            if (managers.Count == 0)
            {
                IntPtr pDefault = GetSessionManager();
                if (pDefault != IntPtr.Zero)
                {
                    managers.Add(pDefault);
                }
            }

            return managers;
        }

        public static List<AppAudioSession> GetAppAudioSessions()
        {
            var sessions = new List<AppAudioSession>();
            var managers = GetSessionManagers();

            foreach (IntPtr pSessionManager in managers)
            {
                IAudioSessionManager2 manager = null;
                IAudioSessionEnumerator sessionEnumerator = null;
                try
                {
                    var obj = Marshal.GetObjectForIUnknown(pSessionManager);
                    manager = obj as IAudioSessionManager2;
                    if (manager == null) continue;

                    int hr = manager.GetSessionEnumerator(out sessionEnumerator);
                    if (hr != 0 || sessionEnumerator == null) continue;

                    sessionEnumerator.GetCount(out int count);
                    for (int i = 0; i < count; i++)
                    {
                        IntPtr pSession = IntPtr.Zero;
                        IAudioSessionControl2? sessionControl2 = null;
                        ISimpleAudioVolume? volume = null;
                        try
                        {
                            hr = sessionEnumerator.GetSession(i, out pSession);
                            if (hr != 0 || pSession == IntPtr.Zero) continue;

                            sessionControl2 = GetSessionControl2(pSession);
                            if (sessionControl2 == null) continue;

                            sessionControl2.GetState(out AudioSessionState state);
                            if (state == AudioSessionState.AudioSessionStateExpired) continue;

                            sessionControl2.GetSessionInstanceIdentifier(out string instanceId);

                            // Prevent duplicates if multiple devices report the same session
                            if (sessions.Any(s => s.SessionInstanceId == instanceId)) continue;

                            bool isSystemSounds = sessionControl2.IsSystemSoundsSession() == 0;
                            uint pid = 0;
                            string name = "";
                            
                            if (isSystemSounds)
                            {
                                name = "System Sounds";
                            }
                            else
                            {
                                sessionControl2.GetProcessId(out pid);
                                name = GetProcessNameCached(pid);
                            }

                            // Get volume
                            volume = GetSimpleAudioVolume(pSession);
                            float volLevel = 0f;
                            bool isMuted = false;
                            if (volume != null)
                            {
                                volume.GetMasterVolume(out volLevel);
                                volume.GetMute(out isMuted);
                            }

                            // Capitalize process name for display
                            string displayName = name;
                            if (name.Equals("chrome", StringComparison.OrdinalIgnoreCase)) displayName = "Google Chrome";
                            else if (name.Equals("spotify", StringComparison.OrdinalIgnoreCase)) displayName = "Spotify";
                            else if (name.Equals("discord", StringComparison.OrdinalIgnoreCase)) displayName = "Discord";
                            else if (name.Equals("firefox", StringComparison.OrdinalIgnoreCase)) displayName = "Firefox";
                            else if (name.Equals("msedge", StringComparison.OrdinalIgnoreCase)) displayName = "Microsoft Edge";
                            else if (name.Equals("system sounds", StringComparison.OrdinalIgnoreCase)) displayName = "Системные звуки";

                            sessions.Add(new AppAudioSession
                            {
                                ProcessName = name,
                                DisplayName = displayName,
                                ProcessId = pid,
                                Volume = volLevel * 100f,
                                IsMuted = isMuted,
                                IsSystemSounds = isSystemSounds,
                                SessionInstanceId = instanceId
                            });
                        }
                        finally
                        {
                            if (sessionControl2 != null) Marshal.ReleaseComObject(sessionControl2);
                            if (volume != null) Marshal.ReleaseComObject(volume);
                            if (pSession != IntPtr.Zero) Marshal.Release(pSession);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GetAppAudioSessions manager exception: {ex}");
                }
                finally
                {
                    if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                    if (manager != null) Marshal.ReleaseComObject(manager);
                    Marshal.Release(pSessionManager);
                }
            }

            return sessions;
        }

        public static void SetAppSessionVolume(string instanceId, float vol)
        {
            var managers = GetSessionManagers();

            foreach (IntPtr pSessionManager in managers)
            {
                IAudioSessionManager2 manager = null;
                IAudioSessionEnumerator sessionEnumerator = null;
                try
                {
                    manager = Marshal.GetObjectForIUnknown(pSessionManager) as IAudioSessionManager2;
                    if (manager == null) continue;

                    int hr = manager.GetSessionEnumerator(out sessionEnumerator);
                    if (hr != 0) continue;

                    sessionEnumerator.GetCount(out int count);
                    bool found = false;
                    for (int i = 0; i < count; i++)
                    {
                        IntPtr pSession = IntPtr.Zero;
                        IAudioSessionControl2? sessionControl2 = null;
                        ISimpleAudioVolume? volume = null;
                        try
                        {
                            hr = sessionEnumerator.GetSession(i, out pSession);
                            if (hr != 0 || pSession == IntPtr.Zero) continue;

                            sessionControl2 = GetSessionControl2(pSession);
                            if (sessionControl2 == null) continue;

                            sessionControl2.GetSessionInstanceIdentifier(out string id);
                            if (id == instanceId)
                            {
                                volume = GetSimpleAudioVolume(pSession);
                                if (volume != null)
                                {
                                    var guid = Guid.Empty;
                                    volume.SetMasterVolume(vol / 100f, ref guid);
                                }
                                found = true;
                                break;
                            }
                        }
                        finally
                        {
                            if (sessionControl2 != null) Marshal.ReleaseComObject(sessionControl2);
                            if (volume != null) Marshal.ReleaseComObject(volume);
                            if (pSession != IntPtr.Zero) Marshal.Release(pSession);
                        }
                    }
                    if (found) break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SetAppSessionVolume exception: {ex}");
                }
                finally
                {
                    if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                    if (manager != null) Marshal.ReleaseComObject(manager);
                    Marshal.Release(pSessionManager);
                }
            }
        }

        public static void SetAppSessionMute(string instanceId, bool mute)
        {
            var managers = GetSessionManagers();

            foreach (IntPtr pSessionManager in managers)
            {
                IAudioSessionManager2 manager = null;
                IAudioSessionEnumerator sessionEnumerator = null;
                try
                {
                    manager = Marshal.GetObjectForIUnknown(pSessionManager) as IAudioSessionManager2;
                    if (manager == null) continue;

                    int hr = manager.GetSessionEnumerator(out sessionEnumerator);
                    if (hr != 0) continue;

                    sessionEnumerator.GetCount(out int count);
                    bool found = false;
                    for (int i = 0; i < count; i++)
                    {
                        IntPtr pSession = IntPtr.Zero;
                        IAudioSessionControl2? sessionControl2 = null;
                        ISimpleAudioVolume? volume = null;
                        try
                        {
                            hr = sessionEnumerator.GetSession(i, out pSession);
                            if (hr != 0 || pSession == IntPtr.Zero) continue;

                            sessionControl2 = GetSessionControl2(pSession);
                            if (sessionControl2 == null) continue;

                            sessionControl2.GetSessionInstanceIdentifier(out string id);
                            if (id == instanceId)
                            {
                                volume = GetSimpleAudioVolume(pSession);
                                if (volume != null)
                                {
                                    var guid = Guid.Empty;
                                    volume.SetMute(mute, ref guid);
                                }
                                found = true;
                                break;
                            }
                        }
                        finally
                        {
                            if (sessionControl2 != null) Marshal.ReleaseComObject(sessionControl2);
                            if (volume != null) Marshal.ReleaseComObject(volume);
                            if (pSession != IntPtr.Zero) Marshal.Release(pSession);
                        }
                    }
                    if (found) break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SetAppSessionMute exception: {ex}");
                }
                finally
                {
                    if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
                    if (manager != null) Marshal.ReleaseComObject(manager);
                    Marshal.Release(pSessionManager);
                }
            }
        }
    }
}
