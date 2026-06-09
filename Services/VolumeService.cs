using System;
using System.Runtime.InteropServices;

namespace MacStyleHub.Services
{
    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    public class MMDeviceEnumerator { }

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

    public static class VolumeService
    {
        private static IntPtr GetVolumeControl()
        {
            try
            {
                var enumerator = new MMDeviceEnumerator();
                IntPtr pEnumerator = Marshal.GetIUnknownForObject(enumerator);
                if (pEnumerator == IntPtr.Zero) return IntPtr.Zero;

                IntPtr vtable = Marshal.ReadIntPtr(pEnumerator);
                IntPtr pGetDefaultAudioEndpoint = Marshal.ReadIntPtr(vtable, 4 * IntPtr.Size);
                var getDefaultAudioEndpoint = Marshal.GetDelegateForFunctionPointer<GetDefaultAudioEndpointDelegate>(pGetDefaultAudioEndpoint);

                int hr = getDefaultAudioEndpoint(pEnumerator, 0, 0, out IntPtr pDevice);
                Marshal.Release(pEnumerator);

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
    }
}
