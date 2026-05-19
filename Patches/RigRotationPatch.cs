using System;
using HarmonyLib;
using UnityEngine;

namespace MonkeRealism.Patches
{
    [HarmonyPatch(typeof(VRRig), nameof(VRRig.PostTick))]
    public class RigRotationPatch
    {
        private static void Postfix(VRRig __instance)
        {
            try
            {
                if (!__instance.enabled)
                    return;
                if (!__instance.isLocal || !Plugin.Instance.ShouldUseTracker.Value)
                    return;

                __instance.transform.rotation = Plugin.Instance.TrackerFollower.transform.rotation;

                __instance.head.MapMine(__instance.scaleFactor, __instance.playerOffsetTransform);
                __instance.leftHand.MapMine(__instance.scaleFactor, __instance.playerOffsetTransform);
                __instance.rightHand.MapMine(__instance.scaleFactor, __instance.playerOffsetTransform);

                __instance.head.rigTarget.rotation = GorillaTagger.Instance.headCollider.transform.rotation;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MonkeRealism]VRRig.PostTick Path Postfix error: {e}");
            }
        }
    }
}
