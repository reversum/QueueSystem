using HarmonyLib;
using System.Collections.Generic;
using CentralAuth;
using Hints;
using UnityEngine;
using MEC;
using System.Reflection;
using System.Linq;
using LabApi.Loader.Features.Plugins;
using LabApi.Features;
using System;
using LabApi.Events.Handlers;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;

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
		public Queue<QueueItem> WaitingQueue = new();

		public override void Enable()
		{
			harmony = new Harmony("de.yannik.scpsl.joinqueue");
			harmony.PatchAll();
			PlayerEvents.Left += OnPlayerLeft;
			ServerEvents.RoundStarted += OnRoundStart;

			WaitingQueue = new();
			Instance = this;
			Timing.RunCoroutine(QueueHintCoroutine());
		}

		public override void Disable()
		{
			harmony.UnpatchAll("de.yannik.scpsl.joinqueue");
			PlayerEvents.Left -= OnPlayerLeft;
			ServerEvents.RoundStarted -= OnRoundStart;
			Timing.KillCoroutines();
		}

		private void OnPlayerLeft(PlayerLeftEventArgs ev)
		{
			if (IsInQueue(ev.Player.ReferenceHub))
			{
				RemoveFromQueue(ev.Player.ReferenceHub);
				return;
			}
			ProcessQueue();  
		}

		private void OnRoundStart()
		{
			Timing.CallDelayed(0.1f, () =>
			{
				GameObject.Find("StartRound").transform.localScale = Vector3.zero;
			});
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
						.Replace("{round_time}", Round.Duration.ToString(@"mm\:ss"));

					Plugin.Instance.SendHint(
						queue.PlayerAuthenticationManager,
						hintMessage,
						3f
					);
				}
				yield return Timing.WaitForSeconds(1f);
			}
		}

		public bool IsInQueue(PlayerAuthenticationManager auth)
		{
			return WaitingQueue.FirstOrDefault(x => x.PlayerAuthenticationManager._hub == auth._hub) != null;
		}
		public bool IsInQueue(ReferenceHub hub)
		{
			return WaitingQueue.FirstOrDefault(x => x.PlayerAuthenticationManager._hub == hub) != null;
		}
		public void RemoveFromQueue(ReferenceHub hub)
		{
			if (WaitingQueue.Count == 0)
				return;

			var newQueue = new Queue<QueueItem>();
			while (WaitingQueue.Count > 0)
			{
				var item = WaitingQueue.Dequeue();
				if (item.PlayerAuthenticationManager._hub != hub)
				{
					newQueue.Enqueue(item);
				}
			}

			WaitingQueue = newQueue;
		}

		public void ShowHint(ReferenceHub hub, string message, float duration = 3f)
		{
			ShowHint(hub, message, new HintParameter[1]
			{
			new StringHintParameter(message)
			}, null, duration);
		}
		
		public void ShowHint(ReferenceHub hub, string message, HintParameter[] hintParameters, HintEffect[] hintEffects, float duration = 3f)
		{
			if (message == null)
			{
				message = string.Empty;
			}

			hub.hints.Show(new TextHint(message, (!hintParameters.IsEmpty()) ? hintParameters : new HintParameter[1]
			{
			new StringHintParameter(message)
			}, hintEffects, duration));
		}

		public void SendHint(PlayerAuthenticationManager manager, string message, float duration = 3f)
		{
			var hub = (ReferenceHub)AccessTools.Field(typeof(PlayerAuthenticationManager), "_hub")?.GetValue(manager);

			if (hub?.hints != null)
			{
				ShowHint(hub, message, duration);
			}
		}

		public static void ProcessQueue()
		{
			while (Instance.WaitingQueue.Count > 0 && Player.List.Count - 1 < Server.MaxPlayers)
			{
				var next = Instance.WaitingQueue.Dequeue();
				var manager = next.PlayerAuthenticationManager;
				var authResponse = next.AuthenticationResponse;

				Instance.SendHint(manager, Instance.Config.QueueLeaveHintMessage);

				var authRequestedField = typeof(CentralAuth.PlayerAuthenticationManager)
					.GetField("_authenticationRequested", BindingFlags.Instance | BindingFlags.NonPublic);
				authRequestedField?.SetValue(manager, true);

				var timeoutTimerField = typeof(CentralAuth.PlayerAuthenticationManager)
					.GetField("_timeoutTimer", BindingFlags.Instance | BindingFlags.NonPublic);
				timeoutTimerField?.SetValue(manager, 0f);

				var processMethod = typeof(CentralAuth.PlayerAuthenticationManager)
					.GetMethod("ProcessAuthenticationResponse", BindingFlags.Instance | BindingFlags.NonPublic);

				Timing.CallDelayed(1f, () =>
				{
					processMethod?.Invoke(manager, new object[] { authResponse });
				});

				break;
			}
		}
	}
}
