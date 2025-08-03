using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using HarmonyLib;
using System.Collections.Generic;
using CentralAuth;
using Hints;
using UnityEngine;
using MEC;
using System.Reflection;
using System.Linq;
using System;

namespace JoinQueuePatch
{
	public class Plugin : Plugin<Config>
	{
		public static Plugin Instance;
		public override string Name => "QueueSystem";
		public override string Author => "yannikaufdie1";
		public override Version RequiredExiledVersion => new Version(9, 7, 0);
		private Harmony harmony;
		public Queue<QueueItem> WaitingQueue = new();

		public override void OnEnabled()
		{
			harmony = new Harmony("de.yannik.scpsl.joinqueue");
			harmony.PatchAll();
			Exiled.Events.Handlers.Player.Left += OnPlayerLeft;

			WaitingQueue = new();
			Instance = this;
			Timing.RunCoroutine(QueueHintCoroutine());
		}

		public override void OnDisabled()
		{
			harmony.UnpatchAll("de.yannik.scpsl.joinqueue");
			Exiled.Events.Handlers.Player.Left -= OnPlayerLeft;
			Timing.KillCoroutines();
		}

		private void OnPlayerLeft(LeftEventArgs ev)
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

					var messageTemplate = Instance.Config.QueueHintMessage;

					string hintMessage = messageTemplate
						.Replace("{queue_count}", Instance.WaitingQueue.Count.ToString())
						.Replace("{position}", position.ToString())
						.Replace("{round_time}", Round.ElapsedTime.ToString(@"mm\:ss"))
						.Replace("{servername}", Server.Name);

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

			return WaitingQueue.Any(x =>
			{
				var queueAuth = x.PlayerAuthenticationManager;
				var queueHub = queueAuth._hub;
				return Equals(queueHub, authHub);
			});
		}

		public static bool IsInQueue(ReferenceHub hub)
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

			var newQueue = new Queue<QueueItem>();
			while (WaitingQueue.Count > 0)
			{
				var item = WaitingQueue.Dequeue();
				var itemhub = item.PlayerAuthenticationManager._hub;

				if (itemhub != hub)
				{
					newQueue.Enqueue(item);
				}
			}

			WaitingQueue = newQueue;
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

			var sorted = Instance.WaitingQueue
				.OrderBy(item => GetPriority(item, priorityMap))
				.ToList();

			Instance.WaitingQueue = new Queue<QueueItem>(sorted);

			while (Instance.WaitingQueue.Count > 0 && Player.List.Count - 1 < Server.MaxPlayerCount)
			{
				var next = Instance.WaitingQueue.Dequeue();
				var manager = next.PlayerAuthenticationManager;
				var authResponse = next.AuthenticationResponse;

				Instance.ShowHint(manager._hub, Instance.Config.QueueLeaveHintMessage);

				manager._authenticationRequested = true;
				manager._timeoutTimer = 0f;

				Timing.CallDelayed(1f, () =>
				{
					manager.ProcessAuthenticationResponse(authResponse);
				});

				break;
			}
		}
	}
}
