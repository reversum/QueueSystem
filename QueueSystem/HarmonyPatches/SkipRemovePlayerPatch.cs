using HarmonyLib;
using LabApi.Features.Console;
using LabApi.Features.Wrappers; // Player
using System.Reflection;

namespace JoinQueuePatch.HarmonyPatches
{
	[HarmonyPatch]
	internal static class RemovePlayerPatch
	{
		static MethodBase TargetMethod()
		{
			return AccessTools.Method(typeof(Player), "RemovePlayer", new[] { typeof(ReferenceHub) });
		}

		static bool Prefix(ReferenceHub referenceHub)
		{
			if (Plugin.Instance.IsInQueue(referenceHub.authManager))
			{
				return false; 
			}
			return true; 
		}
	}
}
