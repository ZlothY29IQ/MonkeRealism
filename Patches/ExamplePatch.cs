using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;

namespace MonkeRealism.Patches
{
	/// <summary>
	/// This is an example patch, made to demonstrate how to use Harmony. You should remove it if it is not used.
	/// </summary>
	[HarmonyPatch(typeof(GorillaLocomotion.GTPlayer))]
	[HarmonyPatch("Awake", MethodType.Normal)]
	internal class ExamplePatch
	{
		private static void Postfix(GorillaLocomotion.GTPlayer __instance)
		{
			Console.WriteLine(__instance.maxJumpSpeed);
		}
	}
}
