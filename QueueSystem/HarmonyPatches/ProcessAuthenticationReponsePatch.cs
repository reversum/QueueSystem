using System;
using System.Collections.Generic;
using CentralAuth;
using HarmonyLib;
using LabApi.Features.Wrappers;
using LabApi.Features.Console;
using System.Linq;
using System.Reflection;
using Cryptography;
using NorthwoodLib;
using static PlayerRoles.PlayerRoleManager;
using InventorySystem;
using LabApi.Loader;
using PlayerStatsSystem;

#if CedMod
using QuerySys = CedMod.Addons.QuerySystem.QuerySystem;
#endif

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

			if (Plugin.Instance.Config.SkipWithReservedSlot &&
			    msg.SignedAuthToken.TryGetToken<AuthenticationToken>("Authentication", out var token1, out var error1,
				    out var userId) &&
			    !string.IsNullOrEmpty(userId))
			{
				#if CedMod
				    if (PluginLoader.Plugins.Any(p => string.Equals(p.Key.Name, "CedMod", StringComparison.OrdinalIgnoreCase)) &&
				        QuerySys.ReservedSlotUserids.Contains(userId))
				        return true;
				#endif

				if (ReservedSlot.HasReservedSlot(userId)) return true;
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
					Plugin.Instance.WaitingQueue.Add(new QueueItem()
						{ PlayerAuthenticationManager = __instance, AuthenticationResponse = msg });
				}

				return false;
			}

			return true;
		}
	}
}
