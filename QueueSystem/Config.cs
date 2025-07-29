using System.Collections.Generic;
using System.ComponentModel;

namespace JoinQueuePatch
{
	public class Config
	{
		[Description("Whether the plugin is enabled.")]
		public bool IsEnabled { get; set; } = true;

		[Description("Enable debug logs.")]
		public bool Debug { get; set; } = false;
		public string QueueHintMessage { get; set; } =
	"<align=\"center\"><color=orange><size=25>⚠️ The Server is Full! ⚠️</size></color>\n" +
	"<color=#A9A9A9><size=23>But don’t worry! You are waiting in a queue!</size></color>\n" +
	"<color=#DCDCDC><size=21>Players waiting:</color> <color=#ADFF2F>{queue_count}</size></color>\n\n" +
	"<size=20>Your position in the queue: <color=#00FF00>#{position}</color></size>\n\n" +
	"<size=18>Round time <color=#2F4F4F>»</color> <color=#C0C0C0>{round_time} elapsed</color>";

		public string QueueLeaveHintMessage { get; set; } = "<color=green>You will now join!</color>";

		[Description("Priority order of queue groups. Higher in the list = higher priority.")]
		public List<string> QueueGroupPriority { get; set; } = new List<string> { "moderator", "owner" };
	}
}
