using System;
using System.Runtime.InteropServices;

namespace SystemHub.Services
{
    public enum ERole : uint
    {
        eConsole = 0,
        eMultimedia = 1,
        eCommunications = 2
    }

    [Guid("f8679f50-850a-41cf-9c72-430f290290c8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPolicyConfig
    {
        [PreserveSig] int GetMixFormat();
        [PreserveSig] int GetDeviceFormat();
        [PreserveSig] int ResetDeviceFormat();
        [PreserveSig] int SetDeviceFormat();
        [PreserveSig] int GetProcessingPeriod();
        [PreserveSig] int SetProcessingPeriod();
        [PreserveSig] int GetShareMode();
        [PreserveSig] int SetShareMode();
        [PreserveSig] int GetPropertyValue();
        [PreserveSig] int SetPropertyValue();
        [PreserveSig] int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string wszDeviceId, ERole eRole);
        [PreserveSig] int SetEndpointVisibility();
    }

    [ComImport, Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
    internal class CPolicyConfigClient { }

    public static class AudioDeviceSwitcher
    {
        public static void SetDefaultDevice(string deviceId)
        {
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    IPolicyConfig policyConfig = (IPolicyConfig)new CPolicyConfigClient();
                    policyConfig.SetDefaultEndpoint(deviceId, ERole.eConsole);
                    policyConfig.SetDefaultEndpoint(deviceId, ERole.eMultimedia);
                    policyConfig.SetDefaultEndpoint(deviceId, ERole.eCommunications);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error setting default endpoint: " + ex.Message);
                }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join(3000);
        }
    }
}

