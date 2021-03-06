﻿using EarTrumpet.Interop;
using EarTrumpet.Interop.MMDeviceAPI;
using System;
using System.Diagnostics;

namespace EarTrumpet.DataModel.Internal.Services
{
    class AudioPolicyConfigService
    {
        const string DEVINTERFACE_AUDIO_RENDER = "#{e6327cad-dcec-4949-ae8a-991e976a79d2}";
        const string MMDEVAPI_TOKEN = @"\\?\SWD#MMDEVAPI#";

        static IAudioPolicyConfigFactory s_sharedPolicyConfig;

        private static void EnsurePolicyConfig()
        {
            if (s_sharedPolicyConfig == null)
            {
                Guid iid = typeof(IAudioPolicyConfigFactory).GUID;
                Combase.RoGetActivationFactory("Windows.Media.Internal.AudioPolicyConfig", ref iid, out object factory);
                s_sharedPolicyConfig = (IAudioPolicyConfigFactory)factory;
            }
        }

        private static string GenerateDeviceId(string deviceId)
        {
            return $"{MMDEVAPI_TOKEN}{deviceId}{DEVINTERFACE_AUDIO_RENDER}";
        }

        private static string UnpackDeviceId(string deviceId)
        {
            if (deviceId.StartsWith(MMDEVAPI_TOKEN)) deviceId = deviceId.Remove(0, MMDEVAPI_TOKEN.Length);
            if (deviceId.EndsWith(DEVINTERFACE_AUDIO_RENDER)) deviceId = deviceId.Remove(deviceId.Length - DEVINTERFACE_AUDIO_RENDER.Length);
            return deviceId;
        }

        public static void SetDefaultEndPoint(string deviceId, int processId)
        {
            Trace.WriteLine($"AudioPolicyConfigService SetDefaultEndPoint {deviceId} {processId}");
            try
            {
                EnsurePolicyConfig();

                IntPtr hstring = IntPtr.Zero;

                if (!string.IsNullOrWhiteSpace(deviceId))
                {
                    var str = GenerateDeviceId(deviceId);
                    Combase.WindowsCreateString(str, (uint)str.Length, out hstring);
                }

                s_sharedPolicyConfig.SetPersistedDefaultAudioEndpoint((uint)processId, EDataFlow.eRender, ERole.eMultimedia, hstring);
                s_sharedPolicyConfig.SetPersistedDefaultAudioEndpoint((uint)processId, EDataFlow.eRender, ERole.eConsole, hstring);
            }
            catch(Exception ex)
            {
                Trace.TraceError($"SetDefaultEndPoint Failed: {ex}");
            }
        }

        public static string GetDefaultEndPoint(int processId)
        {
            try
            {
                EnsurePolicyConfig();

                s_sharedPolicyConfig.GetPersistedDefaultAudioEndpoint((uint)processId, EDataFlow.eRender, ERole.eMultimedia | ERole.eConsole, out string deviceId);
                return UnpackDeviceId(deviceId);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"GetDefaultEndPoint Failed: {ex}");
            }

            return null;
        }
    }
}
