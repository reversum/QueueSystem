using CentralAuth;
using HarmonyLib;
using LabApi.Features.Wrappers;
using LabApi.Features.Console;
using System.Linq;
using Cryptography;
using NorthwoodLib;

namespace JoinQueuePatch.HarmonyPatches
{
	[HarmonyPatch(typeof(PlayerAuthenticationManager), nameof(PlayerAuthenticationManager.ProcessAuthenticationResponse))]
	[HarmonyPriority(Priority.First)]
	internal static class QueuePatch
	{
		static bool Prefix(PlayerAuthenticationManager __instance, CentralAuth.AuthenticationResponse msg)
		{
			int currentPlayers = Player.List.Count(p => p.IsReady == true && !p.IsHost);
			int maxPlayers = Server.MaxPlayers;

			if (Plugin.Instance.Config.SubtractReservedSlots)
			{
				int reservedCount = Player.List.Count(p =>
				{
					return !string.IsNullOrEmpty(p.UserId) && ReservedSlot.HasReservedSlot(p.UserId);
				});

				currentPlayers -= reservedCount;
				if (currentPlayers < 0) currentPlayers = 0;
			}
;
			if (msg.SignedAuthToken.TryGetToken<AuthenticationToken>("Authentication", out var token1, out var error1, out var userId))
			{
				if (!string.IsNullOrEmpty(userId))
				{
					if (ReservedSlot.HasReservedSlot(userId)) return true;
				}
			}

			if (Plugin.Instance.Config.AllowNWStaffToSkipQueue && msg.SignedBadgeToken != null && msg.AuthToken != null)
			{
				if (msg.SignedBadgeToken.TryGetToken<BadgeToken>(
					"Badge request", out var token2, out var error2, out string _))
				{
					if (token2 != null && __instance?.SaltedUserId != null && __instance?._hub?.nicknameSync != null)
					{
						bool success = true;

						if (token2.Serial != msg.AuthToken.Serial)
							success = false;

						if (token2.UserId != Sha.HashToString(Sha.Sha512(__instance.SaltedUserId)))
							success = false;

						if (StringUtils.Base64Decode(token2.Nickname) != __instance._hub.nicknameSync.MyNick)
							success = false;

						if (success && token2.Staff) return true;
					}
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
