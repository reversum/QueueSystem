using CentralAuth;
using Exiled.API.Features;
using HarmonyLib;

namespace JoinQueuePatch.HarmonyPatches
{
	[HarmonyPatch(typeof(PlayerAuthenticationManager), nameof(PlayerAuthenticationManager.ProcessAuthenticationResponse))]
	[HarmonyPriority(Priority.First)]
	internal static class QueuePatch
	{
		static bool Prefix(PlayerAuthenticationManager __instance, CentralAuth.AuthenticationResponse msg)
		{
			int currentPlayers = Player.List.Count;
			int maxPlayers = Server.MaxPlayerCount;

			if (msg.SignedAuthToken.TryGetToken<AuthenticationToken>("Authentication", out var token1, out var error1, out var userId))
			{
				if (!string.IsNullOrEmpty(userId))
				{
					if (ReservedSlot.HasReservedSlot(userId)) return true;
				}
			}

			if (currentPlayers >= maxPlayers)
			{
				if (!Plugin.Instance.IsInQueue(__instance)) 
				{
					Plugin.Instance.WaitingQueue.Enqueue(new QueueItem() { PlayerAuthenticationManager = __instance, AuthenticationResponse = msg });
				}
				return false;
			}
			return true;
		}
	}
}
