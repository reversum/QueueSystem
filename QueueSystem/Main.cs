using HarmonyLib;
using System.Collections.Generic;
using CentralAuth;
using Hints;
using MEC;
using System.Reflection;
using System.Linq;
using LabApi.Loader.Features.Plugins;
using LabApi.Features;
using System;
using LabApi.Events.Handlers;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using LabApi.Features.Console;
using UnityEngine;
using Logger = LabApi.Features.Console.Logger;
using CommandSystem;
using Mirror;

namespace JoinQueuePatch
{
	public class Plugin : Plugin<Config>
	{
		public override string Name { get; } = "QueueSystem";
		public override string Description { get; } = "No Server full screen anymore.";
		public override string Author { get; } = "yannikaufdie1";
		public override Version Version { get; } = new Version(1, 0, 0, 0);
		public override Version RequiredApiVersion { get; } = new Version(LabApiProperties.CompiledVersion);

		public static Plugin Instance;
		private Harmony harmony;
		public List<QueueItem> WaitingQueue = new();

		public override void Enable()
		{
			harmony = new Harmony("de.yannik.scpsl.joinqueue");
			harmony.PatchAll();
			PlayerEvents.Left += OnPlayerLeft;

			WaitingQueue = new();
			Instance = this;
			Timing.RunCoroutine(QueueHintCoroutine());
		}

		public override void Disable()
		{
			harmony.UnpatchAll("de.yannik.scpsl.joinqueue");
			PlayerEvents.Left -= OnPlayerLeft;

			Timing.KillCoroutines();
		}

		public void OnPlayerLeft(PlayerLeftEventArgs ev)
		{
			if (IsInQueue(ev.Player.ReferenceHub))
			{
				RemoveFromQueue(ev.Player.ReferenceHub);
				return;
			}
			ProcessQueue();  
		}

		public IEnumerator<float> QueueHintCoroutine()
		{
			for (; ; )
			{
				foreach (var queue in WaitingQueue.ToList())
				{
					var found = WaitingQueue
						.Select((item, index) => new { item, index })
						.FirstOrDefault(x => x.item.PlayerAuthenticationManager == queue.PlayerAuthenticationManager);

					int position = (found == null) ? -1 : found.index + 1;

					var messageTemplate = Plugin.Instance.Config.QueueHintMessage;

					string hintMessage = messageTemplate
						.Replace("{queue_count}", Plugin.Instance.WaitingQueue.Count.ToString())
						.Replace("{position}", position.ToString())
						.Replace("{round_time}", Round.Duration.ToString(@"mm\:ss"))
						.Replace("{servername}", Server.PlayerListName);

					Instance.ShowHint(
						queue.PlayerAuthenticationManager._hub,
						hintMessage,
						3f
					);
				}
				yield return Timing.WaitForSeconds(1f);
			}
		}

		public bool IsInQueue(PlayerAuthenticationManager auth)
		{
			var authHub = auth._hub;

			return Instance.WaitingQueue.Any(x =>
			{
				var queueAuth = x.PlayerAuthenticationManager;
				var queueHub = queueAuth._hub;
				return Equals(queueHub, authHub);
			});
		}

		public bool IsInQueue(ReferenceHub hub)
		{
			foreach (var item in Instance.WaitingQueue)
			{
				var mgr = item.PlayerAuthenticationManager;
				var managerHub = mgr._hub;
				if (managerHub == hub)
					return true;
			}
			return false;
		}

		public void RemoveFromQueue(ReferenceHub hub)
		{
			if (WaitingQueue.Count == 0)
				return;

			WaitingQueue.RemoveAll(item => item.PlayerAuthenticationManager._hub == hub);
		}

		public void ShowHint(ReferenceHub hub, string message, float duration = 3f)
		{
			hub.hints.Show(new TextHint(message, new HintParameter[1]
			{
			new StringHintParameter(string.Empty)
			}, new HintEffect[0], duration));
		}

		private static int GetPriority(QueueItem item, Dictionary<string, int> map)
		{
			string groupName = null;

			if (item.AuthenticationResponse.SignedAuthToken.TryGetToken<AuthenticationToken>(
				"Authentication", out var token, out _, out var userId))
			{
				var userGroup = ServerStatic.PermissionsHandler.GetUserGroup(userId);
				groupName = userGroup?.Name;
			}

			if (groupName == null)
				return 999;

			return map.TryGetValue(groupName, out var prio) ? prio : 999;
		}

		public static void ProcessQueue()
		{
			var priorityMap = Instance.Config.QueueGroupPriority
				.Select((grp, idx) => new { grp, idx })
				.ToDictionary(x => x.grp, x => x.idx);

			Instance.WaitingQueue = Instance.WaitingQueue
				.OrderBy(item => GetPriority(item, priorityMap))
				.ToList();

			int activePlayers = Player.List.Count(p => p.IsReady && !p.IsHost);

			if (Instance.WaitingQueue.Count > 0 && activePlayers - 1 < Server.MaxPlayers)
			{
				var next = Instance.WaitingQueue[0];
				Instance.WaitingQueue.RemoveAt(0);

				var manager = next.PlayerAuthenticationManager;
				var authResponse = next.AuthenticationResponse;

				Instance.ShowHint(manager._hub, Instance.Config.QueueLeaveHintMessage);

				manager._authenticationRequested = true;
				manager._timeoutTimer = 0f;

				Timing.CallDelayed(1f, () =>
				{
					manager.ProcessAuthenticationResponse(authResponse);
				});
			}
		}
	}
}
