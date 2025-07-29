using CentralAuth;
using HarmonyLib;

namespace JoinQueuePatch.HarmonyPatches
{
	[HarmonyPatch(typeof(PlayerAuthenticationManager), nameof(PlayerAuthenticationManager.RejectAuthentication))]
	[HarmonyPriority(Priority.First)]
	internal static class RejectAuthPatch
	{
		static bool Prefix(PlayerAuthenticationManager __instance)
		{
			if (Plugin.Instance.IsInQueue(__instance))
			{
				return false;
			}
			return true;
		}
	}
}
