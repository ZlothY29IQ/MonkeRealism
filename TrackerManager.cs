using System;
using UnityEngine;
using Valve.VR; //from openvr_api.cs

namespace MonkeRealism
{
    public static class TrackerManager
    {
        private static bool initialized = false;
        public static string hipTrackerSerial = "WAIST"; 
        public static bool IsInitialized => initialized;

        public static void Initialize()
        {
            EVRInitError error = EVRInitError.None;
            OpenVR.Init(ref error, EVRApplicationType.VRApplication_Background);
            initialized = (error == EVRInitError.None && OpenVR.System != null);
            Debug.Log($"[MonkeRealism] OpenVR init set to {initialized}");
        }

        public static Quaternion? GetHipTrackerRotation()
        {
            if (!initialized || OpenVR.System == null)
                return null;

            var poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            OpenVR.System.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0, poses);

            for (uint i = 0; i < poses.Length; i++)
            {
                if (!poses[i].bDeviceIsConnected || !poses[i].bPoseIsValid)
                    continue;

                var deviceClass = OpenVR.System.GetTrackedDeviceClass(i);
                if (deviceClass != ETrackedDeviceClass.GenericTracker)
                    continue;

                string serial = GetDeviceSerial(i);
                //Debug.Log($"[MonkeRealism] Found tracker {i} with serial: {serial}");
                //Use this for degbugging if your dont know your trackers serial name

                if (serial.ToLower().Contains(hipTrackerSerial.ToLower()))
                {
                    var matrix = ConvertSteamVRMatrixToUnity(poses[i].mDeviceToAbsoluteTracking);
                    return Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
                }
            }

            return null;
        }

        public static string GetDeviceSerial(uint i)
        {
            var error = ETrackedPropertyError.TrackedProp_Success;
            var capacity = OpenVR.System.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_SerialNumber_String, null, 0, ref error);

            if (capacity > 1)
            {
                var result = new System.Text.StringBuilder((int)capacity);
                OpenVR.System.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_SerialNumber_String, result, capacity, ref error);
                return result.ToString();
            }

            return null;
        }

        public static Matrix4x4 ConvertSteamVRMatrixToUnity(HmdMatrix34_t pose)
        {
            Matrix4x4 matrix = new Matrix4x4();

            matrix.m00 = pose.m0;
            matrix.m01 = pose.m1;
            matrix.m02 = pose.m2;
            matrix.m03 = pose.m3;

            matrix.m10 = pose.m4;
            matrix.m11 = pose.m5;
            matrix.m12 = pose.m6;
            matrix.m13 = pose.m7;

            matrix.m20 = pose.m8;
            matrix.m21 = pose.m9;
            matrix.m22 = pose.m10;
            matrix.m23 = pose.m11;

            matrix.m30 = 0f;
            matrix.m31 = 0f;
            matrix.m32 = 0f;
            matrix.m33 = 1f;

            return matrix;
        }

       

    }
}
